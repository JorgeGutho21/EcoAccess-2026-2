# Base de datos Oracle

EcoAccess P3 persiste toda su información en Oracle Database. El acceso desde C# usa ADO.NET con `Oracle.ManagedDataAccess` y consultas parametrizadas.

## Carpetas

| Carpeta | Contenido |
|---|---|
| `00_administracion/` | creación manual del usuario y verificación de permisos |
| `01_modelo/` | diagrama entidad-relación y diccionario de datos |
| `02_tablas/` | creación inicial de tablas |
| `03_datos_prueba/` | catálogos, catálogo ambiental y datos ficticios de demostración |
| `04_consultas/` | consultas de estadísticas, auditoría y agregados para informes |
| `05_pruebas/` | pruebas SQL de restricciones |
| `06_migraciones/` | cambios numerados `V0NN__*.sql`; una migración aplicada no se edita |
| `schema/` | DDL **real** extraído de Oracle Cloud, un archivo por objeto, más el [inventario](schema/INVENTARIO.md) y la instalación en un esquema vacío |
| `scripts/` | `Exportar-EsquemaOracle.ps1` (regenera `schema/`) y `Ejecutar-PruebasBD.ps1` (pruebas A–F e I con evidencia) |
| `tests/` | Pruebas A–E (solo lectura), verificación de integrantes y ejercicios académicos |

**Documentación completa** (arquitectura, conexión, diccionario, vistas, paquetes, pruebas y guía para compañeros): [`docs/database/`](../docs/database/README.md).

## Instalación

```powershell
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Preparar-BaseDatos.ps1
```

El script crea el esquema en el contenedor `oracle-free-p3`, guarda la conexión en los User Secrets de Windows sin mostrarla y aplica en orden los pasos pendientes, anotándolos en `registro_migracion`. Si se repite, no vuelve a ejecutar lo ya aplicado. No borra objetos ni datos.

## Reglas

- Las contraseñas nunca se guardan en el repositorio.
- Los datos de prueba son ficticios y se mantienen separados de los reales.
- No se inventan pesos ni se marca como reciclado un material sin verificación.