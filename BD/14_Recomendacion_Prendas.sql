-- ============================================================
-- WardrobeFlow — 14. RECOMENDACIÓN DE PRENDAS PARA UN CLIENTE (PdN13)
-- ------------------------------------------------------------
-- No requiere tablas nuevas: PdN13 es 100% lectura, cruza el
-- historial de PedidoPrenda de un cliente con el catálogo
-- Disponible (BLL.RecomendacionPrendas).
--
-- Solo agrega la patente de menú mnuRecomendacionPrendas, asignada
-- a Administrador y Vendedor (quien la usa al armar el próximo
-- pedido). GerenteComercial la hereda automáticamente por la
-- arista Composite GerenteComercial → Vendedor (T04) — no se
-- asigna directo para no duplicar la fuente de verdad.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Patente de menú mnuRecomendacionPrendas ───────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Ver Recomendación de Prendas', 'mnuRecomendacionPrendas', 'Sistema', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuRecomendacionPrendas');
GO

-- ── 2) Asignación directa a PermisoRelacion (Administrador y Vendedor) ───
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador', 'mnuRecomendacionPrendas'),
    ('Vendedor',       'mnuRecomendacionPrendas')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuRecomendacionPrendas asignado a Administrador y Vendedor.';
GO

-- ── 3) Mapeo de control (pantalla "Perfiles y Permisos" → control mapeado) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, 'Menu', 'recomendacionPrendasToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuRecomendacionPrendas'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'recomendacionPrendasToolStripMenuItem');
GO
