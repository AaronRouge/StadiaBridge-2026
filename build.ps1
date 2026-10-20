$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.build-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
dotnet run --project tests/StadiaBridge.Checks -c Release
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las comprobaciones del protocolo.' }
dotnet publish src/StadiaBridge/StadiaBridge.csproj -c Release --self-contained false -o dist/StadiaBridge
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación.' }
Copy-Item -LiteralPath README.md -Destination dist/StadiaBridge/README.md
Copy-Item -LiteralPath docs/INVESTIGACION.md -Destination dist/StadiaBridge/INVESTIGACION.md
Copy-Item -LiteralPath docs/VISOR-REA.md -Destination dist/StadiaBridge/VISOR-REA.md
Write-Host "Ejecutable listo: $PSScriptRoot\dist\StadiaBridge\StadiaBridge.exe"
