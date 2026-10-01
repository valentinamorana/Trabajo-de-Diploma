<#
.SYNOPSIS
    Compila el instalador con Inno Setup y lo firma con SignTool.

.DESCRIPTION
    1. Compila WardrobeFlow_Setup.iss  ->  Salida\Instalador_WardrobeFlow_V1.exe
    2. Firma el .exe con el certificado .pfx (SignTool, SHA256 + sello de tiempo).
       Si no esta instalado el Windows SDK, firma con Set-AuthenticodeSignature.

    Antes de correrlo hay que compilar la solución en Release (y DbInstaller en Release).
    Si no se encuentra el certificado, el instalador queda compilado SIN firmar (no falla).

    El certificado NO se versiona (contiene la clave privada). Por defecto se busca en
    %USERPROFILE%\wardrobeflow.pfx. La contraseña se toma de la variable de entorno
    WF_PFX_PASS o, si no existe, se pide por consola.

.EXAMPLE
    .\compilar-y-firmar.ps1
    .\compilar-y-firmar.ps1 -Pfx D:\certs\otro.pfx
#>
param(
    [string]$Pfx = (Join-Path $env:USERPROFILE 'wardrobeflow.pfx')
)

$ErrorActionPreference = 'Stop'
$dir = $PSScriptRoot
$exe = Join-Path $dir 'Salida\Instalador_WardrobeFlow_V1.exe'

# ── 0) SQL Server 2022 Express LocalDB (va embebido para equipos sin SQL) ────
$msi = Join-Path $dir 'Redist\SqlLocalDB.msi'
if (-not (Test-Path $msi)) {
    Write-Host 'Descargando SQL Server LocalDB...' -ForegroundColor Cyan
    New-Item -ItemType Directory -Force (Split-Path $msi) | Out-Null
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12  # PowerShell 5.1 no lo usa por defecto
    Invoke-WebRequest 'https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SqlLocalDB.msi' -OutFile $msi -UseBasicParsing
}
if ((Get-AuthenticodeSignature $msi).Status -ne 'Valid') { throw 'SqlLocalDB.msi no tiene una firma valida de Microsoft.' }

# ── 1) Compilar con Inno Setup ───────────────────────────────────────────────
$iscc = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'No se encontro Inno Setup 6 (ISCC.exe).' }

Write-Host 'Compilando el instalador...' -ForegroundColor Cyan
& $iscc (Join-Path $dir 'WardrobeFlow_Setup.iss') | Select-Object -Last 3
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $exe)) { throw 'Fallo la compilacion del instalador.' }

# ── 2) Firmar con SignTool ───────────────────────────────────────────────────
if (-not (Test-Path $Pfx)) {
    Write-Warning "No se encontro el certificado ($Pfx): el instalador queda SIN firmar."
    return
}

$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\x64\\' } | Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName
$pass = $env:WF_PFX_PASS
if (-not $pass) {
    $sec  = Read-Host 'Contrasena del certificado' -AsSecureString
    $pass = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec))
}

Write-Host 'Firmando el instalador...' -ForegroundColor Cyan
if ($signtool) {
    & $signtool sign /f $Pfx /p $pass /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $exe
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la firma con SignTool.' }
} else {
    # Sin Windows SDK: misma firma (SHA256 + sello de tiempo) con el cmdlet de PowerShell.
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($Pfx, $pass)
    $r = Set-AuthenticodeSignature -FilePath $exe -Certificate $cert -HashAlgorithm SHA256 -TimestampServer 'http://timestamp.digicert.com'
    # Con certificado autofirmado el estado es UnknownError (raiz no confiable) pero la firma queda aplicada.
    if (-not $r.SignerCertificate) { throw "Fallo la firma: $($r.StatusMessage)" }
}

$firma = Get-AuthenticodeSignature $exe
Write-Host ("Firmado por: {0}  |  Estado: {1}" -f $firma.SignerCertificate.Subject, $firma.Status) -ForegroundColor Green
Write-Host $exe
