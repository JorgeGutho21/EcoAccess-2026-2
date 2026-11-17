# Informe de pruebas · Carpeta 20

**Universidad Popular del Cesar · Base de Datos · Profesor Amílkar**
**Integrantes:** Jorge Alfonso Gutiérrez Thomas, Heyder Hedrey Canchila de Ángel y Saray Marcela Cadena Palomino

Fecha de las pruebas: 16/11/2026.

Equipo:

- Windows 11 Pro.
- Oracle SQL Developer 26.2 con Data Modeler 24.3.1 integrado. Por decisión del equipo, todo se hizo dentro de SQL Developer, no en el Data Modeler independiente.
- Oracle Database 23ai Free (`localhost:1521/FREEPDB1`). No hizo falta conectarse para abrir modelos.

## 1. Pruebas manuales en SQL Developer (con capturas reales)

| # | Prueba | Resultado | Evidencia |
|---|---|---|---|
| P01 | Abrir `01_DMD_MODELO_COMPLETO/EcoAccess_BD.dmd` desde la carpeta 20 | **PASA**: se abren Logical, Relational_1 y las 5 subvistas | `08_CAPTURAS_PASO_A_PASO/A_…/01` a `03` |
| P02 | Explorador: Relational_1 con 19 tablas, 38 FK y 5 subvistas | **PASA** | `A_…/04`, `05` |
| P03 | Mostrar el diagrama de las subvistas M1 a M5 | **PASA**: las 5 se dibujan con sus tablas y FK | `A_…/06` a `10` |
| P04 | Volver a Relational_1 (19 tablas) | **PASA** | `A_…/11` |
| P05 | Abrir un DDL por módulo como texto, sin ejecutarlo | **PASA** | `B_IMPORTAR_DDL/00` |
| P06 | Importar el DDL de M1 a M5, cada uno en un diseño nuevo | **PASA**: M1 25/25, M2 40/40, M3 28/28, M4 106/106 y M5 67/67 instrucciones importadas; 0 fallidas y 0 no reconocidas | `B_IMPORTAR_DDL/01` a `14` |
| P07 | Importar el DDL completo en un diseño de prueba | **PASA con observación**: 281 instrucciones, 277 importadas, 0 fallidas y 4 no reconocidas (los 4 `ALTER TRIGGER … ENABLE`, que no afectan tablas ni FK). Resultado: 19 tablas y 38 FK. Diseño cerrado **sin guardar**. | `B_IMPORTAR_DDL/15` a `17` |
| P08 | Guardar cada módulo como DMD independiente (Guardar como) | **PASA** (fue necesario crear antes las carpetas destino) | `02_DMD_MODELOS_INDIVIDUALES/` |
| P09 | Cerrar y reabrir `EcoAccess_M1.dmd` … `EcoAccess_M5.dmd` | **PASA**: 7/10, 5/4, 4/3, 10/20 y 8/10 tablas/FK | `C_REABRIR_DMD_POR_MODULO/01` a `05` |
| P10 | Abrir el DMD desde una copia extraída del ZIP, en otra ubicación | **PASA** (sección 3) | `D_PRUEBA_DESDE_ZIP_EXTRAIDO/` |

## 2. Verificación automática (`verificar_carpeta20.ps1`, solo lectura)

Lee los archivos XML del DMD y los `.sql` y no se conecta a Oracle. Ejecución del 16/11/2026 sobre la carpeta de trabajo:

| Id | Comprobación | Resultado |
|---|---|---|
| C01 | El DMD completo tiene su carpeta acompañante | PASA |
| C02–C04 | DMD completo: 19 tablas, 173 columnas, 38 FK | PASA |
| C05 | 5 subvistas con sus nombres exactos | PASA |
| C06–C10 | DMD por módulo: M1 7/81/10 · M2 5/26/4 · M3 4/23/3 · M4 10/106/20 · M5 8/86/10 (tablas/columnas/FK) | PASA |
| C11 | DDL completo idéntico (SHA-256) al de `04_SCRIPTS_ORACLE` | PASA |
| C12 | DDL completo: 19 CREATE TABLE y 38 FOREIGN KEY | PASA |
| C13–C17 | DDL por módulo: con advertencia y 0 sentencias DROP/DELETE/UPDATE/INSERT/TRUNCATE | PASA |
| C18 | Enlaces relativos de todos los `.md` de la carpeta 20 | PASA |

Para repetirla:

```
pwsh -File 20_ABRIR_MODELOS_EN_ORACLE\09_VERIFICACIONES\verificar_carpeta20.ps1
```

## 3. Prueba desde un ZIP extraído en otra ubicación

Se generó un ZIP de prueba con el mismo `18_ENTREGA_Y_RESPALDOS/herramientas/empaquetar.ps1` que produce la entrega: 1 599 archivos, 103,6 MB, 0 contraseñas y 0 archivos prohibidos. Se extrajo en **otra ubicación**, `C:\Users\Usuario\EZ\BaseDeDatos_Amilkar`, fuera de OneDrive.

| # | Prueba en la copia extraída | Resultado | Evidencia |
|---|---|---|---|
| Z1 | La copia contiene las carpetas 00 a 21 | **PASA** | listado de carpetas |
| Z2 | `verificar_carpeta20.ps1` ejecutado dentro de la copia | **PASA 18/18**, con 0 enlaces rotos (C18) | salida de consola |
| Z3 | SQL Developer: **Archivo → Data Modeler → Abrir** → `C:\Users\Usuario\EZ\…\01_DMD_MODELO_COMPLETO\EcoAccess_BD.dmd` | **PASA**: se abren Logical, Relational_1 y M1–M5. El Explorador muestra Tablas [19], Vistas [9], Claves Ajenas [38] y Subvistas [5] | `08_CAPTURAS_PASO_A_PASO/D_PRUEBA_DESDE_ZIP_EXTRAIDO/01` y `02` |
| Z4 | Cerrar el diseño | **PASA**: se cerró sin cambios y sin guardar | — |

Después de la prueba se borró la copia extraída. El ZIP final de la entrega se genera de nuevo, con el mismo script, al terminar todos los cambios.

## 4. Lo que no se probó

- El Data Modeler **independiente** no se usó, por decisión del equipo.
- La opción **Exportar → Archivo DDL** desde el modelo se describe en la guía 06 como referencia; no formó parte de estas pruebas.
- La distribución de los diagramas de los DMD por módulo es la automática de la importación; no se reacomodó a mano.
