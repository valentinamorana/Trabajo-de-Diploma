-- ============================================================
-- WardrobeFlow — 16. LISTA DE ESPERA DE PRENDAS (mejora opcional,
-- no requerida por la cátedra — ver README, sección "Módulos")
-- ------------------------------------------------------------
-- Inspirado en la Lista de Espera de ExperienceHub (TP de un
-- compañero de cursada), adaptado al modelo de WardrobeFlow: acá
-- se espera una PRENDA ESPECÍFICA (mismo IdPrenda), no una
-- categoría genérica.
--
-- Tabla ListaEspera: un cliente se anota por una prenda EnUso.
-- Cuando esa prenda se libera (BLL.Prenda.CambiarEstado, al pasar
-- de EnLimpieza a Disponible), la fila Pendiente más antigua (FIFO)
-- pasa a Reservada con una ventana de 48hs exclusiva para ese
-- cliente (BLL.ListaEspera.HORAS_RESERVA). Si nadie la retira a
-- tiempo, la prenda vuelve a estar disponible para cualquiera —
-- por comparación de fecha, sin job en background, mismo criterio
-- que Cliente.FechaLimiteGracia (PdN6).
--
-- 0=Pendiente, 1=Reservada, 2=Convertida, 3=Cancelada (BE.EstadoListaEspera)
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Tabla ListaEspera ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ListaEspera')
BEGIN
    CREATE TABLE ListaEspera (
        IdListaEspera     INT      IDENTITY(1,1) PRIMARY KEY,
        IdPrenda          INT      NOT NULL REFERENCES Prenda(IdPrenda),
        IdCliente         INT      NOT NULL REFERENCES Cliente(IdCliente),
        FechaAlta         DATETIME NOT NULL DEFAULT GETDATE(),
        Estado            INT      NOT NULL DEFAULT 0,
        FechaLimiteReserva DATETIME NULL,
        FechaResolucion   DATETIME NULL,
        Actor             NVARCHAR(100) NULL
    );
    PRINT 'Tabla ListaEspera creada.';
END
ELSE
    PRINT 'Tabla ListaEspera ya existe — sin cambios.';
GO

-- ── 2) Patente de menú mnuListaEspera (solo gobierna visibilidad de la
--     pantalla, igual que mnuCobroSuscripcion — las mutaciones se validan
--     con la patente mnuStockEditar, ver BLL.ListaEspera) ──────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Lista de Espera', 'mnuListaEspera', 'Inventario', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuListaEspera');
GO

-- ── 3) Asignación directa a PermisoRelacion (Administrador y las 2 patentes
--     de Inventario: Vendedor la usa para anotar clientes, GerenteInventario/
--     OperadorDeInventario para gestionarla) ───────────────────────────────
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador',        'mnuListaEspera'),
    ('Vendedor',              'mnuListaEspera'),
    ('OperadorDeInventario',  'mnuListaEspera')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuListaEspera asignado a Administrador, Vendedor y OperadorDeInventario.';
GO

-- ── 4) Mapeo de control (pantalla "Perfiles y Permisos" → control mapeado) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT p.IdPermiso, 'Menu', 'listaEsperaToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuListaEspera'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'listaEsperaToolStripMenuItem');
GO
