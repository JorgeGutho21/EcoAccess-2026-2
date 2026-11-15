# Estado del proyecto

Fecha de la última revalidación completa: **14/11/2026**, desde la carpeta `BaseDeDatos_Amilkar`, después de corregir las dos observaciones P2 de la revisión del Pull Request #12.

## Niveles de evidencia

- **VERIFICADO:** ejecutado o abierto y comprobado en el PC de trabajo, con la salida guardada.
- **PARCIAL:** hecho, pero falta una comprobación concreta, que se indica.
- **PENDIENTE:** requiere una acción que todavía no se hizo; se indica cuál.
- **NO VALIDADO:** fuera del alcance de la asignatura (hardware físico).

## Estado por componente

| Componente | Estado | Evidencia |
|---|---|---|
| Esquema `ECOACCESS_BD`: 19 tablas, 173 columnas, 9 vistas, 4 paquetes, 3 funciones, 2 procedimientos, 4 triggers, 19 PK, 38 FK, 160 restricciones con nombre y 0 inválidos | VERIFICADO — `INSTALAR_Y_PROBAR.ps1` aprobó **28 de 28** comprobaciones el 14/11/2026, desde la carpeta nueva, sin reinstalar y con las correcciones del PR #12 aplicadas | `16_EVIDENCIAS_Y_PRUEBAS/RESUMEN_ULTIMA_EJECUCION.md` |
| DDL final = scripts = diccionario | VERIFICADO (D01–D10) | `04_SCRIPTS_ORACLE/DDL_FINAL_EXTRAIDO.sql` |
| 163 pruebas del esquema · 19 de prácticas · 10 de la cuenta lectora · 22 consultas | VERIFICADO (T01–T04) | `16_EVIDENCIAS_Y_PRUEBAS/salidas/` |
| Revisión del PR #12, observación 1: `CK_DEPOSITO_MEDIDO` aceptaba mediciones incompletas (un CHECK con NULL da UNKNOWN y Oracle lo acepta) | VERIFICADO — corregido el 14/11/2026 con `IS NOT NULL` en peso, instrumento y precisión; 6 pruebas negativas y 2 positivas (P144–P151). Antes fallaban P144 y P145 | `16_EVIDENCIAS_Y_PRUEBAS/salidas/revision_pr12/` |
| Revisión del PR #12, observación 2: dos sesiones podían crear a la vez la primera tarifa de un material | VERIFICADO — `REGISTRAR_REGLA` bloquea la fila padre de `MATERIAL`; la prueba de dos sesiones (T06) pasó: antes B registraba en 0,0 s, ahora espera unos 4 s a que A termine | `16_EVIDENCIAS_Y_PRUEBAS/salidas/20_concurrencia_tarifas.txt` |
| Los 4 triggers: antes → sentencia → después, rechazos y ROLLBACK | VERIFICADO (T05) | `16_EVIDENCIAS_Y_PRUEBAS/salidas/demo_cuatro_triggers.txt` |
| 309 ejercicios (299 correctos y 10 con error intencional, 0 fallan) | VERIFICADO (G01) | `08_CONSULTAS_Y_EJERCICIOS/resultados_ejercicios.md`; `05_SQL_Y_PLSQL/*/RESULTADOS.md` |
| Diccionario: 173 atributos con significado de 90 a 165 palabras y al menos 5 oraciones; 19 tablas explicadas; 38 FK interpretadas | VERIFICADO contra los metadatos de Oracle | `03_DICCIONARIO_DE_DATOS/VERIFICACION_DICCIONARIO_173_ATRIBUTOS.md` |
| Diccionario dentro de Oracle (`COMMENT ON` de 173 columnas) y 173 SELECT de comprobación | VERIFICADO (D10; 0 errores ORA) | `16_EVIDENCIAS_Y_PRUEBAS/salidas/diccionario_173_select.txt` |
| Proyecto `.dmd` de Data Modeler (lógico y relacional, 5 subvistas, 9 vistas) | VERIFICADO — la regla de `CK_DEPOSITO_MEDIDO` se actualizó en el `.dmd` y `verificar_dmd.ps1` aprobó el 14/11/2026 en la ruta nueva (19 tablas, 173 columnas, 38 FK, igual que Oracle) | `02_MODELOS_DE_DATOS/07_EVIDENCIAS/` |
| Diagramas MER, MEER, relacional general y por módulos, y flujo | VERIFICADO (generados desde los metadatos de Oracle) | `02_MODELOS_DE_DATOS/01_…` a `06_…` |
| Prácticas de clase: secuencia, precio final, triggers libres, HR y CRUD sin DELETE | VERIFICADO (I03 y P99, 19/19) | `05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/` |
| C#: prueba de consola de 10 casos y ventana WinForms que compila | VERIFICADO (N01 y N02) | `16_EVIDENCIAS_Y_PRUEBAS/salidas/16_prueba_csharp.txt` |
| C#: capturas de la ventana (consultar, insertar, actualizar y error ORA) | VERIFICADO por automatización de la interfaz, no por clic humano | `16_EVIDENCIAS_Y_PRUEBAS/capturas_csharp/` |
| Power BI `.pbix` con origen CSV: 5 páginas, glosario, filtros y botones | VERIFICADO en pantalla (11/11/2026); el PBIP sigue íntegro (B02, 14/11/2026) | `16_EVIDENCIAS_Y_PRUEBAS/capturas_power_bi/` |
| Power BI con conexión directa a Oracle (`ECOACCESS_BD_LECTOR`) | **PARCIAL** — el PBIP ya lee las 5 vistas con `Oracle.Database` y el conector llegó al servidor. Falta que la persona dueña escriba la contraseña de la cuenta lectora en Power BI, actualice y guarde el PBIX | `09_POWER_BI/LEEME.md` |
| Capturas de la hoja de trabajo de SQL Developer con conexión | **PARCIAL** — hay capturas de Data Modeler; faltan las de la hoja de trabajo, que requieren escribir la contraseña en la ventana de conexión | `02_MODELOS_DE_DATOS/07_EVIDENCIAS/capturas_data_modeler/` |
| Respaldo Data Pump de `ECOACCESS_BD` y `ECOACCESS_PRACTICAS` | VERIFICADO (exportaciones del 11/11/2026 y del 14/11/2026, esta última antes de las correcciones del PR #12); restauración **no ejecutada**, porque reemplaza el esquema y requiere autorización | `18_ENTREGA_Y_RESPALDOS/registro_respaldo_data_pump/`; el `.DMP` solo está en el PC |
| Comparación con Programación III desde GitHub | VERIFICADO en solo lectura (rama `desarrollo/telegram-doble-ia`, commit `1fa52ac`, V002–V025) | `14_INTEGRACION_PROGRAMACION_III/` |
| Inventario de Oracle Cloud (`ECOACCESS_P3_APP`) | **PENDIENTE** — el script en solo lectura está listo; falta escribir la contraseña en la terminal | `14_INTEGRACION_PROGRAMACION_III/ejecutar_inventario_cloud.ps1` |
| Documentos Word y PDF de la entrega | VERIFICADO — regenerados el 14/11/2026 desde sus fuentes y auditados en Word | `18_ENTREGA_Y_RESPALDOS/herramientas/generacion.log` |
| Matriz de trazabilidad K | VERIFICADO — regenerada el 14/11/2026 | `15_MATRIZ_DE_TRAZABILIDAD/` |
| Informe final APA 7 (`01_DOCUMENTO_FINAL/01_INFORME_FINAL_ECOACCESS_APA7`) | VERIFICADO — 40 páginas, formato APA 7, generado y revisado página por página el 15/11/2026; los cinco puntos del Aula Web en los capítulos 1 a 5 | `01_DOCUMENTO_FINAL/LEEME.md` |
| Documento técnico detallado (`02_DOCUMENTO_DETALLADO_ECOACCESS`) | VERIFICADO — es el informe ampliado anterior, con su contenido completo y el nombre nuevo | `01_DOCUMENTO_FINAL/LEEME.md` |
| Biblioteca de modelos visuales (`19_MODELOS_VISUALES`) | VERIFICADO — 16 diagramas en PDF horizontal (A3 a A0), atlas de 35 páginas, índice, galería HTML sin Internet (68 enlaces, 0 rotos, también desde una copia en otra carpeta) y 243 copias idénticas a los originales | `19_MODELOS_VISUALES/09_GUIAS_Y_VERIFICACIONES/` |
| DMD abierto en SQL Developer: modelo lógico, Relational_1 y 5 subvistas; PK y FK; DDL como código; importación de prueba del DDL | VERIFICADO — 24 capturas reales del 15/11/2026; el DMD original quedó sin cambios | `19_MODELOS_VISUALES/08_CAPTURAS_REALES/` |
| SQL Developer conectado a `ECOACCESS_BD` (19 tablas, columnas, PK y FK, 4 triggers y su código) | **PENDIENTE** — la conexión pide la contraseña de `ECOACCESS_BD`, que debe escribir la persona dueña | `19_MODELOS_VISUALES/08_CAPTURAS_REALES/LEEME.md` |
| DMD en Data Modeler independiente (24.3.1) | **PARCIAL** — está instalado y se abrió su ventana; no se autorizó controlarlo en la sesión. Las mismas operaciones se comprobaron en el Data Modeler integrado | `19_MODELOS_VISUALES/05_DATA_MODELER/04_ABRIR_DMD/GUIA_ABRIR_DMD_EN_DATA_MODELER.md` |
| Consolidación de carpetas locales | VERIFICADO — una sola carpeta activa; las copias redundantes se movieron a `RESPALDOS_LOCALES` con 0 diferencias de SHA-256 | `18_ENTREGA_Y_RESPALDOS/CONSOLIDACION_CARPETAS_20261115.md` |
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
2. Pueden crearse vigencias solapadas en `REGLA_ECOPUNTOS` si se inserta directamente con SQL, sin pasar por `REGISTRAR_REGLA`. Por el procedimiento ya no es posible, ni siquiera con dos sesiones a la vez: desde el 14/11/2026 bloquea la fila padre de `MATERIAL`.
3. El peso estimado de `CONSULTA_VISION` no se valida contra el rango del catálogo, mientras que el de `DEPOSITO` sí.
4. La duración física de apertura (segundos, en P3) y la vigencia de la orden (`FECHA_VENCIMIENTO_UTC`, 2 minutos en los datos sintéticos) son conceptos distintos.

## Seguridad

- Ninguna contraseña, wallet, token ni archivo `.env` está en GitHub ni en el ZIP; `empaquetar.ps1` lo comprueba.
- No se modificaron Oracle Cloud, la base de Programación III, HR ni la configuración de Windows.
- El único cambio administrativo en el Oracle local fue `ALTER USER ECOACCESS_BD_LECTOR ACCOUNT UNLOCK`, el 11/11/2026, después de que Power BI bloqueara la cuenta con una contraseña antigua. No se cambiaron la contraseña ni el perfil.
- Las correcciones del PR #12 se aplicaron solo a `ECOACCESS_BD` local, con `04_SCRIPTS_ORACLE/19_correccion_revision_pr12.sql`, después de un respaldo Data Pump nuevo. Cambiaron una restricción CHECK y el paquete `PKG_ECOPUNTOS`; no cambió ninguna tabla, columna ni relación.
- No se borró ningún dato. Las 9 filas de prueba de C# (ids 1004–1080 en `ECOACCESS_PRACTICAS.EMPLEADO_PRACTICA`) se conservan.

## Cierre de la revisión técnica del Pull Request #12 (14/11/2026)

| Paso | Resultado |
|---|---|
| Respaldo previo | Data Pump de `ECOACCESS_BD` y `ECOACCESS_PRACTICAS` (`registro_respaldo_data_pump/ecoaccess_respaldo_20261114_antes_pr12.log`; el `.DMP` solo está en el PC). |
| Reproducción | Antes de corregir fallaban P144 y P145 (mediciones incompletas aceptadas) y P152 (sin código propio para un material inexistente); en la prueba de dos sesiones, B registró en 0,0 s con la tarifa de A todavía abierta. |
| Corrección | `CK_DEPOSITO_MEDIDO` con `IS NOT NULL` en los tres datos de la medición; `REGISTRAR_REGLA` con `SELECT … FROM material … FOR UPDATE` y ORA-20207 si el material no existe. |
| Regresión | `12_pruebas.sql` 163/163; `INSTALAR_Y_PROBAR.ps1` 28 de 28 (incluida la nueva T06); DDL extraído de nuevo; `verificar_dmd.ps1` PASA. |
| Documentación | Informe final (capítulo «Revisión técnica del Pull Request y correcciones»), diccionario y metadatos, guía de triggers, guía personal, manual de sustentación, guía de construcción, matriz K y este estado. |
| Sin cambios | MER, MEER, modelo relacional y las 38 relaciones: ninguna tabla, columna ni FK cambió. Las tareas del profesor no se modificaron. |
