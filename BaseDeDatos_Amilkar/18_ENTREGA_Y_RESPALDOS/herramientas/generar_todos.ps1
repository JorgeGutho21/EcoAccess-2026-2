<#
  Genera en Word y PDF todos los documentos de la entrega BaseDeDatos_Amilkar.
  1. Regenera el diccionario ampliado (03_DICCIONARIO_DE_DATOS/generar_diccionario_ampliado.ps1): se detiene si
     falta o sobra un atributo, una tabla o una de las 38 FK.
  2. Expande los marcadores <<INCLUIR:ruta>> de cada fuente HTML (fragmentos del diccionario, relaciones, triggers…).
  3. Llama a generar_documentos.ps1 (Microsoft Word) para cada documento del catálogo.
  4. Escribe 03_DICCIONARIO_DE_DATOS/VERIFICACION_DICCIONARIO_173_ATRIBUTOS.md.
  Uso, desde la carpeta raíz:  pwsh -File 18_ENTREGA_Y_RESPALDOS\herramientas\generar_todos.ps1 [-Solo INFORME,GUIA173]
  Requiere Microsoft Word. Registra páginas y tiempo de cada documento en generacion.log.
#>
param([string[]]$Solo)
$ErrorActionPreference = 'Stop'
if ($Solo) { $Solo = @($Solo | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }   # pwsh -File entrega "A,B" como un solo texto
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$gen = Join-Path $PSScriptRoot 'generar_documentos.ps1'
$utf8 = New-Object Text.UTF8Encoding $false
$F = '18_ENTREGA_Y_RESPALDOS/fuentes_documentos'

# Clave = @(fuente HTML, salida sin extensión, título)
$docs = [ordered]@{
  INFORME        = @("$F/fuente_informe.html", '01_DOCUMENTO_FINAL/02_DOCUMENTO_DETALLADO_ECOACCESS', 'Documento técnico detallado')
  INDICE         = @("$F/fuente_indice_general.html", '00_EMPEZAR_AQUI/INDICE_GENERAL', 'Índice general')
  GUIA173        = @('03_DICCIONARIO_DE_DATOS/ampliado/fuentes/guia_173_atributos.html', '03_DICCIONARIO_DE_DATOS/GUIA_COMPLETA_173_ATRIBUTOS_Y_38_RELACIONES', 'Guía de los 173 atributos y relaciones')
  FICHAS24       = @('03_DICCIONARIO_DE_DATOS/fuente_diccionario_doc.html', '03_DICCIONARIO_DE_DATOS/DICCIONARIO_FICHAS_DE_24_CAMPOS', 'Diccionario de fichas de 24 campos')
  INTERPRETACION = @("$F/O_interpretacion_modelo_flujo.html", '02_MODELOS_DE_DATOS/06_FLUJO_DE_DATOS/INTERPRETACION_MODELO_Y_FLUJO_DE_DATOS', 'Interpretación del modelo relacional')
  TRIGGERS       = @("$F/G_triggers_profesor.html", '07_DISPARADORES_TRIGGERS/GUIA_TRIGGERS_LINEA_POR_LINEA', 'Guía de triggers')
  EJERCICIOS     = @('08_CONSULTAS_Y_EJERCICIOS/fuente_guia.html', '08_CONSULTAS_Y_EJERCICIOS/GUIA_DE_ESTUDIO_309_EJERCICIOS', 'Guía de estudio con 309 ejercicios')
  ORACLE         = @("$F/fuente_oracle_conexiones_errores.html", '11_GUIAS_PASO_A_PASO/GUIA_ORACLE_CONEXIONES_Y_ERRORES_FRECUENTES', 'Guía de Oracle, conexiones y errores frecuentes')
  SQLDEVELOPER   = @("$F/D_sql_developer.html", '11_GUIAS_PASO_A_PASO/GUIA_SQL_DEVELOPER', 'Guía SQL Developer')
  DATAMODELER    = @("$F/E_data_modeler.html", '11_GUIAS_PASO_A_PASO/GUIA_DATA_MODELER', 'Guía Data Modeler')
  SQLPLSQL       = @("$F/fuente_guia_plsql.html", '11_GUIAS_PASO_A_PASO/GUIA_SQL_Y_PLSQL', 'Guía SQL y PL/SQL')
  POWERBI        = @("$F/H_power_bi.html", '11_GUIAS_PASO_A_PASO/GUIA_POWER_BI', 'Guía Power BI')
  CSHARP         = @("$F/I_csharp.html", '11_GUIAS_PASO_A_PASO/GUIA_CSHARP_VISUAL_STUDIO', 'Guía C# y Visual Studio')
  MANUAL         = @("$F/J_manual_tecnico.html", '11_GUIAS_PASO_A_PASO/MANUAL_INSTALACION_Y_RECUPERACION', 'Manual de instalación y recuperación')
  PASOAPASO      = @("$F/fuente_paso_a_paso.html", '11_GUIAS_PASO_A_PASO/PASO_A_PASO_INSTALAR_PROBAR_Y_ENTREGAR', 'Paso a paso')
  PERSONAL       = @("$F/L_guia_personal_jorge.html", '12_GUIA_PERSONAL_JORGE/GUIA_PERSONAL_JORGE_DESDE_CERO', 'Guía personal de estudio')
  SUSTENTACION   = @("$F/M_sustentacion.html", '13_SUSTENTACION/MANUAL_DE_SUSTENTACION', 'Manual de sustentación')
  PREGUNTAS      = @("$F/fuente_preguntas.html", '13_SUSTENTACION/PREGUNTAS_SUSTENTACION_MODELO_RELACIONAL', 'Preguntas de sustentación')
  CONSTRUCCION   = @("$F/N_construccion_paso_a_paso.html", '17_COMO_SE_CONSTRUYO/COMO_SE_CONSTRUYO_PASO_A_PASO', 'Cómo se construyó')
}

& pwsh -NoProfile -File (Join-Path $raiz '03_DICCIONARIO_DE_DATOS\generar_diccionario_ampliado.ps1')
if ($LASTEXITCODE) { throw 'Falló generar_diccionario_ampliado.ps1' }

$tmp = Join-Path $raiz '18_ENTREGA_Y_RESPALDOS\fuentes_documentos\_ensambladas'; New-Item -ItemType Directory -Force $tmp | Out-Null
function Incluir([string]$html) { [regex]::Replace($html, '<<INCLUIR:([^>]+)>>', { param($m) [IO.File]::ReadAllText((Join-Path $raiz $m.Groups[1].Value), $utf8) }) }
$log = Join-Path $PSScriptRoot 'generacion.log'
Push-Location $raiz
try {
  foreach ($k in $docs.Keys) {
    if ($Solo -and $k -notin $Solo) { continue }
    $v = $docs[$k]; $t0 = Get-Date
    $fuente = $v[0]
    $html = [IO.File]::ReadAllText((Join-Path $raiz $fuente), $utf8)
    if ($html.Contains('<<INCLUIR:')) { [IO.File]::WriteAllText("$tmp\$k.html", (Incluir $html), $utf8); $fuente = "18_ENTREGA_Y_RESPALDOS/fuentes_documentos/_ensambladas/$k.html" }
    $r = & pwsh -NoProfile -File $gen -Fuente $fuente -Salida $v[1] -Titulo $v[2] 2>&1 | Out-String
    $linea = "$(Get-Date -Format s) [$k] $($v[1]) :: $($r.Trim()) ($([int]((Get-Date)-$t0).TotalSeconds) s)"
    Add-Content $log $linea -Encoding UTF8; Write-Host $linea
  }
} finally { Pop-Location }

$amp = Join-Path $raiz '03_DICCIONARIO_DE_DATOS\ampliado'
$md = [IO.File]::ReadAllText("$amp\verificacion_generada.md", $utf8) + [IO.File]::ReadAllText("$amp\fuentes\verificacion_observaciones.md", $utf8)
[IO.File]::WriteAllText((Join-Path $raiz '03_DICCIONARIO_DE_DATOS\VERIFICACION_DICCIONARIO_173_ATRIBUTOS.md'), $md, $utf8)
'Listo.'
