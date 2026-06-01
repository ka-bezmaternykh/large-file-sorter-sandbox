using LargeFile.Sorter.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class ChunkFileWriter : IChunkFileWriter
{
    private const int BufferSizeBytes = 64 * 1024;

    private readonly IChunkFileAdapter _chunkFileAdapter;
    private readonly byte[] _buffer = new byte[BufferSizeBytes];
    private readonly ILogger<ChunkFileWriter> _logger;
    private FileStream? _stream;
    private int _bufferedBytes;
    private bool _disposed;

    public ChunkFileWriter(IChunkFileAdapter chunkFileAdapter, ILogger<ChunkFileWriter> logger)
    {
        ArgumentNullException.ThrowIfNull(chunkFileAdapter);
        ArgumentNullException.ThrowIfNull(logger);

        _chunkFileAdapter = chunkFileAdapter;
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
        _logger.LogDebug("Flushed {BufferedBytes} buffered bytes to chunk file.", _bufferedBytes);
        _bufferedBytes = 0;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await FlushAsync(CancellationToken.None);
        if (_stream is not null)
        {
            await _stream.DisposeAsync();
        }
        _disposed = true;
    }

    private FileStream EnsureStream()
    {
        _stream ??= _chunkFileAdapter.OpenWriteStream();
        return _stream;
    }
}
