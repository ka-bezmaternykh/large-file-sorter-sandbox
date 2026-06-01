$projectPath = Join-Path $PSScriptRoot "..\DummyFile.Generator\DummyFile.Generator\DummyFile.Generator.csproj"
$outputFilePath = Join-Path $PSScriptRoot "..\LargeFiles\unsorted-100mb.txt"

dotnet run --project $projectPath -- --file $outputFilePath --file-size 100mb --force
