using System.Buffers;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Models;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class MergeBatchProcessor : IMergeBatchProcessor
{
    private readonly IReadOnlyList<ITempFileAdapter> _batchFiles;
    private readonly ITempFileAdapter? _mergeFileAdapter;
    private readonly ITempFileWriter? _mergeFileWriter;
    private readonly ITempFileReader[] _readers;
    private readonly IMergeBatchProgressReporter _mergeBatchProgressReporter;
    private readonly IComparer<Item> _itemComparer;
    private readonly IItemFormatter _itemFormatter;
    private readonly ILogger<MergeBatchProcessor> _logger;
    private bool _started;
    private bool _disposed;

    public MergeBatchProcessor(
        IReadOnlyList<ITempFileAdapter> batchFiles,
        ITempFileAdapter? mergeFileAdapter,
        ITempFileWriter? mergeFileWriter,
        ITempFileReader[] readers,
        IMergeBatchProgressReporter mergeBatchProgressReporter,
        IComparer<Item> itemComparer,
        IItemFormatter itemFormatter,
        ILogger<MergeBatchProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(batchFiles);
        ArgumentNullException.ThrowIfNull(readers);
        ArgumentNullException.ThrowIfNull(mergeBatchProgressReporter);
        ArgumentNullException.ThrowIfNull(itemComparer);
        ArgumentNullException.ThrowIfNull(itemFormatter);
        ArgumentNullException.ThrowIfNull(logger);

        _batchFiles = batchFiles;
        _mergeFileAdapter = mergeFileAdapter;
        _mergeFileWriter = mergeFileWriter;
        _readers = readers;
        _mergeBatchProgressReporter = mergeBatchProgressReporter;
        _itemComparer = itemComparer;
        _itemFormatter = itemFormatter;
        _logger = logger;
    }

    public async Task<ITempFileAdapter> StartMergingAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started)
        {
            throw new InvalidOperationException("Merge batch processing has already been started.");
        }

        _started = true;
        var completedSuccessfully = false;

        try
        {
            var mergeFileAdapter = await MergeAsync(cancellationToken);
            completedSuccessfully = true;
            return mergeFileAdapter;
        }
        finally
        {
            await DisposeAsync();

            if (!completedSuccessfully && _mergeFileAdapter is not null)
            {
                await _mergeFileAdapter.DisposeAsync();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_mergeFileWriter is not null)
        {
            await _mergeFileWriter.DisposeAsync();
        }

        foreach (var reader in _readers)
        {
            await reader.DisposeAsync();
        }

        _disposed = true;
    }

    private async Task<ITempFileAdapter> MergeAsync(CancellationToken cancellationToken)
    {
        if (_batchFiles.Count == 0)
        {
            throw new InvalidOperationException("Merge batch cannot be empty.");
        }

        if (_batchFiles.Count == 1)
        {
            return _batchFiles[0];
        }

        if (_mergeFileAdapter is null || _mergeFileWriter is null)
        {
            throw new InvalidOperationException("Merge output resources were not created for a multi-file batch.");
        }

        _logger.LogInformation("Merging batch of {BatchFileCount} temp files.", _batchFiles.Count);
        var completedSuccessfully = false;

        try
        {
            var priorityQueue = new PriorityQueue<MergeEntry, Item>(_itemComparer);

            for (var readerIndex = 0; readerIndex < _readers.Length; readerIndex++)
            {
                var reader = _readers[readerIndex];
                var item = await reader.ReadNextAsync(cancellationToken);
                if (item.HasValue)
                {
                    priorityQueue.Enqueue(new MergeEntry(readerIndex, item.Value), item.Value);
                }
            }

            var formatBuffer = ArrayPool<byte>.Shared.Rent(1056);
            try
            {
                while (priorityQueue.TryDequeue(out var mergeEntry, out _))
                {
                    EnsureFormatBufferCapacity(ref formatBuffer, mergeEntry.Item.TextBytes.Length + 32);
                    await WriteItemAsync(mergeEntry.Item, _mergeFileWriter, formatBuffer, _mergeBatchProgressReporter, cancellationToken);

                    var nextItem = await _readers[mergeEntry.SourceChunkNumber].ReadNextAsync(cancellationToken);
                    if (nextItem.HasValue)
                    {
                        priorityQueue.Enqueue(mergeEntry with { Item = nextItem.Value }, nextItem.Value);
                    }
                }

                await _mergeFileWriter.FlushAsync(cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(formatBuffer);
            }

            completedSuccessfully = true;
        }
        finally
        {
            await _mergeBatchProgressReporter.CompleteAsync(completedSuccessfully);
        }

        await DisposeTempFileAdaptersAsync(_batchFiles);
        return _mergeFileAdapter;
    }

    private async Task WriteItemAsync(
        Item item,
        ITempFileWriter mergeFileWriter,
        byte[] formatBuffer,
        IMergeBatchProgressReporter mergeBatchProgressReporter,
        CancellationToken cancellationToken)
    {
        if (!_itemFormatter.TryFormat(item, formatBuffer, out var written))
        {
            _logger.LogWarning("Formatter could not format an item during merge.");
            return;
        }

        await mergeFileWriter.WriteAsync(formatBuffer.AsMemory(0, written), cancellationToken);
        mergeBatchProgressReporter.ReportBytesWritten(written);
    }

    private static void EnsureFormatBufferCapacity(ref byte[] buffer, int requiredLength)
    {
        if (buffer.Length >= requiredLength)
        {
            return;
        }

        var resizedBuffer = ArrayPool<byte>.Shared.Rent(requiredLength);
        ArrayPool<byte>.Shared.Return(buffer);
        buffer = resizedBuffer;
    }

    private static async Task DisposeTempFileAdaptersAsync(IEnumerable<ITempFileAdapter> tempFileAdapters)
    {
        foreach (var tempFileAdapter in tempFileAdapters)
        {
            await tempFileAdapter.DisposeAsync();
        }
    }
}
