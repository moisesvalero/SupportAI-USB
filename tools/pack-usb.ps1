param(
    [string]$OutputPath = "USB",
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "src\SupportAI.Cli\bin\Release\net10.0\win-x64\publish"

if (-not $SkipBuild) {
    Write-Host "Publicando SupportAI single-file..." -ForegroundColor Cyan
    & "$root\..\..\..\Program Files\dotnet\dotnet.exe" publish $root\src\SupportAI.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:ReadyToRun=true -p:DebugType=none
    if (-not $?) { throw "Build falló" }
}

$usbDir = Join-Path $root $OutputPath
New-Item -ItemType Directory -Path $usbDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $usbDir "logs") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $usbDir "diagnosticos") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $usbDir "informes") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $usbDir "models") -Force | Out-Null

Copy-Item (Join-Path $publishDir "SupportAI.exe") (Join-Path $usbDir "SupportAI.exe") -Force
Copy-Item (Join-Path $root "tools\sign.ps1") (Join-Path $usbDir "tools") -ErrorAction SilentlyContinue

Write-Host "USB listo en: $usbDir" -ForegroundColor Green
Write-Host "Tamaño: $((Get-Item (Join-Path $usbDir 'SupportAI.exe')).Length / 1MB -as [int]) MB"
