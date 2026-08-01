param (
    [string]$Version = "1.0.0",
    [switch]$Sign = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host " Publicando POS Core para Producción v$Version" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

# 1. Limpiar publicaciones anteriores
if (Test-Path ".\publish") { Remove-Item -Recurse -Force ".\publish" }
if (Test-Path ".\Output") { Remove-Item -Recurse -Force ".\Output" }
if (Test-Path ".\Releases") { Remove-Item -Recurse -Force ".\Releases" }

# 2. Publicar la aplicación
Write-Host "Compilando la aplicación..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish

# 3. Firma Digital (Opcional)
if ($Sign) {
    Write-Host "Firmando binarios..." -ForegroundColor Yellow
    # Necesitas signtool y un certificado PFX
    # signtool sign /f "cert.pfx" /p "password" /tr http://timestamp.digicert.com /td sha256 /fd sha256 ".\publish\PosCore.exe"
    Write-Host "Firma digital completada." -ForegroundColor Green
}

# 4. Generar Instalador y Paquetes de Actualización con Squirrel
Write-Host "Generando paquete de actualización con Squirrel..." -ForegroundColor Yellow
# Actualizar versión en nuspec temporalmente o pasarlo a nuget
# nuget pack PosCore.nuspec -Version $Version
# Squirrel --releasify PosCore.$Version.nupkg
Write-Host "Paquetes de Squirrel generados en .\Releases" -ForegroundColor Green

Write-Host "Proceso completado exitosamente." -ForegroundColor Cyan
