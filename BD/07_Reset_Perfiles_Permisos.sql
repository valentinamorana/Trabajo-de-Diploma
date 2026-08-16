-- ============================================================
-- WardrobeFlow — 07. RESET DE PERFILES Y PERMISOS
-- ------------------------------------------------------------
-- Reconstruye desde cero, en el árbol Composite [PermisoRelacion] (la única
-- fuente real de autorización — así lo documenta BLL.Familia), las patentes
-- de los 7 roles reales del sistema:
--   Administrador, Auditor, Vendedor, GerenteComercial, OperadorLogistico,
--   OperadorDeInventario, GerenteInventario.
--
-- POR QUÉ: una base que viene evolucionando desde hace tiempo puede tener
-- el árbol desincronizado (patentes que faltan, aristas viejas de roles ya
-- retirados que quedaron colgando, etc.) — fue justo lo que causó que el
-- Administrador no viera Suscriptores/Ventas/Analítica en el menú, pese a
-- que el código ya tiene bypass para Admin ahí. Los otros 6 roles NO tienen
-- ese bypass — dependen 100% de que este árbol esté bien armado.
--
-- QUÉ HACE:
--   1) Asegura que existan las 11 patentes base + las 5 de edición granular
--      (Ver vs Configurar) y los 7 nodos-rol.
--   2) BORRA todas las aristas SALIENTES actuales de esos 7 roles (a
--      patentes y a otros roles).
--   3) Las vuelve a crear con el estado correcto — el mismo que ya quedó
--      documentado y verificado contra el código en la matriz de roles.
--   4) Muestra al final una consulta de verificación para chequear a ojo.
--
-- Roles que NO sean estos 7 (si se creó alguno propio) no se tocan. Los
-- roles retirados (Supervisor, ControladorDeStock, EncargadoDeStock) siguen
-- desactivados como estaban, no se reactivan.
--
-- Idempotente: correrlo varias veces siempre deja el mismo estado final.
-- Recomendado: hacer un backup (Administrar → Backup y Restauración) antes
-- de correrlo, por las dudas.
-- ============================================================

