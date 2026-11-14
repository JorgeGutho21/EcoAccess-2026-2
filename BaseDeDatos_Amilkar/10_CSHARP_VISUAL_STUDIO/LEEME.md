# C# y Visual Studio

Esta carpeta contiene la **aplicación académica** de la asignatura. No es la aplicación de Programación III: esa vive en las carpetas `src/` del repositorio y aquí no se duplica.

## La solución `SOLUCION_CSHARP/EcoAccessBD.sln`

| Proyecto | Qué hace |
|---|---|
| `EcoAccessBD.Datos` | Capa de acceso a Oracle con `Oracle.ManagedDataAccess.Core` (ODP.NET). Llama a los procedimientos del paquete `pkg_empleado_crud` y lee cursores. |
| `EcoAccessBD.Consola` | Prueba de consola con 10 casos: conexión, consulta, actualización, INSERT dentro de una transacción que termina en ROLLBACK, error controlado de Oracle y comprobación de que no existe DELETE. |
| `EmpleadosCrud.WinForms` | Ventana CRUD sin botón Eliminar: consultar, insertar y actualizar empleados de práctica y mostrar los errores de Oracle (por ejemplo, ORA-20002). |

## El flujo C# → Oracle

1. La aplicación lee el usuario y la contraseña de las variables de entorno `PRACTICAS_USUARIO` y `PRACTICAS_CLAVE`; no hay contraseñas en el código.
2. Abre una conexión a `localhost:1521/FREEPDB1` con `OracleConnection`.
3. Para modificar datos llama a procedimientos almacenados (`OracleCommand` con `CommandType.StoredProcedure`); la regla «sin DELETE» la garantiza el paquete.
4. Para leer, recibe un `SYS_REFCURSOR` y lo carga en un `DataTable` que muestra la grilla.
5. Si Oracle rechaza algo, el código de error (ORA-xxxxx) se muestra al usuario sin cerrar la aplicación.

## Procedimientos utilizados

Están en el paquete `pkg_empleado_crud` (insertar, consultar y actualizar) de `05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/P05_crud_sin_delete.sql`, en el esquema `ECOACCESS_PRACTICAS`.

## Pruebas y capturas

- **Prueba de consola:** 10 de 10 aprobadas (`16_EVIDENCIAS_Y_PRUEBAS/salidas/16_prueba_csharp.txt`). El instalador la ejecuta en el paso N01 y compila la ventana en el N02.
- **Capturas de la ventana:** consultar, insertar, actualizar y error de Oracle (`16_EVIDENCIAS_Y_PRUEBAS/capturas_csharp/`), obtenidas por automatización de la interfaz. Para no insertar un empleado nuevo en cada ejecución, solo se vuelven a tomar con `INSTALAR_Y_PROBAR.ps1 -CapturasCSharp`.

La guía paso a paso (abrir en Visual Studio, restaurar NuGet, configurar las variables y ejecutar) está en `11_GUIAS_PASO_A_PASO/GUIA_CSHARP_VISUAL_STUDIO.pdf`.
