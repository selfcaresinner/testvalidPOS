# Manual de Usuario - Super POS Express

Bienvenido al sistema de Punto de Venta (POS) Super POS Express. Este manual te guiará en el uso diario de la aplicación, cubriendo desde el inicio de sesión hasta la generación de reportes y cierres de caja.

## 1. Inicio de Sesión

Al abrir la aplicación, se mostrará la pantalla de inicio de sesión.
1. Ingresa tu **Usuario** y **Contraseña**.
2. Haz clic en **Ingresar**.
3. *Nota: La aplicación requiere conexión a internet para validar tus credenciales contra el servidor central por primera vez. Una vez autenticado, podrás seguir operando aunque se pierda la conexión.*

## 2. Realizar una Venta (Pantalla Principal)

La pantalla principal es tu área de trabajo para atender clientes y registrar ventas de forma rápida.

### Agregar productos al carrito
- Puedes hacer clic en los productos que aparecen en la cuadrícula del catálogo central.
- El panel de la derecha mostrará el **Resumen de la Orden (Carrito)**, indicando la cantidad, precio unitario y total por cada producto.

### Cobrar y Finalizar Venta
1. Revisa que el carrito contenga los productos correctos.
2. Verifica el monto **Total** en la parte inferior derecha.
3. Haz clic en el botón verde **Cobrar**.
4. La venta se registrará en el sistema. El inventario se actualizará automáticamente y la orden se preparará para enviarse al servidor central (en segundo plano).
5. El carrito se vaciará, dejándolo listo para el siguiente cliente.

## 3. Control de Inventario (Gestión de Productos)

El módulo de inventario te permite gestionar los productos de tu sucursal. Para acceder, haz clic en el botón **Control de Inventario** en la parte inferior izquierda de la pantalla principal.

### Ver Productos
La tabla mostrará todos los productos disponibles, incluyendo su código de barras, nombre, precio y cantidad en stock.

### Agregar un Nuevo Producto
1. Haz clic en el botón **+ Nuevo Producto**.
2. Completa los campos en el formulario de la derecha: Código, Nombre, Precio y Stock Inicial.
3. Haz clic en **Guardar**.

### Editar un Producto
1. Selecciona un producto de la tabla (se resaltará).
2. Haz clic en **Editar**.
3. Modifica los datos necesarios en el formulario.
4. Haz clic en **Guardar**.

### Eliminar un Producto
1. Selecciona el producto en la tabla.
2. Haz clic en **Eliminar**.
3. Confirma la acción en el cuadro de diálogo que aparecerá.

## 4. Reportes y Cierre de Caja

Para visualizar el rendimiento de tu negocio, haz clic en el botón **Reportes y Cierre** en la pantalla principal.

En esta ventana encontrarás un panel con tres pestañas principales:

1. **Ventas por Día**: Muestra un listado de los días de operación, indicando cuántas órdenes se realizaron y los ingresos totales de cada día.
2. **Productos Más Vendidos**: Un ranking de los 10 productos que más rotación tienen en tu negocio.
3. **Alerta: Bajo Stock**: Muestra los productos que tienen 10 unidades o menos en inventario, para que sepas qué debes reabastecer.

### Imprimir Cierre de Caja (PDF)
En la parte superior de la ventana de Reportes, verás un resumen de las "Ventas de Hoy".
Para guardar este reporte y hacer tu corte de caja:
1. Haz clic en el botón rojo **Imprimir Cierre (PDF)**.
2. Se generará y guardará un archivo PDF con la fecha y hora actuales en la misma carpeta donde está instalado el sistema.
3. Este documento incluye el total de ventas del día y la lista de productos con bajo stock.
