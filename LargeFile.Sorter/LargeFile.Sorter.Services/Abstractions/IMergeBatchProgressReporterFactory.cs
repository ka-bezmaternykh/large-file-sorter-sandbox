namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Creates batch-scoped merge progress reporters with runtime batch metadata.
/// </summary>
public interface IMergeBatchProgressReporterFactory
{
    /// <summary>
    /// Creates a reporter for a specific merge batch.
    /// </summary>
    IMergeBatchProgressReporter Create(int passNumber, int batchNumber, int inputFileCount, long totalInputBytes);
}
