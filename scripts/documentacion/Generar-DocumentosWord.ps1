<#
.SYNOPSIS
    Convierte los HTML generados de la documentación académica en DOCX y PDF con Microsoft Word.
.DESCRIPTION
    1. Genera el inventario del código con Roslyn (tests/herramientas/EcoAccess.P3.Inventario).
    2. Convierte las fuentes Markdown de docs/entrega/fuentes_md en HTML (directivas {{...}} con datos reales).
    3. Con Word: índice automático, encabezado, número de página, tablas ajustadas al ancho y encabezados repetidos.
    4. Guarda DOCX y exporta PDF en docs/entrega/academica y escribe un resumen con páginas por documento.
    Necesita Word instalado. No toca Oracle ni el repositorio de Git.
.EXAMPLE
    powershell -File scripts\documentacion\Generar-DocumentosWord.ps1
#>
param([string] $Solo = "*", [switch] $SinRegenerar, [switch] $Visible)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$herramienta = Join-Path $raiz 'tests\herramientas\EcoAccess.P3.Inventario'
$html = Join-Path $raiz 'docs\entrega\academica\html'
$destino = Join-Path $raiz 'docs\entrega\academica'
New-Item -ItemType Directory -Force $html, $destino | Out-Null

Push-Location $raiz
try { $commit = (git rev-parse --short HEAD).Trim() } finally { Pop-Location }
if (-not $SinRegenerar) {
& dotnet run --project $herramienta -c Release -- $raiz (Join-Path $raiz 'docs\academico\inventario') $commit
if ($LASTEXITCODE -ne 0) { throw 'Falló el inventario.' }
& dotnet run --project $herramienta -c Release --no-build -- docs $raiz (Join-Path $raiz 'docs\entrega\fuentes_md') $html
if ($LASTEXITCODE -ne 0) { throw 'Falló la generación de HTML.' }
}

$ErrorActionPreference = 'Continue'   # las rutas se pasan a Word como [string]: un objeto envuelto de PowerShell dejaba SaveAs2 y ExportAsFixedFormat bloqueados
$bitacora = Join-Path $destino "generacion.log"
function Paso([string] $t) { Add-Content $bitacora ("{0:HH:mm:ss} {1}" -f (Get-Date), $t) }
$word = New-Object -ComObject Word.Application
$word.Visible = [bool]$Visible
$word.DisplayAlerts = 0
# Si el propietario tiene Word abierto, Normal.dotm está en uso: marcarla como guardada evita un diálogo oculto que bloquea SaveAs2.
$word.NormalTemplate.Saved = $true
$resumen = @()
try {
    foreach ($archivo in Get-ChildItem $html -Filter *.html | Where-Object { $_.BaseName -like $Solo } | Sort-Object Name) {
        $nombre = $archivo.BaseName
        $titulo = [Net.WebUtility]::HtmlDecode([regex]::Match([IO.File]::ReadAllText($archivo.FullName), "<title>(.*?)</title>").Groups[1].Value)
        Paso "${nombre}: abrir"
        $docx = Join-Path $destino "$nombre.docx"
        $pdf = Join-Path $destino "$nombre.pdf"
        $h0 = $word.Documents.Open([string]$archivo.FullName, $false, $true, $false)
        Paso "${nombre}: convertir a docx"
        $word.NormalTemplate.Saved = $true
        $h0.SaveAs2([string]$docx, 16, $false, '', $false)
        $h0.Close($false)
        $d = $word.Documents.Open([string]$docx, $false, $false, $false)
        $d.ActiveWindow.View.Type = 3   # diseño de impresión
        foreach ($s in $d.Sections) {
            $s.PageSetup.TopMargin = 60; $s.PageSetup.BottomMargin = 60; $s.PageSetup.LeftMargin = 62; $s.PageSetup.RightMargin = 62
        }
        $util = $d.Sections.Item(1).PageSetup.PageWidth - 124

        Paso "${nombre}: índice"
        # Índice automático donde la portada dejó [[INDICE]].
        $r = $d.Content
        if ($r.Find.Execute('[[INDICE]]')) {
            $r.Text = ''
            $r.InsertParagraphBefore()
            $r.Text = 'Índice'
            $r.Font.Size = 16; $r.Font.Bold = $true; $r.Font.Color = 3828511
            $r.InsertParagraphAfter()
            $p = $r.Paragraphs.Item(1).Range
            $p.Collapse(0)
            $d.TablesOfContents.Add($p, $true, 1, 2) | Out-Null
        }

        Paso "${nombre}: imágenes ($($d.InlineShapes.Count))"
        # Las imágenes del HTML llegan vinculadas: se incrustan en el DOCX y se ajustan al ancho útil.
        foreach ($img in @($d.InlineShapes)) {
            try {
                if ($img.LinkFormat) { $img.LinkFormat.SavePictureWithDocument = $true; $img.LinkFormat.BreakLink() }
            } catch { }
            try {
                if ($img.Width -gt $util) { $alto = $img.Height * $util / $img.Width; $img.LockAspectRatio = 0; $img.Width = $util; $img.Height = $alto }
            } catch { }
        }

        Paso "${nombre}: tablas ($($d.Tables.Count))"
        # Tablas: ancho de la página, encabezado repetido, filas sin partir.
        foreach ($t in $d.Tables) {
            try {
                $t.AllowAutoFit = $true
                $t.PreferredWidthType = 3; $t.PreferredWidth = $util
                $t.AutoFitBehavior(1); $t.AutoFitBehavior(2)
                $t.Rows.Item(1).HeadingFormat = -1
                $t.Rows.AllowBreakAcrossPages = -1
            } catch { }
        }

        Paso "${nombre}: encabezado y pie"
        # Encabezado con el título y pie con «Página X de Y».
        foreach ($s in $d.Sections) {
            $h = $s.Headers.Item(1).Range
            $h.Text = "EcoAccess P3 · $titulo"; $h.Font.Size = 8; $h.Font.Color = 8421504; $h.ParagraphFormat.Alignment = 2
            $f = $s.Footers.Item(1).Range
            $f.Text = 'Página '; $f.Font.Size = 8; $f.ParagraphFormat.Alignment = 1
            $f.Collapse(0); $f.Fields.Add($f, 33) | Out-Null   # PAGE
            $f = $s.Footers.Item(1).Range; $f.Collapse(0); $f.InsertAfter(' de ')
            $f = $s.Footers.Item(1).Range; $f.Collapse(0); $f.Fields.Add($f, 26) | Out-Null   # NUMPAGES
            $s.Headers.Item(1).Range.Font.Name = 'Calibri'; $s.Footers.Item(1).Range.Font.Name = 'Calibri'
        }
        $d.TablesOfContents | % { $_.Update() }
        Paso "${nombre}: guardar"
        $d.TablesOfContents | % { $_.Update() }
        $word.NormalTemplate.Saved = $true
        $d.Save()
        Paso "${nombre}: pdf"
        $d.ExportAsFixedFormat([string]$pdf, 17, $false, 0, 0, 0, 0, 0, $true, $true, 1)
        $paginas = $d.ComputeStatistics(2)
        $resumen += [pscustomobject]@{ Documento = $nombre; Paginas = $paginas; Tablas = $d.Tables.Count; Palabras = $d.ComputeStatistics(0) }
        $d.Close($false)
        Write-Host ("{0,-60} {1,4} páginas" -f $nombre, $paginas)
    }
} finally { $word.NormalTemplate.Saved = $true; $word.Quit() }
$resumen | ConvertTo-Json | Set-Content (Join-Path $destino 'resumen_generacion.json') -Encoding utf8






