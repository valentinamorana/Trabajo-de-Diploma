-- ============================================================
-- WardrobeFlow — 16. FIDELIZACIÓN: PAUSA, REFERIDOS Y CARGO POR DAÑO/PÉRDIDA
-- ------------------------------------------------------------
-- Tres funcionalidades nuevas del Bloque 1 (Fidelización), la última
-- cruza con Bloque 2 (Cobro):
--
--   • Pausa de suscripción (PdN5, Chain of Responsibility): Cliente.FechaPausaHasta.
--     Mientras esté vigente, bloquea pedidos nuevos SIN tocar FechaVencimiento
--     (al reanudar, la fecha de vencimiento queda como estaba — decisión de diseño).
--
--   • Programa de referidos (PdN1 → PdN6): Cliente.IdClienteReferente (quién lo
--     trajo), DescuentoProximoCobro (beneficio pendiente de aplicar) y
--     BeneficioReferidoOtorgado (evita otorgar el beneficio dos veces).
--
--   • Cargo por daño/pérdida (PdN4 → PdN6): Prenda.IdUltimoCliente (a diferencia
--     de IdClienteActual, NUNCA se limpia al devolver — así en el momento de
--     inspeccionarla en Mantenimiento todavía se sabe quién la tuvo) + tabla
--     CargoPrenda (el cargo en sí, pendiente hasta que se cobra junto con la
--     próxima renovación del cliente).
--
-- Ningún rol nuevo ni patente nueva: Pausa reutiliza mnuRenovacionSuscripcion,
-- Referidos reutiliza mnuClientes, Cargo reutiliza mnuPrendas — todas ya
-- asignadas a los roles que hoy manejan esas pantallas.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Pausa de suscripción ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'FechaPausaHasta')
BEGIN
    ALTER TABLE Cliente ADD FechaPausaHasta DATETIME NULL;
    PRINT 'Columna FechaPausaHasta agregada a Cliente.';
END
ELSE
    PRINT 'FechaPausaHasta ya existe en Cliente — sin cambios.';
GO

-- ── 2) Programa de referidos ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'IdClienteReferente')
BEGIN
    ALTER TABLE Cliente ADD IdClienteReferente INT NULL REFERENCES Cliente(IdCliente);
    PRINT 'Columna IdClienteReferente agregada a Cliente.';
END
ELSE
    PRINT 'IdClienteReferente ya existe en Cliente — sin cambios.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'DescuentoProximoCobro')
BEGIN
    ALTER TABLE Cliente ADD DescuentoProximoCobro DECIMAL(10,2) NOT NULL DEFAULT 0;
    PRINT 'Columna DescuentoProximoCobro agregada a Cliente.';
END
ELSE
    PRINT 'DescuentoProximoCobro ya existe en Cliente — sin cambios.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'BeneficioReferidoOtorgado')
BEGIN
    ALTER TABLE Cliente ADD BeneficioReferidoOtorgado BIT NOT NULL DEFAULT 0;
    PRINT 'Columna BeneficioReferidoOtorgado agregada a Cliente.';
END
ELSE
    PRINT 'BeneficioReferidoOtorgado ya existe en Cliente — sin cambios.';
GO

-- ── 3) Cargo por daño/pérdida ─────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Prenda' AND COLUMN_NAME = 'IdUltimoCliente')
BEGIN
    ALTER TABLE Prenda ADD IdUltimoCliente INT NULL REFERENCES Cliente(IdCliente);
    PRINT 'Columna IdUltimoCliente agregada a Prenda.';
END
ELSE
    PRINT 'IdUltimoCliente ya existe en Prenda — sin cambios.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CargoPrenda')
BEGIN
    CREATE TABLE CargoPrenda (
        IdCargo       INT           IDENTITY(1,1) PRIMARY KEY,
        IdPrenda      INT           NOT NULL REFERENCES Prenda(IdPrenda),
        IdCliente     INT           NOT NULL REFERENCES Cliente(IdCliente),
        Motivo        NVARCHAR(200) NOT NULL,
        Monto         DECIMAL(10,2) NOT NULL,
        FechaRegistro DATETIME      NOT NULL DEFAULT GETDATE(),
        FechaCobro    DATETIME      NULL,
        Actor         NVARCHAR(100) NULL,
        Estado        INT           NOT NULL DEFAULT 0  -- 0=Pendiente, 1=Cobrado
    );
    PRINT 'Tabla CargoPrenda creada.';
END
ELSE
    PRINT 'Tabla CargoPrenda ya existe — sin cambios.';
GO

-- Backfill: prendas actualmente EnUso ya tienen IdClienteActual — copiarlo a
-- IdUltimoCliente para que no arranquen en NULL (a las que ya se devolvieron
-- antes de esta migración no hay forma de reconstruirles el dato: se acepta).
UPDATE Prenda SET IdUltimoCliente = IdClienteActual
WHERE IdClienteActual IS NOT NULL AND IdUltimoCliente IS NULL;
PRINT 'Backfill de IdUltimoCliente para prendas EnUso aplicado.';
GO

-- Índices sobre las columnas nuevas usadas para filtrar/agrupar.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CargoPrenda_IdCliente' AND object_id = OBJECT_ID('CargoPrenda'))
    CREATE NONCLUSTERED INDEX IX_CargoPrenda_IdCliente ON CargoPrenda(IdCliente);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Cliente_IdClienteReferente' AND object_id = OBJECT_ID('Cliente'))
    CREATE NONCLUSTERED INDEX IX_Cliente_IdClienteReferente ON Cliente(IdClienteReferente);
PRINT 'Índices de Fidelización verificados/creados.';
GO
