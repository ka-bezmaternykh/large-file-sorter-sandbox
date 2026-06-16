using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services.Tests;

public class MergeBatchProgressReporterTests
{
    [Fact]
    public async Task CompleteAsync_ShouldLogProgressAndFinalSummary()
    {
        var logger = new CapturingLogger<MergeBatchProgressReporter>();
        var reporter = new MergeBatchProgressReporter(
            logger,
            new MergeProgressConfig
            {
                ReportInterval = TimeSpan.FromMilliseconds(10),
                BatchReportInterval = TimeSpan.FromMilliseconds(10)
            },
            passNumber: 1,
            batchNumber: 2,
            inputFileCount: 17,
            totalInputBytes: 1024);

        reporter.ReportBytesWritten(512);

        await Task.Delay(30);

        reporter.ReportBytesWritten(512);
        await reporter.CompleteAsync(completedSuccessfully: true);

        Assert.Contains(logger.Messages, message => message.Contains("Merge batch progress reporting started.", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("Merge batch progress:", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("Merge batch completed:", StringComparison.Ordinal));
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
