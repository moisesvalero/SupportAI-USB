# 🛠️ SupportAI USB

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=.net&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/WPF-Modern%20UI-orange?style=for-the-badge&logo=windows&logoColor=white" alt="WPF UI" />
  <img src="https://img.shields.io/badge/Platform-Windows-0078D7?style=for-the-badge&logo=windows&logoColor=white" alt="Windows" />
  <img src="https://img.shields.io/badge/License-MIT-4caf50?style=for-the-badge" alt="License MIT" />
</p>

SupportAI USB is a **portable Windows diagnostic and automated repair assistant** powered by local and cloud-based AI. Designed to run seamlessly from a USB flash drive, it collects hardware metrics, monitors system events, analyzes health telemetry, and provides one-click executable repairs.

---

## ✨ Key Features

* **⚡ Ultra-fast Telemetry:** Instantly reads CPU, RAM, GPU, storage health, logical drives, active network interfaces, and critical Windows event logs.
* **🧠 Hybrid AI Engine:** 
  * **Online (Primary):** OpenRouter (API key) with automatic failover to Google Gemini.
  * **Local Offline:** Llama.cpp GGUF local model execution.
  * **Fallback Rules:** Offline static rules engine to guarantee operation under any conditions.
* **🛡️ Built-in Privacy Filter:** Automatically redacts usernames, machine names, domains, serial numbers, IP/DNS addresses, and profile paths before sending data to AI endpoints.
* **🧰 One-Click Repairs:** Instantly trigger PowerShell/CMD automation scripts for common system, network, and registry issues.
* **📦 Portable Single-File Build:** Package everything into a standalone executable ready for your IT support USB drive.

---

## 🚀 Quick Start

### 🖥️ Run WPF Desktop Application
Launch the modern WPF desktop dashboard to run automated scans and execute repairs visually:
```powershell
dotnet run --project src\SupportAI.App.Wpf
```

### 📟 Run Command-Line Interface (CLI)
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

---

## 🔑 AI Cloud Configuration (Optional)

To enable cloud-based AI analysis (OpenRouter or Gemini), place your API key text files adjacent to the compiled executable (`SupportAI.exe`):

* `.OPENROUTER_KEY` — API key from [openrouter.ai](https://openrouter.ai) *(Recommended, utilizes high-quality free models)*
* `.GEMINI_KEY` — API key from [Google AI Studio](https://aistudio.google.com) *(Fallback)*

*If no key files are present, SupportAI USB runs safely in **Offline Mode** utilizing local diagnostic rules.*

---

## 🔧 Automated Repair Catalog

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

---

## 📁 Repository Structure

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

---

## 📦 Creating a Standalone USB Build

To package SupportAI USB into a portable, single-file folder structure suitable for USB drives, execute the pack script:

```powershell
tools\pack-usb.ps1
```

This generates a standalone `USB/` directory containing a single-file executable `SupportAI.exe` with all necessary assets.
