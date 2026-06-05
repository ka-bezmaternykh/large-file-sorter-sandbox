namespace LargeFile.Sorter.Services.Options;

public sealed class MergeConfig
{
    public int MaxChunkFilesPerMerge { get; init; }

    public required string TempFilesFolder { get; init; }

    public required string MergeFileTemplate { get; init; }
}
