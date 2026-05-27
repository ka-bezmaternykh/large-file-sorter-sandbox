using DummyFile.Generator.Config;

namespace DummyFile.Generator;

public static class FilesChecker
{
    private const double SafetyMarginPercent = 0.05d;

    public static bool TryValidate(CommandLineOptions options, out string[] validationErrors)
    {
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(options.File) && File.Exists(options.File) && !options.Force)
        {
            errors.Add("The file already exists. Use --force to overwrite it.");
        }

        if (!string.IsNullOrWhiteSpace(options.File) && options.FileSizeBytes.HasValue)
        {
            var requiredFreeSpaceBytes = CalculateRequiredFreeSpaceBytes(options.FileSizeBytes.Value);
            var availableFreeSpaceBytes = GetAvailableFreeSpaceBytes(options.File);
            if (availableFreeSpaceBytes < requiredFreeSpaceBytes)
            {
                errors.Add(
                    $"Not enough free disk space. Required at least {requiredFreeSpaceBytes} bytes for the requested file size plus 5% safety margin, but only {availableFreeSpaceBytes} bytes are available.");
            }
        }

        validationErrors = errors.ToArray();
        return validationErrors.Length == 0;
    }

    private static long CalculateRequiredFreeSpaceBytes(long requestedFileSizeBytes)
    {
        return requestedFileSizeBytes + (long)Math.Ceiling(requestedFileSizeBytes * SafetyMarginPercent);
    }

    private static long GetAvailableFreeSpaceBytes(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var rootPath = Path.GetPathRoot(fullPath)
                       ?? throw new InvalidOperationException($"Unable to determine drive root for path '{filePath}'.");

        var driveInfo = new DriveInfo(rootPath);
        return driveInfo.AvailableFreeSpace;
    }
}
