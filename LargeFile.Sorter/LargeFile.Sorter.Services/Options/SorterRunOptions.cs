namespace LargeFile.Sorter.Services.Options;

public sealed class SorterRunOptions
{
    public required string FilePath { get; init; }

    public required string OutputFilePath { get; init; }

    public bool Force { get; init; }
}
