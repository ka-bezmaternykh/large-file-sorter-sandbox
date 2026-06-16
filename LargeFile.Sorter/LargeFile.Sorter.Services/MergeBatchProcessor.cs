using System.Buffers;
using System.Globalization;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Models;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class MergeBatchProcessor : IMergeBatchProcessor
{
    private readonly MergeConfig _mergeConfig;
    private readonly IServiceProvider _serviceProvider;
    private readonly IComparer<Item> _itemComparer;
    private readonly IItemFormatter _itemFormatter;
    private readonly IMergeBatchProgressReporterFactory _mergeBatchProgressReporterFactory;
    private readonly ILogger<MergeBatchProcessor> _logger;
    private int _nextMergeNumber;

    public MergeBatchProcessor(
        MergeConfig mergeConfig,
        IServiceProvider serviceProvider,
        IComparer<Item> itemComparer,
        IItemFormatter itemFormatter,
        IMergeBatchProgressReporterFactory mergeBatchProgressReporterFactory,
        ILogger<MergeBatchProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(mergeConfig);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(itemComparer);
        ArgumentNullException.ThrowIfNull(itemFormatter);
        ArgumentNullException.ThrowIfNull(mergeBatchProgressReporterFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _mergeConfig = mergeConfig;
        _serviceProvider = serviceProvider;
        _itemComparer = itemComparer;
        _itemFormatter = itemFormatter;
        _mergeBatchProgressReporterFactory = mergeBatchProgressReporterFactory;
        _logger = logger;
    }

    public async Task<ITempFileAdapter> MergeBatchAsync(
        IReadOnlyList<ITempFileAdapter> batchFiles,
        int passNumber,
        int batchNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batchFiles);

        if (batchFiles.Count == 0)
        {
            throw new InvalidOperationException("Merge batch cannot be empty.");
        }

        if (batchFiles.Count == 1)
        {
            return batchFiles[0];
        }

        _logger.LogInformation("Merging batch of {BatchFileCount} temp files.", batchFiles.Count);
        var totalInputBytes = batchFiles.Sum(batchFile => batchFile.GetFileSizeBytes());
        var mergeFilePath = CreateMergeFilePath();
        var mergeFileAdapter = ActivatorUtilities.CreateInstance<MergeFileAdapter>(
            _serviceProvider,
            new MergeFileConfig
            {
                FilePath = mergeFilePath
            });
        var mergeBatchProgressReporter = _mergeBatchProgressReporterFactory.Create(passNumber, batchNumber, batchFiles.Count, totalInputBytes);

        var readers = new Dictionary<int, ITempFileReader>(batchFiles.Count);
        var completedSuccessfully = false;

        try
        {
            await using var mergeFileWriter = ActivatorUtilities.CreateInstance<MergeFileWriter>(_serviceProvider, mergeFileAdapter);
            for (var index = 0; index < batchFiles.Count; index++)
            {
                readers[index] = ActivatorUtilities.CreateInstance<ChunkFileReader>(_serviceProvider, batchFiles[index]);
            }

            var priorityQueue = new PriorityQueue<MergeEntry, Item>(_itemComparer);

            foreach (var (readerIndex, reader) in readers)
            {
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
                    await WriteItemAsync(mergeEntry.Item, mergeFileWriter, formatBuffer, mergeBatchProgressReporter, cancellationToken);

                    var nextItem = await readers[mergeEntry.SourceChunkNumber].ReadNextAsync(cancellationToken);
                    if (nextItem.HasValue)
                    {
                        priorityQueue.Enqueue(mergeEntry with { Item = nextItem.Value }, nextItem.Value);
                    }
                }

                await mergeFileWriter.FlushAsync(cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(formatBuffer);
            }

            completedSuccessfully = true;
        }
        finally
        {
            foreach (var reader in readers.Values)
            {
                await reader.DisposeAsync();
            }

            await mergeBatchProgressReporter.CompleteAsync(completedSuccessfully);
        }

        await DisposeTempFileAdaptersAsync(batchFiles);
        return mergeFileAdapter;
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

    private string CreateMergeFilePath()
    {
        var mergeNumber = Interlocked.Increment(ref _nextMergeNumber);
        var fileName = string.Format(CultureInfo.InvariantCulture, _mergeConfig.MergeFileTemplate, mergeNumber);
        return Path.Combine(_mergeConfig.TempFilesFolder, fileName);
    }

    private static async Task DisposeTempFileAdaptersAsync(IEnumerable<ITempFileAdapter> tempFileAdapters)
    {
        foreach (var tempFileAdapter in tempFileAdapters)
        {
            await tempFileAdapter.DisposeAsync();
        }
    }
}
