# Guía para abrir el diseño EcoAccess_BD.dmd en Oracle SQL Developer Data Modeler (aplicación independiente)

## Estado de la comprobación (15/11/2026)

| Punto | Estado | Evidencia |
|---|---|---|
| Data Modeler independiente está instalado | VERIFICADO | `C:\BaseDeDatos\HerramientasOracle\datamodeler-24.3.1.351.0831-x64\datamodeler\datamodeler.exe` (versión 24.3.1.351). Se inició y abrió su ventana principal «Oracle SQL Developer Data Modeler». |
| Abrir el diseño y recorrer el modelo lógico, el relacional y las 5 subvistas en la aplicación independiente | PENDIENTE | En esta revisión no se autorizó controlar la aplicación independiente, así que no hay capturas propias de ella. |
| Las mismas operaciones con el Data Modeler integrado en SQL Developer | VERIFICADO | Mismo motor (Data Modeler 24.3.1). Capturas 01 a 15 en `08_CAPTURAS_REALES/SQL_DEVELOPER/` y guía `GUIA_ABRIR_DMD_EN_SQL_DEVELOPER.md`. |

El Data Modeler integrado en SQL Developer cubre todo lo que necesita la asignatura: abrir el diseño, ver el modelo lógico y el relacional, abrir las subvistas, ajustar el zoom, ver PK y FK, importar DDL y guardar una copia. No hace falta instalar nada más.

## Pasos en la aplicación independiente

La aplicación independiente usa los mismos diálogos que la versión integrada. La diferencia es que sus opciones están directamente en el menú **Archivo**, sin el submenú «Data Modeler».

1. **Abrir el programa.** Doble clic en `datamodeler.exe` (ruta de arriba) o en el acceso directo **datamodeler** del escritorio. Espera a que aparezca la ventana «Oracle SQL Developer Data Modeler».
2. **Abrir el diseño.** Menú **Archivo → Abrir**. En la ventana **Abrir Diseño**, elige `EcoAccess_BD.dmd` (con su carpeta `EcoAccess_BD/` al lado) y pulsa **Abrir**. Si pregunta qué modelos abrir, deja marcado `Relational_1` y acepta.
3. **Modelo lógico.** Pestaña **Logical**. Ajusta con **Ver → Ajustar Pantalla** (o el icono de la lupa con marco).
4. **Modelo relacional.** Pestaña **Relational_1**: 19 tablas y 38 FK.
5. **Subvistas.** En el **Explorador** (panel izquierdo; si no se ve: **Ver → Explorador**) despliega **Modelos Relacionales → Relational_1 → Subvistas** y haz doble clic en M1, M2, M3, M4 y M5.
6. **Zoom.** **Ver → Acercar / Alejar / Ajustar Pantalla**, o los iconos de lupa de la barra.
7. **Tablas completas.** Doble clic en el título de una tabla → **Columnas** (cierra con **Cancelar**).
8. **Comprobar elementos.** En el Explorador deben verse **Tablas [19]**, **Vistas [9]**, **Claves Ajenas [38]** y **Subvistas [5]**.
9. **Copia de trabajo.** **Archivo → Guardar como…** en otra carpeta y con otro nombre (por ejemplo `EcoAccess_BD_practica.dmd`). Nunca guardes encima del original de la entrega si moviste cajas sin querer.
10. **Comprobar que se conserva.** **Archivo → Cerrar**, vuelve a abrir la copia y comprueba las mismas cuentas del paso 8. Después ejecuta:

```
pwsh -File 02_MODELOS_DE_DATOS\08_HERRAMIENTAS_DE_GENERACION\verificar_dmd.ps1 -Dmd <ruta de la copia .dmd>
```

Debe informar 19 tablas, 173 columnas y 38 FK.

## Cómo leer el diagrama

Es la misma notación que en SQL Developer. P = PK, F = FK, U = única, asterisco rojo = obligatoria. La flecha apunta a la tabla padre; línea continua = FK obligatoria y discontinua = FK opcional. La explicación completa está en la sección 7 de `GUIA_ABRIR_DMD_EN_SQL_DEVELOPER.md` y en la leyenda del `ATLAS_VISUAL_ECOACCESS.pdf`.

## Si el diseño no abre

| Lo que pasa | Qué revisar |
|---|---|
| Abre vacío o faltan objetos | La carpeta `EcoAccess_BD/` no está junto al `.dmd`. |
| Dice que el diseño ya está abierto | Está abierto en SQL Developer. Ciérralo allí (**Archivo → Data Modeler → Cerrar**) antes de abrirlo aquí. |
| Archivos con una nube de OneDrive | Clic derecho → **Mantener siempre en este dispositivo**. |
| Pide guardar al cerrar sin que hayas cambiado nada | Elige **Cerrar Sin Guardar**. |
