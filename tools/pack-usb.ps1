param(
    [string]$OutputPath = "USB",
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$cliPublishDir = Join-Path $root "src\SupportAI.Cli\bin\Release\net10.0-windows\win-x64\publish"
$wpfPublishDir = Join-Path $root "src\SupportAI.App.Wpf\bin\Release\net10.0-windows\win-x64\publish"

if (-not $SkipBuild) {
    Write-Host "Publicando SupportAI CLI (Línea de comandos)..." -ForegroundColor Cyan
    dotnet publish $root\src\SupportAI.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:ReadyToRun=true -p:DebugType=none
    if (-not $?) { throw "Build de la CLI falló" }

    Write-Host "Publicando SupportAI WPF (Interfaz Gráfica)..." -ForegroundColor Cyan
    dotnet publish $root\src\SupportAI.App.Wpf -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:ReadyToRun=true -p:DebugType=none
    if (-not $?) { throw "Build de la GUI falló" }
}

$usbDir = Join-Path $root $OutputPath
@("logs","diagnosticos","informes","models") | ForEach-Object {
    New-Item -ItemType Directory -Path (Join-Path $usbDir $_) -Force | Out-Null
}

Copy-Item (Join-Path $cliPublishDir "SupportAI.exe") (Join-Path $usbDir "SupportAI_CLI.exe") -Force
Copy-Item (Join-Path $wpfPublishDir "SupportAI.App.Wpf.exe") (Join-Path $usbDir "SupportAI.exe") -Force
Copy-Item (Join-Path $root "tools\sign.ps1") (Join-Path $usbDir "tools") -ErrorAction SilentlyContinue

Write-Host "USB listo en: $usbDir" -ForegroundColor Green
Write-Host "Copia los archivos .OPENROUTER_KEY y .GEMINI_KEY junto al .exe para activar IA online."
