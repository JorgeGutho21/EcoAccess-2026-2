# Integración con Programación III

EcoAccess se desarrolla en dos asignaturas que describen **el mismo sistema**, pero en **bases de datos distintas que no se mezclan**.

| | Base de Datos (esta carpeta) | Programación III |
|---|---|---|
| Esquema | `ECOACCESS_BD` (más `ECOACCESS_PRACTICAS` para las tareas) | `ECOACCESS_P3_APP` |
| Dónde | Oracle 26ai Free local, `localhost:1521/FREEPDB1` | Oracle Autonomous Database **ECOACCESS**, región `sa-bogota-1`, alias `ecoaccess_tp`, con wallet |
| Tablas | 19 | 29 en los scripts de la rama más avanzada verificada (`desarrollo/telegram-doble-ia`, commit `1fa52ac`, migraciones V002–V025); `main` llega a V017 |
| Datos | Sintéticos, para estudiar y exponer | Operativos de la aplicación |
| Código | Esta carpeta `BaseDeDatos_Amilkar/` | `src/`, `database/`, `hardware/` y `tests/` del repositorio |

**Los dos modelos físicos no son idénticos.** 17 tablas existen en ambos (contando `CONSULTA_VISION`, que P3 creó en V023 con SQL dinámico). Hay 3 tablas solo académicas, sin equivalente en P3: `AUDITORIA_DEPOSITO`, `HISTORIAL_ESTADO_CONTENEDOR` y `PESO_REFERENCIAL`. P3, por su parte, tiene tablas operativas que el modelo académico no necesita. Las diferencias de nombres y roles (por ejemplo, «Supervisor» en P3 frente a «Administrador» aquí) están en la matriz.

## Archivos

| Archivo | Qué es |
|---|---|
| `MATRIZ_P3_vs_ACADEMICO.md` / `.csv` | Correspondencia tabla por tabla entre P3 y el modelo académico. |
| `comparar_modelos_p3.ps1` | Regenera la matriz leyendo las migraciones de P3 desde GitHub, en solo lectura. |
| `VERIFICACION_GITHUB_Y_NUBE_20261111.md` | Verificación de ramas, commits y migraciones de P3 en GitHub, y la identificación de Oracle Cloud. |
| `inventario_oracle_cloud_SOLO_LECTURA.sql` y `ejecutar_inventario_cloud.ps1` | Inventario de la nube en una transacción `READ ONLY`, solo con SELECT; pide la contraseña en la terminal. |
| `PROPUESTA_lector_bi_p3_NO_EJECUTADO.sql` | Propuesta de una cuenta lectora para Power BI sobre P3. **No se ejecutó**: requiere autorización sobre Oracle Cloud. |

## Estado

- **Verificación de GitHub (solo lectura):** VERIFICADA.
- **Inventario de Oracle Cloud:** PENDIENTE. Necesita que la persona dueña escriba la contraseña de `ECOACCESS_P3_APP` en la terminal. No se modificó nada en la nube.
