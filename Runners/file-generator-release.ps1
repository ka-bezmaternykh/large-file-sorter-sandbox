$projectPath = Join-Path $PSScriptRoot "..\DummyFile.Generator\DummyFile.Generator.csproj"
$outputPath = Join-Path $PSScriptRoot "..\DummyFile.Generator\Release"

dotnet publish $projectPath -c Release -o $outputPath
