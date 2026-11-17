# CRUD de empleado sin eliminación (PL/SQL + C#)

- **Fecha de la clase:** por verificar.
- **Enunciado:** desde una aplicación (C# o Java), insertar, consultar y modificar empleados mediante procedimientos almacenados, **sin ninguna operación de eliminación**.

## Solución verificada

| Parte | Archivo |
|---|---|
| Tabla de práctica, secuencia (inicia en 1000), trigger de PK y paquete `pkg_empleado_crud` (insertar, consultar y actualizar; sin DELETE) | `05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P05_crud_sin_delete.sql` (esquema `ECOACCESS_PRACTICAS`) |
| Aplicación de ventana (WinForms) y prueba de consola | `10_CSHARP_VISUAL_STUDIO/SOLUCION_CSHARP/` |
| Guía paso a paso en Visual Studio | `11_GUIAS_PASO_A_PASO/GUIA_CSHARP_VISUAL_STUDIO.pdf` |

## Evidencia

- **Pruebas P99 (CR01–CR04):** el CRUD funciona y no existe ninguna operación de borrado.
- **Prueba de consola C# (10 casos):** aprobó 10 de 10. Reutiliza su fila de prueba y comprueba el INSERT dentro de una transacción que termina en ROLLBACK (`16_EVIDENCIAS_Y_PRUEBAS/salidas/16_prueba_csharp.txt`).
- **Capturas de la ventana:** consultar, insertar, actualizar y error de Oracle (`16_EVIDENCIAS_Y_PRUEBAS/capturas_csharp/`). Se obtuvieron por automatización de la interfaz.
- **Filas de prueba antiguas:** quedan 9 filas de ejecuciones anteriores (ids 1004–1080) en `ECOACCESS_PRACTICAS.EMPLEADO_PRACTICA`. No se borraron, porque la práctica es precisamente «sin DELETE».
