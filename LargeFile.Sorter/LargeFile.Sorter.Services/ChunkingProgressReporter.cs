using System.Diagnostics;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class ChunkingProgressReporter : IChunkingProgressReporter
{
    private readonly ILogger<ChunkingProgressReporter> _logger;
    private readonly TimeSpan _reportInterval;
    private readonly Lock _syncRoot = new();
    private readonly Stopwatch _stopwatch = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _reportingTask;
    private long _totalInputBytes;
    private long _bytesRead;
    private int _chunksCreated;
    private int _chunksCompleted;
    private int _activeSorters;
    private bool _started;
    private bool _completed;

    public ChunkingProgressReporter(
        ILogger<ChunkingProgressReporter> logger,
        ChunkingProgressConfig chunkingProgressConfig)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(chunkingProgressConfig);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkingProgressConfig.ReportInterval.Ticks);

        _logger = logger;
        _reportInterval = chunkingProgressConfig.ReportInterval;
    }

    public void Start(long totalInputBytes)
    {
        lock (_syncRoot)
        {
            if (_started)
            {
                throw new InvalidOperationException("Chunking progress reporting has already been started.");
            }

            _started = true;
            _totalInputBytes = Math.Max(0, totalInputBytes);
            _cancellationTokenSource = new CancellationTokenSource();
            _stopwatch.Start();
            _reportingTask = RunReportingLoopAsync(_cancellationTokenSource.Token);
        }

        if (totalInputBytes > 0)
        {
            _logger.LogInformation(
                "Chunking progress reporting started. Total input size: {TotalInputSize}. Report interval: {ReportInterval}.",
                FormatBytes(totalInputBytes),
                _reportInterval);
            return;
        }

        _logger.LogInformation(
            "Chunking progress reporting started. Total input size is unknown. Report interval: {ReportInterval}.",
            _reportInterval);
    }

    public void ReportBytesRead(long bytesRead)
    {
        if (bytesRead <= 0)
        {
            return;
        }

        Interlocked.Add(ref _bytesRead, bytesRead);
    }

    public void ReportChunkCreated()
    {
        Interlocked.Increment(ref _chunksCreated);
    }

    public void ReportSorterStarted()
    {
        Interlocked.Increment(ref _activeSorters);
    }

    public void ReportChunkCompleted()
    {
        Interlocked.Increment(ref _chunksCompleted);
    }

    public void ReportSorterFinished()
    {
        Interlocked.Decrement(ref _activeSorters);
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
        LogSnapshot("Chunking phase completed");

        cancellationTokenSource?.Dispose();
    }

    private async Task RunReportingLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(_reportInterval);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                LogSnapshot("Chunking progress");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Chunking progress reporting loop was canceled.");
        }
    }

    private void LogSnapshot(string messagePrefix)
    {
        var totalInputBytes = Interlocked.Read(ref _totalInputBytes);
        var bytesRead = Interlocked.Read(ref _bytesRead);
        var chunksCreated = Interlocked.CompareExchange(ref _chunksCreated, 0, 0);
        var chunksCompleted = Interlocked.CompareExchange(ref _chunksCompleted, 0, 0);
        var activeSorters = Interlocked.CompareExchange(ref _activeSorters, 0, 0);
        var elapsed = _stopwatch.Elapsed;
        var throughputBytesPerSecond = elapsed.TotalSeconds > 0 ? bytesRead / elapsed.TotalSeconds : 0;

        if (totalInputBytes > 0)
        {
            var progressPercent = Math.Min(100d, bytesRead * 100d / totalInputBytes);
            _logger.LogInformation(
                "{MessagePrefix}: {ProgressPercent:F1}% ({BytesRead} / {TotalInputBytes}), chunks created: {ChunksCreated}, completed: {ChunksCompleted}, active sorters: {ActiveSorters}, speed: {ThroughputPerSecond}/s, elapsed: {Elapsed}",
                messagePrefix,
                progressPercent,
                FormatBytes(bytesRead),
                FormatBytes(totalInputBytes),
                chunksCreated,
                chunksCompleted,
                activeSorters,
                FormatBytes((long)throughputBytesPerSecond),
                FormatElapsed(elapsed));
            return;
        }

        _logger.LogInformation(
            "{MessagePrefix}: {BytesRead} read, chunks created: {ChunksCreated}, completed: {ChunksCompleted}, active sorters: {ActiveSorters}, speed: {ThroughputPerSecond}/s, elapsed: {Elapsed}",
            messagePrefix,
            FormatBytes(bytesRead),
            chunksCreated,
            chunksCompleted,
            activeSorters,
            FormatBytes((long)throughputBytesPerSecond),
            FormatElapsed(elapsed));
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:F1} {units[unitIndex]}";
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        return elapsed.ToString(@"hh\:mm\:ss");
    }
}
