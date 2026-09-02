-- ============================================================
-- WardrobeFlow — 05. RENOVACIÓN DE SUSCRIPCIÓN (PdN5)
-- ------------------------------------------------------------
-- Tabla de auditoría del patrón Chain of Responsibility usado en
-- BLL.Manejadores (VerificarVencimiento → IntentarRenovar → CambioPlan →
-- BajaSuscripcion). Cada fila es un intento de resolución de renovación
-- para un cliente: detectado, contactado (fuera del sistema) y resuelto
-- como Renovada / CambioPlan / Baja, o Pendiente si aún no venció.
--
-- Idempotente: se puede volver a ejecutar sin duplicar la tabla.
-- ============================================================

USE WardrobeFlowDB;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HistorialRenovacion')
BEGIN
    CREATE TABLE HistorialRenovacion (
        IdRenovacion    INT           IDENTITY(1,1) PRIMARY KEY,
        IdCliente       INT           NOT NULL REFERENCES Cliente(IdCliente),
        IdPlanAnterior  INT           NULL REFERENCES PlanSuscripcion(IdPlan),
        IdPlanNuevo     INT           NULL REFERENCES PlanSuscripcion(IdPlan),
        FechaDeteccion  DATETIME      NOT NULL DEFAULT GETDATE(),
        FechaResolucion DATETIME      NULL,
        -- 0=Pendiente, 1=Renovada, 2=CambioPlan, 3=Baja, 4=Pausada (BE.EstadoRenovacion)
        Resultado       INT           NOT NULL,
        Actor           NVARCHAR(100) NULL
    );
    PRINT 'Tabla HistorialRenovacion creada.';
END
ELSE
    PRINT 'Tabla HistorialRenovacion ya existe — sin cambios.';
GO

-- ============================================================
-- PERMISO — patente de menú para bases YA CREADAS (idempotente)
-- ------------------------------------------------------------
-- Solo hace falta para instalaciones existentes: 01_Crear_BaseDeDatos.sql ya
-- siembra esto en instalaciones nuevas. Sigue el mismo patrón de migración que
-- el resto del archivo 02 (RolPermiso plano → PermisoRelacion Composite).
-- Supervisor NO se lista acá: hereda mnuRenovacionSuscripcion de Vendedor a
-- través de la arista Composite Supervisor→Vendedor que ya existe.
-- ============================================================

INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Gestionar Renovaciones', 'mnuRenovacionSuscripcion', 'Ventas', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuRenovacionSuscripcion');
GO

INSERT INTO RolPermiso (Rol, IdPermiso)
SELECT r.Rol, p.IdPermiso
FROM (VALUES
    ('Administrador','mnuRenovacionSuscripcion'),
    ('Vendedor','mnuRenovacionSuscripcion')
) AS r(Rol, NombreMenu)
JOIN Permiso p ON p.NombreMenu = r.NombreMenu AND ISNULL(p.EsFamilia,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM RolPermiso x WHERE x.Rol = r.Rol AND x.IdPermiso = p.IdPermiso);
GO

-- Regenerar aristas Composite (rol → patente) a partir de RolPermiso, igual que
-- el resto de las migraciones de permisos de este proyecto.
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT pr.IdPermiso, rp.IdPermiso
FROM   RolPermiso rp
INNER JOIN Permiso pr ON pr.Nombre = rp.Rol AND pr.EsRol = 1 AND pr.IdPermiso <> rp.IdPermiso
WHERE  NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                   WHERE x.IdPadre = pr.IdPermiso AND x.IdHijo = rp.IdPermiso);
PRINT 'Permiso mnuRenovacionSuscripcion asignado a Administrador y Vendedor (Supervisor lo hereda de Vendedor).';
GO

INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, 'Menu', 'renovacionSuscripcionToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuRenovacionSuscripcion'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'renovacionSuscripcionToolStripMenuItem');
GO
