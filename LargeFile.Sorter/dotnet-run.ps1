$env:DOTNET_GCHeapHardLimit = "0x100000000" # 4 GiB

dotnet run --project LargeFile.Sorter -- --file ..\Files\input.txt --output-file ..\Files\sorted.txt --force
