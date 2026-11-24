<#
.SYNOPSIS
    Chequeo de estetica EcoAccess para las areas Operario y Supervisor.

.DESCRIPTION
    Revisa los archivos de las pantallas de gestion (Operario y Supervisor) en
    src/EcoAccess.P3.Presentacion y reporta lo que se aparta del sistema de diseno
    definido en las skills eco-estilo-base, eco-ui-operario y eco-ui-supervisor.

    ERROR (bloquea):
      E1  Imagen decorativa en pantallas de gestion (mascota, ilustraciones, fotos).
      E2  Fuente menor de 9 pt.
      E3  Aspecto Windows clasico (FlatStyle Standard/System/Popup, BorderStyle.Fixed3D).
      E4  Logo fuera de la barra lateral de FormPrincipal.
      E5  Modulo del Supervisor sin la estructura Eco (ModuloEco.Iniciar o VistaGestionCatalogo).
    ADVERTENCIA:
      A1  Color literal fuera de TemaEcoAccess (salvo White, Transparent, Empty).
      A2  Fuente literal (new Font) fuera de TemaEcoAccess.
      A3  Formulario abierto con ShowDialog sin TemaEcoAccess.Aplicar.
      A4  MessageBox con icono de exito/informacion para confirmar una accion (usar mensaje en linea).

    Excepcion puntual: agregar en la misma linea el comentario
        // eco-estetica: permitido <motivo>

.PARAMETER Ruta
    Carpeta del proyecto de presentacion. Por defecto se calcula desde la ubicacion del script.

.PARAMETER Archivos
    Lista de archivos a revisar (nombres dentro de Ruta). Por defecto, todos los de gestion.

.PARAMETER Estricto
    Si se indica, las advertencias tambien hacen fallar el chequeo.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .claude/skills/eco-chequeo-estetica/scripts/Verificar-EsteticaEco.ps1
#>
param(
    [string]$Ruta,
    [string[]]$Archivos,
    [switch]$Estricto
)

$ErrorActionPreference = 'Stop'

if (-not $Ruta) {
    $Ruta = Join-Path $PSScriptRoot '..\..\..\..\src\EcoAccess.P3.Presentacion'
}
$Ruta = (Resolve-Path $Ruta).Path

