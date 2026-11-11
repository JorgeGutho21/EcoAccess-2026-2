<#
.SYNOPSIS
    Genera la definición PBIR del informe Power BI de EcoAccess P3 (EcoAccessP3.Report) a partir de una especificación
    compacta: 5 páginas, tarjetas, gráficos, segmentadores de fecha y textos de advertencia.
.DESCRIPTION
    El modelo semántico (TMDL) está escrito a mano en EcoAccessP3.SemanticModel. Este script solo produce el informe.
    Ejecutar con PowerShell 7: pwsh -File reportes\powerbi\Generar-InformeP3.ps1
    Después: abrir EcoAccessP3.pbip en Power BI Desktop > Actualizar > Archivo > Guardar como EcoAccess.pbix.
#>
$ErrorActionPreference = 'Stop'
$raiz = Join-Path $PSScriptRoot 'EcoAccessP3.Report'
if (Test-Path $raiz) { Remove-Item -Recurse -Force (Join-Path $raiz 'definition') -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force (Join-Path $raiz 'definition\pages') | Out-Null
New-Item -ItemType Directory -Force (Join-Path $raiz 'StaticResources\RegisteredResources') | Out-Null

function Escribir([string] $ruta, $objeto) {
    $dir = Split-Path $ruta
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force $dir | Out-Null }
    $json = if ($objeto -is [string]) { $objeto } else { $objeto | ConvertTo-Json -Depth 40 }
    [IO.File]::WriteAllText($ruta, $json, [Text.UTF8Encoding]::new($false))
}

function Campo([string] $ref) {
    # "M:Nombre" = medida de la tabla Medidas; "Tabla.Columna" = columna.
    if ($ref.StartsWith('M:')) {
        $p = $ref.Substring(2)
        return @{ queryRef = "Medidas.$p"; nativeQueryRef = $p; field = @{ Measure = @{ Property = $p; Expression = @{ SourceRef = @{ Entity = 'Medidas' } } } } }
    }
    $t, $c = $ref.Split('.', 2)
    return @{ queryRef = $ref; nativeQueryRef = $c; field = @{ Column = @{ Property = $c; Expression = @{ SourceRef = @{ Entity = $t } } } } }
}

function Literal($v) { @{ expr = @{ Literal = @{ Value = $v } } } }

function Visual([hashtable] $v, [int] $z) {
    $estado = [ordered]@{}
    foreach ($rol in $v.roles.Keys) { $estado[$rol] = @{ projections = @($v.roles[$rol] | ForEach-Object { Campo $_ }) } }
    $cuerpo = [ordered]@{ visualType = $v.tipo; query = @{ queryState = $estado } }
    $contenedor = [ordered]@{
        title = @(@{ properties = @{ text = (Literal "'$($v.titulo)'"); show = (Literal 'true') } })
        background = @(@{ properties = @{ show = (Literal 'true'); color = @{ solid = @{ color = (Literal "'#FFFFFF'") } } } })
        border = @(@{ properties = @{ show = (Literal 'true'); color = @{ solid = @{ color = (Literal "'#C9DECF'") } }; radius = (Literal '8D') } })
    }
    $cuerpo.visualContainerObjects = $contenedor
    $cuerpo.drillFilterOtherVisuals = $true
    return [ordered]@{
        '$schema' = 'https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.0.0/schema.json'
        name = $v.nombre
        position = @{ x = $v.x; y = $v.y; z = $z; width = $v.w; height = $v.h }
        visual = $cuerpo
    }
}

function Texto([string] $nombre, [int] $x, [int] $y, [int] $w, [int] $h, [string] $texto, [string] $color = '#1F6B3A', [string] $tam = '14pt', [int] $z = 0) {
    [ordered]@{
        '$schema' = 'https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.0.0/schema.json'
        name = $nombre
        position = @{ x = $x; y = $y; z = $z; width = $w; height = $h }
        visual = [ordered]@{
            visualType = 'textbox'
            objects = @{ general = @(@{ properties = @{ paragraphs = @(@{ textRuns = @(@{ value = $texto; textStyle = @{ fontWeight = 'bold'; fontSize = $tam; color = $color } }) }) } }) }
            drillFilterOtherVisuals = $true
        }
    }
}

function V($nombre, $tipo, $titulo, $x, $y, $w, $h, $roles) { @{ nombre = $nombre; tipo = $tipo; titulo = $titulo; x = $x; y = $y; w = $w; h = $h; roles = $roles } }

