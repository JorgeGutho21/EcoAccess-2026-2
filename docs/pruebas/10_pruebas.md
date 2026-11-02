# 10. Pruebas

Fecha: 2026-11-02. Entorno: Windows 11, .NET Framework 4.8, Oracle 26ai Free 23.26.3 en Docker, esquema limpio `ECOACCESS_P3_APP`, compilación Release.

| Suite | Aprobadas | Omitidas | Fallidas |
|---|---|---|---|
| Compilación Release | 0 avisos | | 0 errores |
| Unitarias | 1380 | 1 | 0 |
| Integración con Oracle y modelo real | 191 | 2 | 0 |

Omitidas: enlace simbólico (Windows no concede el permiso), último administrador (solo aplica a una base sin administradores) y evaluación amplia de 24 fotos (se ejecuta aparte).

Las pruebas unitarias heredadas eran 1455 en net10. Se retiraron las de Gemini, OpenAI y ONNX por eliminarse esos componentes, y se añadieron las nuevas.

## Validación integral

`RecorridoCompletoDeEco_FotoRealConfirmacionVentanaVerificacionYValoracion` usa el modelo real, Oracle, el bridge y el simulador como procesos separados. Registro, sesión, chatbot, foto, reconocimiento (cartón), confirmación, cantidad, permisos, orden única con doble pulsación, apertura simulada, **ventana medida de 19,9 s**, cierre, avisos, verificación independiente, ecopuntos sin duplicar, historial, estadísticas y valoración: **aprobada**. De la foto a la pregunta: 2,64 s.

**No usa Telegram real ni hardware.**

| Escenario | Estado |
|---|---|
| A. Ciudadano nuevo por QR | software con simulador; Telegram real y físico pendientes |
| B. Ciudadano existente | software con simulador; Telegram real pendiente |
| C. Operador Windows Forms | servicios reales y simulador; formulario revisado por captura, sin prueba automatizada de clics |
| D. Foto desconocida | automatizado |
| E. Desconexión del ESP32 | adaptador con enlace simulado; físico pendiente |
| F. Solicitudes simultáneas | automatizado y con Oracle |
| G. Segundo computador | **pendiente** |

## Fallos encontrados y corregidos

- Script de base de datos: un BOM hacía creer a SQL*Plus que no había conexión y el script informaba éxito.
- Carrera en una prueba de concurrencia que producía un fallo intermitente.
- El operador no podía buscar ciudadanos: la consulta exigía Administrador.
- Eventos de apertura retenidos hasta terminar la actuación.
- Script de detención con una ruta de otra instalación.

## Power BI

Las seis vistas de informes existen y las consultas de agregados se ejecutan sin errores. **No se generó un archivo .pbix**: Power BI Desktop no está disponible.