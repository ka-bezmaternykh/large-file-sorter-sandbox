using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services.Tests;

public class MergeProgressReporterTests
{
    [Fact]
    public async Task CompleteAsync_ShouldLogProgressAndFinalSummary()
    {
        var logger = new CapturingLogger<MergeProgressReporter>();
        var reporter = new MergeProgressReporter(
            logger,
            new MergeProgressConfig
            {
                ReportInterval = TimeSpan.FromMilliseconds(10),
                BatchReportInterval = TimeSpan.FromMilliseconds(10)
            });

        reporter.Start(initialFileCount: 81, maxChunkFilesPerMerge: 64);
        reporter.ReportPassStarted(passNumber: 1, inputFileCount: 81, plannedBatchCount: 2);
        reporter.ReportBatchScheduled();

        await Task.Delay(30);

        reporter.ReportBatchCompleted();
        reporter.ReportPassCompleted(passNumber: 1, outputFileCount: 2);
        reporter.ReportFinalPromoted();
        await reporter.CompleteAsync();

        Assert.Contains(logger.Messages, message => message.Contains("Merge progress reporting started.", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("Merge pass 1 started.", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("Merge progress:", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("Merge phase completed:", StringComparison.Ordinal));
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Information)
            {
                Messages.Add(formatter(state, exception));
            }
        }
    }
}
