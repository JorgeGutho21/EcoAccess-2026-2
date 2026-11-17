# Capturas de Windows Forms (10/11/2026)

Estas capturas las generó `tests/herramientas/EcoAccess.P3.Capturas` con la aplicación de escritorio real:

- los mismos formularios;
- los mismos servicios, compuestos con `Program.ComponerServicios`;
- Oracle Cloud real.

## Cómo se tomaron

- Cada ventana se dibuja fuera de la pantalla visible con `DrawToBitmap`. Así no se toma el escritorio de quien usa el equipo.
- La navegación se hace con clic por código.
- **Estado: VERIFICADO MEDIANTE SIMULACIÓN.** Los datos y los formularios son reales; la interacción no la hizo una persona.
- Escala al 100 %, en 1366×768 y 1920×1080.

Uso:

```powershell
EcoAccess.P3.Capturas.exe <carpeta> <ancho> <alto>
```

## Datos y privacidad

- Para las secciones administrativas el arnés crea un administrador ficticio, `captura.*@example.invalid`, y lo borra al terminar aunque algo falle.
- Los informes `informe_*.txt` listan cada pantalla y sus controles desbordados. El campo «listo en» es la espera fija del arnés, **no** el tiempo de carga.
- Capturas que no se publican:
  - «Ciudadanos y usuarios» (04): muestra correos de cuentas de prueba.
  - «Sistema y Telegram» (13): muestra el identificador interno de la base.

## Hallazgos

- **Sin confirmar:** en el modo invitado el título «¿Sabías que puedes acumular ecopuntos?» sale recortado en la captura. La etiqueta mide lo que su texto necesita, así que puede ser un efecto de `DrawToBitmap`. Falta mirarlo en pantalla real.
- **Pendiente:** la escala del 150 % no se probó, porque exige cambiar la configuración de Windows del propietario.
