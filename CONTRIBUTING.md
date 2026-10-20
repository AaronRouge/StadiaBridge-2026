# Contribuir a Stadia Bridge

El proyecto se prepara para publicarse gratuitamente. El código propio utiliza [MIT](LICENSE); las dependencias conservan sus licencias en [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) y [licenses/](licenses/README.md).

## Entorno y comprobaciones

Requiere Windows x64, SDK .NET 8 y PowerShell. Desde la raíz:

```powershell
.\build.ps1
```

Las comprobaciones no necesitan mando ni ViGEmBus. El comando publica una versión portable en `dist/StadiaBridge/`. Para el instalador, consulta [installer/README.md](installer/README.md).

Conserva el namespace `StadiaBridge` al reorganizar archivos. El SDK incluye los archivos C# de las subcarpetas automáticamente; las comprobaciones enlazan explícitamente el protocolo y la vibración.

## Proponer cambios

Explica el problema, el comportamiento esperado y cómo comprobaste la solución. Añade comprobaciones cuando cambies interpretación de reportes, mapeo o vibración. Distingue entre resultados simulados y resultados obtenidos con un mando físico.

Para incidencias, indica versión de Stadia Bridge, versión de Windows, conexión USB, pasos para reproducir y si el problema afecta al visor, a la salida Xbox o a la vibración. Retira rutas personales e identificadores del dispositivo de cualquier registro adjunto.

No incluyas cachés, instaladores, descargas de investigación ni datos de pruebas locales en los cambios. Los binarios públicos irán en GitHub Releases.
