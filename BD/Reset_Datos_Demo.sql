-- ============================================================
-- WardrobeFlow — Restablecer los DATOS DE DEMO
-- ------------------------------------------------------------
-- Pide que la próxima corrida de 00_Instalacion_Completa.sql BORRE los datos de negocio
-- (clientes, prendas, pedidos, contrataciones, promociones y bitácora de negocio) y cargue los
-- escenarios de demo de la sección 21e (Juan PedidoFeliz, Ana PrendasNoDisponibles, etc.).
-- Usuarios, planes, permisos, traducciones y bitácora del sistema se conservan.
--
-- Uso (HACER UN BACKUP ANTES: se pierden los datos de negocio actuales):
--   1. Ejecutar este script sobre WardrobeFlowDB.
--   2. Ejecutar BD/00_Instalacion_Completa.sql (o reinstalar la aplicación).
--   3. Abrir la aplicación: recalcula los dígitos verificadores al arrancar.
-- ============================================================
USE WardrobeFlowDB;
GO
IF OBJECT_ID('ParametroSistema') IS NULL
    THROW 50000, 'La base no tiene ParametroSistema: ejecutar primero 00_Instalacion_Completa.sql.', 1;
GO
MERGE ParametroSistema AS t
USING (VALUES (N'ResetDatosDemo', N'1')) AS s(Clave, Valor) ON t.Clave = s.Clave
WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
PRINT 'Pedido de restablecer la demo registrado: ejecutar ahora 00_Instalacion_Completa.sql.';
GO
