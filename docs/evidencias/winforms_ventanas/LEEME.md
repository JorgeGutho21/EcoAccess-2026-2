# Fallo de pestañas y ventanas en Windows Forms (14/11/2026)

## Cómo se reprodujo

`tests/visual/ArnesVentanas` abre la **ventana principal real** (`FormPrincipal`) del área Supervisor y cada módulo del menú.
Oracle es inalcanzable a propósito (`127.0.0.1:1`): no usa credenciales ni datos reales, y además prueba la navegación sin
conexión. Para cada módulo guarda la posición y el tamaño de **todos** los controles visibles y luego hace:

1. minimizar → restaurar;
2. maximizar → restaurar;
3. recorrer todas las pestañas, minimizar, volver a la pestaña original y restaurar;
4. achicar la ventana a 960×600 y volver a 1280×760.

Después de cada acción compara la geometría con la inicial. También busca controles hermanos que se pisan.

```
dotnet build tests/visual/ArnesVentanas -c Debug
tests/visual/ArnesVentanas/bin/Debug/net48/ArnesVentanas.exe <carpeta-de-salida> supervisor
```

La ventana aparece en el escritorio mientras corre (unos 2 minutos). Si alguien hace clic en ella, la medición deja de
valer: en una corrida un clic externo devolvió la vista al Dashboard y no se repitió en las tres corridas siguientes.

## Causas raíz encontradas

| Síntoma | Causa | Corrección |
|---|---|---|
| Dashboard: tras minimizar y restaurar, el contenido medía 2 093 → 2 569 px y quedaba un hueco | Paneles con `AutoSize` en el modo por defecto `GrowOnly`: al estrecharse crecen y nunca vuelven a encoger | `EstabilizadorVentana.PermitirEncoger` pasa a `GrowAndShrink` los paneles de cada módulo al montarlo |
| Usuarios y Materiales: al cambiar de pestaña o restaurar, la vista saltaba 38 a 276 px | Al recuperar el foco, el contenedor desplazable se mueve hasta el control activo. `PanelDesplazable` solo lo evitaba con el ratón presionado | El contenedor sigue al foco solo si se navega con Tab (accesibilidad); además se recuerdan y restauran las posiciones al minimizar |
| Estadísticas: el cuadro de resumen tapaba 16 px de las pestañas | `DisenoFormularios.AjustarDisenoFijo` bajaba los controles de arriba, pero no los estirados arriba y abajo (pestañas) | Los controles anclados arriba y abajo bajan lo necesario sin mover su borde inferior |

## Resultado

| Corrida | Controles que no volvieron a su lugar | Controles superpuestos |
|---|---|---|
| Antes (`antes_informe_supervisor.txt`) | 44 | 1 (Estadísticas) |
| Después (`despues_informe_supervisor.txt`) | 38 | 0 |

Las 38 diferencias que quedan **no se acumulan**: el primer cambio de tamaño reacomoda una sola vez el cuadro de descripción
de Materiales (+23 px) y el Dashboard institucional (−5 px). Después la geometría queda fija.

Capturas `antes_*.png` y `despues_*.png` en esta carpeta.

## Lo que no cubre

- **Escalado de Windows:** solo se probó al 100 %. El 125 % y el 150 % requieren cambiar la configuración de pantalla de
  Windows, y no se tocó la configuración del equipo.
- **Cambio de resolución:** no se probó.
- **Formularios de diálogo** (registro, cambio de contraseña): no entran en el recorrido del menú.
- **Área Operario:** dos corridas después de la corrección (`despues_informe_operario_corrida1.txt` y `…2.txt`), sin
  solapamientos. Las diferencias que cambian de una corrida a otra (Depósitos, Ecopuntos, Registro manual) son textos que
  llegan tarde porque Oracle no responde, no geometría acumulada.
- **Menú lateral de 1 px:** apareció en dos corridas tempranas. El registro de estados mostró que la ventana estaba
  **minimizada sin que el arnés lo pidiera** (algo externo a la aplicación). Ningún código de EcoAccess minimiza la
  ventana. El arnés ahora avisa y restaura antes de medir, y en tres corridas seguidas no volvió a ocurrir.
- **Textos cortados en las capturas del arnés** (fecha y «Área Supervisor» en la cabecera): son del arnés, que no declara
  la escala de pantalla. La captura real `docs/evidencias/winforms/03_dashboard_1366x768.png` muestra la fecha completa.
