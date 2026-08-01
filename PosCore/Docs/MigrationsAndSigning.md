# Guía de Migraciones y Firmado de Certificados

## 1. Migraciones de Entity Framework Core

El proyecto ahora utiliza migraciones de EF Core en lugar de `EnsureCreated()`.
Para crear la migración inicial o futuras migraciones, abre la terminal en la raíz de la solución y ejecuta:

1. Instalar las herramientas de EF Core globalmente (si no lo tienes):
   `dotnet tool install --global dotnet-ef`

2. Crear la migración inicial:
   `dotnet ef migrations add InitialCreate --project PosCore --startup-project PosCore`

3. Aplicar las migraciones a la base de datos (esto también lo hace `Database.Migrate()` en `App.xaml.cs` al iniciar):
   `dotnet ef database update --project PosCore --startup-project PosCore`

## 2. Firmar el instalador con signtool.exe (Squirrel / Inno Setup)

Para evitar advertencias de Windows SmartScreen y distribuir una aplicación confiable, necesitas un certificado de firma de código (PFX).

### Comando Básico para Firmar el .exe:
Usa `signtool.exe` (incluido en el SDK de Windows, usualmente en `C:\Program Files (x86)\Windows Kits\10\bin\...\x64\signtool.exe`).

```bat
signtool sign /f "ruta\a\tu_certificado.pfx" /p "tu_contraseña" /tr http://timestamp.digicert.com /td sha256 /fd sha256 "ruta\al\instalador\SuperPOS_Setup_v1.0.exe"
```

### Firmar Paquetes NuGet (Squirrel):
Squirrel extrae y actualiza binarios, por lo que debes firmar tanto los DLL/EXE dentro del paquete como el ejecutable `Setup.exe` generado por Squirrel.
Puedes pasar banderas a Squirrel para que firme automáticamente durante la generación:

```bat
Squirrel --releasify PosCore.1.0.0.nupkg --signWithParams="/a /f ruta\a\tu_certificado.pfx /p tu_contraseña /tr http://timestamp.digicert.com /td sha256 /fd sha256"
```

### Script de Firmado Recomendado (sign_installer.bat)
Crea un script `.bat` si tienes Inno Setup:
```bat
@echo off
set SIGNTOOL="C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"
set CERT_PATH=".\certs\CodeSigning.pfx"
set CERT_PASS="ContraseñaSuperSecreta"

echo Firmando PosCore.exe...
%SIGNTOOL% sign /f %CERT_PATH% /p %CERT_PASS% /tr http://timestamp.digicert.com /td sha256 /fd sha256 ".\publish\PosCore.exe"

echo Generando Instalador con Inno Setup...
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "installer.iss"

echo Firmando Instalador Final...
%SIGNTOOL% sign /f %CERT_PATH% /p %CERT_PASS% /tr http://timestamp.digicert.com /td sha256 /fd sha256 ".\Output\SuperPOS_Setup_v1.0.exe"

echo Completado!
pause
```
