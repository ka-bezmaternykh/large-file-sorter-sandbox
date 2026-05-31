using System.Globalization;
using LargeFile.Sorter.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class EnvironmentMonitor : IEnvironmentMonitor
{
    private const string DotNetGcHeapHardLimit = "DOTNET_GCHeapHardLimit";

    private readonly ILogger<EnvironmentMonitor> _logger;

    public EnvironmentMonitor(ILogger<EnvironmentMonitor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        MemoryLimit = ResolveMemoryLimit();
        LevelOfParallelism = Environment.ProcessorCount;
    }

    public long MemoryLimit { get; }

    public int LevelOfParallelism { get; }

    public void WriteEnvironment()
    {
        _logger.LogInformation(
            "Environment settings: MemoryLimit={MemoryLimit} bytes, LevelOfParallelism={LevelOfParallelism}.",
            MemoryLimit,
            LevelOfParallelism);
    }

    private static long ResolveMemoryLimit()
    {
        var configuredLimit = Environment.GetEnvironmentVariable(DotNetGcHeapHardLimit);
        if (TryParseBytes(configuredLimit, out var parsedLimit))
        {
            return parsedLimit;
        }

        var runtimeLimit = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        return runtimeLimit > 0 ? runtimeLimit : 0;
    }

    private static bool TryParseBytes(string? value, out long bytes)
    {
        bytes = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return long.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes) && bytes > 0;
        }

        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out bytes) && bytes > 0;
    }
}
