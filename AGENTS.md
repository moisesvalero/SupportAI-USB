# SupportAI USB — reglas del proyecto

## Stack
- .NET 10, C#
- WPF (en fases posteriores)
- PowerShell vía `System.Diagnostics.Process`
- JSON con `System.Text.Json`
- xUnit + coverlet

## Proyectos
- `SupportAI.Core`: modelos, reglas del motor de diagnóstico, contratos
- `SupportAI.Collectors.Windows`: colectores WMI/PowerShell
- `SupportAI.Repairs`: catálogo de reparaciones
- `SupportAI.Cli`: punto de entrada consola
- `SupportAI.Core.Tests`: tests unitarios del motor de diagnóstico

## Comandos útiles
```powershell
dotnet build
dotnet test --reporter=dot
dotnet run --project src\SupportAI.Cli -- --diagnose --json
dotnet run --project src\SupportAI.Cli -- --list-repairs
dotnet run --project src\SupportAI.Cli -- --repair rep.temp.clean --dry-run
```

## Convenciones
- Nombres en español para modelos/UI, inglés para código técnico (interfaces, services)
- `DiagnosticEngine.Analyze` contiene reglas puras, sin dependencias externas
- Los colectores producen JSON → el analizador consume modelos tipados
- `IRepairAction` para acciones de reparación, con `dryRun` siempre disponible

## Fases
- F1: Core + colectores + CLI + 3 reparaciones + tests
- F2: UI WPF + informe PDF + más reparaciones
- F3: Red + Seguridad + Drivers + reglas offline completas + IA OpenRouter
- F4: Gemini fallback + GGUF opcional + anonimización + firma + pack USB
- F5: Tests ampliados, docs, release
