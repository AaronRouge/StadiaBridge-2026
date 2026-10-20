[CmdletBinding()]
param(
    [switch]$Client,
    [switch]$Driver
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$downloadDir = Join-Path $projectRoot '.build-home\downloads'
New-Item -ItemType Directory -Path $downloadDir -Force | Out-Null
if (-not $Client -and -not $Driver) { $Driver = $true }

function Assert-Hash([string]$Path, [string]$Expected) {
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Expected) {
        throw "SHA-256 incorrecto: $Path"
    }
}

function Get-VerifiedFile([string]$Url, [string]$Destination, [string]$Hash) {
    if (Test-Path -LiteralPath $Destination) {
        Assert-Hash $Destination $Hash
        return
    }
    # La descarga solo se usa despues de validar su contenido.
    $partialPath = $Destination + '.download'
    Invoke-WebRequest -Uri $Url -OutFile $partialPath -UseBasicParsing
    Assert-Hash $partialPath $Hash
    Move-Item -LiteralPath $partialPath -Destination $Destination -Force
}

if ($Client) {
    $clientPath = Join-Path $projectRoot 'lib\ViGEmClient.dll'
    $clientHash = '966e579ad8224ca87ae78a8788c5eb0c33ecb932dcc6ca25c39871e20b97a15a'
    if (Test-Path -LiteralPath $clientPath) {
        Assert-Hash $clientPath $clientHash
    } else {
        $archivePath = Join-Path $downloadDir 'stadiacontroller-1.3.1.zip'
        Get-VerifiedFile 'https://github.com/71/stadiacontroller/releases/download/v1.3.1/stadiacontroller.zip' $archivePath '820eb03606a2edc27b8d4b1db862bf7547cf74f7bfe20a0f10dd85f81d919308'
        $extractDir = Join-Path $downloadDir ('client-' + [guid]::NewGuid().ToString('N'))
        Expand-Archive -LiteralPath $archivePath -DestinationPath $extractDir
        $candidates = @(Get-ChildItem -LiteralPath $extractDir -Filter ViGEmClient.dll -File -Recurse)
        if ($candidates.Count -ne 1) { throw 'El ZIP no contiene una unica ViGEmClient.dll.' }
        Assert-Hash $candidates[0].FullName $clientHash
        Copy-Item -LiteralPath $candidates[0].FullName -Destination $clientPath
    }
    Write-Host 'ViGEmClient x64 verificada.'
}

if ($Driver) {
    $vendorDir = Join-Path $projectRoot 'installer\vendor'
    New-Item -ItemType Directory -Path $vendorDir -Force | Out-Null
    $driverPath = Join-Path $vendorDir 'ViGEmBus_1.22.0_x64_x86_arm64.exe'
    Get-VerifiedFile 'https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe' $driverPath '89220a7865076b342892f98865f3499fb7c4cfd673159e89d352c360fd014c6a'
    $signature = Get-AuthenticodeSignature -LiteralPath $driverPath
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Nefarius') {
        throw 'No se pudo validar la firma oficial de Nefarius en ViGEmBus.'
    }
    Write-Host 'ViGEmBus 1.22.0 verificado; no se ha ejecutado ni instalado.'
}
