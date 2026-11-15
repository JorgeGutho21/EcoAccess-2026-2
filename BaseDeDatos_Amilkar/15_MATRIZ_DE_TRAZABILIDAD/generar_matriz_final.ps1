<#
  Genera K = MATRIZ_TRAZABILIDAD_FINAL (.csv y .xlsx) con tres hojas:
   1) Aula Web: los 5 puntos obligatorios y la PÁGINA real donde están en el informe A (leída del .docx con Word).
   2) Requisitos: RF01–RF28 y RNF1–RNF2 (de MATRIZ_TRAZABILIDAD.md) con estado y documentos.
   3) Contenidos de clase y entregables: dónde se cubre cada tema y estado VERIFICADO / PENDIENTE.
  Uso desde la raíz: pwsh -File 15_MATRIZ_DE_TRAZABILIDAD\generar_matriz_final.ps1
#>
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$utf8 = New-Object Text.UTF8Encoding $true
$informe = Join-Path $raiz '01_DOCUMENTO_FINAL\01_INFORME_FINAL_ECOACCESS_APA7.docx'   # documento que se entrega al profesor

# ---------- 1. Páginas reales de los 5 puntos en el informe
$puntos = @(
  @('1', 'Introducción', '^\d+\.\s*Introducción'),
  @('2', 'Objetivos (general y específicos)', '^\d+\.\s*Objetivos'),
  @('3', 'Modelo Entidad–Relación', '^\d+\.\s*Modelo Entidad-Relación'),
  @('4', 'Modelo Relacional', '^\d+\.\s*Modelo relacional'),
  @('5', 'Diccionario de Datos', '^\d+\.\s*Diccionario de datos'))
$paginas = @{}
$w = New-Object -ComObject Word.Application; $w.Visible = $false; $w.DisplayAlerts = 0
try {
  $d = $w.Documents.Open($informe, $false, $true)
  foreach ($p in @($d.Paragraphs)) {
    $est = $p.OutlineLevel
    if ($est -gt 2) { continue }
    $txt = $p.Range.Text.Trim()
    foreach ($q in $puntos) { if (-not $paginas[$q[0]] -and $txt -match $q[2]) { $paginas[$q[0]] = @($txt, $p.Range.Information(3)) } }
  }
  $total = $d.ComputeStatistics(2)
  $d.Close(0)
} finally { $w.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($w) }
$aula = foreach ($q in $puntos) {
  $v = $paginas[$q[0]]
  [pscustomobject]@{ Punto = $q[0]; Requisito_Aula_Web = $q[1]; Capitulo_en_informe = $(if ($v) { $v[0] } else { 'NO ENCONTRADO' }); Pagina = $(if ($v) { $v[1] } else { '' }); Documento = '01_INFORME_FINAL_ECOACCESS_APA7 (.docx/.pdf)'; Estado = $(if ($v) { 'VERIFICADO (página leída del .docx)' } else { 'PENDIENTE' }) }
}

# ---------- 2. Requisitos desde la matriz Markdown
$req = foreach ($l in [IO.File]::ReadAllLines((Join-Path $PSScriptRoot 'MATRIZ_TRAZABILIDAD.md'), $utf8)) {
  if ($l -notmatch '^\|\s*(RF\d+|RNF\d+)\s*\|') { continue }
  $c = $l.Trim().Trim('|') -split '\|' | ForEach-Object { $_.Trim() -replace '`', '' }
  [pscustomobject]@{ Id = $c[0]; Requisito = $c[1]; Tablas_atributos = $c[2]; Objeto_PLSQL = $c[3]; Trigger = $c[4]; Consulta = $c[5]; Evidencia = $c[6]
    Estado = 'VERIFICADO (pruebas 163/163 del 14/11/2026)'; Documentos = 'A (Anexo E), C, F, I' }
}

