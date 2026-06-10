namespace DummyFile.Generator.Services.Abstract;

public interface IProgressLogger
{
    void LogProgress(long writtenBytes, int generatedLines, bool isFinal);
}
