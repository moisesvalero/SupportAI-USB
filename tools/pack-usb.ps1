param(
    [string]$OutputPath = "USB",
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "src\SupportAI.Cli\bin\Release\net10.0-windows\win-x64\publish"

if (-not $SkipBuild) {
    Write-Host "Publicando SupportAI single-file..." -ForegroundColor Cyan
    dotnet publish $root\src\SupportAI.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:ReadyToRun=true -p:DebugType=none
    if (-not $?) { throw "Build falló" }
}

$usbDir = Join-Path $root $OutputPath
@("logs","diagnosticos","informes","models") | ForEach-Object {
    New-Item -ItemType Directory -Path (Join-Path $usbDir $_) -Force | Out-Null
}

Copy-Item (Join-Path $publishDir "SupportAI.exe") (Join-Path $usbDir "SupportAI.exe") -Force
Copy-Item (Join-Path $root "tools\sign.ps1") (Join-Path $usbDir "tools") -ErrorAction SilentlyContinue

Write-Host "USB listo en: $usbDir" -ForegroundColor Green
Write-Host "Tamaño: $((Get-Item (Join-Path $usbDir 'SupportAI.exe')).Length / 1MB -as [int]) MB"
Write-Host "Copia los archivos .OPENROUTER_KEY y .GEMINI_KEY junto al .exe para activar IA online."
