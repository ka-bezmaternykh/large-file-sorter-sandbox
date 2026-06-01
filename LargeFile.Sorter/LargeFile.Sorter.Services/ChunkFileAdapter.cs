using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class ChunkFileAdapter : IChunkFileAdapter
{
    private readonly ILogger<ChunkFileAdapter> _logger;

    public ChunkFileAdapter(ChunkFileConfig config, ILogger<ChunkFileAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.FilePath);
        ArgumentNullException.ThrowIfNull(logger);

        FilePath = config.FilePath;
        _logger = logger;
    }

    public string FilePath { get; }

    public FileStream OpenWriteStream()
    {
        var directoryPath = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            _logger.LogInformation("Ensuring directory exists for chunk file: {DirectoryPath}", directoryPath);
            Directory.CreateDirectory(directoryPath);
        }

        if (File.Exists(FilePath))
        {
            _logger.LogWarning("Chunk file already exists and will be replaced: {FilePath}", FilePath);
            File.Delete(FilePath);
        }

        _logger.LogInformation("Creating chunk file stream: {FilePath}", FilePath);

        return new FileStream(
            path: FilePath,
            mode: FileMode.CreateNew,
            access: FileAccess.Write,
            share: FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
    }
}
