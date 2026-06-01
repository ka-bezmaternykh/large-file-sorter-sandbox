using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace LargeFile.Sorter.Services.Tests;

public class ChunkExecutionLimiterTests
{
    [Fact]
    public void Ctor_ShouldLimitByMemoryAndParallelism()
    {
        var environmentMonitor = new TestEnvironmentMonitor(memoryLimit: 1024L * 1024 * 1024, levelOfParallelism: 8);
        var chunkConfig = new ChunkConfig
        {
            ChunkSize = 128 * 1024 * 1024,
            TempFilesFolder = "./temp",
            TempFileTemplate = "chunk-{0:D4}.tmp"
        };

        IChunkExecutionLimiter limiter = new ChunkExecutionLimiter(
            environmentMonitor,
            chunkConfig,
            NullLogger<ChunkExecutionLimiter>.Instance);

        Assert.Equal(4, limiter.MaxConcurrentSorters);
    }

    [Fact]
    public void Ctor_ShouldFallbackToParallelismWhenMemoryLimitIsUnknown()
    {
        var environmentMonitor = new TestEnvironmentMonitor(memoryLimit: 0, levelOfParallelism: 6);
        var chunkConfig = new ChunkConfig
        {
            ChunkSize = 128 * 1024 * 1024,
            TempFilesFolder = "./temp",
            TempFileTemplate = "chunk-{0:D4}.tmp"
        };

        IChunkExecutionLimiter limiter = new ChunkExecutionLimiter(
            environmentMonitor,
            chunkConfig,
            NullLogger<ChunkExecutionLimiter>.Instance);

        Assert.Equal(6, limiter.MaxConcurrentSorters);
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
