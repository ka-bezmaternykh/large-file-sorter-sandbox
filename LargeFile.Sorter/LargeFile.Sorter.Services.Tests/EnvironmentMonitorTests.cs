using LargeFile.Sorter.Services.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace LargeFile.Sorter.Services.Tests;

public class EnvironmentMonitorTests
{
    [Fact]
    public void Ctor_ShouldReadMemoryLimitFromDotNetGcHeapHardLimit()
    {
        const string environmentVariable = "DOTNET_GCHeapHardLimit";
        var originalValue = Environment.GetEnvironmentVariable(environmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(environmentVariable, "0x100000000");

            IEnvironmentMonitor monitor = new EnvironmentMonitor(NullLogger<EnvironmentMonitor>.Instance);

            Assert.Equal(4L * 1024 * 1024 * 1024, monitor.MemoryLimit);
            Assert.True(monitor.LevelOfParallelism > 0);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariable, originalValue);
        }
    }

    [Fact]
    public void WriteEnvironment_ShouldNotThrow()
    {
        IEnvironmentMonitor monitor = new EnvironmentMonitor(NullLogger<EnvironmentMonitor>.Instance);

        monitor.WriteEnvironment();
    }
}
