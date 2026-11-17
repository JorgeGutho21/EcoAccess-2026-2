# Power BI · EcoAccess BD

## Estado (sin maquillar)

| Pieza | Estado |
|---|---|
| `exportar_csv.sql` → `datos_csv/` (6 CSV) | ✅ Ejecutado con la cuenta **ECOACCESS_BD_LECTOR**, que solo ve vistas. Resultado: 105 depósitos, 187 fotos, 48 movimientos, 91 auditorías, 51 ciudadanos y 4 canecas. |
| `generar_pbip.ps1` → `EcoAccess_BD/EcoAccess_BD.pbip` | ✅ Generado: modelo con 6 tablas, 6 relaciones y 15 medidas; informe con 5 páginas y 50 visuales. |
| Abrir el `.pbip` en Power BI Desktop | ✅ Abrió en Power BI Desktop 2.159, sin errores. |
| Actualizar los datos y calcular las medidas | ✅ Comprobado con `verificar_modelo` (conexión al motor interno de Power BI): 6 tablas cargadas, 6 relaciones activas y 15 medidas sin error. Las cifras coinciden con Oracle: 187 fotos, 105 depósitos (76 / 17 / 12) y 675 EcoPuntos netos. Evidencia: `16_EVIDENCIAS_Y_PRUEBAS/salidas/powerbi_modelo_dax.txt`. |
| Campos de los 50 visuales | ✅ Los 46 campos usados existen en el modelo (validación automática). |
| Ver los visuales dibujados en pantalla | ⏳ No se pudo mirar: otra sesión estaba usando la pantalla del PC. Revísalos al abrirlo. |
| `.pbix` | ⏳ Al abrirlo, usa **Archivo → Guardar como** `.pbix` si el profesor lo pide en ese formato. |
| Verificar el modelo tú mismo | Con Power BI abierto: `dotnet run --project 09_POWER_BI\verificar_modelo -- <puerto> refrescar`. El puerto está en `%LOCALAPPDATA%\Microsoft\Power BI Desktop\AnalysisServicesWorkspaces\*\Data\msmdsrv.port.txt`. |
| `medidas.dax`, `power_query.m` | ✅ Generados desde el mismo script (son la especificación). |

## Cómo abrirlo

1. Lo más fácil: `pwsh -File .\INSTALAR_Y_PROBAR.ps1` desde la carpeta raíz. Exporta los CSV con la cuenta lectora y regenera el proyecto, que son los pasos 1 y 2 de esta lista.
   - Solo los CSV, desde la carpeta raíz: `pwsh -File 18_ENTREGA_Y_RESPALDOS\herramientas\ejecutar_sql.ps1 -Usuario ECOACCESS_BD_LECTOR -Scripts 09_POWER_BI\DATOS_Y_CONSULTAS\exportar_csv.sql`.
2. Ejecuta `pwsh -File 09_POWER_BI\generar_pbip.ps1`. Así escribe la ruta actual de `datos_csv` en el parámetro `CarpetaDatos`; hazlo siempre que muevas la carpeta del proyecto.
3. Abre `EcoAccess_BD\EcoAccess_BD.pbip` en Power BI Desktop y pulsa **Inicio → Actualizar**.
4. Si un visual sale con error, ábrelo, vuelve a arrastrar el campo indicado (los nombres están abajo) y guarda.

## Páginas

| Página | Visuales |
|---|---|
| Inicio | Rótulo DATOS SINTÉTICOS. Tarjetas: Fotos consultadas (NO son depósitos), Depósitos registrados, Depósitos verificados, EcoPuntos netos. 4 botones de navegación y una explicación de cómo leer el tablero. |
| Operación | Segmentadores: intervalo libre `Calendario[Fecha]`, `Calendario[Ventana]`, caneca, material, estado. Tarjetas: verificados, pendientes, rechazados, unidades. Barras por caneca, columnas por semana (fotos frente a depósitos), barras por material. |
| Ranking y pesos | Ventana. Tarjetas: kg estimados, kg medidos, verificados con peso medido, ciudadanos con verificados. Tabla TOP ordenada por verificados. Barras de kg estimados frente a kg medidos por material. |
| EcoPuntos y auditoría | Tarjetas de crédito, reverso y neto. Barras de puntos por material. Tabla de auditoría (trigger 3). |
| Galería de roles | Qué hace y qué no hace el Ciudadano, el Operario y el Administrador. Aclara que **no** es la seguridad: esta la ponen Oracle y la cuenta lectora. |

## Filtros de tiempo

`Calendario[Ventana]` divide los días en tramos **excluyentes**, contados desde el último día con datos:

| Tramo | Cubre |
|---|---|
| 1 | Último día |
| 2 | Días 2 a 7 |
| 3 | Días 8 a 30 |
| 4 | Más de 30 días |

Para filtrar:
- **Último día:** marca el tramo 1.
- **Últimos 7 días:** marca los tramos 1 y 2.
- **Últimos 30 días:** marca los tramos 1, 2 y 3.
- **Semana o mes:** usa `Calendario[Semana]` o `Calendario[Mes]`.
- **Intervalo libre:** usa el segmentador de fecha.

## Reglas que respeta

- Los ciudadanos aparecen con seudónimo (C-0001). No hay nombres, correos, hashes ni Telegram, y el lector recibe ORA-00942 si intenta leer USUARIO.
- Los kg **estimados** y los kg **medidos** son medidas distintas y nunca se suman.
- Los rankings cuentan solo depósitos `ESTADO = "VERIFICADO"`.
- Cada página lleva el rótulo «DATOS SINTÉTICOS».
- No hay RLS porque el tablero solo muestra seudónimos y agregados. Si se publica en el servicio de Power BI, conviene agregar RLS por rol.

## Actualización del 11/11/2026 (revisión en pantalla)

- `generar_pbip.ps1` escribe el informe en el formato antiguo (`report.json` único). Power BI Desktop 2.159 **no lo carga** («No se pudo cargar el informe»). Después de generarlo hay que ejecutar:
  `pwsh -File convertir_a_pbir.ps1 -Origen <...>\EcoAccess_BD.Report -Destino <copia>\EcoAccess_BD.Report`
  El `PROYECTO_PBIP` de esta carpeta ya está convertido (formato PBIR). El original quedó en `99_VERSIONES_ANTERIORES/PBIP_original_formato_report_json/`.
- `ARCHIVO_PBIX/EcoAccess_BD.pbix`: guardado con «Guardar como», cerrado y reabierto; 5 páginas, botones (Ctrl+clic) y segmentadores comprobados. Capturas en `16_EVIDENCIAS_Y_PRUEBAS/capturas_power_bi/`.
- Conexión directa a Oracle (`power_query_oracle_directo.m`): pendiente de que la persona dueña escriba la contraseña de ECOACCESS_BD_LECTOR en Power BI (Inicio → Transformar datos → Configuración de origen de datos).
