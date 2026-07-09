# 🛠️ SupportAI USB

<p align="center">
  <a href="#english">English</a> • <a href="#español">Español</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=.net&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/WPF-Modern%20UI-orange?style=for-the-badge&logo=windows&logoColor=white" alt="WPF UI" />
  <img src="https://img.shields.io/badge/Platform-Windows-0078D7?style=for-the-badge&logo=windows&logoColor=white" alt="Windows" />
  <img src="https://img.shields.io/badge/License-MIT-4caf50?style=for-the-badge" alt="License MIT" />
</p>

---

<a name="english"></a>
## 🇬🇧 English Version

SupportAI USB is a **portable Windows diagnostic and automated repair assistant** powered by local and cloud-based AI. Designed to run seamlessly from a USB flash drive, it collects hardware metrics, monitors system events, analyzes health telemetry, and provides one-click executable repairs.

### ✨ Key Features

* **⚡ Ultra-fast Telemetry:** Instantly reads CPU, RAM, GPU, storage health, logical drives, active network interfaces, and critical Windows event logs.
* **🧠 Hybrid AI Engine:** 
  * **Online (Primary):** OpenRouter (API key) with automatic failover to Google Gemini.
  * **Local Offline:** Llama.cpp GGUF local model execution.
  * **Fallback Rules:** Offline static rules engine to guarantee operation under any conditions.
* **🛡️ Built-in Privacy Filter:** Automatically redacts usernames, machine names, domains, serial numbers, IP/DNS addresses, and profile paths before sending data to AI endpoints.
* **🧰 One-Click Repairs:** Instantly trigger PowerShell/CMD automation scripts for common system, network, and registry issues.
* **📦 Portable Single-File Build:** Package everything into a standalone executable ready for your IT support USB drive.

### 🚀 Quick Start

#### 🖥️ Run WPF Desktop Application
Launch the modern WPF desktop dashboard to run automated scans and execute repairs visually:
```powershell
dotnet run --project src\SupportAI.App.Wpf
```

#### 📟 Run Command-Line Interface (CLI)
SupportAI USB features a powerful headless CLI for automated IT scripts:
```powershell
# Perform full system diagnosis and save JSON output
dotnet run --project src\SupportAI.Cli -- --diagnose --json --out diag.json

# List all available system repair actions
dotnet run --project src\SupportAI.Cli -- --list-repairs

# Execute a specific repair in dry-run mode (simulation)
dotnet run --project src\SupportAI.Cli -- --repair rep.temp.clean --dry-run

# Run a repair action with administrative privileges
dotnet run --project src\SupportAI.Cli -- --repair rep.dns.flush
```

### 🔑 AI Cloud Configuration (Optional)

To enable cloud-based AI analysis (OpenRouter or Gemini), place your API key text files adjacent to the compiled executable (`SupportAI.exe`):

