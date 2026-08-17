-- ============================================================
-- WardrobeFlow — 11. ANÁLISIS DE ROTACIÓN DE PRENDAS (PdN9)
-- ------------------------------------------------------------
-- No requiere tablas nuevas: PdN9 es 100% lectura, cruza Prenda
-- (catálogo activo) con COUNT(*) de PedidoPrenda por prenda
-- (BLL.AnalisisRotacion).
--
-- Solo agrega la patente de menú mnuAnalisisRotacion, asignada a
-- Administrador y GerenteInventario (jefe del área de inventario —
-- quien decide bajas y reposición). Sigue el patrón DIRECTO a
-- PermisoRelacion, igual que 09_Analisis_Abandono.sql.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Patente de menú mnuAnalisisRotacion ───────────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Ver Rotación de Prendas', 'mnuAnalisisRotacion', 'Sistema', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuAnalisisRotacion');
GO

-- ── 2) Asignación directa a PermisoRelacion (Administrador y GerenteInventario) ─
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador',     'mnuAnalisisRotacion'),
    ('GerenteInventario',  'mnuAnalisisRotacion')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuAnalisisRotacion asignado a Administrador y GerenteInventario.';
GO

-- ── 3) Mapeo de control (pantalla "Perfiles y Permisos" → control mapeado) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, 'Menu', 'analisisRotacionToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuAnalisisRotacion'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'analisisRotacionToolStripMenuItem');
GO
