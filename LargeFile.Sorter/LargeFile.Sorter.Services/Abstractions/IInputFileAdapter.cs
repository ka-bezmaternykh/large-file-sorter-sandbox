namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Owns the source input file path and opens read streams for it.
/// </summary>
public interface IInputFileAdapter : IAsyncDisposable
{
    /// <summary>
    /// Gets the full path of the input file.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Tries to open a readable stream for the input file without throwing for a missing file.
    /// </summary>
    bool TryOpenReadStream(out FileStream? stream);
}
