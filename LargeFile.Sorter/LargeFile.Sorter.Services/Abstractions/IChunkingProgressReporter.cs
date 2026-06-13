namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Aggregates chunking-phase progress from multiple components and periodically writes it to the log.
/// </summary>
public interface IChunkingProgressReporter
{
    /// <summary>
    /// Starts chunking progress tracking for the provided total input size.
    /// </summary>
    void Start(long totalInputBytes);

    /// <summary>
    /// Reports that additional source bytes have been read into chunk processing.
    /// </summary>
    void ReportBytesRead(long bytesRead);

    /// <summary>
    /// Reports that a new chunk has been created.
    /// </summary>
    void ReportChunkCreated();

    /// <summary>
    /// Reports that a chunk sorter has started active processing.
    /// </summary>
    void ReportSorterStarted();

    /// <summary>
    /// Reports that a chunk has completed processing successfully.
    /// </summary>
    void ReportChunkCompleted();

    /// <summary>
    /// Reports that a chunk sorter is no longer active.
    /// </summary>
    void ReportSorterFinished();

    /// <summary>
    /// Stops periodic reporting and writes a final summary.
    /// </summary>
    ValueTask CompleteAsync();
}
