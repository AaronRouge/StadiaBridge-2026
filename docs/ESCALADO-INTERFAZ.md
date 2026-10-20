# Corrección del escalado de la interfaz

Estado: escalado e indicadores confirmados por el usuario con el mando físico. La corrección se incluye en la actualización 0.7.1. El instalador 0.7.0 se conserva como referencia.

## Problema observado

El visor vectorial aumentaba con el área disponible, pero las filas de cabecera y ajustes conservaban alturas, fuentes y controles en píxeles fijos. Al maximizar en una pantalla de alta resolución aparecían desproporcionadamente pequeños. El tamaño inicial tampoco se adaptaba explícitamente al DPI del monitor.

## Cambio aplicado

- `UI/ResponsiveLayout.cs` conserva las medidas originales y aplica una escala común a fuentes, posiciones, tamaños, márgenes y filas/columnas absolutas. Cada cambio parte de las medidas originales para evitar que el redondeo se acumule al maximizar y restaurar.
- El factor se obtiene de la ventana respecto al diseño de 1280×860. El visor conserva su proporción y se centra en el espacio restante.
- El tamaño inicial toma en cuenta el DPI y el área de trabajo disponible. Se declara `PerMonitorV2`; se evita mezclar el escalado manual del diseño con el escalado automático por fuente de WinForms.
- Los campos numéricos escalan sus botones y editor interior, y el selector adapta sus espacios y flecha.
- Los botones repintan su fondo completo para evitar esquinas negras en la ventana real, especialmente en modo claro.

Referencia de plataforma: [documentación de escalado de Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/forms/autoscale).

## Pruebas realizadas

- Compilación temporal Debug sin apphost, sin generar un nuevo EXE de distribución ni instalador.
- 1098 comprobaciones del protocolo y vibración aprobadas.
- Capturas simuladas y comprobación de fuentes comunes, controles dentro de sus paneles y editores numéricos sin recortes en 1280×860, 1080×790, 1920×1290, 2560×1440 y 3840×2060; también regreso a 1280×860 y modo claro a 3840×2060.
- Ventana interactiva real en el escritorio a 192 DPI (200 %): apertura, maximizar, cambiar de oscuro a claro y restaurar. Se revisó el repintado de esquinas y se comprobó que el botón de aumentar zona muerta cambia de 5 a 6.
- Prueba posterior con el mando físico en la compilación temporal corregida: conexión USB activa, creación del mando Xbox 360 virtual y salida de vibración USB abierta. El contador aumentó de 935 a 1224 reportes entre la vista normal y la maximizada. Esto confirma continuidad de lectura, no una comprobación individual de todos los botones ni de vibración dentro de juegos.
- El usuario respondió «Todo responde y se ve bien» tras solicitarle comprobar A/B/X/Y, ambos sticks, gatillos y maximizar/restaurar. Esa confirmación completa la revisión física de entradas y legibilidad; no equivale a una prueba de vibración dentro de juegos.
- SHA-256 del instalador conservado sin cambios: `FF51A504DAFB31C672278FD94487D9A03E5DC05EE6AC1F6BA55B67DEFC31C275`.

Las capturas y registros de diseño se guardan bajo `artifacts/`, excluido de Git. Los tamaños de captura son áreas de cliente en píxeles; no representan pruebas en cinco monitores ni cinco ajustes físicos de DPI.

## Repetir la prueba sin empaquetar

Con el SDK .NET 8, desde la raíz:

```powershell
dotnet build src/StadiaBridge/StadiaBridge.csproj -c Debug -p:UseAppHost=false -o artifacts/ui-test
dotnet artifacts/ui-test/StadiaBridge.dll --ui-test
```

Cerrar la ventana temporal antes de volver a compilar sobre la misma carpeta. La ventana indica **DATOS SIMULADOS**, desactiva conexión y vibración y no guarda preferencias visuales. No necesita acceso al mando ni crea otro dispositivo virtual.

Para capturas y verificaciones automáticas de diseño, usar una carpeta nueva para cada ejecución:

```powershell
dotnet artifacts/ui-test/StadiaBridge.dll --preview-scaling artifacts/scaling-check
```

## Pendiente

- Prueba de instalación de la actualización empaquetada y comprobación en Windows limpio.
- Mover la ventana entre monitores con DPI diferentes: se ha implementado la respuesta al cambio, pero no se ha probado físicamente ese recorrido.
- Confirmación de vibración desde un juego. Los cuatro modos manuales del panel añadido después del ajuste de escalado ya fueron confirmados físicamente; véase [VIBRACION-MODOS.md](VIBRACION-MODOS.md).

La compilación de distribución se autoriza después de la confirmación física recibida. Mantener separadas las comprobaciones del paquete y las de instalación: generar un instalador no demuestra que se haya probado en Windows limpio.
