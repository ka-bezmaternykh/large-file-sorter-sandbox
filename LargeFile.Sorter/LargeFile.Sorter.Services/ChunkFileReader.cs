using System.Buffers;
using System.IO.Pipelines;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Models;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class ChunkFileReader : IChunkFileReader
{
    private readonly ITempFileAdapter _chunkFileAdapter;
    private readonly IRowParser _rowParser;
    private readonly ILogger<ChunkFileReader> _logger;
    private FileStream? _stream;
    private PipeReader? _pipeReader;
    private bool _disposed;

    public ChunkFileReader(ITempFileAdapter chunkFileAdapter, IRowParser rowParser, ILogger<ChunkFileReader> logger)
    {
        ArgumentNullException.ThrowIfNull(chunkFileAdapter);
        ArgumentNullException.ThrowIfNull(rowParser);
        ArgumentNullException.ThrowIfNull(logger);

        _chunkFileAdapter = chunkFileAdapter;
        _rowParser = rowParser;
        _logger = logger;
    }

    public async ValueTask<Item?> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        while (true)
        {
            var pipeReader = EnsurePipeReader();
            var readResult = await pipeReader.ReadAsync(cancellationToken);
            var buffer = readResult.Buffer;

            if (TrySliceNextRow(buffer, readResult.IsCompleted, out var rowSlice, out var consumed))
            {
                var rowBuffer = rowSlice.ToArray();
                pipeReader.AdvanceTo(consumed, consumed);
                var rowBytes = rowBuffer.AsSpan();
                if (!rowBytes.IsEmpty && rowBytes[^1] == (byte)'\r')
                {
                    rowBytes = rowBytes[..^1];
                }

                if (!_rowParser.TryParse(rowBytes, out var item))
                {
                    throw new InvalidDataException("Chunk file contains an invalid row.");
                }

                return item;
            }

            if (buffer.IsEmpty && readResult.IsCompleted)
            {
                pipeReader.AdvanceTo(buffer.End, buffer.End);
                _logger.LogDebug("Chunk file reader reached the end of file {FilePath}.", _chunkFileAdapter.FilePath);
                return null;
            }

            pipeReader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_pipeReader is not null)
        {
            await _pipeReader.CompleteAsync();
        }

        await _chunkFileAdapter.CompleteReadAsync();

        _disposed = true;
    }

    private PipeReader EnsurePipeReader()
    {
        if (_pipeReader is not null)
        {
            return _pipeReader;
        }

        _stream = _chunkFileAdapter.OpenReadStream();
        _pipeReader = PipeReader.Create(
            _stream,
            new StreamPipeReaderOptions(
                bufferSize: 32 * 1024,
                leaveOpen: false));

        return _pipeReader;
    }

    private static bool TrySliceNextRow(
        ReadOnlySequence<byte> buffer,
        bool isCompleted,
        out ReadOnlySequence<byte> rowSlice,
        out SequencePosition consumed)
    {
        rowSlice = default;
        consumed = buffer.Start;

        var newLinePosition = buffer.PositionOf((byte)'\n');
        if (newLinePosition.HasValue)
        {
            consumed = buffer.GetPosition(1, newLinePosition.Value);
            rowSlice = buffer.Slice(0, newLinePosition.Value);
            return true;
        }

        if (!buffer.IsEmpty && isCompleted)
        {
            rowSlice = buffer;
            consumed = buffer.End;
            return true;
        }

        return false;
    }
}
