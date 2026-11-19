# Evidencia de pruebas de base de datos · ECOACCESS_P3_APP

- Fecha: 2026-11-18 20:21 (hora del equipo)
- Base: Oracle Cloud (alias ecoaccess_tp)
- Esquema: `ECOACCESS_P3_APP`
- Generado por `database/scripts/Ejecutar-PruebasBD.ps1`
- La contraseña no aparece en este archivo: se envía a SQL*Plus por la entrada estándar.

| Prueba | Resultado | Evidencia | Observaciones |
|---|---|---|---|
| A · Conexión | **APROBADA** | `database/tests/A_conexion.sql` | 5 correctas, 0 fallidas |
| B · Estructura | **APROBADA** | `database/tests/B_estructura.sql` | 19 correctas, 0 fallidas |
| C · Compilación | **APROBADA** | `database/tests/C_compilacion.sql` | 2 correctas, 0 fallidas |
| D · Consultas a las vistas | **APROBADA** | `database/tests/D_consultas_vistas.sql` | 17 correctas, 0 fallidas |
| E · Lógica PL/SQL sin escrituras | **APROBADA** | `database/tests/E_plsql_sin_escritura.sql` | 45 correctas, 0 fallidas |
| I · Seguridad del repositorio | **APROBADA** | `.gitignore` | archivos secretos versionados: 0; archivos que contienen la contraseña de esta conexión: 0; patrones de wallet sin ignorar: 0 |
| F · Flujo de depósito y ecopuntos | **NO EJECUTADA** | `database/05_pruebas/V027_pruebas_capa_academica.sql` | escribe datos: solo en el esquema aislado (`-Secreto ecoaccess-p3-pruebas -IncluirEscritura`) |

## Detalle

### A · Conexión

Archivo: `database/tests/A_conexion.sql`

```text
USUARIO 		 ESQUEMA		  SERVICIO					   BASE
------------------------ ------------------------ ------------------------------------------------ ----------------------------
ECOACCESS_P3_APP	 ECOACCESS_P3_APP	  G5AB3C4722E812D_ECOACCESS_tp.adb.oraclecloud.com G5AB3C4722E812D_ECOACCESS

Oracle 19.0.0.0.0
OK    A1 la sesión está abierta
OK    A2 no es una cuenta administrativa (SYS, SYSTEM, ADMIN)
OK    A3 el esquema actual es el del usuario (sin ALTER SESSION SET CURRENT_SCHEMA)
OK    A4 tiene CREATE SESSION
OK    A5 no tiene privilegios de sistema «ANY» ni de administración (mínimo privilegio)
RESULTADO A: 5 correctas, 0 fallidas.

PL/SQL procedure successfully completed.
```

### B · Estructura

Archivo: `database/tests/B_estructura.sql`

```text
OK    B TABLE: 33 esperados
OK    B VIEW: 21 esperados
OK    B PACKAGE: 5 esperados
OK    B PACKAGE BODY: 5 esperados
OK    B PROCEDURE: 2 esperados
OK    B FUNCTION: 3 esperados
OK    B TRIGGER: 5 esperados
OK    B todos los disparadores están habilitados
OK    B 19 columnas IDENTITY (secuencias ISEQ$$ de sistema)
OK    B restricción CK_CONFIGURACION_CATEGORIA (C) en CONFIGURACION_CANECA
OK    B restricción CK_CONTENEDOR_ESTADO (C) en CONTENEDOR
OK    B restricción CK_MATERIAL_CATEGORIA (C) en MATERIAL
OK    B restricción UQ_DEPOSITO_ORDEN (U) en DEPOSITO
OK    B restricción CK_DEPOSITO_VERIFICACION (C) en DEPOSITO
OK    B restricción UQ_ECOP_OPERACION_CLAVE (U) en OPERACION_ECOPUNTOS
OK    B restricción UQ_ECOP_REVERSO (U) en MOVIMIENTO_ECOPUNTOS
OK    B restricción FK_ECOP_MOV_REVERTIDO (R) en MOVIMIENTO_ECOPUNTOS
OK    B índice único UQ_ECOP_CREDITO_INICIAL (un solo crédito inicial por depósito)
OK    B todas las claves foráneas están habilitadas
RESULTADO B: 19 correctas, 0 fallidas.

PL/SQL procedure successfully completed.
```

### C · Compilación

Archivo: `database/tests/C_compilacion.sql`

```text
no rows selected


no rows selected

OK    C1 objetos inválidos: 0
OK    C2 errores de compilación (USER_ERRORS): 0
RESULTADO C: 2 correctas, 0 fallidas.

PL/SQL procedure successfully completed.
```

### D · Consultas a las vistas

Archivo: `database/tests/D_consultas_vistas.sql`

