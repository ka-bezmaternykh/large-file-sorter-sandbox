$env:DOTNET_GCHeapHardLimit = "0x100000000" # 4 GiB

$projectPath = Join-Path $PSScriptRoot "..\LargeFile.Sorter\LargeFile.Sorter\LargeFile.Sorter.csproj"
$inputFilePath = Join-Path $PSScriptRoot "..\LargeFiles\unsorted-1gb.txt"
$outputFilePath = Join-Path $PSScriptRoot "..\LargeFiles\sorted-1gb.txt"
$tempFilesPath = Join-Path $PSScriptRoot "..\temp"

dotnet run --project $projectPath -- --file $inputFilePath --output-file $outputFilePath --temp-files-dir $tempFilesPath --force
