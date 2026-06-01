namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Orchestrates the end-to-end sorting workflow of the application.
/// </summary>
public interface ISorterApplication
{
    /// <summary>
    /// Runs the sorting pipeline.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken = default);
}
