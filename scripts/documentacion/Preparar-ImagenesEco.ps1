<#
.SYNOPSIS
    Copia y optimiza las ilustraciones oficiales de ECO y de los 15 residuos del prototipo a assets/imagenes.
.DESCRIPTION
    Las imágenes originales (1254 px, ~2 MB) están en una carpeta compartida del equipo, fuera del repositorio.
    Este script las reduce y las deja en el
    repositorio, donde la aplicación las incrusta: así funcionan en cualquier computador sin depender de OneDrive.
    Las ilustraciones de ECO se guardan en PNG con el fondo blanco del borde vuelto transparente; los residuos, en JPEG
    sobre fondo blanco (van sobre tarjetas blancas). No deforma: conserva la proporción.
.EXAMPLE
    pwsh -File scripts\documentacion\Preparar-ImagenesEco.ps1 -Origen "<carpeta con las imágenes originales>"
#>
[CmdletBinding()]
param([Parameter(Mandatory)][string] $Origen)
$ErrorActionPreference = 'Stop'
# System.Drawing completo es de .NET Framework: desde pwsh 7 el script se vuelve a ejecutar con Windows PowerShell 5.1.
if ($PSVersionTable.PSEdition -eq 'Core') { & (Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe') -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -Origen $Origen; exit $LASTEXITCODE }
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class ImagenEco
{
    public static Bitmap Reducir(Image origen, int ladoMayor)
    {
        double k = Math.Min(1.0, (double)ladoMayor / Math.Max(origen.Width, origen.Height));
        int w = Math.Max(1, (int)Math.Round(origen.Width * k)), h = Math.Max(1, (int)Math.Round(origen.Height * k));
        var b = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(b))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            using (var atributos = new ImageAttributes()) { atributos.SetWrapMode(WrapMode.TileFlipXY); g.DrawImage(origen, new Rectangle(0, 0, w, h), 0, 0, origen.Width, origen.Height, GraphicsUnit.Pixel, atributos); }
        }
        return b;
    }

    // Fondo blanco conectado al borde → transparente, con borde suave según lo blanco que sea el píxel.
    public static void FondoTransparente(Bitmap b, int umbral)
    {
        var area = new Rectangle(0, 0, b.Width, b.Height);
        BitmapData d = b.LockBits(area, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int n = b.Width * b.Height; var px = new int[n];
        Marshal.Copy(d.Scan0, px, 0, n);
        var visto = new bool[n]; var cola = new Queue<int>();
        Func<int, int> blancura = c => Math.Min((c >> 16) & 255, Math.Min((c >> 8) & 255, c & 255));
        for (int x = 0; x < b.Width; x++) { cola.Enqueue(x); cola.Enqueue((b.Height - 1) * b.Width + x); }
        for (int y = 0; y < b.Height; y++) { cola.Enqueue(y * b.Width); cola.Enqueue(y * b.Width + b.Width - 1); }
        while (cola.Count > 0)
        {
            int i = cola.Dequeue();
            if (i < 0 || i >= n || visto[i]) continue;
            visto[i] = true;
            int w = blancura(px[i]);
            if (w < umbral) continue;
            int alfa = Math.Max(0, Math.Min(255, (255 - w) * 255 / Math.Max(1, 255 - umbral)));
            px[i] = (alfa << 24) | (px[i] & 0x00FFFFFF);
            int x = i % b.Width;
            if (x > 0) cola.Enqueue(i - 1);
            if (x < b.Width - 1) cola.Enqueue(i + 1);
            cola.Enqueue(i - b.Width); cola.Enqueue(i + b.Width);
        }
        Marshal.Copy(px, 0, d.Scan0, n);
        b.UnlockBits(d);
    }
}
'@

$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$destino = Join-Path $repo 'assets\imagenes'
$jpeg = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object MimeType -eq 'image/jpeg'
$calidad = New-Object System.Drawing.Imaging.EncoderParameters 1
$calidad.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality), 88L

