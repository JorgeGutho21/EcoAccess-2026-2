# Evidencias y pruebas

| Carpeta o archivo | Qué contiene |
|---|---|
| `RESUMEN_ULTIMA_EJECUCION.md` | El resultado de la última ejecución de `INSTALAR_Y_PROBAR.ps1`: cada comprobación con PASA o FALLA. |
| `salidas/ultima_ejecucion/` | Una salida de SQL*Plus (SPOOL) por paso de esa ejecución, sin contraseñas. |
| `salidas/` | Salidas reales guardadas: instalación, 153 pruebas del esquema (`12_pruebas.txt`), consultas de exposición, objetos inválidos, comentarios, cuenta lectora, prueba de C#, banco de 309 ejercicios, prácticas (P00–P05 y P99), demostración de los cuatro triggers, 173 SELECT del diccionario y modelo DAX de Power BI. |
| `capturas_csharp/` | La ventana CRUD de C#: consultar, insertar, actualizar y error de Oracle, más el registro de la demostración. |
| `capturas_power_bi/` | Las cinco páginas del tablero, el filtro por caneca verde, los botones de navegación, el PBIX reabierto con el glosario y la captura del error que tenía el formato antiguo antes de convertirlo a PBIR. |

Las capturas de Data Modeler y la verificación del `.dmd` están en `02_MODELOS_DE_DATOS/07_EVIDENCIAS/`, junto al modelo que comprueban.

## Cómo volver a generar las evidencias

```
pwsh -File .\INSTALAR_Y_PROBAR.ps1
```

El instalador sobrescribe `RESUMEN_ULTIMA_EJECUCION.md` y `salidas/ultima_ejecucion/`. Si el esquema ya existe, no reinstala ni borra nada. Las capturas de C# solo se vuelven a tomar con `-CapturasCSharp`, porque insertan un empleado de demostración.

## Lo que no tiene evidencia todavía

- **Capturas de la hoja de trabajo de SQL Developer con conexión:** requieren que la persona dueña escriba la contraseña en la ventana de conexión.
- **Prueba de restauración del respaldo Data Pump:** no se ejecutó, porque reemplaza el esquema y requiere autorización.
- **Power BI con conexión directa a Oracle e inventario de Oracle Cloud:** ver `00_EMPEZAR_AQUI/ESTADO_DEL_PROYECTO.md`.
