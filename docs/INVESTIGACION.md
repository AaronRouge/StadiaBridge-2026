# Investigación inicial — 20 de octubre de 2026

## Evidencia observada

- Repositorio `71/stadiacontroller`, revisión consultada `a8e6399515cbf474d4f3ec997014a47cca8d59b3`. El README declara falta de mantenimiento, USB y salida Xbox 360. Se consultaron `stadia.go`, `src/stadia.rs`, `hid.go` y `vigem.go`.
- El código identifica VID/PID `18D1:9400`, reporte de entrada `03`, sticks de 8 bits, dos gatillos analógicos y bits de botones. El código Go y el Rust discrepan en el nombre de los dos bits adicionales. La aplicación conserva esos bits sin atribuirles acciones.
- El encabezado oficial `ViGEmClient/include/ViGEm/Client.h` declara el envío de `XUSB_REPORT` por valor. `Common.h` define el destino Xbox360Wired y la estructura de 12 bytes.
- Se descargó el ZIP de la publicación v1.3.1 sin ejecutar su EXE. SHA-256: `820eb03606a2edc27b8d4b1db862bf7547cf74f7bfe20a0f10dd85f81d919308`.
- REA `open_binary` y `inspect_artifact` inspeccionaron ese ZIP. Evidencia completa retenida: `ev_fe13bc7a4bc5b953b67b403454d900730596be7d0dbe92c3393304a92e5c1a55`; vista resumen: `ev_07f9d209d869c266b70f44a7abf7234b755264053e7b4ee36f59950a2bd0da65`. Inventario: 3 nodos, 3 ocurrencias, 2 relaciones, sin contradicciones de integridad del contenedor registradas. Esto no equivale a validar su ejecución.
- REA `inspect_pe_resources` inspeccionó `ViGEmClient.dll`, SHA-256 `966e579ad8224ca87ae78a8788c5eb0c33ecb932dcc6ca25c39871e20b97a15a`. Evidencia: `ev_fd00fe55f043e4ffd017e8a258c9202831e71a97f12a35d981a24cf4d669d123`. PE32+, máquina AMD64 (`34404`), 314880 bytes, dos recursos. No se realizó decompilación ni se deduce la semántica del cliente de sus recursos.
- La página oficial de ViGEmBus muestra el archivo del repositorio y la publicación 1.22.0. La versión 1.22.0 elimina el actualizador; no se instaló el controlador durante la construcción.

## Decisiones propias

Se implementó una aplicación C# / .NET 8 / WinForms, lectura HID mediante APIs de Windows y cliente nativo ViGEm. No requiere paquetes NuGet adicionales. Se traduce el protocolo documentado en el código de referencia, conservando su atribución. La conversión de ejes usa escala monótona propia y centro 128, con zona muerta por eje. Se crea el mando virtual después del primer reporte compatible y se retira ante desconexión.

## Límites y comprobaciones pendientes

La inspección REA fue estática del contenedor y de los recursos PE. No observó ejecución, tiempos de respuesta, vibración ni compatibilidad en juegos. El video de YouTube no pudo cargarse con el navegador de investigación y no se obtuvo una transcripción; no se le atribuyen detalles técnicos.

No se ha verificado el protocolo de un mando con firmware Bluetooth. Tampoco se oculta el mando físico ni se implementa un controlador de kernel propio. Las comprobaciones automáticas del parser no sustituyen una prueba real del recorrido Stadia → HID → ViGEmBus → juego.

## Validación de esta entrega

- Compilación Release y publicación local correctas, sin advertencias ni errores de compilación.
- 1086 comprobaciones del protocolo aprobadas.
- Se ejecutó nuestro `StadiaBridge.exe --diagnose`, con código de salida 0. Resultado: proceso x64, cero interfaces Stadia USB reconocidas y ViGEmBus no disponible (`0xE0000001`). Se pudo cargar la DLL y consultar el bus, pero no crear ni comprobar un mando virtual con el hardware del usuario.
- No se ejecutó el emulador de terceros, ni se instalaron controladores, ni se cambió la configuración del sistema.

## Ampliación: vibración USB

