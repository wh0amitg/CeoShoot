# CEOSHOOT 📸

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-lightgray.svg?logo=windows)]()
[![Framework: .NET](https://img.shields.io/badge/Framework-.NET%20Framework%204.7.2-purple.svg)]()

A lightweight, high-performance screen capture utility built in C# using .NET Framework 4.7.2 and native Win32 APIs. Deployed straight to the system tray, it provides instant region capturing, on-the-fly annotations, and real-time desktop cropping.

<p align="center">
  <img src="https://i.ibb.co/Rp9rH9w3/image-2026-05-21-154140697-Photoroom.jpg" alt="CeoShoot Icon" width="140" height="140" style="border-radius: 20px;">
</p>

---

## 🔥 Key Features

* **🥷 Invisible Background Agent** – Runs quietly in the system tray. Completely stripped from the Windows Taskbar and the `Alt+Tab` switcher menu.
* **⚡ Ultra-Fast Region Selection** – Instant desktop dimming overlay that captures your screen seamlessly without rendering lag.
* **✏️ On-the-Fly Annotations** – Draw custom lines or add rich text layers directly onto your selected screenshot before saving.
* **📈 Live Resolution Feed** – Real-time pixel dimensions dashboard displayed instantly above your current bounding box.
* **⚙️ Registry Autostart Integration** – Implements background persistence that initializes automatically with Windows on user logon.
* **🎨 Dedicated Asset Loading** – Optimally loads compiled custom resource assets directly into system icons without GDI+ memory exhaustion.

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
* .NET Framework 4.7.2
* IDE of choice (Visual Studio, Rider)

### Architecture Overview
The project is decoupled into three clean, primary modules:
1. `Program.cs` – Initial application booting logic and user run
