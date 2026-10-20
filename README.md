# Stadia Bridge

Aplicación gratuita para Windows x64 que convierte un mando Google Stadia **USB** en un mando **Xbox 360 virtual compatible con XInput**. Incluye visor interactivo, estilos Neón y Clásico, modo claro y oscuro, zona muerta y vibración USB.

Proyecto independiente desarrollado y mantenido por [AaronRouge](https://github.com/AaronRouge). Con emulación [ViGEmClient](https://github.com/nefarius/ViGEmClient) / [ViGEmBus](https://github.com/nefarius/ViGEmBus) e inspirado en el protocolo de [71/stadiacontroller](https://github.com/71/stadiacontroller). Los créditos completos y licencias se detallan en [Funcionamiento y créditos](FUNCIONAMIENTO-Y-CREDITOS.md).

<p align="center">
  <img src="docs/screenshots/stadia-bridge-neon-dark.png" alt="Stadia Bridge - Neón Oscuro" width="49%" />
  <img src="docs/screenshots/stadia-bridge-classic-dark.png" alt="Stadia Bridge - Clásico Xbox" width="49%" />
</p>
<p align="center">
  <img src="docs/screenshots/stadia-bridge-neon-light.png" alt="Stadia Bridge - Neón Claro" width="49%" />
  <img src="docs/screenshots/stadia-bridge-classic-light.png" alt="Stadia Bridge - Clásico Claro" width="49%" />
</p>


## Instalar y usar

La actualización local es **0.7.2**. El instalador es `dist/StadiaBridge-Setup-0.7.2-x64.exe`, acompañado de su SHA-256. Incluye un icono original de mando para el programa y sus accesos directos, además de las correcciones y los cuatro modos de vibración confirmados por el usuario. El 0.7.0 se conserva como referencia hasta comprobar la instalación de la actualización. Al publicar el proyecto se adjuntará en GitHub Releases; todavía no hay un enlace de descarga público.

1. Abre el instalador y acepta el permiso de administrador de Windows. Incluye la aplicación, .NET y el instalador oficial de ViGEmBus; su ejecución no necesita Internet.
2. Reinicia si el asistente lo indica.
3. Abre Stadia Bridge y conecta el mando con un cable USB que transmita datos. La conexión comienza automáticamente.
4. Pulsa botones y mueve sticks para comprobar el visor. Con ViGEmBus disponible se crea también el mando Xbox virtual.
5. Usa **Fuerte**, **Suave**, **Ráfaga** o **Pulso** en la tarjeta de vibración. Cada prueba termina sola. **Vibración (%)** ajusta la intensidad; 0 % silencia los motores. **Detener** en la tarjeta cancela la prueba y devuelve el control al juego.

La desinstalación conserva ViGEmBus porque puede estar compartido con otras aplicaciones. El instalador propio no está firmado; el paquete oficial de ViGEmBus incluido conserva su firma.

## Funciones y límites

- Botones, cruceta, sticks y gatillos analógicos; zona muerta de 0 a 30 % por eje.
- Visor con botones iluminados, sticks móviles, gráficos de ejes y barras de gatillos. Muestra la lectura física antes de aplicar la zona muerta a la salida Xbox.
- Estilos Neón y Clásico · Xbox, modo Claro / Oscuro y preferencias guardadas.
- Vibración de dos motores por USB, cuatro patrones de prueba y prueba independiente de ViGEmBus al detener la sesión de conexión.
- Reconexión USB, una instancia y un mando físico.

La salida es Xbox 360, no un dispositivo Xbox One ni un adaptador para consolas físicas. **Bluetooth todavía no está implementado.** Tampoco se incluyen perfiles, reasignación o configuración de HidHide; algunos juegos podrían registrar tanto el mando físico como el virtual.

Se espera un Stadia HID con VID `18D1`, PID `9400` y reporte `03`. Otro firmware necesita validación. El temporizador visual nominal de 33 ms no mide la frecuencia USB ni la latencia.

## Validación

Las comprobaciones automatizadas del protocolo y la vibración completaron 1134 verificaciones. Se observaron reportes USB reales y creación del mando Xbox virtual. El usuario confirmó entradas, legibilidad al maximizar/restaurar y los cuatro modos de vibración con finalización automática. Quedan pendientes las pruebas completas dentro de juegos y la instalación en Windows limpio.

ViGEmBus está archivado y stadiacontroller declara que ya no se mantiene. Usamos sus componentes y referencias con atribución; no presentamos Stadia Bridge como una actualización oficial de ellos.

## Compilar

Requiere Windows x64, SDK .NET 8 y PowerShell. Desde la raíz:

```powershell
.\build.ps1
```

Ejecuta las comprobaciones y genera la aplicación portable en `dist/StadiaBridge/`. Esa variante necesita .NET Desktop Runtime 8 x64 instalado. El instalador autónomo se construye por separado siguiendo [installer/README.md](installer/README.md).

`lib/ViGEmClient.dll` se conserva como dependencia verificada. Si necesitas recuperarla o descargar el controlador para empaquetar:

```powershell
.\scripts\Get-Dependencies.ps1 -Client -Driver
```

El script valida hashes y la firma del instalador oficial; no instala el controlador. El compilador Inno Setup se prepara manualmente según la guía de empaquetado.

La versión se define en `Directory.Build.props`. El flujo de GitHub Actions compila y comprueba en Windows, sin instalar controladores ni publicar releases. Utiliza [actions/checkout](https://github.com/actions/checkout/tree/v4) y [actions/setup-dotnet](https://github.com/actions/setup-dotnet/tree/v4).

## Organización

```text
src/StadiaBridge/
  Program.cs          Entrada y herramientas de diagnóstico
  Devices/            Descubrimiento y comunicación HID
  Protocol/           Reportes y conversión de entradas
  Emulation/          Cliente ViGEm y mando virtual
  Vibration/          Estado, intensidad y escritura de motores
  UI/                 Ventana, dibujo y controles temáticos
tests/                Comprobaciones sin mando físico
scripts/              Preparación de dependencias verificadas
installer/            Asistente e instrucciones de empaquetado
licenses/             Licencias de terceros
lib/                  ViGEmClient x64 y procedencia
docs/                 Investigación y preparación para GitHub
.github/              Integración continua y plantilla de incidencias
```

`dist/`, las cachés, las capturas locales y `research/` se excluyen del repositorio. Los instaladores se distribuirán mediante Releases. La actualización 0.7.1 incluye la reorganización del código y la corrección de escalado. El instalador 0.7.0 conservado permanece sin cambios.

## Diagnóstico

En la versión instalada puedes ejecutar `StadiaBridge.exe --diagnose`. Para un pulso manual desde consola, cierra primero la ventana y usa `--test-rumble`. Los registros se escriben en `%LOCALAPPDATA%\StadiaBridge\Logs`. Revisa y retira datos personales antes de compartirlos.

`--preview-ui <carpeta>` genera vistas con datos simulados identificadas en la imagen; no conecta el USB.

El código en prueba incluye `--ui-test` para una ventana interactiva con datos simulados y `--preview-scaling <carpeta>` para verificar tamaños. Consulta [la revisión de escalado](docs/ESCALADO-INTERFAZ.md). Estos cambios se incluyen en 0.7.1; el instalador 0.7.0 conservado permanece sin cambios.

## Documentación y licencia

- [Funcionamiento, procedencia y créditos](FUNCIONAMIENTO-Y-CREDITOS.md).
- [Preparar la publicación en GitHub](docs/PUBLICAR-GITHUB.md).
- [Modos de vibración USB](docs/VIBRACION-MODOS.md).
- [Historial de cambios](CHANGELOG.md) y [guía para contribuir](CONTRIBUTING.md).
- [Investigación del protocolo](docs/INVESTIGACION.md) y [análisis visual con REA](docs/VISOR-REA.md).
- [MIT para el código propio](LICENSE), [avisos de terceros](THIRD-PARTY-NOTICES.txt) y [licencias adicionales](licenses/README.md).

Google, Stadia, Xbox y Microsoft son nombres de sus respectivos titulares. No existe afiliación ni patrocinio por parte de ellos ni de los proyectos citados.


