# Verificación de GitHub y Oracle Cloud (Programación III) — 11/11/2026, noche

Todo lo de este archivo se hizo **en solo lectura**: no se hizo ningún commit, push, cambio de rama ni modificación en el repositorio P3, y no se ejecutó nada que escriba en Oracle Cloud.

## 1. Repositorio `JorgeGutho21/EcoAccess-P3` (privado)

Consultado con `gh api` y con una copia temporal (`git clone --no-checkout`) fuera del proyecto.

| Rama | Último commit (al consultar) | Migraciones versionadas |
|---|---|---|
| `main` (rama por defecto) | `d30de6d` · 02/11/2026 | V002–V017 (16) |
| `desarrollo/telegram-doble-ia` (PR #11, abierto) | `1fa52ac` · 11/11/2026 20:17 (−05:00) | V002–V025 (24) + reversiones R023–R025 |
| `desarrollo/auditoria-tercer-corte` | `a1af3e6` · 11/11/2026 | V002–V025 (24) |
| `recovery/ecoaccess-oracle-cloud` | `8a14d3b` · 09/11/2026 | V002–V021 (20) |
| `desarrollo/oracle` | `79e840d` · 02/11/2026 | V002–V015 (14) |

**Correcciones a informes anteriores:**

- El informe de cierre anterior decía «V022–V023 no están versionadas». **Ya no es cierto.** Las ramas `desarrollo/telegram-doble-ia` y `desarrollo/auditoria-tercer-corte` traen V022 (objetos PL/SQL de estadísticas), V023 (`CONSULTA_VISION`), V024 (estación piloto de tres canecas) y V025 (lote de simulación).
- La rama `desarrollo/telegram-doble-ia` cambió de commit (antes `afede69`, ahora `1fa52ac`), y hay otra sesión de trabajo activa en P3. Por eso **ninguna** rama se toma como «definitiva» en este documento. La comparación se hizo contra la rama más avanzada en ese momento.
- `main` está atrasada (V017): lo que está en producción en la nube no se puede deducir de `main`.

## 2. Comparación académico vs P3 (regenerada)

`MATRIZ_P3_vs_ACADEMICO.md` y `.csv` se regeneraron con `comparar_modelos_p3.ps1` sobre `origin/desarrollo/telegram-doble-ia` (`1fa52ac`).

- **Tablas:** 19 académicas; 28 tablas en los scripts de P3 detectadas por el comparador; 16 en común.
- **Corrección manual — `CONSULTA_VISION`:** V023 de P3 crea una tabla `CONSULTA_VISION` con `EXECUTE IMMEDIATE` (SQL dinámico), y el comparador, que lee `CREATE TABLE` literales, no la detecta.
  - Desde V023, la `CONSULTA_VISION` académica tiene un equivalente **con el mismo nombre** en P3 (además de `ANALISIS_IMAGEN`).
  - Comparten la misma regla: «una consulta de foto NO es un depósito».
  - En consecuencia, P3 tiene en realidad **29** tablas en scripts y **17** en común con el modelo académico.
- **Roles:** el PR #11 de P3 habla de accesos «Ciudadano, Operario y Supervisor». El modelo académico usa CIUDADANO, OPERARIO y ADMINISTRADOR (códigos 1, 2 y 3).
  - En los scripts SQL de P3 la palabra SUPERVISOR solo aparece en `database/05_pruebas/01_pruebas_restricciones.sql`.
  - Es una diferencia de **nombre en la interfaz** que debe aclararse con el equipo de P3; no se cambió el modelo académico.
- **Reglas de negocio compartidas:**
  - foto ≠ depósito;
  - depósito pendiente → verificado por otra persona;
  - EcoPuntos como libro de créditos y reversos, con idempotencia;
  - tres canecas por estación con el QR compartido;
  - peso estimado ≠ peso medido;
  - fechas en UTC.
- **No se migra nada:** la integración propuesta es por vistas de lectura (`EA_R_*`) y una cuenta lectora. Ver `PROPUESTA_lector_bi_p3_NO_EJECUTADO.sql`: no se ejecutó.

## 3. Oracle Cloud (Autonomous Database `ECOACCESS`, alias `ecoaccess_tp`, esquema `ECOACCESS_P3_APP`)

- Se preparó `ejecutar_inventario_cloud.ps1`.
  - Abre SQL*Plus con el wallet de `TNS_ADMIN`.
  - **La persona dueña de la cuenta escribe la contraseña** en la terminal; la contraseña no se guarda en ningún archivo.
  - Inicia la sesión con `SET TRANSACTION READ ONLY` y ejecuta solo los `SELECT` de `inventario_oracle_cloud_SOLO_LECTURA.sql`.
  - Guarda la salida en `16_EVIDENCIAS_Y_PRUEBAS/salidas/inventario_oracle_cloud_<fecha>.txt`.
- **Estado: PENDIENTE de la contraseña.** El 11/11/2026 a las 20:09 el script quedó esperando la contraseña en la terminal y no se escribió.
- Mientras no exista ese archivo de salida, el número real de tablas, las migraciones instaladas, los objetos inválidos y el estado de canecas y depósitos en la nube **no están verificados**.
