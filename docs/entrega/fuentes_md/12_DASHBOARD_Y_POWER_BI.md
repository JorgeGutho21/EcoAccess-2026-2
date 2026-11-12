---
archivo: 12_DASHBOARD_Y_POWER_BI
titulo: Dashboard institucional y Power BI
subtitulo: Tablero de la aplicación, vistas de informes, modo experimental, cuenta de solo lectura, recursos de Power BI y estado real de cada pieza
codigo: 12
---

# Dos formas de ver los datos

EcoAccess P3 ofrece dos maneras de consultar sus estadísticas:

| Pieza | Dónde vive | Depende de | Estado |
|---|---|---|---|
| **Dashboard institucional** | `FormDashboardInstitucional` en la aplicación de escritorio | solo de Oracle | Verificado real |
| **Informe de Power BI** | Power BI Desktop, con los recursos de `reportes/powerbi` | Power BI Desktop y el wallet de Oracle | **`EcoAccess.pbix` construido y validado con SQL** (5 páginas) |

El dashboard **no depende** de Power BI. Si Power BI no está instalado, la aplicación sigue mostrando todas las cifras.

# El dashboard institucional

## Quién puede verlo

Desde el cierre del 11/11/2026, el **Operario** y el **Administrador** (documento 11). El ciudadano no puede.

{{codigo:src/EcoAccess.P3.Logica/Servicios/ServicioInformeInstitucional.cs:1:60}}

## De dónde salen las cifras

`RepositorioInformeInstitucionalOracle` lee las vistas de informes (`ea_r_*`), que son de **solo lectura**:

- `ea_r_base_depositos`: depósitos por día, caneca, material y estado.
- `ea_r_base_puntos`: ecopuntos acreditados y reversados.
- `ea_r_solicitudes`: solicitudes de apertura.
- `ea_r_opiniones`: valoraciones.

Cada vista trae la columna **`es_ensayo`**, que vale 1 en estos casos:

- canal Simulador o Telegram simulado;
- canecas ficticias;
- estaciones «SIMULACIÓN»;
- códigos de prueba.

## Modo operativo y modo experimental

- **Modo operativo** (por defecto): solo `es_ensayo = 0`. Hoy muestra **0 depósitos reales**, y es lo correcto, porque todavía no ha habido depósitos físicos.
- **Modo experimental**: muestra el lote `SIM-2026-11-CIERRE`, con 100 depósitos sintéticos de 50 ciudadanos ficticios. La pantalla lo rotula como experimental.

**Pesos:** el tablero separa el **peso estimado** (catálogo × unidades) del **peso medido** (báscula). Hoy hay 14,476 kg estimados en el lote y 0 kg medidos, porque no existe báscula. Un estimado nunca se presenta como medido.

## Rendimiento

La prueba `ConsultasDelTableroInstitucional_RespondenEnMenosDeCincoSegundos` midió las consultas del tablero contra
Oracle Cloud. La primera consulta tarda unos 5 s, porque abre la conexión mTLS hasta Bogotá. Las siguientes tardan entre
0,23 y 0,96 s. Los 16,7 s que se habían observado antes **no venían del SQL**: correspondían a la conexión en frío. Por eso
no se crearon índices nuevos.

# Cifras verificadas con SQL

La consulta de referencia está en `database/04_consultas/09_estadisticas_cierre.sql`, y la explicación en
`docs/base_datos/ESTADISTICAS_CIERRE_SQL.md`. Resultados del 11/11/2026 en Oracle Cloud:

| Pregunta | Resultado |
|---|---|
| Depósitos reales | 0 |
| Depósitos experimentales | 100 (85 blanca, 13 verde, 2 negra) |
| Materiales más registrados (experimental) | PET 24, cartón 16, lata 16, restos de alimentos 13, papel 12 |
| Actividad mensual (experimental) | octubre 64, noviembre 36 |
| Participantes | 50 ficticios, 0 reales |
| Peso estimado / medido | 14,476 kg / 0 kg |
| Ecopuntos acreditados / reversados | 0 / 0 (nadie acreditó datos inventados, a propósito) |
| Verificación | 58 verificados, 42 sin verificar |
| Reglas de ecopuntos vigentes | 7 |

# Power BI

## Qué hay

