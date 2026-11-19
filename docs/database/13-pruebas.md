# 13 · Pruebas de la base de datos

## Cómo se ejecutan

```powershell
# Oracle Cloud (solo lectura): A, B, C, D, E e I
pwsh -File database\scripts\Ejecutar-PruebasBD.ps1

# Esquema aislado local: además la F, que escribe y termina con ROLLBACK
pwsh -File database\scripts\Ejecutar-PruebasBD.ps1 -Secreto ecoaccess-p3-pruebas -IncluirEscritura
```

**Qué hace el script:**
- Lee la conexión de los User Secrets y no muestra la contraseña.
- Ejecuta cada archivo de [`database/tests/`](../../database/tests/).
- Interpreta la línea `RESULTADO X: n correctas, m fallidas` de cada prueba.
- Escribe la evidencia en [`docs/database/evidencias/`](evidencias/).
- **Se niega** a ejecutar la F si la conexión no es un esquema de `localhost`.

**Un integrante con cuenta `ECO_DEV_*`** abre [`database/tests/A2_verificacion_integrante.sql`](../../database/tests/A2_verificacion_integrante.sql) en SQL Developer y lo ejecuta con F5.

| Prueba | Archivo | Qué comprueba | Escribe |
|---|---|---|---|
| A · Conexión | `A_conexion.sql` | Usuario, esquema y servicio; no es una cuenta administrativa; sin privilegios `ANY` | No |
| B · Estructura | `B_estructura.sql` | Las 33 tablas, 21 vistas, 5+5 paquetes, 2 procedimientos, 3 funciones y 5 disparadores; disparadores y FK habilitados; 19 IDENTITY; restricciones clave de canecas y ecopuntos | No |
| C · Compilación | `C_compilacion.sql` | Objetos inválidos y errores `ORA-`/`PLS-` exactos de `USER_ERRORS` | No |
| D · Consultas | `D_consultas_vistas.sql` | Cada vista `VW_*` responde con las columnas documentadas; coherencia con las tablas: una fila por depósito y por caneca, colores válidos, suma de saldos igual al libro, seudónimos | No |
| E · Lógica PL/SQL | `E_plsql_sin_escritura.sql` | Funciones y paquetes con datos correctos, inexistentes, nulos y fuera de rango. Cada validación rechaza **antes** de escribir. Termina con `ROLLBACK` | No |
| F · Flujo de depósito | [`database/05_pruebas/V027_pruebas_capa_academica.sql`](../../database/05_pruebas/V027_pruebas_capa_academica.sql) | Crea datos `IT_V027_*` y recorre el flujo: verificar, acreditar sin duplicar, tarifa por unidad, reverso, corrección, rechazo, disparadores de bloqueo y coherencia, historial de canecas, estadísticas y funciones. Termina con `ROLLBACK` | **Sí**, solo en el esquema aislado |
| H · Integración C# | `tests/integracion/EcoAccess.P3.Datos.Tests` | La capa de datos de la aplicación contra Oracle | **Sí**, solo en el esquema aislado |
| I · Seguridad | dentro del script | Ningún wallet, `.env`, `secrets.json` ni contraseña versionados; patrones del wallet ignorados | No |

## Resultados del 18/11/2026