```text
OK    D VW_BI_AUDITORIA responde (0 filas) y tiene las columnas documentadas
OK    D VW_BI_CIUDADANOS responde (74 filas) y tiene las columnas documentadas
OK    D VW_BI_CONSULTAS_VISION responde (34 filas) y tiene las columnas documentadas
OK    D VW_BI_DEPOSITOS responde (113 filas) y tiene las columnas documentadas
OK    D VW_BI_ECOPUNTOS responde (0 filas) y tiene las columnas documentadas
OK    D VW_CANECA_ESTACION responde (9 filas) y tiene las columnas documentadas
OK    D VW_DEPOSITO_DETALLE responde (113 filas) y tiene las columnas documentadas
OK    D VW_RECORRIDO responde (124 filas) y tiene las columnas documentadas
OK    D VW_SALDO_ECOPUNTOS responde (74 filas) y tiene las columnas documentadas
OK    D VW_DEPOSITO_DETALLE tiene una fila por depósito (113 = 113)
OK    D VW_BI_DEPOSITOS tiene una fila por depósito
OK    D VERIFICADO en la vista coincide con ID_MATERIAL_VERIFICADO no nulo
OK    D VW_CANECA_ESTACION tiene una fila por caneca (9)
OK    D toda caneca configurada es BLANCA, VERDE o NEGRA
OK    D la suma de VW_SALDO_ECOPUNTOS es la suma del libro de movimientos de ciudadanos (0)
OK    D VW_RECORRIDO cubre todas las interacciones (124 filas para 124)
OK    D VW_BI_CIUDADANOS seudonimiza (C-0000), sin nombre ni correo
RESULTADO D: 17 correctas, 0 fallidas.

PL/SQL procedure successfully completed.
```

### E · Lógica PL/SQL sin escrituras

Archivo: `database/tests/E_plsql_sin_escritura.sql`

```text
OK    E1.1 FN_RESOLVER_CANECA_DISPONIBLE(estación 1015, material 4) = caneca 1093 (devolvió 1093)
OK    E1.2 identificadores nulos -> ORA-20810
OK    E1.3 identificador negativo -> ORA-20810
OK    E1.4 identificador con decimales -> ORA-20810
OK    E1.5 estación inexistente -> ORA-20818
OK    E1.6 material inexistente -> ORA-20811
OK    E1.7 material peligroso va a ruta especial -> ORA-20813
OK    E2.1 FN_PESO_ESTIMADO_KG con referencia nula devuelve NULL
OK    E2.2 FN_PESO_ESTIMADO_KG con 0 unidades devuelve NULL
OK    E2.3 FN_PESO_ESTIMADO_KG con referencia inexistente devuelve NULL
OK    E2.4 FN_PESO_ESTIMADO_KG(ref 1, 3) = 3 × 120 g / 1000
OK    E3.1 FN_DEPOSITOS_CIUDADANO(4977) devuelve 2 filas = VW_DEPOSITO_DETALLE
OK    E3.2 FN_DEPOSITOS_CIUDADANO de un usuario inexistente devuelve un cursor vacío
OK    E4.1 PKG_ECOPUNTOS.saldo(244) = VW_SALDO_ECOPUNTOS (0)
OK    E4.2 PKG_ECOPUNTOS.saldo de un usuario inexistente es 0
OK    E4.3 PKG_ECOPUNTOS.regla_vigente encuentra la tarifa vigente del material 4
OK    E4.4 PKG_ECOPUNTOS.regla_vigente sin tarifa devuelve NULL (no hay puntos inventados)
OK    E4.5 PKG_ECOPUNTOS.credito_vigente de un depósito inexistente es NULL
OK    E4.6 acreditar un depósito PENDIENTE se rechaza -> ORA-20201
OK    E4.7 acreditar un depósito inexistente -> ORA-20110
OK    E4.8 revertir sin motivo -> ORA-20112
OK    E5.1 estado de un depósito inexistente -> ORA-20110
OK    E5.2 PKG_DEPOSITO.estado de un depósito con material verificado = 1
OK    E5.3 PKG_DEPOSITO.estado de un depósito sin verificar es 0 o 2
OK    E5.4 rechazar sin motivo -> ORA-20112
OK    E5.5 un ciudadano no puede verificar -> ORA-20120
OK    E5.6 corrección con estado inválido -> ORA-20113
OK    E5.7 no hay corrección en curso fuera de PKG_DEPOSITO
OK    E6.1 tiene_rol(administrador 4521, 3) = 1
OK    E6.2 tiene_rol(ciudadano 244, 3) = 0
OK    E6.3 exigir_rol de administrador a un ciudadano -> ORA-20120
OK    E6.4 fijar_actor de un usuario inexistente -> ORA-20100
OK    E6.5 sin actor fijado, actor es NULL
OK    E7.1 un ciudadano no cambia el estado de una caneca -> ORA-20120
OK    E7.2 estado de caneca inválido (9) -> ORA-20121
OK    E8.1 PKG_ESTADISTICAS.a_utc: 1/1/2026 00:00 en Bogotá = 05:00 UTC
OK    E8.2 consultar_resumen del último año cuenta 113 depósitos = conteo directo
OK    E8.3 verificados + pendientes_o_rechazados = depósitos
OK    E8.4 periodo invertido -> ORA-20801
OK    E8.5 periodo de más de 400 días -> ORA-20801
OK    E8.6 estación 0 -> ORA-20802
OK    E8.7 categoría 9 -> ORA-20803
OK    E8.8 ranking_verificados(top 5): 16 filas, último puesto 5 (<= 5) y en orden
OK    E8.9 PR_CONSULTAR_ESTADISTICAS (V022) cuenta 113 depósitos = conteo directo
OK    E8.10 PR_CONSULTAR_ESTADISTICAS con categoría 9 -> ORA-20803
RESULTADO E: 45 correctas, 0 fallidas, 0 omitidas.

PL/SQL procedure successfully completed.
```

