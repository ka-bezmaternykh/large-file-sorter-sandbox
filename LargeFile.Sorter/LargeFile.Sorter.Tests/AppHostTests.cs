using LargeFile.Sorter.Config;
using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Tests;

public class AppHostTests
{
    [Fact]
    public void Build_CreatesHostThatResolvesSorterApplicationAndOptions()
    {
        var commandLineOptions = new CommandLineOptions
        {
            File = "input.txt",
            OutputFile = "sorted.txt",
            TempFilesDir = "temp-root",
            Force = true
        };

        using var host = AppHost.Build([], commandLineOptions);

        var application = host.Services.GetRequiredService<ISorterApplication>();
        var runOptions = host.Services.GetRequiredService<SorterRunOptions>();
        var chunkConfig = host.Services.GetRequiredService<ChunkConfig>();

        Assert.NotNull(application);
        Assert.Equal("input.txt", runOptions.FilePath);
        Assert.Equal("sorted.txt", runOptions.OutputFilePath);
        Assert.True(runOptions.Force);
        Assert.Equal(128 * 1024 * 1024, chunkConfig.ChunkSize);
        Assert.Equal("temp-root", chunkConfig.TempFilesFolder);
        Assert.Equal("chunk-{0:D4}.tmp", chunkConfig.TempFileTemplate);
    }
}