* `.OPENROUTER_KEY` — API key from [openrouter.ai](https://openrouter.ai) *(Recommended, utilizes high-quality free models)*
* `.GEMINI_KEY` — API key from [Google AI Studio](https://aistudio.google.com) *(Fallback)*

*If no key files are present, SupportAI USB runs safely in **Offline Mode** utilizing local diagnostic rules.*

### 🔧 Automated Repair Catalog

SupportAI USB includes 9 built-in administrative repair tools:

| ID | Repair Action | Scope & Description |
|---|---|---|
| `rep.dns.flush` | **Flush DNS Cache** | Clears the local DNS cache resolver. |
| `rep.temp.clean` | **Clean Temp Files** | Safely deletes aging cache files in `%TEMP%` and `C:\Windows\Temp`. |
| `rep.explorer.restart` | **Restart Explorer** | Restarts the Windows shell (`explorer.exe`) to resolve interface hangs. |
| `rep.sfc` | **SFC System Scan** | Executes `sfc /scannow` to repair corrupted Windows system files. |
| `rep.dism.health` | **DISM Health Restore** | Restores local Windows image files via online Component Store health checks. |
| `rep.winsock.reset` | **Reset Winsock** | Resets local network interface adapters to original default state. |
| `rep.spooler.reset` | **Reset Print Spooler** | Restarts print services and purges stalled print queues. |
| `rep.wu.reset` | **Reset Windows Update** | Restarts update services and resets local cache databases. |
| `rep.restore.point` | **Create Restore Point** | Creates a System Restore Point before applying modifications. |

### 📦 Creating a Standalone USB Build

To package SupportAI USB into a portable, single-file release suitable for USB drives:

1. **Connect your USB drive** to your computer.
2. **Open PowerShell** in the root of the project.
3. **Execute the pack script** to build the single-file release:
   ```powershell
   .\tools\pack-usb.ps1
   ```
4. **Copy all contents** of the generated `USB/` directory directly to the root of your USB drive.
5. **Add API keys (Optional):** Copy your `.OPENROUTER_KEY` and `.GEMINI_KEY` files to the root of the USB drive, adjacent to `SupportAI.exe`, to enable cloud-based AI.

---

<a name="español"></a>
## 🇪🇸 Versión en Español

SupportAI USB es un **asistente portátil de diagnóstico y reparación automática de Windows** impulsado por IA local y en la nube. Diseñado para ejecutarse sin problemas desde una unidad flash USB, recopila métricas de hardware, monitorea eventos del sistema, analiza la telemetría de salud y proporciona reparaciones ejecutables con un solo clic.

### ✨ Características Clave

* **⚡ Telemetría ultrarrápida:** Lee instantáneamente CPU, RAM, GPU, estado del almacenamiento, unidades lógicas, interfaces de red activas y registros de eventos críticos de Windows.
* **🧠 Motor de IA híbrido:**
  * **Online (Principal):** OpenRouter (API key) con conmutación por error automática a Google Gemini.
  * **Local Offline:** Ejecución del modelo local GGUF de Llama.cpp.
  * **Reglas de respaldo:** Motor de reglas estáticas offline para garantizar el funcionamiento bajo cualquier condición.
* **🛡️ Filtro de privacidad integrado:** Redacta automáticamente nombres de usuario, nombres de equipo, dominios, números de serie, direcciones IP/DNS y rutas de perfiles antes de enviar datos a la IA.
* **🧰 Reparaciones en un clic:** Ejecuta al instante scripts de automatización de PowerShell/CMD para problemas comunes de sistema, red y registro.
* **📦 Compilación portátil en un solo archivo:** Empaqueta todo en un ejecutable independiente listo para tu pendrive de soporte técnico.

### 🚀 Inicio Rápido

#### 🖥️ Ejecutar la aplicación de escritorio (WPF)
Inicia el panel moderno de escritorio WPF para realizar análisis y ejecutar reparaciones visualmente:
```powershell
dotnet run --project src\SupportAI.App.Wpf
```

#### 📟 Ejecutar la interfaz de línea de comandos (CLI)
SupportAI USB cuenta con una potente CLI sin interfaz ideal para scripts de automatización:
```powershell
# Realizar un diagnóstico completo del sistema y guardar la salida en JSON
dotnet run --project src\SupportAI.Cli -- --diagnose --json --out diag.json

# Listar todas las acciones de reparación disponibles en el sistema
dotnet run --project src\SupportAI.Cli -- --list-repairs

# Ejecutar una reparación específica en modo de simulación (dry-run)
dotnet run --project src\SupportAI.Cli -- --repair rep.temp.clean --dry-run

# Ejecutar una acción de reparación con privilegios de administrador
dotnet run --project src\SupportAI.Cli -- --repair rep.dns.flush
```

### 🔑 Configuración de IA en la Nube (Opcional)

Para habilitar el análisis de IA basado en la nube (OpenRouter o Gemini), coloca tus archivos de texto con las API keys junto al ejecutable compilado (`SupportAI.exe`):

* `.OPENROUTER_KEY` — API key de [openrouter.ai](https://openrouter.ai) *(Recomendado, utiliza modelos gratuitos de alta calidad)*
* `.GEMINI_KEY` — API key de [Google AI Studio](https://aistudio.google.com) *(Respaldo)*

*Si no hay archivos de claves presentes, SupportAI USB se ejecutará de forma segura en **Modo Offline** utilizando las reglas de diagnóstico locales.*

### 🔧 Catálogo de Reparaciones Automáticas

SupportAI USB incluye 9 herramientas de reparación administrativa integradas:

| ID | Acción de Reparación | Alcance y Descripción |
|---|---|---|
| `rep.dns.flush` | **Limpiar Caché DNS** | Vacía el caché del resolver DNS local. |
| `rep.temp.clean` | **Limpiar Archivos Temporales** | Elimina de forma segura archivos temporales en `%TEMP%` y `C:\Windows\Temp`. |
| `rep.explorer.restart` | **Reiniciar Explorer** | Reinicia el shell de Windows (`explorer.exe`) para resolver bloqueos de interfaz. |
| `rep.sfc` | **Escaneo de Sistema SFC** | Ejecuta `sfc /scannow` para reparar archivos corruptos del sistema de Windows. |
| `rep.dism.health` | **Restaurar Salud DISM** | Restaura archivos de imagen locales de Windows mediante comprobaciones en línea. |
| `rep.winsock.reset` | **Restablecer Winsock** | Restablece los adaptadores de red locales a su estado predeterminado. |
| `rep.spooler.reset` | **Reiniciar Cola de Impresión** | Reinicia los servicios de impresión y purga colas de impresión atascadas. |
| `rep.wu.reset` | **Restablecer Windows Update** | Reinicia los servicios de actualización y restablece las bases de datos locales. |
| `rep.restore.point` | **Crear Punto de Restauración** | Crea un Punto de Restauración del Sistema antes de aplicar modificaciones. |

### 📦 Crear una Compilación Independiente para USB

Para empaquetar SupportAI USB en una estructura de carpetas portátil de un solo archivo apta para pendrives:

1. **Conecta tu pendrive USB** al ordenador.
2. **Abre PowerShell** en la raíz del proyecto local.
3. **Ejecuta el script de empaquetado** para compilar la versión optimizada:
   ```powershell
   .\tools\pack-usb.ps1
   ```
4. **Copia el contenido** de la carpeta `USB/` generada directamente a la raíz de tu pendrive.
5. **Añade las API keys (Opcional):** Copia tus archivos `.OPENROUTER_KEY` y `.GEMINI_KEY` a la raíz del pendrive, justo al lado de `SupportAI.exe`, para activar el diagnóstico por IA.

---

## 📁 Repository Structure / Estructura del Repositorio

```
src/
├── SupportAI.Core/                 # Core telemetry models & static rules engine
├── SupportAI.Collectors.Windows/   # PowerShell & WMI telemetry collectors
├── SupportAI.Repairs/              # Catalog of administrative system repairs
├── SupportAI.Ia/                   # AI Provider integrations (OpenRouter, Gemini, GGUF)
├── SupportAI.Cli/                  # Standalone CLI application
└── SupportAI.App.Wpf/              # Desktop WPF User Interface
tests/
└── SupportAI.Core.Tests/           # Comprehensive Unit Tests (xUnit)
```
