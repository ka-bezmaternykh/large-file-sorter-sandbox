namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Limits how many merge batches may execute concurrently.
/// </summary>
public interface IMergeExecutionLimiter
{
    /// <summary>
    /// Gets the maximum number of concurrently executing merge batches.
    /// </summary>
    int MaxConcurrentBatches { get; }

    /// <summary>
    /// Waits until a merge execution slot becomes available.
    /// </summary>
    Task WaitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a previously acquired merge execution slot.
    /// </summary>
    void Release();
}
