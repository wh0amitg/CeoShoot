# CEOSHOOT 📸

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-lightgray.svg?logo=windows)]()
[![Framework: .NET](https://img.shields.io/badge/Framework-.NET%20Framework%204.8+-purple.svg)]()

A lightweight, high-performance screen capture utility built in C# using WinForms and native Win32 APIs. Deployed straight to the system tray, it provides instant region capturing, on-the-fly annotations, and zero-leak memory management.

<p align="center">
  <img src="https://raw.githubusercontent.com/YOUR_USERNAME/CeoShoot/main/Assets/logo.png" alt="CeoShoot Icon" width="128" height="128">
</p>

---

## 🔥 Key Features

* **🥷 Invisible Background Agent** – Runs quietly in the system tray. Completely stripped from the Windows Taskbar and the `Alt+Tab` switcher menu.
* **⚡ Ultra-Fast Region Selection** – Instant desktop dimming overlay that captures screens cleanly without rendering artifacts.
* **✏️ On-the-Fly Annotations** – Draw lines or add rich, draggable text layers directly onto your selected screenshot before saving.
* **📈 Real-Time Resolution Feed** – Live pixel dimensions dashboard displayed directly above your bounding box.
* **⚙️ Registry Autostart Integration** – Seamless background persistence that initializes automatically with Windows on user logon.
* **🧼 Zero GDI+ Memory Leaks** – Re-architected handle disposal to ensure Win32 native icon handles (`Hicon`) are released properly under continuous runtime use.

---

## ⌨️ Global Shortcuts

While the application is active in your tray, use these rapid hotkeys to control your workflow:

| Key Binding | Action Context | Description |
| :--- | :--- | :--- |
| **`PrintScreen`** | Global (Anywhere) | Launches the interactive desktop screenshot selection screen. |
| **`Ctrl + C`** | Inside Selection Form | Copies the cropped image with all your drawings directly to the Clipboard. |
| **`Ctrl + S`** | Inside Selection Form | Launches a Save Dialog to export the image as a `.png` or `.jpg`. |
| **`Escape`** | Inside Selection Form | Instantly aborts the selection session and drops back to background standby. |

---

## 🚀 Getting Started & Setup

### Prerequisites
* Windows OS 10 / 11
* .NET Framework 4.8 or newer
* IDE of choice (Visual Studio, Rider) or MSBuild compiler

### Architecture Overview
The project is decoupled into three primary modules for modular deployment:
1. `Program.cs` – Initial application booting logic and user runkey registry adjustments.
2. `BackgroundControllerForm.cs` – Headless controller that hooks global input listeners and renders dynamic icons.
3. `SelectionForm.cs` – Double-buffered window overlay handling user drawings, text rendering, and image scaling logic.

### Building from Source
Clone the repository and build using your preferred C# toolchain:
```bash
git clone [https://github.com/YOUR_USERNAME/CeoShoot.git](https://github.com/YOUR_USERNAME/CeoShoot.git)
cd CeoShoot
dotnet build --configuration Release
