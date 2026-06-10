$ErrorActionPreference = 'Stop'
$env:DOTNET_GCHeapHardLimit = "0x100000000" # 4 GiB

$releaseDir = Resolve-Path (Join-Path $PSScriptRoot "..\LargeFile.Sorter\Release")
$inputFilePath = Resolve-Path (Join-Path $PSScriptRoot "..\LargeFiles\unsorted-100-lines.txt")
$outputFilePath = Join-Path (Join-Path $PSScriptRoot "..\LargeFiles") "sorted-100-lines.txt"
$tempFilesPath = Resolve-Path (Join-Path $PSScriptRoot "..\temp")

$exePath = Join-Path $releaseDir "LargeFile.Sorter.exe"
if (-not (Test-Path $exePath)) {
    throw "Sorter executable was not found: $exePath"
}

$arguments = @(
    "--file", $inputFilePath,
    "--output-file", $outputFilePath,
    "--temp-files-dir", $tempFilesPath,
    "--force"
)

Start-Process -FilePath $exePath -ArgumentList $arguments -WorkingDirectory $releaseDir -Wait
