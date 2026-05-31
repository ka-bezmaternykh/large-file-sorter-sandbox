namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Exposes runtime environment limits and reports them to logging.
/// </summary>
public interface IEnvironmentMonitor
{
    /// <summary>
    /// Gets the effective memory limit available to the current process.
    /// </summary>
    long MemoryLimit { get; }

    /// <summary>
    /// Gets the preferred degree of parallelism derived from the current environment.
    /// </summary>
    int LevelOfParallelism { get; }

    /// <summary>
    /// Writes the current environment settings to the log.
    /// </summary>
    void WriteEnvironment();
}
