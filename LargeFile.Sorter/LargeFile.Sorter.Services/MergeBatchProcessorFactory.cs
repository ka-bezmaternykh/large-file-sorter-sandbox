using System.Globalization;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Models;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class MergeBatchProcessorFactory : IMergeBatchProcessorFactory
{
    private readonly MergeConfig _mergeConfig;
    private readonly IServiceProvider _serviceProvider;
    private readonly IComparer<Item> _itemComparer;
    private readonly IItemFormatter _itemFormatter;
    private readonly IMergeBatchProgressReporterFactory _mergeBatchProgressReporterFactory;
    private readonly ILogger<MergeBatchProcessor> _mergeBatchProcessorLogger;
    private readonly ILogger<MergeBatchProcessorFactory> _logger;
    private int _nextMergeNumber;

    public MergeBatchProcessorFactory(
        MergeConfig mergeConfig,
        IServiceProvider serviceProvider,
        IComparer<Item> itemComparer,
        IItemFormatter itemFormatter,
        IMergeBatchProgressReporterFactory mergeBatchProgressReporterFactory,
        ILogger<MergeBatchProcessor> mergeBatchProcessorLogger,
        ILogger<MergeBatchProcessorFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(mergeConfig);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(itemComparer);
        ArgumentNullException.ThrowIfNull(itemFormatter);
        ArgumentNullException.ThrowIfNull(mergeBatchProgressReporterFactory);
        ArgumentNullException.ThrowIfNull(mergeBatchProcessorLogger);
        ArgumentNullException.ThrowIfNull(logger);

        _mergeConfig = mergeConfig;
        _serviceProvider = serviceProvider;
        _itemComparer = itemComparer;
        _itemFormatter = itemFormatter;
        _mergeBatchProgressReporterFactory = mergeBatchProgressReporterFactory;
        _mergeBatchProcessorLogger = mergeBatchProcessorLogger;
        _logger = logger;
    }

    public IMergeBatchProcessor Create(IReadOnlyList<ITempFileAdapter> batchFiles, int passNumber, int batchNumber)
    {
        ArgumentNullException.ThrowIfNull(batchFiles);

        ITempFileAdapter? mergeFileAdapter = null;
        ITempFileWriter? mergeFileWriter = null;
        var readers = new ITempFileReader[batchFiles.Count];
        var totalInputBytes = batchFiles.Sum(batchFile => batchFile.GetFileSizeBytes());
        var mergeBatchProgressReporter = _mergeBatchProgressReporterFactory.Create(
            passNumber,
            batchNumber,
            batchFiles.Count,
            totalInputBytes);

        for (var index = 0; index < batchFiles.Count; index++)
        {
            readers[index] = ActivatorUtilities.CreateInstance<ChunkFileReader>(_serviceProvider, batchFiles[index]);
        }

        if (batchFiles.Count > 1)
        {
            var mergeFilePath = CreateMergeFilePath();
            mergeFileAdapter = ActivatorUtilities.CreateInstance<MergeFileAdapter>(
                _serviceProvider,
                new MergeFileConfig
                {
                    FilePath = mergeFilePath
                });
            mergeFileWriter = ActivatorUtilities.CreateInstance<MergeFileWriter>(_serviceProvider, mergeFileAdapter);

            _logger.LogDebug(
                "Created merge batch processor for pass {PassNumber}, batch {BatchNumber}, output file {FilePath}.",
                passNumber,
                batchNumber,
                mergeFilePath);
        }

        return new MergeBatchProcessor(
            batchFiles,
            mergeFileAdapter,
            mergeFileWriter,
            readers,
            mergeBatchProgressReporter,
            _itemComparer,
            _itemFormatter,
            _mergeBatchProcessorLogger);
    }

    private string CreateMergeFilePath()
    {
        var mergeNumber = Interlocked.Increment(ref _nextMergeNumber);
        var fileName = string.Format(CultureInfo.InvariantCulture, _mergeConfig.MergeFileTemplate, mergeNumber);
        return Path.Combine(_mergeConfig.TempFilesFolder, fileName);
    }
}
