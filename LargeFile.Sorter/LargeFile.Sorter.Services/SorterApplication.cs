using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class SorterApplication : ISorterApplication
{
    private readonly ILogger _logger;
    private readonly SorterRunOptions _options;

    public SorterApplication(ILoggerFactory loggerFactory, SorterRunOptions options)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(options);

        _logger = loggerFactory.CreateLogger<SorterApplication>();
        _options = options;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "LargeFile.Sorter infrastructure initialized for source {FilePath}, output {OutputFilePath}, force {Force}.",
            _options.FilePath,
            _options.OutputFilePath,
            _options.Force);
        return Task.CompletedTask;
    }
}
