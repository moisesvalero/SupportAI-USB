param(
    [string]$OutputPath = "USB",
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$cliPublishDir = Join-Path $root "src\SupportAI.Cli\bin\Release\net10.0-windows\win-x64\publish"
$wpfPublishDir = Join-Path $root "src\SupportAI.App.Wpf\bin\Release\net10.0-windows\win-x64\publish"

if (-not $SkipBuild) {
    Write-Host "Publicando SupportAI CLI..." -ForegroundColor Cyan
    dotnet publish $root\src\SupportAI.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:ReadyToRun=true -p:DebugType=none
    if (-not $?) { throw "Build de la CLI falló" }

    Write-Host "Publicando SupportAI WPF (GUI)..." -ForegroundColor Cyan
    dotnet publish $root\src\SupportAI.App.Wpf -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:ReadyToRun=true -p:DebugType=none
    if (-not $?) { throw "Build de la GUI falló" }
}

$usbDir = Join-Path $root $OutputPath

# Limpiar directorio de salida anterior para evitar archivos basura
if (Test-Path $usbDir) {
    Remove-Item $usbDir -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
}
New-Item -ItemType Directory -Path $usbDir -Force | Out-Null

@("logs","diagnosticos","informes","models") | ForEach-Object {
    New-Item -ItemType Directory -Path (Join-Path $usbDir $_) -Force | Out-Null
}

# Copiar todo el contenido de WPF que incluye runtime y dependencias
Write-Host "Copiando dependencias y ejecutable GUI..." -ForegroundColor Cyan
Copy-Item -Path "$wpfPublishDir\*" -Destination $usbDir -Recurse -Force

# Renombrar el ejecutable de WPF en el destino a SupportAI.exe
Rename-Item -Path (Join-Path $usbDir "SupportAI.App.Wpf.exe") -NewName "SupportAI.exe" -Force

# Copiar el ejecutable de la CLI al destino
Write-Host "Copiando ejecutable CLI..." -ForegroundColor Cyan
Copy-Item -Path (Join-Path $cliPublishDir "SupportAI.exe") -Destination (Join-Path $usbDir "SupportAI_CLI.exe") -Force

Copy-Item (Join-Path $root "tools\sign.ps1") (Join-Path $usbDir "tools") -ErrorAction SilentlyContinue

Write-Host "USB listo en: $usbDir" -ForegroundColor Green
Write-Host "Copia los archivos .OPENROUTER_KEY y .GEMINI_KEY junto al .exe para activar IA online."
