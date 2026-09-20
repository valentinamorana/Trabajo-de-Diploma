<#
.SYNOPSIS
    Compila el instalador con Inno Setup y lo firma con SignTool.

.DESCRIPTION
    1. Compila WardrobeFlow_Setup.iss  ->  Salida\Instalador_WardrobeFlow_V1.exe
    2. Firma el .exe con el certificado .pfx (SignTool, SHA256 + sello de tiempo).

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
if (-not $signtool) {
    Write-Warning 'No se encontro signtool.exe (Windows SDK): el instalador queda SIN firmar.'
    return
}

$pass = $env:WF_PFX_PASS
if (-not $pass) {
    $sec  = Read-Host 'Contrasena del certificado' -AsSecureString
    $pass = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec))
}

Write-Host 'Firmando el instalador...' -ForegroundColor Cyan
& $signtool sign /f $Pfx /p $pass /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $exe
if ($LASTEXITCODE -ne 0) { throw 'Fallo la firma con SignTool.' }

$firma = Get-AuthenticodeSignature $exe
Write-Host ("Firmado por: {0}  |  Estado: {1}" -f $firma.SignerCertificate.Subject, $firma.Status) -ForegroundColor Green
Write-Host $exe
