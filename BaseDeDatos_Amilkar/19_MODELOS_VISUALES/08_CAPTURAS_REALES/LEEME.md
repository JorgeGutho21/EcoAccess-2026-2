# Capturas reales (15/11/2026)

Tomadas en Oracle SQL Developer 26.2.0.186.2220, con su Data Modeler integrado 24.3.1, en el computador del proyecto. Las capturas no se editaron: muestran solo la ventana de SQL Developer y sus diálogos.

- **Capturas de 1552×880:** copian la ventana del programa con la función de Windows que la imprime, así que no incluyen otras ventanas.
- **Capturas de 1456×819 (02, 13, 14, 17, 18, 19 y 22):** son las que tienen un diálogo abierto. Se tomaron con una captura de pantalla que oculta las ventanas de otros programas.

## SQL_DEVELOPER: abrir el diseño y recorrerlo

| # | Archivo | Qué muestra |
|---|---|---|
| 01 | `01_menu_archivo_data_modeler_abrir.png` | Menú Archivo → Data Modeler → Abrir |
| 02 | `02_dialogo_abrir_diseno_ruta_dmd.png` | Ventana Abrir Diseño con la ruta de `EcoAccess_BD.dmd` |
| 03 | `03_menu_ver_data_modeler_explorador_y_zoom.png` | Menú Ver → Data Modeler (Explorador, Acercar, Alejar, Ajustar Pantalla) |
| 04 | `04_explorador_relational1_19_tablas_38_fk_5_subvistas.png` | Explorador: Tablas [19], Vistas [9], Claves Ajenas [38], Subvistas [5] (M1 a M5) |
| 05 | `05_modelo_logico_completo.png` | Modelo lógico |
| 06 | `06_modelo_relacional_general_relational1.png` | Modelo relacional general Relational_1 |
| 07–11 | `07_subvista_M1_catalogos.png` … `11_subvista_M5_ecopuntos_auditoria.png` | Las cinco subvistas |
| 12 | `12_relacion_fk_credencial_usuario_seleccionada.png` | Relación FK seleccionada entre CREDENCIAL_ACCESO y USUARIO |
| 13 | `13_propiedades_fk_credencial_usuario.png` | Propiedades de la FK: padre, hija, NO ACTION, columnas |
| 14 | `14_tabla_usuario_columnas_pk.png` | Columnas de USUARIO con su PK y su comentario del diccionario |
| 15 | `15_regreso_modelo_general.png` | Regreso al modelo general |
| 23 | `23_ddl_abierto_como_codigo_create_table.png` | DDL abierto como código, sin ejecutarlo |
| 24 | `24_ddl_pk_check_fk_references.png` | PK, CHECK y FK con REFERENCES en el DDL |

## DDL_IMPORTACION_PRUEBA: importación del DDL en un diseño independiente

| # | Archivo | Qué muestra |
|---|---|---|
| 16 | `16_menu_importar_archivo_ddl_diseno_sin_titulo.png` | Importar → Archivo DDL en el diseño vacío «Sin título_1» |
| 17 | `17_archivo_ddl_seleccionado_importar_a_relational1.png` | DDL elegido, importar a Relational_1 del diseño de prueba |
| 18 | `18_log_importacion_281_sentencias_0_fallidas.png` | Log: 281 sentencias, 277 importadas, 0 fallidas y 4 no reconocidas |
| 19 | `19_comparar_modelos_fusionar_en_diseno_sin_titulo.png` | Comparar Modelos antes de fusionar en el diseño de prueba |
| 20 | `20_diagrama_generado_desde_ddl_disposicion_automatica.png` | Diagrama generado automáticamente, sin ordenar |
| 21 | `21_explorador_diseno_ddl_19_tablas_38_fk_sin_subvistas.png` | Diseño importado: 19 tablas y 38 FK, sin subvistas |
| 22 | `22_cerrar_diseno_prueba_sin_guardar.png` | El diseño de prueba se cierra sin guardar |

## Lo que todavía no tiene captura

| Captura pedida | Estado | Causa |
|---|---|---|
| SQL Developer conectado a `ECOACCESS_BD`, con las 19 tablas, una tabla con sus columnas, sus PK y FK, los 4 triggers y el código de un trigger | PENDIENTE | La conexión «EcoAccess BD LOCAL» pide la contraseña de `ECOACCESS_BD`, y esa contraseña la debe escribir la persona dueña de la cuenta. Con la contraseña escrita, basta desplegar **Conexiones → EcoAccess BD LOCAL → Tablas** y **Disparadores**. |
| Data Modeler independiente con el diseño abierto | PENDIENTE | La aplicación está instalada (24.3.1.351), pero en esta revisión no se autorizó controlarla. Lo mismo se comprobó en el Data Modeler integrado. |
| Atlas PDF y galería HTML en pantalla | Ver `09_GUIAS_Y_VERIFICACIONES/` | Se comprobaron sus páginas y enlaces con programas, no con capturas de pantalla. |

Las capturas anteriores de Data Modeler (11/11/2026) siguen en `02_MODELOS_DE_DATOS/07_EVIDENCIAS/capturas_data_modeler/`.
