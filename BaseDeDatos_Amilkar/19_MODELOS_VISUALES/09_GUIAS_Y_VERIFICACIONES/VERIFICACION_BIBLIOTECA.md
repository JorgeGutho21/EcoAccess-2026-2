# Verificación de la biblioteca de modelos visuales (15/11/2026)

| Comprobación | Estado | Evidencia |
|---|---|---|
| Los diagramas son copias exactas de `02_MODELOS_DE_DATOS` | VERIFICADO | 243 copias con el mismo SHA-256 que su original (`ORIGENES_Y_HASHES.csv`) |
| Cuentas del modelo tomadas de Oracle | VERIFICADO | El generador lee `metadatos.txt` y `restricciones_oracle.txt`: 19 tablas, 173 columnas y 38 FK; se detiene si no coinciden |
| MER, MEER, relacional completo, M1 a M5 y flujo abren en PDF, PNG y SVG | VERIFICADO | 16 PDF horizontales; las páginas se renderizaron y se revisaron |
| Las 7 exportaciones de Data Modeler disponibles | VERIFICADO | `05_DATA_MODELER/01_MODELO_LOGICO`, `02_MODELO_RELACIONAL_GENERAL` y `03_SUBVISTAS/M1` a `M5`, cada una con su PDF vectorial original y una versión grande para imprimir |
| Todos los PDF de diagramas en horizontal | VERIFICADO | MER y MEER en A2; relacional completo en A1 más 4 hojas A3 de lectura; módulos en A2 o A3; Data Modeler en A0, A1 o A2; flujo en A2. Ninguna página vertical |
| Texto legible sin recortes | VERIFICADO | El tamaño de página se calculó para que la letra más pequeña quede cerca de 7 a 8 pt; se revisaron las páginas del MER, M4, el modelo relacional de Data Modeler y la lectura A3 |
| Flujo de datos | VERIFICADO | El SVG original no tenía la etiqueta de cierre `</svg>` y no abría como imagen; se cerró en el original y en su generador, sin cambiar el dibujo |
| Atlas visual | VERIFICADO | `ATLAS_VISUAL_ECOACCESS.pdf`: 35 páginas horizontales con portada, índice, una página explicativa y el diagrama por figura, y la leyenda |
| Índice visual | VERIFICADO | `00_INDICE_VISUAL.pdf`: 3 páginas A4 horizontales con miniaturas |
| Galería HTML sin Internet | VERIFICADO | `verificar_galeria.ps1`: 68 enlaces, 0 rutas absolutas, 0 rotos en la carpeta y en una copia en otra ubicación. Imagen de la copia abierta: `08_CAPTURAS_REALES/GALERIA_HTML/` |
| DMD abierto en SQL Developer | VERIFICADO | Capturas 01 a 15. Explorador: Tablas [19], Vistas [9], Claves Ajenas [38], Subvistas [5]. Se recorrieron el modelo lógico, Relational_1, M1 a M5 y se volvió al general |
| DMD sin cambios después de abrirlo | VERIFICADO | 0 archivos distintos frente a GitHub en `02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE` |
| Copia de consulta del DMD | VERIFICADO | `verificar_dmd.ps1` sobre `05_DATA_MODELER/04_ABRIR_DMD/COPIA_DE_CONSULTA_DMD/EcoAccess_BD.dmd`: PASA |
| DDL visto como código, sin ejecutarlo | VERIFICADO | Capturas 23 y 24 |
| Importación de prueba del DDL en un diseño independiente | VERIFICADO | Capturas 16 a 22: 281 sentencias, 277 importadas, 0 fallidas y 4 no reconocidas (los `ALTER TRIGGER … ENABLE`); 19 tablas y 38 FK; cerrada sin guardar |
| DMD en Data Modeler independiente | PARCIAL | Instalado (24.3.1.351) y abierto; no se autorizó controlarlo durante la sesión |
| SQL Developer conectado a `ECOACCESS_BD` | PENDIENTE | Requiere que la persona dueña escriba la contraseña en la ventana de conexión |

## Cómo repetir las comprobaciones

```
pwsh -File 19_MODELOS_VISUALES\09_GUIAS_Y_VERIFICACIONES\generar_biblioteca_visual.ps1
pwsh -File 19_MODELOS_VISUALES\09_GUIAS_Y_VERIFICACIONES\convertir_guias_pdf.ps1
pwsh -File 19_MODELOS_VISUALES\09_GUIAS_Y_VERIFICACIONES\verificar_galeria.ps1
pwsh -File 02_MODELOS_DE_DATOS\08_HERRAMIENTAS_DE_GENERACION\verificar_dmd.ps1 -Dmd 19_MODELOS_VISUALES\05_DATA_MODELER\04_ABRIR_DMD\COPIA_DE_CONSULTA_DMD\EcoAccess_BD.dmd
```
