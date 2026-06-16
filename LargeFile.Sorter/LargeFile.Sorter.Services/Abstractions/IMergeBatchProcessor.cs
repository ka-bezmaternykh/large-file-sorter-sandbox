namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Performs a single k-way merge for one batch of temporary sorted files.
/// </summary>
public interface IMergeBatchProcessor
{
    /// <summary>
    /// Merges one batch of sorted temporary files into a single temporary output file.
    /// </summary>
    Task<ITempFileAdapter> MergeBatchAsync(
        IReadOnlyList<ITempFileAdapter> batchFiles,
        int passNumber,
        int batchNumber,
        CancellationToken cancellationToken = default);
}
