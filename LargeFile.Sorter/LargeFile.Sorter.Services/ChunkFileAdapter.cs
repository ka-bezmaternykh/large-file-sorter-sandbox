using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class ChunkFileAdapter : ITempFileAdapter
{
    private readonly ILogger<ChunkFileAdapter> _logger;
    private FileStream? _writeStream;
    private FileStream? _readStream;
    private bool _writeCompleted;
    private bool _disposed;

    public ChunkFileAdapter(ChunkFileConfig config, ILogger<ChunkFileAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.FilePath);
        ArgumentNullException.ThrowIfNull(logger);

        FilePath = config.FilePath;
        _logger = logger;
    }

    public string FilePath { get; }

    public FileStream OpenReadStream()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_writeCompleted)
        {
            throw new InvalidOperationException("Chunk file cannot be opened for reading before writing is completed.");
        }

        if (_readStream is not null)
        {
            throw new InvalidOperationException("Chunk file read stream has already been opened.");
        }

        _logger.LogInformation("Opening chunk file stream for reading: {FilePath}", FilePath);

        _readStream = new FileStream(
            path: FilePath,
            mode: FileMode.Open,
            access: FileAccess.Read,
            share: FileShare.Read,
            bufferSize: 32 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        return _readStream;
    }

    public FileStream OpenWriteStream()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_writeCompleted)
        {
            throw new InvalidOperationException("Chunk file write stream has already been completed.");
        }

        if (_writeStream is not null)
        {
            throw new InvalidOperationException("Chunk file write stream has already been opened.");
        }

        var directoryPath = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            _logger.LogDebug("Ensuring directory exists for chunk file: {DirectoryPath}", directoryPath);
            Directory.CreateDirectory(directoryPath);
        }

        if (File.Exists(FilePath))
        {
            _logger.LogDebug("Chunk file already exists and will be replaced: {FilePath}", FilePath);
            File.Delete(FilePath);
        }

        _logger.LogDebug("Creating chunk file stream: {FilePath}", FilePath);

        _writeStream = new FileStream(
            path: FilePath,
            mode: FileMode.CreateNew,
            access: FileAccess.Write,
            share: FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        return _writeStream;
    }

    public async ValueTask CompleteWriteAsync()
    {
        if (_writeStream is null)
        {
            _writeCompleted = true;
            return;
        }

        await _writeStream.DisposeAsync();
        _writeStream = null;
        _writeCompleted = true;
    }

    public async ValueTask CompleteReadAsync()
    {
        if (_readStream is null)
        {
            return;
        }

        await _readStream.DisposeAsync();
        _readStream = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await CompleteReadAsync();
        await CompleteWriteAsync();

        if (File.Exists(FilePath))
        {
            _logger.LogDebug("Deleting chunk file during adapter disposal: {FilePath}", FilePath);
            File.Delete(FilePath);
        }

        _disposed = true;
    }
}
