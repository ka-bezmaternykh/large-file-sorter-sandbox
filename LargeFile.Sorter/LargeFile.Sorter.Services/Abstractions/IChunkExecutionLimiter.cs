namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Controls how many chunk sorters are allowed to run concurrently based on execution limits.
/// </summary>
public interface IChunkExecutionLimiter
{
    /// <summary>
    /// Gets the maximum number of chunk sorters that may be active at the same time.
    /// </summary>
    int MaxConcurrentSorters { get; }

    /// <summary>
    /// Waits until a new chunk sorter is allowed to start.
    /// </summary>
    ValueTask WaitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a previously acquired execution slot.
    /// </summary>
    void Release();
}
