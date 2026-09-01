-- ============================================================
-- WardrobeFlow — 17. COMERCIALIZACIÓN DE LA SUSCRIPCIÓN (PN02)
-- ------------------------------------------------------------
-- Adaptado de un TP de otro compañero de cursada (misma lógica de negocio:
-- alta de cliente + contratación de un plan), reescrito al vocabulario de
-- WardrobeFlow. Roles: Venta (ya existe, es el Vendedor) y Caja (NUEVO,
-- separado de Vendedor a propósito: Vendedor es "operador" de la venta,
-- Caja cobra — separación de funciones).
--
-- Tabla Contratacion: estado intermedio entre "el cliente eligió un plan"
-- (Venta, CrearContratacion) y "la suscripción quedó vigente" (Caja confirma
-- el pago, ConfirmarPago dispara BLL.Cliente.ActivarSuscripcionDesdeContratacion).
-- 0=PendientePago, 1=Pagada, 2=Cancelada (BE.EstadoContratacion).
--
-- El Comprobante (CU02-CAJ) se guarda como columnas propias de Contratacion
-- (NumeroComprobante, FechaComprobante) — no hace falta una tabla aparte.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Tabla Contratacion ────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Contratacion')
BEGIN
    CREATE TABLE Contratacion (
        IdContratacion    INT           IDENTITY(1,1) PRIMARY KEY,
        IdCliente         INT           NOT NULL REFERENCES Cliente(IdCliente),
        IdPlan            INT           NOT NULL REFERENCES PlanSuscripcion(IdPlan),
        IdVendedor        INT           NOT NULL REFERENCES Empleado(IdEmpleado),
        IdCaja            INT           NULL     REFERENCES Empleado(IdEmpleado),
        Modalidad         INT           NOT NULL CONSTRAINT CHK_Contratacion_Modalidad CHECK (Modalidad IN (0,1,2)),
        Estado            INT           NOT NULL DEFAULT 0 CONSTRAINT CHK_Contratacion_Estado CHECK (Estado IN (0,1,2)),
        IntentosPago      INT           NOT NULL DEFAULT 0,
        FechaAlta         DATETIME      NOT NULL DEFAULT GETDATE(),
        FechaResolucion   DATETIME      NULL,
        MedioPago         NVARCHAR(50)  NULL,
        NumeroComprobante NVARCHAR(50)  NULL,
        FechaComprobante  DATETIME      NULL
    );
    PRINT 'Tabla Contratacion creada.';
END
ELSE
    PRINT 'Tabla Contratacion ya existe — sin cambios.';
GO

-- ── 1bis) Constraints de integridad (por si la tabla ya existía sin ellas) ──
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Contratacion_Modalidad')
    ALTER TABLE Contratacion ADD CONSTRAINT CHK_Contratacion_Modalidad CHECK (Modalidad IN (0,1,2));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Contratacion_Estado')
    ALTER TABLE Contratacion ADD CONSTRAINT CHK_Contratacion_Estado CHECK (Estado IN (0,1,2));
GO

-- ── 2) Patentes de menú mnuCaja / mnuCajaEditar ──────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT v.Nombre, v.NombreMenu, v.Tipo, 1, 0, 0
FROM (VALUES
    ('Gestionar Caja',  'mnuCaja',       'Caja'),
    ('Configurar Caja', 'mnuCajaEditar', 'Caja')
) AS v(Nombre, NombreMenu, Tipo)
WHERE NOT EXISTS (SELECT 1 FROM Permiso p
                  WHERE p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0);
GO

-- ── 3) Rol Caja (nuevo, real — separado de Vendedor) ─────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Caja', 'Caja', 'Rol', 1, 1, 1
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE Nombre = 'Caja' AND EsRol = 1);
GO

-- ── 4) Asignación de patentes: Administrador (acceso total) + Caja ───────
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador', 'mnuCaja'),
    ('Administrador', 'mnuCajaEditar'),
    ('Caja',          'mnuCaja'),
    ('Caja',          'mnuCajaEditar')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permisos mnuCaja/mnuCajaEditar asignados a Administrador y Caja.';
GO

-- ── 5) Mapeo de controles (pantalla "Perfiles y Permisos" → ítems de menú) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, v.Formulario, v.NombreControl
FROM (VALUES
    ('mnuClientes', 'Menu', 'nuevaContratacionToolStripMenuItem'),
    ('mnuCaja',     'Menu', 'cajaToolStripMenuItem'),
    ('mnuCaja',     'Menu', 'contratacionesPendientesToolStripMenuItem')
) AS v(NombreMenu, Formulario, NombreControl)
JOIN Permiso p ON p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = v.Formulario AND c.NombreControl = v.NombreControl);
GO
