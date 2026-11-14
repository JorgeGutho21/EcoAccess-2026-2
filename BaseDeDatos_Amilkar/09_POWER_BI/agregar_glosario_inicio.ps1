<#
  Reemplaza el cuadro de texto «explica» de la página Inicio por un GLOSARIO de 6 tarjetas de color
  para que en la exposición se entienda qué es cada cosa:
    datos sintéticos vs. datos del sistema · consulta visual · depósito · kilos · EcoPuntos · roles.
  Trabaja sobre el informe en formato PBIR del proyecto .pbip. Guarda una copia del visual anterior.
  Uso: pwsh -File agregar_glosario_inicio.ps1
#>
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$vis = Join-Path $aqui 'PROYECTO_PBIP\EcoAccess_BD\EcoAccess_BD.Report\definition\pages\ReportSection1\visuals'
$bk = Join-Path $aqui '..\_VERSIONES_ANTERIORES_LOCALES\PBIR_inicio_explica_antes_glosario'
if (Test-Path "$vis\explica") { New-Item -ItemType Directory -Force $bk | Out-Null; Copy-Item "$vis\explica" $bk -Recurse -Force; Remove-Item "$vis\explica" -Recurse -Force }
$URL = 'https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.0.0/schema.json'
function Lit($v) { @{ expr = @{ Literal = @{ Value = $v } } } }
$tarjetas = @(
  @{ n = 'glos_sinteticos'; c = '#424949'; f = '#EAECEE'; t = 'Datos sintéticos vs. datos del sistema'; l = @('Todo lo que se ve aquí fue GENERADO para la demostración (11_datos_sinteticos.sql, semilla 20261110) y lleva es_sintetico = 1.', 'Los datos reales de EcoAccess P3 están en Oracle Cloud y NUNCA se mezclan con estos.') }
  @{ n = 'glos_consulta'; c = '#B03A2E'; f = '#FBEFED'; t = 'Consulta visual (foto)'; l = @('Cada foto que la persona envía a la IA Eco queda en CONSULTA_VISION.', 'Solo ORIENTA (qué material es y a qué caneca va): una foto NO es un depósito.') }
  @{ n = 'glos_deposito'; c = '#B03A2E'; f = '#FBEFED'; t = 'Depósito'; l = @('Residuo que entró a una caneca (o fue declarado sin sensor). Nace PENDIENTE (0).', 'Un operario lo VERIFICA (1) o lo RECHAZA (2). Solo los verificados cuentan.') }
  @{ n = 'glos_kilos'; c = '#9A7D0A'; f = '#FCF8E8'; t = 'Kilos estimados vs. medidos'; l = @('Estimados: salen del catálogo PESO_REFERENCIAL (aproximación).', 'Medidos: solo con báscula. Si no hubo báscula quedan vacíos. Nunca se suman entre sí.') }
  @{ n = 'glos_ecopuntos'; c = '#6C3483'; f = '#F4EEF7'; t = 'EcoPuntos'; l = @('Premio por cada depósito VERIFICADO, según la tarifa (REGLA_ECOPUNTOS).', 'Cada crédito o reverso es un movimiento; el saldo es la SUMA de los movimientos.') }
  @{ n = 'glos_roles'; c = '#2E6DA4'; f = '#EEF4FB'; t = 'Roles'; l = @('Ciudadano (1) recicla y consulta; Operario (2) verifica depósitos de otros;', 'Administrador (3) además gestiona canecas y tarifas. Una persona puede tener varios roles.') }
)
$w = 405; $h = 138; $x0 = 20; $y0 = 420; $gx = 12; $gy = 10
for ($i = 0; $i -lt $tarjetas.Count; $i++) {
  $tj = $tarjetas[$i]; $col = $i % 3; $fil = [math]::Floor($i / 3)
  $par = @(@{ textRuns = @(@{ value = $tj.t; textStyle = @{ fontSize = '13pt'; color = $tj.c; fontWeight = 'bold' } }) })
  foreach ($l in $tj.l) { $par += @{ textRuns = @(@{ value = "• $l"; textStyle = @{ fontSize = '10.5pt'; color = '#1C2833' } }) } }
  $obj = [ordered]@{
    '$schema' = $URL; name = $tj.n
    position = [ordered]@{ x = $x0 + $col * ($w + $gx); y = $y0 + $fil * ($h + $gy); z = 1100 + $i; width = $w; height = $h }
    visual = [ordered]@{
      visualType = 'textbox'
      objects = @{ general = @(@{ properties = @{ paragraphs = $par } }) }
      visualContainerObjects = [ordered]@{
        background = @(@{ properties = [ordered]@{ show = (Lit 'true'); color = @{ solid = @{ color = (Lit "'$($tj.f)'") } }; transparency = (Lit '0D') } })
        border = @(@{ properties = [ordered]@{ show = (Lit 'true'); color = @{ solid = @{ color = (Lit "'$($tj.c)'") } }; radius = (Lit '8D') } })
      }
      drillFilterOtherVisuals = $true
    }
  }
  New-Item -ItemType Directory -Force "$vis\$($tj.n)" | Out-Null
  [IO.File]::WriteAllText("$vis\$($tj.n)\visual.json", ($obj | ConvertTo-Json -Depth 30), (New-Object Text.UTF8Encoding $false))
  "$($tj.n) → x$($obj.position.x) y$($obj.position.y)"
}
