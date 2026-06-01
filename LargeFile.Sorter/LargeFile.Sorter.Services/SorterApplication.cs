using LargeFile.Sorter.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class SorterApplication : ISorterApplication
{
    private readonly IEnvironmentMonitor _environmentMonitor;
    private readonly IChunkSorterFactory _chunkSorterFactory;
    private readonly IInputFileReader _inputFileReader;
    private readonly ILogger<SorterApplication> _logger;

    public SorterApplication(
        IEnvironmentMonitor environmentMonitor,
        IChunkSorterFactory chunkSorterFactory,
        IInputFileReader inputFileReader,
        ILogger<SorterApplication> logger)
    {
        ArgumentNullException.ThrowIfNull(environmentMonitor);
        ArgumentNullException.ThrowIfNull(chunkSorterFactory);
        ArgumentNullException.ThrowIfNull(inputFileReader);
        ArgumentNullException.ThrowIfNull(logger);

        _environmentMonitor = environmentMonitor;
        _chunkSorterFactory = chunkSorterFactory;
        _inputFileReader = inputFileReader;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var inputFileReader = _inputFileReader;

        _environmentMonitor.WriteEnvironment();
        _logger.LogInformation("Starting chunk pipeline from input file.");

        while (await inputFileReader.HasNextAsync(cancellationToken))
        {
            var chunkSorter = await _chunkSorterFactory.CreateAsync(cancellationToken);
            await inputFileReader.ReadNextAsync(chunkSorter.Writer, chunkSorter.ChunkSize, cancellationToken);
        }

        await _chunkSorterFactory.WaitAllAsync(cancellationToken);
        _logger.LogInformation("Chunk pipeline completed.");
    }
}
