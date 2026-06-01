$env:DOTNET_GCHeapHardLimit = "0x100000000" # 4 GiB

$projectPath = Join-Path $PSScriptRoot "..\LargeFile.Sorter\LargeFile.Sorter\LargeFile.Sorter.csproj"
$inputFilePath = Join-Path $PSScriptRoot "..\LargeFiles\unsorted-100mb.txt"
$outputFilePath = Join-Path $PSScriptRoot "..\LargeFiles\sorted-100mb.txt"

dotnet run --project $projectPath -- --file $inputFilePath --output-file $outputFilePath --force