| Prueba | Resultado | Evidencia | Observaciones |
|---|---|---|---|
| A · Conexión (Oracle Cloud) | **APROBADA** | [pruebas_ecoaccess_p3_app_20261118_1932.md](evidencias/pruebas_ecoaccess_p3_app_20261118_1932.md) | 5/5. `ECOACCESS_P3_APP`, servicio `…_ECOACCESS_tp`, sin privilegios `ANY` |
| B · Estructura (Oracle Cloud) | **APROBADA** | ídem | 19/19 |
| C · Compilación (Oracle Cloud) | **APROBADA** | ídem | 0 inválidos, 0 errores |
| D · Consultas (Oracle Cloud) | **APROBADA** | ídem | 17/17 |
| E · Lógica PL/SQL (Oracle Cloud) | **APROBADA** | ídem | 45/45, 0 omitidas |
| A–E (esquema aislado) | **APROBADA** | [pruebas_ecoaccess_p3_pruebas_20261118_1932.md](evidencias/pruebas_ecoaccess_p3_pruebas_20261118_1932.md) | Los mismos resultados |
| F · Flujo de depósito (esquema aislado) | **APROBADA** | ídem | 27/27. Conteos antes y después iguales (86 usuarios, 101 depósitos, 0 movimientos, 0 auditorías): el `ROLLBACK` no dejó filas |
| G · Instalación desde cero | **BLOQUEADA** | — | Falta un esquema vacío: crearlo requiere la contraseña de SYSTEM del dueño del PC (ver [04-instalacion.md](04-instalacion.md)). Comprobación sustituta: el esquema aislado coincide con el oficial en 154 objetos |
| H · Integración C# (esquema aislado) | **APROBADA** | [integracion_csharp_20261118.md](evidencias/integracion_csharp_20261118.md) | 56 correctas, 0 fallidas, 1 omitida, de las clases de conexión, estación de tres canecas, verificación, ecopuntos y estadísticas |
| V029 · corrección de tildes (H-18), esquema aislado | **APROBADA** | [v029_ensayo_20261118.md](evidencias/v029_ensayo_20261118.md) | Aplicada: 13 objetos y 26 valores. DDL después = antes reparado; repetible. Después, A–F aprobadas ([evidencia](evidencias/pruebas_ecoaccess_p3_pruebas_tras_v029.md)), integración C# 58/0/1 e instalador 51/51 |
| V029 · validación en Oracle Cloud | **APROBADA (solo lectura)** | ídem | Fase 1 sola: 13 objetos y 37 valores reparables, 0 no reversibles |
| V029 · aplicación en Oracle Cloud | **APLICADA** | ídem, sección 3 | 13 objetos y 37 valores; 0 inválidos; 0 textos dañados; conteos de filas iguales. Después, A–E e I aprobadas ([evidencia](evidencias/pruebas_ecoaccess_p3_app_20261118_2021.md)) |
| I · Seguridad | **APROBADA** | evidencias A–E | 0 archivos secretos versionados; 0 archivos con la contraseña; 5/5 patrones ignorados |
| A2 · Verificación de integrante | **NO EJECUTADA** con una cuenta real | — | Aún no existe ninguna cuenta `ECO_DEV_*`. Se probó con la cuenta dueña: I3–I7 pasan; I1 e I2 fallan, **como deben**, porque no es una cuenta de integrante. Gracias a esa prueba apareció el hallazgo H-17 |

**Errores que aparecieron durante el trabajo y se corrigieron en las pruebas, no en la base:**
- comparación booleana no admitida por SQL de 19c (D);
- intervalo `'365' DAY` sin precisión (E);
- suposición equivocada sobre el «top 5» de `ranking_verificados`. Era `DENSE_RANK`, y quedó documentado en el hallazgo H-06.

**Oracle Cloud no se modificó:** las pruebas que corrieron allí no tienen `INSERT`, `UPDATE` ni `DELETE`, y E termina con `ROLLBACK`. La única fila nueva que apareció en esas horas es una visita del simulador de las 17:41, anterior a esta fase (hallazgo H-15).

## Qué falta (requiere intervención humana)

| Pendiente | Quién | Cómo |
|---|---|---|
| G · Instalación desde cero | Dueño del PC (contraseña de SYSTEM local) | `scripts\pruebas\Crear-Esquema-Aislado-Local.ps1 -Esquema ECOACCESS_P3_PRUEBAS_DEV -IdSecretos ecoaccess-p3-dev`, y después los pasos de [04-instalacion.md](04-instalacion.md) |
| Verificación de integrantes en Oracle Cloud | ADMIN de Oracle Cloud | Crear las cuentas `ECO_DEV_*` con `06_crear_cuenta_integrante.sql`; cada integrante ejecuta `A2_verificacion_integrante.sql` |
| Código de los objetos académicos de `ECOACCESS_BD` | Quien conozca su contraseña actual | Exportarlos con `Exportar-EsquemaOracle.ps1` desde una conexión propia, si se quieren comparar con los oficiales |
