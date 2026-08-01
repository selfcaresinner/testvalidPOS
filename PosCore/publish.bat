@echo off
echo ========================================================
echo Publicando POS Core para Producción (Single File)
echo ========================================================

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish

echo.
echo Publicación completada en la carpeta .\publish
echo.
echo Siguiente paso:
echo 1. Compilar installer.iss usando Inno Setup para generar el instalador (.exe)
echo 2. (Opcional) Si usas Squirrel, empaqueta con:
echo    nuget pack PosCore.nuspec
echo    Squirrel --releasify PosCore.1.0.0.nupkg
pause
