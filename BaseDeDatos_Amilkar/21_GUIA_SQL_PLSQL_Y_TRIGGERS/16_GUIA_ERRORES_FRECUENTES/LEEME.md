# 16 · Guía de errores frecuentes

Cuando Oracle muestra un error, lee **el código** (ORA-xxxxx) y **la línea** que indica. Esta tabla dice qué significa, por qué pasa en EcoAccess y cómo se corrige.

## Errores de conexión

| Error | Mensaje | Causa típica | Solución |
|---|---|---|---|
| **ORA-01017** | nombre de usuario/contraseña no válidos; conexión denegada | Contraseña mal escrita, Bloq Mayús activado o usuario equivocado (ECOACCESS_BD, ECOACCESS_PRACTICAS o ECOACCESS_LECTOR). | Vuelve a escribirla con cuidado. **No cambies la contraseña** por tu cuenta; la contraseña local la guarda cada integrante en su propio equipo. |
| ORA-12541 / ORA-12514 | no hay listener / servicio desconocido | Oracle no está iniciado o el servicio no es `FREEPDB1`. | Inicia el servicio `OracleServiceFREE` y el listener; usa `localhost:1521/FREEPDB1`. |
| ORA-28000 | la cuenta está bloqueada | Demasiados intentos fallidos. | Un administrador ejecuta `ALTER USER … ACCOUNT UNLOCK`. |

## Errores de SQL

| Error | Mensaje | Causa típica en EcoAccess | Solución |
|---|---|---|---|
| **ORA-00942** | la tabla o vista no existe | Conexión equivocada (por ejemplo, ejecutar P02 en ECOACCESS_BD, o EcoAccess en PRACTICAS) o nombre mal escrito. | Mira arriba a la derecha qué conexión está elegida; ejecuta `SELECT USER FROM dual;`. |
| **ORA-00904** | identificador no válido | Columna inexistente. El clásico: `deposito.id_usuario`, que **no existe**. | Revisa las columnas en el árbol (Tablas → pestaña Columnas). El titular se obtiene por ORDEN_APERTURA → INTERACCION. |
| **ORA-00001** | restricción única violada | Clave primaria o única repetida, por ejemplo un segundo crédito del mismo depósito (UQ_MOV_CLAVE). | No insertes ids a mano; revisa qué índice único dice el mensaje. |
| **ORA-02291** | restricción de integridad violada: clave principal no encontrada | Se insertó una FK que apunta a una fila inexistente, por ejemplo un depósito con una orden que no existe. | Inserta primero el padre o usa un id que exista. |
| ORA-02292 | se ha encontrado un registro hijo | Se quiso borrar un padre con hijos. | EcoAccess no borra: inactiva o compensa. |
| ORA-02290 | restricción CHECK violada | Valor fuera de dominio (estado 4, puntos 0…). | Lee el nombre del CHECK (`ck_…`) en el mensaje. |
| ORA-01400 | no se puede insertar NULL | Falta una columna NOT NULL. | Completa el dato. |
| ORA-00933 / ORA-00936 | comando SQL no terminado / falta expresión | Coma de más, palabra mal escrita o falta `FROM`. | Revisa la línea que señala el error. |
| ORA-01722 | número no válido | Comparar texto con número, por ejemplo `codigo = 1` cuando `codigo` es texto. | Usa comillas o el tipo correcto. |

## Errores de PL/SQL

