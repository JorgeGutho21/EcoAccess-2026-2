<#
  Ejecuta uno o varios scripts .sql con una de las cuentas del proyecto y muestra la salida.
  La contraseña sale de la variable de entorno <USUARIO>_CLAVE, del archivo
  _LOCAL_NO_PUBLICAR\credenciales_locales.env o te la pide en pantalla.

  Ejemplos:
    pwsh -File 18_ENTREGA_Y_RESPALDOS\herramientas\ejecutar_sql.ps1 -Usuario ECOACCESS_BD -Scripts 04_SCRIPTS_ORACLE\12_pruebas.sql
    pwsh -File 18_ENTREGA_Y_RESPALDOS\herramientas\ejecutar_sql.ps1 -Usuario ECOACCESS_BD -Scripts 04_SCRIPTS_ORACLE\13_consultas_exposicion.sql -Salida C:\temp\consultas.txt
  También lo usa 08_CONSULTAS_Y_EJERCICIOS\generar_banco.ps1 (-Ejecutor) para verificar los 309 ejercicios.
#>
param(
    [Parameter(Mandatory)] [ValidateSet('ECOACCESS_BD', 'ECOACCESS_PRACTICAS', 'ECOACCESS_BD_LECTOR')] [string]$Usuario,
    [Parameter(Mandatory)] [string[]]$Scripts,
    [string]$Salida
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'comun.ps1')
# Desde la línea de comandos varios scripts llegan como un solo texto separado por comas.
$Scripts = @($Scripts | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
[void](Initialize-EcoOracle)
(Invoke-EcoSql -Usuario $Usuario -Scripts $Scripts -Salida $Salida).Texto
