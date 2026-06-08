using LargeFile.Sorter.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class MergeFileWriter : ITempFileWriter
{
    private const int BufferSizeBytes = 64 * 1024;

    private readonly ITempFileAdapter _mergeFileAdapter;
    private readonly byte[] _buffer = new byte[BufferSizeBytes];
    private readonly ILogger<MergeFileWriter> _logger;
    private FileStream? _stream;
    private int _bufferedBytes;
    private bool _disposed;

    public MergeFileWriter(ITempFileAdapter mergeFileAdapter, ILogger<MergeFileWriter> logger)
    {
        ArgumentNullException.ThrowIfNull(mergeFileAdapter);
        ArgumentNullException.ThrowIfNull(logger);

        _mergeFileAdapter = mergeFileAdapter;
        _logger = logger;
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (buffer.IsEmpty)
        {
            return;
        }

        if (buffer.Length > _buffer.Length)
        {
            await FlushAsync(cancellationToken);
            await EnsureStream().WriteAsync(buffer, cancellationToken);
            return;
        }

        if (_bufferedBytes + buffer.Length > _buffer.Length)
        {
            await FlushAsync(cancellationToken);
        }

        buffer.CopyTo(_buffer.AsMemory(_bufferedBytes));
        _bufferedBytes += buffer.Length;

        if (_bufferedBytes == _buffer.Length)
        {
            await FlushAsync(cancellationToken);
        }
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_bufferedBytes == 0)
        {
            return;
        }

        await EnsureStream().WriteAsync(_buffer.AsMemory(0, _bufferedBytes), cancellationToken);
        _logger.LogTrace("Flushed {BufferedBytes} buffered bytes to merge file.", _bufferedBytes);
        _bufferedBytes = 0;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await FlushAsync(CancellationToken.None);
        await _mergeFileAdapter.CompleteWriteAsync();

        _disposed = true;
    }

    private FileStream EnsureStream()
    {
        _stream ??= _mergeFileAdapter.OpenWriteStream();
        return _stream;
    }
}
