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
            Force = true
        };

        using var host = AppHost.Build([], commandLineOptions);

        var application = host.Services.GetRequiredService<ISorterApplication>();
        var runOptions = host.Services.GetRequiredService<SorterRunOptions>();

        Assert.NotNull(application);
        Assert.Equal("input.txt", runOptions.FilePath);
        Assert.Equal("sorted.txt", runOptions.OutputFilePath);
        Assert.True(runOptions.Force);
    }
}
