$ErrorActionPreference = 'Stop'
$previousDotnetGcHeapHardLimit = [System.Environment]::GetEnvironmentVariable('DOTNET_GCHeapHardLimit', 'Process')

try {
    [System.Environment]::SetEnvironmentVariable('DOTNET_GCHeapHardLimit', '0x100000000', 'Process')
    # [System.Environment]::SetEnvironmentVariable('DOTNET_GCHeapHardLimit', '0x200000000', 'Process')
    # [System.Environment]::SetEnvironmentVariable('DOTNET_GCHeapHardLimit', '0x300000000', 'Process')
    # [System.Environment]::SetEnvironmentVariable('DOTNET_GCHeapHardLimit', '0x400000000', 'Process')

    $releaseDir = Resolve-Path (Join-Path $PSScriptRoot "..\LargeFile.Sorter\Release")
    $inputFilePath = Resolve-Path (Join-Path $PSScriptRoot "..\LargeFiles\unsorted-100mb.txt")
    $outputFilePath = Join-Path (Join-Path $PSScriptRoot "..\LargeFiles") "sorted-100mb.txt"
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
}
finally {
    [System.Environment]::SetEnvironmentVariable('DOTNET_GCHeapHardLimit', $previousDotnetGcHeapHardLimit, 'Process')
}
