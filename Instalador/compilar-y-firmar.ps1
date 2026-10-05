<#
.SYNOPSIS
    Compila la solución, compila el instalador con Inno Setup y lo firma con SignTool.

.DESCRIPTION
    0. Verifica que no haya cambios sin commitear en BD, Instalador, GUI, BLL, DAL y BE
       (el instalador tiene que salir de un estado versionado, no de archivos sueltos).
    1. Recompila (Rebuild, Release) la solución completa y DbInstaller.
    2. Descarga SQL Server LocalDB si falta y verifica que su firma sea válida Y de Microsoft.
    3. Compila WardrobeFlow_Setup.iss  ->  Salida\Instalador_WardrobeFlow_<version>.exe
       Si hay certificado y SignTool, Inno firma con él el instalador Y el desinstalador
       (directivas SignTool + SignedUninstaller, activadas con /DFIRMAR).
       Sin Windows SDK (sin SignTool) firma solo el instalador con Set-AuthenticodeSignature.

    Si no se encuentra el certificado, el instalador queda compilado SIN firmar (no falla).

    El certificado NO se versiona (contiene la clave privada). Por defecto se busca en
    %USERPROFILE%\wardrobeflow.pfx. La contraseña se toma de la variable de entorno
    WF_PFX_PASS o, si no existe, se pide por consola (SecureString).

    Contraseña y línea de comandos: la contraseña NUNCA se pasa a signtool.exe (antes iba con
    /p y quedaba visible en la lista de procesos). El .pfx se importa temporalmente al almacén
    CurrentUser\My (clave NO exportable), signtool firma por huella (/sha1) y al terminar el
    certificado se borra del almacén, aunque la compilación falle.
    Riesgo residual: si se usa WF_PFX_PASS, la contraseña está en texto plano en el entorno
    del proceso (y de sus hijos) mientras dura el script; pedirla por consola lo evita.
    Mientras dura la compilación, otro proceso del mismo usuario podría usar el certificado
    importado para firmar.

.EXAMPLE
    .\compilar-y-firmar.ps1
    .\compilar-y-firmar.ps1 -Pfx D:\certs\otro.pfx
#>
param(
    [string]$Pfx = (Join-Path $env:USERPROFILE 'wardrobeflow.pfx')
)

$ErrorActionPreference = 'Stop'
$dir  = $PSScriptRoot
$raiz = Split-Path $dir -Parent
$iss  = Join-Path $dir 'WardrobeFlow_Setup.iss'

# Versión del instalador (la misma que usa el .iss para el nombre del .exe).
$version = ([regex]::Match((Get-Content $iss -Raw), '#define MyAppVersion "([^"]+)"')).Groups[1].Value
if (-not $version) { throw 'No se encontró MyAppVersion en WardrobeFlow_Setup.iss.' }
$exe = Join-Path $dir "Salida\Instalador_WardrobeFlow_$version.exe"

# ── 0) Árbol limpio ──────────────────────────────────────────────────────────
$cambios = & git -C $raiz status --porcelain BD Instalador GUI BLL DAL BE
if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar git status.' }
if ($cambios) {
    $cambios | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    throw 'Hay cambios sin commitear en BD/Instalador/GUI/BLL/DAL/BE: commitealos (o descartalos) antes de generar el instalador.'
}

# ── 1) Compilar la solución y DbInstaller (Rebuild, Release) ─────────────────
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = $null
if (Test-Path $vswhere) {
    $msbuild = & $vswhere -latest -prerelease -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
        Select-Object -First 1
}
if (-not $msbuild) { throw 'No se encontró MSBuild (Visual Studio).' }

Write-Host 'Compilando la solución (Rebuild, Release)...' -ForegroundColor Cyan
& $msbuild (Join-Path $raiz 'WardrobeFlow.slnx') -t:Rebuild -p:Configuration=Release -v:minimal -nologo
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de la solución.' }
& $msbuild (Join-Path $dir 'DbInstaller\DbInstaller.csproj') -t:Rebuild -p:Configuration=Release -v:minimal -nologo
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación de DbInstaller.' }