Se añadió recepción de notificaciones de los dos motores con `vigem_target_x360_register_notification`, conservando vivo el delegado administrado hasta liberar el cliente. La firma de callback se comprobó en el encabezado oficial: cliente, destino, motor grande, motor pequeño, LED y datos de usuario; convención Winapi para el callback y Cdecl para las funciones exportadas.

La escritura usa un handle USB separado del lector, con acceso de escritura y E/S asíncrona. Los callbacks únicamente actualizan las intensidades; una tarea independiente envía el reporte `05, grande, grande, pequeño, pequeño`, rellenado a la longitud de salida anunciada por el descriptor HID. Esa codificación coincide con `src/stadia.rs` del proyecto de referencia. La comprobación automática utiliza una salida simulada: no demuestra que el firmware físico acepte el reporte.

Se implementaron ganancia de 0 a 100 %, silencio en el siguiente ciclo de salida (16 ms nominales), prueba de medio segundo y orden final de apagado al cancelar o fallar. La prueba manual puede abrir únicamente la salida USB, sin crear un mando virtual. No se añadió soporte Bluetooth ni se instalaron dependencias o controladores nuevos.

## Prueba física de vibración USB confirmada

El 20 de octubre de 2026 se detectó un Stadia Controller rev. A con VID/PID `18D1:9400`, una interfaz de juego HID y longitud de reporte de salida de 5 bytes. La prueba `--test-rumble` solicitó un pulso de medio segundo a ambos motores al 50 %, seguido de la orden de apagado. El usuario confirmó que sintió la vibración. Esto valida el envío USB de ese pulso en este mando; todavía no verifica cada motor por separado, el cese físico preciso ni la recepción de vibración desde un juego.

La detección inicial de cero interfaces se debía al contexto de ejecución restringido: al ejecutar el mismo diagnóstico con acceso al dispositivo físico, apareció una interfaz compatible. No era evidencia de un cable defectuoso. ViGEmBus seguía sin estar disponible (`0xE0000001`); la prueba física USB se realizó sin ese bus.

## Instalador todo en uno

Se creó un asistente Inno Setup 7.1.0 en español con la aplicación x64 autónoma, runtimes .NET y Windows Desktop 8.0.32 y el instalador oficial de ViGEmBus 1.22.0 sin modificar. La firma Authenticode del controlador es válida y su SHA-256 coincide con el manifiesto de Microsoft Winget: `89220A7865076B342892F98865F3499FB7C4CFD673159E89D352C360FD014C6A`. Se incluyen las licencias y avisos de los runtimes y del controlador.

El asistente requiere elevación, crea accesos directos y desinstalador y no depende de descargas durante la instalación. Conserva un servicio ViGEmBus existente, comprueba los códigos de instalación y de reinicio y consulta la disponibilidad del bus al terminar. No reinicia automáticamente ni elimina ViGEmBus al desinstalar la aplicación. Los registros de la aplicación pasan a `%LOCALAPPDATA%/StadiaBridge/Logs` para funcionar desde Archivos de programa sin elevación.

La construcción aprobó 1098 comprobaciones del protocolo y de la lógica de vibración. Se verificó la configuración autónoma con `includedFrameworks` y los runtimes locales empaquetados. No se ha probado todavía una instalación completa en Windows limpio ni se ha instalado ViGEmBus en el equipo durante esta preparación. El instalador de Stadia Bridge no tiene firma propia.

## Panel visual 0.3

Se analizó la aplicación pública ControllerTest.io con REA y se implementó un visor original en WinForms. El detalle de la evidencia está en `docs/VISOR-REA.md`. Incluye dibujo del mando con botones iluminados, sticks desplazables, gráficos de lectura previa a la zona muerta, barras de gatillos y cuadrícula de botones. El visor conserva la lectura USB si no se puede crear el mando virtual.

La prueba real del panel recibió 876 reportes, representó B e Y pulsados y creó correctamente un Xbox virtual. En este momento ViGEmBus ya estaba disponible en el equipo. El proceso de prueba cerró el visor y retiró el mando virtual al terminar. No se comprobó todavía un juego ni una instalación limpia de la versión nueva.
