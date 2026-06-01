using System.IO.Pipelines;

namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Represents a single chunk processing target that accepts chunk bytes through a pipe.
/// </summary>
public interface IChunkSorter
{
    /// <summary>
    /// Gets the logical size limit of the chunk handled by this sorter.
    /// </summary>
    int ChunkSize { get; }

    /// <summary>
    /// Gets the pipe writer that receives bytes for the current chunk.
    /// </summary>
    PipeWriter Writer { get; }

    /// <summary>
    /// Starts reading chunk bytes from the pipe, sorting parsed items, and writing the result to the chunk file.
    /// </summary>
    Task StartSortingAsync(CancellationToken cancellationToken = default);
}
