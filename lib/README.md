# Biblioteca nativa

`ViGEmClient.dll` es una dependencia AMD64 sin modificar, distribuida bajo MIT. Procede del [ZIP oficial de stadiacontroller v1.3.1](https://github.com/71/stadiacontroller/releases/tag/v1.3.1).

SHA-256 de la DLL: `966e579ad8224ca87ae78a8788c5eb0c33ecb932dcc6ca25c39871e20b97a15a`.

Se conserva en el código fuente para permitir compilar sin depender de una descarga antigua. Puede recuperarse con `scripts/Get-Dependencies.ps1 -Client`, que verifica tanto el ZIP como la DLL. La biblioteca no está firmada con Authenticode. Su procedencia no debe confundirse con la revisión de cabeceras consultada durante la implementación.

Ver [avisos de terceros](../THIRD-PARTY-NOTICES.txt) y [créditos](../FUNCIONAMIENTO-Y-CREDITOS.md).
