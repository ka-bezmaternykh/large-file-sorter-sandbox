namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Creates chunk sorters, tracks produced chunk files, and manages their execution lifecycle.
/// </summary>
public interface IChunkSorterFactory
{
    /// <summary>
    /// Gets the registry of chunk file adapters keyed by chunk number.
    /// </summary>
    IReadOnlyDictionary<int, IChunkFileAdapter> ChunkFileAdapters { get; }

    /// <summary>
    /// Creates the next chunk sorter when execution limits allow it.
    /// </summary>
    Task<IChunkSorter> CreateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until all created chunk sorters finish processing and release their resources.
    /// </summary>
    Task WaitAllAsync(CancellationToken cancellationToken = default);
}
