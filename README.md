# Stadia Bridge

<p align="center">
  <a href="#english"><b>English</b></a> &nbsp;|&nbsp; <a href="#español"><b>Español</b></a>
</p>

<p align="center">
  <img src="docs/screenshots/stadia-bridge-neon-dark.png" alt="Stadia Bridge - Neon Dark" width="49%" />
  <img src="docs/screenshots/stadia-bridge-classic-dark.png" alt="Stadia Bridge - Classic Xbox" width="49%" />
</p>
<p align="center">
  <img src="docs/screenshots/stadia-bridge-neon-light.png" alt="Stadia Bridge - Neon Light" width="49%" />
  <img src="docs/screenshots/stadia-bridge-classic-light.png" alt="Stadia Bridge - Classic Light" width="49%" />
</p>

---

<a name="english"></a>
## English

Free, standalone Windows x64 application that turns your **Google Stadia USB controller** into a **virtual Xbox 360 controller compatible with XInput**. Features an interactive real-time visual dashboard, Neon and Classic Xbox themes, Dark and Light modes, deadzone adjustments, and dual-motor USB rumble.

Independent project developed and maintained by [AaronRouge](https://github.com/AaronRouge). Powered by [ViGEmClient](https://github.com/nefarius/ViGEmClient) / [ViGEmBus](https://github.com/nefarius/ViGEmBus) virtual emulation and inspired by the protocol research of [71/stadiacontroller](https://github.com/71/stadiacontroller). Full credits, licenses, and architecture details are documented in [Funcionamiento y créditos](FUNCIONAMIENTO-Y-CREDITOS.md).

### 🚀 Download & Installation

The latest official release is **0.7.2**:
- **Installer**: [`StadiaBridge-Setup-0.7.2-x64.exe`](https://github.com/AaronRouge/StadiaBridge-2026/releases/tag/v0.7.2) *(includes .NET 8 runtime & official ViGEmBus 1.22.0 driver; 100% offline installation).*
- **Integrity**: Verified with SHA-256 checksum in `StadiaBridge-Setup-0.7.2-x64.sha256`.

1. Run the installer and accept Windows Administrator elevation.
2. Restart your computer if prompted by the driver installer.
3. Open **Stadia Bridge** and connect your Stadia controller with a USB data cable. Auto-detection starts immediately.
4. Press buttons and move analog sticks to check the dashboard. When ViGEmBus is running, a virtual Xbox 360 controller is created automatically.
5. Use **Heavy**, **Light**, **Burst**, or **Pulse** on the vibration card to test dual-motor haptics. Adjust **Vibration (%)** or use 0% to mute.

### 🎮 Features & Specifications
- **Analog inputs**: Buttons, D-pad, thumbsticks, and analog triggers with customizable 0–30% deadzone per axis.
- **Real-time dashboard**: Illuminated buttons, movable sticks, raw physical coordinate graphs, and analog trigger gauges.
- **Customization**: Neon & Classic Xbox styles, Light & Dark themes, persistent user preferences.
- **Dual-motor rumble**: Four USB test patterns and transparent passthrough from PC games.
- **Zero latency & high reliability**: Direct native Windows HID polling (`hid.dll`) without cloud services or extra bloat.

### 🛠️ Build from Source
Requires Windows x64, .NET 8 SDK, and PowerShell:
```powershell
.\build.ps1
```
Compiles and generates the portable package in `dist/StadiaBridge/`.

---

<a name="español"></a>
## Español

<details open>
<summary><b>Haz clic aquí para contraer / expandir el manual en Español</b></summary>
<br>

Aplicación gratuita para Windows x64 que convierte un mando Google Stadia **USB** en un mando **Xbox 360 virtual compatible con XInput**. Incluye visor interactivo, estilos Neón y Clásico, modo claro y oscuro, zona muerta y vibración USB.

Proyecto independiente desarrollado y mantenido por [AaronRouge](https://github.com/AaronRouge). Con emulación [ViGEmClient](https://github.com/nefarius/ViGEmClient) / [ViGEmBus](https://github.com/nefarius/ViGEmBus) e inspirado en el protocolo de [71/stadiacontroller](https://github.com/71/stadiacontroller). Los créditos completos y licencias se detallan en [Funcionamiento y créditos](FUNCIONAMIENTO-Y-CREDITOS.md).

### 🚀 Instalación y uso

La actualización oficial es **0.7.2**:
- **Descarga del instalador**: [`StadiaBridge-Setup-0.7.2-x64.exe`](https://github.com/AaronRouge/StadiaBridge-2026/releases/tag/v0.7.2), acompañado de su SHA-256. Incluye la aplicación, .NET y el instalador oficial de ViGEmBus; su ejecución no necesita Internet.

1. Abre el instalador y acepta el permiso de administrador de Windows. Incluye la aplicación, .NET y el instalador oficial de ViGEmBus; su ejecución no necesita Internet.
2. Reinicia si el asistente lo indica.
3. Abre Stadia Bridge y conecta el mando con un cable USB que transmita datos. La conexión comienza automáticamente.
4. Pulsa botones y mueve sticks para comprobar el visor. Con ViGEmBus disponible se crea también el mando Xbox virtual.
5. Usa **Fuerte**, **Suave**, **Ráfaga** o **Pulso** en la tarjeta de vibración. Cada prueba termina sola. **Vibración (%)** ajusta la intensidad; 0 % silencia los motores. **Detener** en la tarjeta cancela la prueba y devuelve el control al juego.

La desinstalación conserva ViGEmBus porque puede estar compartido con otras aplicaciones. El instalador propio no está firmado; el paquete oficial de ViGEmBus incluido conserva su firma.

### 🎮 Funciones y límites
- Botones, cruceta, sticks y gatillos analógicos; zona muerta de 0 a 30 % por eje.
- Visor con botones iluminados, sticks móviles, gráficos de ejes y barras de gatillos. Muestra la lectura física antes de aplicar la zona muerta a la salida Xbox.
- Estilos Neón y Clásico · Xbox, modo Claro / Oscuro y preferencias guardadas.
- Vibración de dos motores por USB, cuatro patrones de prueba y prueba independiente de ViGEmBus al detener la sesión de conexión.
- Reconexión USB, una instancia y un mando físico.

La salida es Xbox 360, no un dispositivo Xbox One ni un adaptador para consolas físicas. **Bluetooth todavía no está implementado.** Tampoco se incluyen perfiles, reasignación o configuración de HidHide; algunos juegos podrían registrar tanto el mando físico como el virtual.

### 🛠️ Compilar
Requiere Windows x64, SDK .NET 8 y PowerShell. Desde la raíz:
```powershell
.\build.ps1
```
Ejecuta las comprobaciones y genera la aplicación portable en `dist/StadiaBridge/`.

### 📄 Documentación y licencia
- [Funcionamiento, procedencia y créditos](FUNCIONAMIENTO-Y-CREDITOS.md).
- [Modos de vibración USB](docs/VIBRACION-MODOS.md).
- [Historial de cambios](CHANGELOG.md) y [guía para contribuir](CONTRIBUTING.md).
- [MIT para el código propio](LICENSE), [avisos de terceros](THIRD-PARTY-NOTICES.txt) y [licencias adicionales](licenses/README.md).

Google, Stadia, Xbox y Microsoft son nombres de sus respectivos titulares. No existe afiliación ni patrocinio por parte de ellos ni de los proyectos citados.

</details>
