# Catálogo de Comercios y Tenant IDs

El sistema de Punto de Venta (POS) está diseñado para ser multi-inquilino (Multi-Tenant). Esto significa que la misma base de datos central en la nube puede servir a múltiples negocios diferentes, manteniendo sus datos aislados mediante el `TenantId`.

Para lograr compatibilidad universal sin tener que modificar la base de datos para cada tipo de comercio, hemos implementado el campo `CustomAttributes` (formato JSON) en las entidades principales (`Products`, `Orders`, `OrderItem`). Esto permite guardar propiedades específicas (tallas, colores, gestión de mesas, mecánicos asignados, etc.) de forma flexible.

A continuación, se presenta el catálogo de los tipos de comercios soportados, sus Tenant IDs de ejemplo, y las funcionalidades que utilizan:

## 1. Comercio al por Menor (Retail)
Son negocios enfocados en la venta directa de productos físicos con inventario, códigos de barras y variantes (talla, color, etc.).

| Tipo de Comercio | Tenant ID (Ejemplo) | Descripción y Funcionalidades (CustomAttributes) |
| :--- | :--- | :--- |
| **Supermercados / Abarrotes** | `RETAIL_SUPER_001` | **Funcionalidad:** Venta rápida por código de barras, control estricto de stock, productos a granel (pesables).<br>**JSON:** `{"pesable": true, "unidad": "kg"}` |
| **Ropa y Calzado** | `RETAIL_FASHION_001` | **Funcionalidad:** Gestión de variantes de un mismo producto.<br>**JSON:** `{"talla": "M", "color": "Rojo", "marca": "Nike"}` |
| **Farmacias** | `RETAIL_PHARMA_001` | **Funcionalidad:** Control de lotes, fechas de caducidad, y requerimiento de receta médica.<br>**JSON:** `{"lote": "L-12345", "caducidad": "2027-12-01", "receta_requerida": true}` |
| **Ferreterías** | `RETAIL_HARDWARE_001` | **Funcionalidad:** Venta por piezas, metros, litros o cajas. Control de inventario minucioso.<br>**JSON:** `{"unidad_medida": "metros", "ubicacion_pasillo": "A3"}` |
| **Electrónica** | `RETAIL_TECH_001` | **Funcionalidad:** Registro de números de serie para garantías.<br>**JSON:** `{"numero_serie": "SN-987654321", "meses_garantia": 12}` |
| **Jugueterías / Librerías** | `RETAIL_BOOKS_001` | **Funcionalidad:** Clasificación por editorial, autor, o edades recomendadas.<br>**JSON:** `{"autor": "Stephen King", "isbn": "978-3-16-148410-0", "edad_minima": 12}` |

## 2. Alimentos y Bebidas (Hospitality)
Negocios que requieren gestión de mesas, comandas a cocina, modificadores de platillos y división de cuentas.

| Tipo de Comercio | Tenant ID (Ejemplo) | Descripción y Funcionalidades (CustomAttributes) |
| :--- | :--- | :--- |
| **Restaurantes y Bares** | `HOSP_REST_001` | **Funcionalidad:** Gestión de mesas, meseros, propinas, modificadores de comida.<br>**JSON (Order):** `{"mesa": 5, "mesero": "Juan", "personas": 4}`<br>**JSON (OrderItem):** `{"termino": "Medio", "sin_cebolla": true}` |
| **Cafeterías / Panaderías** | `HOSP_CAFE_001` | **Funcionalidad:** Venta rápida, combos, extras, leche alternativa.<br>**JSON (OrderItem):** `{"tipo_leche": "Almendra", "extra_shot": true}` |
| **Fast Food / Food Trucks** | `HOSP_FASTFOOD_001` | **Funcionalidad:** Identificador de turno o número de orden para el cliente, combos.<br>**JSON (Order):** `{"numero_turno": 42, "para_llevar": true}` |

## 3. Negocios de Servicios
Locales donde se cobra por tiempo, agendas de citas o servicios personalizados.

| Tipo de Comercio | Tenant ID (Ejemplo) | Descripción y Funcionalidades (CustomAttributes) |
| :--- | :--- | :--- |
| **Salones de Belleza / Spa** | `SERV_BEAUTY_001` | **Funcionalidad:** Asignación de servicio a un profesional (estilista), pago de comisiones.<br>**JSON (OrderItem):** `{"estilista_id": "EMP_005", "duracion_minutos": 45}` |
| **Talleres Mecánicos** | `SERV_AUTO_001` | **Funcionalidad:** Registro del vehículo, placas, kilometraje, y mecánico asignado.<br>**JSON (Order):** `{"placas": "ABC-123", "vehiculo": "Honda Civic 2018", "km": 45000}` |
| **Gimnasios** | `SERV_GYM_001` | **Funcionalidad:** Membresías, fechas de corte, control de acceso.<br>**JSON (Product):** `{"tipo_membresia": "Mensual", "acceso_24_7": true}` |
| **Consultorios (Médico/Vet)** | `SERV_MED_001` | **Funcionalidad:** Expediente del paciente/mascota, próximo servicio.<br>**JSON (Order):** `{"paciente": "Firulais", "especie": "Perro", "peso_kg": 15.5}` |
| **Lavanderías / Tintorerías** | `SERV_LNDRY_001` | **Funcionalidad:** Ticket de recolección, cobro por peso, instrucciones de lavado.<br>**JSON (Order):** `{"fecha_entrega": "2026-08-01T15:00:00Z", "instrucciones": "Lavado en seco"}` |

---

### ¿Cómo funciona a nivel base de datos?
Al usar Entity Framework Core (PostgreSQL en la nube / SQLite local), las tablas `Products`, `Orders` y `OrderItem` tienen una columna `CustomAttributes` que almacena un string JSON. 

En lugar de crear 50 columnas diferentes en la base de datos para `Talla`, `Color`, `Mesa`, `Kilometraje`, `Paciente` (lo cual sería ineficiente y muy difícil de mantener), el sistema serializa las necesidades específicas de cada negocio en este campo JSON. Cuando el cliente del POS sincroniza con el servidor, la nube simplemente guarda el JSON, haciéndola **100% agnóstica y compatible con cualquier tipo de comercio de la industria**.
