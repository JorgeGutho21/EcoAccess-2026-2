# 05. Presentación

Proyecto `EcoAccess.P3.Presentacion`: Windows Forms sobre net48, con Krypton, FontAwesome y LiveCharts. `Program.cs` es la raíz de composición; los formularios solo usan servicios de Logica.

## Inicio

Comprueba Oracle antes de abrir. Si falta la conexión, `FormActivacionOracle` guía la configuración y solo guarda lo que Oracle aceptó.

## Pantalla principal del operador

**Registro manual** y **Utilizar código QR** encabezan las acciones.

`FormRegistroManual`: buscar o registrar al ciudadano, elegir caneca, buscar el residuo por nombre o sinónimo, ver si la caneca lo admite, solicitar la apertura, seguir los hechos registrados y confirmar la recepción con la cantidad. Los ecopuntos se acreditan en el módulo Ecopuntos.

**Utilizar código QR** abre el seguimiento de operaciones: el ciudadano continúa en Telegram y el operador consulta el estado sin duplicar lógica.

## Otros módulos

Materiales, canecas y QR, puntos de instalación, depósitos, ecopuntos, estadísticas, valoraciones, dashboard institucional, explorar y Próximamente.

## Verificación visual

| Revisión | Resultado |
|---|---|
| Bienvenida en net48, logo, iconos y gráficos | correcta |
| Registro manual a 1106×648 y 1660×960 con datos de Oracle | sin barras de desplazamiento; capturas revisadas |
| Escala de Windows actual | comprobada |
| Otras escalas (125 %, 150 %) y equipo real de 1366×768 | **pendiente** |