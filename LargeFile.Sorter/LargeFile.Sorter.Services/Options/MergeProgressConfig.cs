namespace LargeFile.Sorter.Services.Options;

public sealed class MergeProgressConfig
{
    public required TimeSpan ReportInterval { get; init; }

    public required TimeSpan BatchReportInterval { get; init; }
}
