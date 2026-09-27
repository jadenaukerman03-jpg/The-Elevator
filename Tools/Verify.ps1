param([string]$UnityEditor = '')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$results = Join-Path $projectRoot 'TestResults'
New-Item -ItemType Directory -Path $results -Force | Out-Null
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler not found.' }
$exe = Join-Path $results 'RulesChecks.exe'
& $compiler /nologo /target:exe "/out:$exe" (Join-Path $projectRoot 'Assets\Scripts\Core\RunRules.cs') (Join-Path $projectRoot 'Tests\RulesChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Rule check compilation failed.' }
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Rule checks failed.' }
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'Packages\manifest.json') -Raw | ConvertFrom-Json
if (-not $manifest.dependencies.'com.unity.modules.physics') { throw 'Physics module missing.' }
Write-Output 'PASS: Unity package manifest parses and includes physics.'
if ($UnityEditor) {
    if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw 'Unity Editor executable not found.' }
    $log = Join-Path $results 'unity-validation.log'
    $arguments = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $projectRoot + '"'), '-executeMethod', 'TheElevator.Editor.ProjectTools.ValidateProject', '-logFile', ('"' + $log + '"'))
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -PassThru -WindowStyle Hidden
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity validation failed. See $log" }
    if (-not (Select-String -LiteralPath $log -Pattern 'ELEVATOR VALIDATION PASSED' -Quiet)) { throw 'Unity did not confirm validation.' }
    Write-Output 'PASS: Unity compilation and scene validation.'
} else {
    Write-Output 'NOT RUN: Unity compilation, play mode, graphics, audio, and Windows build. Supply -UnityEditor to validate the import.'
}
