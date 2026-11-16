# 21 · Guía SQL, PL/SQL, triggers y consultas de EcoAccess

**EcoAccess · Base de Datos · Profesor Amílkar · Universidad Popular del Cesar**
Integrantes: Jorge Alfonso Gutiérrez Thomas · Heyder Hedrey Canchila de Ángel · Saray Marcela Cadena Palomino

## Qué contiene y para qué sirve

Es un **manual práctico para estudiar y ejecutar en SQL Developer**. Empieza en lo más sencillo (conectarse y escribir un `SELECT`) y llega a los procedimientos, paquetes, secuencias y triggers reales de EcoAccess. Cada tema trae:

1. qué significa;
2. para qué sirve;
3. por qué se utiliza;
4. cuándo se utiliza;
5. su estructura;
6. un ejemplo básico;
7. un ejemplo de EcoAccess;
8. una explicación línea por línea;
9. el resultado que debe aparecer;
10. cómo ejecutarlo en SQL Developer;
11. los errores frecuentes;
12. las preguntas que podría hacer el profesor.

El manual completo en un solo PDF es [`00_MANUAL_COMPLETO_SQL_PLSQL.pdf`](00_MANUAL_COMPLETO_SQL_PLSQL.pdf). Los archivos `.sql` de cada carpeta son los que se ejecutan.

## Qué programa abrir y con qué conexión

- **Programa:** Oracle SQL Developer.
- **Conexión:** `EcoAccess BD LOCAL`, con el usuario `ECOACCESS_BD`, en `localhost:1521/FREEPDB1`. Las prácticas del profesor usan `ECOACCESS_PRACTICAS`.
- **Contraseña:** la escribes tú cuando SQL Developer la pida. No está en ningún archivo de esta carpeta.

## Orden de estudio

| Paso | Carpeta | Tema | ¿Modifica datos? |
|---:|---|---|---|
| 1 | [01_CONEXIONES_Y_PRIMEROS_COMANDOS](01_CONEXIONES_Y_PRIMEROS_COMANDOS/LEEME.md) | SQL*Plus, SQL Developer, Ctrl+Enter y F5 | No |
| 2 | [02_SELECT_Y_CONSULTAS_BASICAS](02_SELECT_Y_CONSULTAS_BASICAS/LEEME.md) | SELECT, columnas, alias, ORDER BY | No |
| 3 | [03_WHERE_JOINS_GROUP_BY_HAVING](03_WHERE_JOINS_GROUP_BY_HAVING/LEEME.md) | Filtrar, unir tablas, agrupar | No |
| 4 | [04_BLOQUES_ANONIMOS_Y_VARIABLES](04_BLOQUES_ANONIMOS_Y_VARIABLES/LEEME.md) | DECLARE, BEGIN, END, %TYPE, IF | No |
| 5 | [05_CURSORES](05_CURSORES/LEEME.md) | Recorrer filas una a una | No |
| 6 | [06_PROCEDIMIENTOS](06_PROCEDIMIENTOS/LEEME.md) | Programas que realizan una operación | No (llamadas de lectura) |
| 7 | [07_FUNCIONES](07_FUNCIONES/LEEME.md) | Programas que devuelven un valor | No |
| 8 | [08_PAQUETES_PLSQL](08_PAQUETES_PLSQL/LEEME.md) | Los 4 paquetes de EcoAccess | No (llamadas de lectura) |
| 9 | [09_TRIGGERS_ECOACCESS](09_TRIGGERS_ECOACCESS/LEEME.md) | Los 4 disparadores reales | Solo la demostración, que termina en ROLLBACK |
| 10 | [10_TAREA_TRIGGERS_PROFESOR_AMILKAR](10_TAREA_TRIGGERS_PROFESOR_AMILKAR/LEEME.md) | Los 4 puntos de la tarea de triggers | Solo en `ECOACCESS_PRACTICAS` |
| 11 | [11_SECUENCIAS_Y_CLAVES_PRIMARIAS](11_SECUENCIAS_Y_CLAVES_PRIMARIAS/LEEME.md) | IDENTITY, SEQUENCE y NEXTVAL | No |
| 12 | [12_USUARIOS_Y_RESIDUOS_DEPOSITADOS](12_USUARIOS_Y_RESIDUOS_DEPOSITADOS/GUIA_USUARIOS_Y_RESIDUOS.md) | Qué deposita cada usuario (15 consultas) | No |
| 13 | [13_CONSULTAS_ECOPUNTOS_Y_AUDITORIA](13_CONSULTAS_ECOPUNTOS_Y_AUDITORIA/LEEME.md) | Tarifas, saldos, auditoría e historial | No |
| 14 | [14_EJERCICIOS_PROGRESIVOS](14_EJERCICIOS_PROGRESIVOS/LEEME.md) | 12 ejercicios de menor a mayor dificultad | No |
| 15 | [15_RESULTADOS_Y_CAPTURAS](15_RESULTADOS_Y_CAPTURAS/LEEME.md) | Salidas reales y capturas | — |
| 16 | [16_GUIA_ERRORES_FRECUENTES](16_GUIA_ERRORES_FRECUENTES/LEEME.md) | ORA-01017, 00942, 00904, 00001, 02291, 04098, 06550 y 20xxx | No |

## Precauciones

- Todos los archivos marcados «SOLO LECTURA» contienen únicamente `SELECT` o bloques que solo leen. Se pueden ejecutar las veces que quieras.
- La demostración de triggers (`../07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql`) sí modifica filas, pero **termina en ROLLBACK**: al final la base queda igual.
- Los ejercicios del profesor (precio final, notas, empleados) se hacen en `ECOACCESS_PRACTICAS`, nunca en `ECOACCESS_BD`.
- Los datos son **sintéticos** (`es_sintetico = 1`). No son depósitos reales de EcoAccess P3.
- Más práctica: el banco de 309 ejercicios verificados está en [`../05_SQL_Y_PLSQL`](../05_SQL_Y_PLSQL/LEEME.md).
