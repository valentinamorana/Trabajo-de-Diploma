-- ============================================================
-- WardrobeFlow — 10. REPORTE DE VENTAS POR VENDEDOR (PdN8)
-- ------------------------------------------------------------
-- No requiere tablas nuevas: PdN8 es 100% lectura, agrega la
-- tabla Pedido (ya existente) agrupada por Empleado
-- (BLL.ReporteVentasVendedor).
--
-- Solo agrega la patente de menú mnuVentasVendedor, asignada a
-- Administrador y GerenteComercial (jefe del área comercial —
-- quien evalúa desempeño de sus vendedores). Sigue el patrón
-- DIRECTO a PermisoRelacion, igual que 09_Analisis_Abandono.sql.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Patente de menú mnuVentasVendedor ─────────────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Ver Ventas por Vendedor', 'mnuVentasVendedor', 'Sistema', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuVentasVendedor');
GO

-- ── 2) Asignación directa a PermisoRelacion (Administrador y GerenteComercial) ─
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador',    'mnuVentasVendedor'),
    ('GerenteComercial', 'mnuVentasVendedor')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuVentasVendedor asignado a Administrador y GerenteComercial.';
GO

-- ── 3) Mapeo de control (pantalla "Perfiles y Permisos" → control mapeado) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, 'Menu', 'ventasVendedorToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuVentasVendedor'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'ventasVendedorToolStripMenuItem');
GO
