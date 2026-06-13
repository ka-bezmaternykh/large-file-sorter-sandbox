using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class MergeFileAdapter : ITempFileAdapter
{
    private readonly ILogger<MergeFileAdapter> _logger;
    private FileStream? _writeStream;
    private FileStream? _readStream;
    private bool _disposed;

    public MergeFileAdapter(MergeFileConfig config, ILogger<MergeFileAdapter> logger)
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

        if (_readStream is not null)
        {
            throw new InvalidOperationException("Merge file read stream has already been opened.");
        }

        _logger.LogDebug("Opening merge file stream for reading: {FilePath}", FilePath);

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

        if (_writeStream is not null)
        {
            throw new InvalidOperationException("Merge file write stream has already been opened.");
        }

        var directoryPath = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            _logger.LogDebug("Ensuring directory exists for merge file: {DirectoryPath}", directoryPath);
            Directory.CreateDirectory(directoryPath);
        }

        if (File.Exists(FilePath))
        {
            _logger.LogDebug("Merge file already exists and will be replaced: {FilePath}", FilePath);
            File.Delete(FilePath);
        }

        _logger.LogDebug("Creating merge file stream: {FilePath}", FilePath);

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
            return;
        }

        await _writeStream.DisposeAsync();
        _writeStream = null;
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
            _logger.LogDebug("Deleting merge file during adapter disposal: {FilePath}", FilePath);
            File.Delete(FilePath);
        }

        _disposed = true;
    }
}
