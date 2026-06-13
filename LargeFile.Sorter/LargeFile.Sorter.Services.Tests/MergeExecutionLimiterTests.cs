using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace LargeFile.Sorter.Services.Tests;

public class MergeExecutionLimiterTests
{
    [Fact]
    public void Ctor_ShouldLimitByConfiguredParallelismAndMemory()
    {
        var environmentMonitor = new TestEnvironmentMonitor(memoryLimit: 1024L * 1024 * 1024, levelOfParallelism: 8);
        var mergeConfig = new MergeConfig
        {
            MaxChunkFilesPerMerge = 64,
            MaxConcurrentMergeBatches = 4,
            TempFilesFolder = "./temp",
            MergeFileTemplate = "merge-{0:D4}.tmp"
        };

        IMergeExecutionLimiter limiter = new MergeExecutionLimiter(
            environmentMonitor,
            mergeConfig,
            NullLogger<MergeExecutionLimiter>.Instance);

        Assert.Equal(4, limiter.MaxConcurrentBatches);
    }

    [Fact]
    public void Ctor_ShouldFallbackToCpuCapWhenMemoryLimitIsUnknown()
    {
        var environmentMonitor = new TestEnvironmentMonitor(memoryLimit: 0, levelOfParallelism: 6);
        var mergeConfig = new MergeConfig
        {
            MaxChunkFilesPerMerge = 64,
            MaxConcurrentMergeBatches = 10,
            TempFilesFolder = "./temp",
            MergeFileTemplate = "merge-{0:D4}.tmp"
        };

        IMergeExecutionLimiter limiter = new MergeExecutionLimiter(
            environmentMonitor,
            mergeConfig,
            NullLogger<MergeExecutionLimiter>.Instance);

        Assert.Equal(3, limiter.MaxConcurrentBatches);
    }

    private sealed class TestEnvironmentMonitor(long memoryLimit, int levelOfParallelism) : IEnvironmentMonitor
    {
        public long MemoryLimit { get; } = memoryLimit;

        public int LevelOfParallelism { get; } = levelOfParallelism;

        public void WriteEnvironment()
        {
        }
    }
}
