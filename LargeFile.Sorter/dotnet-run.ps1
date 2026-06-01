$env:DOTNET_GCHeapHardLimit = "0x100000000" # 4 GiB

dotnet run --project LargeFile.Sorter -- --file ..\LargeFiles\test.txt --output-file ..\LargeFiles\sorted.txt --force
