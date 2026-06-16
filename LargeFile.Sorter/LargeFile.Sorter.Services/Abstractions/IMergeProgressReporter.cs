namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Aggregates and periodically logs high-level progress for the merge phase across passes and batches.
/// </summary>
public interface IMergeProgressReporter
{
    /// <summary>
    /// Starts merge-phase progress reporting for the given initial temp-file set.
    /// </summary>
    void Start(int initialFileCount, int maxChunkFilesPerMerge);

    /// <summary>
    /// Records the start of a merge pass and its planned batch count.
    /// </summary>
    void ReportPassStarted(int passNumber, int inputFileCount, int plannedBatchCount);

    /// <summary>
    /// Records that a merge batch has been scheduled for execution.
    /// </summary>
    void ReportBatchScheduled();

    /// <summary>
    /// Records that a merge batch has completed successfully.
    /// </summary>
    void ReportBatchCompleted();

    /// <summary>
    /// Records that a merge batch has finished with an error.
    /// </summary>
    void ReportBatchFailed();

    /// <summary>
    /// Records the completion of a merge pass and the number of output temp files it produced.
    /// </summary>
    void ReportPassCompleted(int passNumber, int outputFileCount);

    /// <summary>
    /// Records that the final merge output has been promoted to the configured destination.
    /// </summary>
    void ReportFinalPromoted();

    /// <summary>
    /// Stops periodic reporting and writes a final merge-phase summary.
    /// </summary>
    ValueTask CompleteAsync();
}
