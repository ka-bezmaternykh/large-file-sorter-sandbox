using DummyFile.Generator.Config;

namespace DummyFile.Generator.Tests;

public class CommandLineOptionsTests
{
    [Fact]
    public void TryParse_ShouldReadFileAndLineCount()
    {
        var success = CommandLineOptionsParser.TryParse(
            ["--file", "output.txt", "--file-size-lines", "42", "--force"],
            out var options,
            out var validationErrors);

        Assert.True(success);
        Assert.Empty(validationErrors);
        Assert.Equal("output.txt", options.File);
        Assert.Equal(42, options.FileSizeLines);
        Assert.True(options.Force);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    public void TryParse_ShouldEnableHelpMode()
    {
        var success = CommandLineOptionsParser.TryParse(["--help"], out var options, out var validationErrors);

        Assert.True(success);
        Assert.Empty(validationErrors);
        Assert.True(options.ShowHelp);
    }

    [Fact]
    public void TryParse_ShouldFailWhenValueIsMissing()
    {
        var success = CommandLineOptionsParser.TryParse(["--file"], out _, out var validationErrors);

        Assert.False(success);
        Assert.Equal(["Missing value for --file"], validationErrors);
    }

    [Fact]
    public void TryParse_ShouldFailWhenLineCountIsInvalid()
    {
        var success = CommandLineOptionsParser.TryParse(["--file-size-lines", "0"], out _, out var validationErrors);

        Assert.False(success);
        Assert.Equal(["The value for --file-size-lines must be a positive integer."], validationErrors);
    }

    [Fact]
    public void TryParse_ShouldFailForUnknownArguments()
    {
        var success = CommandLineOptionsParser.TryParse(["--unknown"], out _, out var validationErrors);

        Assert.False(success);
        Assert.Equal(["Unknown argument: --unknown"], validationErrors);
    }

    [Fact]
    public void TryParse_ShouldCollectMultipleValidationErrors()
    {
        var success = CommandLineOptionsParser.TryParse(
            ["--unknown", "--file-size-lines", "0", "--file"],
            out _,
            out var validationErrors);

        Assert.False(success);
        Assert.Equal(
            [
                "Unknown argument: --unknown",
                "The value for --file-size-lines must be a positive integer.",
                "Missing value for --file"
            ],
            validationErrors);
    }
}
