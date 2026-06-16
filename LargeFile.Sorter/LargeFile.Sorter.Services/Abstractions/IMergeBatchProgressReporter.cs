namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Tracks and periodically logs write progress for one merge batch.
/// </summary>
public interface IMergeBatchProgressReporter
{
    /// <summary>
    /// Records additional bytes written by the batch output writer.
    /// </summary>
    void ReportBytesWritten(long bytesWritten);

    /// <summary>
    /// Stops periodic reporting and writes a final batch summary.
    /// </summary>
    ValueTask CompleteAsync(bool completedSuccessfully);
}
