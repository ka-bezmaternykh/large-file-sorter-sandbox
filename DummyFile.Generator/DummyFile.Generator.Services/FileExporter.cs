using DummyFile.Generator.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace DummyFile.Generator.Services;

public sealed class FileExporter : IFileExporter
{
    private const int BufferSizeBytes = 4 * 1024;

    private readonly Stream _stream;
    private readonly byte[] _buffer = new byte[BufferSizeBytes];
    private readonly long? _maxFileSizeBytes;
    private readonly ILogger<FileExporter> _logger;
    private long _acceptedBytes;
    private int _bufferedBytes;
    private bool _disposed;

    public FileExporter(IFileAdapter fileAdapter, ILogger<FileExporter> logger, FileExporterConfig config)
    {
        ArgumentNullException.ThrowIfNull(fileAdapter);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);

        _stream = fileAdapter.CreateWriteStream();
        _logger = logger;
        _maxFileSizeBytes = config.MaxFileSize;
        _acceptedBytes = 0;
        _bufferedBytes = 0;
    }

    public async ValueTask<bool> TryExportAsync(ReadOnlyMemory<byte> row, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_maxFileSizeBytes.HasValue && _acceptedBytes + row.Length > _maxFileSizeBytes.Value)
        {
            _logger.LogInformation(
                "Stopping export because the next row would exceed the file size limit. AcceptedBytes={AcceptedBytes}, RowLength={RowLength}, MaxFileSizeBytes={MaxFileSizeBytes}",
                _acceptedBytes,
                row.Length,
                _maxFileSizeBytes.Value);
            return false;
        }

        if (row.Length > _buffer.Length)
        {
            await FlushBufferAsync(cancellationToken);
            await _stream.WriteAsync(row, cancellationToken);
            _acceptedBytes += row.Length;
            return true;
        }

        if (_bufferedBytes + row.Length > _buffer.Length)
        {
            await FlushBufferAsync(cancellationToken);
        }

        row.CopyTo(_buffer.AsMemory(_bufferedBytes));
        _bufferedBytes += row.Length;
        _acceptedBytes += row.Length;

        if (_bufferedBytes == _buffer.Length)
        {
            await FlushBufferAsync(cancellationToken);
        }

        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        FlushBuffer();
        _stream.Dispose();
        _disposed = true;
        _logger.LogInformation("File exporter disposed synchronously. AcceptedBytes={AcceptedBytes}", _acceptedBytes);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await FlushBufferAsync(CancellationToken.None);
        await _stream.DisposeAsync();
        _disposed = true;
        _logger.LogInformation("File exporter disposed asynchronously. AcceptedBytes={AcceptedBytes}", _acceptedBytes);
    }

    private void FlushBuffer()
    {
        if (_bufferedBytes == 0)
        {
            return;
        }

        _stream.Write(_buffer, 0, _bufferedBytes);
        _logger.LogDebug("Flushed {BufferedBytes} buffered bytes synchronously.", _bufferedBytes);
        _bufferedBytes = 0;
    }

    private async ValueTask FlushBufferAsync(CancellationToken cancellationToken)
    {
        if (_bufferedBytes == 0)
        {
            return;
        }

        await _stream.WriteAsync(_buffer.AsMemory(0, _bufferedBytes), cancellationToken);
        _logger.LogDebug("Flushed {BufferedBytes} buffered bytes asynchronously.", _bufferedBytes);
        _bufferedBytes = 0;
    }
}
