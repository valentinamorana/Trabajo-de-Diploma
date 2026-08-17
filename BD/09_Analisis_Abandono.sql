-- ============================================================
-- WardrobeFlow — 09. ANÁLISIS DE ABANDONO (PdN10)
-- ------------------------------------------------------------
-- No requiere tablas nuevas: PdN10 es 100% lectura, cruza Cliente
-- (suscripción/vencimiento) con MAX(FechaPedido) de la tabla Pedido
-- ya existente (BLL.AnalisisAbandono, patrón Strategy en BLL.Estrategias).
--
-- Solo agrega la patente de menú mnuAnalisisAbandono, asignada a
-- Administrador y GerenteComercial (jefe del área comercial — quien
-- decide acciones de retención). Sigue el patrón DIRECTO a
-- PermisoRelacion, igual que 08_Cobro_Pago.sql.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Patente de menú mnuAnalisisAbandono ───────────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Ver Análisis de Abandono', 'mnuAnalisisAbandono', 'Sistema', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuAnalisisAbandono');
GO

-- ── 2) Asignación directa a PermisoRelacion (Administrador y GerenteComercial) ─
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador',    'mnuAnalisisAbandono'),
    ('GerenteComercial', 'mnuAnalisisAbandono')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuAnalisisAbandono asignado a Administrador y GerenteComercial.';
GO

-- ── 3) Mapeo de control (pantalla "Perfiles y Permisos" → control mapeado) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, 'Menu', 'analisisAbandonoToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuAnalisisAbandono'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'analisisAbandonoToolStripMenuItem');
GO
