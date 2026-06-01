$projectPath = Join-Path $PSScriptRoot "..\DummyFile.Generator\DummyFile.Generator.csproj"
$outputFilePath = Join-Path $PSScriptRoot "..\LargeFiles\unsorted-1gb.txt"

dotnet run --project $projectPath -- --file $outputFilePath --file-size 1gb --force
