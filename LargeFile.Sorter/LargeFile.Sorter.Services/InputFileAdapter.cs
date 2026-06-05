using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class InputFileAdapter : IInputFileAdapter
{
    private readonly ILogger<InputFileAdapter> _logger;
    private FileStream? _readStream;
    private bool _disposed;

    public InputFileAdapter(InputFileConfig config, ILogger<InputFileAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.FilePath);
        ArgumentNullException.ThrowIfNull(logger);

        FilePath = config.FilePath;
        _logger = logger;
    }

    public string FilePath { get; }

    public bool TryOpenReadStream(out FileStream? stream)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!File.Exists(FilePath))
        {
            _logger.LogWarning("Input file does not exist: {FilePath}", FilePath);
            stream = null;
            return false;
        }

        if (_readStream is not null)
        {
            throw new InvalidOperationException("Input file read stream has already been opened.");
        }

        _readStream = new FileStream(
            FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        stream = _readStream;
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_readStream is not null)
        {
            await _readStream.DisposeAsync();
            _readStream = null;
        }

        _disposed = true;
    }
}
