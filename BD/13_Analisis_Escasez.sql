-- ============================================================
-- WardrobeFlow — 13. DETECCIÓN DE ESCASEZ POR TALLE/CATEGORÍA (PdN12)
-- ------------------------------------------------------------
-- No requiere tablas nuevas: PdN12 es 100% lectura, agrupa Prenda
-- Disponible por Talle+Categoría contra un umbral ingresado en
-- pantalla (BLL.AnalisisEscasez).
--
-- Solo agrega la patente de menú mnuAnalisisEscasez, asignada a
-- Administrador y GerenteInventario. Sigue el patrón DIRECTO a
-- PermisoRelacion, igual que 09_Analisis_Abandono.sql.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Patente de menú mnuAnalisisEscasez ────────────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Ver Escasez de Stock', 'mnuAnalisisEscasez', 'Sistema', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuAnalisisEscasez');
GO

-- ── 2) Asignación directa a PermisoRelacion (Administrador y GerenteInventario) ─
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador',     'mnuAnalisisEscasez'),
    ('GerenteInventario',  'mnuAnalisisEscasez')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuAnalisisEscasez asignado a Administrador y GerenteInventario.';
GO

-- ── 3) Mapeo de control (pantalla "Perfiles y Permisos" → control mapeado) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, 'Menu', 'analisisEscasezToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuAnalisisEscasez'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'analisisEscasezToolStripMenuItem');
GO
