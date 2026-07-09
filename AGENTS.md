# SupportAI USB — reglas del proyecto

## Stack
- .NET 10, C#
- WPF (UI moderna, flat design)
- PowerShell vía `System.Diagnostics.Process`
- JSON con `System.Text.Json`
- QuestPDF para informes PDF
- xUnit + coverlet

## Proyectos
- `SupportAI.Core`: modelos, reglas del motor de diagnóstico, contratos
- `SupportAI.Collectors.Windows`: colectores WMI/PowerShell
- `SupportAI.Repairs`: catálogo de reparaciones (9 acciones con dry-run)
- `SupportAI.Cli`: punto de entrada consola
- `SupportAI.App.Wpf`: interfaz gráfica WPF
- `SupportAI.Core.Tests`: tests unitarios del motor de diagnóstico

## Comandos útiles
```powershell
dotnet build                          # Compilar todo
dotnet test --reporter=dot            # Tests
dotnet run --project src\SupportAI.Cli -- --diagnose --json
dotnet run --project src\SupportAI.Cli -- --list-repairs
dotnet run --project src\SupportAI.Cli -- --repair rep.temp.clean --dry-run
dotnet run --project src\SupportAI.App.Wpf  # Lanzar UI
```

## Reparaciones disponibles
| ID | Acción |
|---|---|
| rep.dns.flush | Limpiar caché DNS |
| rep.temp.clean | Limpiar archivos temporales |
| rep.explorer.restart | Reiniciar Explorer |
| rep.sfc | Reparar archivos del sistema (sfc) |
| rep.dism.health | Restaurar imagen de Windows (DISM) |
| rep.winsock.reset | Resetear Winsock |
| rep.spooler.reset | Reiniciar servicio de impresión |
| rep.wu.reset | Reiniciar Windows Update |
| rep.restore.point | Crear punto de restauración |

## Convenciones
- Nombres en español para modelos/UI, inglés para código técnico (interfaces, services)
- `DiagnosticEngine.Analyze` contiene reglas puras, sin dependencias externas
- Los colectores producen JSON → el analizador consume modelos tipados
- `IRepairAction` para acciones de reparación, con `dryRun` siempre disponible
- `CommandRepair` clase base para reparaciones que solo ejecutan un comando

## Fases
- F1: Core + colectores + CLI + 3 reparaciones + tests (✓ completada)
- F2: UI WPF + 9 reparaciones + PDF + auto-scan al inicio (✓ completada)
- F3: Red + Seguridad + Drivers + reglas offline completas + IA OpenRouter
- F4: Gemini fallback + GGUF opcional + anonimización + firma + pack USB
- F5: Tests ampliados, docs, release
