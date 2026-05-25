$publishedExePath = Join-Path $PSScriptRoot "..\publish\DummyFile.Generator.exe"
$outputFilePath = Join-Path $PSScriptRoot "dummy-file-1gb.txt"

if (-not (Test-Path $publishedExePath)) {
    throw "Published executable was not found at '$publishedExePath'. Run .\dotnet-publish.ps1 first."
}

$arguments = @(
    "--file", $outputFilePath,
    "--file-size", "1gb",
    "--force"
)

$startedAt = Get-Date
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$standardOutputPath = Join-Path $PSScriptRoot "dummy-file-generator.stdout.log"
$standardErrorPath = Join-Path $PSScriptRoot "dummy-file-generator.stderr.log"

$process = Start-Process $publishedExePath `
    -ArgumentList $arguments `
    -WorkingDirectory (Split-Path $publishedExePath -Parent) `
    -RedirectStandardOutput $standardOutputPath `
    -RedirectStandardError $standardErrorPath `
    -PassThru

$peakWorkingSetBytes = 0
$lastWorkingSetBytes = 0

while (-not $process.HasExited) {
    $process.Refresh()

    if ($process.WorkingSet64 -gt $peakWorkingSetBytes) {
        $peakWorkingSetBytes = $process.WorkingSet64
    }

    $lastWorkingSetBytes = $process.WorkingSet64
    Start-Sleep -Milliseconds 200
}

$process.WaitForExit()
$process.Refresh()

$stopwatch.Stop()

if ($process.ExitCode -ne 0) {
    $standardOutput = if (Test-Path $standardOutputPath) { Get-Content $standardOutputPath -Raw } else { "" }
    $standardError = if (Test-Path $standardErrorPath) { Get-Content $standardErrorPath -Raw } else { "" }

    throw @"
DummyFile.Generator exited with code $($process.ExitCode).
Republish the application first by running .\dotnet-publish.ps1 if the published executable is outdated.

Standard output:
$standardOutput

Standard error:
$standardError
"@
}

if (-not (Test-Path $outputFilePath)) {
    throw "Expected output file was not created: '$outputFilePath'."
}

$outputFile = Get-Item $outputFilePath

[pscustomobject]@{
    StartedAt         = $startedAt
    Elapsed           = $stopwatch.Elapsed
    OutputFilePath    = $outputFile.FullName
    OutputFileSizeMB  = [math]::Round($outputFile.Length / 1MB, 2)
    OutputFileSizeGB  = [math]::Round($outputFile.Length / 1GB, 3)
    PeakWorkingSetMB  = [math]::Round($peakWorkingSetBytes / 1MB, 2)
    WorkingSetMB      = [math]::Round($lastWorkingSetBytes / 1MB, 2)
    ExitCode          = $process.ExitCode
}
