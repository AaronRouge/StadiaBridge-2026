# Preparar la primera publicación

## Estado local

- La actualización 0.7.1 incorpora el escalado corregido y el panel de cuatro pruebas de vibración, confirmados con el mando físico. Su instalador y SHA-256 se generan en `dist/`. Se conserva también el 0.7.0 hasta comprobar la instalación de la actualización.
- SHA-256 del instalador conservado: `FF51A504DAFB31C672278FD94487D9A03E5DC05EE6AC1F6BA55B67DEFC31C275`.
- El instalador 0.7.0 no se modifica; el nuevo paquete utiliza el código organizado de la versión 0.7.1.
- El código está organizado por función, con versión centralizada, licencia MIT, créditos, guía de contribución y comprobaciones para GitHub Actions.
- Se comprobó desde una copia sin `bin/` ni `obj/`: restauración con caché local, 1098 verificaciones aprobadas y compilación sin errores ni advertencias. Esto no sustituye una prueba de instalación en Windows limpio.
- Todavía no se ha creado ni publicado un repositorio remoto, etiqueta o Release.

## Archivos que forman el repositorio

Incluir `src/`, `tests/`, `scripts/`, `installer/` sin `vendor/`, `licenses/`, `lib/`, `.github/`, documentación de `docs/` sin capturas locales, scripts de construcción y archivos de texto de la raíz.

Se conserva `lib/ViGEmClient.dll`, con origen, hash y licencia documentados. Se excluyen los instaladores de `dist/`, los repositorios anidados y archivos de `research/`, los compiladores descargados, cachés, `bin/`, `obj/` y registros personales. `.gitignore` ya establece esas exclusiones.

## Antes de subir el código

1. Ejecutar `build.ps1` desde una copia limpia. La compilación y comprobaciones deben funcionar con el SDK .NET 8 en Windows x64; no necesitan un mando físico ni instalar controladores.
2. Revisar el conjunto de archivos que se va a añadir. `.gitignore` no retira archivos que ya estuvieran registrados previamente en un repositorio existente.
3. Confirmar el nombre público y la cuenta de GitHub que alojará el proyecto. No hay un propietario remoto configurado por estos cambios.
4. Mantener el nombre de crédito colectivo del `LICENSE` o sustituirlo por la identificación que decida el responsable, conservando siempre los copyrights externos.
5. Al subir el código, comprobar la primera ejecución de Actions. El flujo preparado solo construye y comprueba; no publica automáticamente.

Las referencias de [checkout](https://github.com/actions/checkout/tree/v4) y [setup-dotnet](https://github.com/actions/setup-dotnet/tree/v4) describen las acciones utilizadas por el flujo.

## Antes de publicar la descarga

- Probar el instalador en Windows limpio y documentar cualquier reinicio necesario.
- Comprobar botones, sticks, gatillos y vibración desde un juego compatible con XInput.
- Revisar las condiciones de la edición de Inno Setup usada para empaquetar. Consultar [installer/README.md](../installer/README.md).
- Si se publica el instalador conservado, usar una etiqueta basada en el estado previo a la reorganización. Para distribuir un paquete nuevo del código organizado, incrementar la versión en `Directory.Build.props`, reconstruir y generar un nuevo hash; no sustituir silenciosamente el instalador de referencia.
- El script del instalador impide sobrescribir un paquete de la misma versión que ya exista en `dist/`.
- Adjuntar el instalador y su SHA-256 a una Release, con límites conocidos y referencia al código correspondiente.
- Marcar la primera publicación como preliminar mientras falten las pruebas completas indicadas en el README.

La reorganización del repositorio no demuestra por sí sola compatibilidad con todos los juegos o revisiones de firmware.