| Error | Mensaje | Causa típica | Solución |
|---|---|---|---|
| **ORA-06550** | línea x, columna y: error de PL/SQL | El bloque no compila. Viene acompañado de un PLS-xxxxx (por ejemplo PLS-00201 «identificador debe ser declarado»). | Lee el PLS que sigue, corrige la línea indicada y vuelve a ejecutar. |
| ORA-01403 | no se han encontrado datos | `SELECT … INTO` sin filas. | Captura `NO_DATA_FOUND` o asegúrate de que hay datos. |
| ORA-01422 | la recuperación exacta devuelve más filas | `SELECT … INTO` con varias filas. | Filtra mejor o usa un cursor. |
| ORA-06502 | error numérico o de valor | Texto más largo que la variable. | Declara con `%TYPE`. |
| **ORA-04098** | el disparador no es válido y ha fallado | Un trigger quedó INVALID (no compiló o un objeto del que depende cambió). | `SHOW ERRORS TRIGGER nombre`; recompila con `ALTER TRIGGER nombre COMPILE`. Verifica con `14_verificar_invalidos.sql`. |
| ORA-04091 | la tabla está mutando | Un trigger de fila consulta su misma tabla. | No consultes la tabla del trigger; usa `:OLD`/`:NEW` o tablas relacionadas. |
| ORA-04084 | no se pueden cambiar valores NEW | Asignar `:NEW` en un AFTER. | Usa BEFORE. |
| ORA-14551 | no se puede realizar una operación DML dentro de una consulta | Función con INSERT o UPDATE llamada desde un SELECT. | Llámala desde un bloque PL/SQL. |

## Errores propios de EcoAccess (ORA-20xxx)

`RAISE_APPLICATION_ERROR` usa los números **-20000 a -20999**. Un error 20xxx **no es una falla de Oracle**: es una **regla de negocio** que rechazó la operación a propósito.

| Rango | Quién lo lanza | Ejemplos |
|---|---|---|
| ORA-20100, 20120, 20121 | PKG_CONTEXTO_APP, PR_CAMBIAR_ESTADO_CONTENEDOR | 20100 actor inexistente o inactivo; 20120 rol no permitido; 20121 estado de caneca inválido. |
| ORA-20101 … 20109 | Disparadores 2 y 4 | 20101 verificar el propio depósito; 20102 la caneca no admite el material; 20103 depósito cerrado; 20104 verificador sin rol; 20105 falta el motivo de mal separado; 20106 peso fuera de rango; 20107 depósito nuevo no pendiente; 20108 sensor incoherente; 20109 campo inmutable. |
| ORA-20110 … 20112 | PKG_DEPOSITO | 20111 el depósito no está pendiente; 20112 falta el motivo. |
| ORA-20201 … 20207 | PKG_ECOPUNTOS | 20201 solo se acreditan depósitos verificados; 20203 tarifa ambigua; 20205 no hay crédito para revertir; 20206 y 20207 regla inválida. |
| ORA-20801 … 20803 | PKG_ESTADISTICAS | Periodo, estación o color inválidos. |
| ORA-20810 … 20819 | FN_RESOLVER_CANECA_DISPONIBLE | Estación, material o caneca no disponibles. |
| ORA-20001 | Práctica TRG_VALIDAR_NOTA (ECOACCESS_PRACTICAS) | Nota fuera de 0 a 5. |

La lista completa con el texto exacto está en el código fuente: [`07_procedimientos_funciones_paquetes.sql`](../../04_SCRIPTS_ORACLE/07_procedimientos_funciones_paquetes.sql) y [`08_triggers.sql`](../../04_SCRIPTS_ORACLE/08_triggers.sql).

## Problemas de SQL Developer que no son errores de Oracle

| Síntoma | Causa | Solución |
|---|---|---|
| `PRINT` o `VARIABLE` no hacen nada | Se ejecutó con Ctrl+Enter. | Usa **F5** (ejecutar script). |
| No aparece el texto de DBMS_OUTPUT | Falta `SET SERVEROUTPUT ON`. | Agrégalo arriba del script. |
| Pide un valor «Introducir variables de sustitución» | El script tiene `&variable`. | Escribe el valor o usa `DEFINE variable = valor`. |
| Los cambios desaparecen al cerrar | No se hizo COMMIT (o la demostración terminaba en ROLLBACK, a propósito). | En EcoAccess las demostraciones **no** confirman cambios. |
