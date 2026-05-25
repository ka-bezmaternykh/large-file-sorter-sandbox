using DummyFile.Generator.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace DummyFile.Generator.Services;

public sealed class FileAdapter : IFileAdapter
{
    private readonly FileAdapterConfig _config;
    private readonly ILogger<FileAdapter> _logger;

    public FileAdapter(FileAdapterConfig config, ILogger<FileAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.FilePath);
        ArgumentNullException.ThrowIfNull(logger);

        _config = config;
        _logger = logger;
    }

    public Stream CreateWriteStream()
    {
        var directoryPath = Path.GetDirectoryName(_config.FilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            _logger.LogInformation("Ensuring directory exists for output file: {DirectoryPath}", directoryPath);
            Directory.CreateDirectory(directoryPath);
        }

        if (File.Exists(_config.FilePath))
        {
            if (!_config.IsOverwriteAllowed)
            {
                _logger.LogWarning("Output file already exists and force is disabled: {FilePath}", _config.FilePath);
                throw new IOException($"File '{_config.FilePath}' already exists. Use --force to overwrite it.");
            }

            _logger.LogInformation("Deleting existing output file because force is enabled: {FilePath}", _config.FilePath);
            File.Delete(_config.FilePath);
        }

        _logger.LogInformation("Creating output file stream: {FilePath}", _config.FilePath);
        return new FileStream(
            path: _config.FilePath,
            mode: FileMode.CreateNew,
            access: FileAccess.Write,
            share: FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);
    }
}
