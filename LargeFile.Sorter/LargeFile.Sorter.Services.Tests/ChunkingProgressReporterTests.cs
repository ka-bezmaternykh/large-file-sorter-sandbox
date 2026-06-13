using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services.Tests;

public class ChunkingProgressReporterTests
{
    [Fact]
    public async Task CompleteAsync_ShouldLogProgressAndFinalSummary()
    {
        var logger = new CapturingLogger<ChunkingProgressReporter>();
        var reporter = new ChunkingProgressReporter(
            logger,
            new Options.ChunkingProgressConfig
            {
                ReportInterval = TimeSpan.FromMilliseconds(10)
            });

        reporter.Start(100);
        reporter.ReportBytesRead(50);
        reporter.ReportChunkCreated();
        reporter.ReportSorterStarted();

        await Task.Delay(30);

        reporter.ReportChunkCompleted();
        reporter.ReportSorterFinished();
        await reporter.CompleteAsync();

        Assert.Contains(logger.Messages, message => message.Contains("Chunking progress reporting started.", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("Chunking progress:", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("Chunking phase completed:", StringComparison.Ordinal));
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