| Recurso | Para qué | Estado |
|---|---|---|
| `database/06_migraciones/V019__vistas_power_bi.sql` | vistas para el informe | aplicada en Oracle Cloud |
| `database/00_administracion/cuenta_informes_powerbi.sql` | crea la cuenta `ECOACCESS_P3_INFORMES`, de solo lectura | script listo; necesita la contraseña que elija el propietario |
| `reportes/powerbi/EcoAccessP3.pbip` | proyecto TMDL/PBIR del informe (10 tablas, 22 medidas) | ejecutado en Power BI Desktop |
| `reportes/powerbi/ImportarOracle.pq` y `Consultas.pq` | consultas de Power Query de referencia | integradas en el proyecto |
| `reportes/powerbi/Medidas.dax` | medidas DAX de referencia | las vigentes están en `Medidas.tmdl` |
| `reportes/powerbi/Validacion.sql` | totales de referencia para comparar | ejecutada |
| `src/EcoAccess.P3.Presentacion/AccesoPowerBi.cs` | abre `reportes\powerbi\EcoAccess.pbix` si existe | implementado |

## Informe construido (11/11/2026, commit 8ff0be6)

**`reportes/powerbi/EcoAccess.pbix` existe** (122 KB). Se guardó desde Power BI Desktop después de actualizar los datos
contra Oracle Cloud. Su fuente editable es el proyecto `reportes/powerbi/EcoAccessP3.pbip`, en formato TMDL y PBIR,
versionado en Git. El informe EcoAccess_BD es de **otra asignatura** y no se tocó.

| Página | Contenido |
|---|---|
| Resumen | depósitos reales, kg estimados y medidos, ciudadanos y ecopuntos del modo operativo (todo en 0 hoy) |
| Tres canecas | estado de las canecas 1093, 1400 y 1401 y depósitos por color |
| Actividad y ecopuntos | ecopuntos acreditados, anulados y netos y participación mensual |
| Datos experimentales | lote `SIM-2026-11-CIERRE`, rotulado como sintético |
| Reconocimiento visual | consultas de Eco, destino válido, latencia y exactitud de las rondas 3, 4 y 5 |

- **Fechas:** se muestran en hora de Colombia (UTC − 5).
- **Filtros:** día, semana, mes y rango, con segmentadores de la tabla `Calendario`.
- **Origen:** el modo operativo solo cuenta los registros reales. Los experimentales (`ES_SIMULACION` o `ES_ENSAYO`) van en su propia página.
- **Validación:** las tarjetas coinciden con SQL (`docs/evidencias/powerbi_p3/validacion_sql.txt`):
  - 0 depósitos reales;
  - 100 experimentales, 58 de ellos verificados;
  - 14,476 kg estimados y 0 kg medidos;
  - colores 85 / 13 / 2 y estados 27 / 15 / 58;
  - 0 ecopuntos y 0 consultas de Eco.

  Las capturas de las cinco páginas están en la misma carpeta.

**Riesgo pendiente:** el origen usa la credencial del esquema de la aplicación, que el propietario guardó en Power BI.
La cuenta de solo lectura `ECOACCESS_P3_INFORMES` sigue sin crearse, porque su contraseña la elige el propietario. Para
cambiar a esa cuenta: ejecute `cuenta_informes_powerbi.sql` y, en Power BI, vaya a *Archivo > Opciones > Configuración
de origen de datos > Editar permisos*. Ninguna contraseña se guarda en el repositorio.

## Cómo actualizarlo

1. Abra `reportes\powerbi\EcoAccess.pbix`, o «Análisis en Power BI» desde la aplicación.
2. Pulse **Actualizar**. Si Power BI pide credenciales de Oracle, las escribe el propietario.
3. Compare las tarjetas con `Validacion.sql`.

# Google Forms

**No está implementado.** EcoAccess guarda las opiniones en Oracle (`OPINION`, `COMENTARIO_CIUDADANO`) desde el bot y la
aplicación. No existe ninguna integración con Google Forms, y este documento no la presenta como hecha.

# Preguntas probables

**¿Por qué el dashboard muestra 0 depósitos?** Porque en modo operativo solo cuenta depósitos reales, y todavía no hay
ninguno. El modo experimental muestra los 100 sintéticos, rotulados como tales.

**¿Power BI puede ver las contraseñas?** El informe solo importa vistas de informes y tablas agregadas; ninguna consulta
lee `CREDENCIAL_ACCESO`. Hoy se conecta con la credencial del esquema de la aplicación, que técnicamente podría leerla.
Por eso se recomienda pasar a `ECOACCESS_P3_INFORMES`: solo tiene lectura sobre las vistas y no ve contraseñas ni
datos personales.

**¿Por qué el operario puede ver el tablero?** Es de solo lectura y lo necesita para supervisar. Se comprueba en
`MatrizPermisosTests` con los tres roles.
