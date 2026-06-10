using DummyFile.Generator.Services.Abstract;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DummyFile.Generator.Services;

public sealed class ProgressLogger(
    ILogger<ProgressLogger> logger,
    ProgressConfig progressConfig) : IProgressLogger
{
    private const int ProgressBarWidth = 10;

    private readonly long _startedAt = Stopwatch.GetTimestamp();
    private TimeSpan _nextProgressLogAt = progressConfig.ProgressLogInterval;

    public void LogProgress(long writtenBytes, int generatedLines, bool isFinal)
    {
        var elapsed = Stopwatch.GetElapsedTime(_startedAt);
        if (!isFinal && elapsed < _nextProgressLogAt)
        {
            return;
        }

        var message = BuildProgressMessage(elapsed, writtenBytes, generatedLines);
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (isFinal)
        {
            logger.LogInformation("Final progress: {Progress}", message);
            return;
        }

        _nextProgressLogAt += progressConfig.ProgressLogInterval;
        logger.LogInformation("Progress: {Progress}", message);
    }

    private string BuildProgressMessage(TimeSpan elapsed, long writtenBytes, int generatedLines)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            elapsed = TimeSpan.FromMilliseconds(1);
        }

        if (progressConfig.RequestedFileSizeBytes.HasValue)
        {
            var progress = ClampProgress((double)writtenBytes / progressConfig.RequestedFileSizeBytes.Value);
            var bar = BuildProgressBar(progress);
            var writtenGigabytes = writtenBytes / 1024d / 1024d / 1024d;
            var targetGigabytes = progressConfig.RequestedFileSizeBytes.Value / 1024d / 1024d / 1024d;
            var megabytesPerSecond = writtenBytes / 1024d / 1024d / elapsed.TotalSeconds;

            return FormattableString.Invariant(
                $"{bar} {progress * 100:0.0}% | {writtenGigabytes:0.00} / {targetGigabytes:0.00} GB | {megabytesPerSecond:0.##} MB/s");
        }

        if (progressConfig.RequestedLinesNumber.HasValue)
        {
            var progress = ClampProgress((double)generatedLines / progressConfig.RequestedLinesNumber.Value);
            var bar = BuildProgressBar(progress);
            var rowsPerSecond = generatedLines / elapsed.TotalSeconds;

            return FormattableString.Invariant(
                $"{bar} {progress * 100:0.0}% | {rowsPerSecond:0.##} rows/s");
        }

        return string.Empty;
    }

    private static string BuildProgressBar(double progress)
    {
        var filledCount = (int)Math.Round(progress * ProgressBarWidth, MidpointRounding.AwayFromZero);
        filledCount = Math.Clamp(filledCount, 0, ProgressBarWidth);

        return $"[{new string('#', filledCount)}{new string('-', ProgressBarWidth - filledCount)}]";
    }

    private static double ClampProgress(double progress)
    {
        return Math.Clamp(progress, 0d, 1d);
    }
}
