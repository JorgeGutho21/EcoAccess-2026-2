# Verificaciones de la guía EcoAccess P3 V1

Revisión del 16 de noviembre de 2026 (hora de Colombia). Base auditada: `a6875cce9f9c5b1c08fba2b4862111fa9121da5f`.

| Comprobación ejecutada | Resultado y alcance |
| --- | --- |
| `gh repo view`, `gh pr view 11`, `git ls-remote origin refs/heads/main` | Repo privado `JorgeGutho21/EcoAccess-2026-2`, main y merge del PR #11 en a6875cc |
| `gh pr checks 11` | Jobs compilar y hardware aprobados |
| `dotnet build EcoAccess.P3.sln -c Release -v q --nologo` | 0 errores, 46 advertencias de compilador y analizadores |
| `dotnet test tests/unitarias/EcoAccess.P3.Tests -c Release --no-build --nologo` | 2.164 aprobadas, 0 fallidas, 1 omitida (enlace simbólico) |
| `dotnet test tests/integracion/EcoAccess.P3.Datos.Tests -c Release --no-build --nologo --filter 'FullyQualifiedName~.Unitarias.'` | 29 aprobadas, 0 fallidas. El runner presentó avisos de extensiones. El filtro ejecuta componentes unitarios de Datos |
| `Validar-Entorno.ps1 -ProbarOracle` | Componentes obligatorios disponibles. Consulta real de solo lectura, 41 materiales habilitados |
| Diez SELECT del capítulo 04 con ODP.NET | Los diez ejecutaron correctamente en Oracle Cloud. USER_TABLES no encontró PUNTO_DISPOSICION en esa conexión |
| EXE `--comprobar-telegram` | getMe confirmó @EcoAccessP3_JG_bot, 0 updates pendientes y sin webhook. Sin consumo de updates |
| EXE `--estado-vision` y `ollama --version` | Modo Local, modelo lfm3-vl:latest disponible, Ollama 0.40.16 |
| `tailscale status --json --peers=false` y HTTPS /registro/ sin token | Tailscale Running, listener local 8081 y HTTP 400 en la ruta pública sin token. No se guardó URL privada de sesión ni se modificó Funnel |
| Arduino CLI, FQBN `esp32:esp32:esp32`, core 3.3.12 | Compilación Operación 334.179 B y Calibración 297.595 B. La compilación empleó archivos públicos y no cargó firmware |
| Inventario ESP32 | La validación no encontró puerto COM. Se comprobó topología por fuente y evidencia histórica |
| Banco visual | CSV con 915 filas. Revisados manifiestos v2, conteos y resultados v2b versionados. No se repitió el corpus completo |
| Power BI | PBIX presente, 121.933 bytes. PBIP, TMDL, siete orígenes Oracle y cinco definiciones de página revisados. Desktop 2.159.1231.0 detectado |
| Word y PDF | Generación desde un Markdown, validación de apertura Word, actualización de índice y exportación PDF. La comprobación visual usa las páginas PDF exportadas directamente desde Word, rasterizadas con PDFium. Se verificaron 44 páginas, índice, marcadores y enlaces del contenido, 10 imágenes y patrones de secretos. |

## Evidencias históricas

- `docs/evidencias/esp32_fisico/LEEME.md` y sus JSON registran ensayos firmados de las tres tapas y la investigación de la orden 1683.
- `docs/evidencias/registro_movil/LEEME.md` acredita navegador emulado, con datos ficticios.
- `docs/evidencias/powerbi_p3/validacion_sql_20261115.txt` conserva el corte SQL de solo lectura del cierre. Las capturas existentes tienen su propia fecha y población.
- El CI fijó núcleo ESP32 3.0.7 y compila los cuatro sketches. La prueba local usó 3.3.12 y dos sketches.

## Pendientes de prueba manual

- Instalación desde cero en un segundo PC con las credenciales propias del integrante y su rol Windows Forms.
- Ingreso en OCI y ejecución SQL Worksheet con la cuenta prevista. La consulta del EXE se verificó por separado.
- Alta o vinculación desde celular, regreso a Telegram, lectura de QR y prueba desde datos móviles; repetir con una cuenta de ensayo.
- Laboratorio Oracle Free y simulador completo en un perfil aislado, e integración con Oracle de ensayo. La suite unitaria de Datos sí se ejecutó.
- Confirmar placa, USB, puerto, firmware cargado, fuente, dimensiones, ángulos y las tres tapas del montaje actual. Repetir el recorrido Telegram → Oracle → bridge → ESP32 con supervisor, incluido cierre y caso bloqueado.
- Actualización del PBIX con cuenta de informes y comparación SQL en otro PC. El script histórico de grants no cubre todos los orígenes actuales del PBIP.
- Recorrido con fotos nuevas de celular y repetición integral del banco con versión de modelo registrada.

Las comprobaciones en esta revisión fueron de solo lectura o compilación y prueba local. Se preservaron Oracle productivo, firmware, instalación y servicios activos.
