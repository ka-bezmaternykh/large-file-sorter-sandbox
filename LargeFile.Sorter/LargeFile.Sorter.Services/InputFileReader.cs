using System.Buffers;
using System.IO.Pipelines;
using LargeFile.Sorter.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class InputFileReader : IInputFileReader
{
    private const int MaxLineTailBytes = 1024;

    private readonly IInputFileAdapter _inputFileAdapter;
    private readonly IChunkingProgressReporter _chunkingProgressReporter;
    private readonly ILogger<InputFileReader> _logger;
    private PipeReader? _pipeReader;
    private bool _disposed;
    private bool _isFullyRead;

    public InputFileReader(
        IInputFileAdapter inputFileAdapter,
        IChunkingProgressReporter chunkingProgressReporter,
        ILogger<InputFileReader> logger)
    {
        ArgumentNullException.ThrowIfNull(inputFileAdapter);
        ArgumentNullException.ThrowIfNull(chunkingProgressReporter);
        ArgumentNullException.ThrowIfNull(logger);

        _inputFileAdapter = inputFileAdapter;
        _chunkingProgressReporter = chunkingProgressReporter;
        _logger = logger;
    }

    public async ValueTask<bool> HasNextAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isFullyRead)
        {
            return false;
        }

        while (true)
        {
            if (!TryEnsurePipeReader(out var pipeReader))
            {
                _isFullyRead = true;
                return false;
            }

            var readResult = await pipeReader.ReadAsync(cancellationToken);
            var buffer = readResult.Buffer;

            if (!buffer.IsEmpty)
            {
                pipeReader.AdvanceTo(buffer.Start, buffer.End);
                return true;
            }

            if (readResult.IsCompleted)
            {
                _isFullyRead = true;
                pipeReader.AdvanceTo(buffer.End, buffer.End);
                return false;
            }

            pipeReader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    public async ValueTask ReadNextAsync(PipeWriter writer, int chunkSize, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(writer);

        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), chunkSize, "Chunk size must be positive.");
        }

        if (_isFullyRead)
        {
            await writer.CompleteAsync();
            return;
        }

        var minimumChunkBytes = Math.Max(1, chunkSize - MaxLineTailBytes);
        long totalWritten = 0;

        try
        {
            while (true)
            {
                if (!TryEnsurePipeReader(out var pipeReader))
                {
                    _isFullyRead = true;
                    break;
                }

                var readResult = await pipeReader.ReadAsync(cancellationToken);
                var buffer = readResult.Buffer;

                if (buffer.IsEmpty && readResult.IsCompleted)
                {
                    _isFullyRead = true;
                    pipeReader.AdvanceTo(buffer.End, buffer.End);
                    break;
                }

                if (TrySliceChunk(
                        buffer,
                        totalWritten,
                        minimumChunkBytes,
                        readResult.IsCompleted,
                        out var slice,
                        out var consumed,
                        out var chunkCompleted))
                {
                    await WriteToPipeAsync(slice, writer, cancellationToken);
                    totalWritten += slice.Length;
                    pipeReader.AdvanceTo(consumed, consumed);

                    if (chunkCompleted)
                    {
                        _isFullyRead = readResult.IsCompleted && buffer.Slice(consumed).IsEmpty;
                        break;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            await writer.CompleteAsync(exception);
            throw;
        }

        _logger.LogTrace(
            "Read {BytesWritten} bytes into chunk writer. ChunkSize={ChunkSize}, IsCompleted={IsCompleted}",
            totalWritten,
            chunkSize,
            _isFullyRead);
        _chunkingProgressReporter.ReportBytesRead(totalWritten);

        await writer.CompleteAsync();
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

        await _inputFileAdapter.DisposeAsync();

        _disposed = true;
    }

    private bool TryEnsurePipeReader(out PipeReader pipeReader)
    {
        if (_pipeReader is not null)
        {
            pipeReader = _pipeReader;
            return true;
        }

        if (!_inputFileAdapter.TryOpenReadStream(out var stream) || stream is null)
        {
            pipeReader = null!;
            return false;
        }

        _pipeReader = PipeReader.Create(
            stream,
            new StreamPipeReaderOptions(
                bufferSize: 64 * 1024,
                leaveOpen: false));

        pipeReader = _pipeReader;
        return true;
    }

    private static async ValueTask WriteToPipeAsync(
        ReadOnlySequence<byte> buffer,
        PipeWriter writer,
        CancellationToken cancellationToken)
    {
        foreach (var segment in buffer)
        {
            segment.Span.CopyTo(writer.GetSpan(segment.Length));
            writer.Advance(segment.Length);
        }

        await writer.FlushAsync(cancellationToken);
    }

    private static bool TrySliceChunk(
        ReadOnlySequence<byte> buffer,
        long totalWritten,
        int minimumChunkBytes,
        bool isCompleted,
        out ReadOnlySequence<byte> slice,
        out SequencePosition consumed,
        out bool chunkCompleted)
    {
        slice = default;
        consumed = buffer.Start;
        chunkCompleted = false;

        if (buffer.IsEmpty)
        {
            return false;
        }

        if (totalWritten < minimumChunkBytes)
        {
            var bytesNeeded = minimumChunkBytes - totalWritten;

            if (buffer.Length < bytesNeeded && !isCompleted)
            {
                slice = buffer;
                consumed = buffer.End;
                return true;
            }

            var searchStart = buffer.Length >= bytesNeeded ? buffer.GetPosition(bytesNeeded) : buffer.End;
            if (TryFindNewLine(buffer.Slice(searchStart), out var newLinePosition))
            {
                consumed = buffer.GetPosition(1, newLinePosition);
                slice = buffer.Slice(0, consumed);
                chunkCompleted = true;
                return true;
            }

            if (isCompleted)
            {
                slice = buffer;
                consumed = buffer.End;
                chunkCompleted = true;
                return true;
            }

            slice = buffer;
            consumed = buffer.End;
            return true;
        }

        if (TryFindNewLine(buffer, out var linePosition))
        {
            consumed = buffer.GetPosition(1, linePosition);
            slice = buffer.Slice(0, consumed);
            chunkCompleted = true;
            return true;
        }

        if (isCompleted)
        {
            slice = buffer;
            consumed = buffer.End;
            chunkCompleted = true;
            return true;
        }

        slice = buffer;
        consumed = buffer.End;
        return true;
    }

    private static bool TryFindNewLine(ReadOnlySequence<byte> buffer, out SequencePosition position)
    {
        var newLinePosition = buffer.PositionOf((byte)'\n');
        if (newLinePosition.HasValue)
        {
            position = newLinePosition.Value;
            return true;
        }

        position = default;
        return false;
    }
}
