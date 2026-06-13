namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Coordinates multi-pass batched merge sorting across temporary sorted files.
/// </summary>
public interface IMergeSortingCoordinator
{
    /// <summary>
    /// Initializes merge coordination with the initial set of sorted chunk files.
    /// </summary>
    void Start(IReadOnlyDictionary<int, ITempFileAdapter> initialFiles);

    /// <summary>
    /// Gets whether the current merge pass still has pending batches to start.
    /// </summary>
    bool HasNextBatch { get; }

    /// <summary>
    /// Gets whether more than one temporary file still remains in the merge pipeline.
    /// </summary>
    bool HasMoreThanOneFile { get; }

    /// <summary>
    /// Starts the next pending merge batch when execution limits allow it.
    /// </summary>
    Task MergeNextBatchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for all started batches of the current pass to complete and prepares the next pass.
    /// </summary>
    Task CompleteCurrentPassAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Promotes the final merged file to the configured output path.
    /// </summary>
    Task PromoteFinalAsync(CancellationToken cancellationToken = default);
}
