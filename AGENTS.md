# SupportAI USB — reglas del proyecto

## Stack
- .NET 10, C#
- WPF (UI moderna)
- PowerShell vía `System.Diagnostics.Process`
- JSON con `System.Text.Json`
- QuestPDF para informes PDF
- xUnit + coverlet

## Proyectos
- `SupportAI.Core`: modelos, motor de reglas de diagnóstico (7 módulos)
- `SupportAI.Collectors.Windows`: colectores WMI/PowerShell (6 secciones)
- `SupportAI.Repairs`: catálogo de 9 reparaciones con dry-run
- `SupportAI.Ia`: proveedores IA (OpenRouter, Gemini, Reglas offline) + PrivacyFilter
- `SupportAI.Cli`: CLI headless con --diagnose --json --repair
- `SupportAI.App.Wpf`: UI WPF con panel de diagnóstico e integración IA
- `SupportAI.Core.Tests`: 16 tests unitarios

## Comandos útiles
```powershell
dotnet build
dotnet test
dotnet run --project src\SupportAI.Cli -- --diagnose --json
dotnet run --project src\SupportAI.Cli -- --list-repairs
dotnet run --project src\SupportAI.Cli -- --repair rep.temp.clean --dry-run
dotnet run --project src\SupportAI.App.Wpf
```

## API keys (IA online)
Crear archivos `.OPENROUTER_KEY` y `.GEMINI_KEY` junto al .exe.
Sin ellos, funciona offline con reglas locales.

## Fases
- F1: Core + colectores + CLI + 3 reparaciones + tests ✓
- F2: UI WPF + 9 reparaciones + PDF + auto-scan ✓
- F3: Red + Seguridad + Drivers + IA (OpenRouter + Gemini + Reglas) + PrivacyFilter ✓
- F4: Tests ampliados (16) + pack-usb + README ✓
- F5: GGUF opcional + firma + release
