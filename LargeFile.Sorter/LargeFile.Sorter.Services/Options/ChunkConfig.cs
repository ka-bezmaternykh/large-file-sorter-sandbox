namespace LargeFile.Sorter.Services.Options;

public sealed class ChunkConfig
{
    public required int ChunkSize { get; init; }

    public required string TempFilesFolder { get; init; }

    public required string TempFileTemplate { get; init; }
}
