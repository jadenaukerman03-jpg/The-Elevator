param([string]$RoslynDirectory = $PSHOME)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
foreach ($assembly in @('Microsoft.CodeAnalysis.dll', 'Microsoft.CodeAnalysis.CSharp.dll')) {
    $path = Join-Path $RoslynDirectory $assembly
    if (-not (Test-Path -LiteralPath $path)) { throw 'Roslyn not found. Use PowerShell 7 or supply -RoslynDirectory.' }
    Add-Type -Path $path
}
$files = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Recurse -Filter '*.cs'
$errorsFound = 0
foreach ($file in $files) {
    $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([System.IO.File]::ReadAllText($file.FullName))
    foreach ($diagnostic in $tree.GetDiagnostics()) {
        if ($diagnostic.Severity.ToString() -eq 'Error') {
            Write-Output ($file.Name + ': ' + $diagnostic.ToString())
            $errorsFound++
        }
    }
}
if ($errorsFound -gt 0) { throw "$errorsFound C# syntax errors." }
Write-Output "PASS: $($files.Count) C# files parsed with no syntax errors. This does not verify Unity API compatibility or runtime behavior."
