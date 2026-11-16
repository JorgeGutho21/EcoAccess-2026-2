# Ventanas y escalado — cierre de software (15/11/2026)

Arnés: `tests/visual/ArnesVentanas` con Oracle inalcanzable a propósito (127.0.0.1:1, sin datos reales). Para cada módulo
de las áreas Supervisor y Operario: minimizar → restaurar, maximizar → restaurar, recorrer pestañas, achicar a 960×600 y
volver, y medir el área lógica de cinco escalas.

## Cómo se emula el escalado

Con `PerMonitorV2` y `AutoScaleMode.Dpi`, una pantalla al 125 % o 150 % equivale al mismo formulario en un área lógica más
pequeña: (pantalla − barra de tareas) ÷ escala. Se midieron 1904×993 (100 % en 1920×1080), 1520×790 (125 %), 1264×655 (150 %),
1077×545 (125 % en 1366×768) y 895×450 (150 % en 1366×768). **No se cambió la escala de Windows**: falta verlo en un monitor
real con esa escala.

## Resultado

| Área | Controles que no vuelven a su lugar | Textos cortados o controles fuera de su panel |
|---|---:|---:|
| Supervisor (13 módulos) | 0 | 0 |
| Operario (8 módulos) | 21, todos en «Registro manual» | 0 |

Los 21 de «Registro manual» no son de diseño: el texto de estado pasa de «Consultando el catálogo…» a «El catálogo no
respondió…» (dos líneas) cuando vence la consulta a Oracle inalcanzable, y lo de abajo baja una línea.

Antes de las correcciones: 70 recortes en Supervisor y 30 en Operario (botones y etiqueta del panel derecho de «Canecas y
QR», filtros de «Estadísticas», «Registro manual» a 150 %, textos de «Tarifas y ecopuntos» y «Sistema y Telegram», tarjeta
del Dashboard a 150 %).

`canecas_qr_pantalla_real_corregido.png` es una captura de la pantalla real (no DrawToBitmap, que corta textos que en
pantalla se ven completos): el panel derecho ya no se sale ni pide desplazamiento horizontal.