# Pantallas de gestion. Los modulos compartidos por Operario y Supervisor siguen la regla sin imagenes.
$Gestion = @(
    'FormPrincipal.cs', 'FormPrincipal.Designer.cs',
    'PanelResumenAdministrativo.cs', 'PanelGraficosResumen.cs', 'VistaGestionCatalogo.cs',
    'FormUsuarios.cs', 'FormUsuarios.Designer.cs', 'FormUsuarios.Diseno.cs',
    'FormMateriales.cs', 'FormMateriales.Designer.cs', 'FormMateriales.Diseno.cs',
    'FormContenedores.cs', 'FormContenedores.Designer.cs', 'FormContenedores.Diseno.cs',
    'FormPuntos.cs', 'FormPuntos.Designer.cs', 'FormPuntos.Diseno.cs',
    'FormEcopuntos.cs',
    'FormInteraccionesDepositos.cs', 'FormInteraccionesDepositos.Designer.cs', 'FormInteraccionesDepositos.Diseno.cs',
    'FormEstadisticas.cs', 'FormEstadisticas.Designer.cs', 'FormEstadisticas.Diseno.cs',
    'FormDashboardInstitucional.cs', 'FormDiagnosticoOracle.cs',
    'FormConsultaOperario.cs', 'FormRegistroManual.cs',
    'FormCambioContrasena.cs', 'FormCambioContrasena.Designer.cs', 'FormCambioContrasena.Diseno.cs',
    'FormQrCaneca.cs', 'FormQrCaneca.Designer.cs', 'FormQrCaneca.Diseno.cs',
    'FormOpiniones.cs'
)
if (-not $Archivos) { $Archivos = $Gestion }
# Con -File, "a.cs,b.cs" llega como un solo texto: se separa por comas.
$Archivos = @($Archivos | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

$Permitido = 'eco-estetica:\s*permitido'

$Reglas = @(
    @{ Id = 'E1'; Nivel = 'ERROR'; Texto = 'Imagen decorativa en pantalla de gestion: usar icono FontAwesome';
       Patron = 'RecursosEco\.(Eco|Imagen|Residuo)\b|\bImagenRedondeada\b|\bImagenAjustada\b|IconosMaterial\.Ilustracion|BackgroundImage\s*=|Image\.FromFile|Properties\.Resources\.' },
    @{ Id = 'E2'; Nivel = 'ERROR'; Texto = 'Fuente menor de 9 pt';
       Patron = 'new\s+(System\.Drawing\.)?Font\(\s*"[^"]+"\s*,\s*[0-8](\.\d+)?f?\s*[,)]' },
    @{ Id = 'E3'; Nivel = 'ERROR'; Texto = 'Aspecto Windows clasico: usar FlatStyle.Flat y cajas sin borde 3D';
       Patron = 'FlatStyle\.(Standard|System|Popup)\b|BorderStyle\.Fixed3D' },
    @{ Id = 'A1'; Nivel = 'ADVERTENCIA'; Texto = 'Color literal: usar un token de TemaEcoAccess';
       Patron = 'Color\.FromArgb\(|Color\.(?!White\b|Transparent\b|Empty\b|FromArgb\b)[A-Z][A-Za-z]+\b' },
    @{ Id = 'A2'; Nivel = 'ADVERTENCIA'; Texto = 'Fuente literal: usar TemaEcoAccess.Titulo/TituloSeccion/Subtitulo/Valor/Entrada/Cuerpo/Pequena';
       Patron = 'new\s+(System\.Drawing\.)?Font\(' },
    @{ Id = 'A4'; Nivel = 'ADVERTENCIA'; Texto = 'Confirmacion con MessageBox: preferir mensaje en linea (CampoEco.MostrarValido o PastillaEstado)';
       Patron = 'MessageBox\.Show\(.*(guardad|registrad|actualizad|creada|creado|verificad)' }
)

$Hallazgos = New-Object System.Collections.Generic.List[object]

function Agregar-Hallazgo($archivo, $linea, $regla, $nivel, $texto, $codigo) {
    $script:Hallazgos.Add([pscustomobject]@{
        Nivel = $nivel; Regla = $regla; Archivo = $archivo; Linea = $linea
        Detalle = $texto; Codigo = $codigo.Trim()
    })
}

$Revisados = 0
foreach ($nombre in $Archivos) {
    $archivoRuta = Join-Path $Ruta $nombre
    if (-not (Test-Path $archivoRuta)) { Write-Host "  (no existe, se omite) $nombre" -ForegroundColor DarkGray; continue }
    $Revisados++
    $lineas = Get-Content -LiteralPath $archivoRuta -Encoding UTF8
    $contenido = $lineas -join "`n"

    for ($i = 0; $i -lt $lineas.Count; $i++) {
        $l = $lineas[$i]
        $sinComentario = ($l -replace '//.*$', '')
        if ($l -match $Permitido) { continue }
        if ($sinComentario.Trim().Length -eq 0) { continue }

        foreach ($r in $Reglas) {
            if ($sinComentario -match $r.Patron) {
                Agregar-Hallazgo $nombre ($i + 1) $r.Id $r.Nivel $r.Texto $l
            }
        }

        # E4: el logo solo vive en la barra lateral del FormPrincipal.
        if ($sinComentario -match '\bLogoEcoAccess\b' -and $nombre -notlike 'FormPrincipal*') {
            Agregar-Hallazgo $nombre ($i + 1) 'E4' 'ERROR' 'Logo fuera de la barra lateral de FormPrincipal' $l
        }
    }

    # A3: formularios abiertos como dialogo deben recibir el tema.
    $dialogos = [regex]::Matches($contenido, 'var\s+(\w+)\s*=\s*new\s+(Form\w+)\s*\(')
    foreach ($d in $dialogos) {
        $var = $d.Groups[1].Value; $tipo = $d.Groups[2].Value
        if ($contenido -notmatch ([regex]::Escape($var) + '\.ShowDialog\(')) { continue }
        $temaLocal = $contenido -match ('TemaEcoAccess\.Aplicar\(\s*' + [regex]::Escape($var) + '\s*\)')
        $archivoTipo = Join-Path $Ruta ($tipo + '.cs')
        $temaPropio = (Test-Path $archivoTipo) -and ((Get-Content -LiteralPath $archivoTipo -Raw -Encoding UTF8) -match 'TemaEcoAccess\.Aplicar\(\s*this\s*\)|BackColor\s*=\s*TemaEcoAccess\.Fondo')
        if (-not ($temaLocal -or $temaPropio)) {
            $num = ($contenido.Substring(0, $d.Index) -split "`n").Count
            if ($lineas[$num - 1] -match $Permitido) { continue }
            Agregar-Hallazgo $nombre $num 'A3' 'ADVERTENCIA' "Dialogo $tipo sin TemaEcoAccess.Aplicar" $lineas[$num - 1]
        }
    }
}

# E5: cada modulo del Supervisor usa la estructura Eco del ejemplo (encabezado con icono, tarjetas con titulo y campos
# con etiqueta arriba e icono): ModuloEco.Iniciar o VistaGestionCatalogo en alguno de sus archivos.
$Estructura = 'ModuloEco\.Iniciar\(|new\s+VistaGestionCatalogo\(|ModuloEco\.Encabezado\('
$ModulosSupervisor = @(
    'FormUsuarios', 'FormMateriales', 'FormContenedores', 'FormPuntos', 'FormEcopuntos', 'FormInteraccionesDepositos',
    'FormEstadisticas', 'FormOpiniones', 'FormDashboardInstitucional', 'FormDiagnosticoOracle', 'FormCambioContrasena', 'FormQrCaneca'
)
if (-not $PSBoundParameters.ContainsKey('Archivos')) {
    foreach ($modulo in $ModulosSupervisor) {
        $archivosModulo = @(Get-ChildItem -LiteralPath $Ruta -Filter "$modulo*.cs" | Where-Object { $_.Name -notlike '*.Designer.cs' })
        if ($archivosModulo.Count -eq 0) { continue }
        $usa = $false
        foreach ($a in $archivosModulo) { if ((Get-Content -LiteralPath $a.FullName -Raw -Encoding UTF8) -match $Estructura) { $usa = $true } }
        if (-not $usa) {
            Agregar-Hallazgo "$modulo.cs" 1 'E5' 'ERROR' 'Modulo sin estructura Eco: usar ModuloEco.Iniciar (encabezado, tarjetas, campos con icono)' $modulo
        }
    }
}

$errores = @($Hallazgos | Where-Object Nivel -eq 'ERROR')
$advertencias = @($Hallazgos | Where-Object Nivel -eq 'ADVERTENCIA')

Write-Host ''
Write-Host "Chequeo de estetica EcoAccess (Operario / Supervisor)" -ForegroundColor Green
Write-Host "Carpeta: $Ruta"
Write-Host "Archivos revisados: $Revisados"
Write-Host ''

foreach ($grupo in ($Hallazgos | Sort-Object Nivel, Archivo, Linea | Group-Object Archivo)) {
    Write-Host $grupo.Name -ForegroundColor Cyan
    foreach ($h in $grupo.Group) {
        $color = if ($h.Nivel -eq 'ERROR') { 'Red' } else { 'Yellow' }
        $codigo = if ($h.Codigo.Length -gt 110) { $h.Codigo.Substring(0, 110) + '...' } else { $h.Codigo }
        Write-Host ("  [{0}] {1,-4} linea {2,-4} {3}" -f $h.Regla, $h.Nivel.Substring(0, 3), $h.Linea, $h.Detalle) -ForegroundColor $color
        Write-Host ("        {0}" -f $codigo) -ForegroundColor DarkGray
    }
}

# Resumen por regla
Write-Host ''
Write-Host 'Resumen por regla:'
$Hallazgos | Group-Object Regla | Sort-Object Name | ForEach-Object { Write-Host ("  {0}: {1}" -f $_.Name, $_.Count) }
Write-Host ''
Write-Host ("Errores: {0}   Advertencias: {1}" -f $errores.Count, $advertencias.Count)

if ($Revisados -eq 0) {
    Write-Host 'RESULTADO: no se reviso ningun archivo (revisa -Ruta / -Archivos).' -ForegroundColor Red
    exit 2
}
$falla = $errores.Count -gt 0 -or ($Estricto -and $advertencias.Count -gt 0)
if ($falla) {
    Write-Host 'RESULTADO: NO CUMPLE la estetica EcoAccess.' -ForegroundColor Red
    exit 1
}
Write-Host 'RESULTADO: CUMPLE la estetica EcoAccess.' -ForegroundColor Green
exit 0
