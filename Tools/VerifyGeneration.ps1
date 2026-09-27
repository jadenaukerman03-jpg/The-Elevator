$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$results = Join-Path $projectRoot 'TestResults'
New-Item -ItemType Directory -Path $results -Force | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\Scripts\Generation\Core') -Filter '*.cs' | ForEach-Object FullName)
$sources += Join-Path $projectRoot 'Tests\GenerationChecks.cs'
$exe = Join-Path $results 'GenerationChecks.exe'
& $compiler /nologo /target:exe "/out:$exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Generation checks did not compile.' }
& $exe | Tee-Object -FilePath (Join-Path $results 'generation-checks.txt')
if ($LASTEXITCODE -ne 0) { throw 'Generation checks failed.' }
