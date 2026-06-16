namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Represents one dedicated merge worker responsible for processing a single batch of temp files.
/// </summary>
public interface IMergeBatchProcessor : IAsyncDisposable
{
    /// <summary>
    /// Starts merging the assigned batch into a single temp output file.
    /// </summary>
    Task<ITempFileAdapter> StartMergingAsync(CancellationToken cancellationToken = default);
}
