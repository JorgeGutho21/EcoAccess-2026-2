# Estado del proyecto

Fecha de la última revalidación completa: **14/11/2026**, desde la carpeta `BaseDeDatos_Amilkar`.

## Niveles de evidencia

- **VERIFICADO:** ejecutado o abierto y comprobado en el PC de trabajo, con la salida guardada.
- **PARCIAL:** hecho, pero falta una comprobación concreta, que se indica.
- **PENDIENTE:** requiere una acción que todavía no se hizo; se indica cuál.
- **NO VALIDADO:** fuera del alcance de la asignatura (hardware físico).

## Estado por componente

| Componente | Estado | Evidencia |
|---|---|---|
| Esquema `ECOACCESS_BD`: 19 tablas, 173 columnas, 9 vistas, 4 paquetes, 3 funciones, 2 procedimientos, 4 triggers, 19 PK, 38 FK, 160 restricciones con nombre y 0 inválidos | VERIFICADO — `INSTALAR_Y_PROBAR.ps1` aprobó **27 de 27** comprobaciones el 14/11/2026, desde la carpeta nueva y sin reinstalar | `16_EVIDENCIAS_Y_PRUEBAS/RESUMEN_ULTIMA_EJECUCION.md` |
| DDL final = scripts = diccionario | VERIFICADO (D01–D10) | `04_SCRIPTS_ORACLE/DDL_FINAL_EXTRAIDO.sql` |
| 153 pruebas del esquema · 19 de prácticas · 10 de la cuenta lectora · 22 consultas | VERIFICADO (T01–T04) | `16_EVIDENCIAS_Y_PRUEBAS/salidas/` |
| Los 4 triggers: antes → sentencia → después, rechazos y ROLLBACK | VERIFICADO (T05) | `16_EVIDENCIAS_Y_PRUEBAS/salidas/demo_cuatro_triggers.txt` |
| 309 ejercicios (299 correctos y 10 con error intencional, 0 fallan) | VERIFICADO (G01) | `08_CONSULTAS_Y_EJERCICIOS/resultados_ejercicios.md`; `05_SQL_Y_PLSQL/*/RESULTADOS.md` |
| Diccionario: 173 atributos con significado de 90 a 165 palabras y al menos 5 oraciones; 19 tablas explicadas; 38 FK interpretadas | VERIFICADO contra los metadatos de Oracle | `03_DICCIONARIO_DE_DATOS/VERIFICACION_DICCIONARIO_173_ATRIBUTOS.md` |
| Diccionario dentro de Oracle (`COMMENT ON` de 173 columnas) y 173 SELECT de comprobación | VERIFICADO (D10; 0 errores ORA) | `16_EVIDENCIAS_Y_PRUEBAS/salidas/diccionario_173_select.txt` |
| Proyecto `.dmd` de Data Modeler (lógico y relacional, 5 subvistas, 9 vistas) | VERIFICADO — `verificar_dmd.ps1` aprobó el 14/11/2026 en la ruta nueva (19 tablas, 173 columnas, 38 FK, igual que Oracle) | `02_MODELOS_DE_DATOS/07_EVIDENCIAS/` |
| Diagramas MER, MEER, relacional general y por módulos, y flujo | VERIFICADO (generados desde los metadatos de Oracle) | `02_MODELOS_DE_DATOS/01_…` a `06_…` |
| Prácticas de clase: secuencia, precio final, triggers libres, HR y CRUD sin DELETE | VERIFICADO (I03 y P99, 19/19) | `05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/` |
| C#: prueba de consola de 10 casos y ventana WinForms que compila | VERIFICADO (N01 y N02) | `16_EVIDENCIAS_Y_PRUEBAS/salidas/16_prueba_csharp.txt` |
| C#: capturas de la ventana (consultar, insertar, actualizar y error ORA) | VERIFICADO por automatización de la interfaz, no por clic humano | `16_EVIDENCIAS_Y_PRUEBAS/capturas_csharp/` |
| Power BI `.pbix` con origen CSV: 5 páginas, glosario, filtros y botones | VERIFICADO en pantalla (11/11/2026); el PBIP sigue íntegro (B02, 14/11/2026) | `16_EVIDENCIAS_Y_PRUEBAS/capturas_power_bi/` |
| Power BI con conexión directa a Oracle (`ECOACCESS_BD_LECTOR`) | **PARCIAL** — el PBIP ya lee las 5 vistas con `Oracle.Database` y el conector llegó al servidor. Falta que la persona dueña escriba la contraseña de la cuenta lectora en Power BI, actualice y guarde el PBIX | `09_POWER_BI/LEEME.md` |
| Capturas de la hoja de trabajo de SQL Developer con conexión | **PARCIAL** — hay capturas de Data Modeler; faltan las de la hoja de trabajo, que requieren escribir la contraseña en la ventana de conexión | `02_MODELOS_DE_DATOS/07_EVIDENCIAS/capturas_data_modeler/` |
| Respaldo Data Pump de `ECOACCESS_BD` y `ECOACCESS_PRACTICAS` | VERIFICADO (exportación); restauración **no ejecutada**, porque reemplaza el esquema y requiere autorización | `18_ENTREGA_Y_RESPALDOS/registro_respaldo_data_pump/`; el `.DMP` solo está en el PC |
| Comparación con Programación III desde GitHub | VERIFICADO en solo lectura (rama `desarrollo/telegram-doble-ia`, commit `1fa52ac`, V002–V025) | `14_INTEGRACION_PROGRAMACION_III/` |
| Inventario de Oracle Cloud (`ECOACCESS_P3_APP`) | **PENDIENTE** — el script en solo lectura está listo; falta escribir la contraseña en la terminal | `14_INTEGRACION_PROGRAMACION_III/ejecutar_inventario_cloud.ps1` |
| Documentos Word y PDF de la entrega | VERIFICADO — regenerados el 14/11/2026 desde sus fuentes y auditados en Word | `18_ENTREGA_Y_RESPALDOS/herramientas/generacion.log` |
| Matriz de trazabilidad K | VERIFICADO — regenerada el 14/11/2026 | `15_MATRIZ_DE_TRAZABILIDAD/` |
| Prototipo físico ESP32 de las tres canecas | NO VALIDADO (fuera del alcance de Base de Datos) | — |
| Rúbrica final posterior del profesor | No se encontró; la única pauta disponible es la del avance del Aula Web | `06_TAREAS_PROFESOR_AMILKAR/2026-09-18_AVANCE_PROYECTO_FINAL_AULA_WEB/` |

