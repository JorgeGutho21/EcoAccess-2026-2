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
| **Informe de Power BI** | Power BI Desktop, con los recursos de `reportes/powerbi` | Power BI Desktop, una cuenta de solo lectura y el cliente Oracle | Recursos preparados; **el `.pbix` de P3 no está construido** |

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
| `reportes/powerbi/ImportarOracle.pq` y `Consultas.pq` | consultas de Power Query | sin ejecutar en Power BI |
| `reportes/powerbi/Medidas.dax` | medidas DAX | sin ejecutar en Power BI |
| `reportes/powerbi/Validacion.sql` | totales de referencia para comparar | ejecutada |
| `src/EcoAccess.P3.Presentacion/AccesoPowerBi.cs` | abre `reportes\powerbi\EcoAccess.pbix` si existe | implementado |

## Qué falta y por qué

1. **El `.pbix` de P3 no existe.** Power BI Desktop está instalado en el equipo del propietario. El 11/11/2026 tenía abierto el informe **EcoAccess_BD**, que es de **otra asignatura** y no se modificó.
2. Para construir el informe hace falta conectarse con la cuenta de solo lectura, y su contraseña **solo la conoce el propietario**. Por seguridad, esa contraseña no se escribe en scripts ni en documentos.
3. El cruce de datos ya se hizo: la vista `EA_R_DEPOSITOS` que leería Power BI devuelve **0 filas operativas**, igual que la consulta SQL. Si el informe muestra 0 depósitos reales, está funcionando bien.

## Cómo construir el informe (propietario)

1. Ejecute `cuenta_informes_powerbi.sql` como administrador de Oracle. El script pide la contraseña y no la guarda.
2. Abra Power BI Desktop y elija **Obtener datos > Oracle Database**. Use el alias `ecoaccess_tp`, con `TNS_ADMIN` apuntando al wallet.
3. Siga `reportes/powerbi/LEEME.md`: parámetros, consultas, modelo (calendario y dimensiones con relaciones 1:*) y siete páginas.
4. Guarde el archivo como `reportes\powerbi\EcoAccess.pbix`. La aplicación lo abre desde «Análisis en Power BI».
5. Compare las tarjetas con `Validacion.sql`.

# Google Forms

**No está implementado.** EcoAccess guarda las opiniones en Oracle (`OPINION`, `COMENTARIO_CIUDADANO`) desde el bot y la
aplicación. No existe ninguna integración con Google Forms, y este documento no la presenta como hecha.

# Preguntas probables

**¿Por qué el dashboard muestra 0 depósitos?** Porque en modo operativo solo cuenta depósitos reales, y todavía no hay
ninguno. El modo experimental muestra los 100 sintéticos, rotulados como tales.

**¿Power BI puede ver las contraseñas?** No. La cuenta `ECOACCESS_P3_INFORMES` solo tiene permiso de lectura sobre las
vistas de informes. No ve `CREDENCIAL_ACCESO` ni datos personales.

**¿Por qué el operario puede ver el tablero?** Es de solo lectura y lo necesita para supervisar. Se comprueba en
`MatrizPermisosTests` con los tres roles.
