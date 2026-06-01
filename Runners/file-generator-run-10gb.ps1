$projectPath = Join-Path $PSScriptRoot "..\DummyFile.Generator\DummyFile.Generator\DummyFile.Generator.csproj"
$outputFilePath = Join-Path $PSScriptRoot "..\LargeFiles\unsorted-10gb.txt"

dotnet run --project $projectPath -- --file $outputFilePath --file-size 10gb --force
