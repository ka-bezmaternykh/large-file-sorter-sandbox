namespace LargeFile.Sorter.Services.Options;

public sealed class ChunkingProgressConfig
{
    public required TimeSpan ReportInterval { get; init; }
}
