namespace LargeFile.Sorter.Services.Models;

public readonly record struct Item(long Number, byte[] TextBytes);
