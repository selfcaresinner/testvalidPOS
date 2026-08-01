# Guía de Pruebas: Instalador y Actualizaciones Automáticas

## Estado Actual de los Componentes

*   ✅ **Instalador y Actualizador (Squirrel)**: Configurado. Squirrel se encarga de generar el `Setup.exe` y gestionar las actualizaciones silenciosas. No usamos Inno Setup porque interfiere con los permisos de actualización de Squirrel.
*   ✅ **Actualización con Squirrel**: Configurado. Se ha agregado `PosCore.nuspec` para crear el paquete NuGet y usar `--releasify`.
*   ✅ **Firma Digital (SmartScreen)**: Integrada en el pipeline de GitHub Actions y en el script de PowerShell (comentado para que el usuario añada su certificado `.pfx`).
*   ✅ **Automatización (CI/CD)**: Se agregó un flujo de GitHub Actions (`.github/workflows/build-release.yml`) que compila, empaqueta y genera los binarios listos en un entorno Windows limpio.

## Paso 1: Generación de Binarios de Producción

Hemos creado un script automatizado en PowerShell (`PosCore/build_and_package.ps1`).

**Para ejecutarlo localmente (en Windows):**
1. Abre PowerShell como administrador.
2. Navega a la carpeta `PosCore`.
3. Ejecuta el comando para publicar la app:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish
   ```
*(La carpeta `publish` contendrá el ejecutable autónomo y sus recursos).*

## Paso 2: Generar el Instalador y Paquetes de Actualización con Squirrel

Squirrel se encarga de las actualizaciones silenciosas. Utiliza un feed web para saber si hay nuevas versiones.

1. Instala las herramientas: Descarga `nuget.exe` y configúralo en tu PATH. Instala Squirrel globalmente con `dotnet tool install -g squirrel.windows`.
2. Dentro de `PosCore`, crea el paquete base de NuGet:
   ```cmd
   nuget pack PosCore.nuspec -Version 1.0.0
   ```
3. Genera los archivos de actualización (Releasify):
   ```cmd
   Squirrel --releasify PosCore.1.0.0.nupkg
   ```
4. Se creará una carpeta `Releases`. Copia todo el contenido de esta carpeta a tu servidor web configurado (por ejemplo, `https://api.tu-pos-central.com/releases`).

## Paso 4: Prueba en Máquina Limpia (VM)

Para validar que las librerías nativas o el SDK no son una dependencia oculta:

1. Levanta una VM con Windows 10/11 usando Hyper-V o VirtualBox. **No le instales el .NET SDK**.
2. Copia el archivo `Setup.exe` (generado en la carpeta `Releases`) a la VM y ejecútalo.
3. Abre el POS. Como compilamos con `--self-contained true`, **debe ejecutarse sin pedir instalar .NET**.
4. Para probar la actualización: Sube una versión `1.0.1` a tu servidor. Abre la app en la VM; el servicio `SyncService` (o el `CheckForUpdatesAsync`) descargará la versión silenciosamente en segundo plano. Al reiniciar, estará actualizado.

## Paso 5: Evitar SmartScreen (Firma Digital)

El filtro SmartScreen de Windows bloquea instaladores desconocidos. Para solucionarlo:
1. Adquiere un Certificado de Firma de Código (Code Signing Certificate) estándar o EV.
2. Utiliza `signtool` (incluido en Windows SDK) para firmar tanto el `PosCore.exe` como el instalador final `.exe`.
3. En el archivo `build_and_package.ps1` y en `.github/workflows/build-release.yml` he dejado los comandos comentados para que inyectes tu certificado PFX.
