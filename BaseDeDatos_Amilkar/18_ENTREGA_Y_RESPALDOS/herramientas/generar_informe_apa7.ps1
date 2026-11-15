<#
  Genera 01_DOCUMENTO_FINAL/01_INFORME_FINAL_ECOACCESS_APA7.docx y .pdf desde
  18_ENTREGA_Y_RESPALDOS/fuentes_documentos/fuente_informe_apa7.txt, con Microsoft Word.

  Formato APA 7 (trabajo de estudiante): carta, márgenes de 2,54 cm, Times New Roman 12, interlineado doble,
  sangría de primera línea de 1,27 cm, alineación a la izquierda, número de página arriba a la derecha,
  títulos de nivel 1 centrados en negrita, de nivel 2 a la izquierda en negrita y de nivel 3 en negrita cursiva,
  figuras y tablas numeradas con su título en cursiva, referencias con sangría francesa.
  Las figuras grandes van en páginas horizontales (excepción de composición), sin deformarlas.

  Formato de la fuente (una línea por elemento):
    # / ## / ###                 títulos
    texto                        párrafo
    - texto                      viñeta
    TABLA|Título|col;col;...     tabla; luego una fila por línea con ';' y una línea FIN
    FIGURA|ruta|Título|Nota|horizontal
    PORTADA, RESUMEN|texto, CLAVES|texto, INDICE, REFERENCIAS, ANEXOS
  Uso: pwsh -File 18_ENTREGA_Y_RESPALDOS\herramientas\generar_informe_apa7.ps1
#>
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$fuente = Join-Path $raiz '18_ENTREGA_Y_RESPALDOS\fuentes_documentos\fuente_informe_apa7.txt'
$salida = Join-Path $raiz '01_DOCUMENTO_FINAL\01_INFORME_FINAL_ECOACCESS_APA7'
$lineas = Get-Content $fuente -Encoding UTF8

$integrantes = 'Jorge Alfonso Gutiérrez Thomas', 'Heyder Hedrey Canchila de Ángel', 'Saray Marcela Cadena Palomino'
$titulo = 'EcoAccess: base de datos para la gestión y clasificación de residuos'

