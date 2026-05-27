using LargeFile.Sorter.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLargeFileSorterServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISorterApplication, SorterApplication>();

        return services;
    }
}
