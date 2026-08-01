# Sistema de Modificadores (Estilo Caffenio)

Para soportar la personalización profunda de productos (UX/UI tipo Caffenio), hemos implementado tres nuevas tablas en la base de datos (tanto en SQLite como en PostgreSQL):

## 1. ProductModifiers (El Grupo de Opciones)
Define una categoría de personalización.
*Ejemplo Caffenio:* "Tipo de Leche", "Temperatura", "Endulzante".
*Ejemplo Burger King:* "Ingredientes Extra", "Tamaño del Combo".
*Campos Clave:*
- `IsRequired`: Si es true, el cajero (o cliente) está obligado a elegir (ej: Tamaño del café).
- `MinSelections`: Mínimo de opciones a elegir.
- `MaxSelections`: Máximo de opciones (ej: máximo 2 tipos de leche).

## 2. ModifierOptions (Las Opciones Individuales)
Las opciones dentro del grupo, con su posible costo extra.
*Ejemplo Caffenio (Leche):* "Entera" (+$0), "Deslactosada" (+$5), "Almendra" (+$10).
*Campos Clave:*
- `PriceAdjustment`: Cuánto suma (o resta) al precio final del producto.
- `IsDefault`: Seleccionado por defecto para agilizar la UI.

## 3. ProductModifierLinks (La Relación Producto - Modificador)
Muchos productos pueden compartir los mismos grupos de modificadores. En vez de crear "Tipo de Leche" 100 veces para 100 cafés diferentes, se crea una vez y se vincula.
- Vincula el producto "Cappuccino" con el grupo "Tipo de Leche".
- Vincula el producto "Cappuccino" con el grupo "Temperatura".

## ¿Cómo se guarda en la Orden Final (El JSON)?
Cuando se completa la venta, en lugar de crear tablas gigantes para historial, el sistema utiliza el campo `CustomAttributes` en la tabla `OrderItem` (que ya configuramos con `jsonb` en Postgres).
Se guardará algo así:
```json
{
  "modificadores": [
    { "nombre": "Tipo de Leche", "seleccion": "Almendra", "costo_extra": 10.00 },
    { "nombre": "Temperatura", "seleccion": "Caliente", "costo_extra": 0.00 }
  ],
  "notas_cocina": "En vaso reutilizable"
}
```

Esta arquitectura es la misma que utilizan sistemas de clase mundial como Toast POS, Square o Aloha.
