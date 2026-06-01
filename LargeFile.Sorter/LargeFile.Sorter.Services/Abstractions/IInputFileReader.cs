using System.IO.Pipelines;

namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Reads the source file sequentially and fills chunk writers with chunk-sized byte ranges.
/// </summary>
public interface IInputFileReader : IAsyncDisposable
{
    /// <summary>
    /// Determines whether the input still contains unread data for another chunk.
    /// </summary>
    ValueTask<bool> HasNextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the next chunk of input bytes into the provided pipe writer.
    /// </summary>
    ValueTask ReadNextAsync(PipeWriter writer, int chunkSize, CancellationToken cancellationToken = default);
}
