# Guía de Despliegue del Servidor (Railway)

El servidor central (`PosServer`) está construido en .NET 8 y está preparado para ser alojado en plataformas en la nube modernas como [Railway](https://railway.app/). 

## 1. Requisitos Previos

- Una cuenta en [Railway.app](https://railway.app/).
- Una cuenta en [GitHub](https://github.com/) (para vincular tu repositorio).
- Un repositorio en GitHub con el código de tu proyecto (asegúrate de subir la carpeta `PosServer`).
- Una base de datos PostgreSQL (puede ser la base de datos nativa de Railway, o una en Supabase como en tu configuración actual).

## 2. Preparar el Repositorio

Ya hemos configurado el código por ti:
1. **Dockerfile**: Dentro de la carpeta `PosServer` hay un archivo `Dockerfile` que Railway usará automáticamente para compilar y ejecutar tu aplicación.
2. **Puerto Dinámico**: El archivo `Program.cs` está configurado para escuchar en el puerto que Railway asigne dinámicamente (`PORT`).

Solo necesitas hacer `commit` y `push` de tu código a tu repositorio de GitHub.

## 3. Desplegar en Railway

1. Entra a tu panel de Railway y haz clic en **New Project**.
2. Selecciona **Deploy from GitHub repo**.
3. Selecciona tu repositorio.
4. **¡Importante!** Como tu repositorio tiene tanto la aplicación de escritorio (`PosCore`) como el servidor (`PosServer`), debes decirle a Railway dónde está el servidor:
   - Ve a la configuración de tu servicio en Railway (Settings).
   - En la sección **Root Directory**, escribe `/PosServer`.
   - Railway detectará automáticamente el `Dockerfile` y comenzará a compilar.

## 4. Configurar Variables de Entorno en Railway

Tu aplicación necesita conectarse a la base de datos (por ejemplo, Supabase) y usar una clave secreta para los tokens (JWT). En lugar de escribir tus contraseñas en el código, Railway te permite usar Variables de Entorno.

En el panel de tu servicio en Railway, ve a la pestaña **Variables** y agrega las siguientes variables:

### Conexión a la Base de Datos
En .NET, puedes sobrescribir cualquier valor de `appsettings.json` usando variables de entorno con doble guion bajo (`__`).

- **Variable:** `ConnectionStrings__DefaultConnection`
- **Valor:** `Host=aws-1-us-east-2.pooler.supabase.com;Database=postgres;Username=postgres.aklyqyrfhkimxxgbdhqy;Password=TU_PASSWORD;SSL Mode=Require;Trust Server Certificate=true`
*(Asegúrate de poner tu contraseña real aquí)*

### Claves de Seguridad (Opcional, pero recomendado)
Si quieres cambiar la clave secreta en producción:
- **Variable:** `Jwt__Key`
- **Valor:** *(Escribe una clave larga y segura, mínimo 32 caracteres)*

## 5. ¡Listo!

Una vez agregadas las variables, Railway reiniciará tu aplicación automáticamente.
En la pestaña **Settings**, bajo la sección **Networking**, haz clic en **Generate Domain** para obtener una URL pública (ejemplo: `pos-server-production.up.railway.app`).

### 6. Conectar el POS al nuevo Servidor
Copia la URL pública generada por Railway y ve al archivo `appsettings.json` de la **aplicación de escritorio** (`PosCore`). Actualiza la URL base:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://TU-URL-DE-RAILWAY.up.railway.app/api/"
  }
}
```

¡Ahora tu POS de escritorio se sincronizará con tu servidor alojado en la nube!
