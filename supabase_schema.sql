-- Eliminación de tablas existentes (Idempotencia)
DROP TABLE IF EXISTS "OrderItems" CASCADE;
DROP TABLE IF EXISTS "Orders" CASCADE;
DROP TABLE IF EXISTS "Products" CASCADE;
DROP TABLE IF EXISTS "CashMovements" CASCADE;
DROP TABLE IF EXISTS "CashRegisterShifts" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;
DROP TABLE IF EXISTS "OutboxMessages" CASCADE;

-- 1. Tabla de Usuarios (Users)
CREATE TABLE "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" VARCHAR(255) NOT NULL,
    "Pin" VARCHAR(50) NOT NULL,
    "Role" VARCHAR(50) NOT NULL DEFAULT 'Cashier',
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "TenantId" VARCHAR(255) NOT NULL
);

-- 2. Tabla de Productos (Products)
CREATE TABLE "Products" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL,
    "Barcode" VARCHAR(255) NOT NULL,
    "Price" DECIMAL(18, 2) NOT NULL,
    "StockQuantity" INT NOT NULL DEFAULT 0,
    "MinStockThreshold" INT NOT NULL DEFAULT 10,
    "Category" VARCHAR(255) NOT NULL DEFAULT 'General',
    "LastUpdated" TIMESTAMP NOT NULL DEFAULT NOW(),
    "TenantId" VARCHAR(255) NOT NULL,
    CONSTRAINT "UC_Product_Barcode_Tenant" UNIQUE ("Barcode", "TenantId")
);

-- 3. Tabla de Órdenes (Orders)
CREATE TABLE "Orders" (
    "Id" SERIAL PRIMARY KEY,
    "OrderDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "CustomerName" VARCHAR(255) NULL,
    "SubTotal" DECIMAL(18, 2) NOT NULL,
    "TaxAmount" DECIMAL(18, 2) NOT NULL,
    "TotalAmount" DECIMAL(18, 2) NOT NULL,
    "IsSynced" BOOLEAN NOT NULL DEFAULT FALSE,
    "LastUpdated" TIMESTAMP NOT NULL DEFAULT NOW(),
    "IsReturned" BOOLEAN NOT NULL DEFAULT FALSE,
    "ReturnReason" TEXT NULL,
    "AuthorizedBy" VARCHAR(255) NULL,
    "PaymentDetails" VARCHAR(255) NULL,
    "TenantId" VARCHAR(255) NOT NULL
);

-- 4. Tabla de Ítems de Órdenes (OrderItems)
CREATE TABLE "OrderItems" (
    "Id" SERIAL PRIMARY KEY,
    "OrderId" INT NOT NULL,
    "ProductId" INT NOT NULL,
    "ProductBarcode" VARCHAR(255) NOT NULL,
    "Quantity" INT NOT NULL,
    "UnitPrice" DECIMAL(18, 2) NOT NULL,
    "Discount" DECIMAL(18, 2) NOT NULL DEFAULT 0,
    "Notes" TEXT NULL,
    "LastUpdated" TIMESTAMP NOT NULL DEFAULT NOW(),
    "TenantId" VARCHAR(255) NOT NULL,
    CONSTRAINT "FK_OrderItems_Orders" FOREIGN KEY ("OrderId") REFERENCES "Orders"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_OrderItems_Products" FOREIGN KEY ("ProductId") REFERENCES "Products"("Id") ON DELETE CASCADE
);

-- 5. Tabla de Turnos de Caja (CashRegisterShifts)
CREATE TABLE "CashRegisterShifts" (
    "Id" SERIAL PRIMARY KEY,
    "OpenedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "ClosedAt" TIMESTAMP NULL,
    "OpenedBy" VARCHAR(255) NOT NULL,
    "ClosedBy" VARCHAR(255) NULL,
    "StartingCash" DECIMAL(18, 2) NOT NULL,
    "ExpectedEndingCash" DECIMAL(18, 2) NULL,
    "ActualEndingCash" DECIMAL(18, 2) NULL,
    "Difference" DECIMAL(18, 2) NULL,
    "IsClosed" BOOLEAN NOT NULL DEFAULT FALSE,
    "LastUpdated" TIMESTAMP NOT NULL DEFAULT NOW(),
    "TenantId" VARCHAR(255) NOT NULL
);

-- 6. Tabla de Movimientos de Caja (CashMovements)
CREATE TABLE "CashMovements" (
    "Id" SERIAL PRIMARY KEY,
    "ShiftId" INT NOT NULL,
    "Type" VARCHAR(50) NOT NULL, -- 'Entrada' o 'Salida'
    "Amount" DECIMAL(18, 2) NOT NULL,
    "Reason" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "TenantId" VARCHAR(255) NOT NULL,
    CONSTRAINT "FK_CashMovements_CashRegisterShifts" FOREIGN KEY ("ShiftId") REFERENCES "CashRegisterShifts"("Id") ON DELETE CASCADE
);