$w = New-Object -ComObject Word.Application
$w.Visible = $false; $w.DisplayAlerts = 0
try {
  $doc = $w.Documents.Add()
  $ps = $doc.PageSetup
  $ps.PaperSize = 2   # carta
  foreach ($m in 'TopMargin', 'BottomMargin', 'LeftMargin', 'RightMargin') { $ps.$m = 72 }
  # ---- estilos
  $normal = $doc.Styles.Item(-1)
  $normal.Font.Name = 'Times New Roman'; $normal.Font.Size = 12; $normal.Font.Color = 0
  $normal.ParagraphFormat.LineSpacingRule = 2; $normal.ParagraphFormat.SpaceBefore = 0; $normal.ParagraphFormat.SpaceAfter = 0
  $normal.ParagraphFormat.FirstLineIndent = 36; $normal.ParagraphFormat.Alignment = 0
  $niveles = @{ 1 = -2; 2 = -3; 3 = -4 }
  foreach ($n in 1, 2, 3) {
    $s = $doc.Styles.Item($niveles[$n])
    $s.Font.Name = 'Times New Roman'; $s.Font.Size = 12; $s.Font.Bold = $true; $s.Font.Italic = ($n -eq 3); $s.Font.Color = 0
    $s.ParagraphFormat.LineSpacingRule = 2; $s.ParagraphFormat.SpaceBefore = 0; $s.ParagraphFormat.SpaceAfter = 0
    $s.ParagraphFormat.FirstLineIndent = 0; $s.ParagraphFormat.LeftIndent = 0
    $s.ParagraphFormat.Alignment = $(if ($n -eq 1) { 1 } else { 0 })
    $s.ParagraphFormat.PageBreakBefore = ($n -eq 1); $s.ParagraphFormat.KeepWithNext = $true
  }
  $sel = $w.Selection
  function Fin { $sel.EndKey(6) | Out-Null }
  function Parrafo([string]$texto, [int]$estilo = -1, [int]$alin = -9, [double]$sangria = -1, [switch]$Negrita, [switch]$Cursiva, [double]$interlineado = 0) {
    Fin
    $sel.Style = $doc.Styles.Item($estilo)
    if ($alin -ne -9) { $sel.ParagraphFormat.Alignment = $alin }
    if ($sangria -ge 0) { $sel.ParagraphFormat.FirstLineIndent = $sangria }
    if ($interlineado -gt 0) { $sel.ParagraphFormat.LineSpacingRule = 0 }
    if ($Negrita) { $sel.Font.Bold = $true }; if ($Cursiva) { $sel.Font.Italic = $true }
    $sel.TypeText($texto)
    if ($Negrita) { $sel.Font.Bold = $false }; if ($Cursiva) { $sel.Font.Italic = $false }
    $sel.TypeParagraph()
  }
  function SaltoPagina { Fin; $sel.InsertBreak(7) }
  function Seccion([int]$orientacion) {
    Fin; $sel.InsertBreak(2)
    $p = $sel.Sections.Item(1).PageSetup
    if ($p.Orientation -ne $orientacion) { $p.Orientation = $orientacion }
    foreach ($m in 'TopMargin', 'BottomMargin', 'LeftMargin', 'RightMargin') { $p.$m = 72 }
  }
  $script:nFig = 0; $script:nTab = 0; $script:enHorizontal = $false
  function VolverVertical { if ($script:enHorizontal) { Seccion 0; $script:enHorizontal = $false } }
  function Figura([string]$ruta, [string]$tit, [string]$nota, [bool]$horizontal) {
    $script:nFig++
    if ($horizontal) { if ($script:enHorizontal) { SaltoPagina } else { Seccion 1; $script:enHorizontal = $true } } else { VolverVertical }
    Parrafo "Figura $($script:nFig)" -estilo -1 -sangria 0 -Negrita -interlineado 1
    $sel.Paragraphs.Item(1).Range.ParagraphFormat.KeepWithNext = $true
    Parrafo $tit -sangria 0 -Cursiva -interlineado 1
    Fin; $sel.Style = $doc.Styles.Item(-1); $sel.ParagraphFormat.FirstLineIndent = 0; $sel.ParagraphFormat.Alignment = 1; $sel.ParagraphFormat.LineSpacingRule = 0
    $img = $sel.InlineShapes.AddPicture((Join-Path $raiz ($ruta -replace '/', '\')), $false, $true)
    $pag = $sel.Sections.Item(1).PageSetup
    $maxW = $pag.PageWidth - $pag.LeftMargin - $pag.RightMargin
    $maxH = $pag.PageHeight - $pag.TopMargin - $pag.BottomMargin - $(if ($horizontal) { 95 } else { 140 })
    $img.LockAspectRatio = -1
    $f = [Math]::Min($maxW / $img.Width, $maxH / $img.Height)
    $img.Width = $img.Width * $f
    $sel.TypeParagraph()
    if ($nota) {
      Fin; $sel.Style = $doc.Styles.Item(-1); $sel.ParagraphFormat.FirstLineIndent = 0; $sel.ParagraphFormat.Alignment = 0; $sel.ParagraphFormat.LineSpacingRule = 0
      $sel.Font.Italic = $true; $sel.TypeText('Nota.'); $sel.Font.Italic = $false; $sel.TypeText(" $nota"); $sel.TypeParagraph()
    }
    if (-not $horizontal) { Parrafo '' -sangria 0 }
  }
  function Tabla([string]$tit, [string[]]$cols, [System.Collections.Generic.List[string[]]]$filas) {
    $script:nTab++
    Parrafo "Tabla $($script:nTab)" -sangria 0 -Negrita
    Parrafo $tit -sangria 0 -Cursiva
    Fin
    $t = $doc.Tables.Add($sel.Range, $filas.Count + 1, $cols.Count)
    $t.Range.Style = $doc.Styles.Item(-1)
    $t.Range.Font.Size = 10; $t.Range.ParagraphFormat.LineSpacingRule = 0; $t.Range.ParagraphFormat.FirstLineIndent = 0
    $t.Range.ParagraphFormat.SpaceBefore = 2; $t.Range.ParagraphFormat.SpaceAfter = 2; $t.Range.ParagraphFormat.Alignment = 0
    $t.Borders.Enable = 0
    for ($c = 0; $c -lt $cols.Count; $c++) { $cel = $t.Cell(1, $c + 1).Range; $cel.Text = $cols[$c]; $cel.Font.Bold = $true }
    for ($r = 0; $r -lt $filas.Count; $r++) { for ($c = 0; $c -lt $cols.Count; $c++) { $v = if ($c -lt $filas[$r].Count) { $filas[$r][$c] } else { '' }; $t.Cell($r + 2, $c + 1).Range.Text = $v } }
    $t.Rows.Item(1).Borders.Item(-1).LineStyle = 1   # superior
    $t.Rows.Item(1).Borders.Item(-3).LineStyle = 1   # inferior del encabezado
    $t.Rows.Item($t.Rows.Count).Borders.Item(-3).LineStyle = 1
    $t.Rows.Item(1).HeadingFormat = -1
    $t.PreferredWidthType = 2; $t.PreferredWidth = 100; $t.AllowAutoFit = $true; $t.AutoFitBehavior(1); $t.AutoFitBehavior(2)
    Fin; Parrafo '' -sangria 0
  }

  # ---- recorrido de la fuente
  $i = 0
  while ($i -lt $lineas.Count) {
    $l = $lineas[$i].TrimEnd(); $i++
    if (-not $l) { continue }
    if ($l -notlike 'FIGURA|*') { VolverVertical }
    if ($l -eq 'PORTADA') {
      Fin; $sel.ParagraphFormat.Alignment = 1
      1..3 | ForEach-Object { Parrafo '' -alin 1 -sangria 0 }
      Parrafo $titulo -alin 1 -sangria 0 -Negrita
      Parrafo '' -alin 1 -sangria 0
      foreach ($p in $integrantes) { Parrafo $p -alin 1 -sangria 0 }
      Parrafo 'Universidad Popular del Cesar' -alin 1 -sangria 0
      Parrafo 'Base de Datos' -alin 1 -sangria 0
      Parrafo 'Profesor Amílkar' -alin 1 -sangria 0
      Parrafo 'Noviembre de 2026' -alin 1 -sangria 0
      continue
    }
    if ($l -like 'RESUMEN|*') { SaltoPagina; Parrafo 'Resumen' -alin 1 -sangria 0 -Negrita; Parrafo ($l.Substring(8)) -sangria 0; continue }
    if ($l -like 'CLAVES|*') { Fin; $sel.Style = $doc.Styles.Item(-1); $sel.ParagraphFormat.FirstLineIndent = 36; $sel.Font.Italic = $true; $sel.TypeText('Palabras clave: '); $sel.Font.Italic = $false; $sel.TypeText($l.Substring(7)); $sel.TypeParagraph(); continue }
    if ($l -eq 'INDICE') {
      SaltoPagina; Parrafo 'Tabla de contenido' -alin 1 -sangria 0 -Negrita
      Fin; $doc.TablesOfContents.Add($sel.Range, $true, 1, 2) | Out-Null; Fin; $sel.TypeParagraph(); continue
    }
    if ($l -eq 'REFERENCIAS' -or $l -eq 'ANEXOS') {
      Fin; $sel.Style = $doc.Styles.Item(-2); $sel.TypeText($(if ($l -eq 'REFERENCIAS') { 'Referencias' } else { 'Anexos y documentos complementarios' })); $sel.TypeParagraph()
      if ($l -eq 'REFERENCIAS') {
        while ($i -lt $lineas.Count -and $lineas[$i].Trim()) {
          Fin; $sel.Style = $doc.Styles.Item(-1); $sel.ParagraphFormat.FirstLineIndent = -36; $sel.ParagraphFormat.LeftIndent = 36
          $sel.TypeText($lineas[$i].Trim()); $sel.TypeParagraph(); $i++
        }
        Fin; $sel.ParagraphFormat.FirstLineIndent = 36; $sel.ParagraphFormat.LeftIndent = 0
      }
      continue
    }
    if ($l -match '^(#{1,3}) (.+)$') { Parrafo $Matches[2] -estilo $niveles[$Matches[1].Length]; continue }
    if ($l -like '- *') { Fin; $sel.Style = $doc.Styles.Item(-49); $sel.ParagraphFormat.LineSpacingRule = 2; $sel.ParagraphFormat.SpaceAfter = 0; $sel.TypeText($l.Substring(2)); $sel.TypeParagraph(); continue }
    if ($l -like 'TABLA|*') {
      $p = $l -split '\|'; $cols = $p[2] -split ';'; $filas = New-Object 'System.Collections.Generic.List[string[]]'
      while ($lineas[$i].Trim() -ne 'FIN') { $filas.Add([string[]]($lineas[$i].Trim() -split ';')); $i++ }
      $i++; Tabla $p[1] $cols $filas; continue
    }
    if ($l -like 'FIGURA|*') { $p = $l -split '\|'; Figura $p[1] $p[2] $p[3] ($p[4] -eq 'horizontal'); continue }
    Parrafo $l
  }

  # cursiva de títulos de obras en las referencias: *texto*
  $rg = $doc.Content; $f = $rg.Find; $f.ClearFormatting(); $f.Replacement.ClearFormatting()
  $f.Text = '\*(*)\*'; $f.Replacement.Text = '\1'; $f.Replacement.Font.Italic = $true; $f.MatchWildcards = $true
  [void]$f.Execute([ref]'\*(*)\*', [ref]$false, [ref]$false, [ref]$true, [ref]$false, [ref]$false, [ref]$true, [ref]1, [ref]$true, [ref]'\1', [ref]2)

  # número de página arriba a la derecha en todas las secciones
  foreach ($s in $doc.Sections) {
    $h = $s.Headers.Item(1)
    if ($s.Index -gt 1) { $h.LinkToPrevious = $true; continue }
    $h.Range.Text = ''; $h.Range.ParagraphFormat.Alignment = 2; $h.Range.ParagraphFormat.FirstLineIndent = 0
    $h.Range.Font.Name = 'Times New Roman'; $h.Range.Font.Size = 12
    [void]$h.Range.Fields.Add($h.Range, 33)
    $h.Range.ParagraphFormat.Alignment = 2; $h.Range.ParagraphFormat.FirstLineIndent = 0
  }
  # propiedades visibles del documento
  try {
    $bp = [System.__ComObject].InvokeMember('BuiltInDocumentProperties', 'GetProperty', $null, $doc, $null)
    $props = @{ Title = $titulo; Author = ($integrantes -join '; '); Subject = 'Informe final de Base de Datos · Universidad Popular del Cesar'; Keywords = 'Oracle; PL/SQL; modelo relacional; EcoAccess' }
    foreach ($k in $props.Keys) { $it = [System.__ComObject].InvokeMember('Item', 'GetProperty', $null, $bp, @($k)); [System.__ComObject].InvokeMember('Value', 'SetProperty', $null, $it, @($props[$k])) }
  } catch { Write-Warning "No se pudieron fijar las propiedades: $($_.Exception.Message)" }

  $doc.TablesOfContents.Item(1).Update()
  $doc.Fields.Update() | Out-Null
  $doc.TablesOfContents.Item(1).Update()
  $doc.SaveAs2("$salida.docx", 16)
  $doc.ExportAsFixedFormat("$salida.pdf", 17, $false, 0)
  $paginas = $doc.ComputeStatistics(2)
  $doc.Close(0)
  "Informe APA 7: $paginas páginas · $($script:nFig) figuras · $($script:nTab) tablas -> $salida.docx / .pdf"
} finally { $w.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($w) }
