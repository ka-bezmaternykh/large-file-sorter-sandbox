using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.DependencyInjection;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Services.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddLargeFileSorterServices_RegistersSorterApplication()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new SorterRunOptions
        {
            FilePath = "input.txt",
            OutputFilePath = "sorted.txt",
            Force = true
        });
        services.AddLargeFileSorterServices();

        await using var serviceProvider = services.BuildServiceProvider();

        var application = serviceProvider.GetRequiredService<ISorterApplication>();

        await application.RunAsync();
    }
}
