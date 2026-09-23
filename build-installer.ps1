param([string]$Compiler='ISCC.exe')
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
& $Compiler (Join-Path $PSScriptRoot 'PnyColor.Setup.iss')
if($LASTEXITCODE -ne 0){throw 'Falha ao criar instalador.'}
