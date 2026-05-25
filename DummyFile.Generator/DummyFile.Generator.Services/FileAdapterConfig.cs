namespace DummyFile.Generator.Services;

public sealed class FileAdapterConfig
{
    public required string FilePath { get; init; }

    public bool IsOverwriteAllowed { get; init; }
}
