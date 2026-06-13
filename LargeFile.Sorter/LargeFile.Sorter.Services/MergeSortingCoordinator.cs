using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class MergeSortingCoordinator : IMergeSortingCoordinator
{
    private readonly MergeConfig _mergeConfig;
    private readonly SorterRunOptions _sorterRunOptions;
    private readonly IMergeBatchProcessor _mergeBatchProcessor;
    private readonly IMergeExecutionLimiter _mergeExecutionLimiter;
    private readonly ILogger<MergeSortingCoordinator> _logger;
    private readonly Queue<IReadOnlyList<ITempFileAdapter>> _pendingBatches = [];
    private readonly List<Task<ITempFileAdapter>> _activeBatchTasks = [];
    private List<ITempFileAdapter> _currentPassFiles = [];
    private int _passNumber;
    private bool _started;

    public MergeSortingCoordinator(
        MergeConfig mergeConfig,
        SorterRunOptions sorterRunOptions,
        IMergeBatchProcessor mergeBatchProcessor,
        IMergeExecutionLimiter mergeExecutionLimiter,
        ILogger<MergeSortingCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(mergeConfig);
        ArgumentNullException.ThrowIfNull(sorterRunOptions);
        ArgumentNullException.ThrowIfNull(mergeBatchProcessor);
        ArgumentNullException.ThrowIfNull(mergeExecutionLimiter);
        ArgumentNullException.ThrowIfNull(logger);

        _mergeConfig = mergeConfig;
        _sorterRunOptions = sorterRunOptions;
        _mergeBatchProcessor = mergeBatchProcessor;
        _mergeExecutionLimiter = mergeExecutionLimiter;
        _logger = logger;
    }

    public bool HasNextBatch
    {
        get => _pendingBatches.Count > 0;
    }

    public bool HasMoreThanOneFile
    {
        get => _currentPassFiles.Count > 1;
    }

    public void Start(IReadOnlyDictionary<int, ITempFileAdapter> initialFiles)
    {
        ArgumentNullException.ThrowIfNull(initialFiles);

        if (_started)
        {
            throw new InvalidOperationException("Merge sorting coordinator has already been started.");
        }

        _logger.LogDebug(
            "Merge sorting coordinator started with {InitialFileCount} temp files. Batch size: {BatchSize}.",
            initialFiles.Count,
            _mergeConfig.MaxChunkFilesPerMerge);

        _started = true;
        _passNumber = 1;
        _currentPassFiles = initialFiles
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value)
            .ToList();

        if (_currentPassFiles.Count > 1)
        {
            EnqueueBatches(_currentPassFiles);
        }

        _logger.LogDebug(
            "Planned {BatchCount} merge batches for pass {PassNumber}.",
            _pendingBatches.Count,
            _passNumber);
    }

    public async Task MergeNextBatchAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingBatches.Count == 0)
        {
            throw new InvalidOperationException("No pending merge batches are available.");
        }

        var batch = _pendingBatches.Dequeue();
        var currentPassNumber = _passNumber;

        _logger.LogDebug(
            "Scheduling merge batch for pass {PassNumber}. Batch size: {BatchFileCount}. Pending batches left: {PendingBatchCount}.",
            currentPassNumber,
            batch.Count,
            _pendingBatches.Count);

        await _mergeExecutionLimiter.WaitAsync(cancellationToken);
        try
        {
            var batchTask = ProcessBatchAsync(batch, currentPassNumber, cancellationToken);
            _activeBatchTasks.Add(batchTask);
        }
        catch
        {
            _mergeExecutionLimiter.Release();
            throw;
        }
    }

    public async Task CompleteCurrentPassAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingBatches.Count > 0)
        {
            throw new InvalidOperationException("Cannot complete current merge pass while pending batches still exist.");
        }

        var activeBatchTasks = _activeBatchTasks.ToArray();
        _activeBatchTasks.Clear();
        var completedPassNumber = _passNumber;

        _logger.LogDebug(
            "Completing merge pass {PassNumber}. Awaiting {ActiveBatchCount} active batches.",
            completedPassNumber,
            activeBatchTasks.Length);

        var nextPassFiles = await Task.WhenAll(activeBatchTasks).WaitAsync(cancellationToken);

        _currentPassFiles = [.. nextPassFiles];
        if (_currentPassFiles.Count > 1)
        {
            _passNumber++;
            EnqueueBatches(_currentPassFiles);
        }

        _logger.LogDebug(
            "Completed merge pass {PassNumber}. Reduced to {OutputFileCount} temp files.",
            completedPassNumber,
            nextPassFiles.Length);
    }

    public async Task PromoteFinalAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_started)
        {
            throw new InvalidOperationException("Merge sorting coordinator has not been started.");
        }

        if (_currentPassFiles.Count != 1 || _pendingBatches.Count != 0 || _activeBatchTasks.Count != 0)
        {
            throw new InvalidOperationException("Final merge file is not ready for promotion.");
        }

        var finalFile = _currentPassFiles[0];
        _logger.LogDebug("Promoting final merged temp file to output: {TempFilePath}", finalFile.FilePath);
        PromoteFileToOutput(finalFile.FilePath);
        await finalFile.DisposeAsync();
    }

    private async Task<ITempFileAdapter> ProcessBatchAsync(
        IReadOnlyList<ITempFileAdapter> batch,
        int passNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Starting merge batch for pass {PassNumber} with {BatchFileCount} temp files.",
                passNumber,
                batch.Count);

            return await _mergeBatchProcessor.MergeBatchAsync(batch, cancellationToken);
        }
        finally
        {
            _mergeExecutionLimiter.Release();
        }
    }

    private void EnqueueBatches(IReadOnlyList<ITempFileAdapter> currentPassFiles)
    {
        _pendingBatches.Clear();

        for (var index = 0; index < currentPassFiles.Count; index += _mergeConfig.MaxChunkFilesPerMerge)
        {
            _pendingBatches.Enqueue(
                currentPassFiles
                    .Skip(index)
                    .Take(_mergeConfig.MaxChunkFilesPerMerge)
                    .ToList());
        }
    }

    private void PromoteFileToOutput(string sourcePath)
    {
        var outputDirectoryPath = Path.GetDirectoryName(_sorterRunOptions.OutputFilePath);
        if (!string.IsNullOrWhiteSpace(outputDirectoryPath))
        {
            Directory.CreateDirectory(outputDirectoryPath);
        }

        if (File.Exists(_sorterRunOptions.OutputFilePath))
        {
            _logger.LogWarning("Output file already exists and will be replaced: {OutputFilePath}", _sorterRunOptions.OutputFilePath);
            File.Delete(_sorterRunOptions.OutputFilePath);
        }

        File.Move(sourcePath, _sorterRunOptions.OutputFilePath);
        _logger.LogDebug("Promoted merged file to output: {OutputFilePath}", _sorterRunOptions.OutputFilePath);
    }
}
