namespace DummyFile.Generator.Config;

public sealed class CommandLineOptions
{
    public string? File { get; set; }

    public int? FileSizeLines { get; set; }

    public bool Force { get; set; }

    public bool ShowHelp { get; set; }
}
