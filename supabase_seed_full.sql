-- =========================================================================
-- SUPER POS - CLOUD DATABASE SCHEMA (SUPABASE / POSTGRESQL)
-- Includes Multi-Tenant Support & Example Data for Catalog Businesses
-- =========================================================================

-- Enable UUID extension if not already
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. Tables Creation

CREATE TABLE IF NOT EXISTS "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" text NOT NULL,
    "Pin" text NOT NULL,
    "Role" text NOT NULL DEFAULT 'Cajero',
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
    "TenantId" text NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username");

CREATE TABLE IF NOT EXISTS "Products" (
    "Id" SERIAL PRIMARY KEY,
    "Name" text NOT NULL,
    "Barcode" text NOT NULL,
    "Price" numeric NOT NULL,
    "StockQuantity" integer NOT NULL DEFAULT 0,
    "MinStockThreshold" integer NOT NULL DEFAULT 10,
    "Category" text NOT NULL DEFAULT 'General',
    "CustomAttributes" jsonb NOT NULL DEFAULT '{}'::jsonb,
    "LastUpdated" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
    "TenantId" text NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Products_TenantId_Barcode" ON "Products" ("TenantId", "Barcode");

CREATE TABLE IF NOT EXISTS "Orders" (
    "Id" SERIAL PRIMARY KEY,
    "OrderDate" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
    "CustomerName" text DEFAULT '',
    "SubTotal" numeric NOT NULL DEFAULT 0,
    "TaxAmount" numeric NOT NULL DEFAULT 0,
    "TotalAmount" numeric NOT NULL,
    "IsSynced" boolean NOT NULL DEFAULT false,
    "IsReturned" boolean NOT NULL DEFAULT false,
    "ReturnReason" text DEFAULT '',
    "AuthorizedBy" text DEFAULT '',
    "PaymentDetails" text DEFAULT '',
    "CustomAttributes" jsonb NOT NULL DEFAULT '{}'::jsonb,
    "LastUpdated" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
    "TenantId" text NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_Orders_OrderDate" ON "Orders" ("OrderDate");

CREATE TABLE IF NOT EXISTS "OrderItems" (
    "Id" SERIAL PRIMARY KEY,
    "OrderId" integer NOT NULL REFERENCES "Orders"("Id") ON DELETE CASCADE,
    "ProductId" integer NOT NULL REFERENCES "Products"("Id") ON DELETE CASCADE,
    "ProductBarcode" text DEFAULT '',
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric NOT NULL,
    "Discount" numeric NOT NULL DEFAULT 0,
    "Notes" text DEFAULT '',
    "CustomAttributes" jsonb NOT NULL DEFAULT '{}'::jsonb,
    "LastUpdated" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
    "TenantId" text NOT NULL
);

CREATE TABLE IF NOT EXISTS "CashRegisterShifts" (
    "Id" SERIAL PRIMARY KEY,
    "OpenedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
    "ClosedAt" timestamp with time zone,
    "OpenedBy" text NOT NULL,
    "ClosedBy" text,
    "StartingCash" numeric NOT NULL,
    "ExpectedEndingCash" numeric,
    "ActualEndingCash" numeric,
    "Difference" numeric,
    "IsClosed" boolean NOT NULL DEFAULT false,
    "LastUpdated" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
    "TenantId" text NOT NULL
);

CREATE TABLE IF NOT EXISTS "CashMovements" (
    "Id" SERIAL PRIMARY KEY,
    "ShiftId" integer NOT NULL REFERENCES "CashRegisterShifts"("Id") ON DELETE CASCADE,
    "Type" text NOT NULL,
    "Amount" numeric NOT NULL,
    "Reason" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
    "TenantId" text NOT NULL
);

CREATE TABLE IF NOT EXISTS "OutboxMessages" (
    "Id" SERIAL PRIMARY KEY,
    "EventType" text NOT NULL,
    "Payload" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
    "ProcessedAt" timestamp with time zone,
    "RetryCount" integer NOT NULL DEFAULT 0,
    "TenantId" text NOT NULL
);

CREATE TABLE IF NOT EXISTS "ProductModifiers" (
    "Id" SERIAL PRIMARY KEY,
    "Name" text NOT NULL,
    "Description" text,
    "IsRequired" boolean NOT NULL DEFAULT false,
    "MinSelections" integer NOT NULL DEFAULT 0,
    "MaxSelections" integer NOT NULL DEFAULT 1,
    "LastUpdated" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now()),
    "TenantId" text NOT NULL
);

CREATE TABLE IF NOT EXISTS "ModifierOptions" (
    "Id" SERIAL PRIMARY KEY,
    "ProductModifierId" integer NOT NULL REFERENCES "ProductModifiers"("Id") ON DELETE CASCADE,
    "Name" text NOT NULL,
    "PriceAdjustment" numeric NOT NULL DEFAULT 0,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "TenantId" text NOT NULL
);

CREATE TABLE IF NOT EXISTS "ProductModifierLinks" (
    "Id" SERIAL PRIMARY KEY,
    "ProductId" integer NOT NULL REFERENCES "Products"("Id") ON DELETE CASCADE,
    "ProductModifierId" integer NOT NULL REFERENCES "ProductModifiers"("Id") ON DELETE CASCADE,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "TenantId" text NOT NULL
);

CREATE TABLE IF NOT EXISTS "Licenses" (
    "Id" SERIAL PRIMARY KEY,
    "LicenseKey" text NOT NULL,
    "TenantId" text NOT NULL,
    "Description" text,
    "IsActive" boolean NOT NULL DEFAULT true,
    "MaxTerminals" integer NOT NULL DEFAULT 1,
    "ValidUntil" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT timezone('utc'::text, now())
);


-- =========================================================================
-- 2. SEED DATA (Tenants from Catalog)
-- =========================================================================

-- Insert Admins and Employees for each Tenant
INSERT INTO "Users" ("Username", "Pin", "Role", "TenantId") VALUES 
-- Super Admin
('admin', 'admin123', 'Admin', 'TENANT_001'),
('cajero', 'cajero123', 'Cajero', 'TENANT_001'),
-- 1. Retail
('admin_super', 'admin123', 'Admin', 'RETAIL_SUPER_001'),
('cajero_super', '1234', 'Cajero', 'RETAIL_SUPER_001'),
('admin_fashion', 'admin123', 'Admin', 'RETAIL_FASHION_001'),
('cajero_fashion', '1234', 'Cajero', 'RETAIL_FASHION_001'),
('admin_pharma', 'admin123', 'Admin', 'RETAIL_PHARMA_001'),
('cajero_pharma', '1234', 'Cajero', 'RETAIL_PHARMA_001'),
('admin_hardware', 'admin123', 'Admin', 'RETAIL_HARDWARE_001'),
('cajero_hardware', '1234', 'Cajero', 'RETAIL_HARDWARE_001'),
('admin_tech', 'admin123', 'Admin', 'RETAIL_TECH_001'),
('cajero_tech', '1234', 'Cajero', 'RETAIL_TECH_001'),
('admin_books', 'admin123', 'Admin', 'RETAIL_BOOKS_001'),
('cajero_books', '1234', 'Cajero', 'RETAIL_BOOKS_001'),
-- 2. Hospitality
('admin_rest', 'admin123', 'Admin', 'HOSP_REST_001'),
('mesero_juan', '1111', 'Mesero', 'HOSP_REST_001'),
('admin_cafe', 'admin123', 'Admin', 'HOSP_CAFE_001'),
('cajero_cafe', '1234', 'Cajero', 'HOSP_CAFE_001'),
('admin_fast', 'admin123', 'Admin', 'HOSP_FASTFOOD_001'),
('cajero_fast', '1234', 'Cajero', 'HOSP_FASTFOOD_001'),
-- 3. Services
('admin_salon', 'admin123', 'Admin', 'SERV_BEAUTY_001'),
('estilista_maria', '2222', 'Estilista', 'SERV_BEAUTY_001'),
('admin_auto', 'admin123', 'Admin', 'SERV_AUTO_001'),
('mecanico_pepe', '3333', 'Mecanico', 'SERV_AUTO_001'),
('admin_gym', 'admin123', 'Admin', 'SERV_GYM_001'),
('recepcion_gym', '1234', 'Recepcion', 'SERV_GYM_001'),
('admin_med', 'admin123', 'Admin', 'SERV_MED_001'),
('doctora_ana', '1234', 'Medico', 'SERV_MED_001'),
('admin_laundry', 'admin123', 'Admin', 'SERV_LNDRY_001'),
('cajero_laundry', '1234', 'Cajero', 'SERV_LNDRY_001')
ON CONFLICT DO NOTHING;

-- ================== 1. RETAIL ==================

-- Supermercado
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Manzana Red', 'PROD_MZN_001', 45.00, 100, 'Frutas y Verduras', '{"pesable": true, "unidad": "kg"}', 'RETAIL_SUPER_001'),
('Leche Entera 1L', '7501020304050', 25.50, 50, 'Lácteos', '{}', 'RETAIL_SUPER_001');

-- Moda
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Playera Básica Roja M', 'TSHIRT_R_M', 150.00, 20, 'Ropa', '{"talla": "M", "color": "Rojo", "marca": "Genérica"}', 'RETAIL_FASHION_001'),
('Tenis Running 27', 'SHOE_RUN_27', 850.00, 5, 'Calzado', '{"talla_mx": 27, "color": "Negro"}', 'RETAIL_FASHION_001');

-- Farmacia
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Paracetamol 500mg', 'PHARMA_001', 35.00, 200, 'Medicamentos', '{"lote": "L-12345", "caducidad": "2027-12-01", "receta_requerida": false}', 'RETAIL_PHARMA_001'),
('Antibiótico Amoxicilina', 'PHARMA_002', 150.00, 50, 'Medicamentos', '{"lote": "L-98765", "caducidad": "2026-10-01", "receta_requerida": true}', 'RETAIL_PHARMA_001');

-- Ferretería
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Cable Eléctrico Calibre 12', 'HARDW_001', 12.00, 500, 'Eléctrico', '{"unidad_medida": "metros", "ubicacion_pasillo": "A3"}', 'RETAIL_HARDWARE_001'),
('Martillo Truper', 'HARDW_002', 180.00, 30, 'Herramientas', '{"unidad_medida": "piezas", "ubicacion_pasillo": "B1"}', 'RETAIL_HARDWARE_001');

-- Electrónica
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Laptop Dell Inspiron', 'TECH_001', 15000.00, 10, 'Computación', '{"numero_serie": "SN-987654321", "meses_garantia": 12}', 'RETAIL_TECH_001'),
('Cable HDMI 2m', 'TECH_002', 150.00, 100, 'Accesorios', '{"numero_serie": "", "meses_garantia": 3}', 'RETAIL_TECH_001');

-- Jugueterías / Librerías
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('El Resplandor - Stephen King', 'BOOK_001', 350.00, 15, 'Libros', '{"autor": "Stephen King", "isbn": "978-3-16-148410-0", "edad_minima": 15}', 'RETAIL_BOOKS_001'),
('Lego Star Wars', 'TOY_001', 1200.00, 8, 'Juguetes', '{"marca": "LEGO", "edad_minima": 8}', 'RETAIL_BOOKS_001');


-- ================== 2. HOSPITALITY ==================

-- Restaurantes
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Hamburguesa Clásica', 'FOOD_HAMB_01', 120.00, 999, 'Alimentos', '{"preparacion": "Cocina"}', 'HOSP_REST_001'),
('Cerveza Artesanal', 'BEV_BEER_01', 65.00, 999, 'Bebidas', '{}', 'HOSP_REST_001');
INSERT INTO "ProductModifiers" ("Id", "Name", "Description", "IsRequired", "MinSelections", "MaxSelections", "TenantId") VALUES 
(1, 'Término de Carne', 'Elige cómo quieres la carne', true, 1, 1, 'HOSP_REST_001'),
(2, 'Extras', 'Agrega ingredientes', false, 0, 3, 'HOSP_REST_001');
INSERT INTO "ModifierOptions" ("ProductModifierId", "Name", "PriceAdjustment", "IsDefault", "SortOrder", "TenantId") VALUES 
(1, 'Rojo', 0, false, 1, 'HOSP_REST_001'),
(1, 'Medio', 0, true, 2, 'HOSP_REST_001'),
(1, 'Bien Cocido', 0, false, 3, 'HOSP_REST_001'),
(2, 'Tocino', 15.00, false, 1, 'HOSP_REST_001'),
(2, 'Queso Extra', 10.00, false, 2, 'HOSP_REST_001');
-- (Links para restaurante deben hacerse vía ID de producto insertado, lo manejamos simplificado)

-- Cafetería
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Latte Grande', 'CAFE_001', 65.00, 999, 'Bebidas Calientes', '{"preparacion": "Barra"}', 'HOSP_CAFE_001'),
('Pan de Elote', 'CAFE_002', 45.00, 20, 'Postres', '{}', 'HOSP_CAFE_001');

-- Fast Food
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Combo Hot Dog', 'FASTF_001', 95.00, 999, 'Combos', '{"incluye_bebida": true}', 'HOSP_FASTFOOD_001'),
('Papas Fritas Gde', 'FASTF_002', 40.00, 999, 'Complementos', '{}', 'HOSP_FASTFOOD_001');


-- ================== 3. SERVICES ==================

-- Salon / Spa
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Corte de Cabello Dama', 'SERV_CUT_W', 250.00, 999, 'Servicios', '{"requiere_cita": true, "duracion_minutos": 45}', 'SERV_BEAUTY_001'),
('Tinte Completo', 'SERV_COLOR', 800.00, 999, 'Servicios', '{"requiere_cita": true, "duracion_minutos": 120}', 'SERV_BEAUTY_001');

-- Talleres Mecánicos
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Cambio de Aceite', 'AUTO_001', 450.00, 999, 'Mantenimiento', '{"requiere_datos_vehiculo": true}', 'SERV_AUTO_001'),
('Alineación y Balanceo', 'AUTO_002', 600.00, 999, 'Mantenimiento', '{"requiere_datos_vehiculo": true}', 'SERV_AUTO_001');

-- Gimnasios
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Membresía Mensual', 'GYM_001', 500.00, 999, 'Membresías', '{"duracion_dias": 30, "acceso_24_7": false}', 'SERV_GYM_001'),
('Suplemento Proteína 2kg', 'GYM_002', 1200.00, 15, 'Productos', '{"sabor": "Chocolate"}', 'SERV_GYM_001');

-- Consultorios
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Consulta Médica General', 'MED_001', 600.00, 999, 'Consultas', '{"requiere_expediente": true}', 'SERV_MED_001'),
('Certificado Médico', 'MED_002', 200.00, 999, 'Documentos', '{}', 'SERV_MED_001');

-- Lavanderías
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "Category", "CustomAttributes", "TenantId") VALUES 
('Lavado por Kilo', 'LNDR_001', 25.00, 999, 'Lavado', '{"unidad": "kg", "requiere_fecha_entrega": true}', 'SERV_LNDRY_001'),
('Edredón King Size', 'LNDR_002', 150.00, 999, 'Lavado Especial', '{"unidad": "pieza", "requiere_fecha_entrega": true}', 'SERV_LNDRY_001');

