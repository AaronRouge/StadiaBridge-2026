$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.build-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
[xml]$versionProps = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Directory.Build.props') -Raw
$appVersion = [string]$versionProps.Project.PropertyGroup.Version
if ($appVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Version no valida en Directory.Build.props.' }
$installerName = "StadiaBridge-Setup-$appVersion-x64.exe"
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot "dist\$installerName")) {
    throw 'Ya existe un instalador de esta version. Se conserva sin cambios; incrementa Version en Directory.Build.props antes de generar otro.'
}
$compiler = Join-Path $PSScriptRoot '.build-tools\InnoSetup\ISCC.exe'
$driver = Join-Path $PSScriptRoot 'installer\vendor\ViGEmBus_1.22.0_x64_x86_arm64.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Falta el compilador Inno Setup portable. Consulta installer/README.md.' }
if (-not (Test-Path -LiteralPath $driver)) { throw 'Falta el instalador oficial ViGEmBus 1.22.0. Consulta installer/README.md.' }
$expected = '89220A7865076B342892F98865F3499FB7C4CFD673159E89D352C360FD014C6A'
if ((Get-FileHash -LiteralPath $driver -Algorithm SHA256).Hash -ne $expected) { throw 'La huella de ViGEmBus no coincide con la versión oficial.' }
if ((Get-AuthenticodeSignature -LiteralPath $driver).Status -ne 'Valid') { throw 'La firma de ViGEmBus no es válida.' }
dotnet run --project tests/StadiaBridge.Checks -c Release
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las comprobaciones.' }
dotnet publish src/StadiaBridge/StadiaBridge.csproj -c Release -r win-x64 --self-contained true -p:NuGetAudit=false -o dist/StadiaBridge-Standalone
if ($LASTEXITCODE -ne 0) { throw 'Falló la publicación autónoma.' }
foreach ($document in @('README.md', 'FUNCIONAMIENTO-Y-CREDITOS.md', 'CHANGELOG.md', 'CONTRIBUTING.md')) {
    Copy-Item -LiteralPath $document -Destination dist/StadiaBridge-Standalone
}
$docsDir = Join-Path $PSScriptRoot 'dist\StadiaBridge-Standalone\docs'
New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
Get-ChildItem -LiteralPath docs -Filter '*.md' -File | Copy-Item -Destination $docsDir
$sourceLicenseDir = Join-Path $PSScriptRoot 'dist\StadiaBridge-Standalone\licenses'
New-Item -ItemType Directory -Path $sourceLicenseDir -Force | Out-Null
Get-ChildItem -LiteralPath licenses -File | Copy-Item -Destination $sourceLicenseDir
$licenseDir = Join-Path $PSScriptRoot 'dist\StadiaBridge-Standalone\Licencias'
New-Item -ItemType Directory -Path $licenseDir -Force | Out-Null
Copy-Item -LiteralPath licenses/ViGEmBus-LICENSE.txt -Destination $licenseDir
$runtimeConfig = Get-Content dist/StadiaBridge-Standalone/StadiaBridge.runtimeconfig.json -Raw | ConvertFrom-Json
foreach ($framework in $runtimeConfig.runtimeOptions.includedFrameworks) {
    $packageName = ($framework.name + '.Runtime.win-x64').ToLowerInvariant()
    $packageDir = Join-Path $env:DOTNET_CLI_HOME ('.nuget\packages\' + $packageName + '\' + $framework.version)
    $runtimeLicenses = @(Get-ChildItem -LiteralPath $packageDir -File | Where-Object { $_.Name -match '^LICENSE(\.TXT)?$|^THIRD-PARTY-NOTICES\.TXT$' })
    if ($runtimeLicenses.Count -eq 0) { throw "No se encontraron licencias del runtime en $packageDir" }
    foreach ($licenseFile in $runtimeLicenses) {
        Copy-Item -LiteralPath $licenseFile.FullName -Destination (Join-Path $licenseDir ($framework.name + '-' + $licenseFile.Name))
    }
}
& $compiler /Q "/DAppVersion=$appVersion" installer/StadiaBridge.iss
if ($LASTEXITCODE -ne 0) { throw 'Falló la creación del instalador.' }
$hash = Get-FileHash -LiteralPath (Join-Path 'dist' $installerName) -Algorithm SHA256
Set-Content -LiteralPath (Join-Path 'dist' "StadiaBridge-Setup-$appVersion-x64.sha256") -Value ($hash.Hash + '  ' + $installerName) -Encoding ascii
$hash | Format-List
