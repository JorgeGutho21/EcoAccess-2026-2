<#
  Reorganiza la entrega de Base de Datos en la estructura definitiva BaseDeDatos_Amilkar.
  COPIA (no mueve ni borra) desde la carpeta de trabajo EcoAccessBD_Exposoftware.
  1. Calcula la ruta nueva de cada archivo con reglas ordenadas (la primera que coincide gana).
  2. Copia el archivo y registra la correspondencia en 18_ENTREGA_Y_RESPALDOS/CORRESPONDENCIA_RUTAS_ANTIGUAS_NUEVAS.csv.
  3. Reescribe, en los archivos de texto copiados, cada ruta antigua por la nueva (archivo por archivo y carpeta por carpeta).
  Uso: pwsh -File BaseDeDatos_Amilkar_reorganizar.ps1 -Origen <EcoAccessBD_Exposoftware> -Destino <BaseDeDatos_Amilkar>
#>
param([Parameter(Mandatory)] [string]$Origen, [Parameter(Mandatory)] [string]$Destino)
$ErrorActionPreference = 'Stop'
$utf8 = New-Object Text.UTF8Encoding $false
if (Test-Path $Destino) { throw "El destino ya existe: $Destino (no se sobrescribe)." }

# Excluidos de la entrega nueva (siguen intactos en el origen y en el respaldo verificado)
$excluir = @(
  '^99_VERSIONES_ANTERIORES/', '^_LOCAL_NO_PUBLICAR/', '/(bin|obj|\.vs)/', '(^|/)~\$', '\.tmp$',
  '^MANIFIESTO_FINAL\.md$',                                           # se regenera
  '^13_POWER_BI/ARCHIVO_PBIX/EcoAccess bd\.pbix$',                    # informe vacío guardado por accidente (1 página sin visuales)
  '^00_DOCUMENTOS_FINALES_WORD_PDF/A_INFORME_FINAL_BASE_DE_DATOS_AMILKAR\.(docx|pdf)$',   # reemplazado por el informe definitivo
  '^00_DOCUMENTOS_FINALES_WORD_PDF/AMPLIADOS_173_ATRIBUTOS/A_INFORME_FINAL_BASE_DE_DATOS_AMILKAR_AMPLIADO\.(docx|pdf)$',  # se regenera como informe definitivo
  '^00_DOCUMENTOS_FINALES_WORD_PDF/AMPLIADOS_173_ATRIBUTOS/(fuentes_ensambladas/|generacion\.log)',   # intermedios regenerables
  '^00_DOCUMENTOS_FINALES_WORD_PDF/AMPLIADOS_173_ATRIBUTOS/ECOACCESS_VERIFICACION',                  # se regenera en 03
  '^16_RESPALDO_Y_RECUPERACION/.*\.DMP$'                              # respaldo Data Pump: solo local (ver 18_ENTREGA_Y_RESPALDOS/RESPALDOS_LOCALES)
)
$D = '00_DOCUMENTOS_FINALES_WORD_PDF'
$reglas = [ordered]@{
  # --- documentos Word/PDF
  "^$D/AMPLIADOS_173_ATRIBUTOS/ECOACCESS_GUIA_COMPLETA_173_ATRIBUTOS_Y_RELACIONES\.(docx|pdf)$" = '03_DICCIONARIO_DE_DATOS/GUIA_COMPLETA_173_ATRIBUTOS_Y_38_RELACIONES.$1'
  "^$D/AMPLIADOS_173_ATRIBUTOS/ECOACCESS_PREGUNTAS_SUSTENTACION_MODELO_RELACIONAL\.(docx|pdf)$" = '13_SUSTENTACION/PREGUNTAS_SUSTENTACION_MODELO_RELACIONAL.$1'
  "^$D/B_GUIA_DE_ESTUDIO_COMPLETA\.(docx|pdf)$" = '08_CONSULTAS_Y_EJERCICIOS/GUIA_DE_ESTUDIO_309_EJERCICIOS.$1'
  "^$D/C_DICCIONARIO_DE_DATOS_EXPLICADO_ATRIBUTO_POR_ATRIBUTO\.(docx|pdf)$" = '03_DICCIONARIO_DE_DATOS/DICCIONARIO_FICHAS_DE_24_CAMPOS.$1'
  "^$D/D_GUIA_SQL_DEVELOPER_DESDE_CERO\.(docx|pdf)$" = '11_GUIAS_PASO_A_PASO/GUIA_SQL_DEVELOPER.$1'
  "^$D/E_GUIA_ORACLE_DATA_MODELER\.(docx|pdf)$" = '11_GUIAS_PASO_A_PASO/GUIA_DATA_MODELER.$1'
  "^$D/F_GUIA_SQL_Y_PLSQL\.(docx|pdf)$" = '11_GUIAS_PASO_A_PASO/GUIA_SQL_Y_PLSQL.$1'
  "^$D/G_GUIA_TRIGGERS_DEL_PROFESOR\.(docx|pdf)$" = '07_DISPARADORES_TRIGGERS/GUIA_TRIGGERS_LINEA_POR_LINEA.$1'
  "^$D/H_GUIA_POWER_BI_ORACLE\.(docx|pdf)$" = '11_GUIAS_PASO_A_PASO/GUIA_POWER_BI.$1'
  "^$D/I_GUIA_CSHARP_ORACLE_Y_P3\.(docx|pdf)$" = '11_GUIAS_PASO_A_PASO/GUIA_CSHARP_VISUAL_STUDIO.$1'
  "^$D/J_MANUAL_TECNICO_INSTALACION_Y_RECUPERACION\.(docx|pdf)$" = '11_GUIAS_PASO_A_PASO/MANUAL_INSTALACION_Y_RECUPERACION.$1'
  "^$D/L_GUIA_PRACTICA_PERSONAL_DE_JORGE_ECOACCESS_DESDE_CERO\.(docx|pdf)$" = '12_GUIA_PERSONAL_JORGE/GUIA_PERSONAL_JORGE_DESDE_CERO.$1'
  "^$D/M_MANUAL_DE_SUSTENTACION_COMO_EXPONER\.(docx|pdf)$" = '13_SUSTENTACION/MANUAL_DE_SUSTENTACION.$1'
  "^$D/N_COMO_SE_CONSTRUYO_PASO_A_PASO\.(docx|pdf)$" = '17_COMO_SE_CONSTRUYO/COMO_SE_CONSTRUYO_PASO_A_PASO.$1'
  "^$D/O_INTERPRETACION_MODELO_RELACIONAL_Y_FLUJO_DE_DATOS\.(docx|pdf)$" = '02_MODELOS_DE_DATOS/06_FLUJO_DE_DATOS/INTERPRETACION_MODELO_Y_FLUJO_DE_DATOS.$1'
  '^00_LEEME_PRIMERO/PASO_A_PASO\.(docx|pdf)$' = '11_GUIAS_PASO_A_PASO/PASO_A_PASO_INSTALAR_PROBAR_Y_ENTREGAR.$1'
  "^$D/fuentes/(.+)$" = '18_ENTREGA_Y_RESPALDOS/fuentes_documentos/$1'
  "^$D/(generar_todos\.ps1|generacion\.log)$" = '18_ENTREGA_Y_RESPALDOS/herramientas/$1'
  '^07_INFORME_FINAL_WORD_PDF/(fuente_.+\.html)$' = '18_ENTREGA_Y_RESPALDOS/fuentes_documentos/$1'
  '^07_INFORME_FINAL_WORD_PDF/(generar_documentos\.ps1)$' = '18_ENTREGA_Y_RESPALDOS/herramientas/$1'
  '^11_GUIA_PLSQL/(.+)$' = '18_ENTREGA_Y_RESPALDOS/fuentes_documentos/$1'
  # --- herramientas y control
  '^00_HERRAMIENTAS/estado_manifiesto\.md$' = '18_ENTREGA_Y_RESPALDOS/herramientas/estado_manifiesto.md'
  '^00_HERRAMIENTAS/(.+)$' = '18_ENTREGA_Y_RESPALDOS/herramientas/$1'
  '^LEEME_PRIMERO\.md$' = '17_COMO_SE_CONSTRUYO/historial/LEEME_PRIMERO_20261114.md'
  '^INFORME_DE_CIERRE_20261111\.md$' = '17_COMO_SE_CONSTRUYO/historial/INFORME_DE_CIERRE_20261111.md'
  '^INSTALAR_Y_PROBAR\.ps1$' = 'INSTALAR_Y_PROBAR.ps1'
  '^01_REQUISITOS_Y_ANALISIS/01_DIAGNOSTICO\.md$' = '17_COMO_SE_CONSTRUYO/historial/DIAGNOSTICO_INICIAL_20261110.md'
  # --- modelos
  '^02_MODELOS_MER_MEER_RELACIONAL/EXPORTACIONES/(MER_.+)$' = '02_MODELOS_DE_DATOS/01_MER/$1'
  '^02_MODELOS_MER_MEER_RELACIONAL/EXPORTACIONES/(MEER_.+)$' = '02_MODELOS_DE_DATOS/02_MEER/$1'
  '^02_MODELOS_MER_MEER_RELACIONAL/EXPORTACIONES/(MR_00_.+)$' = '02_MODELOS_DE_DATOS/03_MODELO_RELACIONAL_GENERAL/$1'
  '^02_MODELOS_MER_MEER_RELACIONAL/EXPORTACIONES/(MR_M.+)$' = '02_MODELOS_DE_DATOS/04_MODELO_RELACIONAL_POR_MODULOS/$1'
  '^02_MODELOS_MER_MEER_RELACIONAL/EXPORTACIONES/(FLUJO_.+)$' = '02_MODELOS_DE_DATOS/06_FLUJO_DE_DATOS/$1'
  '^02_MODELOS_MER_MEER_RELACIONAL/DATA_MODELER_DMD/(.+)$' = '02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE/$1'
  '^02_MODELOS_MER_MEER_RELACIONAL/([^/]+\.ps1)$' = '02_MODELOS_DE_DATOS/08_HERRAMIENTAS_DE_GENERACION/$1'
  '^06_PRUEBAS_Y_EVIDENCIAS/capturas_data_modeler/(.+)$' = '02_MODELOS_DE_DATOS/07_EVIDENCIAS/capturas_data_modeler/$1'
  '^06_PRUEBAS_Y_EVIDENCIAS/salidas/verificacion_dmd\.txt$' = '02_MODELOS_DE_DATOS/07_EVIDENCIAS/verificacion_dmd.txt'
  # --- diccionario, scripts, ejercicios
  '^05_DICCIONARIO_DE_DATOS/(.+)$' = '03_DICCIONARIO_DE_DATOS/$1'
  '^03_SCRIPTS_ORACLE/(.+)$' = '04_SCRIPTS_ORACLE/$1'
  '^04_PLSQL_Y_TRIGGERS/practicas/(.+)$' = '05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS/$1'
  '^04_PLSQL_Y_TRIGGERS/(demo_cuatro_triggers\.sql)$' = '07_DISPARADORES_TRIGGERS/$1'
  '^08_GUIA_DE_ESTUDIO_WORD_PDF/(.+)$' = '08_CONSULTAS_Y_EJERCICIOS/$1'
  # --- Power BI, C#, P3, matriz, evidencias
  '^13_POWER_BI/(.+)$' = '09_POWER_BI/$1'
  '^12_GUIA_CSHARP_ORACLE/(.+)$' = '10_CSHARP_VISUAL_STUDIO/$1'
  '^14_GUIA_SUSTENTACION/(.+)$' = '13_SUSTENTACION/$1'
  '^18_INTEGRACION_P3/(.+)$' = '14_INTEGRACION_PROGRAMACION_III/$1'
  '^15_MATRIZ_TRAZABILIDAD/(.+)$' = '15_MATRIZ_DE_TRAZABILIDAD/$1'
  '^06_PRUEBAS_Y_EVIDENCIAS/(.+)$' = '16_EVIDENCIAS_Y_PRUEBAS/$1'
  '^17_VALIDACION_FINAL/(.+)$' = '16_EVIDENCIAS_Y_PRUEBAS/$1'
  '^16_RESPALDO_Y_RECUPERACION/99_PLAN_REVERSION\.md$' = '18_ENTREGA_Y_RESPALDOS/PLAN_DE_REVERSION.md'
  '^16_RESPALDO_Y_RECUPERACION/(.+\.log)$' = '18_ENTREGA_Y_RESPALDOS/registro_respaldo_data_pump/$1'
}

