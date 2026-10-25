-- ============================================================
-- WardrobeFlow — Restablecer los DATOS DE DEMO (por SQL)
-- ------------------------------------------------------------
-- Lo mismo que el botón de la app Administración → Backup → "Restablecer datos de demo" (que
-- además hace un backup antes y lo deja en la bitácora): BORRA los datos de negocio (clientes,
-- prendas, pedidos, contrataciones, cobros, promociones, lista de espera, cargos y bitácora de
-- negocio) y vuelve a cargar la demo de la instalación con las fechas corridas a HOY (escenarios
-- de PN01–PN04 y 6 meses de historia, incluidos los datos del Centro de decisiones comerciales de
-- PN03: cohortes, recomendaciones, decisiones, metas y promociones para medir; ver
-- BD/00_Instalacion_Completa.sql, sección 21z3b).
-- Usuarios, planes, permisos, traducciones y bitácora del sistema se conservan.
-- Restablecer como máximo 2 días antes de la defensa.
--
-- Uso (HACER UN BACKUP ANTES: se pierden los datos de negocio actuales):
--   1. Ejecutar este script sobre WardrobeFlowDB.
--   2. Abrir la aplicación: inicializa los dígitos verificadores de las tablas que cambiaron.
-- Si la base es de una versión anterior (sin el procedimiento dbo.RestablecerDatosDemo), el script
-- deja el pedido y hay que ejecutar después BD/00_Instalacion_Completa.sql (o reinstalar).
-- ============================================================
USE WardrobeFlowDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
IF OBJECT_ID('ParametroSistema') IS NULL
    THROW 50000, 'La base no tiene ParametroSistema: ejecutar primero 00_Instalacion_Completa.sql.', 1;
GO
IF OBJECT_ID('dbo.RestablecerDatosDemo', 'P') IS NOT NULL
BEGIN
    EXEC dbo.RestablecerDatosDemo @Confirmacion = N'RESTABLECER';
    PRINT 'Datos de demo restablecidos. Abrir la aplicación para que inicialice los dígitos verificadores.';
END
ELSE
BEGIN
    MERGE ParametroSistema AS t
    USING (VALUES (N'ResetDatosDemo', N'1')) AS s(Clave, Valor) ON t.Clave = s.Clave
    WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
    WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
    PRINT 'Pedido de restablecer la demo registrado: ejecutar ahora 00_Instalacion_Completa.sql.';
END
GO
