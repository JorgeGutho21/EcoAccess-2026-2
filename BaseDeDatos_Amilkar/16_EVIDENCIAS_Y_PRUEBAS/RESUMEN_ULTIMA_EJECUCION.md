# Resumen de la última ejecución de INSTALAR_Y_PROBAR.ps1

- **Fecha:** 2026-11-14 13:49 (hora del PC) · duración 1 min
- **Equipo:** GUTIOME · Oracle: `localhost:1521/FREEPDB1` · SQL*Plus: `C:\app\Usuario\product\26ai\dbhomeFree\bin\sqlplus.exe`
- **Opciones:** sin reinstalar
- **Resultado:** 28 de 28 comprobaciones PASAN · **todo funciona**
- **Evidencias completas:** `16_EVIDENCIAS_Y_PRUEBAS/salidas/ultima_ejecucion/` (una salida de SQL*Plus por paso, sin contraseñas).

| Estado | Id | Qué se comprobó | Detalle |
|---|---|---|---|
| ✅ PASA | E01 | SQL*Plus encontrado | C:\app\Usuario\product\26ai\dbhomeFree\bin\sqlplus.exe |
| ✅ PASA | E02 | El listener publica el servicio FREEPDB1 | TNS_ADMIN=C:\app\Usuario\product\26ai\dbhomeFree\network\admin |
| ✅ PASA | E04 | Conexión como ECOACCESS_BD | //localhost:1521/FREEPDB1 |
| ✅ PASA | E04 | Conexión como ECOACCESS_PRACTICAS | //localhost:1521/FREEPDB1 |
| ✅ PASA | E04 | Conexión como ECOACCESS_BD_LECTOR | //localhost:1521/FREEPDB1 |
| ✅ PASA | I02 | Esquema ya instalado (19 tablas); no se reinstaló | usa -Reinstalar para empezar de cero |
| ✅ PASA | I03 | Prácticas P00 → P05 ejecutadas (sus demos de error son intencionales) | código 0 |
| ✅ PASA | T01 | Pruebas del esquema (12_pruebas.sql) | 163/163 |
| ✅ PASA | T02 | Pruebas de las prácticas (P99) | 19/19 |
| ✅ PASA | T03 | Mínimo privilegio de la cuenta lectora (16) | 10/10 |
| ✅ PASA | T04 | 22 consultas de exposición sin errores (13) | 22 consultas ejecutadas |
| ✅ PASA | T05 | Los 4 triggers: antes → sentencia → después y su rechazo (demo) | ORA-20120, ORA-20101, ORA-20103, auditoría :OLD/:NEW y ROLLBACK |
| ✅ PASA | T06 | Concurrencia: dos sesiones no crean dos tarifas abiertas (registrar_regla bloquea MATERIAL) | B esperó 4,0 s y registró solo después de que A terminó: nunca hubo dos tarifas abiertas a la vez. |
| ✅ PASA | D01 | DDL final extraído (04_SCRIPTS_ORACLE\DDL_FINAL_EXTRAIDO.sql) | tablas 19, vistas 9, paquetes 4, triggers 4, 1978 líneas |
| ✅ PASA | D02 | 0 objetos inválidos | 0 inválidos |
| ✅ PASA | D03 | Tablas: 02_tablas.sql = Oracle | scripts 19 = Oracle 19 |
| ✅ PASA | D04 | Vistas: 06_vistas.sql = Oracle | scripts 9 = Oracle 9 |
| ✅ PASA | D05 | Paquetes, funciones y procedimientos: 07 = Oracle | scripts 9 = Oracle 9 |
| ✅ PASA | D06 | Disparadores: 08_triggers.sql = Oracle | scripts 4 = Oracle 4 |
| ✅ PASA | D07 | Índices de 04_indices.sql existen en Oracle | 40 índices del script, 81 índices en total en Oracle |
| ✅ PASA | D08 | Restricciones con nombre: 02 + 03 = Oracle | scripts 160 = Oracle 160 |
| ✅ PASA | D09 | Columnas: diccionario (173) = Oracle | scripts 173 = Oracle 173 |
| ✅ PASA | D10 | Columnas con comentario del diccionario dentro de Oracle | 173 de 173 |
| ✅ PASA | G01 | Banco de ejercicios ejecutado en Oracle | 309 ejercicios · 299 OK · 10 con error intencional · 0 fallan |
| ✅ PASA | N01 | Prueba de integración C# (lector + CRUD sin DELETE) | 10 casos PASA |
| ✅ PASA | N02 | La ventana WinForms del CRUD compila |  |
| ✅ PASA | B01 | CSV exportados con la cuenta lectora | vw_bi_auditoria=91, vw_bi_ciudadanos=51, vw_bi_consultas_vision=187, vw_bi_depositos=105, vw_bi_ecopuntos=48, vw_caneca_estacion=4 |
| ✅ PASA | B02 | Proyecto Power BI íntegro (PBIP + PBIX; origen Oracle directo con respaldo CSV) | 6 tablas · 15 medidas · 5 tablas leen Oracle · 5 páginas |
