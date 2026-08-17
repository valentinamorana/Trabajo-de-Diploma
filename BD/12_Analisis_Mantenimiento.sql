-- ============================================================
-- WardrobeFlow — 12. ANÁLISIS DE TIEMPOS DE MANTENIMIENTO (PdN11)
-- ------------------------------------------------------------
-- No requiere tablas nuevas: PdN11 es 100% lectura sobre
-- MantenimientoPrenda (ya existente desde PdN4), agrupada por
-- prenda (BLL.AnalisisMantenimiento).
--
-- Solo agrega la patente de menú mnuAnalisisMantenimiento, asignada
-- a Administrador y GerenteInventario. Sigue el patrón DIRECTO a
-- PermisoRelacion, igual que 09_Analisis_Abandono.sql.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Patente de menú mnuAnalisisMantenimiento ──────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Ver Tiempos de Mantenimiento', 'mnuAnalisisMantenimiento', 'Sistema', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuAnalisisMantenimiento');
GO

-- ── 2) Asignación directa a PermisoRelacion (Administrador y GerenteInventario) ─
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador',     'mnuAnalisisMantenimiento'),
    ('GerenteInventario',  'mnuAnalisisMantenimiento')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuAnalisisMantenimiento asignado a Administrador y GerenteInventario.';
GO

-- ── 3) Mapeo de control (pantalla "Perfiles y Permisos" → control mapeado) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, 'Menu', 'analisisMantenimientoToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuAnalisisMantenimiento'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'analisisMantenimientoToolStripMenuItem');
GO
