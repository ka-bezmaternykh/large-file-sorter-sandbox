$projectPath = Join-Path $PSScriptRoot "..\LargeFile.Sorter\LargeFile.Sorter.csproj"
$outputPath = Join-Path $PSScriptRoot "..\LargeFile.Sorter\Release"

dotnet publish $projectPath -c Release -o $outputPath