$mapa = New-Object Collections.Generic.List[object]; $sinRegla = @(); $excluidos = @()
foreach ($f in Get-ChildItem $Origen -Recurse -File -Force) {
  $rel = $f.FullName.Substring($Origen.Length + 1).Replace('\', '/')
  if ($excluir | Where-Object { $rel -match $_ }) { $excluidos += $rel; continue }
  $nuevo = $null
  foreach ($k in $reglas.Keys) { if ($rel -match $k) { $nuevo = [regex]::Replace($rel, $k, $reglas[$k]); break } }
  if (-not $nuevo) { $sinRegla += $rel; continue }
  $dst = Join-Path $Destino ($nuevo.Replace('/', '\'))
  New-Item -ItemType Directory -Force (Split-Path $dst) | Out-Null
  if (Test-Path $dst) { throw "Dos archivos irían al mismo destino: $nuevo" }
  Copy-Item $f.FullName $dst
  $mapa.Add([pscustomobject]@{ Antigua = $rel; Nueva = $nuevo; Bytes = $f.Length; SHA256 = (Get-FileHash $f.FullName).Hash })
}
if ($sinRegla) { throw "Archivos sin regla (no se copiaron):`n$($sinRegla -join "`n")" }

# Pares de reemplazo: archivo a archivo y, cuando todos los archivos de una carpeta van a la misma carpeta nueva, carpeta a carpeta
$pares = @{}
foreach ($m in $mapa) { if ($m.Antigua -ne $m.Nueva) { $pares[$m.Antigua] = $m.Nueva } }
$porCarpeta = $mapa | Group-Object { ($_.Antigua -replace '/[^/]+$', '') }
foreach ($g in $porCarpeta) {
  if ($g.Name -notmatch '/' -and $g.Name -notmatch '^\d\d_') { continue }
  $destinos = $g.Group | ForEach-Object { $_.Nueva -replace '/[^/]+$', '' } | Sort-Object -Unique
  if (@($destinos).Count -eq 1 -and $g.Name -ne [string]$destinos) { $pares[$g.Name] = [string]$destinos }
}
$pares[$Origen.TrimEnd('\')] = $Destino.TrimEnd('\')   # rutas absolutas antiguas (p. ej. origen CSV de Power BI)
# carpetas raíz que se renombran completas
foreach ($r in @{ '03_SCRIPTS_ORACLE' = '04_SCRIPTS_ORACLE'; '05_DICCIONARIO_DE_DATOS' = '03_DICCIONARIO_DE_DATOS'; '08_GUIA_DE_ESTUDIO_WORD_PDF' = '08_CONSULTAS_Y_EJERCICIOS'
                  '13_POWER_BI' = '09_POWER_BI'; '12_GUIA_CSHARP_ORACLE' = '10_CSHARP_VISUAL_STUDIO'; '18_INTEGRACION_P3' = '14_INTEGRACION_PROGRAMACION_III'
                  '15_MATRIZ_TRAZABILIDAD' = '15_MATRIZ_DE_TRAZABILIDAD'; '06_PRUEBAS_Y_EVIDENCIAS' = '16_EVIDENCIAS_Y_PRUEBAS'; '17_VALIDACION_FINAL' = '16_EVIDENCIAS_Y_PRUEBAS'
                  '00_HERRAMIENTAS' = '18_ENTREGA_Y_RESPALDOS/herramientas'; '02_MODELOS_MER_MEER_RELACIONAL/DATA_MODELER_DMD' = '02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE'
                  '04_PLSQL_Y_TRIGGERS/practicas' = '05_SQL_Y_PLSQL/00_PRACTICAS_VERIFICADAS' }.GetEnumerator()) { $pares[$r.Key] = $r.Value }
$orden = $pares.Keys | Sort-Object { $_.Length } -Descending

$texto = '\.(ps1|sql|md|html|txt|csv|cs|csproj|json|m|dax|bim|xml|sln|log|pbip|pbir|pbism)$'
$cambiados = 0
foreach ($m in $mapa) {
  if ($m.Nueva -notmatch $texto -or $m.Nueva -match '05_DATA_MODELER_EDITABLE/EcoAccess_BD/') { continue }
  $p = Join-Path $Destino ($m.Nueva.Replace('/', '\'))
  $t = [IO.File]::ReadAllText($p); $orig = $t
  foreach ($k in $orden) {
    $v = $pares[$k]
    $t = $t.Replace($k, $v)
    $kb = $k.Replace('/', '\'); if ($kb -ne $k) { $t = $t.Replace($kb, $v.Replace('/', '\')) }
  }
  if ($t -ne $orig) { $bom = [IO.File]::ReadAllBytes($p)[0..2] -join ',' -eq '239,187,191'; [IO.File]::WriteAllText($p, $t, (New-Object Text.UTF8Encoding $bom)); $cambiados++ }
}
$csv = Join-Path $Destino '18_ENTREGA_Y_RESPALDOS\CORRESPONDENCIA_RUTAS_ANTIGUAS_NUEVAS.csv'
$mapa | Export-Csv $csv -Encoding utf8BOM -Delimiter ';'
$excl = Join-Path $Destino '18_ENTREGA_Y_RESPALDOS\ARCHIVOS_EXCLUIDOS_DE_LA_ENTREGA.csv'
$excluidos | ForEach-Object { [pscustomobject]@{ Ruta = $_ } } | Export-Csv $excl -Encoding utf8BOM -Delimiter ';'
"Copiados: $($mapa.Count) · excluidos: $($excluidos.Count) · archivos de texto con rutas actualizadas: $cambiados · pares de reemplazo: $($pares.Count)"