$segmentadores = @(
    (V 'filtroFecha' 'slicer' 'Rango de fechas (Colombia)' 20 640 400 70 @{ Values = @('Calendario.Fecha') }),
    (V 'filtroMes' 'slicer' 'Mes' 440 640 200 70 @{ Values = @('Calendario.Mes') }),
    (V 'filtroSemana' 'slicer' 'Semana' 660 640 300 70 @{ Values = @('Calendario.Semana') })
)

$paginas = [ordered]@{
    resumen = @{
        titulo = 'Resumen general'
        textos = @(
            (Texto 'titulo' 20 8 1240 40 'EcoAccess P3 · Resumen general (datos reales de Oracle Cloud)'),
            (Texto 'nota' 20 600 1240 30 'Modo operativo: solo depósitos reales. Hoy no hay depósitos físicos registrados, por eso las cifras operativas son 0. Los datos sintéticos están en la página «Datos experimentales».' '#6B4E00' '10pt')
        )
        visuales = @(
            (V 'kpiCiudadanos' 'card' 'Ciudadanos participantes' 20 56 170 110 @{ Values = @('M:Ciudadanos participantes reales') }),
            (V 'kpiConsultas' 'card' 'Consultas visuales (Echo)' 200 56 170 110 @{ Values = @('M:Consultas visuales') }),
            (V 'kpiDeclarados' 'card' 'Depósitos registrados' 380 56 170 110 @{ Values = @('M:Depósitos reales') }),
            (V 'kpiVerificados' 'card' 'Depósitos verificados' 560 56 170 110 @{ Values = @('M:Depósitos verificados reales') }),
            (V 'kpiKgEstimados' 'card' 'Kg estimados' 740 56 170 110 @{ Values = @('M:Kg estimados reales') }),
            (V 'kpiKgMedidos' 'card' 'Kg medidos (báscula)' 920 56 170 110 @{ Values = @('M:Kg medidos reales') }),
            (V 'kpiEcopuntos' 'card' 'Ecopuntos netos' 1100 56 160 110 @{ Values = @('M:Ecopuntos netos') }),
            (V 'actividad' 'lineChart' 'Actividad por día' 20 180 820 400 @{ Category = @('Calendario.Fecha'); Y = @('M:Depósitos reales', 'M:Consultas visuales') }),
            (V 'porMes' 'clusteredColumnChart' 'Depósitos por mes' 860 180 400 400 @{ Category = @('Calendario.Mes'); Y = @('M:Depósitos reales') })
        ) + $segmentadores
    }
    canecas = @{
        titulo = 'Tres canecas'
        textos = @((Texto 'titulo' 20 8 1240 40 'Estación de tres canecas: blanca, verde y negra (datos reales)'))
        visuales = @(
            (V 'estacion' 'tableEx' 'Canecas de la estación piloto (Oracle, V024)' 20 56 760 170 @{ Values = @('Canecas.IdContenedor', 'Canecas.Caneca', 'Canecas.Color', 'Canecas.Ubicacion', 'Canecas.Dispositivo', 'Canecas.Estado') }),
            (V 'materialTop' 'card' 'Material más frecuente' 800 56 460 170 @{ Values = @('M:Material mas frecuente real') }),
            (V 'porColor' 'clusteredColumnChart' 'Depósitos por caneca (color)' 20 240 400 340 @{ Category = @('Depositos.Color'); Y = @('M:Depósitos reales') }),
            (V 'porCategoria' 'donutChart' 'Distribución por categoría' 440 240 400 340 @{ Category = @('Depositos.Categoria'); Y = @('M:Depósitos reales') }),
            (V 'materiales' 'clusteredBarChart' 'Comparación de materiales' 860 240 400 340 @{ Category = @('Materiales.Nombre'); Y = @('M:Depósitos reales') })
        ) + $segmentadores
    }
    actividad = @{
        titulo = 'Actividad y ecopuntos'
        textos = @(
            (Texto 'titulo' 20 8 1240 40 'Actividad y ecopuntos (datos reales)'),
            (Texto 'nota' 20 600 1240 30 'Ecopuntos confirmados = libro MOVIMIENTO_ECOPUNTOS (solo depósitos verificados). Los ecopuntos «provisionales» que el bot muestra antes de verificar no se guardan en Oracle, por eso no se grafican.' '#6B4E00' '10pt')
        )
        visuales = @(
            (V 'diaria' 'lineChart' 'Actividad diaria' 20 56 410 260 @{ Category = @('Calendario.Fecha'); Y = @('M:Depósitos reales') }),
            (V 'semanal' 'clusteredColumnChart' 'Actividad semanal' 440 56 410 260 @{ Category = @('Calendario.Semana'); Y = @('M:Depósitos reales') }),
            (V 'mensual' 'clusteredColumnChart' 'Actividad mensual' 860 56 400 260 @{ Category = @('Calendario.Mes'); Y = @('M:Depósitos reales') }),
            (V 'participantes' 'clusteredColumnChart' 'Ciudadanos participantes por mes' 20 326 410 260 @{ Category = @('Participacion.Mes'); Y = @('M:Ciudadanos participantes reales') }),
            (V 'cantidades' 'clusteredBarChart' 'Unidades declaradas por material' 440 326 410 260 @{ Category = @('Materiales.Nombre'); Y = @('M:Unidades declaradas reales') }),
            (V 'acreditados' 'card' 'Ecopuntos confirmados' 860 326 200 125 @{ Values = @('M:Ecopuntos acreditados') }),
            (V 'anulados' 'card' 'Correcciones y anulaciones' 1070 326 190 125 @{ Values = @('M:Ecopuntos anulados') }),
            (V 'netos' 'card' 'Ecopuntos netos' 860 461 400 125 @{ Values = @('M:Ecopuntos netos') })
        ) + $segmentadores
    }
    experimental = @{
        titulo = 'Datos experimentales'
        textos = @(
            (Texto 'titulo' 20 8 1240 40 'DATOS SINTÉTICOS · Lote SIM-2026-11-CIERRE · No son reciclaje ni depósitos físicos' '#B00020' '16pt')
        )
        visuales = @(
            (V 'kpiFicticios' 'card' 'Ciudadanos ficticios' 20 56 230 110 @{ Values = @('M:Ciudadanos ficticios del lote') }),
            (V 'kpiSinteticos' 'card' 'Depósitos sintéticos' 260 56 230 110 @{ Values = @('M:Depósitos experimentales') }),
            (V 'kpiVerifExp' 'card' 'Verificados (sintéticos)' 500 56 230 110 @{ Values = @('M:Verificados experimentales') }),
            (V 'kpiKgEstExp' 'card' 'Kg estimados (sintéticos)' 740 56 260 110 @{ Values = @('M:Kg estimados experimentales') }),
            (V 'kpiKgMedExp' 'card' 'Kg medidos (sin báscula)' 1010 56 250 110 @{ Values = @('M:Kg medidos experimentales') }),
            (V 'expPorColor' 'clusteredColumnChart' 'Depósitos sintéticos por caneca' 20 180 400 300 @{ Category = @('Depositos.Color'); Y = @('M:Depósitos experimentales') }),
            (V 'expTendencia' 'lineChart' 'Tendencia diaria del lote' 440 180 820 300 @{ Category = @('Calendario.Fecha'); Y = @('M:Depósitos experimentales') }),
            (V 'expMateriales' 'clusteredBarChart' 'Materiales del lote' 20 490 400 220 @{ Category = @('Depositos.Material'); Y = @('M:Depósitos experimentales') }),
            (V 'expEstados' 'donutChart' 'Estado de registro' 440 490 400 220 @{ Category = @('Depositos.Estado'); Y = @('M:Depósitos experimentales') }),
            (V 'lotes' 'tableEx' 'Lotes registrados (V025)' 860 490 400 220 @{ Values = @('Lotes.Lote', 'Lotes.Ciudadanos', 'Lotes.DepositosCreados', 'Lotes.Estado') })
        )
    }
    vision = @{
        titulo = 'Reconocimiento visual'
        textos = @(
            (Texto 'titulo' 20 8 1240 40 'Reconocimiento visual · Consultas reales de Echo (Oracle, V023)'),
            (Texto 'separador' 20 360 1240 40 'Evaluación documentada del modelo (fotos públicas etiquetadas; NO son datos operativos)' '#6B4E00' '13pt'),
            (Texto 'limites' 20 670 1240 40 'Limitaciones: icopor y multicapa son los puntos débiles; la apertura exige confirmación y los ecopuntos esperan la verificación del operario. No hubo entrenamiento propio.' '#6B4E00' '10pt')
        )
        visuales = @(
            (V 'kpiEcho' 'card' 'Consultas de Echo' 20 56 300 110 @{ Values = @('M:Consultas visuales') }),
            (V 'kpiValidas' 'card' '% con destino válido' 330 56 300 110 @{ Values = @('M:Destino valido texto') }),
            (V 'kpiLatencia' 'card' 'Latencia media (ms)' 640 56 300 110 @{ Values = @('M:Latencia texto') }),
            (V 'echoMaterial' 'clusteredBarChart' 'Materiales reconocidos' 20 176 620 180 @{ Category = @('ConsultasVision.Material'); Y = @('M:Consultas visuales') }),
            (V 'echoResultado' 'donutChart' 'Resultado de las consultas' 660 176 600 180 @{ Category = @('ConsultasVision.Resultado'); Y = @('M:Consultas visuales') }),
            (V 'evaluacion' 'tableEx' 'Rondas independientes (docs/vision)' 20 404 780 260 @{ Values = @('EvaluacionVision.Ronda', 'EvaluacionVision.Fotos', 'EvaluacionVision.Correctas', 'EvaluacionVision.FalsasAdmisiones', 'EvaluacionVision.OfertasIndebidas', 'EvaluacionVision.FalsosRechazos', 'EvaluacionVision.Independiente') }),
            (V 'exactitud' 'clusteredColumnChart' 'Exactitud por ronda' 820 404 440 260 @{ Category = @('EvaluacionVision.Ronda'); Y = @('M:Exactitud de la evaluación') })
        )
    }
}

