using System.Buffers;
using System.IO.Pipelines;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Models;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class ChunkSorter : IChunkSorter
{
    private readonly IChunkFileWriter _chunkFileWriter;
    private readonly IRowParser _rowParser;
    private readonly IComparer<Item> _itemComparer;
    private readonly IItemFormatter _itemFormatter;
    private readonly Pipe _pipe;
    private readonly ILogger<ChunkSorter> _logger;
    private bool _started;
    private bool _disposed;

    public ChunkSorter(
        IChunkFileWriter chunkFileWriter,
        IRowParser rowParser,
        IComparer<Item> itemComparer,
        IItemFormatter itemFormatter,
        ChunkConfig chunkConfig,
        ILogger<ChunkSorter> logger)
    {
        ArgumentNullException.ThrowIfNull(chunkFileWriter);
        ArgumentNullException.ThrowIfNull(rowParser);
        ArgumentNullException.ThrowIfNull(itemComparer);
        ArgumentNullException.ThrowIfNull(itemFormatter);
        ArgumentNullException.ThrowIfNull(chunkConfig);
        ArgumentNullException.ThrowIfNull(logger);

        _chunkFileWriter = chunkFileWriter;
        _rowParser = rowParser;
        _itemComparer = itemComparer;
        _itemFormatter = itemFormatter;
        _logger = logger;
        ChunkSize = chunkConfig.ChunkSize;
        _pipe = new Pipe(new PipeOptions(
            pauseWriterThreshold: ChunkSize,
            resumeWriterThreshold: Math.Max(ChunkSize / 2L, 1L),
            minimumSegmentSize: 64 * 1024,
            useSynchronizationContext: false));
        Writer = _pipe.Writer;
    }

    public int ChunkSize { get; }

    public PipeWriter Writer { get; }

    public async Task StartSortingAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started)
        {
            throw new InvalidOperationException("Chunk sorting has already been started.");
        }

        _started = true;

        try
        {
            await SortAsync(cancellationToken);
        }
        finally
        {
            await DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _pipe.Reader.CompleteAsync();
        await _chunkFileWriter.DisposeAsync();

        _disposed = true;
    }

    private async Task SortAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Chunk sorter started processing chunk.");
        var chunkBuffer = new ArrayBufferWriter<byte>();

        await ReadChunkToBuffer(cancellationToken, chunkBuffer);

        var items = ParseItems(chunkBuffer.WrittenSpan);
        items.Sort(_itemComparer);

        await WriteSortedListAsync(items, cancellationToken);
        await _chunkFileWriter.FlushAsync(cancellationToken);
        _logger.LogDebug("Chunk sorter completed writing sorted chunk output.");
    }

    private async Task ReadChunkToBuffer(CancellationToken cancellationToken, ArrayBufferWriter<byte> chunkBuffer)
    {
        while (true)
        {
            var readResult = await _pipe.Reader.ReadAsync(cancellationToken);
            var buffer = readResult.Buffer;

            foreach (var segment in buffer)
            {
                chunkBuffer.Write(segment.Span);
            }

            _pipe.Reader.AdvanceTo(buffer.End);

            if (readResult.IsCompleted)
            {
                break;
            }
        }
    }

    private async Task WriteSortedListAsync(List<Item> items, CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(item.TextBytes.Length + 32);
            try
            {
                if (!_itemFormatter.TryFormat(item, buffer, out var written))
                {
                    _logger.LogWarning("Formatter could not format an item.");
                    continue;
                }

                await _chunkFileWriter.WriteAsync(buffer.AsMemory(0, written), cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    private List<Item> ParseItems(ReadOnlySpan<byte> chunk)
    {
        if (chunk.IsEmpty)
        {
            return [];
        }

        var items = new List<Item>();
        var lineStart = 0;

        for (var index = 0; index < chunk.Length; index++)
        {
            if (chunk[index] != (byte)'\n')
            {
                continue;
            }

            ParseRow(chunk[lineStart..index], items);
            lineStart = index + 1;
        }

        if (lineStart < chunk.Length)
        {
            ParseRow(chunk[lineStart..], items);
        }

        return items;
    }

    private void ParseRow(ReadOnlySpan<byte> row, List<Item> items)
    {
        if (!row.IsEmpty && row[^1] == (byte)'\r')
        {
            row = row[..^1];
        }

        if (!_rowParser.TryParse(row, out var item))
        {
            throw new InvalidDataException("Chunk contains an invalid row.");
        }

        items.Add(item);
    }
}
