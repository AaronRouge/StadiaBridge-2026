# Historial de cambios

## 0.7.2 — icono de la aplicación

- Icono original de mando, con fondo oscuro y acentos cian/violeta, incluido en nueve tamaños para Windows.
- Integrado en el ejecutable, la ventana, la barra de tareas, el instalador y los accesos directos.
- Fuente SVG y generador del archivo ICO incluidos en el repositorio.

## 0.7.1 — actualización local, 20 de octubre de 2026

- Tarjeta de vibración con Fuerte, Suave, Ráfaga y Pulso, indicadores de ambos motores y botón para detener la prueba.
- Patrones breves que respetan la intensidad global y recuperan la orden más reciente del juego. Los cuatro fueron confirmados físicamente por el usuario.
- 1134 comprobaciones automatizadas, incluyendo límites temporales, intervalos, ondas, silencio y cancelación de los patrones nuevos.
- Corrección del escalado: cabecera, ajustes, fuentes y controles comparten la escala del tamaño de ventana; tamaño inicial adaptado al DPI y al área disponible.
- Pruebas de interfaz separadas mediante `--ui-test` y capturas verificables con `--preview-scaling`; no utilizan el mando físico.
- Código organizado por dispositivos, protocolo, emulación, vibración e interfaz.
- Versión centralizada en `Directory.Build.props`.
- Licencia MIT para el código propio, créditos y licencia de ViGEmBus conservados en el repositorio.
- Guía de contribución, preparación de dependencias y flujo de comprobaciones para GitHub Actions.
- Documentación y avisos de licencia incluidos con sus rutas relativas en el paquete.
- Usuario confirmó que botones, sticks y gatillos responden y que la interfaz se ve correctamente al maximizar y restaurar.
- La versión 0.7.0 se conserva como referencia hasta comprobar la instalación de esta actualización. Todavía no se ha publicado una Release en GitHub.

## 0.7.0 — versión local de referencia

- Selector de modo claro y oscuro, independiente del estilo del mando.
- Conserva estilos Neón y Clásico · Xbox, botones iluminados, sticks y gatillos analógicos.
- Controles personalizados para zona muerta, intensidad de vibración y selección de estilo.
- Emulación Xbox 360 mediante ViGEmBus, reconexión USB y prueba manual de vibración de dos motores.
- Instalador todo en uno para Windows x64 con runtime .NET y ViGEmBus incluidos.

La lectura USB, la creación del mando virtual y el pulso USB se han comprobado. Quedan pendientes las pruebas completas dentro de juegos y en Windows limpio.
