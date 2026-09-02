-- ============================================================
-- WardrobeFlow — 20. HARDENING DE INTEGRIDAD (auditoría de BD)
-- ------------------------------------------------------------
-- Varias columnas INT respaldadas por un enum de C# (Prenda.Estado,
-- Pedido.Estado, HistorialRenovacion.Resultado, HistorialCobro.Resultado,
-- ListaEspera.Estado, CargoPrenda.Estado, Bitacora.criticidad) quedaron
-- sin el CHECK que sí se agregó para los módulos más nuevos (Contratacion,
-- Promocion, SugerenciaPromocion, Prenda.PrecioReposicion — ver 17/18/19).
-- Hoy la app nunca escribe un valor fuera de rango (siempre castea el
-- enum), pero sin el CHECK un UPDATE manual o una migración futura que
-- agregue un miembro al enum sin agregar el CHECK correspondiente podría
-- dejar un valor inválido sin que el motor lo impida. Este script cierra
-- esa brecha para las columnas viejas, con el mismo patrón idempotente
-- (ALTER TABLE ... ADD CONSTRAINT IF NOT EXISTS) que ya usan 17/18/19.
--
-- También agrega índices sobre Prenda.Estado y Pedido.Estado: son el
-- predicado principal de varias consultas calientes del DAL
-- (Prenda.ObtenerDisponibles, Prenda.ObtenerConteoDisponiblesPorTalleCategoria,
-- Pedido.ObtenerPendientes) que hoy no tienen índice de apoyo.
--
-- Idempotente: se puede volver a ejecutar sin duplicar ni romper nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) CHECK constraints — columnas Estado/Resultado respaldadas por enum ──

-- Prenda.Estado (BE.EstadoPrenda: Disponible=0, EnUso=1, EnLimpieza=2, Baja=3)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Prenda_Estado')
    ALTER TABLE Prenda ADD CONSTRAINT CHK_Prenda_Estado CHECK (Estado IN (0,1,2,3));
GO

-- Pedido.Estado (BE.EstadoPedido: Pendiente=0, Despachado=1, Entregado=2, Cancelado=3)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Pedido_Estado')
    ALTER TABLE Pedido ADD CONSTRAINT CHK_Pedido_Estado CHECK (Estado IN (0,1,2,3));
GO

-- HistorialRenovacion.Resultado (BE.EstadoRenovacion: Pendiente=0, Renovada=1,
-- CambioPlan=2, Baja=3, Pausada=4)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HistorialRenovacion_Resultado')
    ALTER TABLE HistorialRenovacion ADD CONSTRAINT CHK_HistorialRenovacion_Resultado CHECK (Resultado IN (0,1,2,3,4));
GO

-- HistorialCobro.Resultado (BE.EstadoCobro: Pendiente=0, Cobrado=1, Gracia=2, Suspendido=3)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HistorialCobro_Resultado')
    ALTER TABLE HistorialCobro ADD CONSTRAINT CHK_HistorialCobro_Resultado CHECK (Resultado IN (0,1,2,3));
GO

-- ListaEspera.Estado (BE.EstadoListaEspera: Pendiente=0, Reservada=1, Convertida=2, Cancelada=3)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_ListaEspera_Estado')
    ALTER TABLE ListaEspera ADD CONSTRAINT CHK_ListaEspera_Estado CHECK (Estado IN (0,1,2,3));
GO

-- CargoPrenda.Estado (BE.EstadoCargo: Pendiente=0, Cobrado=1)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_CargoPrenda_Estado')
    ALTER TABLE CargoPrenda ADD CONSTRAINT CHK_CargoPrenda_Estado CHECK (Estado IN (0,1));
GO

-- Bitacora.criticidad (BE.Criticidad: None=0, Baja=1, Media=2, Alta=3,
-- IntentosLogin=4, RecuperacionClave=5, BloqueosCuenta=6)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Bitacora_criticidad')
    ALTER TABLE Bitacora ADD CONSTRAINT CHK_Bitacora_criticidad CHECK (criticidad IN (0,1,2,3,4,5,6));
GO

-- ── 2) Índices sobre Estado (predicado principal de varias consultas del DAL) ──

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Prenda_Estado' AND object_id = OBJECT_ID('Prenda'))
    CREATE NONCLUSTERED INDEX IX_Prenda_Estado ON Prenda(Estado) INCLUDE (Categoria, Talle);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pedido_Estado' AND object_id = OBJECT_ID('Pedido'))
    CREATE NONCLUSTERED INDEX IX_Pedido_Estado ON Pedido(Estado);
PRINT 'Índices de Estado (Prenda/Pedido) verificados/creados.';
GO
