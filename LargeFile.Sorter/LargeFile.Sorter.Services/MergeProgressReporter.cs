using System.Diagnostics;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class MergeProgressReporter : IMergeProgressReporter
{
    private readonly ILogger<MergeProgressReporter> _logger;
    private readonly TimeSpan _reportInterval;
    private readonly Lock _syncRoot = new();
    private readonly Stopwatch _stopwatch = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _reportingTask;
    private int _currentPassNumber;
    private int _currentPassInputFileCount;
    private int _currentPassPlannedBatchCount;
    private int _currentPassCompletedBatchCount;
    private int _currentPassFailedBatchCount;
    private int _activeBatchCount;
    private int _lastPassOutputFileCount;
    private int _totalCompletedBatchCount;
    private bool _started;
    private bool _completed;

    public MergeProgressReporter(
        ILogger<MergeProgressReporter> logger,
        MergeProgressConfig mergeProgressConfig)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(mergeProgressConfig);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mergeProgressConfig.ReportInterval.Ticks);

        _logger = logger;
        _reportInterval = mergeProgressConfig.ReportInterval;
    }

    public void Start(int initialFileCount, int maxChunkFilesPerMerge)
    {
        lock (_syncRoot)
        {
            if (_started)
            {
                throw new InvalidOperationException("Merge progress reporting has already been started.");
            }

            _started = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _stopwatch.Start();
            _reportingTask = RunReportingLoopAsync(_cancellationTokenSource.Token);
        }

        _logger.LogInformation(
            "Merge progress reporting started. Initial temp files: {InitialFileCount}. Max files per merge: {MaxChunkFilesPerMerge}. Report interval: {ReportInterval}.",
            initialFileCount,
            maxChunkFilesPerMerge,
            _reportInterval);
    }

    public void ReportPassStarted(int passNumber, int inputFileCount, int plannedBatchCount)
    {
        Interlocked.Exchange(ref _currentPassNumber, passNumber);
        Interlocked.Exchange(ref _currentPassInputFileCount, inputFileCount);
        Interlocked.Exchange(ref _currentPassPlannedBatchCount, plannedBatchCount);
        Interlocked.Exchange(ref _currentPassCompletedBatchCount, 0);
        Interlocked.Exchange(ref _currentPassFailedBatchCount, 0);
        Interlocked.Exchange(ref _activeBatchCount, 0);

        _logger.LogInformation(
            "Merge pass {PassNumber} started. Input temp files: {InputFileCount}. Planned batches: {PlannedBatchCount}.",
            passNumber,
            inputFileCount,
            plannedBatchCount);
    }

    public void ReportBatchScheduled()
    {
        Interlocked.Increment(ref _activeBatchCount);
    }

    public void ReportBatchCompleted()
    {
        Interlocked.Increment(ref _currentPassCompletedBatchCount);
        Interlocked.Increment(ref _totalCompletedBatchCount);
        Interlocked.Decrement(ref _activeBatchCount);
    }

    public void ReportBatchFailed()
    {
        Interlocked.Increment(ref _currentPassFailedBatchCount);
        Interlocked.Decrement(ref _activeBatchCount);
    }

    public void ReportPassCompleted(int passNumber, int outputFileCount)
    {
        Interlocked.Exchange(ref _lastPassOutputFileCount, outputFileCount);

        _logger.LogInformation(
            "Merge pass {PassNumber} completed. Output temp files: {OutputFileCount}.",
            passNumber,
            outputFileCount);
    }

    public void ReportFinalPromoted()
    {
        _logger.LogInformation(
            "Merge final output promoted successfully after {Elapsed}.",
            FormatElapsed(_stopwatch.Elapsed));
    }

    public async ValueTask CompleteAsync()
    {
        Task? reportingTask;
        CancellationTokenSource? cancellationTokenSource;

        lock (_syncRoot)
        {
            if (!_started || _completed)
            {
                return;
            }

            _completed = true;
            reportingTask = _reportingTask;
            cancellationTokenSource = _cancellationTokenSource;
        }

        cancellationTokenSource?.Cancel();
        if (reportingTask is not null)
        {
            await reportingTask;
        }

        _stopwatch.Stop();
        LogSnapshot("Merge phase completed");
        cancellationTokenSource?.Dispose();
    }

    private async Task RunReportingLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(_reportInterval);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                LogSnapshot("Merge progress");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Merge progress reporting loop was canceled.");
        }
    }

    private void LogSnapshot(string messagePrefix)
    {
        var currentPassNumber = Interlocked.CompareExchange(ref _currentPassNumber, 0, 0);
        var currentPassInputFileCount = Interlocked.CompareExchange(ref _currentPassInputFileCount, 0, 0);
        var currentPassPlannedBatchCount = Interlocked.CompareExchange(ref _currentPassPlannedBatchCount, 0, 0);
        var activeBatchCount = Interlocked.CompareExchange(ref _activeBatchCount, 0, 0);
        var lastPassOutputFileCount = Interlocked.CompareExchange(ref _lastPassOutputFileCount, 0, 0);
        var totalCompletedBatchCount = Interlocked.CompareExchange(ref _totalCompletedBatchCount, 0, 0);

        _logger.LogInformation(
            "{MessagePrefix}: pass {PassNumber}, input temp files: {InputFileCount}, planned batches: {PlannedBatchCount}, active batches: {ActiveBatchCount}, last pass outputs: {LastPassOutputFileCount}, total completed batches: {TotalCompletedBatchCount}, elapsed: {Elapsed}",
            messagePrefix,
            currentPassNumber,
            currentPassInputFileCount,
            currentPassPlannedBatchCount,
            activeBatchCount,
            lastPassOutputFileCount,
            totalCompletedBatchCount,
            FormatElapsed(_stopwatch.Elapsed));
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        return elapsed.ToString(@"hh\:mm\:ss");
    }
}
