namespace LargeFile.Sorter.Services.Abstractions;

public interface ISorterApplication
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
