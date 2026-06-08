namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Merges up to the configured number of sorted chunk files into the final output file.
/// </summary>
public interface IMergeSorter
{
    /// <summary>
    /// Performs a single-pass merge for the provided chunk files.
    /// </summary>
    Task MergeAsync(IReadOnlyDictionary<int, ITempFileAdapter> chunkFileAdapters, CancellationToken cancellationToken = default);
}