## Los cinco puntos del Aula Web

| Punto | Capítulo del informe final |
|---|---|
| 1. Introducción | 1. Introducción |
| 2. Objetivos general y específicos | 2. Objetivos |
| 3. Modelo entidad-relación | 3. Modelo Entidad-Relación (MER) y 3.2 MEER |
| 4. Modelo relacional | 4. Modelo relacional (incluye la normalización) |
| 5. Diccionario de datos | 5. Diccionario de datos y Anexo A |

Las páginas exactas las calcula Word en la tabla inicial del informe y están también en la hoja «Aula Web 5 puntos» de la matriz K.

## Observaciones técnicas abiertas

Están documentadas en el informe final (capítulo «Observaciones técnicas del modelo») y en `03_DICCIONARIO_DE_DATOS/VERIFICACION_DICCIONARIO_173_ATRIBUTOS.md`. Ninguna hace fallar las pruebas, y el esquema no se modificó solo para ocultarlas.

1. `AUDITORIA_DEPOSITO.ID_ACTOR` no tiene FK, mientras que `HISTORIAL_ESTADO_CONTENEDOR.ID_ACTOR` sí la tiene.
2. Pueden crearse vigencias solapadas en `REGLA_ECOPUNTOS` si se inserta sin pasar por `REGISTRAR_REGLA`.
3. El peso estimado de `CONSULTA_VISION` no se valida contra el rango del catálogo, mientras que el de `DEPOSITO` sí.
4. La duración física de apertura (segundos, en P3) y la vigencia de la orden (`FECHA_VENCIMIENTO_UTC`, 2 minutos en los datos sintéticos) son conceptos distintos.

## Seguridad

- Ninguna contraseña, wallet, token ni archivo `.env` está en GitHub ni en el ZIP; `empaquetar.ps1` lo comprueba.
- No se modificaron Oracle Cloud, la base de Programación III, HR ni la configuración de Windows.
- El único cambio administrativo en el Oracle local fue `ALTER USER ECOACCESS_BD_LECTOR ACCOUNT UNLOCK`, el 11/11/2026, después de que Power BI bloqueara la cuenta con una contraseña antigua. No se cambiaron la contraseña ni el perfil.
- No se borró ningún dato. Las 9 filas de prueba de C# (ids 1004–1080 en `ECOACCESS_PRACTICAS.EMPLEADO_PRACTICA`) se conservan.
