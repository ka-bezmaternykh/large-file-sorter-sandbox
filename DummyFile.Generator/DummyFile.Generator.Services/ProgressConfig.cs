namespace DummyFile.Generator.Services;

public sealed class ProgressConfig
{
    public TimeSpan ProgressLogInterval { get; init; } = TimeSpan.FromSeconds(10);

    public int? RequestedLinesNumber { get; init; }

    public long? RequestedFileSizeBytes { get; init; }
}