# ---------- 3. Contenidos de clase y entregables
$C = { param($t, $d, $e, $s) [pscustomobject]@{ Tema_o_entregable = $t; Donde = $d; Evidencia = $e; Estado = $s } }
$temas = @(
  (& $C 'SELECT, WHERE, ORDER BY' 'B (temas 1–3), D' '17_banco_ejercicios.txt' 'VERIFICADO')
  (& $C 'INSERT / UPDATE' 'B, P05' '17_banco_ejercicios.txt, practicas_P00_P05.txt' 'VERIFICADO')
  (& $C 'JOIN, GROUP BY / HAVING, UNION, subconsultas' 'B, I (Q01–Q22)' '13_consultas_exposicion.txt' 'VERIFICADO')
  (& $C 'Bloques anónimos, variables, IF, bucles' 'F cap. 2, B' '17_banco_ejercicios.txt' 'VERIFICADO')
  (& $C 'Cursores implícitos y explícitos' 'F, P04 (HR)' 'practicas_P00_P05.txt' 'VERIFICADO')
  (& $C 'Procedimientos, funciones, SYS_REFCURSOR, paquetes' 'F, 07_procedimientos_funciones_paquetes.sql' '12_pruebas.txt, 16_prueba_csharp.txt' 'VERIFICADO')
  (& $C 'Excepciones y RAISE_APPLICATION_ERROR' 'F, B (10 errores intencionales)' '17_banco_ejercicios.txt' 'VERIFICADO')
  (& $C 'Transacciones COMMIT / ROLLBACK' 'F, H' '12_pruebas.txt (termina en ROLLBACK), 16_prueba_csharp.txt' 'VERIFICADO')
  (& $C 'Usuarios, roles y privilegios' 'J, 09_permisos.sql, 16_prueba_lector.sql' 'T03 10/10' 'VERIFICADO')
  (& $C 'Secuencias' 'P01 (secuencia + trigger de PK)' 'P99 19/19' 'VERIFICADO')
  (& $C 'Triggers BEFORE/AFTER, :OLD/:NEW, FOR EACH ROW (4 del proyecto)' 'F, I, 08_triggers.sql' 'demo_cuatro_triggers.txt, T05' 'VERIFICADO')
  (& $C 'Informe final breve en formato APA 7 (5 puntos del Aula Web y desarrollo hasta noviembre de 2026)' '01_DOCUMENTO_FINAL/01_INFORME_FINAL_ECOACCESS_APA7' 'generar_informe_apa7.ps1; páginas en la hoja Aula Web' 'VERIFICADO (15/11/2026)')
  (& $C 'Biblioteca de modelos visuales: MER, MEER, relacional, 5 módulos, 7 exportaciones de Data Modeler y flujo, en PDF horizontal' '19_MODELOS_VISUALES (galería, atlas, PDF)' 'ORIGENES_Y_HASHES.csv; verificar_galeria.ps1 (68 enlaces, 0 rotos)' 'VERIFICADO (15/11/2026)')
  (& $C 'DMD abierto en SQL Developer: modelo lógico, Relational_1 y 5 subvistas; DDL visto como código e importado en un diseño de prueba' '19_MODELOS_VISUALES/05_DATA_MODELER y 07_DDL_Y_CODIGO' '08_CAPTURAS_REALES (24 capturas)' 'VERIFICADO (15/11/2026)')
  (& $C 'SQL Developer conectado a ECOACCESS_BD (tablas, triggers y código)' '19_MODELOS_VISUALES/08_CAPTURAS_REALES' 'Requiere la contraseña de ECOACCESS_BD escrita por su dueño' 'PENDIENTE')
  (& $C 'DMD en Data Modeler independiente' 'C:\BaseDeDatos\HerramientasOracle\datamodeler-24.3.1' 'Instalado y abierto; control no autorizado en la sesión' 'PARCIAL')
  (& $C 'Revisión PR #12 (P2): CK_DEPOSITO_MEDIDO rechaza mediciones incompletas (CHECK con UNKNOWN)' 'A cap. «Revisión técnica del Pull Request», 03_restricciones.sql, 19_correccion_revision_pr12.sql' '12_pruebas.txt P144–P151; revision_pr12/antes y después' 'VERIFICADO (14/11/2026)')
  (& $C 'Revisión PR #12 (P2): REGISTRAR_REGLA bloquea la fila de MATERIAL (concurrencia de tarifas)' 'A cap. «Revisión técnica del Pull Request», 07_procedimientos_funciones_paquetes.sql, 20_prueba_concurrencia_tarifas.sql' '20_concurrencia_tarifas.txt (T06); 12_pruebas.txt P152–P153' 'VERIFICADO (14/11/2026)')
  (& $C 'Práctica precio_final = precio_unitario*(1+utilidad/100), protegido' 'P02' 'P99 (PR05)' 'VERIFICADO')
  (& $C 'Auditoría salarial BEFORE/AFTER y nota inválida ORA-20001' 'P03' 'P99' 'VERIFICADO')
  (& $C 'HR con cursor, procedimiento y función' 'P04' 'practicas_P00_P05.txt' 'VERIFICADO')
  (& $C 'CRUD de empleado sin DELETE (PL/SQL + C#)' 'P05, I' 'capturas_csharp (4), registro_demo.txt, 16_prueba_csharp.txt (10 casos)' 'VERIFICADO (consola 10/10 el 11/11/2026 noche; ventana por automatización; la prueba reutiliza su fila, sin DELETE)')
  (& $C '309 ejercicios (299 OK + 10 errores intencionales)' 'B' 'G01: 0 fallan' 'VERIFICADO')
  (& $C 'DDL final extraído y comparado' '04_SCRIPTS_ORACLE/DDL_FINAL_EXTRAIDO.sql' 'D01–D10' 'VERIFICADO')
  (& $C 'Diccionario 173 atributos = Oracle' 'C' 'D09, D10' 'VERIFICADO')
  (& $C 'MER, MEER y modelo relacional (PNG/SVG/PDF)' '02_MODELOS_DE_DATOS (01_MER, 02_MEER, 03, 04, 06)' 'generados desde Oracle' 'VERIFICADO')
  (& $C 'Proyecto .dmd de Data Modeler (modelo lógico + relacional)' 'E, L' 'DATA_MODELER_DMD/EcoAccess_BD.dmd, verificacion_dmd.txt (PASA 19/173/38), capturas_data_modeler (4)' 'VERIFICADO (creado, guardado, cerrado y reabierto el 11/11/2026)')
  (& $C 'Power BI .pbip: modelo, 6 tablas, 6 relaciones, 15 medidas' 'H' 'powerbi_modelo_dax.txt; informe convertido a PBIR (convertir_a_pbir.ps1); B02 del instalador' 'VERIFICADO (abre y dibuja las 5 páginas)')
  (& $C 'Power BI: revisión visual de las 5 páginas, botones y filtros' 'H, L' 'capturas_power_bi (9); filtro VERDE 11/2/2 = Oracle' 'VERIFICADO')
  (& $C 'Power BI .pbix guardado y reabierto' 'H' 'ARCHIVO_PBIX/EcoAccess_BD.pbix; 01_pbix_reabierto_operacion.jpg' 'VERIFICADO')
  (& $C 'Power BI conexión directa a Oracle con lector' 'H (cap. Origen de datos), agregar_origen_oracle.ps1' 'PBIP con OrigenDatos=ORACLE (B02); conector llegó al servidor (ORA-01017 por credencial antigua); lector lee las 5 vistas en SQL*Plus' 'PARCIAL: falta escribir la contraseña del lector en Power BI, actualizar y guardar el PBIX')
  (& $C 'Capturas de SQL Developer' 'D, E' 'capturas_data_modeler (dentro de SQL Developer)' 'PARCIAL: hay capturas de Data Modeler; faltan las de hoja de trabajo con conexión (requiere contraseña)')
  (& $C 'Comparación con Programación III (rama desarrollo/telegram-doble-ia 1fa52ac, V002–V025)' 'A, I, 14_INTEGRACION_PROGRAMACION_III' 'MATRIZ_P3_vs_ACADEMICO.md/.csv; VERIFICACION_GITHUB_Y_NUBE_20261111.md' 'VERIFICADO desde GitHub (solo lectura); 17 tablas comunes contando CONSULTA_VISION de V023')
  (& $C 'Inventario de Oracle Cloud (ECOACCESS, sa-bogota-1, ECOACCESS_P3_APP)' 'A, L' 'inventario_oracle_cloud_SOLO_LECTURA.sql + ejecutar_inventario_cloud.ps1 (READ ONLY)' 'PENDIENTE: el script quedó esperando la contraseña de ECOACCESS_P3_APP en la terminal; no se escribió')
  (& $C 'Guía práctica personal (L)' 'L' 'L_GUIA_PRACTICA_PERSONAL_DE_JORGE_ECOACCESS_DESDE_CERO' 'VERIFICADO (sus consultas se ejecutaron en Oracle sin errores)')
  (& $C 'Diccionario: 24 campos por atributo y 173 SELECT de comprobación' 'C' 'diccionario_173_select.txt (0 errores ORA)' 'VERIFICADO')
(& $C 'Modelo relacional ordenado por módulos (general + M1–M5), 0 líneas superpuestas' 'A, E, O' '02_MODELOS_DE_DATOS/03_MODELO_RELACIONAL_GENERAL y 04_MODELO_RELACIONAL_POR_MODULOS' 'VERIFICADO')
(& $C '.dmd ordenado por módulos, colores y 5 subvistas; reabierto desde el proyecto' 'E' 'EXPORTACIONES_DMD (7 PDF/PNG), capturas 05–07' 'VERIFICADO')
(& $C 'MER sin cruces y flujo de datos' 'A, O' 'MER_conceptual, FLUJO_datos_ecoaccess' 'VERIFICADO')
(& $C 'Power BI: glosario de 6 tarjetas, PBIX guardado y reabierto 7:36 p. m.' 'H' '09_pbix_reabierto_inicio_con_glosario.jpg' 'VERIFICADO')
(& $C 'Documentos Word y PDF de la entrega revisados' 'Todos' 'generar_todos.ps1; auditoría Word (sin marcadores, sin imágenes fuera de margen); hoja Documentos' 'VERIFICADO (regenerados y auditados el 14/11/2026)')
  (& $C 'Informe A con los 5 puntos del Aula Web primero (Parte I) y complementos aparte (Parte II)' 'A' 'Tabla inicial con páginas calculadas por Word (PAGEREF)' 'VERIFICADO')
  (& $C 'Diccionario ampliado: 173 atributos con significado de 90+ palabras y 5+ oraciones, 19 tablas explicadas' 'Informe final (Anexo A), Guía 173 atributos' 'generar_diccionario_ampliado.ps1 (se detiene si falta o sobra un atributo); VERIFICACION_DICCIONARIO_173_ATRIBUTOS.md' 'VERIFICADO (173/173, 19/19, 13/11/2026)')
  (& $C 'Interpretación de las 38 FK (cardinalidad y opcionalidad leídas de Oracle) y de los 4 triggers por bloques' 'Informe final (Anexos A.1 y A.2), Guía 173 atributos (Partes V y VIII)' 'restricciones_oracle.txt (38 FK, 6 compuestas); código real de 08_triggers.sql' 'VERIFICADO')
  (& $C 'Banco de 43 preguntas de sustentación y 12 consultas SQL de demostración' 'Preguntas de sustentación, Guía 173 atributos (Partes IX y X)' 'Las 12 consultas se ejecutaron en Oracle el 13/11/2026 con 0 errores ORA' 'VERIFICADO')
  (& $C 'Respaldo Data Pump de los esquemas académicos' 'J, 18_ENTREGA_Y_RESPALDOS' 'log sin errores, SHA-256' 'VERIFICADO (restauración no ejecutada)')
  (& $C 'Rúbrica final posterior del docente' '—' 'No se encontró en las fuentes disponibles' 'NO DISPONIBLE')
)

