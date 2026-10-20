# Construir el instalador todo en uno

El resultado actual es `dist/StadiaBridge-Setup-0.7.2-x64.exe`, un único asistente en español para Windows x64. Incluye la aplicación autónoma con .NET 8 y ViGEmBus 1.22.0. La instalación no necesita Internet.

## Fuentes y preparación

Se utilizó Inno Setup 7.1.0 oficial en modo portable dentro de `.build-tools/InnoSetup`. Descarga oficial: https://github.com/jrsoftware/issrc/releases/tag/is-7_1_0. Su instalador admite `/PORTABLE=1 /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /NOICONS /DIR="ruta absoluta del proyecto\.build-tools\InnoSetup"`.

Ejecuta `./scripts/Get-Dependencies.ps1 -Driver` desde la raíz para obtener `ViGEmBus_1.22.0_x64_x86_arm64.exe` en `installer/vendor/` desde la publicación oficial https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0. La licencia se conserva en `licenses/ViGEmBus-LICENSE.txt` dentro del repositorio. El script descarga y verifica, sin instalar ni modificar el ejecutable. Su huella SHA-256 es `89220A7865076B342892F98865F3499FB7C4CFD673159E89D352C360FD014C6A`, contrastada con el manifiesto oficial de Microsoft Winget.

El script de construcción verifica la huella y la firma Authenticode del controlador antes de empaquetarlo. Los parámetros silenciosos `/exenoui /qn /norestart` proceden del manifiesto de Winget en https://github.com/microsoft/winget-pkgs/blob/master/manifests/v/ViGEm/ViGEmBus/1.22.0/ViGEm.ViGEmBus.installer.yaml.

Ejecuta `./build-installer.ps1` con el SDK .NET 8. La primera construcción necesita Internet para restaurar los paquetes de runtime win-x64. Las posteriores pueden usar la caché local. El compilador y las descargas no se registran en el repositorio.

La versión se lee de `Directory.Build.props` y se pasa al compilador Inno Setup. El script incorpora la licencia MIT propia, los avisos de terceros y las licencias del runtime. El empaquetado vuelve a generar carpetas de trabajo en `dist/`.

El compilador utilizado mostró «Non-commercial use only». Antes de una publicación, revisa la [licencia oficial y condiciones de la edición de Inno Setup](https://github.com/jrsoftware/issrc/releases/tag/is-7_1_0). La preparación del compilador sigue siendo manual; no se promete una construcción del instalador sin ese paso.

El instalador 0.7.0 que se conserva localmente no se recompiló con la reorganización. Para una nueva distribución del código organizado, incrementa la versión y conserva la trazabilidad entre código, paquete y hash.

## Comportamiento

- Solicita elevación de Windows para instalar en Archivos de programa y registrar el controlador.
- Si existe el servicio ViGEmBus, conserva esa instalación. No elimina ni reemplaza controladores existentes.
- Si falta el servicio, ejecuta el instalador oficial incluido. Comprueba códigos de error y reinicio; no reinicia automáticamente.
- Copia el runtime junto a la aplicación. No requiere instalar .NET a nivel del equipo.
- Crea accesos directos y una entrada de desinstalación.
- Comprueba la conexión al bus al terminar. Si todavía no está disponible, indica reinicio y no abre la aplicación automáticamente.
- Desinstalar Stadia Bridge conserva ViGEmBus porque otras aplicaciones pueden utilizarlo.
- Los diagnósticos se escriben en `%LOCALAPPDATA%/StadiaBridge/Logs` para que la aplicación funcione sin elevación después de instalarse.

## Límites de validación

La creación del instalador y las comprobaciones del programa no equivalen a probar una instalación completa en un Windows limpio. No se instaló ViGEmBus en el equipo del usuario como parte de la construcción. El instalador de Stadia Bridge no está firmado con un certificado propio; el instalador de ViGEmBus incluido sí conserva su firma oficial.


