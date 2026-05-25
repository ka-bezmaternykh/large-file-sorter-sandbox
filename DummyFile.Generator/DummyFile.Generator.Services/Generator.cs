using DummyFile.Generator.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace DummyFile.Generator.Services;

public class Generator(
    IItemsGenerator itemsGenerator,
    IRowFormatter rowFormatter,
    IFileExporter fileExporter,
    ILogger<Generator> logger,
    GeneratorConfig config)
{
    // One generated row is ASCII, so bytes match characters here.
    // Expected max row length is 1037 bytes: 10 for int, 2 for ". ", 1024 for text, 1 for '\n'.
    // 1100 keeps a small safety margin without over-allocating too much.
    private const int RowBufferSizeBytes = 1100;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var rowBuffer = new byte[RowBufferSizeBytes];
        var generatedLines = 0;
        var maxLines = config.RequestedLinesNumber;

        logger.LogInformation("Generator loop started. MaxLines={MaxLines}", maxLines);

        while (!cancellationToken.IsCancellationRequested && (!maxLines.HasValue || generatedLines < maxLines.Value))
        {
            itemsGenerator.Generate(out var item);
            if (!rowFormatter.TryFormat(item, rowBuffer, out var written))
            {
                logger.LogWarning("Row formatter could not format the generated item. Skipping row.");
                continue;
            }

            if (!await fileExporter.TryExportAsync(rowBuffer.AsMemory(0, written), cancellationToken))
            {
                logger.LogInformation("Generator loop stopped because exporter rejected the next row.");
                break;
            }

            generatedLines++;
        }

        logger.LogInformation(
            "Generator loop finished. GeneratedLines={GeneratedLines}, CancellationRequested={CancellationRequested}",
            generatedLines,
            cancellationToken.IsCancellationRequested);
    }
}
