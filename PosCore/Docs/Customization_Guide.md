# Guía de Personalización y Marca Blanca (White-Label)

Super POS Express permite personalizar su apariencia visual y su comportamiento sin necesidad de modificar el código fuente. Esto se logra editando el archivo de configuración `appsettings.json` que se encuentra en el directorio raíz de la instalación de la aplicación.

## Localizar el archivo de configuración

1. Haz clic derecho sobre el acceso directo de "Super POS Express" en tu escritorio y selecciona **Abrir la ubicación del archivo**.
2. En la carpeta que se abre, busca el archivo llamado `appsettings.json`.
3. Abre este archivo con un editor de texto plano (como el Bloc de notas).

## Estructura del Archivo

El archivo tiene formato JSON y se ve similar a esto:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.tu-pos-central.com/api/"
  },
  "DatabaseSettings": {
    "ConnectionString": "Data Source=pos_local.db"
  },
  "WhiteLabel": {
    "CompanyName": "Super POS Express",
    "PrimaryColor": "#6366f1",
    "LogoPath": "Assets/logo.png"
  },
  "Modules": {
    "EnableTableManagement": false,
    "EnableInventoryControl": true
  },
  "Tenant": {
    "CurrentTenantId": "TENANT_001"
  }
}
```

## 1. Configuración del Servidor (API)

Si cambias de servidor o instalas el backend (PosServer) en un dominio diferente, debes actualizar la URL base:

- Localiza la sección `"ApiSettings"`.
- Modifica `"BaseUrl"` con la URL de tu nuevo servidor.
- *Importante: Asegúrate de mantener `/api/` al final si tus rutas lo requieren, y verifica si usa `http://` o `https://`.*

## 2. Personalización Visual (Marca Blanca)

Puedes adaptar el POS a la identidad corporativa de tu cliente:

- **Nombre de la Empresa (`"CompanyName"`)**: Modifica el valor. Este nombre se mostrará en los tickets, reportes y barra de título de la aplicación.
- **Color Principal (`"PrimaryColor"`)**: Cambia este valor por el código Hexadecimal de la marca (ejemplo: `#FF0000` para rojo, `#4CAF50` para verde). Este color se aplicará a los botones, bordes y acentos visuales de la aplicación.
- **Logo de la Empresa (`"LogoPath"`)**: 
  1. Copia el logotipo de la empresa (preferiblemente un `.png` transparente) a la carpeta `Assets` dentro de la ubicación del POS.
  2. Modifica esta propiedad para apuntar al nuevo archivo (ejemplo: `"Assets/mi-logo.png"`).

## 3. Habilitar o Deshabilitar Módulos

Puedes encender o apagar partes del sistema según el plan que haya pagado el cliente:

- Localiza la sección `"Modules"`.
- **`"EnableTableManagement"`**: Cambia a `true` si es un restaurante y necesita control de mesas. (Si está en `false`, el botón se ocultará).
- **`"EnableInventoryControl"`**: Cambia a `true` si el cliente necesita el botón para entrar al CRUD de inventario.

## 4. Identificador del Cliente (Multi-Tenant)

Si tienes varios clientes conectados a un mismo servidor central, cada uno debe tener un ID único para no mezclar los datos.

- Modifica el valor `"CurrentTenantId"` (ej. `"SUCURSAL_NORTE_01"`).
- *Nota: Si la aplicación gestiona esto automáticamente al iniciar sesión, es posible que no necesites tocar este valor manualmente, pero está disponible para configuraciones offline iniciales.*

---
**IMPORTANTE:** Después de realizar y guardar los cambios en el archivo `appsettings.json`, **debes cerrar y volver a abrir la aplicación** para que los nuevos colores, módulos y configuraciones surtan efecto.
