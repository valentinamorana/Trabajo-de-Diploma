-- ============================================================
-- WardrobeFlow — 06. REDISEÑO DE MENÚ (UX/UI)
-- ------------------------------------------------------------
-- Los ítems nuevos ("mnu.suscriptores", "mnu.ventana") se auto-siembran solos en el primer
-- arranque (BLL.Idioma.SeedearDesdeHardcode → InsertarSiNoExiste), no necesitan SQL.
--
-- Este script solo cubre lo que el auto-seed NO hace: actualizar el TEXTO de una clave que
-- una base YA TENÍA sembrada de antes ("mnu.bitacora" pasa de "Bitácora" a "Analítica").
-- InsertarSiNoExiste es insert-only — nunca pisa una fila existente — así que sin este UPDATE
-- una BD que ya corrió el sistema seguiría mostrando "Bitácora" para siempre.
--
-- Idempotente: un UPDATE al mismo valor no rompe nada si se corre más de una vez.
-- ============================================================

USE WardrobeFlowDB;
GO

UPDATE tr
SET tr.Texto = CASE i.Codigo
    WHEN 'ES' THEN N'Analítica'
    WHEN 'EN' THEN N'Analytics'
    WHEN 'RU' THEN N'Аналитика'
    WHEN 'PT' THEN N'Análise'
    ELSE tr.Texto
END
FROM Traduccion tr
JOIN Control c ON c.IdControl = tr.IdControl
JOIN Idioma  i ON i.IdIdioma  = tr.IdIdioma
WHERE c.Clave = 'mnu.bitacora'
  AND i.Codigo IN ('ES', 'EN', 'RU', 'PT');

PRINT 'mnu.bitacora actualizado a "Analítica" (y equivalentes EN/RU/PT) en las bases que ya lo tenían sembrado.';
GO
