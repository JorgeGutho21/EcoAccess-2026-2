<#
.SYNOPSIS
  Regenera diagramas, Word y PDF desde el Markdown de la guía.
.DESCRIPTION
  Requiere Windows con Microsoft Word de escritorio y Python con python-docx
  y Pillow. Word actualiza el índice y exporta el PDF con marcadores.
#>
[CmdletBinding()]
param([string] $Python = 'python')

$ErrorActionPreference = 'Stop'
$carpeta = Split-Path -Parent $PSScriptRoot
$docx = Join-Path $carpeta 'EcoAccess_V1_GUIA_MAESTRA.docx'
$pdf = Join-Path $carpeta 'EcoAccess_V1_GUIA_MAESTRA.pdf'

& $Python (Join-Path $PSScriptRoot 'diagramas.py')
if ($LASTEXITCODE) { throw 'Falló la generación de diagramas.' }
& $Python (Join-Path $PSScriptRoot 'generar_docx.py')
if ($LASTEXITCODE) { throw 'Falló la generación de Word.' }

$word = New-Object -ComObject Word.Application
$documento = $null
try {
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $documento = $word.Documents.Open($docx, $false, $false)
    if ($documento.TablesOfContents.Count -ne 1) { throw 'Se esperaba un índice automático.' }
    $null = $documento.TablesOfContents.Item(1).Update()
    $documento.Repaginate()
    $null = $documento.TablesOfContents.Item(1).UpdatePageNumbers()
    $null = $documento.Fields.Update()
    $documento.Repaginate()
    $documento.Save()
    # Formato 17 = PDF. CreateBookmarks 1 = encabezados de Word.
    $documento.ExportAsFixedFormat($pdf, 17, $false, 0, 0, 1, 1, 0, $true, $true, 1)
    $paginas = $documento.ComputeStatistics(2)
    Write-Output "Documentos regenerados. Páginas: $paginas."
}
finally {
    if ($null -ne $documento) { $documento.Close($false) }
    $word.Quit()
}
