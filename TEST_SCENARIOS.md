# Pruebas de Sincronización en Escenarios Reales

## Estrategia de Resolución Implementada

Para resolver los conflictos en un entorno distribuido offline-first, hemos implementado las siguientes estrategias:

1. **Ventas y Stock (Operaciones Aditivas/Sustractivas)**: 
   - Cuando se realiza una venta offline, se guarda la orden en un `Outbox` local.
   - Al recuperar la conexión, el `SyncService` envía la orden al `OrdersController` del servidor.
   - El servidor **no sobrescribe** el stock absoluto, sino que **resta** la cantidad vendida del stock actual en la base de datos central.
   - Esto evita que dos terminales offline sobrescriban el stock del otro. Si la Terminal A vende 2 y la Terminal B vende 3, el servidor restará 5 en total.

2. **Edición de Productos (Last-Write-Wins)**:
   - Si se edita el nombre o precio de un producto, se envía al servidor con la fecha de modificación (`LastUpdated`).
   - El servidor compara el `LastUpdated` recibido con el que tiene almacenado. Si el local es más reciente, sobrescribe los datos en el servidor.
   - Cuando las terminales vuelven a hacer pull, reciben la versión ganadora.

## Cómo Simular los Escenarios

Dado que la aplicación es una app de escritorio (WPF), puedes simular los escenarios ejecutando dos instancias de la aplicación en la misma PC (abriendo el ejecutable dos veces desde la carpeta `bin/Debug/net8.0-windows`).

### Escenario 1: Venta Offline y Reconexión
1. Inicia el servidor backend (`PosServer`).
2. Inicia una instancia del cliente (`PosCore`).
3. Cierra o apaga el servidor backend (para simular caída de red).
4. En el cliente, agrega un producto al carrito y completa la venta.
   - *Resultado esperado:* La venta se completa localmente. El stock se reduce en la UI.
5. Vuelve a iniciar el servidor backend.
6. Espera unos 10 segundos (el intervalo del `SyncService`).
   - *Resultado esperado:* Verás en el archivo de log (en la carpeta `logs/` de la app cliente) y en la consola del servidor que la orden pendiente en el `Outbox` se sincronizó con éxito.

### Escenario 2: Dos Terminales Offline (Conflicto de Inventario)
1. Inicia el servidor backend.
2. Inicia **dos instancias** del cliente (Terminal A y Terminal B).
3. Apaga el servidor backend (ambas terminales quedan offline).
4. En la Terminal A, vende 2 unidades de "Café Americano".
5. En la Terminal B, vende 3 unidades del mismo "Café Americano".
6. Inicia el servidor backend.
7. Espera a que ambas terminales se sincronicen.
   - *Resultado esperado:* El servidor procesa ambas órdenes. Si el stock original era 100, la Terminal A envió -2 y la Terminal B envió -3. El stock central ahora es 95.
8. En la siguiente iteración de sincronización (o reiniciando las terminales), ambas descargarán el catálogo actualizado y mostrarán el stock correcto de 95.

### Escenario 3: Edición Concurrente
1. Con ambas terminales offline (servidor apagado).
2. Terminal A edita el nombre de "Croissant" a "Croissant de Mantequilla" y guarda.
3. Un minuto después, Terminal B edita el nombre a "Croissant Vegano" y guarda.
4. Enciende el servidor.
   - *Resultado esperado:* Ambas terminales enviarán sus cambios. El servidor aplicará el cambio de la Terminal B (por tener un `LastUpdated` más reciente). Al final, ambas terminales mostrarán "Croissant Vegano".

## Verificación en Logs
La aplicación cliente utiliza **Serilog** y guarda los logs en la carpeta `logs/pos-log-.txt`.
Puedes revisar este archivo para ver mensajes como:
- `Iniciando sincronización: X mensajes pendientes.`
- `Mensaje ID X (OrderCreated) sincronizado con éxito.`
- `Actualizando producto X con versión del servidor.`
- `Conflicto detectado: La versión local de X es más reciente.`
