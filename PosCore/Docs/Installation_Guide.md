# Guía de Instalación y Configuración

El sistema Super POS Express está diseñado para ser una aplicación de escritorio rápida, moderna y capaz de funcionar sin conexión permanente (Offline-First). Esta guía explica cómo instalar y preparar el sistema para operar en una nueva sucursal o caja.

## 1. Requisitos del Sistema

- **Sistema Operativo**: Windows 10 (versión 1607 o superior) o Windows 11.
- **Arquitectura**: 64 bits (x64).
- **Dependencias**: Ninguna externa (el sistema es *Self-Contained*, por lo que NO necesitas instalar .NET Framework ni Java; todo viene incluido).
- **Almacenamiento**: Al menos 200 MB de espacio libre.

## 2. Descarga e Instalación

1. Descarga el instalador proporcionado por el administrador (`SuperPOS_Setup_v1.0.exe`).
2. Haz doble clic sobre el instalador.
3. *Aviso de SmartScreen*: Si Windows muestra un aviso de "Windows protegió su PC" (SmartScreen), haz clic en **Más información** y luego en **Ejecutar de todas formas**.
4. Sigue las instrucciones del asistente (generalmente solo necesitas hacer clic en "Siguiente" e "Instalar").
5. Por defecto, la aplicación creará un acceso directo en el Escritorio.

## 3. Configuración Inicial (Base de Datos Local)

El sistema utiliza SQLite como base de datos local rápida, por lo que no es necesario instalar SQL Server ni MySQL en la computadora de la caja.

1. La primera vez que ejecutas la aplicación, esta detectará si la base de datos local existe.
2. De no existir, el sistema aplicará automáticamente las migraciones (creará el archivo `pos_local.db` con todas las tablas necesarias) en la carpeta de instalación.
3. No necesitas realizar ninguna configuración manual de base de datos.

## 4. Conexión y Sincronización

Para que el sistema empiece a funcionar y descargue el catálogo de productos de tu cuenta, necesitas:

1. **Asegurar conexión a internet**: Solo es estrictamente necesaria en el primer inicio de sesión.
2. **Iniciar sesión**: Ingresa con las credenciales que te fueron asignadas.
3. **Descarga de catálogo**: Al iniciar sesión con éxito, el sistema se comunicará con el servidor central y descargará automáticamente todos los productos y configuraciones de tu sucursal.
4. Una vez terminado este proceso inicial (dura unos segundos), puedes desconectar el internet si lo deseas. El POS funcionará normalmente y enviará las ventas acumuladas automáticamente cuando recupere la conexión.

## 5. Actualizaciones Automáticas

El sistema cuenta con un gestor de actualizaciones silencioso (Squirrel).
- Cuando inicies la aplicación, esta buscará en segundo plano si hay una nueva versión en el servidor.
- Si la hay, la descargará y la preparará.
- La próxima vez que cierres y abras el POS, la nueva versión se aplicará automáticamente.
