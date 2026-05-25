namespace DummyFile.Generator.Services.Abstract;

public interface IRowFormatter
{
    bool TryFormat(Item item, Span<byte> destination, out int written);
}
