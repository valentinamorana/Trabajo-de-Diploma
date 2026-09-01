-- ============================================================
-- WardrobeFlow — 18. MÉTRICAS, PROMOCIONES Y TOMA DE DECISIONES (PN03)
-- ------------------------------------------------------------
-- Adaptado del proyecto "SIRVI" de un compañero de cursada (Franco De
-- Benedetto), reescrito al vocabulario de WardrobeFlow. Roles:
--   Gerencia    → reusa GerenteComercial (ya existe)
--   Vendedor    → reusa Vendedor (ya existe), sugiere la baja
--   Administración → NUEVO rol AdministracionComercial
--   Contabilidad   → NUEVO rol, separado de Administración a propósito
--                     (separación de funciones: quien aprueba el impacto
--                     económico no es quien redacta la promoción)
--
-- Tabla SugerenciaPromocion: idea cruda de Gerencia (0=Pendiente, 1=Evaluada).
-- Tabla Promocion: aplica a UN plan o a UNA categoría de prenda, nunca ambos.
--   Estado: 0=EnRevisionContable, 1=Vigente, 2=RechazadaContabilidad,
--           3=BajaSolicitada, 4=Desactivada (BE.EstadoPromocion).
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Tabla SugerenciaPromocion ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SugerenciaPromocion')
BEGIN
    CREATE TABLE SugerenciaPromocion (
        IdSugerencia          INT           IDENTITY(1,1) PRIMARY KEY,
        IdPlan                INT           NULL REFERENCES PlanSuscripcion(IdPlan),
        CategoriaPrenda       NVARCHAR(100) NULL,
        Motivo                NVARCHAR(500) NOT NULL,
        TipoDescuentoSugerido INT           NOT NULL CONSTRAINT CHK_SugerenciaPromocion_Tipo CHECK (TipoDescuentoSugerido IN (0,1,2)),
        BeneficioEstimado     DECIMAL(10,2) NOT NULL,
        Estado                INT           NOT NULL DEFAULT 0 CONSTRAINT CHK_SugerenciaPromocion_Estado CHECK (Estado IN (0,1)),
        FechaAlta             DATETIME      NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Tabla SugerenciaPromocion creada.';
END
ELSE
    PRINT 'Tabla SugerenciaPromocion ya existe — sin cambios.';
GO

-- ── 1bis) Constraints de integridad (por si la tabla ya existía sin ellas) ──
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SugerenciaPromocion_Tipo')
    ALTER TABLE SugerenciaPromocion ADD CONSTRAINT CHK_SugerenciaPromocion_Tipo CHECK (TipoDescuentoSugerido IN (0,1,2));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SugerenciaPromocion_Estado')
    ALTER TABLE SugerenciaPromocion ADD CONSTRAINT CHK_SugerenciaPromocion_Estado CHECK (Estado IN (0,1));
GO

-- ── 2) Tabla Promocion ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Promocion')
BEGIN
    CREATE TABLE Promocion (
        IdPromocion        INT           IDENTITY(1,1) PRIMARY KEY,
        Nombre             NVARCHAR(150) NOT NULL,
        Descripcion        NVARCHAR(500) NULL,
        TipoDescuento      INT           NOT NULL CONSTRAINT CHK_Promocion_Tipo CHECK (TipoDescuento IN (0,1,2)),
        Valor              DECIMAL(10,2) NOT NULL CONSTRAINT CHK_Promocion_Valor CHECK (Valor > 0),
        FechaInicio        DATE          NOT NULL,
        FechaFin           DATE          NOT NULL,
        Estado             INT           NOT NULL DEFAULT 0 CONSTRAINT CHK_Promocion_Estado CHECK (Estado IN (0,1,2,3,4)),
        IdPlan             INT           NULL REFERENCES PlanSuscripcion(IdPlan),
        CategoriaPrenda    NVARCHAR(100) NULL,
        MargenEstimado     DECIMAL(10,2) NOT NULL DEFAULT 0,
        ImpactoEconomico   NVARCHAR(500) NULL,
        Observacion        NVARCHAR(500) NULL,
        MotivoBaja         NVARCHAR(500) NULL,
        IdSugerenciaOrigen INT           NULL REFERENCES SugerenciaPromocion(IdSugerencia),
        FechaAlta          DATETIME      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT CHK_Promocion_Fechas CHECK (FechaFin >= FechaInicio),
        CONSTRAINT CHK_Promocion_Destino CHECK (
            (IdPlan IS NOT NULL AND CategoriaPrenda IS NULL) OR
            (IdPlan IS NULL AND CategoriaPrenda IS NOT NULL)),
        -- TipoDescuento 0 = Porcentaje (BE.TipoDescuento): no puede superar el 100%.
        CONSTRAINT CHK_Promocion_Porcentaje CHECK (TipoDescuento <> 0 OR Valor <= 100)
    );
    PRINT 'Tabla Promocion creada.';