# ---------- 4. Tareas del profesor: dónde está cada una (página real leída de los .docx)

$T = { param($tarea, $doc, $titulo, $sql, $carpeta, $prueba, $estado) [pscustomobject]@{ Tarea = $tarea; Documento = $doc; Titulo_en_documento = $titulo; Pagina = ''; Archivo_SQL = $sql; Carpeta = $carpeta; Prueba = $prueba; Estado = $estado } }
$tareas = @(
  (& $T 'Los 4 disparadores del proyecto final (justificación, código, antes/sentencia/después, rechazo, ROLLBACK)' 'G' 'Los cuatro disparadores del proyecto EcoAccess' '08_triggers.sql; demo_cuatro_triggers.sql' '04_SCRIPTS_ORACLE; 07_DISPARADORES_TRIGGERS' 'T05 (demo) y T01 163/163' 'VERIFICADO')
  (& $T 'Los 4 disparadores: código completo y explicación línea por línea' 'F' 'Los cuatro disparadores de EcoAccess' '08_triggers.sql' '04_SCRIPTS_ORACLE' 'T01, T05' 'VERIFICADO')
  (& $T 'Secuencia para clave primaria subrogada + trigger' 'G' 'Disparador con secuencia (clave primaria automática)' 'P01_secuencia_trigger.sql' '05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS' 'P99: SQ01' 'VERIFICADO')
  (& $T 'Precio final automático (1000 → 1200; 1500 → 1800)' 'G' 'Trigger de precio final' 'P02_precio_final.sql' '05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS' 'P99: PR01–PR06' 'VERIFICADO')
  (& $T 'Dos triggers de tema libre; BEFORE/AFTER, :OLD/:NEW, FOR EACH ROW; auditoría salarial; ejemplos 32–34' 'G' 'Disparadores de tema libre (ejemplos 32, 33 y 34 de clase)' 'P03_triggers_libres.sql' '05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS' 'P99: TL01–TL02, EJ32, EJ34' 'VERIFICADO')
  (& $T 'Bloques anónimos, variables, IF y bucles' 'F' 'Del bloque anónimo al paquete' 'P04_hr_plsql.sql; banco_ejercicios_*.sql' '05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS; 08_CONSULTAS_Y_EJERCICIOS' 'G01 (309 ejercicios)' 'VERIFICADO')
  (& $T 'Cursores implícitos y explícitos; funciones con cursores; procedimientos; funciones' 'F' 'Del bloque anónimo al paquete' 'P04_hr_plsql.sql; 07_procedimientos_funciones_paquetes.sql' '05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS; 04_SCRIPTS_ORACLE' 'I03, T01' 'VERIFICADO')
  (& $T 'Ejercicios del esquema HR (solo lectura)' 'F' 'Prácticas de la tarea (esquema ECOACCESS_PRACTICAS)' 'P04_hr_plsql.sql' '05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS' 'I03' 'VERIFICADO')
  (& $T 'CRUD de empleados sin DELETE (PL/SQL + C#)' 'I' 'Prueba de consola (10 casos)' 'P05_crud_sin_delete.sql' '05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS; 10_CSHARP_VISUAL_STUDIO' 'P99: CR01–CR04; N01 10/10' 'VERIFICADO')
  (& $T 'Consultas SQL básicas y avanzadas (22 de exposición)' 'B' 'Banco de 309 ejercicios verificados' '13_consultas_exposicion.sql' '04_SCRIPTS_ORACLE' 'T04' 'VERIFICADO')
  (& $T 'Banco de 309 ejercicios SQL y PL/SQL' 'B' 'Banco de 309 ejercicios verificados' 'banco_ejercicios_BD.sql; banco_ejercicios_PRACTICAS.sql' '08_CONSULTAS_Y_EJERCICIOS' 'G01: 299 OK + 10 errores intencionales' 'VERIFICADO')
  (& $T 'Informe: 5 puntos del Aula Web' 'A' 'Dónde están los cinco puntos exigidos en el Aula Web' '—' '01_DOCUMENTO_FINAL' 'Páginas calculadas por Word' 'VERIFICADO')
)
$docsInfo = @()
# Documentos de la entrega: clave usada en las tareas -> ruta del .docx (estructura BaseDeDatos_Amilkar)
$catalogo = [ordered]@{
  APA7 = '01_DOCUMENTO_FINAL\01_INFORME_FINAL_ECOACCESS_APA7.docx'
  A = '01_DOCUMENTO_FINAL\02_DOCUMENTO_DETALLADO_ECOACCESS.docx'
  INDICE = '00_EMPEZAR_AQUI\INDICE_GENERAL.docx'
  INTERPRETACION = '02_MODELOS_DE_DATOS\06_FLUJO_DE_DATOS\INTERPRETACION_MODELO_Y_FLUJO_DE_DATOS.docx'
  GUIA173 = '03_DICCIONARIO_DE_DATOS\GUIA_COMPLETA_173_ATRIBUTOS_Y_38_RELACIONES.docx'
  C = '03_DICCIONARIO_DE_DATOS\DICCIONARIO_FICHAS_DE_24_CAMPOS.docx'
  G = '07_DISPARADORES_TRIGGERS\GUIA_TRIGGERS_LINEA_POR_LINEA.docx'
  B = '08_CONSULTAS_Y_EJERCICIOS\GUIA_DE_ESTUDIO_309_EJERCICIOS.docx'
  ORACLE = '11_GUIAS_PASO_A_PASO\GUIA_ORACLE_CONEXIONES_Y_ERRORES_FRECUENTES.docx'
  D = '11_GUIAS_PASO_A_PASO\GUIA_SQL_DEVELOPER.docx'
  E = '11_GUIAS_PASO_A_PASO\GUIA_DATA_MODELER.docx'
  F = '11_GUIAS_PASO_A_PASO\GUIA_SQL_Y_PLSQL.docx'
  H = '11_GUIAS_PASO_A_PASO\GUIA_POWER_BI.docx'
  I = '11_GUIAS_PASO_A_PASO\GUIA_CSHARP_VISUAL_STUDIO.docx'
  J = '11_GUIAS_PASO_A_PASO\MANUAL_INSTALACION_Y_RECUPERACION.docx'
  PASO = '11_GUIAS_PASO_A_PASO\PASO_A_PASO_INSTALAR_PROBAR_Y_ENTREGAR.docx'
  L = '12_GUIA_PERSONAL_JORGE\GUIA_PERSONAL_JORGE_DESDE_CERO.docx'
  M = '13_SUSTENTACION\MANUAL_DE_SUSTENTACION.docx'
  PREGUNTAS = '13_SUSTENTACION\PREGUNTAS_SUSTENTACION_MODELO_RELACIONAL.docx'
  N = '17_COMO_SE_CONSTRUYO\COMO_SE_CONSTRUYO_PASO_A_PASO.docx'
}
$w2 = New-Object -ComObject Word.Application; $w2.Visible = $false; $w2.DisplayAlerts = 0
try {
  $cache = @{}
  foreach ($clave in $catalogo.Keys) {
    $ruta = Join-Path $raiz $catalogo[$clave]
    if (-not (Test-Path $ruta)) { $docsInfo += [pscustomobject]@{ Letra = $clave; Documento = [IO.Path]::GetFileNameWithoutExtension($ruta); Paginas = 'FALTA'; DOCX = 'FALTA'; PDF = 'FALTA'; Generado = '' }; continue }
    $d = $w2.Documents.Open($ruta, $false, $true); $tit = @{}
    foreach ($p in @($d.Paragraphs)) { if ($p.OutlineLevel -le 2) { $x = ($p.Range.Text -replace "[\r\a]", '').Trim() -replace '^\d+(\.\d+)*\.?\s+', ''; if ($x -and -not $tit.ContainsKey($x)) { $tit[$x] = $p.Range.Information(3) } } }
    $cache[$clave] = $tit
    $pdf = [IO.Path]::ChangeExtension($ruta, '.pdf'); $rel = $catalogo[$clave].Replace('\', '/')
    $docsInfo += [pscustomobject]@{ Letra = $clave; Documento = [IO.Path]::GetFileNameWithoutExtension($ruta); Paginas = $d.ComputeStatistics(2); DOCX = $rel; PDF = $(if (Test-Path $pdf) { [IO.Path]::ChangeExtension($rel, '.pdf') } else { 'FALTA' }); Generado = (Get-Item $ruta).LastWriteTime.ToString('yyyy-MM-dd HH:mm') }
    $d.Close(0)
  }
  foreach ($x in $tareas) { $m = $cache[$x.Documento]; if ($m -and $m.ContainsKey($x.Titulo_en_documento)) { $x.Pagina = $m[$x.Titulo_en_documento] } else { $x.Pagina = 'NO ENCONTRADO'; $x.Estado = 'REVISAR' } }
  foreach ($x in $tareas) { if ($catalogo.Contains($x.Documento)) { $x.Documento = [IO.Path]::GetFileNameWithoutExtension($catalogo[$x.Documento]) } }
} finally { $w2.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($w2) }$docsInfo += [pscustomobject]@{ Letra = 'K'; Documento = 'K_MATRIZ_TRAZABILIDAD_FINAL (este Excel)'; Paginas = '—'; DOCX = '—'; PDF = '—'; Generado = (Get-Date -Format 'yyyy-MM-dd HH:mm') }
$guia = @(
  [pscustomobject]@{ Hoja = 'Resumen'; Columna = 'Total / Verificado / Parcial / Pendiente'; Significado = 'Fórmulas CONTAR.SI sobre las columnas Estado de las demás hojas: se recalculan solas si se cambia un estado.' }
  [pscustomobject]@{ Hoja = 'Aula Web 5 puntos'; Columna = 'Capitulo_en_informe / Pagina'; Significado = 'Título y página real del informe A, leídos del .docx con Word (no escritos a mano).' }
  [pscustomobject]@{ Hoja = 'Requisitos RF RNF'; Columna = 'Tablas_atributos / Objeto_PLSQL / Trigger / Consulta / Evidencia'; Significado = 'Dónde se implementa cada requisito en Oracle y qué prueba lo demuestra.' }
  [pscustomobject]@{ Hoja = 'Contenidos y entregables'; Columna = 'Donde / Evidencia / Estado'; Significado = 'Documento (letra) que lo explica, archivo de evidencia y nivel de comprobación.' }
  [pscustomobject]@{ Hoja = 'Tareas del profesor'; Columna = 'Documento / Pagina / Archivo_SQL / Carpeta / Prueba'; Significado = 'Para abrir de inmediato cualquier tarea: documento y página (leída del .docx), script y carpeta, y la prueba automática que la comprueba.' }
  [pscustomobject]@{ Hoja = 'Documentos'; Columna = 'Paginas / DOCX / PDF / Generado'; Significado = 'Cada documento, su número de páginas según Word, si existe su PDF y cuándo se generó.' }
  [pscustomobject]@{ Hoja = '(todas)'; Columna = 'Estado'; Significado = 'VERIFICADO = ejecutado y comprobado en este PC con evidencia; PARCIAL = hecho pero falta una comprobación (se dice cuál); PENDIENTE = no se pudo comprobar (se dice por qué); NO DISPONIBLE = no existe la fuente.' }
)
# ---------- CSV
$out = $PSScriptRoot
$aula | Export-Csv (Join-Path $out 'MATRIZ_K1_AULA_WEB_5_PUNTOS.csv') -NoTypeInformation -Encoding utf8BOM
$req | Export-Csv (Join-Path $out 'MATRIZ_K2_REQUISITOS.csv') -NoTypeInformation -Encoding utf8BOM
$temas | Export-Csv (Join-Path $out 'MATRIZ_K3_CONTENIDOS_Y_ENTREGABLES.csv') -NoTypeInformation -Encoding utf8BOM
$tareas | Export-Csv (Join-Path $out 'MATRIZ_K4_TAREAS_DEL_PROFESOR.csv') -NoTypeInformation -Encoding utf8BOM
$docsInfo | Export-Csv (Join-Path $out 'MATRIZ_K5_DOCUMENTOS.csv') -NoTypeInformation -Encoding utf8BOM

# ---------- XLSX con Excel
$xl = New-Object -ComObject Excel.Application; $xl.Visible = $false; $xl.DisplayAlerts = $false
try {
  $wb = $xl.Workbooks.Add()
  while ($wb.Worksheets.Count -lt 7) { [void]$wb.Worksheets.Add([Type]::Missing, $wb.Worksheets.Item($wb.Worksheets.Count)) }
  $hojas = @(@('Aula Web 5 puntos', $aula), @('Requisitos RF RNF', $req), @('Contenidos y entregables', $temas), @('Tareas del profesor', $tareas), @('Documentos', $docsInfo), @('Cómo leer', $guia))
  for ($i = 0; $i -lt 6; $i++) {
    $ws = $wb.Worksheets.Item($i + 1); $ws.Name = $hojas[$i][0]; $datos = @($hojas[$i][1])
    $cols = $datos[0].PSObject.Properties.Name
    for ($c = 0; $c -lt $cols.Count; $c++) { $ws.Cells.Item(1, $c + 1).Value2 = $cols[$c] }
    for ($r = 0; $r -lt $datos.Count; $r++) { for ($c = 0; $c -lt $cols.Count; $c++) { $ws.Cells.Item($r + 2, $c + 1).Value2 = [string]$datos[$r].($cols[$c]) } }
    $rg = $ws.Range($ws.Cells.Item(1, 1), $ws.Cells.Item($datos.Count + 1, $cols.Count))
    [void]$ws.ListObjects.Add(1, $rg, $null, 1)
    $ws.Columns.ColumnWidth = 28; $rg.WrapText = $true; $rg.VerticalAlignment = -4160
  }
  # Hoja Resumen con fórmulas (se recalculan si cambia un estado)
  $ws = $wb.Worksheets.Item(7); $ws.Name = 'Resumen'; $ws.Move($wb.Worksheets.Item(1))
  $ws = $wb.Worksheets.Item('Resumen')
  $ws.Cells.Item(1, 1).Value2 = 'K · Matriz de trazabilidad — EcoAccess BD — Profesor Amílkar'
  $enc = 'Hoja', 'Filas', 'VERIFICADO', 'PARCIAL', 'PENDIENTE', 'Otros'
  for ($c = 0; $c -lt $enc.Count; $c++) { $ws.Cells.Item(3, $c + 1).Value2 = $enc[$c] }
  $fila = 4
  foreach ($h in @(@('Aula Web 5 puntos', 'F'), @('Requisitos RF RNF', 'H'), @('Contenidos y entregables', 'D'), @('Tareas del profesor', 'H'))) {
    $ref = "'$($h[0])'!$($h[1]):$($h[1])"
    $ws.Cells.Item($fila, 1).Value2 = $h[0]
    $ws.Cells.Item($fila, 2).Formula = "=COUNTA($ref)-1"
    $ws.Cells.Item($fila, 3).Formula = "=COUNTIF($ref,""VERIFICADO*"")"
    $ws.Cells.Item($fila, 4).Formula = "=COUNTIF($ref,""PARCIAL*"")"
    $ws.Cells.Item($fila, 5).Formula = "=COUNTIF($ref,""PENDIENTE*"")"
    $ws.Cells.Item($fila, 6).Formula = "=B$fila-C$fila-D$fila-E$fila"
    $fila++
  }
  $ws.Cells.Item($fila, 1).Value2 = 'TOTAL'
  foreach ($c in 2..6) { $col = [char](64 + $c); $ws.Cells.Item($fila, $c).Formula = "=SUM(${col}4:${col}$($fila-1))" }
  $ws.Range("A3:F$fila").Borders.LineStyle = 1; $ws.Range('A3:F3').Font.Bold = $true; $ws.Range("A${fila}:F$fila").Font.Bold = $true; $ws.Cells.Item(1, 1).Font.Bold = $true; $ws.Cells.Item(1, 1).Font.Size = 14
  $ws.Columns.Item(1).ColumnWidth = 30; $ws.Range('B:F').ColumnWidth = 13
  $ws.Cells.Item($fila + 2, 1).Value2 = "Generado: $(Get-Date -Format 'yyyy-MM-dd HH:mm'). Significado de cada columna: hoja «Cómo leer»."
  $xl.Calculate()
  $resumen = for ($r = 4; $r -le $fila; $r++) { "{0,-28} filas {1,3} · verificado {2,3} · parcial {3,2} · pendiente {4,2} · otros {5,2}" -f $ws.Cells.Item($r, 1).Text, $ws.Cells.Item($r, 2).Text, $ws.Cells.Item($r, 3).Text, $ws.Cells.Item($r, 4).Text, $ws.Cells.Item($r, 5).Text, $ws.Cells.Item($r, 6).Text }
  $wb.Worksheets.Item(1).Activate()
  $xlsx = Join-Path $out 'K_MATRIZ_TRAZABILIDAD_FINAL.xlsx'
  if (Test-Path $xlsx) { Remove-Item $xlsx }
  $wb.SaveAs($xlsx, 51); $wb.Close($false)
} finally { $xl.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($xl) }
"Informe A: $total páginas"
$aula | Format-Table Punto, Requisito_Aula_Web, Capitulo_en_informe, Pagina -AutoSize | Out-String -Width 200
"Requisitos: $(@($req).Count) · Temas/entregables: $(@($temas).Count) · Tareas: $(@($tareas).Count) · Documentos: $(@($docsInfo).Count)"
$resumen
$tareas | Format-Table Tarea, Documento, Pagina, Estado -AutoSize | Out-String -Width 220
