using LargeFile.Sorter.Services.Abstractions;
using LargeFile.Sorter.Services.Options;
using Microsoft.Extensions.Logging;

namespace LargeFile.Sorter.Services;

public sealed class InputFileAdapter : IInputFileAdapter
{
    private readonly ILogger<InputFileAdapter> _logger;

    public InputFileAdapter(InputFileConfig config, ILogger<InputFileAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.FilePath);
        ArgumentNullException.ThrowIfNull(logger);

        FilePath = config.FilePath;
        _logger = logger;
    }

    public string FilePath { get; }

    public bool TryOpenReadStream(out FileStream? stream)
    {
        if (!File.Exists(FilePath))
        {
            _logger.LogWarning("Input file does not exist: {FilePath}", FilePath);
            stream = null;
            return false;
        }

        stream = new FileStream(
            FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
        return true;
    }
}
