namespace LargeFile.Sorter.Config;

public sealed class CommandLineOptions
{
    public string? File { get; set; }

    public string? OutputFile { get; set; }

    public bool Force { get; set; }

    public bool ShowHelp { get; set; }
}
