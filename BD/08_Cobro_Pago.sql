-- ============================================================
-- WardrobeFlow — 08. COBRO Y PAGO DE SUSCRIPCIÓN (PdN6)
-- ------------------------------------------------------------
-- Tabla de auditoría del patrón Chain of Responsibility usado en
-- BLL.Manejadores (DetectarCobro → ProcesarPago → AplicarGracia →
-- Suspender). Cada fila es un intento de cobro para un cliente:
-- detectado, intentado (fuera del sistema: efectivo/transferencia,
-- sin pasarela — ver alcance) y resuelto como Cobrado / Gracia /
-- Suspendido, o Pendiente si aún no correspondía cobrar.
--
-- Complementa a HistorialRenovacion (PdN5): un cobro exitoso
-- confirma la renovación (extiende FechaVencimiento reutilizando el
-- mismo Builder de PdN1); un cobro fallido no cancela la suscripción
-- de inmediato, abre un período de gracia (Cliente.FechaLimiteGracia)
-- antes de bloquear nuevos pedidos.
--
-- Sigue el patrón DIRECTO a PermisoRelacion (no vía RolPermiso), el
-- mismo criterio que 07_Reset_Perfiles_Permisos.sql documenta como
-- la única fuente real de autorización.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Columna Cliente.FechaLimiteGracia (período de gracia) ────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'FechaLimiteGracia')
BEGIN
    ALTER TABLE Cliente ADD FechaLimiteGracia DATE NULL;
    PRINT 'Columna FechaLimiteGracia agregada a Cliente.';
END
ELSE
    PRINT 'FechaLimiteGracia ya existe en Cliente — sin cambios.';
GO

-- ── 2) Tabla HistorialCobro ───────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HistorialCobro')
BEGIN
    CREATE TABLE HistorialCobro (
        IdCobro         INT           IDENTITY(1,1) PRIMARY KEY,
        IdCliente       INT           NOT NULL REFERENCES Cliente(IdCliente),
        Importe         DECIMAL(10,2) NOT NULL DEFAULT 0,
        FechaDeteccion  DATETIME      NOT NULL DEFAULT GETDATE(),
        FechaResolucion DATETIME      NULL,
        -- 0=Pendiente, 1=Cobrado, 2=Gracia, 3=Suspendido (BE.EstadoCobro)
        Resultado       INT           NOT NULL,
        Actor           NVARCHAR(100) NULL
    );
    PRINT 'Tabla HistorialCobro creada.';
END
ELSE
    PRINT 'Tabla HistorialCobro ya existe — sin cambios.';
GO

-- ── 3) Patente de menú mnuCobroSuscripcion ───────────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Gestionar Cobros', 'mnuCobroSuscripcion', 'Ventas', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuCobroSuscripcion');
GO

-- ── 4) Asignación directa a PermisoRelacion (Administrador y Vendedor;
--     GerenteComercial la hereda de Vendedor por la arista Composite ya
--     existente, igual que ya pasa con mnuRenovacionSuscripcion) ─────────
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador', 'mnuCobroSuscripcion'),
    ('Vendedor',       'mnuCobroSuscripcion')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuCobroSuscripcion asignado a Administrador y Vendedor (GerenteComercial lo hereda de Vendedor).';
GO

-- ── 5) Mapeo de control (pantalla "Perfiles y Permisos" → control mapeado) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, 'Menu', 'cobroSuscripcionToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuCobroSuscripcion'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'cobroSuscripcionToolStripMenuItem');
GO