foreach ($id in $paginas.Keys) {
    $p = $paginas[$id]
    $dir = Join-Path $raiz "definition\pages\$id"
    Escribir (Join-Path $dir 'page.json') ([ordered]@{
        '$schema' = 'https://developer.microsoft.com/json-schemas/fabric/item/report/definition/page/1.0.0/schema.json'
        name = $id; displayName = $p.titulo; displayOption = 'FitToPage'; height = 720; width = 1280
    })
    $z = 0
    foreach ($t in $p.textos) { $t.position.z = $z; $z += 100; Escribir (Join-Path $dir "visuals\$($t.name)\visual.json") $t }
    foreach ($v in $p.visuales) { Escribir (Join-Path $dir "visuals\$($v.nombre)\visual.json") (Visual $v $z); $z += 100 }
}

Escribir (Join-Path $raiz 'definition\pages\pages.json') ([ordered]@{
    '$schema' = 'https://developer.microsoft.com/json-schemas/fabric/item/report/definition/pagesMetadata/1.0.0/schema.json'
    pageOrder = @($paginas.Keys); activePageName = 'resumen'
})
Escribir (Join-Path $raiz 'definition\version.json') ([ordered]@{
    '$schema' = 'https://developer.microsoft.com/json-schemas/fabric/item/report/definition/versionMetadata/1.0.0/schema.json'; version = '2.0.0'
})
Escribir (Join-Path $raiz 'definition\report.json') ([ordered]@{
    '$schema' = 'https://developer.microsoft.com/json-schemas/fabric/item/report/definition/report/1.0.0/schema.json'
    layoutOptimization = 'None'
    themeCollection = [ordered]@{
        baseTheme = @{ name = 'CY24SU10'; reportVersionAtImport = '5.59'; type = 'SharedResources' }
        customTheme = @{ name = 'EcoAccess.json'; reportVersionAtImport = '5.59'; type = 'RegisteredResources' }
    }
    resourcePackages = @(@{ name = 'RegisteredResources'; type = 'RegisteredResources'; items = @(@{ name = 'EcoAccess.json'; path = 'EcoAccess.json'; type = 'CustomTheme' }) })
    settings = @{ useStylableVisualContainerHeader = $true; defaultDrillFilterOtherVisuals = $true }
})
Escribir (Join-Path $raiz 'StaticResources\RegisteredResources\EcoAccess.json') ([ordered]@{
    name = 'EcoAccess'
    dataColors = @('#1F6B3A', '#7BB661', '#2E2E2E', '#9CC5A8', '#F2B134', '#3D8B5A', '#C0392B', '#5D6D7E')
    background = '#FFFFFF'; foreground = '#1D2B22'; tableAccent = '#1F6B3A'
})
Escribir (Join-Path $raiz 'definition.pbir') ([ordered]@{
    '$schema' = 'https://developer.microsoft.com/json-schemas/fabric/item/report/definitionProperties/2.0.0/schema.json'
    version = '4.0'; datasetReference = @{ byPath = @{ path = '../EcoAccessP3.SemanticModel' } }
})
Escribir (Join-Path $raiz '.platform') ([ordered]@{
    '$schema' = 'https://developer.microsoft.com/json-schemas/fabric/gitIntegration/platformProperties/2.0.0/schema.json'
    metadata = @{ type = 'Report'; displayName = 'EcoAccessP3' }
    config = @{ version = '2.0'; logicalId = 'c4e7a2d9-1b38-4f6a-9e05-7d2b8c1a3f64' }
})
"Informe generado en $raiz"
