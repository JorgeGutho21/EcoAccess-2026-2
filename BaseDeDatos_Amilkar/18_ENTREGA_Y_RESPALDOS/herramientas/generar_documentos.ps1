<#
  Arma un documento Word (.docx) y PDF a partir de una fuente HTML con marcadores:
    {{IMG:ruta|ancho}}        imagen (se incrusta en el .docx)
    {{FILE:ruta}}             archivo de texto completo, escapado (para <pre>)
    {{FILELINES:ruta:a:b}}    líneas a..b de un archivo de texto
    {{HTML:ruta}}             fragmento HTML (diccionario, ejercicios)
    {{MD:ruta}}               tablas Markdown → HTML
    [[TOC]]                   índice automático de Word (niveles 1-2)
  Numera los capítulos (h1/h2) salvo los de clase "sinnum" y los anexos.
  Las rutas son relativas a la carpeta raíz EcoAccess_BD_Amilkar. Requiere Microsoft Word.
  Uso: pwsh -File generar_documentos.ps1 -Fuente 18_ENTREGA_Y_RESPALDOS\fuentes_documentos\fuente_informe.html -Salida 07_INFORME_FINAL_WORD_PDF\Informe_Final_EcoAccess_BD -Titulo "Informe final"
#>
param([Parameter(Mandatory)] [string]$Fuente, [Parameter(Mandatory)] [string]$Salida, [string]$Titulo = 'EcoAccess BD')
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))   # herramientas -> 18 -> raíz
$utf8 = New-Object Text.UTF8Encoding $false
function Ruta($r) { Join-Path $raiz ($r -replace '/', '\') }
function Esc($t) { [Net.WebUtility]::HtmlEncode($t) }
function EnLinea($t) { $t = Esc $t; $t = [regex]::Replace($t, '`([^`]+)`', '<code>$1</code>'); [regex]::Replace($t, '\*\*([^*]+)\*\*', '<b>$1</b>') }
function MdHtml($ruta) {
  $sb = New-Object Text.StringBuilder; $enTabla = $false; $fila = 0
  foreach ($l in [IO.File]::ReadAllLines((Ruta $ruta), $utf8)) {
    if ($l -match '^\|') {
      if ($l -match '^\|[\s\-:|]+\|\s*$') { continue }
      if (-not $enTabla) { [void]$sb.AppendLine('<table class="mat">'); $enTabla = $true; $fila = 0 }
      $celdas = $l.Trim().Trim('|') -split '\|'
      $tag = if ($fila -eq 0) { 'th' } else { 'td' }
      [void]$sb.AppendLine('<tr>' + (($celdas | ForEach-Object { "<$tag>$(EnLinea $_.Trim())</$tag>" }) -join '') + '</tr>'); $fila++
    } else {
      if ($enTabla) { [void]$sb.AppendLine('</table>'); $enTabla = $false }
      if ($l -match '^#\s') { continue }
      if ($l -match '^##+\s+(.*)') { [void]$sb.AppendLine("<h3 class='sinnum'>$(EnLinea $matches[1])</h3>"); continue }
      if ($l -match '^-\s+(.*)') { [void]$sb.AppendLine("<p class='li'>• $(EnLinea $matches[1])</p>") }
      elseif ($l.Trim()) { [void]$sb.AppendLine("<p>$(EnLinea $l)</p>") }
    }
  }
  if ($enTabla) { [void]$sb.AppendLine('</table>') }
  $sb.ToString()
}
$html = [IO.File]::ReadAllText((Ruta $Fuente), $utf8)
# Portada oficial única para todos los documentos: {{PORTADA:Título del documento|subtítulo opcional}}
$html = [regex]::Replace($html, '\{\{PORTADA:([^|}]+)(?:\|([^}]*))?\}\}', {
    param($m)
    $sub = if ($m.Groups[2].Success -and $m.Groups[2].Value) { "<p class='ps2'>$($m.Groups[2].Value)</p>" } else { '' }
    "<div class='portada'><p class='pu'>UNIVERSIDAD POPULAR DEL CESAR</p><p class='pc'>Asignatura: Base de Datos · Docente: Profesor Amílkar</p><p class='pt'>$($m.Groups[1].Value)</p>$sub<p class='ps'>ECOACCESS — PROYECTO FINAL DE BASE DE DATOS — PROFESOR AMÍLKAR<br/>EcoAccess — Sistema Inteligente de Gestión y Clasificación de Residuos</p><p class='pl'>Integrantes</p><p class='pn'>Jorge Alfonso Gutiérrez Thomas<br/>Heyder Hedrey Canchila de Ángel<br/>Saray Marcela Cadena Palomino</p><p class='pc'>Valledupar, noviembre de 2026</p></div>"
  })
Add-Type -AssemblyName System.Drawing
$html = [regex]::Replace($html, '\{\{IMG:([^|}]+)\|(\d+)\}\}', { param($m)
    $ruta = Ruta $m.Groups[1].Value; $ancho = [int]$m.Groups[2].Value; $alto = ''
    try { $im = [Drawing.Image]::FromFile($ruta); $alto = " height='$([int][Math]::Round($ancho * $im.Height / $im.Width))'"; $im.Dispose() } catch { }
    "<img src='file:///$($ruta -replace '\\','/')' width='$ancho'$alto/>" })
$html = [regex]::Replace($html, '\{\{FILELINES:([^:}]+):(\d+):(\d+)\}\}', { param($m) $ls = [IO.File]::ReadAllLines((Ruta $m.Groups[1].Value), $utf8); Esc (($ls[([int]$m.Groups[2].Value - 1)..([int]$m.Groups[3].Value - 1)]) -join "`n") })
$html = [regex]::Replace($html, '\{\{FILE:([^}]+)\}\}', { param($m) Esc ([IO.File]::ReadAllText((Ruta $m.Groups[1].Value), $utf8).Replace("`t", '    ').TrimEnd()) })
$html = [regex]::Replace($html, '\{\{HTML:([^}]+)\}\}', { param($m) [IO.File]::ReadAllText((Ruta $m.Groups[1].Value), $utf8) -replace '<h2>', '<h2 class="sinnum">' -replace '<h3>', '<h3 class="sinnum">' })
$html = [regex]::Replace($html, '\{\{MD:([^}]+)\}\}', { param($m) MdHtml $m.Groups[1].Value })
# Numeración de capítulos
$c1 = 0; $c2 = 0
$html = [regex]::Replace($html, '<h([12])(\s[^>]*)?>(.*?)</h\1>', {
    param($m)
    $n = $m.Groups[1].Value; $attr = $m.Groups[2].Value; $txt = $m.Groups[3].Value
    if ($attr -match 'sinnum' -or $txt -match '^Anexo') { if ($n -eq '1') { $script:c2 = -1 }; return $m.Value }
    if ($n -eq '1') { $script:c1++; $script:c2 = 0; return "<h1$attr>$($script:c1). $txt</h1>" }
    if ($script:c2 -lt 0) { return $m.Value }
    $script:c2++; "<h2$attr>$($script:c1).$($script:c2) $txt</h2>"
  })
# Rutas largas en <code>: punto de corte invisible tras / y \ para que el texto justificado no se estire
$html = [regex]::Replace($html, '<code>([^<]{30,})</code>', { param($m) '<code>' + ($m.Groups[1].Value -replace '([/\\])', ('$1' + [char]0x200B)) + '</code>' })
# [[CAP:Título]] -> número de capítulo/sección calculado (no escrito a mano)
$caps = @{}
foreach ($m in [regex]::Matches($html, '<h[12][^>]*>(\d+(?:\.\d+)?)\.?\s+(.*?)</h[12]>')) { $t = ($m.Groups[2].Value -replace '<[^>]+>', '').Trim(); if (-not $caps.ContainsKey($t)) { $caps[$t] = $m.Groups[1].Value } }
$html = [regex]::Replace($html, '\[\[CAP:([^\]]+)\]\]', { param($m) $k = $m.Groups[1].Value; if ($caps.ContainsKey($k)) { $caps[$k] } else { throw "[[CAP:$k]] no corresponde a ningún título" } })
$css = @'
<style>
@page { size:612pt 792pt; margin:72pt 64pt 64pt 64pt; }
body { font-family:Calibri, Arial, sans-serif; font-size:11pt; line-height:1.35; color:#1d1d1d; }
h1 { font-size:18pt; color:#1E6B3A; page-break-before:always; border-bottom:1.5pt solid #1E6B3A; padding-bottom:3pt; }
h2 { font-size:14pt; color:#1E6B3A; margin-top:14pt; }
h3 { font-size:12pt; color:#2E4D3A; margin-top:10pt; }
p { margin:4pt 0 6pt 0; text-align:justify; }
code { font-family:Consolas, monospace; font-size:9.5pt; color:#7A2E00; }
pre { font-family:Consolas, monospace; font-size:7.5pt; background:#F4F6F4; border:0.75pt solid #C8D2C8; padding:5pt; white-space:pre-wrap; }
pre.out { background:#FAFAF2; }
table.t, table.dic, table.mat { border-collapse:collapse; width:100%; margin:6pt 0 10pt 0; }
table.t th, table.t td, table.dic th, table.dic td, table.mat th, table.mat td { border:0.75pt solid #9DB09D; padding:3pt 4pt; vertical-align:top; font-size:9.5pt; }
table.dic th, table.dic td { font-size:7.5pt; }
table.mat th, table.mat td { font-size:8pt; }
th { background:#DCEBDD; }
p.fig { text-align:center; } p.cap { text-align:center; font-size:9pt; font-style:italic; color:#444; }
.portada { text-align:center; }
.portada p { text-align:center; }
p.pu { font-size:20pt; font-weight:bold; color:#1E6B3A; margin-top:40pt; }
p.pt { font-size:26pt; font-weight:bold; color:#1E6B3A; margin-top:60pt; }
p.ps { font-size:12pt; color:#333; margin:10pt 30pt 50pt 30pt; }
p.ps2 { font-size:13pt; color:#1E6B3A; margin:6pt 40pt 10pt 40pt; }
p.pl { font-size:11pt; color:#666; margin-top:18pt; text-transform:uppercase; }
p.pn { font-size:14pt; font-weight:bold; }
p.pc { font-size:12pt; margin-top:20pt; }
div.ej { border-left:3pt solid #1E6B3A; padding-left:6pt; margin:8pt 0; }
div.ej p { text-align:left; }
p.ejt { font-size:11pt; } span.est { color:#555; font-size:9pt; }
p.res { font-size:9pt; color:#555; margin-bottom:0; }
p.nota { background:#FFF6DA; border:0.75pt solid #E0C060; padding:5pt; }
p.li { margin:2pt 0 2pt 12pt; }
table.ficha { border-collapse:collapse; width:100%; margin:4pt 0 10pt 0; page-break-inside:avoid; }
table.ficha th { background:#1E6B3A; color:#FFFFFF; text-align:left; font-size:10pt; padding:3pt 5pt; border:0.75pt solid #1E6B3A; }
table.ficha td { border:0.75pt solid #C8D2C8; padding:2pt 5pt; font-size:8.5pt; vertical-align:top; }
table.ficha td.fk { width:28%; background:#F4F8F4; font-weight:bold; color:#2E4D3A; }
table.ficha td.sig { font-size:10pt; text-align:justify; line-height:1.3; }
table.fichaa { border-collapse:collapse; width:100%; margin:4pt 0 10pt 0; }
table.fichaa th { background:#1E6B3A; color:#FFFFFF; text-align:left; font-size:10pt; padding:3pt 5pt; border:0.75pt solid #1E6B3A; }
table.fichaa td { border:0.75pt solid #C8D2C8; padding:2pt 5pt; font-size:9pt; vertical-align:top; }
table.fichaa td.fk { width:22%; background:#F4F8F4; font-weight:bold; color:#2E4D3A; }
table.fichaa td.sig { font-size:10pt; text-align:justify; line-height:1.3; }
div.tec { background:#F4F8F4; border:0.75pt solid #C8D2C8; padding:4pt 6pt; margin:6pt 0 10pt 0; }
div.tec p { font-size:9.5pt; text-align:left; margin:2pt 0; }
img.captura { border:0.75pt solid #9DB09D; }
p.tind { font-size:18pt; color:#1E6B3A; font-weight:bold; page-break-before:always; border-bottom:1.5pt solid #1E6B3A; }
.portada + p.tind { page-break-before:always; }
</style>
'@
$html = $html -replace '<br class="salto"/>', "<br clear='all' style='page-break-before:always'/>"
$doc = "<html><head><meta charset='utf-8'/><title>$(Esc $Titulo)</title>$css</head><body>$html</body></html>"
$tmp = Join-Path $env:TEMP 'ecoacc_doc'; New-Item -ItemType Directory -Force $tmp | Out-Null
$base = [IO.Path]::GetFileName($Salida)
$htmlTmp = Join-Path $tmp "$base.html"
[IO.File]::WriteAllText($htmlTmp, $doc, (New-Object Text.UTF8Encoding $true))

$w = New-Object -ComObject Word.Application
$w.Visible = $false; $w.DisplayAlerts = 0
try {
  $d = $w.Documents.Open($htmlTmp, $false, $false, $false)
  $d.ActiveWindow.View.Type = 3
  # Márgenes fijos (Word no siempre respeta los de @page al abrir HTML)
  foreach ($sec in @($d.Sections)) { $ps = $sec.PageSetup; $ps.PageWidth = 612; $ps.PageHeight = 792; $ps.TopMargin = 72; $ps.BottomMargin = 64; $ps.LeftMargin = 64; $ps.RightMargin = 64 }
  # Incrustar imágenes y ajustarlas al ancho útil
  $util = $d.PageSetup.PageWidth - $d.PageSetup.LeftMargin - $d.PageSetup.RightMargin - 2
  foreach ($s in @($d.InlineShapes)) {
    try { if ($s.LinkFormat) { $s.LinkFormat.SavePictureWithDocument = $true; $s.LinkFormat.BreakLink() } } catch {}
    $alto = ($d.PageSetup.PageHeight - $d.PageSetup.TopMargin - $d.PageSetup.BottomMargin) * 0.78
    $r = [Math]::Min(1, [Math]::Min($util / $s.Width, $alto / $s.Height)); if ($r -lt 1) { $s.LockAspectRatio = -1; $s.Width = $s.Width * $r; $s.Height = $s.Height * $r }
    $s.Range.ParagraphFormat.KeepWithNext = $true
  }
  # Índice automático
  $f = $d.Content.Find
  if ($f.Execute('[[TOC]]')) {
    $rg = $f.Parent; $rg.Text = ''
    [void]$d.TablesOfContents.Add($rg, $true, 1, 2)
  }
  # Secciones horizontales entre [[H_INI]] y [[H_FIN]] (tablas anchas)
  foreach ($marca in '[[H_INI]]', '[[H_FIN]]') {
    while ($true) {
      $rg = $d.Content; if (-not $rg.Find.Execute($marca)) { break }
      $rg.Text = ''; $rg.InsertBreak(3)
      $ps = $rg.Sections(1).PageSetup
      if ($marca -eq '[[H_INI]]') { $ps.Orientation = 1; $ps.TopMargin = 48; $ps.BottomMargin = 48; $ps.LeftMargin = 40; $ps.RightMargin = 40 }
      else { $ps.Orientation = 0; $ps.TopMargin = 72; $ps.BottomMargin = 64; $ps.LeftMargin = 64; $ps.RightMargin = 64 }
    }
  }
  # Tablas más anchas que el área de texto de su sección: se ajustan a la ventana (wdAutoFitWindow)
  foreach ($tb in @($d.Tables)) {
    try { $tb.AllowAutoFit = $true; $tb.PreferredWidthType = 2; $tb.PreferredWidth = 100; $tb.AutoFitBehavior(2) } catch {}
  }
  # Encabezado con la identificación del proyecto (la portada no lo lleva)
  foreach ($sec in @($d.Sections)) { $sec.PageSetup.DifferentFirstPageHeaderFooter = $(if ($sec.Index -eq 1) { -1 } else { 0 }); $h = $sec.Headers(1).Range; $h.Text = 'EcoAccess — Proyecto Final de Base de Datos — Profesor Amílkar · Universidad Popular del Cesar'; $h.Font.Size = 8; $h.Font.Color = 8421504; $h.ParagraphFormat.Alignment = 1 }
  # Número de página en el pie
  foreach ($sec in @($d.Sections)) { [void]$sec.Footers(1).PageNumbers.Add(2, ($sec.Index -ne 1)) }
  # La portada (primera página) va sin encabezado ni número
  $s1 = $d.Sections(1); $s1.PageSetup.DifferentFirstPageHeaderFooter = -1
  $s1.Headers(2).Range.Text = ''; $s1.Footers(2).Range.Text = ''
  foreach ($sec in @($d.Sections)) { $sec.Headers(1).Range.Font.Name = 'Calibri' }
  # [[PAG:Título]] -> campo PAGEREF a un marcador en ese título (página real, la calcula Word)
  $titulos = @(foreach ($p in $d.Paragraphs) { if ($p.OutlineLevel -le 3) { $p } })
  $nb = 0
  while ($true) {
    $rg = $d.Content; $fd = $rg.Find; $fd.MatchWildcards = $true
    if (-not $fd.Execute('\[\[PAG:*\]\]')) { break }
    $clave = $rg.Text.Substring(6, $rg.Text.Length - 8)
    $par = $titulos | Where-Object { (($_.Range.Text -replace "[\r\a]", '').Trim() -replace '^\d+(\.\d+)*\.?\s+', '') -eq $clave } | Select-Object -First 1
    if (-not $par) { throw "[[PAG:$clave]] no corresponde a ningún título" }
    $nb++; $marca = "pag_$nb"
    $tr = $par.Range.Duplicate; $tr.MoveEnd(1, -1) | Out-Null
    [void]$d.Bookmarks.Add($marca, $tr)
    $rg.Text = ''
    [void]$d.Fields.Add($rg, -1, "PAGEREF $marca \h", $false)
  }
  $d.Content.ParagraphFormat.Widowcontrol = $true
  foreach ($toc in @($d.TablesOfContents)) { $toc.Update() }
  $d.Fields.Update() | Out-Null
  $docx = Join-Path $tmp "$base.docx"; $pdf = Join-Path $tmp "$base.pdf"
  $d.SaveAs2($docx, 16)
  $d.ExportAsFixedFormat($pdf, 17, $false, 0, 0, 1, 1, 0, $true, $true, 1)
  $paginas = $d.ComputeStatistics(2)
  $secciones = $d.Sections.Count
  $horizontales = @($d.Sections | Where-Object { $_.PageSetup.Orientation -eq 1 }).Count
  $imagenes = $d.InlineShapes.Count
  $d.Close(0)
} finally { $w.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($w) }
Copy-Item $docx "$(Ruta $Salida).docx" -Force
Copy-Item $pdf "$(Ruta $Salida).pdf" -Force
"Listo: $base.docx y .pdf · páginas: $paginas · secciones: $secciones (horizontales: $horizontales) · imágenes: $imagenes"
