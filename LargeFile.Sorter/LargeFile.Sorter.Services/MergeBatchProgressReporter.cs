using System.Diagnostics;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class MergeBatchProgressReporter : IMergeBatchProgressReporter
{
    private readonly ILogger<MergeBatchProgressReporter> _logger;
    private readonly TimeSpan _reportInterval;
    private readonly Stopwatch _stopwatch = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _reportingTask;
    private readonly int _passNumber;
    private readonly int _batchNumber;
    private readonly int _inputFileCount;
    private readonly long _totalInputBytes;
    private long _bytesWritten;
    private bool _completed;

    public MergeBatchProgressReporter(
        ILogger<MergeBatchProgressReporter> logger,
        MergeProgressConfig mergeProgressConfig,
        int passNumber,
        int batchNumber,
        int inputFileCount,
        long totalInputBytes)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(mergeProgressConfig);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mergeProgressConfig.BatchReportInterval.Ticks);

        _logger = logger;
        _reportInterval = mergeProgressConfig.BatchReportInterval;
        _passNumber = passNumber;
        _batchNumber = batchNumber;
        _inputFileCount = inputFileCount;
        _totalInputBytes = Math.Max(0, totalInputBytes);
        _stopwatch.Start();
        _reportingTask = RunReportingLoopAsync(_cancellationTokenSource.Token);

        if (_totalInputBytes > 0)
        {
            _logger.LogInformation(
                "Merge batch progress reporting started. Pass: {PassNumber}, Batch: {BatchNumber}, Input files: {InputFileCount}, Total input size: {TotalInputSize}, Report interval: {ReportInterval}.",
                _passNumber,
                _batchNumber,
                _inputFileCount,
                FormatBytes(_totalInputBytes),
                _reportInterval);
            return;
        }

        _logger.LogInformation(
            "Merge batch progress reporting started. Pass: {PassNumber}, Batch: {BatchNumber}, Input files: {InputFileCount}, Total input size is unknown, Report interval: {ReportInterval}.",
            _passNumber,
            _batchNumber,
            _inputFileCount,
            _reportInterval);
    }

    public void ReportBytesWritten(long bytesWritten)
    {
        if (bytesWritten <= 0)
        {
            return;
        }

        Interlocked.Add(ref _bytesWritten, bytesWritten);
    }

    public async ValueTask CompleteAsync(bool completedSuccessfully)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _cancellationTokenSource.Cancel();

        await _reportingTask;

        _stopwatch.Stop();
        LogSnapshot(completedSuccessfully ? "Merge batch completed" : "Merge batch stopped");
        _cancellationTokenSource.Dispose();
    }

    private async Task RunReportingLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(_reportInterval);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                LogSnapshot("Merge batch progress");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Merge batch progress reporting loop was canceled. Pass: {PassNumber}, Batch: {BatchNumber}.",
                _passNumber,
                _batchNumber);
        }
    }

    private void LogSnapshot(string messagePrefix)
    {
        var bytesWritten = Interlocked.Read(ref _bytesWritten);
        var elapsed = _stopwatch.Elapsed;
        var throughputBytesPerSecond = elapsed.TotalSeconds > 0 ? bytesWritten / elapsed.TotalSeconds : 0;

        if (_totalInputBytes > 0)
        {
            var progressPercent = Math.Min(100d, bytesWritten * 100d / _totalInputBytes);
            _logger.LogInformation(
                "{MessagePrefix}: pass {PassNumber}, batch {BatchNumber}, input files: {InputFileCount}, {ProgressPercent:F1}% ({BytesWritten} / {TotalInputBytes}), write speed: {ThroughputPerSecond}/s, elapsed: {Elapsed}",
                messagePrefix,
                _passNumber,
                _batchNumber,
                _inputFileCount,
                progressPercent,
                FormatBytes(bytesWritten),
                FormatBytes(_totalInputBytes),
                FormatBytes((long)throughputBytesPerSecond),
                FormatElapsed(elapsed));
            return;
        }

        _logger.LogInformation(
            "{MessagePrefix}: pass {PassNumber}, batch {BatchNumber}, input files: {InputFileCount}, written: {BytesWritten}, write speed: {ThroughputPerSecond}/s, elapsed: {Elapsed}",
            messagePrefix,
            _passNumber,
            _batchNumber,
            _inputFileCount,
            FormatBytes(bytesWritten),
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
