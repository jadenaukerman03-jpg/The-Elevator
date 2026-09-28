param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe',
    [switch]$BuildPlayer
)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$validationRoot=Join-Path $projectRoot 'TestResults\GenerationValidation'
New-Item -ItemType Directory -Path $validationRoot -Force | Out-Null
$busy=Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($validationRoot) }
if($busy){throw 'The isolated Unity validation project is already open.'}
foreach($folder in @('Assets','Packages','ProjectSettings')){Copy-Item -LiteralPath (Join-Path $projectRoot $folder) -Destination $validationRoot -Recurse -Force}
$checks=@(@('TheElevator.Editor.CabinValidation.Run','cabin-validation.log',$false),@('TheElevator.Editor.OfficeValidation.RunGeometry','office-geometry.log',$true),@('TheElevator.Editor.OfficeValidation.RunPlay','office-play.log',$false),@('TheElevator.Editor.OfficeSocialValidation.Run','office-social.log',$false),@('TheElevator.Editor.OfficeInteractionValidation.Run','office-interactions.log',$false),@('TheElevator.Editor.OfficeTools.RenderShowcase','office-render.log',$true))
if($BuildPlayer){
    $checks+=,@('TheElevator.Editor.OfficeTools.BuildPlayer','office-build.log',$true)
    $checks+=,@('TheElevator.Editor.ProjectTools.BuildWindows','windows-build.log',$true)
}
foreach($check in $checks){
    $logPath=Join-Path $validationRoot $check[1]
    $unityArgs=@('-batchmode','-force-d3d11','-projectPath',('"'+$validationRoot+'"'),'-executeMethod',$check[0],'-logFile',('"'+$logPath+'"'))
    if($check[2]){$unityArgs+='-quit'}
    $validationProcess=Start-Process -FilePath $UnityEditor -ArgumentList $unityArgs -PassThru -WindowStyle Hidden
    if(-not $validationProcess.WaitForExit(240000)){$validationProcess.Kill();throw "Validation timed out: $logPath"}
    Select-String -LiteralPath $logPath -Pattern 'CABIN PASS|OFFICE .*PASS|OFFICE SHOWCASE|error CS|Shader error' | ForEach-Object Line
    if($validationProcess.ExitCode -ne 0){throw "Office validation failed: $logPath"}
}
$output=Join-Path $projectRoot 'TestResults\Office'
New-Item -ItemType Directory -Path $output -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $validationRoot 'TestResults\Office') -File | Copy-Item -Destination $output -Force
if($BuildPlayer){
    $buildRoot=Join-Path $projectRoot 'Builds'
    New-Item -ItemType Directory -Path $buildRoot -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $validationRoot 'Builds\Office') -Destination $buildRoot -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $validationRoot 'Builds\Windows') -Destination $buildRoot -Recurse -Force
}
Write-Output 'PASS: office validation. Evidence is in TestResults/Office.'





