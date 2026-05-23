using DummyFile.Generator.Config;

namespace DummyFile.Generator.Tests;

public class FilesCheckerTests
{
    [Fact]
    public void TryValidate_ShouldPassWhenFileDoesNotExist()
    {
        var options = new CommandLineOptions
        {
            File = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt")
        };

        var success = FilesChecker.TryValidate(options, out var validationErrors);

        Assert.True(success);
        Assert.Empty(validationErrors);
    }

    [Fact]
    public void TryValidate_ShouldFailWhenFileExistsAndForceIsFalse()
    {
        var filePath = Path.GetTempFileName();

        try
        {
            var options = new CommandLineOptions
            {
                File = filePath,
                Force = false
            };

            var success = FilesChecker.TryValidate(options, out var validationErrors);

            Assert.False(success);
            Assert.Equal(["The file already exists. Use --force to overwrite it."], validationErrors);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void TryValidate_ShouldPassWhenFileExistsAndForceIsTrue()
    {
        var filePath = Path.GetTempFileName();

        try
        {
            var options = new CommandLineOptions
            {
                File = filePath,
                Force = true
            };

            var success = FilesChecker.TryValidate(options, out var validationErrors);

            Assert.True(success);
            Assert.Empty(validationErrors);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
