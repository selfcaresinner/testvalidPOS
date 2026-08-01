# Super POS Express

Super POS Express es un sistema de Punto de Venta (POS) moderno, inteligente y diseñado con una arquitectura **Offline-First**. Esto significa que las sucursales pueden seguir operando y realizando ventas incluso si pierden su conexión a internet, sincronizando los datos automáticamente una vez que la conexión se restablece.

## 🏗️ Arquitectura del Proyecto
La solución está dividida en tres proyectos principales:
1. **`PosCore`**: La aplicación de escritorio (cliente). Construida con WPF y .NET 8. Utiliza una base de datos SQLite local para garantizar el funcionamiento offline.
2. **`PosServer`**: El servidor central (API). Construido con ASP.NET Core 8. Se encarga de centralizar las ventas, manejar el catálogo global de productos y la autenticación. Utiliza PostgreSQL como base de datos principal.
3. **`PosCore.Tests`**: Proyecto de pruebas unitarias para asegurar la calidad y estabilidad de la lógica de negocio y la sincronización.

## 🚀 Características Principales y Capacidades
El sistema cuenta actualmente con las siguientes funcionalidades operativas:

*   **Punto de Venta (Ventas y Cobro)**: Interfaz intuitiva para añadir productos al carrito, modificar cantidades y completar ventas rápidamente.
*   **Módulo de Pagos Avanzado**: Ventana de cobro con teclado numérico táctil (Numpad), cálculo de cambio automático, cobro exacto y simulación de programa de lealtad (búsqueda de clientes por teléfono).
*   **Suspensión y Retoma de Órdenes**: Capacidad de guardar ventas en proceso (en espera) y retomarlas más tarde, ideal para no bloquear la caja.
*   **Descuentos y Modificadores**: Permite agregar notas personalizadas por producto (ej. "sin cebolla") y aplicar descuentos directos en pesos o porcentajes al subtotal.
*   **Autorización de Gerente**: Ventanas de control de acceso por PIN para operaciones sensibles y registro de motivos en caso de anulaciones y devoluciones.
*   **Impresión de Tickets Directa (Térmica)**: Impresión nativa mediante comandos RAW (ESC/POS y `winspool.drv`) hacia impresoras térmicas en entornos Windows. Incluye reimpresión de tickets.
*   **Feedback de Red y Hardware**: Indicadores visuales en tiempo real del estado de conexión (Online/Offline) y banners de advertencia sobre problemas con la impresora.
*   **Gestión de Inventario**: Control de existencias, umbrales de stock mínimo (`MinStockThreshold`) y alertas visuales.
*   **Arqueo y Turnos**: Apertura y cierre de turnos de caja, registro de saldos iniciales, cálculo de dinero esperado contra el real e historial de diferencias.
*   **Devoluciones**: Proceso de devoluciones de órdenes previas, regresando la mercancía al inventario y generando "Notas de Crédito" impresas directamente en la ticketera.
*   **Reportes y Cierres**: Generación de reportes de ventas, listado de órdenes y cierres diarios.
*   **Módulo de Logs**: Visor integrado de registros (logs) del sistema, que permite auditar errores, sincronizaciones, eventos de red o problemas con la impresora.
*   **Gestión de Mesas**: Módulo habilitable mediante configuración para rubros gastronómicos (habilitable en `appsettings.json`).
*   **Offline-First y Tolerancia a Fallos**: Operación ininterrumpida sin internet. Las transacciones se guardan en un sistema *Outbox* con SQLite local, para ser sincronizadas posteriormente (`SyncService`) de manera transparente.
*   **Marca Blanca (White-Label)**: Fácil personalización de colores, logos y nombre de empresa a través del archivo de configuración local.
*   **Gestión Multi-Tenant**: Soporte para múltiples sucursales con identificadores únicos.

## 🛠️ Requisitos Previos
Para compilar y ejecutar este proyecto en tu entorno de desarrollo, necesitas:
*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [Visual Studio 2022](https://visualstudio.microsoft.com/) o IDE compatible (como Rider o VS Code con la extensión de C#).
*   PostgreSQL (para el servidor) o una cuenta en Supabase.
*   Sistema operativo **Windows** (requerido para impresión nativa hacia ticketeras `winspool.drv`).

## 📚 Documentación
Para obtener instrucciones detalladas sobre instalación, despliegue y uso, consulta la documentación incluida en la carpeta `PosCore/Docs`:
*   [Manual de Usuario](./PosCore/Docs/User_Manual.md)
*   [Guía de Instalación](./PosCore/Docs/Installation_Guide.md)
*   [Guía de Personalización](./PosCore/Docs/Customization_Guide.md)
*   [Guía de Despliegue del Servidor](./PosCore/Docs/Deployment_Guide.md)
*   [Guía de Pruebas y CI/CD](./INSTALLER_TESTING_GUIDE.md)

## 🚀 Novedades Recientes
*   **Compatibilidad Supabase**: Se ha solucionado el error 500 en Login y sincronización, corrigiendo el guardado de fechas (UTC/Local) en PostgreSQL (`Npgsql.EnableLegacyTimestampBehavior`) y compatibilidad con Transaction Pooler, añadiendo compatibilidad nativa con Transaction Pooler (pgbouncer) de Supabase en `PosServer` desactivando Prepared Statements cuando se detecta el pooler.
*   **Sincronización Inteligente**: Corrección de bugs en Backoff y sincronización de OutboxMessages.

## 💻 Cómo Ejecutar en Desarrollo

### 1. Levantar el Servidor (PosServer)
1. Navega a la carpeta del servidor: `cd PosServer`
2. Configura tu cadena de conexión a PostgreSQL en `appsettings.json` o `appsettings.Development.json`.
3. Aplica las migraciones: `dotnet ef database update`.
4. Ejecuta el servidor: `dotnet run` (Ej. `http://localhost:5000`).

### 2. Levantar el Cliente de Escritorio (PosCore)
1. Navega a la carpeta del cliente: `cd PosCore`
2. Configura los parámetros en `appsettings.json`:
   - `ApiSettings:BaseUrl`: URL de tu servidor local.
   - `Printer:PortName`: Nombre de la impresora (Ej. `POS-80` o `COM1`).
3. Ejecuta la aplicación: `dotnet run`
   - Se abrirá la interfaz gráfica (WPF). Al iniciar sesión, se creará la BD local y se descargarán los catálogos.

## 📦 Empaquetado y Producción
El proyecto incluye un script en PowerShell (`PosCore/build_and_package.ps1`) y un flujo de GitHub Actions (`.github/workflows/build-release.yml`) para compilar un único ejecutable (Self-Contained) y generar un instalador `.exe` utilizando Inno Setup y Squirrel.
Consulta la [Guía de Pruebas e Instalador](./INSTALLER_TESTING_GUIDE.md) para más detalles.
