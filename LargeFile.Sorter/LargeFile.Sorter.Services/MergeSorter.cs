using System.Buffers;
using System.Globalization;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Models;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class MergeSorter : IMergeSorter
{
    private readonly MergeConfig _mergeConfig;
    private readonly SorterRunOptions _sorterRunOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IComparer<Item> _itemComparer;
    private readonly IItemFormatter _itemFormatter;
    private readonly ILogger<MergeSorter> _logger;
    private int _nextMergeNumber;

    public MergeSorter(
        MergeConfig mergeConfig,
        SorterRunOptions sorterRunOptions,
        IServiceProvider serviceProvider,
        IComparer<Item> itemComparer,
        IItemFormatter itemFormatter,
        ILogger<MergeSorter> logger)
    {
        ArgumentNullException.ThrowIfNull(mergeConfig);
        ArgumentNullException.ThrowIfNull(sorterRunOptions);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(itemComparer);
        ArgumentNullException.ThrowIfNull(itemFormatter);
        ArgumentNullException.ThrowIfNull(logger);

        _mergeConfig = mergeConfig;
        _sorterRunOptions = sorterRunOptions;
        _serviceProvider = serviceProvider;
        _itemComparer = itemComparer;
        _itemFormatter = itemFormatter;
        _logger = logger;
    }

    public async Task MergeAsync(IReadOnlyDictionary<int, ITempFileAdapter> chunkFileAdapters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunkFileAdapters);

        if (chunkFileAdapters.Count == 0)
        {
            _logger.LogInformation("No chunk files were produced. Merge phase is skipped.");
            return;
        }

        var currentFiles = chunkFileAdapters
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value)
            .ToList();

        while (currentFiles.Count > 1)
        {
            currentFiles = await RunMergePassAsync(currentFiles, cancellationToken);
        }

        var finalFile = currentFiles[0];
        PromoteFileToOutput(finalFile.FilePath);
        await finalFile.DisposeAsync();
    }

    private async Task<List<ITempFileAdapter>> RunMergePassAsync(
        IReadOnlyList<ITempFileAdapter> inputFiles,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting merge pass for {InputFileCount} temp files with batch size {BatchSize}.",
            inputFiles.Count,
            _mergeConfig.MaxChunkFilesPerMerge);

        var passResults = new List<ITempFileAdapter>();

        for (var index = 0; index < inputFiles.Count; index += _mergeConfig.MaxChunkFilesPerMerge)
        {
            var batch = inputFiles
                .Skip(index)
                .Take(_mergeConfig.MaxChunkFilesPerMerge)
                .ToList();

            passResults.Add(await MergeBatchAsync(batch, cancellationToken));
        }

        _logger.LogInformation(
            "Completed merge pass. Reduced {InputFileCount} temp files to {OutputFileCount}.",
            inputFiles.Count,
            passResults.Count);

        return passResults;
    }

    private async Task<ITempFileAdapter> MergeBatchAsync(
        IReadOnlyList<ITempFileAdapter> batchFiles,
        CancellationToken cancellationToken)
    {
        if (batchFiles.Count == 0)
        {
            throw new InvalidOperationException("Merge batch cannot be empty.");
        }

        if (batchFiles.Count == 1)
        {
            return batchFiles[0];
        }

        _logger.LogInformation("Merging batch of {BatchFileCount} temp files.", batchFiles.Count);
        var mergeFilePath = CreateMergeFilePath();
        var mergeFileAdapter = ActivatorUtilities.CreateInstance<MergeFileAdapter>(
            _serviceProvider,
            new MergeFileConfig
            {
                FilePath = mergeFilePath
            });

        var readers = new Dictionary<int, ITempFileReader>(batchFiles.Count);

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
                    await WriteItemAsync(mergeEntry.Item, mergeFileWriter, formatBuffer, cancellationToken);

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
        }
        finally
        {
            foreach (var reader in readers.Values)
            {
                await reader.DisposeAsync();
            }
        }

        await DisposeTempFileAdaptersAsync(batchFiles);
        return mergeFileAdapter;
    }

    private async Task WriteItemAsync(
        Item item,
        ITempFileWriter mergeFileWriter,
        byte[] formatBuffer,
        CancellationToken cancellationToken)
    {
        if (!_itemFormatter.TryFormat(item, formatBuffer, out var written))
        {
            _logger.LogWarning("Formatter could not format an item during merge.");
            return;
        }

        await mergeFileWriter.WriteAsync(formatBuffer.AsMemory(0, written), cancellationToken);
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

    private void PromoteFileToOutput(string sourcePath)
    {
        var outputDirectoryPath = Path.GetDirectoryName(_sorterRunOptions.OutputFilePath);
        if (!string.IsNullOrWhiteSpace(outputDirectoryPath))
        {
            Directory.CreateDirectory(outputDirectoryPath);
        }

        if (File.Exists(_sorterRunOptions.OutputFilePath))
        {
            _logger.LogWarning("Output file already exists and will be replaced: {OutputFilePath}", _sorterRunOptions.OutputFilePath);
            File.Delete(_sorterRunOptions.OutputFilePath);
        }

        File.Move(sourcePath, _sorterRunOptions.OutputFilePath);
        _logger.LogInformation("Promoted merged file to output: {OutputFilePath}", _sorterRunOptions.OutputFilePath);
    }

    private static async Task DisposeTempFileAdaptersAsync(IEnumerable<ITempFileAdapter> tempFileAdapters)
    {
        foreach (var tempFileAdapter in tempFileAdapters)
        {
            await tempFileAdapter.DisposeAsync();
        }
    }
}