function Guardar([string] $Archivo, [string] $Salida, [int] $Lado, [switch] $Transparente)
{
    $ruta = Join-Path $Origen $Archivo
    if (-not (Test-Path -LiteralPath $ruta)) { throw "No encuentro $Archivo en $Origen" }
    $original = [System.Drawing.Image]::FromFile($ruta)
    try
    {
        $imagen = [ImagenEco]::Reducir($original, $Lado)
        $final = Join-Path $destino $Salida
        New-Item -ItemType Directory -Force (Split-Path -Parent $final) | Out-Null
        if ($Transparente) { [ImagenEco]::FondoTransparente($imagen, 232); $imagen.Save($final, [System.Drawing.Imaging.ImageFormat]::Png) }
        else
        {
            $fondo = New-Object System.Drawing.Bitmap $imagen.Width, $imagen.Height
            $g = [System.Drawing.Graphics]::FromImage($fondo); $g.Clear([System.Drawing.Color]::White); $g.DrawImage($imagen, 0, 0); $g.Dispose()
            $fondo.Save($final, $jpeg, $calidad); $fondo.Dispose()
        }
        $imagen.Dispose()
        '{0,-38} {1,5} KB' -f $Salida, [int]((Get-Item -LiteralPath $final).Length / 1KB)
    }
    finally { $original.Dispose() }
}

# ECO: misma versión del personaje (cabeza blanca, pantalla negra, hojas verdes, chaqueta blanca y verde).
Guardar 'ECO_OFICIAL.png' 'eco\eco_tablet.png' 520 -Transparente
Guardar 'Mascota ECO Saludando y Reciclando-2.png' 'eco\eco_saluda.png' 440 -Transparente
Guardar '¡Eco robot celebrando!-10.png' 'eco\eco_celebra.png' 440 -Transparente
Guardar 'Mascota EcoAccess con botella y pulgar arriba-8.png' 'eco\eco_botella.png' 440 -Transparente
Guardar 'ECO EN LA UPC.png' 'eco\eco_upc.jpg' 1200
# Franja de edificios del campus (sin ECO) para el fondo derecho de la portada del inicio.
Guardar 'FOTO UNIVERSIDAD MINIMALISTA.png' 'eco\campus.jpg' 900

# Residuos: la mano oficial de ECO sosteniendo cada material. La botella de agua no tiene ilustración de la mano:
# se usa ECO con una botella reciclable.
$residuos = [ordered]@{
    'Caneca verde — Orgánicos aprovechables.png'     = 'residuos\verde\banano.jpg'
    'Caneca verde — Orgánicos aprovechables (2).png' = 'residuos\verde\naranja.jpg'
    'Caneca verde — Orgánicos aprovechables (4).png' = 'residuos\verde\hojas.jpg'
    'Caneca verde — Orgánicos aprovechables (3).png' = 'residuos\verde\kiwi.jpg'
    'Caneca verde — Orgánicos aprovechables (5).png' = 'residuos\verde\mandarina.jpg'
    'Caneca verde — Orgánicos aprovechables (6).png' = 'residuos\verde\limon.jpg'
    'Robot ecológico con botella reciclable-7.png'   = 'residuos\blanca\botella-agua.jpg'
    'Caneca blanca — Aprovechables (2).png'          = 'residuos\blanca\botella-gaseosa.jpg'
    'Caneca blanca — Aprovechables.png'              = 'residuos\blanca\papel.jpg'
    'Caneca blanca — Aprovechables (5).png'          = 'residuos\blanca\caja-jugo.jpg'
    'Caneca blanca — Aprovechables (4).png'          = 'residuos\blanca\vasos.jpg'
    'Caneca negra — No aprovechables.png'            = 'residuos\negra\papel-higienico.jpg'
    'Caneca negra — No aprovechables (3).png'        = 'residuos\negra\servilletas.jpg'
    'Caneca negra — No aprovechables (2).png'        = 'residuos\negra\vaso-contaminado.jpg'
    'Caneca negra — No aprovechables (4).png'        = 'residuos\negra\plato-icopor.jpg'
}
foreach ($par in $residuos.GetEnumerator()) { Guardar $par.Key $par.Value 420 }
