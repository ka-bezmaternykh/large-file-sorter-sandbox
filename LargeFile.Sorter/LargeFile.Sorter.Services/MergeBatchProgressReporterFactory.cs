using LargeFile.Sorter.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFile.Sorter.Services;

public sealed class MergeBatchProgressReporterFactory : IMergeBatchProgressReporterFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MergeBatchProgressReporterFactory(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _serviceProvider = serviceProvider;
    }

    public IMergeBatchProgressReporter Create(int passNumber, int batchNumber, int inputFileCount, long totalInputBytes)
    {
        return ActivatorUtilities.CreateInstance<MergeBatchProgressReporter>(
            _serviceProvider,
            passNumber,
            batchNumber,
            inputFileCount,
            totalInputBytes);
    }
}
