$projectPath = Join-Path $PSScriptRoot "..\DummyFile.Generator\DummyFile.Generator\DummyFile.Generator.csproj"
$outputFilePath = Join-Path $PSScriptRoot "..\LargeFiles\unsorted-100-lines.txt"

dotnet run --project $projectPath -- --file $outputFilePath --file-size-lines 100 --force
