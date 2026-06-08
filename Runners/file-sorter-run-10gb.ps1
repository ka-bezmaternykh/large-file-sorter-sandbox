$ErrorActionPreference = 'Stop'
$env:DOTNET_GCHeapHardLimit = "0x100000000" # 4 GiB
$env:Logging__LogLevel__Default = "Debug"
${env:Logging__LogLevel__LargeFile.Sorter} = "Debug"
${env:Logging__LogLevel__LargeFile.Sorter.Services} = "Debug"
${env:Logging__LogLevel__LargeFile.Sorter.Services.ChunkExecutionLimiter} = "Debug"
${env:Logging__LogLevel__LargeFile.Sorter.Services.ChunkSorterFactory} = "Debug"
${env:Logging__LogLevel__LargeFile.Sorter.Services.SorterApplication} = "Debug"

$releaseDir = Resolve-Path (Join-Path $PSScriptRoot "..\LargeFile.Sorter\Release")
$inputFilePath = Resolve-Path (Join-Path $PSScriptRoot "..\LargeFiles\unsorted-10gb.txt")
$outputFilePath = Join-Path (Join-Path $PSScriptRoot "..\LargeFiles") "sorted-10gb.txt"
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
