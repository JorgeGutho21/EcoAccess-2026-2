# Plan de reversión — EcoAccess BD (esquema académico)

## Qué se instala y dónde
- **Base de datos:** Oracle local `FREEPDB1` (`localhost:1521`). Nunca Oracle Cloud.
- **Usuarios** (los crea `01_esquema_practicas.sql`):
  - `ECOACCESS_BD`: el modelo;
  - `ECOACCESS_PRACTICAS`: ejercicios de clase;
  - `ECOACCESS_BD_LECTOR`: Power BI.
- **Rol:** `ROL_ECO_LECTURA_BI`.
- **No se tocan:** `ECOACCESS_P3`, `ECOACCESS_P3_PRUEBAS`, `HR` (solo se lee) ni la nube.

## Casos y qué hacer

| Situación | Acción | Destructivo |
|---|---|---|
| Un script falló a mitad (por ejemplo 07) | Corrige y vuelve a ejecutar **ese** script: 06–09 usan `CREATE OR REPLACE` | No |
| 02 falló con ORA-20011 (instalación parcial) | Ejecuta `99_reversion_esquema_academico.sql` y vuelve a 02 | **Sí**, pero solo en ECOACCESS_BD |
| Quiero regenerar los datos sintéticos | Ejecuta `99_reversion_esquema_academico.sql` y luego 02 → 14 | **Sí**, pero solo en ECOACCESS_BD |
| Quiero quitar todo el proyecto académico del PC | Como SYSTEM: `DROP USER ecoaccess_bd CASCADE;` (y lo mismo con los otros dos usuarios), y `DROP ROLE rol_eco_lectura_bi;` | **Sí**. No forma parte de la instalación; se hace solo por decisión expresa |
| Quiero deshacer una prueba | No hace falta: `12_pruebas.sql` termina con `ROLLBACK` | No |

## Comprobación después de revertir o reinstalar

```sql
SELECT COUNT(*) FROM user_objects;            -- 0 tras revertir
@14_verificar_invalidos.sql                    -- 0 inválidos tras reinstalar
```

## Por qué no se usa `DROP USER … CASCADE` como instalación
Un instalador que borra el usuario cada vez puede destruir datos por error: basta con apuntar a la conexión equivocada. Por eso la instalación es **aditiva**:
- 02 se detiene si ya hay tablas;
- 11 se detiene si ya hay datos sintéticos.

Borrar requiere este script aparte, con una confirmación escrita.
