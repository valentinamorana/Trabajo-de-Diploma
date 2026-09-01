-- ============================================================
-- WardrobeFlow — 19. INSPECCIÓN DE DEVOLUCIÓN (PN04)
-- ------------------------------------------------------------
-- Alineado a la lógica real de Nuuly (binaria, sin aprobador): al inspeccionar
-- una prenda devuelta (EnLimpieza) el Depósito resuelve directo entre dos
-- caminos únicos — reingresa a Disponible sin cargo, o se da de baja y se
-- cobra el precio de reposición completo (BLL.CargoPrenda.RegistrarCargo, ya
-- existente desde Bloque 1, sin aprobación de nadie). Una prenda que nunca
-- vuelve físicamente (perdida) se reporta directo desde EnUso → Baja
-- (CU-DEP-02), habilitado en BE.Estados.EstadoEnUso.
--
-- Rol: reusa OperadorDeInventario (ya tiene StockEditar/Stock, ya es el
-- "Depósito" conceptual de PN01) — sin rol nuevo, coherente con que en el
-- modelo real de Nuuly no hay un aprobador. GerenteInventario lo hereda.
--
-- La patente mnuInspeccionDevolucion controla SOLO la visibilidad del menú de
-- esta pantalla nueva: la escritura real (CambiarEstado, RegistrarCargo)
-- sigue exigiendo StockEditar/Stock, sin cambios — mismo mecanismo de
-- siempre, ahora con una pantalla propia en vez de reusar la de Stock
-- genérica. A propósito NO se crea una "mnuInspeccionDevolucionEditar": no
-- hay una noción de "editar" separada de "ver" en este módulo (a diferencia
-- de Stock, que sí distingue alta/baja de prendas de la sola consulta), así
-- que una patente Editar que nada verifica sería engañosa en Perfiles y
-- Permisos.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Columna Prenda.PrecioReposicion ───────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Prenda' AND COLUMN_NAME = 'PrecioReposicion')
BEGIN
    ALTER TABLE Prenda ADD PrecioReposicion DECIMAL(10,2) NULL;
    PRINT 'Columna Prenda.PrecioReposicion agregada.';
END
ELSE
    PRINT 'Columna Prenda.PrecioReposicion ya existe — sin cambios.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Prenda_PrecioReposicion')
    ALTER TABLE Prenda ADD CONSTRAINT CHK_Prenda_PrecioReposicion CHECK (PrecioReposicion IS NULL OR PrecioReposicion > 0);
GO

-- ── 2) Patente de menú mnuInspeccionDevolucion ────────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT v.Nombre, v.NombreMenu, v.Tipo, 1, 0, 0
FROM (VALUES
    ('Inspección de Devolución', 'mnuInspeccionDevolucion', 'Inventario')
) AS v(Nombre, NombreMenu, Tipo)
WHERE NOT EXISTS (SELECT 1 FROM Permiso p
                  WHERE p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0);
GO

-- ── 3) Asignación de patente: Administrador + OperadorDeInventario ───────
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador',         'mnuInspeccionDevolucion'),
    ('OperadorDeInventario',  'mnuInspeccionDevolucion')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuInspeccionDevolucion asignado a Administrador y OperadorDeInventario.';
GO

-- ── 4) Mapeo de controles (pantalla "Perfiles y Permisos" → ítems de menú) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, v.Formulario, v.NombreControl
FROM (VALUES
    ('mnuInspeccionDevolucion', 'Menu', 'inspeccionDevolucionToolStripMenuItem')
) AS v(NombreMenu, Formulario, NombreControl)
JOIN Permiso p ON p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = v.Formulario AND c.NombreControl = v.NombreControl);
GO