# ── 2) SQL Server 2022 Express LocalDB (va embebido para equipos sin SQL) ────
$msi = Join-Path $dir 'Redist\SqlLocalDB.msi'
if (-not (Test-Path $msi)) {
    Write-Host 'Descargando SQL Server LocalDB...' -ForegroundColor Cyan
    New-Item -ItemType Directory -Force (Split-Path $msi) | Out-Null
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12  # PowerShell 5.1 no lo usa por defecto
    Invoke-WebRequest 'https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SqlLocalDB.msi' -OutFile $msi -UseBasicParsing
}
$firmaMsi = Get-AuthenticodeSignature $msi
if ($firmaMsi.Status -ne 'Valid') { throw 'SqlLocalDB.msi no tiene una firma válida.' }
if (-not $firmaMsi.SignerCertificate -or $firmaMsi.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
    throw "SqlLocalDB.msi está firmado, pero no por Microsoft: $($firmaMsi.SignerCertificate.Subject)"
}

# ── 3) Compilar con Inno Setup (y firmar) ────────────────────────────────────
$iscc = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'No se encontró Inno Setup 6 (ISCC.exe).' }

if (-not (Test-Path $Pfx)) {
    Write-Host 'Compilando el instalador...' -ForegroundColor Cyan
    & $iscc $iss | Select-Object -Last 3
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $exe)) { throw 'Falló la compilación del instalador.' }
    Write-Warning "No se encontró el certificado ($Pfx): el instalador queda SIN firmar."
    Write-Host $exe
    return
}

if ($env:WF_PFX_PASS) {
    $secPass = ConvertTo-SecureString $env:WF_PFX_PASS -AsPlainText -Force
} else {
    $secPass = Read-Host 'Contraseña del certificado' -AsSecureString
}

$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\x64\\' } | Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if ($signtool) {
    # Import temporal al almacén del usuario: signtool firma por huella y nunca ve la contraseña.
    $cert = Import-PfxCertificate -FilePath $Pfx -CertStoreLocation 'Cert:\CurrentUser\My' -Password $secPass
    $huella = $cert.Thumbprint
    try {
        # $q = comillas y $f = archivo a firmar (sintaxis de la directiva SignTool de Inno).
        $comando = "`$q$signtool`$q sign /sha1 $huella /s My /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 `$f"
        Write-Host 'Compilando y firmando el instalador y el desinstalador...' -ForegroundColor Cyan
        & $iscc '/DFIRMAR' "/Swfsign=$comando" $iss | Select-Object -Last 3
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path $exe)) { throw 'Falló la compilación/firma del instalador.' }
    } finally {
        Remove-Item "Cert:\CurrentUser\My\$huella" -ErrorAction SilentlyContinue
    }
} else {
    # Sin Windows SDK: se compila sin firmar el desinstalador y se firma el instalador con el
    # cmdlet de PowerShell (misma firma: SHA256 + sello de tiempo).
    Write-Warning 'No se encontró SignTool (Windows SDK): el desinstalador queda sin firmar.'
    & $iscc $iss | Select-Object -Last 3
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $exe)) { throw 'Falló la compilación del instalador.' }
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($Pfx, $secPass)
    $r = Set-AuthenticodeSignature -FilePath $exe -Certificate $cert -HashAlgorithm SHA256 -TimestampServer 'http://timestamp.digicert.com'
    # Con certificado autofirmado el estado es UnknownError (raíz no confiable) pero la firma queda aplicada.
    if (-not $r.SignerCertificate) { throw "Falló la firma: $($r.StatusMessage)" }
}

$firma = Get-AuthenticodeSignature $exe
Write-Host ("Firmado por: {0}  |  Estado: {1}" -f $firma.SignerCertificate.Subject, $firma.Status) -ForegroundColor Green
Write-Host $exe