END
ELSE
    PRINT 'Tabla Promocion ya existe — sin cambios.';
GO

-- ── 2bis) Constraints de integridad (por si la tabla ya existía sin ellas) ──
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Tipo')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Tipo CHECK (TipoDescuento IN (0,1,2));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Valor')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Valor CHECK (Valor > 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Estado')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Estado CHECK (Estado IN (0,1,2,3,4));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Fechas')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Fechas CHECK (FechaFin >= FechaInicio);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Destino')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Destino CHECK (
        (IdPlan IS NOT NULL AND CategoriaPrenda IS NULL) OR
        (IdPlan IS NULL AND CategoriaPrenda IS NOT NULL));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Porcentaje')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Porcentaje CHECK (TipoDescuento <> 0 OR Valor <= 100);
GO

-- ── 3) Patentes de menú ──────────────────────────────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT v.Nombre, v.NombreMenu, v.Tipo, 1, 0, 0
FROM (VALUES
    ('Sugerir Promoción',           'mnuSugerenciaPromocion',        'Promociones'),
    ('Gestionar Promociones',       'mnuPromocionesAdmin',           'Promociones'),
    ('Configurar Promociones Admin','mnuPromocionesAdminEditar',     'Promociones'),
    ('Revisión Contable',           'mnuPromocionesContable',        'Promociones'),
    ('Configurar Revisión Contable','mnuPromocionesContableEditar',  'Promociones'),
    ('Ver Promociones Vigentes',    'mnuPromocionesVigentes',        'Promociones'),
    ('Sugerir Baja de Promoción',   'mnuPromocionesVigentesEditar',  'Promociones')
) AS v(Nombre, NombreMenu, Tipo)
WHERE NOT EXISTS (SELECT 1 FROM Permiso p
                  WHERE p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0);
GO

-- ── 4) Roles nuevos: AdministracionComercial y Contabilidad ─────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT v.Rol, v.Rol, 'Rol', 1, 1, 1
FROM (VALUES ('AdministracionComercial'), ('Contabilidad')) AS v(Rol)
WHERE NOT EXISTS (SELECT 1 FROM Permiso p WHERE p.Nombre = v.Rol AND p.EsRol = 1);
GO

-- ── 5) Asignación de patentes ────────────────────────────────────────────
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    -- Administrador: acceso total también a este módulo nuevo.
    ('Administrador', 'mnuSugerenciaPromocion'), ('Administrador', 'mnuPromocionesAdmin'),
    ('Administrador', 'mnuPromocionesAdminEditar'), ('Administrador', 'mnuPromocionesContable'),
    ('Administrador', 'mnuPromocionesContableEditar'), ('Administrador', 'mnuPromocionesVigentes'),
    ('Administrador', 'mnuPromocionesVigentesEditar'),
    -- Gerencia (reusa GerenteComercial): sugiere promociones.
    ('GerenteComercial', 'mnuSugerenciaPromocion'),
    -- Vendedor: consulta vigentes y puede sugerir la baja.
    ('Vendedor', 'mnuPromocionesVigentes'), ('Vendedor', 'mnuPromocionesVigentesEditar'),
    -- Administración: gestiona el ciclo completo de la promoción.
    ('AdministracionComercial', 'mnuPromocionesAdmin'), ('AdministracionComercial', 'mnuPromocionesAdminEditar'),
    -- Contabilidad: aprueba o rechaza.
    ('Contabilidad', 'mnuPromocionesContable'), ('Contabilidad', 'mnuPromocionesContableEditar')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permisos de Promociones asignados a Administrador, GerenteComercial, Vendedor, AdministracionComercial y Contabilidad.';
GO

-- ── 6) Mapeo de controles (pantalla "Perfiles y Permisos" → ítems de menú) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, v.Formulario, v.NombreControl
FROM (VALUES
    ('mnuSugerenciaPromocion', 'Menu', 'promocionesToolStripMenuItem'),
    ('mnuSugerenciaPromocion', 'Menu', 'sugerirPromocionToolStripMenuItem'),
    ('mnuPromocionesAdmin',    'Menu', 'gestionPromocionesToolStripMenuItem'),
    ('mnuPromocionesContable', 'Menu', 'revisionContablePromocionesToolStripMenuItem'),
    ('mnuPromocionesVigentes', 'Menu', 'promocionesVigentesToolStripMenuItem')
) AS v(NombreMenu, Formulario, NombreControl)
JOIN Permiso p ON p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = v.Formulario AND c.NombreControl = v.NombreControl);
GO
