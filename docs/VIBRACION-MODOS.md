# Panel de pruebas de vibración USB

Incorporado en la versión 0.7.1. El usuario confirmó con su Stadia físico que los cuatro modos se distinguen y terminan automáticamente.

## Opciones

| Botón | Salida solicitada antes del porcentaje global | Duración |
| --- | --- | --- |
| Fuerte | Solo motor grande, intensidad 255/255 | 650 ms |
| Suave | Solo motor pequeño, intensidad 255/255 | 650 ms |
| Ráfaga | Tres golpes de ambos motores a 220/255, de 120 ms separados por intervalos de silencio de 100 ms | 660 ms incluyendo el intervalo final |
| Pulso | Dos ondas suaves de ambos motores, con máximo 200/255 y 600 ms por onda | 1200 ms |

«Fuerte» y «Suave» distinguen los motores; la sensación depende de su respuesta física. **Vibración (%)** ajusta todas las pruebas y la vibración solicitada por los juegos. Con 0 % no se envía intensidad de motor.

Los indicadores laterales del icono representan la salida solicitada después del ajuste de intensidad. No son sensores de vibración física.

## Durante un juego

La prueba sustituye temporalmente la orden de vibración del juego. Cuando termina, recupera la orden más reciente. Pulsar otra opción reemplaza la prueba activa.

**Detener** en la tarjeta cancela la prueba y recupera la salida del juego: si el juego solicita vibración, el mando puede seguir vibrando. Para silenciar toda la salida se puede poner el porcentaje en 0 % o detener la conexión mediante el botón superior.

La prueba independiente sin sesión virtual utiliza el mismo escritor USB y apaga los motores al terminar o cancelar. Mientras esa prueba independiente está en ejecución no se inicia otra simultáneamente. Si la aplicación está intentando conectar, hay que detener esa conexión antes de iniciar una prueba independiente.

## Diseño y comprobaciones

La tarjeta utiliza gráficos propios: borde redondeado, distintivo ámbar, ondas centrales, dos indicadores y cuatro botones con acentos naranja, azul, violeta y rosa. Se adapta a los estilos Neón y Clásico y a los modos claro y oscuro. La referencia visual aportada por el usuario es ControllerTest.io; no se han incorporado sus gráficos ni su código.

La lógica se encuentra en [Rumble.cs](../src/StadiaBridge/Vibration/Rumble.cs) y la tarjeta en [VibrationPanel.cs](../src/StadiaBridge/UI/VibrationPanel.cs).

Las comprobaciones automatizadas verifican los motores independientes, los intervalos de ráfaga, las ondas, la duración, la intensidad, el silencio, la cancelación y la recuperación de la orden más reciente del juego. El conjunto del proyecto completa **1134 comprobaciones**. La prueba física del usuario no sustituye las pruebas pendientes de vibración originada desde juegos ni de instalación en Windows limpio.

La prueba de consola `--test-rumble` conserva el pulso heredado de ambos motores durante medio segundo.
