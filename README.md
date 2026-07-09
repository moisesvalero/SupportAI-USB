# SupportAI USB

Asistente portátil de diagnóstico y reparación para Windows con IA.

Conecta un pendrive, ejecuta y obtén diagnóstico completo, causas probables y reparaciones con un clic.

## Stack

- .NET 10, C#
- WPF (UI moderna)
- PowerShell/WMI (colectores de sistema)
- QuestPDF (informes PDF)
- IA: OpenRouter (primario) → Gemini (fallback) → Reglas offline (siempre)

## Uso

```powershell
# CLI
dotnet run --project src\SupportAI.Cli -- --diagnose --json --out diag.json
dotnet run --project src\SupportAI.Cli -- --list-repairs
dotnet run --project src\SupportAI.Cli -- --repair rep.temp.clean --dry-run

# UI WPF
dotnet run --project src\SupportAI.App.Wpf
```

## IA online (opcional)

Para usar OpenRouter o Gemini, crea un archivo junto al .exe:

- `.OPENROUTER_KEY` — API key de [openrouter.ai](https://openrouter.ai) (free tier)
- `.GEMINI_KEY` — API key de [Google AI Studio](https://aistudio.google.com) (fallback)

Sin estos archivos, la app funciona en modo offline con reglas locales.

## Reparaciones disponibles (9)

| ID | Acción |
|---|---|
| `rep.dns.flush` | Limpiar caché DNS |
| `rep.temp.clean` | Limpiar archivos temporales |
| `rep.explorer.restart` | Reiniciar Explorer |
| `rep.sfc` | Reparar archivos del sistema |
| `rep.dism.health` | Restaurar imagen de Windows |
| `rep.winsock.reset` | Resetear Winsock |
| `rep.spooler.reset` | Reiniciar servicio de impresión |
| `rep.wu.reset` | Reiniciar Windows Update |
| `rep.restore.point` | Crear punto de restauración |

## Estructura

```
src/
├── SupportAI.Core/              Modelos y motor de reglas
├── SupportAI.Collectors.Windows/ Colectores PowerShell/WMI
├── SupportAI.Repairs/           Catálogo de reparaciones
├── SupportAI.Ia/                Proveedores IA (OpenRouter, Gemini, Reglas)
├── SupportAI.Cli/               CLI portátil
└── SupportAI.App.Wpf/           UI WPF
```

## Empaquetar para USB

```powershell
tools\pack-usb.ps1
```

Genera una carpeta `USB/` con el .exe single-file listo para copiar a un pendrive.
