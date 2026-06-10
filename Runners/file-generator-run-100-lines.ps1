$applicationPath = Join-Path $PSScriptRoot "..\DummyFile.Generator\Release\DummyFile.Generator.exe"
$outputFilePath = Join-Path $PSScriptRoot "..\LargeFiles\unsorted-100-lines.txt"

if (-not (Test-Path $applicationPath)) {
    throw "Published DummyFile.Generator.exe was not found at '$applicationPath'. Run .\file-generator-release.ps1 first."
}

Start-Process -FilePath $applicationPath -ArgumentList @("--file", $outputFilePath, "--file-size-lines", "100", "--force") -Wait -NoNewWindow