USE WardrobeFlowDB;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- ── 1) Asegurar que las patentes base + las de edición granular existan ──
    INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
    SELECT v.Nombre, v.NombreMenu, v.Tipo, 1, 0, 0
    FROM (VALUES
        ('Gestionar Usuarios',          'mnuUsuarios',              'Sistema'),
        ('Ver Auditoria',               'mnuAuditoria',             'Sistema'),
        ('Ver Prendas',                 'mnuPrendas',               'Inventario'),
        ('Ver Outfits',                 'mnuOutfits',               'Inventario'),
        ('Ver Categorias',              'mnuCategorias',            'Inventario'),
        ('Gestionar Stock',             'mnuStock',                 'Inventario'),
        ('Gestionar Clientes',          'mnuClientes',              'Ventas'),
        ('Gestionar PlanSuscripciones', 'mnuPlanSuscripciones',     'Ventas'),
        ('Gestionar Renovaciones',      'mnuRenovacionSuscripcion', 'Ventas'),
        ('Realizar Ventas',             'mnuPedidosVenta',          'Ventas'),
        ('Ver Pedidos Realizados',      'mnuPedidosRealizados',     'Ventas'),
        ('Configurar Stock',             'mnuStockEditar',             'Inventario'),
        ('Configurar Clientes',          'mnuClientesEditar',          'Ventas'),
        ('Configurar PlanSuscripciones', 'mnuPlanSuscripcionesEditar', 'Ventas'),
        ('Configurar PedidosVenta',      'mnuPedidosVentaEditar',      'Ventas'),
        ('Configurar PedidosRealizados', 'mnuPedidosRealizadosEditar', 'Ventas')
    ) AS v(Nombre, NombreMenu, Tipo)
    WHERE NOT EXISTS (SELECT 1 FROM Permiso p
                      WHERE p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0);

    -- ── 2) Asegurar que los 7 nodos-rol existan ─────────────────────────────
    INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
    SELECT v.Rol, v.Rol, 'Rol', 1, 1, 1
    FROM (VALUES
        ('Administrador'), ('Auditor'), ('Vendedor'), ('GerenteComercial'),
        ('OperadorLogistico'), ('OperadorDeInventario'), ('GerenteInventario')
    ) AS v(Rol)
    WHERE NOT EXISTS (SELECT 1 FROM Permiso p WHERE p.Nombre = v.Rol AND p.EsRol = 1);

    -- ── 3) Reset: borrar TODO lo que sale de estos 7 roles en el árbol ──────
    DELETE r
    FROM PermisoRelacion r
    JOIN Permiso rol ON rol.IdPermiso = r.IdPadre AND rol.EsRol = 1
    WHERE rol.Nombre IN ('Administrador','Auditor','Vendedor','GerenteComercial',
                          'OperadorLogistico','OperadorDeInventario','GerenteInventario');

    -- ── 4) Aristas rol → patente (accesos directos/propios de cada rol) ─────
    INSERT INTO PermisoRelacion (IdPadre, IdHijo)
    SELECT rol.IdPermiso, pat.IdPermiso
    FROM (VALUES
        -- Administrador: acceso total (las 11 patentes de menú)
        ('Administrador', 'mnuUsuarios'),   ('Administrador', 'mnuAuditoria'),
        ('Administrador', 'mnuPrendas'),    ('Administrador', 'mnuOutfits'),
        ('Administrador', 'mnuCategorias'), ('Administrador', 'mnuStock'),
        ('Administrador', 'mnuClientes'),   ('Administrador', 'mnuPlanSuscripciones'),
        ('Administrador', 'mnuRenovacionSuscripcion'),
        ('Administrador', 'mnuPedidosVenta'), ('Administrador', 'mnuPedidosRealizados'),
        -- Auditor: solo auditoría (100% solo lectura)
        ('Auditor', 'mnuAuditoria'),
        -- Vendedor: mostrador comercial
        ('Vendedor', 'mnuPrendas'), ('Vendedor', 'mnuClientes'),
        ('Vendedor', 'mnuPlanSuscripciones'), ('Vendedor', 'mnuRenovacionSuscripcion'),
        ('Vendedor', 'mnuPedidosVenta'),
        -- GerenteComercial: patente PROPIA (el resto lo hereda de Vendedor, paso 5)
        ('GerenteComercial', 'mnuPedidosRealizados'),
        -- OperadorLogistico: despacho
        ('OperadorLogistico', 'mnuPedidosRealizados'),
        -- OperadorDeInventario: mantenimiento de prendas
        ('OperadorDeInventario', 'mnuPrendas'), ('OperadorDeInventario', 'mnuStock'),
        -- GerenteInventario: patentes PROPIAS (el resto lo hereda de los dos operadores, paso 5)
        ('GerenteInventario', 'mnuCategorias'), ('GerenteInventario', 'mnuOutfits'),
        -- Edición granular (Ver ≠ Configurar) — se otorga a quien de hecho gestiona cada módulo
        ('Administrador', 'mnuStockEditar'), ('Administrador', 'mnuClientesEditar'),
        ('Administrador', 'mnuPlanSuscripcionesEditar'), ('Administrador', 'mnuPedidosVentaEditar'),
        ('Administrador', 'mnuPedidosRealizadosEditar'),
        ('Vendedor', 'mnuClientesEditar'), ('Vendedor', 'mnuPlanSuscripcionesEditar'),
        ('Vendedor', 'mnuPedidosVentaEditar'),
        ('OperadorDeInventario', 'mnuStockEditar'),
        ('OperadorLogistico', 'mnuPedidosRealizadosEditar')
    ) AS v(Rol, NombreMenu)
    JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
    JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0;

    -- ── 5) Aristas rol → rol (herencia Composite: el padre suma lo del hijo) ─
    INSERT INTO PermisoRelacion (IdPadre, IdHijo)
    SELECT padre.IdPermiso, hijo.IdPermiso
    FROM (VALUES
        ('GerenteComercial',  'Vendedor'),
        ('GerenteInventario', 'OperadorLogistico'),
        ('GerenteInventario', 'OperadorDeInventario')
    ) AS v(Padre, Hijo)
    JOIN Permiso padre ON padre.Nombre = v.Padre AND padre.EsRol = 1
    JOIN Permiso hijo  ON hijo.Nombre  = v.Hijo  AND hijo.EsRol  = 1;

    COMMIT TRANSACTION;
    PRINT 'Perfiles y permisos reseteados: los 7 roles quedaron reconstruidos desde cero en PermisoRelacion.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'ERROR — no se aplicó el reset, se revirtió todo. Detalle: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

-- ============================================================
-- VERIFICACIÓN — aristas directas de cada rol (no resuelve la herencia
-- rol→rol; para eso mirá el menú real logueada con cada usuario, o la
-- pantalla Perfiles y Permisos que sí resuelve el árbol completo).
-- ============================================================
SELECT rol.Nombre AS Rol, pat.NombreMenu AS Patente
FROM PermisoRelacion r
JOIN Permiso rol ON rol.IdPermiso = r.IdPadre AND rol.EsRol = 1
JOIN Permiso pat ON pat.IdPermiso = r.IdHijo
WHERE rol.Nombre IN ('Administrador','Auditor','Vendedor','GerenteComercial',
                      'OperadorLogistico','OperadorDeInventario','GerenteInventario')
ORDER BY rol.Nombre, pat.NombreMenu;
GO
