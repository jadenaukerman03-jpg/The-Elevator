param([string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw 'Set -UnityEditor to your Unity 6000.3.25f1 executable.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
$validationRoot = Join-Path $projectRoot 'TestResults\GenerationValidation'
New-Item -ItemType Directory -Path $validationRoot -Force | Out-Null
$busy = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($validationRoot) }
if ($busy) { throw 'The isolated validation project is already open. Wait for that run to finish.' }
foreach ($folder in @('Assets','Packages','ProjectSettings')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $folder) -Destination $validationRoot -Recurse -Force
}
function Invoke-Validation([string]$method, [string]$logName, [bool]$quit) {
    $logPath = Join-Path $validationRoot $logName
    $unityArgs = @('-batchmode','-force-d3d11','-projectPath',('"' + $validationRoot + '"'),'-executeMethod',$method,'-logFile',('"' + $logPath + '"'))
    if ($quit) { $unityArgs += '-quit' }
    $validationProcess = Start-Process -FilePath $UnityEditor -ArgumentList $unityArgs -PassThru -WindowStyle Hidden
    if (-not $validationProcess.WaitForExit(240000)) {
        $validationProcess.Kill()
        throw "Validation exceeded four minutes. Only the test process was stopped. See $logPath"
    }
    Select-String -LiteralPath $logPath -Pattern 'VALIDATION PASSED|GEOMETRY PASSED|MAP READY|error CS|Shader error' | ForEach-Object Line
    if ($validationProcess.ExitCode -ne 0) { throw "Unity validation failed. See $logPath" }
}
Invoke-Validation 'TheElevator.Editor.GenerationValidation.BatchValidate' 'validation.log' $true
Invoke-Validation 'TheElevator.Editor.GenerationPlayValidation.Run' 'play-validation.log' $false
foreach ($file in @('generated-arrival.png','validated-map.json','play-validation.txt')) {
    Copy-Item -LiteralPath (Join-Path $validationRoot ('TestResults\' + $file)) -Destination (Join-Path $projectRoot 'TestResults') -Force
}
Write-Output 'PASS: isolated Unity import, geometry and Play-mode generation checks.'
