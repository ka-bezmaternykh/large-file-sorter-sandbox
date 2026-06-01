using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class ChunkExecutionLimiter : IChunkExecutionLimiter, IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<ChunkExecutionLimiter> _logger;
    private bool _disposed;

    public ChunkExecutionLimiter(
        IEnvironmentMonitor environmentMonitor,
        ChunkConfig chunkConfig,
        ILogger<ChunkExecutionLimiter> logger)
    {
        ArgumentNullException.ThrowIfNull(environmentMonitor);
        ArgumentNullException.ThrowIfNull(chunkConfig);
        ArgumentNullException.ThrowIfNull(logger);

        if (chunkConfig.ChunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkConfig), chunkConfig.ChunkSize, "Chunk size must be positive.");
        }

        _logger = logger;
        MaxConcurrentSorters = CalculateMaxConcurrentSorters(environmentMonitor, chunkConfig);
        _semaphore = new SemaphoreSlim(MaxConcurrentSorters, MaxConcurrentSorters);
        _logger.LogInformation(
            "Chunk execution limiter allows {MaxConcurrentSorters} concurrent sorters. MemoryLimit={MemoryLimit} bytes, LevelOfParallelism={LevelOfParallelism}, ChunkSize={ChunkSize} bytes.",
            MaxConcurrentSorters,
            environmentMonitor.MemoryLimit,
            environmentMonitor.LevelOfParallelism,
            chunkConfig.ChunkSize);
    }

    public int MaxConcurrentSorters { get; }

    public ValueTask WaitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _logger.LogDebug(
            "Waiting for a chunk sorter execution slot. MaxConcurrentSorters={MaxConcurrentSorters}, CurrentActiveSorters={CurrentActiveSorters}.",
            MaxConcurrentSorters,
            GetCurrentActiveSorters());
        return new ValueTask(_semaphore.WaitAsync(cancellationToken));
    }

    public void Release()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _semaphore.Release();
        _logger.LogDebug(
            "Released a chunk sorter execution slot. MaxConcurrentSorters={MaxConcurrentSorters}, CurrentActiveSorters={CurrentActiveSorters}.",
            MaxConcurrentSorters,
            GetCurrentActiveSorters());
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _semaphore.Dispose();
        _disposed = true;
    }

    private static int CalculateMaxConcurrentSorters(IEnvironmentMonitor environmentMonitor, ChunkConfig chunkConfig)
    {
        var maxByCpu = Math.Max(1, environmentMonitor.LevelOfParallelism);

        if (environmentMonitor.MemoryLimit <= 0)
        {
            return maxByCpu;
        }

        var estimatedSorterFootprintBytes = chunkConfig.ChunkSize * 2L;
        var maxByMemory = Math.Max(1L, environmentMonitor.MemoryLimit / estimatedSorterFootprintBytes);

        return (int)Math.Min(maxByCpu, maxByMemory);
    }

    private int GetCurrentActiveSorters()
    {
        return MaxConcurrentSorters - _semaphore.CurrentCount;
    }
}