-- 7. Tabla de Sincronización (OutboxMessages)
CREATE TABLE "OutboxMessages" (
    "Id" SERIAL PRIMARY KEY,
    "EventType" VARCHAR(255) NOT NULL,
    "Payload" JSONB NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "ProcessedAt" TIMESTAMP NULL,
    "RetryCount" INT NOT NULL DEFAULT 0,
    "TenantId" VARCHAR(255) NOT NULL
);

-- Índices adicionales para rendimiento
CREATE INDEX "IX_Orders_OrderDate" ON "Orders"("OrderDate");
CREATE INDEX "IX_Orders_TenantId" ON "Orders"("TenantId");
CREATE INDEX "IX_OrderItems_OrderId" ON "OrderItems"("OrderId");
CREATE INDEX "IX_Products_TenantId" ON "Products"("TenantId");
CREATE INDEX "IX_OutboxMessages_Unprocessed" ON "OutboxMessages"("CreatedAt") WHERE "ProcessedAt" IS NULL;


-- DATOS DE PRUEBA (SEED DATA) --
-- Insertar Usuarios
INSERT INTO "Users" ("Username", "Pin", "Role", "IsActive", "TenantId") VALUES
('admin', '1234', 'Admin', TRUE, 'LOCAL'),
('cajero1', '1111', 'Cashier', TRUE, 'LOCAL'),
('cajero2', '2222', 'Cashier', TRUE, 'LOCAL');

-- Insertar Productos
INSERT INTO "Products" ("Name", "Barcode", "Price", "StockQuantity", "MinStockThreshold", "Category", "TenantId") VALUES
('Coca Cola 600ml', '7501055300075', 18.00, 50, 10, 'Bebidas', 'LOCAL'),
('Sabritas Sal 40g', '7501011111111', 15.00, 30, 10, 'Botanas', 'LOCAL'),
('Agua Ciel 1L', '7501022222222', 12.00, 40, 10, 'Bebidas', 'LOCAL'),
('Gansito Marinela', '7501033333333', 20.00, 25, 5, 'Postres', 'LOCAL'),
('Doritos Nacho 58g', '7501044444444', 16.00, 35, 10, 'Botanas', 'LOCAL');

-- Insertar un Turno de Caja (Cerrado ayer)
INSERT INTO "CashRegisterShifts" ("OpenedAt", "ClosedAt", "OpenedBy", "ClosedBy", "StartingCash", "ExpectedEndingCash", "ActualEndingCash", "Difference", "IsClosed", "TenantId") VALUES
(NOW() - INTERVAL '1 day', NOW() - INTERVAL '12 hours', 'admin', 'admin', 500.00, 850.00, 850.00, 0.00, TRUE, 'LOCAL');

-- Insertar un Turno de Caja (Abierto hoy)
INSERT INTO "CashRegisterShifts" ("OpenedAt", "OpenedBy", "StartingCash", "IsClosed", "TenantId") VALUES
(NOW() - INTERVAL '2 hours', 'cajero1', 500.00, FALSE, 'LOCAL');

-- Insertar Movimientos de caja (asociados al turno abierto ID=2)
INSERT INTO "CashMovements" ("ShiftId", "Type", "Amount", "Reason", "TenantId") VALUES
(2, 'Entrada', 100.00, 'Cambio adicional', 'LOCAL'),
(2, 'Salida', 50.00, 'Pago de garrafón de agua', 'LOCAL');

-- Insertar Órdenes
INSERT INTO "Orders" ("OrderDate", "CustomerName", "SubTotal", "TaxAmount", "TotalAmount", "IsSynced", "PaymentDetails", "TenantId") VALUES
(NOW() - INTERVAL '1 hour', 'Público General', 30.00, 4.80, 34.80, TRUE, 'Efectivo', 'LOCAL'),
(NOW() - INTERVAL '30 minutes', 'Público General', 20.00, 3.20, 23.20, TRUE, 'Tarjeta', 'LOCAL');

-- Insertar Ítems de Órdenes
-- Orden 1: 1 Coca Cola y 1 Agua Ciel
INSERT INTO "OrderItems" ("OrderId", "ProductId", "ProductBarcode", "Quantity", "UnitPrice", "Discount", "TenantId") VALUES
(1, 1, '7501055300075', 1, 18.00, 0.00, 'LOCAL'),
(1, 3, '7501022222222', 1, 12.00, 0.00, 'LOCAL');

-- Orden 2: 1 Gansito
INSERT INTO "OrderItems" ("OrderId", "ProductId", "ProductBarcode", "Quantity", "UnitPrice", "Discount", "TenantId") VALUES
(2, 4, '7501033333333', 1, 20.00, 0.00, 'LOCAL');

