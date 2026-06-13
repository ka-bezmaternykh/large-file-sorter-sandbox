using LargeFile.Sorter.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LargeFile.Sorter.Services;

public sealed class SorterApplication : ISorterApplication
{
    private readonly IEnvironmentMonitor _environmentMonitor;
    private readonly IChunkSorterFactory _chunkSorterFactory;
    private readonly IChunkingProgressReporter _chunkingProgressReporter;
    private readonly IInputFileAdapter _inputFileAdapter;
    private readonly IMergeSortingCoordinator _mergeSortingCoordinator;
    private readonly IInputFileReader _inputFileReader;
    private readonly ILogger<SorterApplication> _logger;

    public SorterApplication(
        IEnvironmentMonitor environmentMonitor,
        IChunkSorterFactory chunkSorterFactory,
        IChunkingProgressReporter chunkingProgressReporter,
        IInputFileAdapter inputFileAdapter,
        IMergeSortingCoordinator mergeSortingCoordinator,
        IInputFileReader inputFileReader,
        ILogger<SorterApplication> logger)
    {
        ArgumentNullException.ThrowIfNull(environmentMonitor);
        ArgumentNullException.ThrowIfNull(chunkSorterFactory);
        ArgumentNullException.ThrowIfNull(chunkingProgressReporter);
        ArgumentNullException.ThrowIfNull(inputFileAdapter);
        ArgumentNullException.ThrowIfNull(mergeSortingCoordinator);
        ArgumentNullException.ThrowIfNull(inputFileReader);
        ArgumentNullException.ThrowIfNull(logger);

        _environmentMonitor = environmentMonitor;
        _chunkSorterFactory = chunkSorterFactory;
        _chunkingProgressReporter = chunkingProgressReporter;
        _inputFileAdapter = inputFileAdapter;
        _mergeSortingCoordinator = mergeSortingCoordinator;
        _inputFileReader = inputFileReader;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var inputFileReader = _inputFileReader;

        try
        {
            _environmentMonitor.WriteEnvironment();
            _logger.LogDebug("Starting chunking pipeline from input file.");
            _chunkingProgressReporter.Start(_inputFileAdapter.GetInputFileSizeBytes());

            while (await inputFileReader.HasNextAsync(cancellationToken))
            {
                var chunkSorter = await _chunkSorterFactory.CreateAsync(cancellationToken);
                await inputFileReader.ReadNextAsync(chunkSorter.Writer, chunkSorter.ChunkSize, cancellationToken);
            }

            await _chunkSorterFactory.WaitAllAsync(cancellationToken);
            await _chunkingProgressReporter.CompleteAsync();

            _logger.LogDebug("Chunk pipeline completed.");

            if (_chunkSorterFactory.ChunkFileAdapters.Count == 0)
            {
                _logger.LogWarning("Chunk pipeline produced no temp files. Merge phase will be skipped.");
                return;
            }

            _logger.LogInformation("Starting merging pipeline.");
            _mergeSortingCoordinator.Start(_chunkSorterFactory.ChunkFileAdapters);

            while (_mergeSortingCoordinator.HasMoreThanOneFile)
            {
                while (_mergeSortingCoordinator.HasNextBatch)
                {
                    await _mergeSortingCoordinator.MergeNextBatchAsync(cancellationToken);
                }

                await _mergeSortingCoordinator.CompleteCurrentPassAsync(cancellationToken);
            }

            await _mergeSortingCoordinator.PromoteFinalAsync(cancellationToken);
            _logger.LogInformation("Merging pipeline completed.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Sorter application execution failed.");
            throw;
        }
        finally
        {
            await _chunkingProgressReporter.CompleteAsync();

            var process = Process.GetCurrentProcess();
            _logger.LogInformation("Small set of memory metrics in the end");
            _logger.LogInformation("Peak working set: {PeakWorkingSetMb} MB", process.PeakWorkingSet64 / 1024 / 1024);
            _logger.LogInformation("Current working set: {CurrentWorkingSetMb} MB", process.WorkingSet64 / 1024 / 1024);
        }
    }

}
