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
    private readonly IMergeProgressReporter _mergeProgressReporter;
    private readonly ILogger<MergeSortingCoordinator> _logger;
    private readonly Queue<IReadOnlyList<ITempFileAdapter>> _pendingBatches = [];
    private readonly List<Task<ITempFileAdapter>> _activeBatchTasks = [];
    private List<ITempFileAdapter> _currentPassFiles = [];
    private int _nextBatchNumber;
    private int _passNumber;
    private bool _started;

    public MergeSortingCoordinator(
        MergeConfig mergeConfig,
        SorterRunOptions sorterRunOptions,
        IMergeBatchProcessor mergeBatchProcessor,
        IMergeExecutionLimiter mergeExecutionLimiter,
        IMergeProgressReporter mergeProgressReporter,
        ILogger<MergeSortingCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(mergeConfig);
        ArgumentNullException.ThrowIfNull(sorterRunOptions);
        ArgumentNullException.ThrowIfNull(mergeBatchProcessor);
        ArgumentNullException.ThrowIfNull(mergeExecutionLimiter);
        ArgumentNullException.ThrowIfNull(mergeProgressReporter);
        ArgumentNullException.ThrowIfNull(logger);

        _mergeConfig = mergeConfig;
        _sorterRunOptions = sorterRunOptions;
        _mergeBatchProcessor = mergeBatchProcessor;
        _mergeExecutionLimiter = mergeExecutionLimiter;
        _mergeProgressReporter = mergeProgressReporter;
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
        _nextBatchNumber = 0;
        _passNumber = 1;
        _currentPassFiles = initialFiles
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value)
            .ToList();

        _mergeProgressReporter.Start(_currentPassFiles.Count, _mergeConfig.MaxChunkFilesPerMerge);

        if (_currentPassFiles.Count > 1)
        {
            EnqueueBatches(_currentPassFiles);
            _mergeProgressReporter.ReportPassStarted(_passNumber, _currentPassFiles.Count, _pendingBatches.Count);
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
        var batchNumber = ++_nextBatchNumber;

        _logger.LogDebug(
            "Scheduling merge batch {BatchNumber} for pass {PassNumber}. Batch size: {BatchFileCount}. Pending batches left: {PendingBatchCount}.",
            batchNumber,
            currentPassNumber,
            batch.Count,
            _pendingBatches.Count);

        await _mergeExecutionLimiter.WaitAsync(cancellationToken);
        try
        {
            _mergeProgressReporter.ReportBatchScheduled();
            var batchTask = ProcessBatchAsync(batch, currentPassNumber, batchNumber, cancellationToken);
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
        _mergeProgressReporter.ReportPassCompleted(completedPassNumber, nextPassFiles.Length);

        if (_currentPassFiles.Count > 1)
        {
            _passNumber++;
            EnqueueBatches(_currentPassFiles);
            _mergeProgressReporter.ReportPassStarted(_passNumber, _currentPassFiles.Count, _pendingBatches.Count);
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
        _mergeProgressReporter.ReportFinalPromoted();
        await finalFile.DisposeAsync();
    }

    private async Task<ITempFileAdapter> ProcessBatchAsync(
        IReadOnlyList<ITempFileAdapter> batch,
        int passNumber,
        int batchNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Starting merge batch {BatchNumber} for pass {PassNumber} with {BatchFileCount} temp files.",
                batchNumber,
                passNumber,
                batch.Count);

            var result = await _mergeBatchProcessor.MergeBatchAsync(batch, passNumber, batchNumber, cancellationToken);
            _mergeProgressReporter.ReportBatchCompleted();
            return result;
        }
        catch
        {
            _mergeProgressReporter.ReportBatchFailed();
            throw;
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
