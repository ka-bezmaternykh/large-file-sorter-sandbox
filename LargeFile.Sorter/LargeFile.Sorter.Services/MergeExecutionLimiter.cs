using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class MergeExecutionLimiter : IMergeExecutionLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<MergeExecutionLimiter> _logger;

    public MergeExecutionLimiter(
        IEnvironmentMonitor environmentMonitor,
        MergeConfig mergeConfig,
        ILogger<MergeExecutionLimiter> logger)
    {
        ArgumentNullException.ThrowIfNull(environmentMonitor);
        ArgumentNullException.ThrowIfNull(mergeConfig);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        MaxConcurrentBatches = CalculateMaxConcurrentBatches(environmentMonitor, mergeConfig);
        _semaphore = new SemaphoreSlim(MaxConcurrentBatches, MaxConcurrentBatches);

        _logger.LogInformation(
            "Merge execution limiter allows up to {MaxConcurrentBatches} concurrent merge batches. MemoryLimit={MemoryLimit}, LevelOfParallelism={LevelOfParallelism}, MaxChunkFilesPerMerge={MaxChunkFilesPerMerge}, ConfiguredMaxConcurrentMergeBatches={ConfiguredMaxConcurrentMergeBatches}",
            MaxConcurrentBatches,
            environmentMonitor.MemoryLimit,
            environmentMonitor.LevelOfParallelism,
            mergeConfig.MaxChunkFilesPerMerge,
            mergeConfig.MaxConcurrentMergeBatches);
    }

    public int MaxConcurrentBatches { get; }

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Waiting for merge execution slot. MaxConcurrentBatches={MaxConcurrentBatches}, CurrentActiveBatches={CurrentActiveBatches}",
            MaxConcurrentBatches,
            MaxConcurrentBatches - _semaphore.CurrentCount);

        await _semaphore.WaitAsync(cancellationToken);
    }

    public void Release()
    {
        _semaphore.Release();
        _logger.LogDebug(
            "Released merge execution slot. MaxConcurrentBatches={MaxConcurrentBatches}, CurrentActiveBatches={CurrentActiveBatches}",
            MaxConcurrentBatches,
            MaxConcurrentBatches - _semaphore.CurrentCount);
    }

    private static int CalculateMaxConcurrentBatches(IEnvironmentMonitor environmentMonitor, MergeConfig mergeConfig)
    {
        var configuredCap = Math.Max(1, mergeConfig.MaxConcurrentMergeBatches);
        var cpuCap = Math.Max(1, environmentMonitor.LevelOfParallelism / 2);

        if (environmentMonitor.MemoryLimit <= 0)
        {
            return Math.Min(configuredCap, cpuCap);
        }

        var estimatedBytesPerBatch = Math.Max(mergeConfig.MaxChunkFilesPerMerge, 1) * 256L * 1024L;
        var memoryCap = Math.Max(1, (int)(environmentMonitor.MemoryLimit / estimatedBytesPerBatch));
        return Math.Min(configuredCap, Math.Min(cpuCap, memoryCap));
    }
}
