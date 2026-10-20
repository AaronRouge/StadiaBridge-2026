# Visor de Stadia Bridge 0.3

## Referencia y análisis estático REA

Se descargaron el HTML público de https://controllertest.io/ y seis módulos JavaScript distribuidos por la página: GamepadManager, Hero, GamepadVisualizer, gamepadStore, TriggerTest y RawDataView. Se analizaron como un árbol local con `analyze_javascript_application`, sin ejecutar esos scripts ni incluir publicidad o analítica en Stadia Bridge.

Evidencia completa retenida por REA: `ev_48fe733d1ce17e1222e1d70df86551e5f8d4a6642ad030ec46fc7a8069402cba`. Resumen: `ev_1cee5b582c3ea59ec393119ac66f01ff49babbf8ffd276d344ee365a232f3bee`. Identidad del árbol capturado: SHA-256 `69782dc5c30af6178688205b626d4673a934b4474436db57a4557d9a64e3feff`.

REA examinó seis módulos JavaScript, 39303 bytes de texto y 7597 nodos AST, sin fallos de parseo. La cobertura es parcial: nueve archivos inventariados quedaron fuera del análisis y hay relaciones dinámicas no resueltas. La inspección no observó una sesión real con un mando en esa web.

## Observaciones en los módulos distribuidos

- GamepadManager consulta `navigator.getGamepads()` y agenda la siguiente lectura con `requestAnimationFrame`. También registra conexiones y desconexiones.
- El visor dibuja el mando con SVG y consulta los estados de botones y ejes para actualizar su representación.
- TriggerTest consulta los valores analógicos de `buttons[6]` y `buttons[7]`.
- El almacenamiento compartido conserva el slot seleccionado y los mandos detectados.

Estos son detalles de la aplicación web de referencia. Stadia Bridge utiliza HID nativo y WinForms; no utiliza la Gamepad API ni ejecuta el sitio dentro de su interfaz.

## Implementación propia

Se implementó un panel vectorial con GDI+, dibujo original de un mando con sticks simétricos, botones iluminados, sticks desplazables, dos gráficos de posición, dos barras analógicas y una cuadrícula de botones con sus nombres. Los botones adicionales mantienen los nombres Extra 1 y Extra 2 hasta verificar su identidad física.

La lectura USB continúa en una tarea independiente. El panel se actualiza con un temporizador de 33 ms nominales; esto no es una medición de la frecuencia USB ni de latencia. Los gráficos usan la lectura previa a la zona muerta y muestran por separado la salida mapeada. El visor y la prueba de vibración pueden funcionar aunque no se pueda crear el Xbox virtual; el estado informa de la diferencia.

No se incorporó el SVG, la marca ni el código del sitio al producto. Las capturas generadas con `--preview-ui` contienen datos simulados y se identifican expresamente como vistas de diseño.

## Comprobación visual y física

Se generaron y revisaron capturas con datos simulados a 1280 × 860 y 1080 × 790. La revisión detectó un problema de escala de fuentes en el entorno de alta densidad; se corrigió usando unidades de píxel dentro del lienzo escalado y en los controles. También se corrigió el reparto de altura de la barra de conexión para mantener visible el detalle de emulación.

El 20 de octubre de 2026 se ejecutó el visor durante 15 segundos con el Stadia conectado, sin inyectar datos simulados. Se recibieron 876 reportes USB, se creó el Xbox virtual y la captura final mostró la máscara `0xA000` (B e Y) tanto en el dibujo como en la cuadrícula. Los sticks y gatillos estaban en reposo al tomar esa captura. Evidencia local: `docs/previews/stadia-bridge-usb.png` y su archivo `.txt` asociado. Esto no mide latencia ni comprueba un juego.

## Acabado neón 0.4

La representación del mando se redibujó con carcasa grafito, contorno cian/violeta, empuñaduras texturizadas, cruceta continua, sticks con anillos y ranuras y botones ABXY con sus propios colores. Las pulsaciones activan halos en el control y en la cuadrícula. El panel utiliza tarjetas oscuras con degradados y botones redondeados que representan correctamente los estados deshabilitado, foco y puntero.

Los gráficos siguen siendo vectoriales nativos GDI+: no se añaden temas descargados ni dependencias de ejecución. Los efectos visuales se calculan a partir del mismo estado del mando y no cambian su mapeo ni el protocolo USB. Se revisaron imágenes de reposo, pulsación y ventana compacta, generadas con datos simulados en `docs/previews/neon/`.

## Selector de estilos 0.5

Se añadió un estilo Clásico · Xbox inspirado en la captura del usuario: tarjetas cálidas oscuras, silueta mate, sticks grandes en distribución asimétrica, cruceta de una sola pieza, botones azules al pulsar, ejes verdes y gatillos morados. Es una disposición visual alternativa para el mismo mando Stadia y no cambia los bits enviados a Xbox.

El selector Estilo conserva también Neón. La elección se aplica sin reconectar el mando y se guarda en `%LOCALAPPDATA%/StadiaBridge/visual-style.txt`. Si todavía no hay preferencia, se abre el estilo clásico. Las pruebas de diseño omiten el guardado de preferencias para no cambiar la selección del usuario. Se generaron y revisaron ambos estilos a tamaño normal y compacto en `docs/previews/styles/`, usando datos simulados identificados en las imágenes.

## Controles y bordes 0.6

Se sustituyeron los cuadros NumericUpDown y ComboBox de apariencia nativa por controles propios: campos oscuros redondeados con botones de menos y más y selector con flecha dibujada y menú oscuro. Los campos permiten escritura, confirmación con Enter, flechas arriba/abajo y conservan los rangos de 0–30 % y 0–100 %. La tecla Escape recupera el valor vigente si se estaba escribiendo.

El selector sigue permitiendo elegir los mismos dos estilos y conserva su preferencia. Los controles cambian de paleta con el tema, muestran el foco de teclado y no utilizan las flechas blancas del sistema. Se reorganizaron los ajustes en dos columnas para mantener completos los títulos y el valor 100 en la ventana compacta. Las capturas de ambos estilos se generaron con datos simulados en `docs/previews/inputs/` y se revisaron visualmente.

## Modo claro y oscuro 0.7

Se añadió una paleta central y un selector de color independiente del estilo del mando. Las cuatro combinaciones (claro/oscuro y neón/clásico) cambian fondos, tarjetas, texto, bordes, controles de entrada, menús y botones. El mando conserva su carcasa para que sus botones sigan siendo reconocibles; los textos de su superficie mantienen el contraste. En Windows compatible, la barra de título acompaña el modo seleccionado.

La preferencia de color se guarda en `%LOCALAPPDATA%/StadiaBridge/color-mode.txt`, separada de la preferencia de estilo. El modo inicial es oscuro si no hay preferencia. Cambiarlo no crea ni retira el mando virtual. Las capturas con datos simulados se generan en `docs/previews/modes/`, incluyendo ambas opciones en ventanas normales y compactas y el regreso de claro a oscuro.
