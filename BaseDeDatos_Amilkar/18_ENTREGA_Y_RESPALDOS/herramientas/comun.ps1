<#
  Funciones compartidas por INSTALAR_Y_PROBAR.ps1 y ejecutar_sql.ps1.
  Qué resuelven, en palabras simples:
   - Buscar SQL*Plus en tu PC y apuntar TNS_ADMIN a la configuración LOCAL de Oracle
     (en este equipo TNS_ADMIN apuntaba a la wallet de la nube y eso causaba ORA-01017).
   - Conseguir la contraseña de cada cuenta SIN escribirla en ningún archivo del proyecto:
       1) variable de entorno <USUARIO>_CLAVE   (por ejemplo ECOACCESS_BD_CLAVE)
       2) archivo _LOCAL_NO_PUBLICAR\credenciales_locales.env (solo en tu PC, nunca en Git)
       3) si no está, te la pide en pantalla sin mostrarla.
   - Ejecutar scripts .sql en SQL*Plus enviando la conexión por la entrada estándar
     (así la contraseña no queda en disco ni en la lista de procesos) y quitarla de la salida.
#>
$script:Raiz = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)   # herramientas -> 18_ENTREGA_Y_RESPALDOS -> raíz
$script:Utf8 = New-Object Text.UTF8Encoding $false

function Get-EcoServicio { if ($env:ECOACCESS_SERVICIO) { $env:ECOACCESS_SERVICIO } else { 'localhost:1521/FREEPDB1' } }

function Initialize-EcoOracle {
    $sql = (Get-Command sqlplus -ErrorAction SilentlyContinue).Source
    if (-not $sql) {
        $sql = Get-ChildItem 'C:\app\*\product\*\*\bin\sqlplus.exe', 'C:\app\*\product\*\*\*\bin\sqlplus.exe' -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
    }
    if (-not $sql) { throw 'No encontré SQL*Plus. Instala Oracle Database Free o agrega su carpeta bin al PATH.' }
    $admin = Join-Path (Split-Path (Split-Path $sql)) 'network\admin'
    if (-not $env:ECOACCESS_TNS_ADMIN_FIJO) { $env:TNS_ADMIN = $admin }
    $env:NLS_LANG = 'SPANISH_COLOMBIA.AL32UTF8'
    [Console]::OutputEncoding = $script:Utf8
    $global:OutputEncoding = $script:Utf8
    $script:SqlPlus = $sql
    $sql
}

function Get-EcoClave([string]$Usuario, [switch]$Obligatoria) {
    $var = "$($Usuario)_CLAVE"
    $v = [Environment]::GetEnvironmentVariable($var)
    if ($v) { return $v }
    $archivo = Join-Path $script:Raiz '_LOCAL_NO_PUBLICAR\credenciales_locales.env'
    if (Test-Path $archivo) {
        foreach ($l in Get-Content $archivo -Encoding UTF8) {
            if ($l -match "^$([regex]::Escape($Usuario))=(.+)$") { [Environment]::SetEnvironmentVariable($var, $matches[1]); return $matches[1] }
        }
    }
    $s = Read-Host "Contraseña de $Usuario (no se muestra)" -AsSecureString
    $v = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))
    if (-not $v -and $Obligatoria) { throw "No se dio contraseña para $Usuario." }
    [Environment]::SetEnvironmentVariable($var, $v)
    $v
}

function Invoke-EcoSql {
    param(
        [Parameter(Mandatory)] [string]$Usuario,
        [string[]]$Scripts = @(),
        [string[]]$Sentencias = @(),
        [string[]]$Respuestas = @(),
        [string]$Salida,
        # Solo para crear los usuarios la primera vez: entra como administrador del sistema
        # operativo (/ AS SYSDBA, sin contraseña) y se ubica en la PDB del servicio.
        [switch]$ComoSysdba,
        # Carpeta desde la que corre SQL*Plus (los SPOOL relativos se escriben allí). Por defecto, la raíz.
        [string]$Directorio
    )
    if (-not $script:SqlPlus) { [void](Initialize-EcoOracle) }
    $servicio = Get-EcoServicio
    $lineas = New-Object Collections.Generic.List[string]
    if ($ComoSysdba) {
        if (-not $env:ORACLE_SID) { $env:ORACLE_SID = 'FREE' }
        $clave = $null
        $lineas.Add('CONNECT / AS SYSDBA')
        $lineas.Add("ALTER SESSION SET CONTAINER = $(($servicio -split '/')[-1]);")
    } else {
        $clave = Get-EcoClave $Usuario -Obligatoria
        $lineas.Add("CONNECT $Usuario/""$clave""@//$servicio")
    }
    $lineas.Add('SET SQLBLANKLINES ON')
    $lineas.Add("ALTER SESSION SET NLS_DATE_FORMAT='YYYY-MM-DD HH24:MI';")
    $primero = $true
    foreach ($s in $Scripts) {
        $completo = (Resolve-Path $s).Path
        # SQL*Plus no abre scripts cuya ruta completa pasa de ~240 caracteres (SP2-0310) y se confunde con
        # tildes. Por eso cada script se copia a una carpeta corta del sistema y se ejecuta desde allí;
        # el directorio de trabajo sigue siendo la raíz del proyecto (los SPOOL relativos caen en su lugar).
        $corta = Join-Path ([IO.Path]::GetTempPath()) 'ecoaccess_sql'
        New-Item -ItemType Directory -Force $corta | Out-Null
        $rel = Join-Path $corta ([IO.Path]::GetFileName($completo))
        # Se copian todos los .sql de la misma carpeta: así funcionan los @@ del script maestro.
        Copy-Item (Join-Path (Split-Path $completo) '*.sql') $corta -Force
        $lineas.Add("PROMPT ######## $([IO.Path]::GetFileName($completo)) ########")
        $lineas.Add("@""$rel""")
        if ($primero) { foreach ($r in $Respuestas) { $lineas.Add($r) }; $primero = $false }
    }
    foreach ($x in $Sentencias) { $lineas.Add($x) }
    $lineas.Add('EXIT')
    Push-Location $(if ($Directorio) { $Directorio } else { $script:Raiz })
    try { $sal = ($lineas | & $script:SqlPlus -S -L /nolog 2>&1 | Out-String); $codigo = $LASTEXITCODE } finally { Pop-Location }
    foreach ($secreto in @($clave) + $Respuestas) { if ($secreto -and $secreto.Length -ge 6) { $sal = $sal.Replace($secreto, '********') } }
    if ($Salida) { [IO.File]::WriteAllText($Salida, $sal, $script:Utf8) }
    [pscustomobject]@{ Texto = $sal; Codigo = $codigo }
}
