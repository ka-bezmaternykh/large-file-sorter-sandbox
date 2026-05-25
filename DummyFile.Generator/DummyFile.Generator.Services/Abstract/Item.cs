namespace DummyFile.Generator.Services.Abstract;

public readonly struct Item
{
    public Item(int number, ReadOnlyMemory<byte> text)
    {
        Number = number;
        Text = text;
    }

    public int Number { get; }

    public ReadOnlyMemory<byte> Text { get; }

    public ReadOnlySpan<byte> TextSpan => Text.Span;
}
