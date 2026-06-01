using System.Globalization;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class ChunkSorterFactory : IChunkSorterFactory
{
    private readonly ChunkConfig _chunkConfig;
    private readonly IChunkExecutionLimiter _chunkExecutionLimiter;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChunkSorterFactory> _logger;
    private readonly Dictionary<int, IChunkFileAdapter> _chunkFileAdapters = [];
    private readonly List<Task> _activeSorters = [];
    private readonly Lock _syncRoot = new();
    private int _nextChunkNumber;

    public ChunkSorterFactory(
        ChunkConfig chunkConfig,
        IChunkExecutionLimiter chunkExecutionLimiter,
        IServiceProvider serviceProvider,
        ILogger<ChunkSorterFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(chunkConfig);
        ArgumentNullException.ThrowIfNull(chunkExecutionLimiter);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _chunkConfig = chunkConfig;
        _chunkExecutionLimiter = chunkExecutionLimiter;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IReadOnlyDictionary<int, IChunkFileAdapter> ChunkFileAdapters => _chunkFileAdapters;

    public async Task<IChunkSorter> CreateAsync(CancellationToken cancellationToken = default)
    {
        await _chunkExecutionLimiter.WaitAsync(cancellationToken);
        try
        {
            var chunkNumber = Interlocked.Increment(ref _nextChunkNumber);
            var filePath = CreateFilePath(chunkNumber);

            var chunkFileConfig = new ChunkFileConfig
            {
                FilePath = filePath
            };
            var chunkFileAdapter = ActivatorUtilities.CreateInstance<ChunkFileAdapter>(_serviceProvider, chunkFileConfig);
            _chunkFileAdapters[chunkNumber] = chunkFileAdapter;

            var chunkFileWriter = ActivatorUtilities.CreateInstance<ChunkFileWriter>(_serviceProvider, chunkFileAdapter);
            var chunkSorter = ActivatorUtilities.CreateInstance<ChunkSorter>(_serviceProvider, chunkFileWriter);
            var processingTask = TrackSorterAsync(chunkSorter);

            lock (_syncRoot)
            {
                _activeSorters.Add(processingTask);
            }

            _logger.LogInformation("Created chunk sorter #{ChunkNumber} with file {FileName}.", chunkNumber, filePath);
            return chunkSorter;
        }
        catch
        {
            _chunkExecutionLimiter.Release();
            throw;
        }
    }

    private string CreateFilePath(int chunkNumber)
    {
        var fileName = string.Format(CultureInfo.InvariantCulture, _chunkConfig.TempFileTemplate, chunkNumber);
        var filePath = Path.Combine(_chunkConfig.TempFilesFolder, fileName);

        return filePath;
    }

    public async Task WaitAllAsync(CancellationToken cancellationToken = default)
    {
        Task[] tasks;
        lock (_syncRoot)
        {
            tasks = [.. _activeSorters];
            _activeSorters.Clear();
        }

        await Task.WhenAll(tasks).WaitAsync(cancellationToken);
    }

    private async Task TrackSorterAsync(ChunkSorter chunkSorter)
    {
        try
        {
            await chunkSorter.StartSortingAsync();
        }
        finally
        {
            await chunkSorter.DisposeAsync();
            _chunkExecutionLimiter.Release();
        }
    }
}
