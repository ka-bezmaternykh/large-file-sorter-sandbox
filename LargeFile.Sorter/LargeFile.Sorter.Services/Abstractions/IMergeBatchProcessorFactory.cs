namespace LargeFile.Sorter.Services.Abstractions;

/// <summary>
/// Creates dedicated merge batch processors together with their temp output targets.
/// </summary>
public interface IMergeBatchProcessorFactory
{
    /// <summary>
    /// Creates a processor for one merge batch.
    /// </summary>
    IMergeBatchProcessor Create(IReadOnlyList<ITempFileAdapter> batchFiles, int passNumber, int batchNumber);
}
