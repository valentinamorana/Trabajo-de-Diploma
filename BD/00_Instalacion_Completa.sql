-- ============================================================
-- WardrobeFlow — INSTALACIÓN COMPLETA (script único, instalación nueva)
-- ------------------------------------------------------------
-- Script único para levantar WardrobeFlowDB de cero: estructura, datos
-- semilla, y TODOS los módulos (Bloque 1, Bloque 3 / Idea de Negocio, y los
-- 4 procesos de negocio PN01-PN04). Este es el ÚNICO script de esquema
-- que se edita de acá en más — no se regenera concatenando archivos.
--
-- Los números de sección (01, 03, 05, 06, 08...21) que aparecen abajo son el
-- orden histórico en el que cada módulo se agregó al proyecto y se conservan
-- como referencia; no indican que falten partes. Este es el ÚNICO script de
-- la carpeta BD/: los scripts individuales ya no existen (su historial está
-- en git).
--
-- Incluye al final (sección 21e) los DATOS DE PRUEBA de todos los procesos,
-- para que la base quede lista para probar apenas se instala.
--
-- Idempotente de punta a punta: correr este archivo dos veces no duplica
-- nada ni rompe datos existentes.
--
-- NOTA DE CODIFICACIÓN: este archivo es UTF-8 y contiene acentos.
--   • En SSMS se ejecuta sin problemas.
--   • Con sqlcmd usar el codepage UTF-8:
--     sqlcmd -S .\SQLEXPRESS -E -f 65001 -i 00_Instalacion_Completa.sql
-- ============================================================


-- ============================================================
-- WardrobeFlow — 01. CREAR BASE DE DATOS DE CERO
-- ------------------------------------------------------------
-- Crea la base WardrobeFlowDB completa: estructura + datos
-- semilla (permisos, roles, usuarios, idiomas) + árbol Composite.
-- Idempotente: se puede re-ejecutar sin romper datos existentes.
--
-- Usuarios semilla: 10 (uno por rol; ver Instalador/Credenciales_Iniciales.txt):
--   admin/administrador1!   
--   vendedor/vendedor1!     deposito/deposito1!
--
-- Sobre una BD ya existente también es seguro: es idempotente y migra lo que haga falta.
--
-- Orden: 1) crear BD  2) tablas  3) seeds  4) migración Composite  5) datos demo
--
-- NOTA DE CODIFICACIÓN: este archivo es UTF-8 y contiene acentos (Básico, Lucía…).
--   • En SSMS se ejecuta sin problemas.
--   • Con sqlcmd usar el codepage UTF-8:  sqlcmd -S .\SQLEXPRESS -E -f 65001 -i 00_Instalacion_Completa.sql
-- ============================================================

-- Opciones de sesión que exigen los índices filtrados y las columnas calculadas. sqlcmd arranca con
-- QUOTED_IDENTIFIER OFF: sin esto, correr el script por segunda vez con sqlcmd fallaba (Msg 1934).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'WardrobeFlowDB')
BEGIN
    BEGIN TRY
        CREATE DATABASE WardrobeFlowDB;
        PRINT 'Base de datos WardrobeFlowDB creada.';
    END TRY
    BEGIN CATCH
        -- En la carpeta de datos quedaron archivos WardrobeFlowDB.mdf/_log.ldf huérfanos (una
        -- desinstalación incompleta, una instancia LocalDB borrada a mano...): CREATE DATABASE no
        -- puede reutilizarlos. Esos archivos pueden tener los DATOS de una instalación anterior, así
        -- que primero se intenta ADJUNTARLOS (y el resto del script los actualiza). Solo si no se
        -- pueden adjuntar (dañados, de otra versión, en uso) se crea la base con archivos de nombre
        -- nuevo, sin tocar los viejos. En los dos casos queda un AVISO en el log del instalador.
        DECLARE @errCreate NVARCHAR(2048) = ERROR_MESSAGE();
        DECLARE @dir NVARCHAR(400) = CONVERT(NVARCHAR(400), SERVERPROPERTY('InstanceDefaultDataPath'));
        IF @dir IS NULL
            SELECT @dir = LEFT(physical_name, LEN(physical_name) - CHARINDEX('\', REVERSE(physical_name)) + 1)
            FROM sys.master_files WHERE database_id = DB_ID('master') AND file_id = 1;
        IF RIGHT(@dir, 1) <> N'\' SET @dir = @dir + N'\';
        DECLARE @sql NVARCHAR(MAX);
        BEGIN TRY
            SET @sql = N'CREATE DATABASE WardrobeFlowDB ON (FILENAME = ''' + REPLACE(@dir, '''', '''''') + N'WardrobeFlowDB.mdf''), ' +
                       N'(FILENAME = ''' + REPLACE(@dir, '''', '''''') + N'WardrobeFlowDB_log.ldf'') FOR ATTACH';
            EXEC (@sql);
            PRINT N'AVISO: se encontraron archivos WardrobeFlowDB.mdf/_log.ldf existentes en ' + @dir +
                  N' y se ADJUNTARON (conservan los datos de una instalación anterior; se actualizan a esta versión).';
        END TRY
        BEGIN CATCH
            DECLARE @errAttach NVARCHAR(2048) = ERROR_MESSAGE();
            DECLARE @sufijo NVARCHAR(20) = FORMAT(GETDATE(), 'yyyyMMddHHmmss');
            SET @sql =
                N'CREATE DATABASE WardrobeFlowDB ON (NAME = WardrobeFlowDB, FILENAME = ''' + REPLACE(@dir, '''', '''''') + N'WardrobeFlowDB_' + @sufijo + N'.mdf'') ' +
                N'LOG ON (NAME = WardrobeFlowDB_log, FILENAME = ''' + REPLACE(@dir, '''', '''''') + N'WardrobeFlowDB_' + @sufijo + N'_log.ldf'')';
            EXEC (@sql);
            PRINT N'AVISO: no se pudo crear la base con los archivos por defecto (' + @errCreate + N') ni adjuntar los ' +
                  N'existentes en ' + @dir + N' (' + @errAttach + N'). Se creó una base NUEVA y vacía con archivos ' +
                  N'WardrobeFlowDB_' + @sufijo + N'.mdf/_log.ldf; los archivos viejos quedaron intactos por si tienen datos.';
        END CATCH
    END CATCH
END
ELSE
    PRINT 'Base de datos WardrobeFlowDB ya existe — se actualiza su contenido.';
GO

USE WardrobeFlowDB;
GO

-- ============================================================
-- REINSTALACIÓN: FOTO DE LOS PERMISOS
-- ------------------------------------------------------------
-- Varias secciones otorgan permisos con "INSERT INTO PermisoRelacion ... WHERE NOT EXISTS" en
-- cada corrida. Sobre una base ya instalada eso devolvía las patentes que el Administrador había
-- quitado desde el Gestor de Perfiles. Acá se guarda cómo estaban los permisos antes de correr el
-- script; al final (sección 21d) se quitan las relaciones que el script volvió a agregar entre
-- roles y patentes que ya existían. Lo nuevo de una actualización (patentes o roles creados en
-- esta corrida) se conserva. Solo aplica si la base ya pasó por una instalación completa.
-- Las migraciones de una sola vez que otorguen permisos van DESPUÉS de esa limpieza (como 21d-4).
-- ============================================================
IF OBJECT_ID('ReinstalacionPermisos', 'U') IS NOT NULL DROP TABLE ReinstalacionPermisos;
-- IF anidados a propósito: SQL Server no corta el AND, y en una instalación nueva ParametroSistema
-- todavía no existe (la consulta fallaría aunque el primer OBJECT_ID diera NULL).
IF OBJECT_ID('ParametroSistema', 'U') IS NOT NULL AND OBJECT_ID('PermisoRelacion', 'U') IS NOT NULL
BEGIN
    -- Ya instalada: la marca quedó en Aplicada (instalación completa) o NoAplica (base anterior a la marca).
    IF EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor IN (N'Aplicada', N'NoAplica'))
    BEGIN
        SELECT IdPadre, IdHijo INTO ReinstalacionPermisos FROM PermisoRelacion;
        DELETE FROM ParametroSistema WHERE Clave = N'ReinstalacionMaxIdPermiso';
        INSERT INTO ParametroSistema (Clave, Valor, Fecha)
        SELECT N'ReinstalacionMaxIdPermiso', CONVERT(NVARCHAR(20), ISNULL(MAX(IdPermiso), 0)), GETDATE() FROM Permiso;
        PRINT 'Reinstalación: se guardó la configuración de permisos actual para respetarla.';
    END
END
GO

-- ============================================================
-- TABLAS BASE
-- ============================================================

-- Usuario
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Usuario')
BEGIN
    CREATE TABLE Usuario (
        IdUsuario        INT           IDENTITY(1,1) PRIMARY KEY,
        Username         NVARCHAR(100) NOT NULL,
        Clave            NVARCHAR(500) NOT NULL,
        Rol              NVARCHAR(100) NULL,
        Perfil           NVARCHAR(100) NULL,
        Estado           BIT           NOT NULL DEFAULT 1,
        IntentosFallidos INT           NOT NULL DEFAULT 0,
        DVH              INT           NULL,
        IdIdioma         VARCHAR(5)    NULL,
        Activo           BIT           NOT NULL DEFAULT 1,   -- RF-10: 1=activo, 0=archivado (baja lógica)
        FechaBaja        DATETIME      NULL,                 -- RF-10: fecha de archivado (para purga >1 año)
        CantidadBloqueos INT           NOT NULL DEFAULT 0,   -- Bloqueo progresivo: nº de bloqueos (define duración)
        FechaBloqueo     DATETIME      NULL,                 -- Bloqueo progresivo: instante del último bloqueo
        RequiereCambioClave BIT        NOT NULL DEFAULT 0,   -- 1 = clave temporal/generada pendiente de cambio
        Nombre           NVARCHAR(100) NULL,                 -- ABM: datos administrativos NO sensibles
        Apellido         NVARCHAR(100) NULL,
        FechaNacimiento  DATE          NULL,
        Email            NVARCHAR(200) NULL,
        CONSTRAINT UQ_Usuario_Username UNIQUE (Username)
    );
    PRINT 'Tabla Usuario creada.';
END
ELSE
    PRINT 'Tabla Usuario ya existe — sin cambios.';
GO

-- Usuario_Seguridad (T07 — tabla ESPEJO de integridad, ver DAL.EspejoUsuario)
-- Copia sombra de los campos que entran al DVH de cada usuario, más su DVH. La app la
-- mantiene en sincronía con cada escritura LEGÍTIMA (junto al recálculo del DVH), lo que
-- permite, ante una manipulación directa en BD: (1) diagnosticar QUÉ campo cambió comparando
-- contra el espejo y (2) REPARAR restaurando el valor legítimo sin necesitar un backup
-- completo. Sin esta tabla, DAL.EspejoUsuario degrada en silencio (ver su propio comentario
-- de TOLERANCIA) y BLL.RecuperacionIntegridad.Diagnosticar() nunca puede ofrecer "Reparar
-- desde Espejo" — antes solo se creaba en un script de migración aparte
-- (ya retirado), y faltaba acá, así que una instalación nueva vía este script nunca
-- la tenía.
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Usuario_Seguridad')
BEGIN
    CREATE TABLE Usuario_Seguridad (
        IdUsuario          INT           PRIMARY KEY,   -- mismo Id que Usuario (NO identity: lo fija la app)
        Username           NVARCHAR(100) NOT NULL,
        Clave              NVARCHAR(500) NOT NULL,
        Rol                NVARCHAR(100) NULL,
        Perfil             NVARCHAR(100) NULL,
        Estado             BIT           NOT NULL DEFAULT 1,
        IntentosFallidos   INT           NOT NULL DEFAULT 0,
        DVH                INT           NULL,
        FechaActualizacion DATETIME      NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Tabla Usuario_Seguridad (espejo de integridad) creada.';
END
ELSE
    PRINT 'Tabla Usuario_Seguridad ya existe — sin cambios.';
GO

-- DVVertical (T07 Dígitos Verificadores)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DVVertical')
BEGIN
    CREATE TABLE DVVertical (
        Id           INT          IDENTITY(1,1) PRIMARY KEY,
        NombreTabla  VARCHAR(100) NOT NULL,
        DVV          INT          NOT NULL,
        FechaCalculo DATETIME     NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_DVVertical_Tabla UNIQUE (NombreTabla)
    );
    PRINT 'Tabla DVVertical creada.';
END
ELSE
    PRINT 'Tabla DVVertical ya existe — sin cambios.';
GO

-- PlanSuscripcion
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PlanSuscripcion')
BEGIN
    CREATE TABLE PlanSuscripcion (
        IdPlan        INT            IDENTITY(1,1) PRIMARY KEY,
        Nombre        NVARCHAR(100)  NOT NULL,
        LimitePrendas INT            NOT NULL DEFAULT 0,
        Precio        DECIMAL(10, 2) NOT NULL DEFAULT 0,
        Estado        BIT            NOT NULL DEFAULT 1
    );
    PRINT 'Tabla PlanSuscripcion creada.';
END
ELSE
    PRINT 'Tabla PlanSuscripcion ya existe — sin cambios.';
GO

-- Permiso (T04 Composite — EsFamilia discrimina Familia vs Patente)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Permiso')
BEGIN
    CREATE TABLE Permiso (
        IdPermiso       INT           IDENTITY(1,1) PRIMARY KEY,
        Nombre          NVARCHAR(100) NOT NULL,
        NombreMenu      NVARCHAR(100) NULL,
        TipoComponente  NVARCHAR(100) NULL,
        Estado          BIT           NOT NULL DEFAULT 1,
        EsFamilia       BIT           NOT NULL DEFAULT 0,
        EsRol           BIT           NOT NULL DEFAULT 0
    );
    PRINT 'Tabla Permiso creada.';
END
ELSE
BEGIN
    -- Si ya existe, asegurar que EsFamilia esté presente (migración v6.0)
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_NAME = 'Permiso' AND COLUMN_NAME = 'EsFamilia')
    BEGIN
        ALTER TABLE Permiso ADD EsFamilia BIT NOT NULL DEFAULT 0;
        PRINT 'Columna EsFamilia agregada a Permiso (migración).';
    END
    -- EsRol: marca los nodos-rol del Composite (migración v7.0 — T04)
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_NAME = 'Permiso' AND COLUMN_NAME = 'EsRol')
    BEGIN
        ALTER TABLE Permiso ADD EsRol BIT NOT NULL DEFAULT 0;
        PRINT 'Columna EsRol agregada a Permiso (migración).';
    END
END
GO

-- Idioma (T05 Multiidioma)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Idioma')
BEGIN
    CREATE TABLE Idioma (
        IdIdioma  INT IDENTITY(1,1) PRIMARY KEY,
        Codigo    VARCHAR(5)    NOT NULL,
        Nombre    NVARCHAR(50)  NOT NULL,
        Activo    BIT           NOT NULL DEFAULT 1,
        EsDefault BIT           NOT NULL DEFAULT 0,
        CONSTRAINT UQ_Idioma_Codigo UNIQUE (Codigo)
    );
    PRINT 'Tabla Idioma creada.';
END
ELSE
    PRINT 'Tabla Idioma ya existe — sin cambios.';
GO

-- Control (claves de traducción — T05)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Control')
BEGIN
    CREATE TABLE Control (
        IdControl  INT IDENTITY(1,1) PRIMARY KEY,
        Clave      VARCHAR(100) NOT NULL,
        Formulario VARCHAR(50)  NOT NULL DEFAULT 'General',
        CONSTRAINT UQ_Control_Clave UNIQUE (Clave)
    );
    PRINT 'Tabla Control creada.';
END
ELSE
    PRINT 'Tabla Control ya existe — sin cambios.';
GO

-- ============================================================
-- TABLAS CON FK
-- ============================================================

-- Bitacora (sistema — refs Usuario)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Bitacora')
BEGIN
    CREATE TABLE Bitacora (
        Id         INT            IDENTITY(1,1) PRIMARY KEY,
        fecha      DATETIME       NOT NULL DEFAULT GETDATE(),
        usuario    INT            NULL REFERENCES Usuario(IdUsuario),
        modulo     NVARCHAR(100)  NULL,
        actividad  NVARCHAR(200)  NULL,
        detalle    NVARCHAR(1000) NULL,
        criticidad INT            NOT NULL DEFAULT 0,
        ip         NVARCHAR(50)   NULL
    );
    PRINT 'Tabla Bitacora creada.';
END
ELSE
    PRINT 'Tabla Bitacora ya existe — sin cambios.';
GO

-- Empleado (refs Usuario — FK nullable: empleado puede no tener usuario del sistema)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Empleado')
BEGIN
    CREATE TABLE Empleado (
        IdEmpleado   INT           IDENTITY(1,1) PRIMARY KEY,
        Nombre       NVARCHAR(100) NOT NULL,
        Apellido     NVARCHAR(100) NOT NULL,
        DNI          NVARCHAR(200) NOT NULL,  -- en texto plano (no se cifra)
        Email        NVARCHAR(200) NULL,
        FechaIngreso DATETIME      NOT NULL DEFAULT GETDATE(),
        Puesto       NVARCHAR(100) NULL,
        Legajo       NVARCHAR(50)  NULL,
        IdUsuario    INT           NULL REFERENCES Usuario(IdUsuario),
        DVH          INT           NULL              -- T07: dígito verificador horizontal
    );
    PRINT 'Tabla Empleado creada.';
END
ELSE
    PRINT 'Tabla Empleado ya existe — sin cambios.';
GO

-- Cliente (refs PlanSuscripcion)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Cliente')
BEGIN
    CREATE TABLE Cliente (
        IdCliente        INT           IDENTITY(1,1) PRIMARY KEY,
        Nombre           NVARCHAR(100) NOT NULL,
        Apellido         NVARCHAR(100) NOT NULL,
        DNI              NVARCHAR(200) NOT NULL,  -- en texto plano: se busca por DNI exacto (PN01)
        Email            NVARCHAR(200) NULL,
        -- El medio de pago preferido (IdMedioPagoPreferido, FK a MedioPago) lo agrega la sección
        -- 20c5: el catálogo MedioPago se crea recién en la 20c.
        IdPlan           INT           NULL REFERENCES PlanSuscripcion(IdPlan),
        FechaAlta        DATETIME      NOT NULL DEFAULT GETDATE(),
        FechaNacimiento  DATE          NOT NULL,  -- obligatoria: validar mayoría de edad
        Activo           BIT           NOT NULL DEFAULT 1,
        DVH              INT           NULL              -- T07: dígito verificador horizontal
    );
    PRINT 'Tabla Cliente creada.';
END
ELSE
    PRINT 'Tabla Cliente ya existe — sin cambios.';
GO

-- Prenda (refs Cliente — FK nullable: prenda disponible no tiene cliente)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Prenda')
BEGIN
    CREATE TABLE Prenda (
        IdPrenda        INT           IDENTITY(1,1) PRIMARY KEY,
        Nombre          NVARCHAR(100) NOT NULL,
        Descripcion     NVARCHAR(500) NULL,
        Talle           NVARCHAR(20)  NULL,
        Color           NVARCHAR(50)  NULL,
        Categoria       NVARCHAR(100) NULL,
        Estado          INT           NOT NULL DEFAULT 0,
        IdClienteActual INT           NULL REFERENCES Cliente(IdCliente),
        FechaAlta       DATETIME      NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Tabla Prenda creada.';
END
ELSE
    PRINT 'Tabla Prenda ya existe — sin cambios.';
GO

-- Pedido (refs Cliente + Empleado)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Pedido')
BEGIN
    CREATE TABLE Pedido (
        IdPedido          INT           IDENTITY(1,1) PRIMARY KEY,
        IdCliente         INT           NOT NULL REFERENCES Cliente(IdCliente),
        IdEmpleado        INT           NOT NULL REFERENCES Empleado(IdEmpleado),
        Estado            INT           NOT NULL DEFAULT 0,
        FechaPedido       DATETIME      NOT NULL DEFAULT GETDATE(),
        FechaDespacho     DATETIME      NULL,
        FechaEntrega      DATETIME      NULL,
        MotivoCancelacion NVARCHAR(500) NULL,
        DVH               INT           NULL              -- T07: DV horizontal (incluye sus líneas)
    );
    PRINT 'Tabla Pedido creada.';
END
ELSE
    PRINT 'Tabla Pedido ya existe — sin cambios.';
GO

-- PedidoPrenda (junction Pedido-Prenda)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PedidoPrenda')
BEGIN
    CREATE TABLE PedidoPrenda (
        IdPedido INT NOT NULL REFERENCES Pedido(IdPedido),
        IdPrenda INT NOT NULL REFERENCES Prenda(IdPrenda),
        CONSTRAINT PK_PedidoPrenda PRIMARY KEY (IdPedido, IdPrenda)
    );
    PRINT 'Tabla PedidoPrenda creada.';
END
ELSE
    PRINT 'Tabla PedidoPrenda ya existe — sin cambios.';
GO

-- PedidoHistorial (auditoría de cambios de pedidos — refs Pedido + Usuario)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PedidoHistorial')
BEGIN
    CREATE TABLE PedidoHistorial (
        IdHistorial   INT            IDENTITY(1,1) PRIMARY KEY,
        IdPedido      INT            NOT NULL REFERENCES Pedido(IdPedido),
        IdOperacion   INT            NOT NULL,
        Fecha         DATETIME       NOT NULL DEFAULT GETDATE(),
        IdUsuario     INT            NULL REFERENCES Usuario(IdUsuario),
        NombreUsuario NVARCHAR(200)  NULL,
        Accion        NVARCHAR(200)  NOT NULL,
        Campo         NVARCHAR(100)  NOT NULL,
        ValorAnterior NVARCHAR(1000) NULL,
        ValorNuevo    NVARCHAR(1000) NULL
    );
    PRINT 'Tabla PedidoHistorial creada.';
END
ELSE
    PRINT 'Tabla PedidoHistorial ya existe — sin cambios.';
GO

-- BitacoraNegocio (eventos de negocio — refs Usuario, Pedido, Prenda, Cliente)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BitacoraNegocio')
BEGIN
    CREATE TABLE BitacoraNegocio (
        IdEvento    INT           IDENTITY(1,1) PRIMARY KEY,
        Fecha       DATETIME      NOT NULL DEFAULT GETDATE(),
        Tipo        NVARCHAR(100) NOT NULL,
        IdUsuario   INT           NULL REFERENCES Usuario(IdUsuario),
        IdPedido    INT           NULL REFERENCES Pedido(IdPedido),
        IdPrenda    INT           NULL REFERENCES Prenda(IdPrenda),
        IdCliente   INT           NULL REFERENCES Cliente(IdCliente),
        Descripcion NVARCHAR(500) NOT NULL
    );
    PRINT 'Tabla BitacoraNegocio creada.';
END
ELSE
    PRINT 'Tabla BitacoraNegocio ya existe — sin cambios.';
GO

-- RolPermiso (asignación PLANA rol→patente) — TABLA DE PASO DEL INSTALADOR.
-- Se usa únicamente para sembrar el árbol: desde estas asignaciones se generan los nodos-rol y
-- las aristas de [PermisoRelacion], que es la ÚNICA fuente de verdad de autorización en runtime.
-- El sistema no la lee ni la escribe; la sección 21z5 la borra al final del script (3FN).
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RolPermiso')
BEGIN
    CREATE TABLE RolPermiso (
        Rol       NVARCHAR(100) NOT NULL,
        IdPermiso INT           NOT NULL REFERENCES Permiso(IdPermiso),
        CONSTRAINT PK_RolPermiso PRIMARY KEY (Rol, IdPermiso)
    );
    PRINT 'Tabla RolPermiso creada.';
END
ELSE
    PRINT 'Tabla RolPermiso ya existe — sin cambios.';
GO

-- PermisoRelacion (árbol Composite padre → hijo — T04)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PermisoRelacion')
BEGIN
    CREATE TABLE PermisoRelacion (
        IdPadre INT NOT NULL REFERENCES Permiso(IdPermiso),
        IdHijo  INT NOT NULL REFERENCES Permiso(IdPermiso),
        CONSTRAINT PK_PermisoRelacion PRIMARY KEY (IdPadre, IdHijo)
    );
    PRINT 'Tabla PermisoRelacion creada.';
END
ELSE
    PRINT 'Tabla PermisoRelacion ya existe — sin cambios.';
GO

-- Traduccion (PK compuesta — T05)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Traduccion')
BEGIN
    CREATE TABLE Traduccion (
        IdControl INT            NOT NULL REFERENCES Control(IdControl),
        IdIdioma  INT            NOT NULL REFERENCES Idioma(IdIdioma),
        Texto     NVARCHAR(1000) NOT NULL DEFAULT '',
        CONSTRAINT PK_Traduccion PRIMARY KEY (IdControl, IdIdioma)
    );
    PRINT 'Tabla Traduccion creada.';
END
ELSE
    PRINT 'Tabla Traduccion ya existe — sin cambios.';
GO

-- HistorialUsuario (snapshots de cambios de usuarios — T06)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HistorialUsuario')
BEGIN
    CREATE TABLE HistorialUsuario (
        IdVersion    INT           IDENTITY(1,1) PRIMARY KEY,
        IdUsuario    INT           NOT NULL REFERENCES Usuario(IdUsuario),
        Fecha        DATETIME      NOT NULL DEFAULT GETDATE(),
        Actor        NVARCHAR(100) NOT NULL,
        Detalle      NVARCHAR(500) NOT NULL,
        UsernameSnap NVARCHAR(100) NOT NULL,
        NombreSnap   NVARCHAR(100) NULL,        -- Snapshots de datos administrativos NO sensibles
        ApellidoSnap NVARCHAR(100) NULL,
        FechaNacSnap DATE          NULL,
        EmailSnap    NVARCHAR(200) NULL,
        ClaveSnap    NVARCHAR(500) NOT NULL,    -- Trazabilidad interna: nunca se muestra ni se restaura
        EstadoSnap   BIT           NOT NULL,
        IntentosSnap INT           NOT NULL
    );
    PRINT 'Tabla HistorialUsuario creada.';
END
ELSE
    PRINT 'Tabla HistorialUsuario ya existe — sin cambios.';
GO

-- HistorialIntegridad (historial de verificaciones de integridad DV — T07)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HistorialIntegridad')
BEGIN
    CREATE TABLE HistorialIntegridad (
        Id                INT          IDENTITY(1,1) PRIMARY KEY,
        NombreTabla       VARCHAR(100) NOT NULL,
        DVVAlmacenado     INT          NULL,
        DVVCalculado      INT          NOT NULL,
        Resultado         BIT          NOT NULL,
        FilasCorruptas    INT          NOT NULL DEFAULT 0,
        FechaVerificacion DATETIME     NOT NULL DEFAULT GETDATE(),
        DisparadoPor      VARCHAR(50)  NOT NULL
            CONSTRAINT CHK_HistInteg_Origen CHECK (DisparadoPor IN ('Arranque', 'Timer', 'Manual'))
    );
    PRINT 'Tabla HistorialIntegridad creada.';
END
ELSE
    PRINT 'Tabla HistorialIntegridad ya existe — sin cambios.';
GO

-- ClaveRecuperacion (claves de emergencia de 1 solo uso para desbloquear un Administrador)
-- Las claves se guardan HASHEADAS (PBKDF2); el .txt con las claves en texto plano es la
-- copia física del admin (como los códigos de respaldo de Steam / 2FA).
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClaveRecuperacion')
BEGIN
    CREATE TABLE ClaveRecuperacion (
        IdClave       INT           IDENTITY(1,1) PRIMARY KEY,
        ClaveHash     NVARCHAR(500) NOT NULL,            -- PBKDF2-SHA256 (igual que Usuario.Clave)
        Usada         BIT           NOT NULL DEFAULT 0,  -- 1 = ya consumida (uso único)
        UsadaPor      NVARCHAR(100) NULL,                -- username del admin que la canjeó
        FechaUso      DATETIME      NULL,
        FechaCreacion DATETIME      NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Tabla ClaveRecuperacion creada.';
END
ELSE
    PRINT 'Tabla ClaveRecuperacion ya existe — sin cambios.';
GO

-- Preferencia (preferencias de UI por usuario: fuente, tamaño, tema, formato de fecha…).
-- ON DELETE CASCADE: al purgar físicamente un usuario, su fila de preferencias se borra sola.
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Preferencia')
BEGIN
    CREATE TABLE Preferencia (
        IdUsuario      INT          NOT NULL PRIMARY KEY,
        FuenteFamilia  NVARCHAR(50) NULL,            -- "Segoe UI", "Verdana", "Calibri"…
        FuenteTamano   VARCHAR(10)  NULL,            -- 'Chico' / 'Normal' / 'Grande'
        Tema           VARCHAR(20)  NULL,            -- 'Claro' / 'Oscuro'
        FormatoFecha   VARCHAR(20)  NULL,            -- 'dd/MM/yyyy', 'yyyy-MM-dd'…
        Notificaciones BIT          NULL,            -- placeholder (se guarda; sin efecto funcional aún)
        CONSTRAINT FK_Preferencia_Usuario FOREIGN KEY (IdUsuario)
            REFERENCES Usuario(IdUsuario) ON DELETE CASCADE
    );
    PRINT 'Tabla Preferencia creada.';
END
ELSE
    PRINT 'Tabla Preferencia ya existe — sin cambios.';
GO

-- ============================================================
-- PARÁMETROS DEL SISTEMA / MARCAS DE MIGRACIÓN
-- ParametroSistema guarda marcas estables de pasos que deben correr UNA sola vez
-- (no en cada reinstalación/actualización): siembra de usuarios, migraciones de roles,
-- datos de prueba. Idempotente.
--
-- 'SemillaUsuarios': las cuentas semilla/demo (y sus Empleado/Cliente demo) se crean SOLO en
-- una instalación NUEVA. Se decide acá, antes de sembrar nada: si la tabla Usuario está vacía
-- es una base nueva ('Pendiente'); si ya tiene usuarios es una actualización ('NoAplica') y NO
-- se vuelve a crear ninguna cuenta. Antes, una actualización recreaba 'admin' (o cualquier usuario
-- demo renombrado/borrado en producción) con la clave PUBLICADA y DVH = 0, lo que además dejaba
-- la tabla "mezclada" y la app entraba en modo mantenimiento. La sección final del script pasa la
-- marca a 'Aplicada'. Los roles y patentes nuevos SÍ se crean siempre (son esquema, no cuentas).
-- Si una versión futura necesitara un usuario semilla NUEVO en una actualización, tiene que
-- insertarlo con un DVH válido (calculado como lo hace la app), no con DVH = 0.
-- ============================================================
IF OBJECT_ID('ParametroSistema','U') IS NULL CREATE TABLE ParametroSistema (Clave NVARCHAR(100) NOT NULL PRIMARY KEY, Valor NVARCHAR(400) NULL, Fecha DATETIME NOT NULL DEFAULT GETDATE());
GO
IF NOT EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios')
BEGIN
    INSERT INTO ParametroSistema (Clave, Valor, Fecha)
    SELECT N'SemillaUsuarios', CASE WHEN EXISTS (SELECT 1 FROM Usuario) THEN N'NoAplica' ELSE N'Pendiente' END, GETDATE();
    IF EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor = N'NoAplica')
        PRINT 'Base existente: no se crean usuarios semilla/demo (solo se actualiza el esquema).';
END
GO

-- ============================================================
-- SEEDS INICIALES
-- ============================================================

-- ── Permisos: PATENTES (permisos simples) ───────────────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT v.Nombre, v.NombreMenu, v.Tipo, 1, 0, 0
FROM (VALUES
    ('Gestionar Usuarios',          'mnuUsuarios',           'Sistema'),
    ('Ver Auditoria',               'mnuAuditoria',          'Sistema'),
    ('Ver Prendas',                 'mnuPrendas',            'Inventario'),
    ('Gestionar Stock',             'mnuStock',              'Inventario'),
    ('Gestionar Clientes',          'mnuClientes',           'Ventas'),
    ('Gestionar PlanSuscripciones', 'mnuPlanSuscripciones',  'Ventas'),
    ('Gestionar Renovaciones',      'mnuRenovacionSuscripcion', 'Ventas'),
    ('Gestionar Cobros',            'mnuCobroSuscripcion',   'Ventas'),
    ('Realizar Ventas',             'mnuPedidosVenta',       'Ventas'),
    ('Ver Pedidos Realizados',      'mnuPedidosRealizados',  'Ventas'),
    (N'Ver Análisis de Abandono',    'mnuAnalisisAbandono',   'Sistema'),
    ('Ver Ventas por Vendedor',     'mnuVentasVendedor',     'Sistema'),
    (N'Ver Rotación de Prendas',     'mnuAnalisisRotacion',   'Sistema'),
    ('Ver Tiempos de Mantenimiento','mnuAnalisisMantenimiento', 'Sistema'),
    ('Ver Escasez de Stock',        'mnuAnalisisEscasez',    'Sistema'),
    (N'Ver Recomendación de Prendas','mnuRecomendacionPrendas', 'Sistema')
) AS v(Nombre, NombreMenu, Tipo)
WHERE NOT EXISTS (SELECT 1 FROM Permiso p
                  WHERE p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0);
PRINT 'Patentes (permisos simples) inicializadas.';
GO

-- ── Renombre de rol alineado a los procesos de negocio ───────────────────────
-- OperadorDeInventario → Deposito (PN01/PN04 lo llaman "Depósito"). En una BD nueva
-- no hace nada; en una BD ya instalada migra usuarios, asignaciones y nodo-rol.
-- Va ANTES de sembrar RolPermiso para no duplicar el nodo-rol.
IF EXISTS (SELECT 1 FROM Usuario WHERE Rol = 'OperadorDeInventario'
                                    OR Perfil IN ('OperadorDeInventario','Operador de Inventario'))
BEGIN
    UPDATE Usuario SET Rol = 'Deposito', Perfil = 'Deposito'
    WHERE Rol = 'OperadorDeInventario' OR Perfil IN ('OperadorDeInventario','Operador de Inventario');
    -- El DVH incluye el Rol: se resetea para que la app lo recalcule limpio en el próximo arranque.
    UPDATE Usuario SET DVH = 0;
    UPDATE DVVertical SET DVV = 0 WHERE NombreTabla = 'Usuario';
    PRINT 'Usuarios migrados: OperadorDeInventario → Deposito.';
END
UPDATE RolPermiso SET Rol = 'Deposito' WHERE Rol = 'OperadorDeInventario';
UPDATE Permiso SET Nombre = 'Deposito', NombreMenu = 'Deposito'
WHERE EsRol = 1 AND Nombre = 'OperadorDeInventario'
  AND NOT EXISTS (SELECT 1 FROM Permiso WHERE EsRol = 1 AND Nombre = 'Deposito');
GO

-- ── Asignación rol → patente (RolPermiso) ────────────────────────────────────
-- Se asigna por NombreMenu para no depender de IDs de identidad.
INSERT INTO RolPermiso (Rol, IdPermiso)
SELECT r.Rol, p.IdPermiso
FROM (VALUES
    -- Administrador: acceso total
    ('Administrador','mnuUsuarios'),('Administrador','mnuAuditoria'),
    ('Administrador','mnuPrendas'),
    ('Administrador','mnuStock'),
    ('Administrador','mnuClientes'),('Administrador','mnuPlanSuscripciones'),
    ('Administrador','mnuRenovacionSuscripcion'),
    ('Administrador','mnuCobroSuscripcion'),
    ('Administrador','mnuAnalisisAbandono'),
    ('Administrador','mnuVentasVendedor'),('Administrador','mnuAnalisisRotacion'),
    ('Administrador','mnuAnalisisMantenimiento'),('Administrador','mnuAnalisisEscasez'),
    ('Administrador','mnuRecomendacionPrendas'),
    ('Administrador','mnuPedidosVenta'),('Administrador','mnuPedidosRealizados'),
    -- Vendedor: prendas + clientes + planes + ventas
    ('Vendedor','mnuPrendas'),('Vendedor','mnuClientes'),
    ('Vendedor','mnuPlanSuscripciones'),('Vendedor','mnuRenovacionSuscripcion'),
    ('Vendedor','mnuPedidosVenta'),
    -- Deposito: solo despacho
    ('Deposito','mnuPedidosRealizados')
) AS r(Rol, NombreMenu)
JOIN Permiso p ON p.NombreMenu = r.NombreMenu AND ISNULL(p.EsFamilia,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM RolPermiso x WHERE x.Rol = r.Rol AND x.IdPermiso = p.IdPermiso);
PRINT 'Asignaciones rol→patente inicializadas.';
GO

-- ── Usuarios iniciales (clave hasheada PBKDF2; ver Instalador/Credenciales_Iniciales.txt) ───────────
-- admin/administrador1!  vendedor/vendedor1!  deposito/deposito1!
-- DVH=0 → la app recalcula el DV en el primer arranque.
INSERT INTO Usuario (Username, Clave, Rol, Perfil, Estado, IntentosFallidos, DVH, IdIdioma)
SELECT v.Username, v.Clave, v.Rol, v.Perfil, 1, 0, 0, 'ES'
FROM (VALUES
    ('admin',      '3ZTrmLBPYN+Dr4uWxFV6gfhtzhqVjnLEaPuUd2v+MNHwAaWlmPPfHwmMMwS0bZuP', 'Administrador',        'Administrador'),
    ('vendedor',   'VWyQxHK8Dxr+BBWgw63IMTgFG91ZeDZSxRtj5FIpH9qxHbayJVLUBFpErIgLdOmZ', 'Vendedor',             'Vendedor'),
    ('deposito',   'xL86BMbo9P5XpIZ7fW+jdMVNsgP+jhQykYOFpbClQoSsU44mv9HKYdU1aDgp4cBV', 'Deposito', 'Deposito')
) AS v(Username, Clave, Rol, Perfil)
WHERE NOT EXISTS (SELECT 1 FROM Usuario u WHERE u.Username = v.Username)
  AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor = N'Pendiente');  -- solo en instalación nueva
PRINT 'Usuarios iniciales: verificados.';
GO

-- Idiomas (ES, EN, RU)
INSERT INTO Idioma (Codigo, Nombre, Activo, EsDefault)
SELECT v.Codigo, v.Nombre, v.Activo, v.EsDefault
FROM (VALUES
    ('ES', N'Español', 1, 1),
    ('EN', N'English', 1, 0),
    ('RU', N'Русский', 1, 0),
    ('PT', N'Português', 1, 0)   -- la app también lo crea al sembrar traducciones; acá queda para la FK de Usuario.IdIdioma
) AS v(Codigo, Nombre, Activo, EsDefault)
WHERE NOT EXISTS (SELECT 1 FROM Idioma WHERE Codigo = v.Codigo);
PRINT 'Idiomas inicializados (ES, EN, RU, PT).';
GO

-- DVV inicial para la tabla Usuario (en 0 — recalcular desde la app)
IF NOT EXISTS (SELECT 1 FROM DVVertical WHERE NombreTabla = 'Usuario')
BEGIN
    INSERT INTO DVVertical (NombreTabla, DVV, FechaCalculo)
    VALUES ('Usuario', 0, GETDATE());
    PRINT 'DVV inicial insertado para tabla Usuario.';
END
GO

-- DVH = 0 para usuarios existentes sin DVH
UPDATE Usuario SET DVH = 0 WHERE DVH IS NULL;
GO

-- IdIdioma = ES para usuarios existentes sin preferencia
UPDATE Usuario SET IdIdioma = 'ES' WHERE IdIdioma IS NULL;
GO

-- ============================================================
-- MIGRACIÓN COMPOSITE (T04)
-- Genera nodos Familia desde TipoComponente si no existen aún.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM Permiso WHERE EsFamilia = 1)
BEGIN
    DECLARE @mapa TABLE (Grupo NVARCHAR(100), IdFamilia INT);

    INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia)
    OUTPUT INSERTED.Nombre, INSERTED.IdPermiso INTO @mapa (Grupo, IdFamilia)
    SELECT DISTINCT
        TipoComponente,
        TipoComponente,
        TipoComponente,
        1,
        1
    FROM Permiso
    WHERE TipoComponente IS NOT NULL
      AND LTRIM(RTRIM(TipoComponente)) <> ''
      AND EsFamilia = 0;

    INSERT INTO PermisoRelacion (IdPadre, IdHijo)
    SELECT m.IdFamilia, p.IdPermiso
    FROM   Permiso p
    INNER JOIN @mapa m ON p.TipoComponente = m.Grupo
    WHERE  p.EsFamilia = 0;

    PRINT 'Árbol Composite generado desde grupos TipoComponente.';
END
ELSE
    PRINT 'Árbol Composite ya inicializado — sin cambios.';
GO

-- ============================================================
-- MIGRACIÓN COMPOSITE (T04) — Roles como NODOS del árbol
-- A partir de v7.0 [PermisoRelacion] es la única fuente de verdad de la
-- composición. Se crea un nodo-rol por cada rol de [RolPermiso] y se migran
-- las asignaciones planas a aristas rol→permiso.
-- ============================================================

-- Crear nodo-rol faltante por cada rol existente en RolPermiso
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT DISTINCT rp.Rol, rp.Rol, 'Rol', 1, 1, 1
FROM   RolPermiso rp
WHERE  NOT EXISTS (SELECT 1 FROM Permiso p WHERE p.Nombre = rp.Rol AND p.EsRol = 1);

-- Migrar asignaciones planas a aristas Composite. Corre UNA sola vez por base (marca
-- 'MigracionRolPermiso', que se cierra en la sección de mnuRenovacionSuscripcion): PermisoRelacion
-- es la única fuente de verdad y RolPermiso es legacy. Si corriera siempre, cada reinstalación le
-- devolvería a un rol las patentes que el Administrador le quitó (y esas aristas nuevas, sin dígito
-- verificador, darían una falsa alarma de integridad).
IF NOT EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'MigracionRolPermiso')
BEGIN
    INSERT INTO PermisoRelacion (IdPadre, IdHijo)
    SELECT pr.IdPermiso, rp.IdPermiso
    FROM   RolPermiso rp
    INNER JOIN Permiso pr ON pr.Nombre = rp.Rol AND pr.EsRol = 1 AND pr.IdPermiso <> rp.IdPermiso
    WHERE  NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                       WHERE x.IdPadre = pr.IdPermiso AND x.IdHijo = rp.IdPermiso);
    PRINT 'Nodos-rol y aristas rol→permiso migrados (T04 v7.0).';
END
GO

-- ============================================================
-- JERARQUÍA DE ROLES (T04) — roles nuevos con vistas + rol-dentro-de-rol
-- Idempotente. Crea los nodos-rol nuevos, les asigna sus patentes (vistas)
-- propias y arma las aristas rol→rol para que el padre HEREDE recursivamente
-- las vistas del hijo (demostración real del Composite con jerarquía de roles).
--
--   Auditor                                → Auditoría
--   GerenteComercial  ⊃ Vendedor           → (Vendedor) + Pedidos Realizados
--   GerenteInventario ⊃ OperadorLogistico + Deposito → despacho + prendas + stock + reportes
-- ============================================================

-- 1) Nodos-rol para los roles nuevos (si faltan).
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT v.Rol, v.Rol, 'Rol', 1, 1, 1
FROM (VALUES
    ('Auditor'), ('GerenteComercial'), ('OperadorLogistico'),
    ('GerenteInventario')
) AS v(Rol)
WHERE NOT EXISTS (SELECT 1 FROM Permiso p WHERE p.Nombre = v.Rol AND p.EsRol = 1);
GO

-- 2) Patentes PROPIAS (vistas directas) de cada rol nuevo (aristas rol→patente).
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Auditor',           'mnuAuditoria'),
    ('GerenteComercial',  'mnuPedidosRealizados'),
    ('GerenteComercial',  'mnuAnalisisAbandono'),
    ('GerenteComercial',  'mnuVentasVendedor'),
    ('OperadorLogistico', 'mnuPedidosRealizados'),
    ('GerenteInventario', 'mnuAnalisisRotacion'),
    ('GerenteInventario', 'mnuAnalisisMantenimiento'),
    ('GerenteInventario', 'mnuAnalisisEscasez'),
    ('Vendedor',          'mnuRecomendacionPrendas')
) AS v(Rol, NombreMenu)
INNER JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
INNER JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu
                       AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
GO

-- 3) Jerarquía rol→rol: el padre hereda recursivamente las vistas del hijo.
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT padre.IdPermiso, hijo.IdPermiso
FROM (VALUES
    ('GerenteComercial',  'Vendedor'),
    ('GerenteInventario', 'OperadorLogistico'),
    ('GerenteInventario', 'Deposito')
) AS v(Padre, Hijo)
INNER JOIN Permiso padre ON padre.Nombre = v.Padre AND padre.EsRol = 1
INNER JOIN Permiso hijo  ON hijo.Nombre  = v.Hijo  AND hijo.EsRol  = 1
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = padre.IdPermiso AND x.IdHijo = hijo.IdPermiso);
GO

-- ============================================================
-- Idempotente. Crea un usuario demo por cada rol nuevo (claves en Instalador/Credenciales_Iniciales.txt).
-- Roles compuestos por HERENCIA: GerenteComercial ⊃ Vendedor y GerenteInventario ⊃ OperadorLogistico + Deposito.
-- Los permisos efectivos se obtienen del árbol Composite (no se copian).
-- Las claves se guardan hasheadas (PBKDF2). Texto plano: usuario1! (ver Instalador/Credenciales_Iniciales.txt).
-- ============================================================

INSERT INTO Usuario (Username, Clave, Rol, Perfil, Estado, IntentosFallidos, DVH, IdIdioma, Activo)
SELECT v.Username, v.Clave, v.Rol, v.Perfil, 1, 0, 0, 'ES', 1
FROM (VALUES
  ('auditor',     'SmnGp5hqSC+FXdbLiccYieNpC6vEaWn6nVpgZFrlyyeOXqx8yTjJYOgaw+oXAP7B', 'Auditor',           'Auditor'),
  ('gcomercial',  '1KWgyl8MkMcuisigf4fRBDb2f803LIsA/hvfKpzfwcMbRboUiS5CwuvFQq64GJft', 'GerenteComercial',  'GerenteComercial'),
  ('ginventario', 'G89lxogCulMeAK+WA5rNHSyWpuz5QKF/DBH8PSfiPbUv5SBKStFVQYXylikq+OMw', 'GerenteInventario', 'GerenteInventario'),
  ('logistico',   'U3g703lDqDJfLgaXpOaNiAQpDOaBSq1LUMzEu3X7x8hh6097jTcMUNAsPfl1SCVG', 'OperadorLogistico', 'OperadorLogistico')
) AS v(Username, Clave, Rol, Perfil)
WHERE NOT EXISTS (SELECT 1 FROM Usuario u WHERE u.Username = v.Username)
  AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor = N'Pendiente');  -- solo en instalación nueva
PRINT 'Usuarios demo de roles nuevos inicializados.';
GO

-- ── Datos administrativos (NO sensibles) de los usuarios semilla — ABM ───────
-- Solo completa los que estén vacíos (idempotente). Habilita búsqueda por nombre/apellido/email.
UPDATE u SET u.Nombre = v.Nombre, u.Apellido = v.Apellido, u.Email = v.Email, u.FechaNacimiento = v.FechaNac
FROM Usuario u
JOIN (VALUES
    ('admin',       N'Admin',     N'Sistema',      'admin@wardrobeflow.com',       '1985-01-15'),
    ('vendedor',    N'Valentina', N'Morana',       'vendedor@wardrobeflow.com',    '1995-06-20'),
    ('deposito',    N'Oscar',     N'Pérez',        'deposito@wardrobeflow.com',    '1990-03-10'),
    ('auditor',     N'Ana',       N'Díaz',         'auditor@wardrobeflow.com',     '1988-09-05'),
    ('gcomercial',  N'Gabriel',   N'Morán',        'gcomercial@wardrobeflow.com',  '1983-11-25'),
    ('ginventario', N'Gisela',    N'Ortiz',        'ginventario@wardrobeflow.com', '1986-07-30'),
    ('logistico',   N'Lucas',     N'Gómez',        'logistico@wardrobeflow.com',   '1992-02-18')
) AS v(Username, Nombre, Apellido, Email, FechaNac) ON u.Username = v.Username
WHERE u.Nombre IS NULL AND u.Apellido IS NULL
  AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor = N'Pendiente');  -- solo en instalación nueva
PRINT 'Datos administrativos de usuarios semilla aplicados.';
GO


-- ============================================================
-- CONSOLIDACIÓN DE ROLES (v8) — decisión de diseño 2da entrega
-- Lleva la jerarquía al ESTADO OBJETIVO (idempotente, sin importar el estado previo):
--   • Inventario — dos operadores con responsabilidades CLARAS, sin duplicados:
--       - OperadorLogistico    → pedidos / despacho       (Ver Pedidos Realizados)
--       - Deposito → mantenimiento de prendas (Ver Prendas + Gestionar Stock)
--       - GerenteInventario ⊃ AMBOS                       (+ Categorías + Outfits)
--     Se RETIRAN EncargadoDeStock y ControladorDeStock (eran redundantes con lo anterior).
--   • Comercial — se RETIRA Supervisor; el jefe es GerenteComercial ⊃ Vendedor.
-- Este bloque SUPERSEDE los bloques previos para los nodos afectados.
-- ============================================================

-- (a) Asegurar el nodo-rol Deposito (por si la BD no lo tenía).
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Deposito', 'Deposito', 'Rol', 1, 1, 1
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE Nombre = 'Deposito' AND EsRol = 1);
GO

-- (b)-(e) son una MIGRACIÓN de una sola vez (marca ParametroSistema 'ConsolidacionRolesV8'):
-- antes corrían en cada reinstalación/actualización y borraban las personalizaciones que el
-- Administrador hubiera hecho al rol Deposito desde el Gestor de Perfiles.
-- (b) Deposito: sus patentes propias quedan en Prendas + Stock + Pedidos Realizados (Ver; la de
--     Editar la asegura la sección 21b). Se quita cualquier otra patente vieja y se agregan las nuevas.
--     Las patentes de secciones posteriores (Lista de Espera, Inspección, Control de Stock) se
--     vuelven a asignar más abajo, en esas mismas secciones.
IF NOT EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'ConsolidacionRolesV8')
BEGIN
    DELETE r FROM PermisoRelacion r
    JOIN Permiso rol ON rol.IdPermiso = r.IdPadre AND rol.Nombre = 'Deposito' AND rol.EsRol = 1
    JOIN Permiso pat ON pat.IdPermiso = r.IdHijo  AND ISNULL(pat.EsRol,0) = 0
    WHERE pat.NombreMenu NOT IN ('mnuPrendas','mnuStock','mnuStockEditar','mnuPedidosRealizados',
                                'mnuPedidosRealizadosEditar','mnuListaEspera','mnuListaEsperaEditar',
                                'mnuInspeccionDevolucion','mnuControlStock','mnuControlStockEditar');
    INSERT INTO PermisoRelacion (IdPadre, IdHijo)
    SELECT rol.IdPermiso, pat.IdPermiso
    FROM (VALUES ('mnuPrendas'), ('mnuStock'), ('mnuPedidosRealizados')) AS v(NombreMenu)
    JOIN Permiso rol ON rol.Nombre = 'Deposito' AND rol.EsRol = 1
    JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
    WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
END
GO

-- (c) GerenteInventario ⊃ OperadorLogistico + Deposito (y se quita la arista vieja a EncargadoDeStock).
IF NOT EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'ConsolidacionRolesV8')
BEGIN
    DELETE r FROM PermisoRelacion r
    JOIN Permiso gi ON gi.IdPermiso = r.IdPadre AND gi.Nombre = 'GerenteInventario' AND gi.EsRol = 1
    JOIN Permiso ed ON ed.IdPermiso = r.IdHijo  AND ed.Nombre = 'EncargadoDeStock'  AND ed.EsRol = 1;
    INSERT INTO PermisoRelacion (IdPadre, IdHijo)
    SELECT gi.IdPermiso, h.IdPermiso
    FROM (VALUES ('OperadorLogistico'), ('Deposito')) AS v(Hijo)
    JOIN Permiso gi ON gi.Nombre = 'GerenteInventario' AND gi.EsRol = 1
    JOIN Permiso h  ON h.Nombre  = v.Hijo AND h.EsRol = 1
    WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x WHERE x.IdPadre = gi.IdPermiso AND x.IdHijo = h.IdPermiso);
END
GO

-- (d) Migrar usuarios de los roles que se retiran ANTES de desactivarlos.
DECLARE @rolesCambiados INT = 0;
IF NOT EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'ConsolidacionRolesV8')
BEGIN
    UPDATE Usuario SET Rol = 'Deposito', Perfil = 'Deposito'
    WHERE Rol IN ('EncargadoDeStock','ControladorDeStock') OR Perfil IN ('EncargadoDeStock','ControladorDeStock','Controlador de Stock');
    SET @rolesCambiados = @rolesCambiados + @@ROWCOUNT;
    UPDATE Usuario SET Rol = 'GerenteComercial', Perfil = 'GerenteComercial'
    WHERE Rol = 'Supervisor' OR Perfil = 'Supervisor';
    SET @rolesCambiados = @rolesCambiados + @@ROWCOUNT;

-- (e) Retirar los roles redundantes: quitar TODAS sus aristas (como padre o como hijo) y desactivar el nodo.
    DELETE r FROM PermisoRelacion r
    JOIN Permiso p ON (p.IdPermiso = r.IdPadre OR p.IdPermiso = r.IdHijo)
    WHERE p.EsRol = 1 AND p.Nombre IN ('EncargadoDeStock','ControladorDeStock','Supervisor');
    UPDATE Permiso SET Estado = 0 WHERE EsRol = 1 AND Nombre IN ('EncargadoDeStock','ControladorDeStock','Supervisor');

    INSERT INTO ParametroSistema (Clave, Valor, Fecha)
    VALUES (N'ConsolidacionRolesV8', N'Consolidación de roles v8 aplicada (bloques b-e)', GETDATE());
END

-- (f) Si cambió el Rol de algún usuario, el DVH (que incluye el Rol) quedó desfasado:
--     resetear el DV de Usuario para que la app lo recalcule limpio en el próximo arranque.
IF @rolesCambiados > 0
BEGIN
    UPDATE Usuario SET DVH = 0;
    UPDATE DVVertical SET DVV = 0 WHERE NombreTabla = 'Usuario';
    PRINT 'Roles consolidados; DV de Usuario reseteado para recálculo en el próximo arranque.';
END
PRINT 'Consolidación de roles (v8) aplicada.';
GO

-- ============================================================
-- Simplificación de Permisos — ELIMINAR FAMILIAS del árbol Composite
-- Decisión de revisión: los permisos (patentes) son un catálogo fijo y las FAMILIAS se
-- retiran de la experiencia. Para no perder permisos efectivos: (1) se materializan las
-- relaciones Rol→Patente alcanzables a través de familias, (2) se quitan las aristas que
-- tocan familias y (3) se desactivan los nodos Familia. El anidamiento Rol→Rol se preserva,
-- de modo que el patrón Composite sigue vigente (Rol = nodo compuesto, Patente = hoja).
-- Idempotente: si no hay familias activas, no hace nada.
-- ============================================================
IF EXISTS (SELECT 1 FROM Permiso WHERE ISNULL(EsFamilia,0)=1 AND ISNULL(EsRol,0)=0 AND Estado=1)
BEGIN
    -- (1) Patentes alcanzables desde cada Rol descendiendo SOLO por familias (no por roles).
    ;WITH Arbol AS (
        SELECT r.IdPermiso AS IdRol, pr.IdHijo AS IdNodo
        FROM Permiso r
        JOIN PermisoRelacion pr ON pr.IdPadre = r.IdPermiso
        WHERE r.EsRol = 1
        UNION ALL
        SELECT a.IdRol, pr.IdHijo
        FROM Arbol a
        JOIN Permiso n ON n.IdPermiso = a.IdNodo AND ISNULL(n.EsFamilia,0)=1 AND ISNULL(n.EsRol,0)=0
        JOIN PermisoRelacion pr ON pr.IdPadre = a.IdNodo
    )
    INSERT INTO PermisoRelacion (IdPadre, IdHijo)
    SELECT DISTINCT a.IdRol, a.IdNodo
    FROM Arbol a
    JOIN Permiso p ON p.IdPermiso = a.IdNodo AND ISNULL(p.EsFamilia,0)=0 AND ISNULL(p.EsRol,0)=0
    WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x WHERE x.IdPadre=a.IdRol AND x.IdHijo=a.IdNodo)
    OPTION (MAXRECURSION 50);

    -- (2) Quitar todas las aristas que toquen una Familia (como padre o como hijo).
    DELETE r FROM PermisoRelacion r
    JOIN Permiso p ON (p.IdPermiso = r.IdPadre OR p.IdPermiso = r.IdHijo)
    WHERE ISNULL(p.EsFamilia,0)=1 AND ISNULL(p.EsRol,0)=0;

    -- (3) Desactivar los nodos Familia (no se borran: conservan trazabilidad/FKs).
    UPDATE Permiso SET Estado = 0 WHERE ISNULL(EsFamilia,0)=1 AND ISNULL(EsRol,0)=0;

    PRINT 'Familias eliminadas del Composite: roles aplanados a Rol->Patente.';
END
ELSE
    PRINT 'No hay familias activas — árbol ya aplanado.';
GO

-- ============================================================
-- Etapa 4 — Permisos a nivel de CONTROL (mapeo patente ↔ control de un formulario)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ControlMapeado')
BEGIN
    CREATE TABLE ControlMapeado (
        IdControlMapeado INT           IDENTITY(1,1) PRIMARY KEY,
        IdPermiso        INT           NOT NULL,
        Formulario       NVARCHAR(100) NOT NULL,
        NombreControl    NVARCHAR(100) NOT NULL,
        CONSTRAINT FK_ControlMapeado_Permiso FOREIGN KEY (IdPermiso) REFERENCES Permiso(IdPermiso),
        CONSTRAINT UQ_ControlMapeado UNIQUE (Formulario, NombreControl)
    );
    PRINT 'Tabla ControlMapeado creada (Etapa 4 - permisos por control).';
END
ELSE
    PRINT 'Tabla ControlMapeado ya existe — sin cambios.';
GO

-- Seed inicial: mapea cada patente con NombreMenu al item de menu correspondiente del Menu
-- principal, como punto de partida coherente con la visibilidad actual. Idempotente.
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT ISNULL(MIN(CASE WHEN p.Estado = 1 THEN p.IdPermiso END), MIN(p.IdPermiso)), v.Formulario, v.NombreControl
FROM (VALUES
    ('mnuPrendas',           'Menu', 'prendasToolStripMenuItem'),
    ('mnuClientes',          'Menu', 'clientesToolStripMenuItem'),
    ('mnuPlanSuscripciones', 'Menu', 'planesToolStripMenuItem'),
    ('mnuRenovacionSuscripcion', 'Menu', 'renovacionSuscripcionToolStripMenuItem'),
    ('mnuCobroSuscripcion',  'Menu', 'cobroSuscripcionToolStripMenuItem'),
    ('mnuAnalisisAbandono',  'Menu', 'analisisAbandonoToolStripMenuItem'),
    ('mnuPedidosVenta',      'Menu', 'pedidosVentaToolStripMenuItem'),
    ('mnuPedidosRealizados', 'Menu', 'pedidosRealizadosToolStripMenuItem'),
    ('mnuUsuarios',          'Menu', 'usuariosToolStripMenuItem'),
    ('mnuAuditoria',         'Menu', 'bitacoraToolStripMenuItem')
) AS v(NombreMenu, Formulario, NombreControl)
INNER JOIN Permiso p ON p.NombreMenu = v.NombreMenu
                    AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM ControlMapeado x
                  WHERE x.Formulario = v.Formulario AND x.NombreControl = v.NombreControl)
GROUP BY v.Formulario, v.NombreControl;
PRINT 'Seed de ControlMapeado (mapeos de menu) aplicado.';
GO

-- Si se insertaron usuarios nuevos en una BD ya inicializada, resetear el DV para que la app
-- lo recalcule limpio en el próximo arranque (evita una falsa alarma de integridad por mezcla).
IF EXISTS (SELECT 1 FROM Usuario WHERE Username IN ('auditor','gcomercial','ginventario','logistico') AND DVH = 0)
   AND EXISTS (SELECT 1 FROM Usuario WHERE DVH <> 0)
BEGIN
    UPDATE Usuario SET DVH = 0;
    UPDATE DVVertical SET DVV = 0 WHERE NombreTabla = 'Usuario';
    PRINT 'DV de Usuario reseteado para recálculo en el próximo arranque.';
END
GO

-- ============================================================
-- MIGRACIONES INCREMENTALES
-- ============================================================

-- FechaVencimiento en Cliente (suscripción con fecha de vencimiento)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'FechaVencimiento')
BEGIN
    ALTER TABLE Cliente ADD FechaVencimiento DATE NULL;
    PRINT 'Columna FechaVencimiento agregada a Cliente.';
END
ELSE
    PRINT 'FechaVencimiento ya existe en Cliente — sin cambios.';
GO

-- FechaLimiteGracia en Cliente y tabla HistorialCobro (PdN6 — período de gracia
-- tras un cobro fallido y auditoría del patrón Chain of Responsibility de cobro).
-- Fuente única: 08_Cobro_Pago.sql (documenta el detalle y el "por qué"; ambos
-- bloques son idempotentes, así que da igual si 01 u 08 corre primero).
-- Ver BE.Cliente.EstaEnGracia / EstaSuspendidoPorPago.

-- FechaNacimiento en Cliente — OBLIGATORIA (NOT NULL) para validar mayoría de edad.
-- 1) Agregar la columna como NULL si todavía no existe.
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'FechaNacimiento')
BEGIN
    ALTER TABLE Cliente ADD FechaNacimiento DATE NULL;
    PRINT 'Columna FechaNacimiento agregada a Cliente.';
END
GO
-- 2) Backfill de filas legacy sin fecha (placeholder mayor de edad) para poder imponer NOT NULL.
UPDATE Cliente SET FechaNacimiento = '1990-01-01' WHERE FechaNacimiento IS NULL;
GO
-- 3) Imponer NOT NULL si la columna todavía admite nulos.
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
           WHERE TABLE_NAME='Cliente' AND COLUMN_NAME='FechaNacimiento' AND IS_NULLABLE='YES')
BEGIN
    ALTER TABLE Cliente ALTER COLUMN FechaNacimiento DATE NOT NULL;
    PRINT 'Cliente.FechaNacimiento ahora es NOT NULL (obligatoria).';
END
ELSE
    PRINT 'Cliente.FechaNacimiento ya es NOT NULL — sin cambios.';
GO

-- MantenimientoPrenda (historial de limpieza/mantenimiento por prenda)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MantenimientoPrenda')
BEGIN
    CREATE TABLE MantenimientoPrenda (
        IdMantenimiento INT           IDENTITY(1,1) PRIMARY KEY,
        IdPrenda        INT           NOT NULL REFERENCES Prenda(IdPrenda),
        FechaEntrada    DATETIME      NOT NULL DEFAULT GETDATE(),
        FechaSalida     DATETIME      NULL,
        Actor           NVARCHAR(100) NULL
    );
    PRINT 'Tabla MantenimientoPrenda creada.';
END
ELSE
    PRINT 'Tabla MantenimientoPrenda ya existe — sin cambios.';
GO

-- ============================================================
-- DATOS DEMO (presentación / pruebas)
-- ------------------------------------------------------------
-- Idempotente: cada bloque sólo inserta si el registro falta.
-- El DNI se guarda en TEXTO PLANO; la app lo tolera (TryDesencriptar
-- acepta registros legacy sin cifrar) y lo muestra tal cual.
-- DVH=0 → recalcular el DV desde la app (Usuarios → Recalcular DV)
-- antes del primer uso para que la verificación de integridad cierre.
-- ============================================================

-- Planes de suscripción
INSERT INTO PlanSuscripcion (Nombre, LimitePrendas, Precio, Estado)
SELECT v.Nombre, v.Limite, v.Precio, 1
FROM (VALUES
    (N'Básico',   5,  8000.00),
    (N'Estándar', 15, 15000.00),
    (N'Premium',  30, 25000.00)
) AS v(Nombre, Limite, Precio)
WHERE NOT EXISTS (SELECT 1 FROM PlanSuscripcion p WHERE p.Nombre = v.Nombre);
PRINT 'Demo: planes de suscripción.';
GO

-- Empleados (vinculados a los usuarios del sistema por Username)
INSERT INTO Empleado (Nombre, Apellido, DNI, Email, FechaIngreso, Puesto, Legajo, IdUsuario, DVH)
SELECT v.Nombre, v.Apellido, v.DNI, v.Email, GETDATE(), v.Puesto, v.Legajo,
       (SELECT TOP 1 IdUsuario FROM Usuario u WHERE u.Username = v.Username), 0
FROM (VALUES
    (N'Valentina', N'Morana', '33111000', 'vendedor@wardrobeflow.com', N'Vendedora',           'L-001', 'vendedor'),
    (N'Bruno',     N'Díaz',   '31222000', 'deposito@wardrobeflow.com', N'Depósito',           'L-002', 'deposito')
) AS v(Nombre, Apellido, DNI, Email, Puesto, Legajo, Username)
WHERE NOT EXISTS (SELECT 1 FROM Empleado e WHERE e.Legajo = v.Legajo)
  AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor = N'Pendiente');  -- solo en instalación nueva
PRINT 'Demo: empleados.';
GO

-- Clientes (todos mayores de edad; FechaNacimiento obligatoria). Sin medio de pago preferido: el
-- catálogo MedioPago todavía no existe (sección 20c) y en una instalación nueva la sección 21e
-- reemplaza estos clientes por los de los escenarios de demo.
INSERT INTO Cliente (Nombre, Apellido, DNI, Email, IdPlan, FechaAlta, FechaNacimiento, Activo, DVH)
SELECT v.Nombre, v.Apellido, v.DNI, v.Email,
       (SELECT TOP 1 IdPlan FROM PlanSuscripcion p WHERE p.Nombre = v.PlanNom),
       GETDATE(), v.FechaNac, 1, 0
FROM (VALUES
    (N'Lucía',  N'Fernández', '30111222', 'lucia.fernandez@mail.com', N'Premium',  CONVERT(date,'1990-03-15')),
    (N'Martín', N'Gómez',     '28999111', 'martin.gomez@mail.com',    N'Estándar', CONVERT(date,'1985-07-22')),
    (N'Sofía',  N'Rossi',     '35444555', 'sofia.rossi@mail.com',     N'Básico',   CONVERT(date,'1998-11-02')),
    (N'Diego',  N'Paz',       '27333444', 'diego.paz@mail.com',       N'Estándar', CONVERT(date,'1982-01-30')),
    (N'Camila', N'Torres',    '40222333', 'camila.torres@mail.com',   N'Premium',  CONVERT(date,'2001-06-10'))
) AS v(Nombre, Apellido, DNI, Email, PlanNom, FechaNac)
WHERE NOT EXISTS (SELECT 1 FROM Cliente c WHERE (c.Nombre = v.Nombre AND c.Apellido = v.Apellido) OR c.DNI = v.DNI)  -- también por DNI: un cliente demo renombrado no se duplica
  AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor = N'Pendiente');  -- solo en instalación nueva
PRINT 'Demo: clientes.';
GO

-- Prendas (catálogo de stock; todas Disponible inicialmente)
INSERT INTO Prenda (Nombre, Descripcion, Talle, Color, Categoria, Estado, IdClienteActual, FechaAlta)
SELECT v.Nombre, v.Descripcion, v.Talle, v.Color, v.Categoria, 0, NULL, GETDATE()
FROM (VALUES
    (N'Vestido Largo Negro',    N'Vestido de fiesta largo',  'M',  N'Negro',      N'Vestido'),
    (N'Blazer Beige',           N'Blazer entallado',         'L',  N'Beige',      N'Saco'),
    (N'Camisa Blanca Clásica',  N'Camisa de algodón',        'M',  N'Blanco',     N'Camisa'),
    (N'Pantalón Sastre Gris',   N'Pantalón de vestir',       '42', N'Gris',       N'Pantalón'),
    (N'Abrigo Largo Camel',     N'Tapado de paño',           'L',  N'Camel',      N'Abrigo'),
    (N'Vestido Floral',         N'Vestido estampado verano', 'S',  N'Estampado',  N'Vestido'),
    (N'Camisa Celeste',         N'Camisa de lino',           'L',  N'Celeste',    N'Camisa'),
    (N'Jean Recto Azul',        N'Jean clásico',             '40', N'Azul',       N'Pantalón'),
    (N'Saco a Cuadros',         N'Saco príncipe de Gales',   'M',  N'Multicolor', N'Saco'),
    (N'Falda Plisada Negra',    N'Falda midi plisada',       'S',  N'Negro',      N'Falda'),
    (N'Sweater Oversize Crema', N'Sweater de lana',          'L',  N'Crema',      N'Sweater'),
    (N'Gabardina Verde',        N'Gabardina impermeable',    'M',  N'Verde',      N'Abrigo')
) AS v(Nombre, Descripcion, Talle, Color, Categoria)
WHERE NOT EXISTS (SELECT 1 FROM Prenda pr WHERE pr.Nombre = v.Nombre)
  AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor = N'Pendiente');  -- solo en instalación nueva
PRINT 'Demo: prendas (stock).';
GO

-- Pedidos demo (sólo si aún no hay pedidos) + prendas EnUso asignadas a su cliente,
-- replicando lo que hace la app al crear/despachar (Prenda.Estado=EnUso, IdClienteActual).
IF NOT EXISTS (SELECT 1 FROM Pedido)
   AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor = N'Pendiente')  -- solo en instalación nueva
   AND EXISTS (SELECT 1 FROM Empleado WHERE Legajo = 'L-001')
   AND EXISTS (SELECT 1 FROM Cliente WHERE Nombre = N'Lucía' AND Apellido = N'Fernández')
BEGIN
    DECLARE @emp     INT = (SELECT TOP 1 IdEmpleado FROM Empleado WHERE Legajo = 'L-001');
    DECLARE @cLucia  INT = (SELECT TOP 1 IdCliente FROM Cliente WHERE Nombre=N'Lucía'  AND Apellido=N'Fernández');
    DECLARE @cMartin INT = (SELECT TOP 1 IdCliente FROM Cliente WHERE Nombre=N'Martín' AND Apellido=N'Gómez');
    DECLARE @cSofia  INT = (SELECT TOP 1 IdCliente FROM Cliente WHERE Nombre=N'Sofía'  AND Apellido=N'Rossi');

    DECLARE @pr1 INT = (SELECT IdPrenda FROM Prenda WHERE Nombre=N'Vestido Largo Negro');
    DECLARE @pr2 INT = (SELECT IdPrenda FROM Prenda WHERE Nombre=N'Blazer Beige');
    DECLARE @pr3 INT = (SELECT IdPrenda FROM Prenda WHERE Nombre=N'Camisa Blanca Clásica');
    DECLARE @pr4 INT = (SELECT IdPrenda FROM Prenda WHERE Nombre=N'Pantalón Sastre Gris');

    -- Pedido 1: Lucía — Entregado (2 prendas)
    INSERT INTO Pedido (IdCliente, IdEmpleado, Estado, FechaPedido, FechaDespacho, FechaEntrega, DVH)
    VALUES (@cLucia, @emp, 2, DATEADD(day,-20,GETDATE()), DATEADD(day,-18,GETDATE()), DATEADD(day,-15,GETDATE()), 0);
    DECLARE @ped1 INT = SCOPE_IDENTITY();
    INSERT INTO PedidoPrenda (IdPedido, IdPrenda) VALUES (@ped1, @pr1), (@ped1, @pr2);

    -- Pedido 2: Martín — Despachado (1 prenda)
    INSERT INTO Pedido (IdCliente, IdEmpleado, Estado, FechaPedido, FechaDespacho, DVH)
    VALUES (@cMartin, @emp, 1, DATEADD(day,-5,GETDATE()), DATEADD(day,-3,GETDATE()), 0);
    DECLARE @ped2 INT = SCOPE_IDENTITY();
    INSERT INTO PedidoPrenda (IdPedido, IdPrenda) VALUES (@ped2, @pr3);

    -- Pedido 3: Sofía — Pendiente (1 prenda)
    INSERT INTO Pedido (IdCliente, IdEmpleado, Estado, FechaPedido, DVH)
    VALUES (@cSofia, @emp, 0, DATEADD(day,-1,GETDATE()), 0);
    DECLARE @ped3 INT = SCOPE_IDENTITY();
    INSERT INTO PedidoPrenda (IdPedido, IdPrenda) VALUES (@ped3, @pr4);

    -- Marcar prendas EnUso (Estado=1) con su cliente
    UPDATE Prenda SET Estado=1, IdClienteActual=@cLucia  WHERE IdPrenda IN (@pr1,@pr2);
    UPDATE Prenda SET Estado=1, IdClienteActual=@cMartin WHERE IdPrenda=@pr3;
    UPDATE Prenda SET Estado=1, IdClienteActual=@cSofia  WHERE IdPrenda=@pr4;

    PRINT 'Demo: 3 pedidos (Entregado/Despachado/Pendiente) + prendas asignadas.';
END
ELSE
    PRINT 'Demo: pedidos ya existen o faltan datos base — sin cambios.';
GO

-- ============================================================
-- ÍNDICES NO-CLUSTERED Y RESTRICCIONES DE INTEGRIDAD
-- Idempotente: solo crea lo que falta. Mejoran los joins por FK y los filtros por fecha,
-- y el CHECK respalda en el motor la prohibición de auto-referencia del árbol Composite.
-- ============================================================

-- Índices sobre columnas FK que NO son la columna LÍDER de su PK (la PK ya indexa su primera
-- columna) y sobre columnas usadas para filtrar/ordenar (fechas).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bitacora_fecha' AND object_id = OBJECT_ID('Bitacora'))
    CREATE NONCLUSTERED INDEX IX_Bitacora_fecha ON Bitacora(fecha);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bitacora_usuario' AND object_id = OBJECT_ID('Bitacora'))
    CREATE NONCLUSTERED INDEX IX_Bitacora_usuario ON Bitacora(usuario);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BitacoraNegocio_Fecha' AND object_id = OBJECT_ID('BitacoraNegocio'))
    CREATE NONCLUSTERED INDEX IX_BitacoraNegocio_Fecha ON BitacoraNegocio(Fecha);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PermisoRelacion_IdHijo' AND object_id = OBJECT_ID('PermisoRelacion'))
    CREATE NONCLUSTERED INDEX IX_PermisoRelacion_IdHijo ON PermisoRelacion(IdHijo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RolPermiso_IdPermiso' AND object_id = OBJECT_ID('RolPermiso'))
    CREATE NONCLUSTERED INDEX IX_RolPermiso_IdPermiso ON RolPermiso(IdPermiso);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Traduccion_IdIdioma' AND object_id = OBJECT_ID('Traduccion'))
    CREATE NONCLUSTERED INDEX IX_Traduccion_IdIdioma ON Traduccion(IdIdioma);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pedido_IdCliente' AND object_id = OBJECT_ID('Pedido'))
    CREATE NONCLUSTERED INDEX IX_Pedido_IdCliente ON Pedido(IdCliente);
-- PN01: un solo pedido en curso por cliente (EnControlStock, ConFaltantes, Separado, Pendiente,
-- Despachado). La BLL ya lo valida; el índice cierra la carrera de dos operadores enviando a la vez.
SET QUOTED_IDENTIFIER ON;   -- los índices filtrados lo exigen (sqlcmd lo trae en OFF)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Pedido_ClienteActivo' AND object_id = OBJECT_ID('Pedido'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Pedido_ClienteActivo ON Pedido(IdCliente) WHERE Estado IN (0, 1, 4, 5, 6);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pedido_IdEmpleado' AND object_id = OBJECT_ID('Pedido'))
    CREATE NONCLUSTERED INDEX IX_Pedido_IdEmpleado ON Pedido(IdEmpleado);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pedido_FechaPedido' AND object_id = OBJECT_ID('Pedido'))
    CREATE NONCLUSTERED INDEX IX_Pedido_FechaPedido ON Pedido(FechaPedido);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PedidoPrenda_IdPrenda' AND object_id = OBJECT_ID('PedidoPrenda'))
    CREATE NONCLUSTERED INDEX IX_PedidoPrenda_IdPrenda ON PedidoPrenda(IdPrenda);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PedidoHistorial_IdPedido' AND object_id = OBJECT_ID('PedidoHistorial'))
    CREATE NONCLUSTERED INDEX IX_PedidoHistorial_IdPedido ON PedidoHistorial(IdPedido);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Prenda_IdClienteActual' AND object_id = OBJECT_ID('Prenda'))
    CREATE NONCLUSTERED INDEX IX_Prenda_IdClienteActual ON Prenda(IdClienteActual);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialUsuario_IdUsuario' AND object_id = OBJECT_ID('HistorialUsuario'))
    CREATE NONCLUSTERED INDEX IX_HistorialUsuario_IdUsuario ON HistorialUsuario(IdUsuario);
PRINT 'Índices no-clustered verificados/creados.';
GO

-- CHECK anti auto-referencia del árbol Composite: un permiso no puede ser su propio hijo.
-- (La validación de ciclos completa vive en BE.Familia; esto la respalda en el motor por si
-- alguien escribe por SQL directo.) Se limpian filas inválidas preexistentes para que no falle.
IF EXISTS (SELECT 1 FROM PermisoRelacion WHERE IdPadre = IdHijo)
BEGIN
    DELETE FROM PermisoRelacion WHERE IdPadre = IdHijo;
    PRINT 'PermisoRelacion: filas auto-referenciadas (IdPadre = IdHijo) eliminadas.';
END
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PermisoRelacion_NoAutoref')
BEGIN
    ALTER TABLE PermisoRelacion ADD CONSTRAINT CK_PermisoRelacion_NoAutoref CHECK (IdPadre <> IdHijo);
    PRINT 'CHECK CK_PermisoRelacion_NoAutoref agregado (IdPadre <> IdHijo).';
END
ELSE
    PRINT 'CHECK CK_PermisoRelacion_NoAutoref ya existe — sin cambios.';
GO

PRINT '';
PRINT '=== WardrobeFlowDB deploy completo. ===';
PRINT 'IMPORTANTE: Ejecutar recálculo de DVH/DVV desde la aplicación';
PRINT '            antes del primer uso (Administrar → Usuarios → Recalcular DV).';
PRINT 'Las traducciones se seedean automáticamente en el primer uso de la app.';
GO

/* ============================================================================
   WardrobeFlow — 03. PERMISOS GRANULARES (Ver vs. Configurar)
   Tier 2 — Permisos de ACCIÓN granular: separar "Ver" de "Configurar".

   Crea una patente de EDICIÓN por cada módulo de negocio (alta/modificación/baja)
   y la propaga ("grandfather") a los roles que hoy pueden editar, de modo que el
   comportamiento NO cambie al aplicar el script. Luego, desde el Gestor de Perfiles,
   el Administrador puede QUITAR la patente de edición a los roles que deban quedar
   de solo-lectura (verán el módulo pero no podrán modificarlo).

   Resolución en runtime: BLL.PermisosAccion. Si estas patentes NO existen, la BLL
   cae al permiso de VER (retrocompatibilidad), así que aplicar el código sin correr
   este script tampoco rompe nada.

   IDEMPOTENTE: se puede ejecutar varias veces sin duplicar filas.
   Reiniciá la aplicación luego de correrlo (el catálogo de patentes se cachea al inicio).
   ============================================================================ */

SET NOCOUNT ON;

DECLARE @map TABLE (VerMenu NVARCHAR(100), EditarMenu NVARCHAR(100), EditarNombre NVARCHAR(200));
INSERT INTO @map (VerMenu, EditarMenu, EditarNombre) VALUES
 ('mnuStock',             'mnuStockEditar',             'Configurar Prendas / Stock'),
 ('mnuClientes',          'mnuClientesEditar',          'Configurar Clientes'),
 ('mnuPlanSuscripciones', 'mnuPlanSuscripcionesEditar', N'Configurar Planes de Suscripción'),
 ('mnuPedidosVenta',      'mnuPedidosVentaEditar',      'Configurar Pedidos de Venta'),
 ('mnuPedidosRealizados', 'mnuPedidosRealizadosEditar', 'Configurar Pedidos Realizados');

-- 1) Crear las patentes de EDICIÓN que falten (EsFamilia=0, EsRol=0 → hojas / patentes).
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT m.EditarNombre, m.EditarMenu, N'Acción', 1, 0, 0
FROM   @map m
WHERE  EXISTS     (SELECT 1 FROM Permiso v WHERE v.NombreMenu = m.VerMenu)
  AND  NOT EXISTS (SELECT 1 FROM Permiso e WHERE e.NombreMenu = m.EditarMenu);

-- 2) "Grandfather": donde una patente de VER esté asignada DIRECTAMENTE a un rol/familia,
--    asignar también su patente de EDITAR (preserva el comportamiento actual).
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT r.IdPadre, e.IdPermiso
FROM   PermisoRelacion r
JOIN   Permiso v ON v.IdPermiso  = r.IdHijo
JOIN   @map    m ON m.VerMenu    = v.NombreMenu
JOIN   Permiso e ON e.NombreMenu = m.EditarMenu
WHERE  NOT EXISTS (SELECT 1 FROM PermisoRelacion r2
                   WHERE r2.IdPadre = r.IdPadre AND r2.IdHijo = e.IdPermiso);

PRINT 'OK: patentes de acción creadas y propagadas (idempotente).';

/* ----------------------------------------------------------------------------
   OPCIONAL — ocultar también los botones de edición en la GUI para los usuarios
   sin la patente de edición. WardrobeFlow ya tiene el mecanismo data-driven
   (ControlMapeado + ManejadorSeguridad): basta mapear los botones Guardar/Eliminar
   de cada formulario a su patente "…Editar" desde la pantalla "Mapeo de Controles".
   La re-validación de backend (BLL.PermisosAccion) ya protege la operación aunque
   el botón no se oculte.
   ---------------------------------------------------------------------------- */

-- ============================================================
-- WardrobeFlow — 05. RENOVACIÓN DE SUSCRIPCIÓN (PdN5)
-- ------------------------------------------------------------
-- Tabla de auditoría del patrón Chain of Responsibility usado en
-- BLL.Manejadores (VerificarVencimiento → IntentarRenovar → CambioPlan →
-- BajaSuscripcion). Cada fila es un intento de resolución de renovación
-- para un cliente: detectado, contactado (fuera del sistema) y resuelto
-- como Renovada / CambioPlan / Baja, o Pendiente si aún no venció.
--
-- Idempotente: se puede volver a ejecutar sin duplicar la tabla.
-- ============================================================

USE WardrobeFlowDB;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HistorialRenovacion')
BEGIN
    CREATE TABLE HistorialRenovacion (
        IdRenovacion    INT           IDENTITY(1,1) PRIMARY KEY,
        IdCliente       INT           NOT NULL REFERENCES Cliente(IdCliente),
        IdPlanAnterior  INT           NULL REFERENCES PlanSuscripcion(IdPlan),
        IdPlanNuevo     INT           NULL REFERENCES PlanSuscripcion(IdPlan),
        FechaDeteccion  DATETIME      NOT NULL DEFAULT GETDATE(),
        FechaResolucion DATETIME      NULL,
        -- 0=Pendiente, 1=Renovada, 2=CambioPlan, 3=Baja, 4=Pausada (BE.EstadoRenovacion)
        Resultado       INT           NOT NULL,
        Actor           NVARCHAR(100) NULL
    );
    PRINT 'Tabla HistorialRenovacion creada.';
END
ELSE
    PRINT 'Tabla HistorialRenovacion ya existe — sin cambios.';
GO

-- ============================================================
-- PERMISO — patente de menú para bases YA CREADAS (idempotente)
-- ------------------------------------------------------------
-- Solo hace falta para instalaciones existentes: la sección 01 (arriba) ya
-- siembra esto en instalaciones nuevas. Sigue el mismo patrón de migración que
-- el resto de las migraciones (RolPermiso plano → PermisoRelacion Composite).
-- Supervisor NO se lista acá: hereda mnuRenovacionSuscripcion de Vendedor a
-- través de la arista Composite Supervisor→Vendedor que ya existe.
-- ============================================================

INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Gestionar Renovaciones', 'mnuRenovacionSuscripcion', 'Ventas', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuRenovacionSuscripcion');
GO

INSERT INTO RolPermiso (Rol, IdPermiso)
SELECT r.Rol, p.IdPermiso
FROM (VALUES
    ('Administrador','mnuRenovacionSuscripcion'),
    ('Vendedor','mnuRenovacionSuscripcion')
) AS r(Rol, NombreMenu)
JOIN Permiso p ON p.NombreMenu = r.NombreMenu AND ISNULL(p.EsFamilia,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM RolPermiso x WHERE x.Rol = r.Rol AND x.IdPermiso = p.IdPermiso);
GO

-- Regenerar aristas Composite (rol → patente) a partir de RolPermiso: solo en la primera corrida
-- sobre esta base (ver 'MigracionRolPermiso' en la migración T04). Acá se cierra la marca.
IF NOT EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'MigracionRolPermiso')
BEGIN
    INSERT INTO PermisoRelacion (IdPadre, IdHijo)
    SELECT pr.IdPermiso, rp.IdPermiso
    FROM   RolPermiso rp
    INNER JOIN Permiso pr ON pr.Nombre = rp.Rol AND pr.EsRol = 1 AND pr.IdPermiso <> rp.IdPermiso
    WHERE  NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                       WHERE x.IdPadre = pr.IdPermiso AND x.IdHijo = rp.IdPermiso);
    INSERT INTO ParametroSistema (Clave, Valor, Fecha) VALUES (N'MigracionRolPermiso', N'Aplicada', GETDATE());
    PRINT 'Permiso mnuRenovacionSuscripcion asignado a Administrador y Vendedor (Supervisor lo hereda de Vendedor).';
END
GO

INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT TOP 1 p.IdPermiso, 'Menu', 'renovacionSuscripcionToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuRenovacionSuscripcion'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'renovacionSuscripcionToolStripMenuItem') ORDER BY p.Estado DESC, p.IdPermiso;
GO

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

-- ============================================================
-- WardrobeFlow — 08. COBRO Y PAGO DE SUSCRIPCIÓN (PdN6)
-- ------------------------------------------------------------
-- Tabla de auditoría del patrón Chain of Responsibility usado en
-- BLL.Manejadores (DetectarCobro → ProcesarPago → AplicarGracia →
-- Suspender). Cada fila es un intento de cobro para un cliente:
-- detectado, intentado (fuera del sistema: efectivo/transferencia,
-- sin pasarela — ver alcance) y resuelto como Cobrado / Gracia /
-- Suspendido, o Pendiente si aún no correspondía cobrar.
--
-- Complementa a HistorialRenovacion (PdN5): un cobro exitoso
-- confirma la renovación (extiende FechaVencimiento reutilizando el
-- mismo Builder de PdN1); un cobro fallido no cancela la suscripción
-- de inmediato, abre un período de gracia (Cliente.FechaLimiteGracia)
-- antes de bloquear nuevos pedidos.
--
-- Sigue el patrón DIRECTO a PermisoRelacion (no vía RolPermiso), el
-- mismo criterio que el resto de la jerarquía de roles: es
-- la única fuente real de autorización.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Columna Cliente.FechaLimiteGracia (período de gracia) ────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'FechaLimiteGracia')
BEGIN
    ALTER TABLE Cliente ADD FechaLimiteGracia DATE NULL;
    PRINT 'Columna FechaLimiteGracia agregada a Cliente.';
END
ELSE
    PRINT 'FechaLimiteGracia ya existe en Cliente — sin cambios.';
GO

-- ── 2) Tabla HistorialCobro ───────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'HistorialCobro')
BEGIN
    CREATE TABLE HistorialCobro (
        IdCobro         INT           IDENTITY(1,1) PRIMARY KEY,
        IdCliente       INT           NOT NULL REFERENCES Cliente(IdCliente),
        Importe         DECIMAL(10,2) NOT NULL DEFAULT 0,
        FechaDeteccion  DATETIME      NOT NULL DEFAULT GETDATE(),
        FechaResolucion DATETIME      NULL,
        -- 0=Pendiente, 1=Cobrado, 2=Gracia, 3=Suspendido (BE.EstadoCobro)
        Resultado       INT           NOT NULL,
        Actor           NVARCHAR(100) NULL
    );
    PRINT 'Tabla HistorialCobro creada.';
END
ELSE
    PRINT 'Tabla HistorialCobro ya existe — sin cambios.';
GO

-- ── 3) Patente de menú mnuCobroSuscripcion ───────────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Gestionar Cobros', 'mnuCobroSuscripcion', 'Ventas', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuCobroSuscripcion');
GO

-- ── 4) Asignación directa a PermisoRelacion (Administrador; el rol Caja la
--     recibe en la sección 21d: el cobro recurrente lo hace Caja, no el Vendedor) ─
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador', 'mnuCobroSuscripcion')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuCobroSuscripcion asignado a Administrador.';
GO

-- ── 5) Mapeo de control (pantalla "Perfiles y Permisos" → control mapeado) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT TOP 1 p.IdPermiso, 'Menu', 'cobroSuscripcionToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuCobroSuscripcion'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'cobroSuscripcionToolStripMenuItem') ORDER BY p.Estado DESC, p.IdPermiso;
GO

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
SELECT N'Ver Análisis de Abandono', 'mnuAnalisisAbandono', 'Sistema', 1, 0, 0
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
SELECT TOP 1 p.IdPermiso, 'Menu', 'analisisAbandonoToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuAnalisisAbandono'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'analisisAbandonoToolStripMenuItem') ORDER BY p.Estado DESC, p.IdPermiso;
GO

-- ============================================================
-- WardrobeFlow — 10. REPORTE DE VENTAS POR VENDEDOR (PdN8)
-- ------------------------------------------------------------
-- No requiere tablas nuevas: PdN8 es 100% lectura, agrega la
-- tabla Pedido (ya existente) agrupada por Empleado
-- (BLL.ReporteVentasVendedor).
--
-- Solo agrega la patente de menú mnuVentasVendedor, asignada a
-- Administrador y GerenteComercial (jefe del área comercial —
-- quien evalúa desempeño de sus vendedores). Sigue el patrón
-- DIRECTO a PermisoRelacion, igual que 09_Analisis_Abandono.sql.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Patente de menú mnuVentasVendedor ─────────────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Ver Ventas por Vendedor', 'mnuVentasVendedor', 'Sistema', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso WHERE NombreMenu = 'mnuVentasVendedor');
GO

-- ── 2) Asignación directa a PermisoRelacion (Administrador y GerenteComercial) ─
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador',    'mnuVentasVendedor'),
    ('GerenteComercial', 'mnuVentasVendedor')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuVentasVendedor asignado a Administrador y GerenteComercial.';
GO

-- ── 3) Mapeo de control (pantalla "Perfiles y Permisos" → control mapeado) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT TOP 1 p.IdPermiso, 'Menu', 'ventasVendedorToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuVentasVendedor'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'ventasVendedorToolStripMenuItem') ORDER BY p.Estado DESC, p.IdPermiso;
GO

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
SELECT N'Ver Rotación de Prendas', 'mnuAnalisisRotacion', 'Sistema', 1, 0, 0
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
SELECT TOP 1 p.IdPermiso, 'Menu', 'analisisRotacionToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuAnalisisRotacion'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'analisisRotacionToolStripMenuItem') ORDER BY p.Estado DESC, p.IdPermiso;
GO

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
SELECT TOP 1 p.IdPermiso, 'Menu', 'analisisMantenimientoToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuAnalisisMantenimiento'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'analisisMantenimientoToolStripMenuItem') ORDER BY p.Estado DESC, p.IdPermiso;
GO

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
SELECT TOP 1 p.IdPermiso, 'Menu', 'analisisEscasezToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuAnalisisEscasez'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'analisisEscasezToolStripMenuItem') ORDER BY p.Estado DESC, p.IdPermiso;
GO

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
SELECT N'Ver Recomendación de Prendas', 'mnuRecomendacionPrendas', 'Sistema', 1, 0, 0
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
SELECT TOP 1 p.IdPermiso, 'Menu', 'recomendacionPrendasToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuRecomendacionPrendas'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'recomendacionPrendasToolStripMenuItem') ORDER BY p.Estado DESC, p.IdPermiso;
GO

-- ============================================================
-- WardrobeFlow — 15. FIDELIZACIÓN: PAUSA, REFERIDOS Y CARGO POR DAÑO/PÉRDIDA
-- ------------------------------------------------------------
-- Tres funcionalidades nuevas del Bloque 1 (Fidelización), la última
-- cruza con Bloque 2 (Cobro):
--
--   • Pausa de suscripción (PdN5, Chain of Responsibility): Cliente.FechaPausaHasta.
--     Mientras esté vigente, bloquea pedidos nuevos SIN tocar FechaVencimiento
--     (al reanudar, la fecha de vencimiento queda como estaba — decisión de diseño).
--
--   • Programa de referidos (PdN1 → PdN6): Cliente.IdClienteReferente (quién lo
--     trajo), DescuentoProximoCobro (beneficio pendiente de aplicar) y
--     BeneficioReferidoOtorgado (evita otorgar el beneficio dos veces).
--
--   • Cargo por daño/pérdida (PdN4 → PdN6): Prenda.IdUltimoCliente (a diferencia
--     de IdClienteActual, NUNCA se limpia al devolver — así en el momento de
--     inspeccionarla en Mantenimiento todavía se sabe quién la tuvo) + tabla
--     CargoPrenda (el cargo en sí, pendiente hasta que se cobra junto con la
--     próxima renovación del cliente).
--
-- Ningún rol nuevo ni patente nueva: Pausa reutiliza mnuRenovacionSuscripcion,
-- Referidos reutiliza mnuClientes, Cargo reutiliza mnuPrendas — todas ya
-- asignadas a los roles que hoy manejan esas pantallas.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Pausa de suscripción ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'FechaPausaHasta')
BEGIN
    ALTER TABLE Cliente ADD FechaPausaHasta DATETIME NULL;
    PRINT 'Columna FechaPausaHasta agregada a Cliente.';
END
ELSE
    PRINT 'FechaPausaHasta ya existe en Cliente — sin cambios.';
GO

-- ── 2) Programa de referidos ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'IdClienteReferente')
BEGIN
    ALTER TABLE Cliente ADD IdClienteReferente INT NULL REFERENCES Cliente(IdCliente);
    PRINT 'Columna IdClienteReferente agregada a Cliente.';
END
ELSE
    PRINT 'IdClienteReferente ya existe en Cliente — sin cambios.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'DescuentoProximoCobro')
BEGIN
    ALTER TABLE Cliente ADD DescuentoProximoCobro DECIMAL(10,2) NOT NULL DEFAULT 0;
    PRINT 'Columna DescuentoProximoCobro agregada a Cliente.';
END
ELSE
    PRINT 'DescuentoProximoCobro ya existe en Cliente — sin cambios.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Cliente' AND COLUMN_NAME = 'BeneficioReferidoOtorgado')
BEGIN
    ALTER TABLE Cliente ADD BeneficioReferidoOtorgado BIT NOT NULL DEFAULT 0;
    PRINT 'Columna BeneficioReferidoOtorgado agregada a Cliente.';
END
ELSE
    PRINT 'BeneficioReferidoOtorgado ya existe en Cliente — sin cambios.';
GO

-- ── 3) Cargo por daño/pérdida ─────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Prenda' AND COLUMN_NAME = 'IdUltimoCliente')
BEGIN
    ALTER TABLE Prenda ADD IdUltimoCliente INT NULL REFERENCES Cliente(IdCliente);
    PRINT 'Columna IdUltimoCliente agregada a Prenda.';
END
ELSE
    PRINT 'IdUltimoCliente ya existe en Prenda — sin cambios.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CargoPrenda')
BEGIN
    CREATE TABLE CargoPrenda (
        IdCargo       INT           IDENTITY(1,1) PRIMARY KEY,
        IdPrenda      INT           NOT NULL REFERENCES Prenda(IdPrenda),
        IdCliente     INT           NOT NULL REFERENCES Cliente(IdCliente),
        Motivo        NVARCHAR(200) NOT NULL,
        Monto         DECIMAL(10,2) NOT NULL,
        FechaRegistro DATETIME      NOT NULL DEFAULT GETDATE(),
        FechaCobro    DATETIME      NULL,
        Actor         NVARCHAR(100) NULL,
        Estado        INT           NOT NULL DEFAULT 0  -- 0=Pendiente, 1=Cobrado
    );
    PRINT 'Tabla CargoPrenda creada.';
END
ELSE
    PRINT 'Tabla CargoPrenda ya existe — sin cambios.';
GO

-- Backfill: prendas actualmente EnUso ya tienen IdClienteActual — copiarlo a
-- IdUltimoCliente para que no arranquen en NULL (a las que ya se devolvieron
-- antes de esta migración no hay forma de reconstruirles el dato: se acepta).
UPDATE Prenda SET IdUltimoCliente = IdClienteActual
WHERE IdClienteActual IS NOT NULL AND IdUltimoCliente IS NULL;
PRINT 'Backfill de IdUltimoCliente para prendas EnUso aplicado.';
GO

-- Índices sobre las columnas nuevas usadas para filtrar/agrupar.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CargoPrenda_IdCliente' AND object_id = OBJECT_ID('CargoPrenda'))
    CREATE NONCLUSTERED INDEX IX_CargoPrenda_IdCliente ON CargoPrenda(IdCliente);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Cliente_IdClienteReferente' AND object_id = OBJECT_ID('Cliente'))
    CREATE NONCLUSTERED INDEX IX_Cliente_IdClienteReferente ON Cliente(IdClienteReferente);
PRINT 'Índices de Fidelización verificados/creados.';
GO

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
--     Deposito para gestionarla) ───────────────────────────────
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador',        'mnuListaEspera'),
    ('Vendedor',              'mnuListaEspera'),
    ('Deposito',  'mnuListaEspera')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuListaEspera asignado a Administrador, Vendedor y Deposito.';
GO

-- ── 4) Mapeo de control (pantalla "Perfiles y Permisos" → control mapeado) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT TOP 1 p.IdPermiso, 'Menu', 'listaEsperaToolStripMenuItem'
FROM Permiso p
WHERE p.NombreMenu = 'mnuListaEspera'
  AND NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = 'Menu' AND c.NombreControl = 'listaEsperaToolStripMenuItem') ORDER BY p.Estado DESC, p.IdPermiso;
GO

-- ============================================================
-- WardrobeFlow — 17. COMERCIALIZACIÓN DE LA SUSCRIPCIÓN (PN02)
-- ------------------------------------------------------------
-- Adaptado de un TP de otro compañero de cursada (misma lógica de negocio:
-- alta de cliente + contratación de un plan), reescrito al vocabulario de
-- WardrobeFlow. Roles: Venta (ya existe, es el Vendedor) y Caja (NUEVO,
-- separado de Vendedor a propósito: Vendedor es "operador" de la venta,
-- Caja cobra — separación de funciones).
--
-- Tabla Contratacion: estado intermedio entre "el cliente eligió un plan"
-- (Venta, CrearContratacion) y "la suscripción quedó vigente" (Caja confirma
-- el pago, ConfirmarPago dispara BLL.Cliente.ActivarSuscripcionDesdeContratacion).
-- 0=PendientePago, 1=Pagada, 2=Cancelada (BE.EstadoContratacion).
--
-- El Comprobante (CU02-CAJ) se guarda como columnas propias de Contratacion
-- (NumeroComprobante, FechaComprobante) — no hace falta una tabla aparte.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Tabla Contratacion ────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Contratacion')
BEGIN
    CREATE TABLE Contratacion (
        IdContratacion    INT           IDENTITY(1,1) PRIMARY KEY,
        IdCliente         INT           NOT NULL REFERENCES Cliente(IdCliente),
        IdPlan            INT           NOT NULL REFERENCES PlanSuscripcion(IdPlan),
        IdVendedor        INT           NOT NULL REFERENCES Empleado(IdEmpleado),
        IdCaja            INT           NULL     REFERENCES Empleado(IdEmpleado),
        Modalidad         INT           NOT NULL CONSTRAINT CHK_Contratacion_Modalidad CHECK (Modalidad IN (0,1,2)),
        Estado            INT           NOT NULL DEFAULT 0 CONSTRAINT CHK_Contratacion_Estado CHECK (Estado IN (0,1,2)),
        FechaAlta         DATETIME      NOT NULL DEFAULT GETDATE(),
        FechaResolucion   DATETIME      NULL,
        NumeroComprobante NVARCHAR(50)  NULL,
        FechaComprobante  DATETIME      NULL
    );
    PRINT 'Tabla Contratacion creada.';
END
ELSE
    PRINT 'Tabla Contratacion ya existe — sin cambios.';
GO

-- ── 1bis) Constraints de integridad (por si la tabla ya existía sin ellas) ──
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Contratacion_Modalidad')
    ALTER TABLE Contratacion ADD CONSTRAINT CHK_Contratacion_Modalidad CHECK (Modalidad IN (0,1,2));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Contratacion_Estado')
    ALTER TABLE Contratacion ADD CONSTRAINT CHK_Contratacion_Estado CHECK (Estado IN (0,1,2));
GO

-- ── 2) Patentes mnuCaja/mnuCajaEditar + rol Caja (nuevo, real — separado de
--     Vendedor). Van juntas: ambas son filas de Permiso (dos hojas/patentes
--     y un nodo-rol), no hace falta un INSERT por fila cuando es la misma
--     tabla — Caja es un rol simple (sin hijos propios en el árbol
--     Composite), así que no necesita más que esto para existir.
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT v.Nombre, v.NombreMenu, v.Tipo, 1, v.EsFamilia, v.EsRol
FROM (VALUES
    ('Gestionar Caja',  'mnuCaja',       'Caja', 0, 0),
    ('Configurar Caja', 'mnuCajaEditar', 'Caja', 0, 0),
    ('Caja',             'Caja',         'Rol',  1, 1)
) AS v(Nombre, NombreMenu, Tipo, EsFamilia, EsRol)
WHERE NOT EXISTS (SELECT 1 FROM Permiso p
                  WHERE p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = v.EsFamilia AND ISNULL(p.EsRol,0) = v.EsRol);
GO

-- ── 3) Asignación de patentes: Administrador (acceso total) + Caja ───────
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador', 'mnuCaja'),
    ('Administrador', 'mnuCajaEditar'),
    ('Caja',          'mnuCaja'),
    ('Caja',          'mnuCajaEditar')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permisos mnuCaja/mnuCajaEditar asignados a Administrador y Caja.';
GO

-- ── 4) Mapeo de controles (pantalla "Perfiles y Permisos" → ítems de menú) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT ISNULL(MIN(CASE WHEN p.Estado = 1 THEN p.IdPermiso END), MIN(p.IdPermiso)), v.Formulario, v.NombreControl
FROM (VALUES
    ('mnuClientes', 'Menu', 'nuevaContratacionToolStripMenuItem'),
    ('mnuCaja',     'Menu', 'cajaToolStripMenuItem'),
    ('mnuCaja',     'Menu', 'contratacionesPendientesToolStripMenuItem')
) AS v(NombreMenu, Formulario, NombreControl)
JOIN Permiso p ON p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = v.Formulario AND c.NombreControl = v.NombreControl)
GROUP BY v.Formulario, v.NombreControl;
GO

-- ── 5) Usuario demo del rol Caja ──────────────────────────────────────────
-- Mismo criterio que los usuarios demo de la sección 01 (Auditor/
-- GerenteComercial/...): clave hasheada (PBKDF2), texto plano 'usuario1!'.
-- Un solo INSERT (sin UPDATE aparte): al ser un username nuevo, los datos
-- administrativos se cargan directo, no hace falta el patrón "completar
-- solo si está vacío" que sí usan las siembras que reparten varios roles.
INSERT INTO Usuario (Username, Clave, Rol, Perfil, Nombre, Apellido, Email, FechaNacimiento,
                      Estado, IntentosFallidos, DVH, IdIdioma, Activo)
SELECT 'caja', 'IVYc7mzk/g0OD/k6lOKrMK4xXI4dnw14xGccH3ZuTbVWsQyUpkNFKnY6hzQcYBG0', 'Caja', 'Caja',
       N'Carolina', N'Ibáñez', 'caja@wardrobeflow.com', '1991-04-12', 1, 0, 0, 'ES', 1
WHERE NOT EXISTS (SELECT 1 FROM Usuario WHERE Username = 'caja')
  AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor = N'Pendiente');  -- solo en instalación nueva
PRINT 'Usuario demo del rol Caja inicializado.';
GO

-- ============================================================
-- WardrobeFlow — 18. MÉTRICAS, PROMOCIONES Y TOMA DE DECISIONES (PN03)
-- ------------------------------------------------------------
-- Adaptado del proyecto "SIRVI" de un compañero de cursada (Franco De
-- Benedetto), reescrito al vocabulario de WardrobeFlow. Roles:
--   Gerencia    → reusa GerenteComercial (ya existe)
--   Vendedor    → reusa Vendedor (ya existe), sugiere la baja
--   Administración → NUEVO rol AdministracionComercial
--   Contabilidad   → NUEVO rol, separado de Administración a propósito
--                     (separación de funciones: quien aprueba el impacto
--                     económico no es quien redacta la promoción)
--
-- Tabla SugerenciaPromocion: idea cruda de Gerencia (0=Pendiente, 1=Evaluada).
-- Tabla Promocion: aplica a UN plan o a UNA categoría de prenda, nunca ambos.
--   Estado: 0=EnRevisionContable, 1=Vigente, 2=RechazadaContabilidad,
--           3=BajaSolicitada, 4=Desactivada (BE.EstadoPromocion).
--   La sección 20d agrega el flujo aprobado: 5=Descartada, 6=Vencida, sugerencia
--   Descartada (2), y las tablas PromocionHistorial, DictamenContable y
--   SolicitudBajaPromocion (reemplazan Promocion.Observacion y MotivoBaja).
--   La sección 20g simplifica el flujo: solo promociones por plan (IdPlan NOT NULL,
--   sin CategoriaPrenda), estados 0, 1, 2, 4=DadaDeBaja y 6, y la solicitud de baja
--   pasa a ser Pendiente/Atendida con su respuesta (no cambia el estado).
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Tabla SugerenciaPromocion ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SugerenciaPromocion')
BEGIN
    CREATE TABLE SugerenciaPromocion (
        IdSugerencia          INT           IDENTITY(1,1) PRIMARY KEY,
        IdPlan                INT           NULL REFERENCES PlanSuscripcion(IdPlan),
        CategoriaPrenda       NVARCHAR(100) NULL,
        Motivo                NVARCHAR(500) NOT NULL,
        TipoDescuentoSugerido INT           NOT NULL CONSTRAINT CHK_SugerenciaPromocion_Tipo CHECK (TipoDescuentoSugerido IN (0,1,2)),
        BeneficioEstimado     DECIMAL(10,2) NOT NULL,
        Estado                INT           NOT NULL DEFAULT 0 CONSTRAINT CHK_SugerenciaPromocion_Estado CHECK (Estado IN (0,1)),
        FechaAlta             DATETIME      NOT NULL DEFAULT GETDATE()
    );
    PRINT 'Tabla SugerenciaPromocion creada.';
END
ELSE
    PRINT 'Tabla SugerenciaPromocion ya existe — sin cambios.';
GO

-- ── 1bis) Constraints de integridad (por si la tabla ya existía sin ellas) ──
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SugerenciaPromocion_Tipo')
    ALTER TABLE SugerenciaPromocion ADD CONSTRAINT CHK_SugerenciaPromocion_Tipo CHECK (TipoDescuentoSugerido IN (0,1,2));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SugerenciaPromocion_Estado')
    ALTER TABLE SugerenciaPromocion ADD CONSTRAINT CHK_SugerenciaPromocion_Estado CHECK (Estado IN (0,1));
GO

-- ── 2) Tabla Promocion ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Promocion')
BEGIN
    CREATE TABLE Promocion (
        IdPromocion        INT           IDENTITY(1,1) PRIMARY KEY,
        Nombre             NVARCHAR(150) NOT NULL,
        Descripcion        NVARCHAR(500) NULL,
        TipoDescuento      INT           NOT NULL CONSTRAINT CHK_Promocion_Tipo CHECK (TipoDescuento IN (0,1,2)),
        Valor              DECIMAL(10,2) NOT NULL CONSTRAINT CHK_Promocion_Valor CHECK (Valor > 0),
        FechaInicio        DATE          NOT NULL,
        FechaFin           DATE          NOT NULL,
        Estado             INT           NOT NULL DEFAULT 0 CONSTRAINT CHK_Promocion_Estado CHECK (Estado IN (0,1,2,3,4)),
        IdPlan             INT           NULL REFERENCES PlanSuscripcion(IdPlan),
        CategoriaPrenda    NVARCHAR(100) NULL,
        MargenEstimado     DECIMAL(10,2) NOT NULL DEFAULT 0,
        ImpactoEconomico   NVARCHAR(500) NULL,
        Observacion        NVARCHAR(500) NULL,
        MotivoBaja         NVARCHAR(500) NULL,
        IdSugerenciaOrigen INT           NULL REFERENCES SugerenciaPromocion(IdSugerencia),
        FechaAlta          DATETIME      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT CHK_Promocion_Fechas CHECK (FechaFin >= FechaInicio),
        CONSTRAINT CHK_Promocion_Destino CHECK (
            (IdPlan IS NOT NULL AND CategoriaPrenda IS NULL) OR
            (IdPlan IS NULL AND CategoriaPrenda IS NOT NULL)),
        -- TipoDescuento 0 = Porcentaje (BE.TipoDescuento): no puede superar el 100%.
        CONSTRAINT CHK_Promocion_Porcentaje CHECK (TipoDescuento <> 0 OR Valor <= 100)
    );
    PRINT 'Tabla Promocion creada.';
END
ELSE
    PRINT 'Tabla Promocion ya existe — sin cambios.';
GO

-- ── 2bis) Constraints de integridad (por si la tabla ya existía sin ellas) ──
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Tipo')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Tipo CHECK (TipoDescuento IN (0,1,2));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Valor')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Valor CHECK (Valor > 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Estado')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Estado CHECK (Estado IN (0,1,2,3,4));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Fechas')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Fechas CHECK (FechaFin >= FechaInicio);
GO
-- Solo mientras exista la columna: la sección 20g quita las promociones por categoría.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Destino')
   AND COL_LENGTH('Promocion', 'CategoriaPrenda') IS NOT NULL
    EXEC(N'ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Destino CHECK (
        (IdPlan IS NOT NULL AND CategoriaPrenda IS NULL) OR
        (IdPlan IS NULL AND CategoriaPrenda IS NOT NULL))');
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Porcentaje')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Porcentaje CHECK (TipoDescuento <> 0 OR Valor <= 100);
GO

-- ── 3) Patentes de menú + roles nuevos AdministracionComercial y
--     Contabilidad. Van juntas (mismo criterio que el rol Caja en la
--     sección 17): todo es Permiso, y ninguno de los dos roles tiene hijos
--     propios en el árbol Composite — son roles simples.
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT v.Nombre, v.NombreMenu, v.Tipo, 1, v.EsFamilia, v.EsRol
FROM (VALUES
    (N'Sugerir Promoción',           'mnuSugerenciaPromocion',        'Promociones', 0, 0),
    ('Gestionar Promociones',       'mnuPromocionesAdmin',           'Promociones', 0, 0),
    ('Configurar Promociones Admin','mnuPromocionesAdminEditar',     'Promociones', 0, 0),
    (N'Revisión Contable',           'mnuPromocionesContable',        'Promociones', 0, 0),
    (N'Configurar Revisión Contable','mnuPromocionesContableEditar',  'Promociones', 0, 0),
    ('Ver Promociones Vigentes',    'mnuPromocionesVigentes',        'Promociones', 0, 0),
    (N'Sugerir Baja de Promoción',   'mnuPromocionesVigentesEditar',  'Promociones', 0, 0),
    ('AdministracionComercial',     'AdministracionComercial',       'Rol',         1, 1),
    ('Contabilidad',                'Contabilidad',                  'Rol',         1, 1)
) AS v(Nombre, NombreMenu, Tipo, EsFamilia, EsRol)
WHERE NOT EXISTS (SELECT 1 FROM Permiso p
                  WHERE p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = v.EsFamilia AND ISNULL(p.EsRol,0) = v.EsRol);
GO

-- ── 4) Asignación de patentes ────────────────────────────────────────────
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    -- Administrador: acceso total también a este módulo nuevo.
    ('Administrador', 'mnuSugerenciaPromocion'), ('Administrador', 'mnuPromocionesAdmin'),
    ('Administrador', 'mnuPromocionesAdminEditar'), ('Administrador', 'mnuPromocionesContable'),
    ('Administrador', 'mnuPromocionesContableEditar'), ('Administrador', 'mnuPromocionesVigentes'),
    ('Administrador', 'mnuPromocionesVigentesEditar'),
    -- Gerencia (reusa GerenteComercial): sugiere promociones.
    ('GerenteComercial', 'mnuSugerenciaPromocion'),
    -- Vendedor: consulta vigentes y puede sugerir la baja.
    ('Vendedor', 'mnuPromocionesVigentes'), ('Vendedor', 'mnuPromocionesVigentesEditar'),
    -- Administración: gestiona el ciclo completo de la promoción.
    ('AdministracionComercial', 'mnuPromocionesAdmin'), ('AdministracionComercial', 'mnuPromocionesAdminEditar'),
    -- Contabilidad: aprueba o rechaza.
    ('Contabilidad', 'mnuPromocionesContable'), ('Contabilidad', 'mnuPromocionesContableEditar')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permisos de Promociones asignados a Administrador, GerenteComercial, Vendedor, AdministracionComercial y Contabilidad.';
GO

-- ── 5) Mapeo de controles (pantalla "Perfiles y Permisos" → ítems de menú) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT ISNULL(MIN(CASE WHEN p.Estado = 1 THEN p.IdPermiso END), MIN(p.IdPermiso)), v.Formulario, v.NombreControl
FROM (VALUES
    ('mnuSugerenciaPromocion', 'Menu', 'promocionesToolStripMenuItem'),
    ('mnuSugerenciaPromocion', 'Menu', 'sugerirPromocionToolStripMenuItem'),
    ('mnuPromocionesAdmin',    'Menu', 'gestionPromocionesToolStripMenuItem'),
    ('mnuPromocionesContable', 'Menu', 'revisionContablePromocionesToolStripMenuItem'),
    ('mnuPromocionesVigentes', 'Menu', 'promocionesVigentesToolStripMenuItem')
) AS v(NombreMenu, Formulario, NombreControl)
JOIN Permiso p ON p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = v.Formulario AND c.NombreControl = v.NombreControl)
GROUP BY v.Formulario, v.NombreControl;
GO

-- ── 6) Usuarios demo de los roles nuevos ──────────────────────────────────
-- Mismo criterio que el usuario 'caja' de la sección 17: clave hasheada
-- (PBKDF2), texto plano 'usuario1!', un solo INSERT con los datos
-- administrativos ya cargados (usernames nuevos, sin necesidad del patrón
-- insert+update "completar solo si está vacío").
INSERT INTO Usuario (Username, Clave, Rol, Perfil, Nombre, Apellido, Email, FechaNacimiento,
                      Estado, IntentosFallidos, DVH, IdIdioma, Activo)
SELECT v.Username, v.Clave, v.Rol, v.Perfil, v.Nombre, v.Apellido, v.Email, v.FechaNac, 1, 0, 0, 'ES', 1
FROM (VALUES
  ('admcomercial', 'gm4kogeeRRIphufl0aWPR2S0wW17VwHYqiSihJ3GXs3WzW85kcb+i/7yVDHRNDZA', 'AdministracionComercial', 'AdministracionComercial', N'Agustina', N'Ríos',   'admcomercial@wardrobeflow.com', '1989-08-22'),
  ('contable',     '9IehcthmyLV2v3I9zeWkUkelEhorzkOrjxcWqylFYwOVmVV/x1fGwsgCNOv+yvcM', 'Contabilidad',            'Contabilidad',            N'Carlos',   N'Suárez', 'contable@wardrobeflow.com',     '1984-05-14')
) AS v(Username, Clave, Rol, Perfil, Nombre, Apellido, Email, FechaNac)
WHERE NOT EXISTS (SELECT 1 FROM Usuario u WHERE u.Username = v.Username)
  AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor = N'Pendiente');  -- solo en instalación nueva
PRINT 'Usuarios demo de AdministracionComercial y Contabilidad inicializados.';
GO

-- ============================================================
-- WardrobeFlow — 19. INSPECCIÓN DE DEVOLUCIÓN (PN04)
-- ------------------------------------------------------------
-- Alineado a la lógica real de Nuuly (binaria, sin aprobador): al inspeccionar
-- una prenda devuelta (EnLimpieza) el Depósito resuelve directo entre dos
-- caminos únicos — reingresa a Disponible sin cargo, o se da de baja y se
-- cobra el precio de reposición completo (BLL.CargoPrenda.RegistrarCargo, ya
-- existente desde Bloque 1, sin aprobación de nadie). Una prenda que nunca
-- vuelve físicamente (perdida) se reporta directo desde EnUso → Baja
-- (CU06-DEP), habilitado en BE.Estados.EstadoEnUso.
--
-- Rol: reusa Deposito (ya tiene StockEditar/Stock, ya es el
-- "Depósito" conceptual de PN01) — sin rol nuevo, coherente con que en el
-- modelo real de Nuuly no hay un aprobador. GerenteInventario lo hereda.
--
-- La patente mnuInspeccionDevolucion controla SOLO la visibilidad del menú de
-- esta pantalla nueva: la escritura real (CambiarEstado, RegistrarCargo)
-- sigue exigiendo StockEditar/Stock, sin cambios — mismo mecanismo de
-- siempre, ahora con una pantalla propia en vez de reusar la de Stock
-- genérica. A propósito NO se crea una "mnuInspeccionDevolucionEditar": no
-- hay una noción de "editar" separada de "ver" en este módulo (a diferencia
-- de Stock, que sí distingue alta/baja de prendas de la sola consulta), así
-- que una patente Editar que nada verifica sería engañosa en Perfiles y
-- Permisos.
--
-- Idempotente: se puede volver a ejecutar sin duplicar nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) Columna Prenda.PrecioReposicion ───────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'Prenda' AND COLUMN_NAME = 'PrecioReposicion')
BEGIN
    ALTER TABLE Prenda ADD PrecioReposicion DECIMAL(10,2) NULL;
    PRINT 'Columna Prenda.PrecioReposicion agregada.';
END
ELSE
    PRINT 'Columna Prenda.PrecioReposicion ya existe — sin cambios.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Prenda_PrecioReposicion')
    ALTER TABLE Prenda ADD CONSTRAINT CHK_Prenda_PrecioReposicion CHECK (PrecioReposicion IS NULL OR PrecioReposicion > 0);
GO

-- ── 2) Patente de menú mnuInspeccionDevolucion ────────────────────────────
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT v.Nombre, v.NombreMenu, v.Tipo, 1, 0, 0
FROM (VALUES
    (N'Inspección de Devolución', 'mnuInspeccionDevolucion', 'Inventario')
) AS v(Nombre, NombreMenu, Tipo)
WHERE NOT EXISTS (SELECT 1 FROM Permiso p
                  WHERE p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0);
GO

-- ── 3) Asignación de patente: Administrador + Deposito ───────
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador',         'mnuInspeccionDevolucion'),
    ('Deposito',  'mnuInspeccionDevolucion')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuInspeccionDevolucion asignado a Administrador y Deposito.';
GO

-- ── 4) Mapeo de controles (pantalla "Perfiles y Permisos" → ítems de menú) ─
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT ISNULL(MIN(CASE WHEN p.Estado = 1 THEN p.IdPermiso END), MIN(p.IdPermiso)), v.Formulario, v.NombreControl
FROM (VALUES
    ('mnuInspeccionDevolucion', 'Menu', 'inspeccionDevolucionToolStripMenuItem')
) AS v(NombreMenu, Formulario, NombreControl)
JOIN Permiso p ON p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = v.Formulario AND c.NombreControl = v.NombreControl)
GROUP BY v.Formulario, v.NombreControl;
GO

-- ============================================================
-- WardrobeFlow — 20. HARDENING DE INTEGRIDAD (auditoría de BD)
-- ------------------------------------------------------------
-- Varias columnas INT respaldadas por un enum de C# (Prenda.Estado,
-- Pedido.Estado, HistorialRenovacion.Resultado, HistorialCobro.Resultado,
-- ListaEspera.Estado, CargoPrenda.Estado, Bitacora.criticidad) quedaron
-- sin el CHECK que sí se agregó para los módulos más nuevos (Contratacion,
-- Promocion, SugerenciaPromocion, Prenda.PrecioReposicion — ver 17/18/19).
-- Hoy la app nunca escribe un valor fuera de rango (siempre castea el
-- enum), pero sin el CHECK un UPDATE manual o una migración futura que
-- agregue un miembro al enum sin agregar el CHECK correspondiente podría
-- dejar un valor inválido sin que el motor lo impida. Este script cierra
-- esa brecha para las columnas viejas, con el mismo patrón idempotente
-- (ALTER TABLE ... ADD CONSTRAINT IF NOT EXISTS) que ya usan 17/18/19.
--
-- También agrega índices sobre Prenda.Estado y Pedido.Estado: son el
-- predicado principal de varias consultas calientes del DAL
-- (Prenda.ObtenerDisponibles, Prenda.ObtenerConteoDisponiblesPorTalleCategoria,
-- Pedido.ObtenerPendientes) que hoy no tienen índice de apoyo.
--
-- Idempotente: se puede volver a ejecutar sin duplicar ni romper nada.
-- ============================================================

USE WardrobeFlowDB;
GO

-- ── 1) CHECK constraints — columnas Estado/Resultado respaldadas por enum ──

-- Prenda.Estado (BE.EstadoPrenda: Disponible=0, EnUso=1, EnLimpieza=2, Baja=3)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Prenda_Estado')
    ALTER TABLE Prenda ADD CONSTRAINT CHK_Prenda_Estado CHECK (Estado IN (0,1,2,3));
GO

-- Pedido.Estado (BE.EstadoPedido: Pendiente=0, Despachado=1, Entregado=2, Cancelado=3)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Pedido_Estado')
    ALTER TABLE Pedido ADD CONSTRAINT CHK_Pedido_Estado CHECK (Estado IN (0,1,2,3));
GO

-- HistorialRenovacion.Resultado (BE.EstadoRenovacion: Pendiente=0, Renovada=1,
-- CambioPlan=2, Baja=3, Pausada=4)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HistorialRenovacion_Resultado')
    ALTER TABLE HistorialRenovacion ADD CONSTRAINT CHK_HistorialRenovacion_Resultado CHECK (Resultado IN (0,1,2,3,4));
GO

-- HistorialCobro.Resultado (BE.EstadoCobro: Pendiente=0, Cobrado=1, Gracia=2, Suspendido=3)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HistorialCobro_Resultado')
    ALTER TABLE HistorialCobro ADD CONSTRAINT CHK_HistorialCobro_Resultado CHECK (Resultado IN (0,1,2,3));
GO

-- ListaEspera.Estado (BE.EstadoListaEspera: Pendiente=0, Reservada=1, Convertida=2, Cancelada=3)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_ListaEspera_Estado')
    ALTER TABLE ListaEspera ADD CONSTRAINT CHK_ListaEspera_Estado CHECK (Estado IN (0,1,2,3));
GO

-- CargoPrenda.Estado (BE.EstadoCargo: Pendiente=0, Cobrado=1)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_CargoPrenda_Estado')
    ALTER TABLE CargoPrenda ADD CONSTRAINT CHK_CargoPrenda_Estado CHECK (Estado IN (0,1));
GO

-- Bitacora.criticidad (BE.Criticidad: None=0, Baja=1, Media=2, Alta=3,
-- IntentosLogin=4, RecuperacionClave=5, BloqueosCuenta=6)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Bitacora_criticidad')
    ALTER TABLE Bitacora ADD CONSTRAINT CHK_Bitacora_criticidad CHECK (criticidad IN (0,1,2,3,4,5,6));
GO

-- ── 2) Índices sobre Estado (predicado principal de varias consultas del DAL) ──

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Prenda_Estado' AND object_id = OBJECT_ID('Prenda'))
    CREATE NONCLUSTERED INDEX IX_Prenda_Estado ON Prenda(Estado) INCLUDE (Categoria, Talle);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pedido_Estado' AND object_id = OBJECT_ID('Pedido'))
    CREATE NONCLUSTERED INDEX IX_Pedido_Estado ON Pedido(Estado);
PRINT 'Índices de Estado (Prenda/Pedido) verificados/creados.';
GO


-- ============================================================
-- WardrobeFlow — 20b. PN03 APLICADO AL COBRO + INTEGRIDAD DE PN02/PN03/PN04
-- ------------------------------------------------------------
-- (1) Contratacion guarda lo que realmente se cobró: importe, descuento aplicado y la promoción
--     vigente que se usó (regla de NUULY: UN solo descuento por ciclo — BE.PoliticaDescuento).
-- (2) Restricciones que solo estaban en la BLL: monto de cargo > 0, intentos de pago 0..3, una
--     única contratación pendiente por cliente y una única anotación activa por prenda y cliente.
-- (3) Índices sobre las columnas por las que se filtran las tablas nuevas.
-- Idempotente: cada elemento se crea solo si falta.
-- ============================================================
SET QUOTED_IDENTIFIER ON;
GO

-- (1) Importe cobrado, descuento y promoción aplicada
IF COL_LENGTH('Contratacion', 'Importe') IS NULL
    ALTER TABLE Contratacion ADD Importe DECIMAL(10,2) NULL;
IF COL_LENGTH('Contratacion', 'DescuentoAplicado') IS NULL
    ALTER TABLE Contratacion ADD DescuentoAplicado DECIMAL(10,2) NULL;
IF COL_LENGTH('Contratacion', 'IdPromocion') IS NULL
    ALTER TABLE Contratacion ADD IdPromocion INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contratacion_Promocion')
    ALTER TABLE Contratacion ADD CONSTRAINT FK_Contratacion_Promocion
        FOREIGN KEY (IdPromocion) REFERENCES Promocion(IdPromocion);
GO

-- (2) Restricciones de integridad
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CargoPrenda_Monto')
    ALTER TABLE CargoPrenda ADD CONSTRAINT CK_CargoPrenda_Monto CHECK (Monto > 0);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Contratacion_Intentos')
   AND COL_LENGTH('Contratacion', 'IntentosPago') IS NOT NULL
    EXEC(N'ALTER TABLE Contratacion ADD CONSTRAINT CK_Contratacion_Intentos CHECK (IntentosPago BETWEEN 0 AND 3)');  -- dinámico: la columna la quita 20c
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Contratacion_Importe')
    ALTER TABLE Contratacion ADD CONSTRAINT CK_Contratacion_Importe
        CHECK ((Importe IS NULL OR Importe >= 0) AND (DescuentoAplicado IS NULL OR DescuentoAplicado >= 0));
GO

-- Un cliente no puede tener dos contrataciones pendientes de pago a la vez (la BLL ya lo valida,
-- pero entre dos sesiones simultáneas de Venta solo el motor puede garantizarlo).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Contratacion_UnaPendientePorCliente' AND object_id = OBJECT_ID('Contratacion'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Contratacion_UnaPendientePorCliente
        ON Contratacion(IdCliente) WHERE Estado = 0;

-- Un cliente no puede estar anotado dos veces (Pendiente o Reservada) en la lista de espera de la misma prenda.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ListaEspera_AnotacionActiva' AND object_id = OBJECT_ID('ListaEspera'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_ListaEspera_AnotacionActiva
        ON ListaEspera(IdPrenda, IdCliente) WHERE Estado IN (0, 1);
GO

-- (3) Índices de las tablas nuevas
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Contratacion_Estado' AND object_id = OBJECT_ID('Contratacion'))
    CREATE NONCLUSTERED INDEX IX_Contratacion_Estado ON Contratacion(Estado);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ListaEspera_Prenda_Estado' AND object_id = OBJECT_ID('ListaEspera'))
    CREATE NONCLUSTERED INDEX IX_ListaEspera_Prenda_Estado ON ListaEspera(IdPrenda, Estado);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MantenimientoPrenda_IdPrenda' AND object_id = OBJECT_ID('MantenimientoPrenda'))
    CREATE NONCLUSTERED INDEX IX_MantenimientoPrenda_IdPrenda ON MantenimientoPrenda(IdPrenda);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Promocion_Estado' AND object_id = OBJECT_ID('Promocion'))
    CREATE NONCLUSTERED INDEX IX_Promocion_Estado ON Promocion(Estado);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Prenda_IdUltimoCliente' AND object_id = OBJECT_ID('Prenda'))
    CREATE NONCLUSTERED INDEX IX_Prenda_IdUltimoCliente ON Prenda(IdUltimoCliente);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Cliente_IdPlan' AND object_id = OBJECT_ID('Cliente'))
    CREATE NONCLUSTERED INDEX IX_Cliente_IdPlan ON Cliente(IdPlan);
PRINT 'Sección 20b: importe/promoción en Contratacion, restricciones e índices verificados.';
GO
-- ============================================================
-- WardrobeFlow — 20c. PN02: MEDIOS DE PAGO, INTENTOS Y DESISTIMIENTOS
-- ------------------------------------------------------------
-- Diagrama de actividad de PN02 (adaptado a WardrobeFlow):
--   · «Medio de pago»: catálogo propio (antes texto libre en Contratacion.MedioPago).
--   · «Intento»: cada cobro que no se concreta queda registrado (antes solo un contador
--     Contratacion.IntentosPago, dato derivable). La cantidad se cuenta sobre la tabla.
--   · «Aviso de desistimiento»: el cliente identificado no elige plan y modalidad.
--   · «Constancia de suscripción»: período activado por el cobro (VigenciaDesde/Hasta).
-- Idempotente: migra los datos existentes y quita las columnas reemplazadas.
-- ============================================================
SET QUOTED_IDENTIFIER ON;
GO

-- (1) Catálogo de medios de pago
IF OBJECT_ID('MedioPago', 'U') IS NULL
BEGIN
    CREATE TABLE MedioPago (
        IdMedioPago     INT           NOT NULL PRIMARY KEY,
        Nombre          NVARCHAR(50)  NOT NULL CONSTRAINT UX_MedioPago_Nombre UNIQUE,
        ClaveTraduccion NVARCHAR(100) NOT NULL
    );
    PRINT 'Tabla MedioPago creada.';
END
GO
INSERT INTO MedioPago (IdMedioPago, Nombre, ClaveTraduccion)
SELECT v.Id, v.Nombre, v.Clave
FROM (VALUES (1, N'Efectivo', 'medio.efectivo'), (2, N'Tarjeta', 'medio.tarjeta'), (3, N'Transferencia', 'medio.transferencia'))
     AS v(Id, Nombre, Clave)
WHERE NOT EXISTS (SELECT 1 FROM MedioPago m WHERE m.IdMedioPago = v.Id);
GO

-- (2) Contratacion: medio de pago por FK y período activado
IF COL_LENGTH('Contratacion', 'IdMedioPago') IS NULL
    ALTER TABLE Contratacion ADD IdMedioPago INT NULL;
IF COL_LENGTH('Contratacion', 'VigenciaDesde') IS NULL
    ALTER TABLE Contratacion ADD VigenciaDesde DATE NULL;
IF COL_LENGTH('Contratacion', 'VigenciaHasta') IS NULL
    ALTER TABLE Contratacion ADD VigenciaHasta DATE NULL;
-- Precio mensual pactado al registrar la contratación («Orden de cobro»): Caja cobra ese precio
-- aunque el plan cambie de precio mientras la contratación espera en la cola.
IF COL_LENGTH('Contratacion', 'PrecioMensual') IS NULL
    ALTER TABLE Contratacion ADD PrecioMensual DECIMAL(10,2) NULL;
-- «¿Referido? Sí → Acreditar crédito»: a quién se le acreditó el beneficio con este cobro.
IF COL_LENGTH('Contratacion', 'IdReferenteAcreditado') IS NULL
    ALTER TABLE Contratacion ADD IdReferenteAcreditado INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contratacion_Referente')
    ALTER TABLE Contratacion ADD CONSTRAINT FK_Contratacion_Referente
        FOREIGN KEY (IdReferenteAcreditado) REFERENCES Cliente(IdCliente);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contratacion_MedioPago')
    ALTER TABLE Contratacion ADD CONSTRAINT FK_Contratacion_MedioPago
        FOREIGN KEY (IdMedioPago) REFERENCES MedioPago(IdMedioPago);
GO

-- Migración del texto libre al catálogo y baja de la columna reemplazada.
IF COL_LENGTH('Contratacion', 'MedioPago') IS NOT NULL
BEGIN
    EXEC(N'UPDATE c SET c.IdMedioPago = m.IdMedioPago
           FROM Contratacion c JOIN MedioPago m ON m.Nombre = LTRIM(RTRIM(c.MedioPago))
           WHERE c.IdMedioPago IS NULL AND c.MedioPago IS NOT NULL;
           DECLARE @n INT = (SELECT COUNT(*) FROM Contratacion WHERE IdMedioPago IS NULL AND MedioPago IS NOT NULL);
           IF @n > 0 PRINT ''AVISO: '' + CAST(@n AS NVARCHAR(10)) + '' cobro(s) con un medio de pago fuera del catálogo quedaron como Efectivo.'';
           UPDATE Contratacion SET IdMedioPago = 1
           WHERE IdMedioPago IS NULL AND MedioPago IS NOT NULL;');
    ALTER TABLE Contratacion DROP COLUMN MedioPago;
    PRINT 'Contratacion.MedioPago migrado a IdMedioPago.';
END
GO

-- (3) Intentos de cobro fallidos
IF OBJECT_ID('ContratacionIntentoPago', 'U') IS NULL
BEGIN
    CREATE TABLE ContratacionIntentoPago (
        IdIntento      INT IDENTITY(1,1) PRIMARY KEY,
        IdContratacion INT           NOT NULL CONSTRAINT FK_IntentoPago_Contratacion REFERENCES Contratacion(IdContratacion),
        NroIntento     INT           NOT NULL CONSTRAINT CHK_IntentoPago_Nro CHECK (NroIntento BETWEEN 1 AND 3),
        Fecha          DATETIME      NOT NULL DEFAULT GETDATE(),
        IdMedioPago    INT           NULL     CONSTRAINT FK_IntentoPago_MedioPago REFERENCES MedioPago(IdMedioPago),
        Motivo         NVARCHAR(200) NOT NULL,
        IdCaja         INT           NULL     CONSTRAINT FK_IntentoPago_Caja REFERENCES Empleado(IdEmpleado),
        CONSTRAINT UX_IntentoPago_Nro UNIQUE (IdContratacion, NroIntento)
    );
    PRINT 'Tabla ContratacionIntentoPago creada.';
END
GO

-- Migración del contador: un registro por intento ya contado, y baja de la columna derivable.
IF COL_LENGTH('Contratacion', 'IntentosPago') IS NOT NULL
BEGIN
    EXEC(N'INSERT INTO ContratacionIntentoPago (IdContratacion, NroIntento, Fecha, IdMedioPago, Motivo, IdCaja)
           SELECT c.IdContratacion, n.Nro, ISNULL(c.FechaResolucion, c.FechaAlta), NULL,
                  N''Intento registrado antes del detalle por intento'', c.IdCaja
           FROM Contratacion c
           JOIN (VALUES (1), (2), (3)) AS n(Nro) ON n.Nro <= c.IntentosPago
           WHERE NOT EXISTS (SELECT 1 FROM ContratacionIntentoPago i
                             WHERE i.IdContratacion = c.IdContratacion AND i.NroIntento = n.Nro);');
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Contratacion_Intentos')
        ALTER TABLE Contratacion DROP CONSTRAINT CK_Contratacion_Intentos;
    DECLARE @df SYSNAME = (SELECT d.name FROM sys.default_constraints d
                           JOIN sys.columns col ON col.object_id = d.parent_object_id AND col.column_id = d.parent_column_id
                           WHERE d.parent_object_id = OBJECT_ID('Contratacion') AND col.name = 'IntentosPago');
    IF @df IS NOT NULL EXEC(N'ALTER TABLE Contratacion DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE Contratacion DROP COLUMN IntentosPago;
    PRINT 'Contratacion.IntentosPago migrado a ContratacionIntentoPago.';
END
GO

-- (4) Desistimientos ("¿Elige plan y modalidad? No → Asentar desistimiento")
IF OBJECT_ID('DesistimientoContratacion', 'U') IS NULL
BEGIN
    CREATE TABLE DesistimientoContratacion (
        IdDesistimiento INT IDENTITY(1,1) PRIMARY KEY,
        IdCliente       INT           NOT NULL CONSTRAINT FK_DesistContr_Cliente  REFERENCES Cliente(IdCliente),
        IdPlan          INT           NULL     CONSTRAINT FK_DesistContr_Plan     REFERENCES PlanSuscripcion(IdPlan),
        Modalidad       INT           NULL     CONSTRAINT CHK_DesistContr_Modalidad CHECK (Modalidad IN (0,1,2)),
        Motivo          NVARCHAR(200) NOT NULL,
        Fecha           DATETIME      NOT NULL DEFAULT GETDATE(),
        IdVendedor      INT           NOT NULL CONSTRAINT FK_DesistContr_Vendedor REFERENCES Empleado(IdEmpleado),
        CONSTRAINT CHK_DesistContr_ModalidadConPlan CHECK (IdPlan IS NOT NULL OR Modalidad IS NULL)
    );
    PRINT 'Tabla DesistimientoContratacion creada.';
END
GO

-- Bases donde la tabla ya existía sin esta restricción (creada antes de agregarla).
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_DesistContr_ModalidadConPlan')
   AND NOT EXISTS (SELECT 1 FROM DesistimientoContratacion WHERE IdPlan IS NULL AND Modalidad IS NOT NULL)
    ALTER TABLE DesistimientoContratacion ADD CONSTRAINT CHK_DesistContr_ModalidadConPlan
        CHECK (IdPlan IS NOT NULL OR Modalidad IS NULL);
GO

-- (5) Integridad: una contratación Pagada tiene medio de pago y comprobante; el comprobante es único.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Contratacion_Pagada')
   AND NOT EXISTS (SELECT 1 FROM Contratacion WHERE Estado = 1 AND (IdMedioPago IS NULL OR NumeroComprobante IS NULL))
    ALTER TABLE Contratacion ADD CONSTRAINT CHK_Contratacion_Pagada
        CHECK (Estado <> 1 OR (IdMedioPago IS NOT NULL AND NumeroComprobante IS NOT NULL));
ELSE IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Contratacion_Pagada')
    PRINT 'AVISO: CHK_Contratacion_Pagada no creada: hay contrataciones Pagadas sin medio de pago o sin comprobante.';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Contratacion_Comprobante' AND object_id = OBJECT_ID('Contratacion'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Contratacion_Comprobante
        ON Contratacion(NumeroComprobante) WHERE NumeroComprobante IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IntentoPago_Contratacion' AND object_id = OBJECT_ID('ContratacionIntentoPago'))
    CREATE NONCLUSTERED INDEX IX_IntentoPago_Contratacion ON ContratacionIntentoPago(IdContratacion);
PRINT 'Sección 20c: medios de pago, intentos y desistimientos de PN02 verificados.';
GO
-- ============================================================
-- WardrobeFlow — 20c2. PN02: PAGO EN CUOTAS CON TARJETA DE CRÉDITO
-- ------------------------------------------------------------
-- La tarjeta financia: Caja cobra el total en un solo cobro y se registra el plan de cuotas
-- elegido y el recargo por financiación (decisión de la alumna).
--   · MedioPago: se separa «Tarjeta» en «Tarjeta de débito» y «Tarjeta de crédito», el único medio que
--     permite cuotas (PermiteCuotas = 1). Si la base YA tiene cobros o intentos con «Tarjeta», no se sabe
--     si fueron débito o crédito: «Tarjeta» queda como medio histórico (Activo = 0: no se ofrece ni se
--     acepta en cobros nuevos) y «Tarjeta de débito» se crea aparte. Sin cobros, «Tarjeta» se renombra.
--   · PlanCuotas: catálogo de planes (1 cuota sin interés; 3 cuotas 5 %; 6 cuotas 10 %; 12 cuotas 20 %).
--   · Contratacion: IdPlanCuotas (FK) y RecargoCuotas (recargo cobrado, en pesos; se guarda porque
--     el porcentaje puede cambiar y el comprobante se reimprime). El valor de cada cuota se deriva.
--   Regla de BLL (BE.PoliticaCuotas): no más cuotas que los meses de la modalidad (1 / 3 / 12).
--   Las columnas nuevas entran al DVH de Contratacion: si se agregan sobre una base que ya tenía
--   los dígitos calculados, se pide el recálculo ('DVReinicializar', ver sección de DV).
-- Idempotente.
-- ============================================================
IF COL_LENGTH('MedioPago', 'PermiteCuotas') IS NULL
    ALTER TABLE MedioPago ADD PermiteCuotas BIT NOT NULL CONSTRAINT DF_MedioPago_PermiteCuotas DEFAULT 0;
IF COL_LENGTH('MedioPago', 'Activo') IS NULL
    ALTER TABLE MedioPago ADD Activo BIT NOT NULL CONSTRAINT DF_MedioPago_Activo DEFAULT 1;
GO
IF EXISTS (SELECT 1 FROM MedioPago WHERE IdMedioPago = 2 AND Nombre = N'Tarjeta')
BEGIN
    IF EXISTS (SELECT 1 FROM Contratacion WHERE IdMedioPago = 2)
       OR EXISTS (SELECT 1 FROM ContratacionIntentoPago WHERE IdMedioPago = 2)
    BEGIN
        UPDATE MedioPago SET Nombre = N'Tarjeta (anterior a débito/crédito)', ClaveTraduccion = 'medio.tarjeta_historica', Activo = 0
        WHERE IdMedioPago = 2;
        IF NOT EXISTS (SELECT 1 FROM MedioPago WHERE IdMedioPago = 5)
            INSERT INTO MedioPago (IdMedioPago, Nombre, ClaveTraduccion, PermiteCuotas, Activo)
            VALUES (5, N'Tarjeta de débito', 'medio.tarjeta_debito', 0, 1);
        PRINT 'MedioPago: «Tarjeta» tiene cobros o intentos: queda como medio histórico; se creó «Tarjeta de débito».';
    END
    ELSE
        UPDATE MedioPago SET Nombre = N'Tarjeta de débito', ClaveTraduccion = 'medio.tarjeta_debito' WHERE IdMedioPago = 2;
END
IF NOT EXISTS (SELECT 1 FROM MedioPago WHERE IdMedioPago = 4)
    INSERT INTO MedioPago (IdMedioPago, Nombre, ClaveTraduccion, PermiteCuotas)
    VALUES (4, N'Tarjeta de crédito', 'medio.tarjeta_credito', 1);
UPDATE MedioPago SET PermiteCuotas = 1 WHERE IdMedioPago = 4 AND PermiteCuotas = 0;
GO

IF OBJECT_ID('PlanCuotas', 'U') IS NULL
BEGIN
    CREATE TABLE PlanCuotas (
        IdPlanCuotas      INT          NOT NULL PRIMARY KEY,
        CantidadCuotas    INT          NOT NULL CONSTRAINT UX_PlanCuotas_Cantidad UNIQUE,
        RecargoPorcentaje DECIMAL(5,2) NOT NULL,
        Activo            BIT          NOT NULL CONSTRAINT DF_PlanCuotas_Activo DEFAULT 1,
        CONSTRAINT CHK_PlanCuotas_Cantidad CHECK (CantidadCuotas >= 1),
        CONSTRAINT CHK_PlanCuotas_Recargo  CHECK (RecargoPorcentaje >= 0 AND RecargoPorcentaje <= 100)
    );
    PRINT 'Tabla PlanCuotas creada.';
END
GO
INSERT INTO PlanCuotas (IdPlanCuotas, CantidadCuotas, RecargoPorcentaje)
SELECT v.Id, v.Cuotas, v.Recargo
FROM (VALUES (1, 1, 0.00), (2, 3, 5.00), (3, 6, 10.00), (4, 12, 20.00)) AS v(Id, Cuotas, Recargo)
WHERE NOT EXISTS (SELECT 1 FROM PlanCuotas p WHERE p.IdPlanCuotas = v.Id);
GO

DECLARE @cuotasNuevas BIT = 0;
IF COL_LENGTH('Contratacion', 'IdPlanCuotas') IS NULL
BEGIN
    ALTER TABLE Contratacion ADD IdPlanCuotas INT NULL
        CONSTRAINT FK_Contratacion_PlanCuotas REFERENCES PlanCuotas(IdPlanCuotas);
    SET @cuotasNuevas = 1;
END
IF COL_LENGTH('Contratacion', 'RecargoCuotas') IS NULL
BEGIN
    ALTER TABLE Contratacion ADD RecargoCuotas DECIMAL(10,2) NULL;   -- igual que Importe
    SET @cuotasNuevas = 1;
END
-- Base ya instalada con los dígitos calculados: las columnas nuevas cambian el DVH de Contratacion.
IF @cuotasNuevas = 1 AND OBJECT_ID('ParametroSistema') IS NOT NULL
   AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'FormatoDV' AND TRY_CONVERT(INT, Valor) >= 2)
BEGIN
    MERGE ParametroSistema AS t
    USING (VALUES ('DVReinicializar', '1')) AS s(Clave, Valor) ON t.Clave = s.Clave
    WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
    WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
    PRINT 'Contratacion: columnas de cuotas agregadas; se pidió el recálculo de los dígitos verificadores.';
END
GO

-- Las dos columnas van juntas: con plan de cuotas hay recargo (0 si es sin interés); sin plan, ninguno.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Contratacion_Cuotas')
    ALTER TABLE Contratacion ADD CONSTRAINT CHK_Contratacion_Cuotas
        CHECK ((IdPlanCuotas IS NULL AND RecargoCuotas IS NULL) OR (IdPlanCuotas IS NOT NULL AND RecargoCuotas >= 0));
PRINT 'Sección 20c2: pago en cuotas con tarjeta de crédito verificado.';
GO
-- ============================================================
-- WardrobeFlow — 20c3. PN02: UPGRADE CON CRÉDITO Y ANULACIÓN DE CONTRATACIONES
-- ------------------------------------------------------------
--   · Contratacion.CreditoCambioPlan: al pasar a un plan más caro con el período vigente, el plan
--     nuevo rige desde hoy y los días no usados del plan anterior se descuentan del cobro
--     (BE.PoliticaCambioPlan). Se guarda para el comprobante.
--   · Contratacion.MotivoAnulacion: Caja puede anular una contratación pendiente (el cliente se
--     arrepintió, error de carga) sin inventar intentos de pago fallidos.
--   Las columnas entran al DVH de Contratacion: si se agregan sobre una base con los dígitos ya
--   calculados, se pide el recálculo (igual que la sección 20c2).
-- Idempotente.
-- ============================================================
DECLARE @colsNuevas BIT = 0;
IF COL_LENGTH('Contratacion', 'CreditoCambioPlan') IS NULL
BEGIN
    ALTER TABLE Contratacion ADD CreditoCambioPlan DECIMAL(10,2) NULL;   -- igual que Importe
    SET @colsNuevas = 1;
END
IF COL_LENGTH('Contratacion', 'MotivoAnulacion') IS NULL
BEGIN
    ALTER TABLE Contratacion ADD MotivoAnulacion NVARCHAR(200) NULL;    -- BLL.Contratacion.LargoMaximoMotivo
    SET @colsNuevas = 1;
END
IF @colsNuevas = 1 AND OBJECT_ID('ParametroSistema') IS NOT NULL
   AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'FormatoDV' AND TRY_CONVERT(INT, Valor) >= 2)
BEGIN
    MERGE ParametroSistema AS t
    USING (VALUES ('DVReinicializar', '1')) AS s(Clave, Valor) ON t.Clave = s.Clave
    WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
    WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
    PRINT 'Contratacion: columnas de upgrade/anulación agregadas; se pidió el recálculo de los dígitos verificadores.';
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Contratacion_CreditoCambioPlan')
    ALTER TABLE Contratacion ADD CONSTRAINT CHK_Contratacion_CreditoCambioPlan
        CHECK (CreditoCambioPlan IS NULL OR CreditoCambioPlan > 0);
-- Solo una contratación Cancelada (2) puede tener motivo de anulación.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Contratacion_MotivoAnulacion')
    ALTER TABLE Contratacion ADD CONSTRAINT CHK_Contratacion_MotivoAnulacion
        CHECK (MotivoAnulacion IS NULL OR Estado = 2);
PRINT 'Sección 20c3: upgrade con crédito y anulación de contrataciones verificado.';
GO
-- ============================================================
-- WardrobeFlow — 20c4. N01: COBRO RECURRENTE CON MEDIO DE PAGO, COMPROBANTE Y PROMOCIÓN APLICADA
-- ------------------------------------------------------------
--   HistorialCobro guarda, igual que Contratacion en PN02, con qué medio pagó el cliente, el
--   comprobante emitido, la modalidad cobrada y el descuento aplicado (promoción vigente o crédito
--   por referido). Con IdPromocion + DescuentoAplicado se puede medir el impacto de cada promoción
--   en los cobros (PN03). Las filas viejas quedan en NULL. HistorialCobro no lleva DVH.
-- Idempotente.
-- ============================================================
IF COL_LENGTH('HistorialCobro', 'IdMedioPago') IS NULL
    ALTER TABLE HistorialCobro ADD IdMedioPago INT NULL
        CONSTRAINT FK_HistorialCobro_MedioPago REFERENCES MedioPago(IdMedioPago);
IF COL_LENGTH('HistorialCobro', 'NumeroComprobante') IS NULL
    ALTER TABLE HistorialCobro ADD NumeroComprobante NVARCHAR(50) NULL;
IF COL_LENGTH('HistorialCobro', 'Modalidad') IS NULL
    ALTER TABLE HistorialCobro ADD Modalidad INT NULL;
IF COL_LENGTH('HistorialCobro', 'DescuentoAplicado') IS NULL
    ALTER TABLE HistorialCobro ADD DescuentoAplicado DECIMAL(10,2) NULL;
IF COL_LENGTH('HistorialCobro', 'IdPromocion') IS NULL
    ALTER TABLE HistorialCobro ADD IdPromocion INT NULL
        CONSTRAINT FK_HistorialCobro_Promocion REFERENCES Promocion(IdPromocion);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HistorialCobro_Modalidad')
    ALTER TABLE HistorialCobro ADD CONSTRAINT CHK_HistorialCobro_Modalidad CHECK (Modalidad IS NULL OR Modalidad IN (0,1,2));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_HistorialCobro_Descuento')
    ALTER TABLE HistorialCobro ADD CONSTRAINT CHK_HistorialCobro_Descuento CHECK (DescuentoAplicado IS NULL OR DescuentoAplicado > 0);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_HistorialCobro_IdPromocion' AND object_id = OBJECT_ID('HistorialCobro'))
    CREATE NONCLUSTERED INDEX IX_HistorialCobro_IdPromocion ON HistorialCobro(IdPromocion);
PRINT 'Sección 20c4: medio de pago, comprobante y promoción del cobro recurrente verificados.';
GO
-- ============================================================
-- WardrobeFlow — 20c5. CLIENTE: MEDIO DE PAGO PREFERIDO POR CATÁLOGO (3FN)
-- ------------------------------------------------------------
--   Cliente.MetodoPago era texto libre ("Efectivo", "Crédito"...) con los mismos valores que el
--   catálogo MedioPago de Caja escrito de otra forma: el nombre del medio dependía del cliente y no
--   de la clave. Pasa a Cliente.IdMedioPagoPreferido (FK a MedioPago).
--   Migración del texto (sin perder datos): se mapea por la clave de traducción del catálogo
--   (el Id de «Tarjeta de débito» puede ser 2 o 5 según la base, ver 20c2) y después por nombre;
--   un texto que no está en el catálogo se conserva como medio HISTÓRICO (Activo = 0: no se ofrece
--   en cobros nuevos ni en el alta de clientes; Ids desde 100). Vacío → NULL. Después se quita la
--   columna vieja (y su DEFAULT, si lo tenía).
--   La columna entra en el dígito verificador de Cliente (DAL.Cliente.DV_Columnas): si la base ya
--   tenía la columna vieja, se pide el recálculo ('DVReinicializar', sección 22).
-- Idempotente.
-- ============================================================
IF COL_LENGTH('Cliente', 'IdMedioPagoPreferido') IS NULL
BEGIN
    ALTER TABLE Cliente ADD IdMedioPagoPreferido INT NULL
        CONSTRAINT FK_Cliente_MedioPagoPreferido REFERENCES MedioPago(IdMedioPago);
    PRINT 'Cliente: columna IdMedioPagoPreferido agregada.';
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Cliente_IdMedioPagoPreferido' AND object_id = OBJECT_ID('Cliente'))
    CREATE NONCLUSTERED INDEX IX_Cliente_IdMedioPagoPreferido ON Cliente(IdMedioPagoPreferido);
GO

IF COL_LENGTH('Cliente', 'MetodoPago') IS NOT NULL
BEGIN
    -- Dinámico: en una base nueva la columna ya no existe y el lote no compilaría.
    EXEC(N'
        DECLARE @mapa TABLE (Texto NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY, IdMedioPago INT NULL);
        INSERT INTO @mapa (Texto)
        SELECT DISTINCT LTRIM(RTRIM(MetodoPago)) FROM Cliente
        WHERE MetodoPago IS NOT NULL AND LTRIM(RTRIM(MetodoPago)) <> N'''';

        -- 1) Por la clave del catálogo (los valores que ofrecía el formulario de clientes).
        UPDATE m SET m.IdMedioPago = mp.IdMedioPago
        FROM @mapa m
        JOIN MedioPago mp ON mp.ClaveTraduccion =
             CASE m.Texto
                  WHEN N''Efectivo''      THEN ''medio.efectivo''
                  WHEN N''Débito''        THEN ''medio.tarjeta_debito''
                  WHEN N''Debito''        THEN ''medio.tarjeta_debito''
                  WHEN N''Crédito''       THEN ''medio.tarjeta_credito''
                  WHEN N''Credito''       THEN ''medio.tarjeta_credito''
                  WHEN N''Transferencia'' THEN ''medio.transferencia''
             END;

        -- 2) Por el nombre del catálogo ("Tarjeta de débito", "Tarjeta de crédito"...).
        UPDATE m SET m.IdMedioPago = mp.IdMedioPago
        FROM @mapa m JOIN MedioPago mp ON mp.Nombre = LEFT(m.Texto, 50)
        WHERE m.IdMedioPago IS NULL;

        -- 3) Fuera del catálogo: medio histórico inactivo, para no perder el dato del cliente.
        DECLARE @base INT = (SELECT CASE WHEN ISNULL(MAX(IdMedioPago), 0) < 100 THEN 100 ELSE MAX(IdMedioPago) + 1 END FROM MedioPago);
        INSERT INTO MedioPago (IdMedioPago, Nombre, ClaveTraduccion, PermiteCuotas, Activo)
        SELECT @base + ROW_NUMBER() OVER (ORDER BY n.Nombre) - 1, n.Nombre,
               N''medio.historico.'' + CAST(@base + ROW_NUMBER() OVER (ORDER BY n.Nombre) - 1 AS NVARCHAR(10)), 0, 0
        FROM (SELECT DISTINCT LEFT(Texto, 50) AS Nombre FROM @mapa WHERE IdMedioPago IS NULL) n
        WHERE NOT EXISTS (SELECT 1 FROM MedioPago mp WHERE mp.Nombre = n.Nombre);
        DECLARE @historicos INT = @@ROWCOUNT;
        UPDATE m SET m.IdMedioPago = mp.IdMedioPago
        FROM @mapa m JOIN MedioPago mp ON mp.Nombre = LEFT(m.Texto, 50)
        WHERE m.IdMedioPago IS NULL;

        UPDATE c SET c.IdMedioPagoPreferido = m.IdMedioPago
        FROM Cliente c JOIN @mapa m ON m.Texto = LTRIM(RTRIM(c.MetodoPago))
        WHERE c.IdMedioPagoPreferido IS NULL;
        DECLARE @migrados INT = @@ROWCOUNT;
        PRINT ''Cliente.MetodoPago migrado a IdMedioPagoPreferido: '' + CAST(@migrados AS NVARCHAR(10)) +
              '' cliente(s); medios históricos creados: '' + CAST(@historicos AS NVARCHAR(10)) + ''.'';');

    DECLARE @dfMetodo SYSNAME = (SELECT d.name FROM sys.default_constraints d
                                 JOIN sys.columns col ON col.object_id = d.parent_object_id AND col.column_id = d.parent_column_id
                                 WHERE d.parent_object_id = OBJECT_ID('Cliente') AND col.name = 'MetodoPago');
    IF @dfMetodo IS NOT NULL EXEC(N'ALTER TABLE Cliente DROP CONSTRAINT [' + @dfMetodo + N']');
    ALTER TABLE Cliente DROP COLUMN MetodoPago;

    -- El DVH de Cliente cambia de columna: recálculo en la sección 22.
    MERGE ParametroSistema AS t
    USING (VALUES (N'DVReinicializar', N'1')) AS s(Clave, Valor) ON t.Clave = s.Clave
    WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
    WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
    PRINT 'Cliente: columna MetodoPago quitada; DV a recalcular.';
END
PRINT 'Sección 20c5: medio de pago preferido del cliente por catálogo verificado.';
GO
-- ============================================================
-- WardrobeFlow — 20d. PN03: FLUJO APROBADO DE PROMOCIONES
-- ------------------------------------------------------------
-- Diagrama de actividad de PN03 (corregido y aprobado):
--   · SugerenciaPromocion: estado Descartada (2) con motivo obligatorio, origen de la
--     métrica (0=Abandono, 1=Rotación, 2=Manual, BE.OrigenMetrica), quién la creó y
--     cuándo la evaluó Administración.
--   · Promocion: estados Descartada (5) y Vencida (6); quién la creó (IdUsuarioAlta: no
--     puede dictaminarla).
--   · PromocionHistorial: una fila por transición de estado.
--   · DictamenContable: «Dictamen contable» (resultado, observación, usuario, fecha).
--   · SolicitudBajaPromocion: «Solicitud de baja» y su «Resolución de baja»
--     (0=Pendiente, 1=Aprobada, 2=Rechazada, BE.EstadoSolicitudBaja).
--   Promocion.Observacion y Promocion.MotivoBaja se migran a esas tablas y se quitan (3FN).
-- Idempotente: sirve para una instalación nueva y migra una base existente sin perder datos.
-- Las sentencias que nombran columnas que pueden no existir todavía van con EXEC (dinámico).
-- ============================================================
SET QUOTED_IDENTIFIER ON;
GO

-- (1) Estados nuevos en los CHECK.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SugerenciaPromocion_Estado' AND definition NOT LIKE '%(2)%')
    ALTER TABLE SugerenciaPromocion DROP CONSTRAINT CHK_SugerenciaPromocion_Estado;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SugerenciaPromocion_Estado')
    ALTER TABLE SugerenciaPromocion ADD CONSTRAINT CHK_SugerenciaPromocion_Estado CHECK (Estado IN (0,1,2));
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Estado' AND definition NOT LIKE '%(6)%')
    ALTER TABLE Promocion DROP CONSTRAINT CHK_Promocion_Estado;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Estado')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Estado CHECK (Estado IN (0,1,2,3,4,5,6));
GO

-- (2) SugerenciaPromocion: origen de la métrica, creador, motivo de descarte y fecha de evaluación.
IF COL_LENGTH('SugerenciaPromocion', 'OrigenMetrica') IS NULL
    ALTER TABLE SugerenciaPromocion ADD OrigenMetrica INT NOT NULL
        CONSTRAINT DF_SugerenciaPromocion_Origen DEFAULT 2;   -- las existentes quedan como Manual
IF COL_LENGTH('SugerenciaPromocion', 'IdUsuarioAlta') IS NULL
    ALTER TABLE SugerenciaPromocion ADD IdUsuarioAlta INT NULL;
IF COL_LENGTH('SugerenciaPromocion', 'MotivoDescarte') IS NULL
    ALTER TABLE SugerenciaPromocion ADD MotivoDescarte NVARCHAR(500) NULL;
IF COL_LENGTH('SugerenciaPromocion', 'FechaEvaluacion') IS NULL
    ALTER TABLE SugerenciaPromocion ADD FechaEvaluacion DATETIME NULL;
-- (3) Promocion: quién la creó.
IF COL_LENGTH('Promocion', 'IdUsuarioAlta') IS NULL
    ALTER TABLE Promocion ADD IdUsuarioAlta INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SugerenciaPromocion_Origen')
    EXEC(N'ALTER TABLE SugerenciaPromocion ADD CONSTRAINT CHK_SugerenciaPromocion_Origen CHECK (OrigenMetrica IN (0,1,2))');
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SugerenciaPromocion_Descarte')
    EXEC(N'ALTER TABLE SugerenciaPromocion ADD CONSTRAINT CHK_SugerenciaPromocion_Descarte
           CHECK (Estado <> 2 OR (MotivoDescarte IS NOT NULL AND FechaEvaluacion IS NOT NULL))');
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SugerenciaPromocion_UsuarioAlta')
    EXEC(N'ALTER TABLE SugerenciaPromocion ADD CONSTRAINT FK_SugerenciaPromocion_UsuarioAlta
           FOREIGN KEY (IdUsuarioAlta) REFERENCES Usuario(IdUsuario)');
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Promocion_UsuarioAlta')
    EXEC(N'ALTER TABLE Promocion ADD CONSTRAINT FK_Promocion_UsuarioAlta
           FOREIGN KEY (IdUsuarioAlta) REFERENCES Usuario(IdUsuario)');
GO

-- (4) Historial de estados: una fila por transición.
IF OBJECT_ID('PromocionHistorial', 'U') IS NULL
BEGIN
    CREATE TABLE PromocionHistorial (
        IdHistorial    INT IDENTITY(1,1) PRIMARY KEY,
        IdPromocion    INT           NOT NULL CONSTRAINT FK_PromocionHistorial_Promocion REFERENCES Promocion(IdPromocion),
        EstadoAnterior INT           NULL     CONSTRAINT CHK_PromocionHistorial_Anterior CHECK (EstadoAnterior IN (0,1,2,3,4,5,6)),
        EstadoNuevo    INT           NOT NULL CONSTRAINT CHK_PromocionHistorial_Nuevo CHECK (EstadoNuevo IN (0,1,2,3,4,5,6)),
        IdUsuario      INT           NULL     CONSTRAINT FK_PromocionHistorial_Usuario REFERENCES Usuario(IdUsuario),
        Fecha          DATETIME      NOT NULL DEFAULT GETDATE(),
        Observacion    NVARCHAR(500) NULL
    );
    CREATE NONCLUSTERED INDEX IX_PromocionHistorial_Promocion ON PromocionHistorial(IdPromocion);
    PRINT 'Tabla PromocionHistorial creada.';
END
GO

-- (5) «Dictamen contable».
IF OBJECT_ID('DictamenContable', 'U') IS NULL
BEGIN
    CREATE TABLE DictamenContable (
        IdDictamen  INT IDENTITY(1,1) PRIMARY KEY,
        IdPromocion INT           NOT NULL CONSTRAINT FK_DictamenContable_Promocion REFERENCES Promocion(IdPromocion),
        IdUsuario   INT           NOT NULL CONSTRAINT FK_DictamenContable_Usuario REFERENCES Usuario(IdUsuario),
        Aprobada    BIT           NOT NULL,
        Observacion NVARCHAR(500) NOT NULL,
        Fecha       DATETIME      NOT NULL DEFAULT GETDATE()
    );
    CREATE NONCLUSTERED INDEX IX_DictamenContable_Promocion ON DictamenContable(IdPromocion);
    PRINT 'Tabla DictamenContable creada.';
END
GO

-- (6) «Solicitud de baja» y su «Resolución de baja».
IF OBJECT_ID('SolicitudBajaPromocion', 'U') IS NULL
BEGIN
    CREATE TABLE SolicitudBajaPromocion (
        IdSolicitud       INT IDENTITY(1,1) PRIMARY KEY,
        IdPromocion       INT           NOT NULL CONSTRAINT FK_SolicitudBaja_Promocion REFERENCES Promocion(IdPromocion),
        IdUsuarioSolicita INT           NOT NULL CONSTRAINT FK_SolicitudBaja_Solicita REFERENCES Usuario(IdUsuario),
        Motivo            NVARCHAR(500) NOT NULL,
        FechaSolicitud    DATETIME      NOT NULL DEFAULT GETDATE(),
        Estado            INT           NOT NULL DEFAULT 0 CONSTRAINT CHK_SolicitudBaja_Estado CHECK (Estado IN (0,1,2)),
        IdUsuarioResuelve INT           NULL     CONSTRAINT FK_SolicitudBaja_Resuelve REFERENCES Usuario(IdUsuario),
        MotivoResolucion  NVARCHAR(500) NULL,
        FechaResolucion   DATETIME      NULL,
        -- Pendiente sin resolver; resuelta con quién y cuándo; el rechazo exige motivo.
        CONSTRAINT CHK_SolicitudBaja_Resolucion CHECK (
            (Estado = 0 AND IdUsuarioResuelve IS NULL AND FechaResolucion IS NULL) OR
            (Estado <> 0 AND IdUsuarioResuelve IS NOT NULL AND FechaResolucion IS NOT NULL)),
        CONSTRAINT CHK_SolicitudBaja_MotivoRechazo CHECK (Estado <> 2 OR MotivoResolucion IS NOT NULL)
    );
    CREATE NONCLUSTERED INDEX IX_SolicitudBaja_Promocion ON SolicitudBajaPromocion(IdPromocion);
    -- Una sola solicitud pendiente por promoción.
    CREATE UNIQUE NONCLUSTERED INDEX UX_SolicitudBaja_UnaPendiente ON SolicitudBajaPromocion(IdPromocion) WHERE Estado = 0;
    PRINT 'Tabla SolicitudBajaPromocion creada.';
END
GO

-- (7) Migración de datos existentes.
-- Historial inicial de las promociones que todavía no tienen ninguna transición registrada.
INSERT INTO PromocionHistorial (IdPromocion, EstadoAnterior, EstadoNuevo, IdUsuario, Fecha, Observacion)
SELECT p.IdPromocion, NULL, p.Estado, NULL, p.FechaAlta, N'Estado al incorporar el historial de PN03'
FROM Promocion p
WHERE NOT EXISTS (SELECT 1 FROM PromocionHistorial h WHERE h.IdPromocion = p.IdPromocion);
GO

-- Promocion.Observacion → «Dictamen contable» (la firma un usuario de Contabilidad o, si no hay, el primero).
IF COL_LENGTH('Promocion', 'Observacion') IS NOT NULL
BEGIN
    EXEC(N'DECLARE @u INT = COALESCE((SELECT TOP 1 IdUsuario FROM Usuario WHERE Rol = ''Contabilidad'' ORDER BY IdUsuario),
                                     (SELECT MIN(IdUsuario) FROM Usuario));
           INSERT INTO DictamenContable (IdPromocion, IdUsuario, Aprobada, Observacion, Fecha)
           SELECT p.IdPromocion, @u, CASE WHEN p.Estado = 2 THEN 0 ELSE 1 END, p.Observacion, p.FechaAlta
           FROM Promocion p
           WHERE @u IS NOT NULL AND p.Observacion IS NOT NULL AND p.Estado <> 0
             AND NOT EXISTS (SELECT 1 FROM DictamenContable d WHERE d.IdPromocion = p.IdPromocion);');
    ALTER TABLE Promocion DROP COLUMN Observacion;
    PRINT 'Promocion.Observacion migrada a DictamenContable.';
END
GO

-- Promocion.MotivoBaja → «Solicitud de baja» (pendiente si sigue con baja solicitada; aprobada si
-- quedó desactivada; rechazada si volvió a estar vigente).
IF COL_LENGTH('Promocion', 'MotivoBaja') IS NOT NULL
BEGIN
    EXEC(N'DECLARE @vend INT = COALESCE((SELECT TOP 1 IdUsuario FROM Usuario WHERE Rol = ''Vendedor'' ORDER BY IdUsuario),
                                        (SELECT MIN(IdUsuario) FROM Usuario));
           DECLARE @adm INT = COALESCE((SELECT TOP 1 IdUsuario FROM Usuario WHERE Rol = ''AdministracionComercial'' ORDER BY IdUsuario),
                                       (SELECT MIN(IdUsuario) FROM Usuario));
           INSERT INTO SolicitudBajaPromocion (IdPromocion, IdUsuarioSolicita, Motivo, FechaSolicitud, Estado,
                                               IdUsuarioResuelve, MotivoResolucion, FechaResolucion)
           SELECT p.IdPromocion, @vend, p.MotivoBaja, p.FechaAlta,
                  CASE p.Estado WHEN 3 THEN 0 WHEN 4 THEN 1 ELSE 2 END,
                  CASE WHEN p.Estado = 3 THEN NULL ELSE @adm END,
                  CASE WHEN p.Estado IN (3, 4) THEN NULL ELSE N''Resolución anterior al registro de la solicitud de baja'' END,
                  CASE WHEN p.Estado = 3 THEN NULL ELSE GETDATE() END
           FROM Promocion p
           WHERE @vend IS NOT NULL AND p.MotivoBaja IS NOT NULL
             AND NOT EXISTS (SELECT 1 FROM SolicitudBajaPromocion s WHERE s.IdPromocion = p.IdPromocion);');
    ALTER TABLE Promocion DROP COLUMN MotivoBaja;
    PRINT 'Promocion.MotivoBaja migrado a SolicitudBajaPromocion.';
END
GO
PRINT 'Sección 20d: flujo aprobado de PN03 (historial, dictamen, solicitud de baja, vencimiento) verificado.';
GO
-- ============================================================
-- WardrobeFlow — 20e. CATÁLOGO DE CATEGORÍAS DE PRENDA (3FN)
-- ------------------------------------------------------------
--   Prenda.Categoria, Promocion.CategoriaPrenda y SugerenciaPromocion.CategoriaPrenda eran texto
--   libre: la misma categoría podía escribirse de varias formas ("Vestido" en los datos y
--   "Vestidos" en el formulario de prendas), y una promoción por categoría podía no aplicar a
--   ninguna prenda. Pasa a ser un catálogo (Categoria) referenciado por CLAVE NATURAL
--   (Categoria.Nombre, UNIQUE) con ON UPDATE CASCADE: renombrar una categoría actualiza sus
--   prendas y promociones, y no hace falta cambiar el tipo de las columnas existentes.
--   Migración: se recortan los espacios; el plural que ofrecía el formulario viejo pasa al
--   singular de los datos ("Vestidos" → "Vestido", "Blazers" → "Saco"); todo texto que quede y
--   no esté en el catálogo se agrega (no se pierde ningún dato). Una prenda sin categoría queda
--   NULL; una promoción o sugerencia con la categoría en blanco pasa a "Otro" (el CHECK de
--   destino exige categoría cuando no hay plan). Va antes de la 21e: los datos de demo usan
--   las categorías del catálogo. Prenda y Promocion no tienen dígito verificador.
-- Idempotente.
-- ============================================================
IF OBJECT_ID('Categoria', 'U') IS NULL
BEGIN
    CREATE TABLE Categoria (
        IdCategoria INT IDENTITY(1,1) CONSTRAINT PK_Categoria PRIMARY KEY,
        Nombre      NVARCHAR(100) NOT NULL CONSTRAINT UX_Categoria_Nombre UNIQUE,
        Activo      BIT           NOT NULL CONSTRAINT DF_Categoria_Activo DEFAULT 1,
        CONSTRAINT CHK_Categoria_Nombre CHECK (LEN(LTRIM(RTRIM(Nombre))) > 0)
    );
    PRINT 'Tabla Categoria creada.';
END
GO

-- Catálogo inicial (solo con la tabla vacía: después lo administra el negocio).
IF NOT EXISTS (SELECT 1 FROM Categoria)
    INSERT INTO Categoria (Nombre)
    VALUES (N'Abrigo'), (N'Accesorio'), (N'Camisa'), (N'Conjunto'), (N'Falda'), (N'Pantalón'),
           (N'Ropa deportiva'), (N'Saco'), (N'Sweater'), (N'Top'), (N'Vestido'), (N'Otro');
GO

-- Normalización de los textos existentes (antes de las FK; sobre datos ya normalizados no cambia nada).
DECLARE @plural TABLE (Plural NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY, Singular NVARCHAR(100) NOT NULL);
INSERT INTO @plural VALUES (N'Vestidos', N'Vestido'), (N'Faldas', N'Falda'), (N'Pantalones', N'Pantalón'),
                           (N'Tops', N'Top'), (N'Blazers', N'Saco'), (N'Abrigos', N'Abrigo'),
                           (N'Conjuntos', N'Conjunto'), (N'Accesorios', N'Accesorio');
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Prenda_Categoria')
BEGIN
    UPDATE Prenda SET Categoria = NULLIF(LTRIM(RTRIM(Categoria)), N'')
    WHERE Categoria IS NOT NULL AND (DATALENGTH(Categoria) <> DATALENGTH(LTRIM(RTRIM(Categoria))) OR LTRIM(RTRIM(Categoria)) = N'');
    UPDATE p SET p.Categoria = m.Singular FROM Prenda p JOIN @plural m ON m.Plural = p.Categoria;
END
-- Promocion/SugerenciaPromocion.CategoriaPrenda solo existen hasta la sección 20g (que las quita):
-- en una base ya migrada estas sentencias no corren (van con EXEC porque la columna puede no existir).
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Promocion_Categoria')
   AND COL_LENGTH('Promocion', 'CategoriaPrenda') IS NOT NULL
    EXEC(N'UPDATE Promocion SET CategoriaPrenda = ISNULL(NULLIF(LTRIM(RTRIM(CategoriaPrenda)), N''''), N''Otro'')
           WHERE CategoriaPrenda IS NOT NULL AND (DATALENGTH(CategoriaPrenda) <> DATALENGTH(LTRIM(RTRIM(CategoriaPrenda))) OR LTRIM(RTRIM(CategoriaPrenda)) = N'''');
           UPDATE p SET p.CategoriaPrenda = m.Singular
           FROM Promocion p JOIN (VALUES (N''Vestidos'', N''Vestido''), (N''Faldas'', N''Falda''), (N''Pantalones'', N''Pantalón''),
                                         (N''Tops'', N''Top''), (N''Blazers'', N''Saco''), (N''Abrigos'', N''Abrigo''),
                                         (N''Conjuntos'', N''Conjunto''), (N''Accesorios'', N''Accesorio'')) AS m(Plural, Singular)
                ON m.Plural = p.CategoriaPrenda;
           INSERT INTO Categoria (Nombre) SELECT DISTINCT CategoriaPrenda FROM Promocion p
           WHERE CategoriaPrenda IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Categoria c WHERE c.Nombre = p.CategoriaPrenda);');
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SugerenciaPromocion_Categoria')
   AND COL_LENGTH('SugerenciaPromocion', 'CategoriaPrenda') IS NOT NULL
    EXEC(N'UPDATE SugerenciaPromocion SET CategoriaPrenda = ISNULL(NULLIF(LTRIM(RTRIM(CategoriaPrenda)), N''''), N''Otro'')
           WHERE CategoriaPrenda IS NOT NULL AND (DATALENGTH(CategoriaPrenda) <> DATALENGTH(LTRIM(RTRIM(CategoriaPrenda))) OR LTRIM(RTRIM(CategoriaPrenda)) = N'''');
           UPDATE s SET s.CategoriaPrenda = m.Singular
           FROM SugerenciaPromocion s JOIN (VALUES (N''Vestidos'', N''Vestido''), (N''Faldas'', N''Falda''), (N''Pantalones'', N''Pantalón''),
                                                   (N''Tops'', N''Top''), (N''Blazers'', N''Saco''), (N''Abrigos'', N''Abrigo''),
                                                   (N''Conjuntos'', N''Conjunto''), (N''Accesorios'', N''Accesorio'')) AS m(Plural, Singular)
                ON m.Plural = s.CategoriaPrenda;
           INSERT INTO Categoria (Nombre) SELECT DISTINCT CategoriaPrenda FROM SugerenciaPromocion s
           WHERE CategoriaPrenda IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Categoria c WHERE c.Nombre = s.CategoriaPrenda);');

-- Textos de prenda fuera del catálogo: se agregan como categorías (no se pierde el dato).
INSERT INTO Categoria (Nombre)
SELECT DISTINCT p.Categoria FROM Prenda p
WHERE p.Categoria IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Categoria c WHERE c.Nombre = p.Categoria);
IF @@ROWCOUNT > 0 PRINT 'Categoria: se agregaron al catálogo categorías que ya usaban las prendas.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Prenda_Categoria')
    ALTER TABLE Prenda ADD CONSTRAINT FK_Prenda_Categoria
        FOREIGN KEY (Categoria) REFERENCES Categoria(Nombre) ON UPDATE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Promocion_Categoria')
   AND COL_LENGTH('Promocion', 'CategoriaPrenda') IS NOT NULL
    EXEC(N'ALTER TABLE Promocion ADD CONSTRAINT FK_Promocion_Categoria
           FOREIGN KEY (CategoriaPrenda) REFERENCES Categoria(Nombre) ON UPDATE CASCADE');
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SugerenciaPromocion_Categoria')
   AND COL_LENGTH('SugerenciaPromocion', 'CategoriaPrenda') IS NOT NULL
    EXEC(N'ALTER TABLE SugerenciaPromocion ADD CONSTRAINT FK_SugerenciaPromocion_Categoria
           FOREIGN KEY (CategoriaPrenda) REFERENCES Categoria(Nombre) ON UPDATE CASCADE');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Promocion_CategoriaPrenda' AND object_id = OBJECT_ID('Promocion'))
   AND COL_LENGTH('Promocion', 'CategoriaPrenda') IS NOT NULL
    EXEC(N'CREATE NONCLUSTERED INDEX IX_Promocion_CategoriaPrenda ON Promocion(CategoriaPrenda)');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SugerenciaPromocion_CategoriaPrenda' AND object_id = OBJECT_ID('SugerenciaPromocion'))
   AND COL_LENGTH('SugerenciaPromocion', 'CategoriaPrenda') IS NOT NULL
    EXEC(N'CREATE NONCLUSTERED INDEX IX_SugerenciaPromocion_CategoriaPrenda ON SugerenciaPromocion(CategoriaPrenda)');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Prenda_Categoria' AND object_id = OBJECT_ID('Prenda'))
    CREATE NONCLUSTERED INDEX IX_Prenda_Categoria ON Prenda(Categoria);
PRINT 'Sección 20e: catálogo de categorías de prenda verificado.';
GO

-- ============================================================
-- WardrobeFlow — 20g. PN03: PROMOCIONES SOLO POR PLAN Y BAJA SIMPLIFICADA
-- ------------------------------------------------------------
-- Simplificación del flujo de PN03 (BE.EstadoPromocion, BE.EstadoSolicitudBaja):
--   · Estados de Promocion: 0=EnRevisionContable, 1=Vigente, 2=RechazadaContabilidad,
--     4=DadaDeBaja, 6=Vencida. Se quitan 3=BajaSolicitada y 5=Descartada:
--       - una promoción con la baja solicitada (3) vuelve a Vigente (1) y su solicitud queda
--         Pendiente: pedir la baja ya no cambia el estado;
--       - una descartada (5) pasa a Dada de baja (4); el motivo queda en el historial
--         ("Descartada: ...").
--   · SolicitudBajaPromocion: Estado 0=Pendiente, 1=Atendida, con la «Respuesta a la
--     solicitud» (Respuesta, antes MotivoResolucion) y DioDeBaja (1 si Administración estuvo
--     de acuerdo y dio de baja la promoción). Una aprobada (1) queda Atendida con DioDeBaja = 1;
--     una rechazada (2), Atendida con DioDeBaja = 0.
--   · Las promociones por categoría de prenda se discontinúan: se BORRAN con su historial,
--     dictamen y solicitudes (decisión de la alumna, 11/10: nunca descontaron en un cobro) y las
--     sugerencias por categoría todavía pendientes se cierran como Descartadas. Se quitan la FK,
--     el CHECK de destino, los índices y la columna CategoriaPrenda de Promocion y de
--     SugerenciaPromocion, y Promocion.IdPlan pasa a NOT NULL. SugerenciaPromocion.IdPlan queda
--     NULL solo para las sugerencias por categoría ya evaluadas (la BLL exige plan).
--   · Patentes: "Dar de baja" y "Atender solicitudes" usan la patente de escritura de
--     Administración (mnuPromocionesAdminEditar), la misma que ya usaban "Desactivar",
--     "Descartar promoción" y "Resolver baja": los roles no cambian.
--   · Textos de idiomas: se actualizan los de fábrica que cambiaron y se borran las claves que
--     ya no usa la aplicación (las nuevas las carga la aplicación desde traducciones.tsv).
-- Idempotente: en una base ya migrada no cambia nada. Va antes de la 21e (datos de demo).
-- ============================================================
SET QUOTED_IDENTIFIER ON;
GO

-- (1) SolicitudBajaPromocion: respuesta y resultado.
IF COL_LENGTH('SolicitudBajaPromocion', 'Respuesta') IS NULL
    ALTER TABLE SolicitudBajaPromocion ADD Respuesta NVARCHAR(500) NULL;
IF COL_LENGTH('SolicitudBajaPromocion', 'DioDeBaja') IS NULL
    ALTER TABLE SolicitudBajaPromocion ADD DioDeBaja BIT NOT NULL CONSTRAINT DF_SolicitudBaja_DioDeBaja DEFAULT 0;
GO
IF COL_LENGTH('SolicitudBajaPromocion', 'MotivoResolucion') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SolicitudBaja_MotivoRechazo')
        ALTER TABLE SolicitudBajaPromocion DROP CONSTRAINT CHK_SolicitudBaja_MotivoRechazo;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SolicitudBaja_Estado')
        ALTER TABLE SolicitudBajaPromocion DROP CONSTRAINT CHK_SolicitudBaja_Estado;
    -- El SET usa los valores anteriores de todas las columnas (Estado se recalcula al final).
    EXEC(N'UPDATE SolicitudBajaPromocion SET
               DioDeBaja = CASE WHEN Estado = 1 THEN 1 ELSE 0 END,
               Respuesta = CASE WHEN Estado = 0 THEN NULL
                                ELSE COALESCE(NULLIF(LTRIM(RTRIM(MotivoResolucion)), N''''),
                                              CASE WHEN Estado = 1 THEN N''Baja aprobada''
                                                   ELSE N''Baja rechazada: la promoción sigue vigente'' END) END,
               Estado    = CASE WHEN Estado = 0 THEN 0 ELSE 1 END;');
    ALTER TABLE SolicitudBajaPromocion DROP COLUMN MotivoResolucion;
    PRINT 'SolicitudBajaPromocion: resolución migrada a Respuesta/DioDeBaja (Pendiente/Atendida).';
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SolicitudBaja_Estado')
    ALTER TABLE SolicitudBajaPromocion ADD CONSTRAINT CHK_SolicitudBaja_Estado CHECK (Estado IN (0,1));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SolicitudBaja_Respuesta')
    ALTER TABLE SolicitudBajaPromocion ADD CONSTRAINT CHK_SolicitudBaja_Respuesta
        CHECK ((Estado = 0 AND Respuesta IS NULL AND DioDeBaja = 0) OR (Estado = 1 AND Respuesta IS NOT NULL));
GO

-- (2) Estados 3 y 5 de Promocion y de su historial.
UPDATE Promocion SET Estado = 1 WHERE Estado = 3;   -- la solicitud sigue Pendiente
UPDATE Promocion SET Estado = 4 WHERE Estado = 5;
-- Pedir la baja (Vigente → 3) y rechazarla (3 → Vigente) ya no son transiciones: la solicitud
-- y su respuesta quedan en SolicitudBajaPromocion.
DELETE FROM PromocionHistorial WHERE (EstadoAnterior = 1 AND EstadoNuevo = 3) OR (EstadoAnterior = 3 AND EstadoNuevo = 1);
UPDATE PromocionHistorial SET EstadoAnterior = 1 WHERE EstadoAnterior = 3;
UPDATE PromocionHistorial SET EstadoNuevo = 1 WHERE EstadoNuevo = 3;
UPDATE PromocionHistorial SET EstadoNuevo = 4, Observacion = LEFT(N'Descartada: ' + ISNULL(Observacion, N'(sin motivo)'), 500)
WHERE EstadoNuevo = 5;
UPDATE PromocionHistorial SET EstadoAnterior = 4 WHERE EstadoAnterior = 5;
GO

-- (3) Promociones y sugerencias por categoría de prenda.
IF COL_LENGTH('Promocion', 'CategoriaPrenda') IS NOT NULL
    EXEC(N'DECLARE @cat TABLE (IdPromocion INT PRIMARY KEY);
           INSERT INTO @cat SELECT IdPromocion FROM Promocion WHERE IdPlan IS NULL;
           IF EXISTS (SELECT 1 FROM Contratacion c JOIN @cat x ON x.IdPromocion = c.IdPromocion)
              OR EXISTS (SELECT 1 FROM HistorialCobro h JOIN @cat x ON x.IdPromocion = h.IdPromocion)
               THROW 50020, ''Hay cobros registrados con una promoción por categoría: revisarlos antes de discontinuarlas.'', 1;
           DELETE h FROM PromocionHistorial h     JOIN @cat x ON x.IdPromocion = h.IdPromocion;
           DELETE d FROM DictamenContable d       JOIN @cat x ON x.IdPromocion = d.IdPromocion;
           DELETE s FROM SolicitudBajaPromocion s JOIN @cat x ON x.IdPromocion = s.IdPromocion;
           DELETE p FROM Promocion p              JOIN @cat x ON x.IdPromocion = p.IdPromocion;
           DECLARE @n INT = @@ROWCOUNT;
           IF @n > 0 PRINT CONCAT(''Promociones por categoría borradas: '', @n, ''.'');');
IF COL_LENGTH('SugerenciaPromocion', 'CategoriaPrenda') IS NOT NULL
    EXEC(N'UPDATE SugerenciaPromocion
           SET Estado = 2, MotivoDescarte = N''Discontinuada: promociones por categoría'', FechaEvaluacion = GETDATE()
           WHERE Estado = 0 AND IdPlan IS NULL;');
GO

-- (4) Sin CategoriaPrenda: FK, CHECK de destino, índices y columna.
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Promocion_Categoria')
    ALTER TABLE Promocion DROP CONSTRAINT FK_Promocion_Categoria;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SugerenciaPromocion_Categoria')
    ALTER TABLE SugerenciaPromocion DROP CONSTRAINT FK_SugerenciaPromocion_Categoria;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Destino')
    ALTER TABLE Promocion DROP CONSTRAINT CHK_Promocion_Destino;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Promocion_CategoriaPrenda' AND object_id = OBJECT_ID('Promocion'))
    DROP INDEX IX_Promocion_CategoriaPrenda ON Promocion;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SugerenciaPromocion_CategoriaPrenda' AND object_id = OBJECT_ID('SugerenciaPromocion'))
    DROP INDEX IX_SugerenciaPromocion_CategoriaPrenda ON SugerenciaPromocion;
IF COL_LENGTH('Promocion', 'CategoriaPrenda') IS NOT NULL
    ALTER TABLE Promocion DROP COLUMN CategoriaPrenda;
IF COL_LENGTH('SugerenciaPromocion', 'CategoriaPrenda') IS NOT NULL
    ALTER TABLE SugerenciaPromocion DROP COLUMN CategoriaPrenda;
GO

-- (5) Promocion.IdPlan obligatorio (con su FK con nombre: la de la sección 18 no lo tenía).
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Promocion') AND name = 'IdPlan' AND is_nullable = 1)
BEGIN
    DECLARE @fkPlan SYSNAME = (SELECT TOP 1 fk.name FROM sys.foreign_keys fk
                               JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
                               JOIN sys.columns c ON c.object_id = fc.parent_object_id AND c.column_id = fc.parent_column_id
                               WHERE fk.parent_object_id = OBJECT_ID('Promocion') AND c.name = 'IdPlan');
    IF @fkPlan IS NOT NULL EXEC(N'ALTER TABLE Promocion DROP CONSTRAINT [' + @fkPlan + N']');
    ALTER TABLE Promocion ALTER COLUMN IdPlan INT NOT NULL;
    PRINT 'Promocion.IdPlan pasa a ser obligatorio.';
END
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys fk
               JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
               JOIN sys.columns c ON c.object_id = fc.parent_object_id AND c.column_id = fc.parent_column_id
               WHERE fk.parent_object_id = OBJECT_ID('Promocion') AND c.name = 'IdPlan')
    ALTER TABLE Promocion ADD CONSTRAINT FK_Promocion_Plan FOREIGN KEY (IdPlan) REFERENCES PlanSuscripcion(IdPlan);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Promocion_IdPlan' AND object_id = OBJECT_ID('Promocion'))
    CREATE NONCLUSTERED INDEX IX_Promocion_IdPlan ON Promocion(IdPlan);
GO

-- (6) CHECK de los estados nuevos (Promocion y su historial).
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Estado'
           AND (definition LIKE '%(3)%' OR definition LIKE '%(5)%'))
    ALTER TABLE Promocion DROP CONSTRAINT CHK_Promocion_Estado;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Estado')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Estado CHECK (Estado IN (0,1,2,4,6));
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_PromocionHistorial_Anterior'
           AND (definition LIKE '%(3)%' OR definition LIKE '%(5)%'))
    ALTER TABLE PromocionHistorial DROP CONSTRAINT CHK_PromocionHistorial_Anterior;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_PromocionHistorial_Anterior')
    ALTER TABLE PromocionHistorial ADD CONSTRAINT CHK_PromocionHistorial_Anterior CHECK (EstadoAnterior IN (0,1,2,4,6));
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_PromocionHistorial_Nuevo'
           AND (definition LIKE '%(3)%' OR definition LIKE '%(5)%'))
    ALTER TABLE PromocionHistorial DROP CONSTRAINT CHK_PromocionHistorial_Nuevo;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_PromocionHistorial_Nuevo')
    ALTER TABLE PromocionHistorial ADD CONSTRAINT CHK_PromocionHistorial_Nuevo CHECK (EstadoNuevo IN (0,1,2,4,6));
GO

-- (7) Textos de idiomas: los de fábrica que cambiaron (sin pisar los editados en Idiomas) y las
--     claves que la aplicación ya no usa.
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @TextosPN03 TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                               TextoAnterior NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @TextosPN03 (Codigo, Clave, TextoAnterior, TextoNuevo) VALUES
    (N'ES', N'msg.sugerencia.sinanalisis', N'Los reportes de rotación y abandono no detectan casos para sugerir por ahora.', N'El reporte de abandono no detecta planes para sugerir por ahora.'),
    (N'EN', N'msg.sugerencia.sinanalisis', N'The rotation and churn reports do not detect any cases to suggest for now.', N'The churn report does not detect any plans to suggest for now.'),
    (N'RU', N'msg.sugerencia.sinanalisis', N'Отчёты по обороту и оттоку пока не выявляют случаев для предложения.', N'Отчёт по оттоку пока не выявляет тарифов для предложения.'),
    (N'PT', N'msg.sugerencia.sinanalisis', N'Os relatórios de rotação e abandono não detectam casos para sugerir por enquanto.', N'O relatório de abandono não detecta planos para sugerir por enquanto.'),
    (N'ES', N'doc.promo.rechazada', N'RECHAZADA — vuelve a Administración para reformularla o descartarla.', N'RECHAZADA — vuelve a Administración para reformularla o darla de baja.'),
    (N'EN', N'doc.promo.rechazada', N'REJECTED — it goes back to Administration to be reformulated or discarded.', N'REJECTED — it goes back to Administration to be reformulated or withdrawn.'),
    (N'RU', N'doc.promo.rechazada', N'ОТКЛОНЕНО — возвращается в администрацию для переформулирования или отклонения.', N'ОТКЛОНЕНО — возвращается в администрацию для переформулирования или снятия.'),
    (N'PT', N'doc.promo.rechazada', N'REJEITADA — volta à Administração para ser reformulada ou descartada.', N'REJEITADA — volta à Administração para ser reformulada ou baixada.'),
    (N'ES', N'ayuda.SugerirPromocionForm', N'1. Elegí el período y Analizar métricas: abandono por plan, rotación por categoría e impacto de las promociones en los cobros del período.' + NCHAR(10) + N'2. Si hay oportunidad, usá la idea propuesta (se completa el formulario) o cargala a mano.' + NCHAR(10) + N'3. Enviar a Administración. La sugerencia se imprime con Imprimir sugerencia.', N'1. Elegí el período y Analizar métricas: abandono por plan, rotación por categoría e impacto de las promociones en los cobros del período.' + NCHAR(10) + N'2. Si hay oportunidad por plan, usá la idea propuesta (se completa el formulario) o cargala a mano. Las oportunidades por categoría son solo informativas: las promociones son siempre por plan.' + NCHAR(10) + N'3. Enviar a Administración. La sugerencia se imprime con Imprimir sugerencia.'),
    (N'ES', N'ayuda.PromocionesAdministracionForm', N'• Sugerencias: crear la promoción a partir de una sugerencia o descartarla (con motivo).' + NCHAR(10) + N'• Promociones: crear, reformular las rechazadas por Contabilidad o descartarlas.' + NCHAR(10) + N'• Bajas: resolver las solicitudes de baja de Ventas, o desactivar directamente.' + NCHAR(10) + N'• Imprimir ▾: ficha, dictamen, solicitud y resolución de baja, constancia de descarte.' + NCHAR(10) + N'La fecha de fin no puede haber pasado. Las promociones por categoría son informativas: el descuento se aplica a las promociones por plan.', N'• Sugerencias: crear la promoción a partir de una sugerencia o descartarla (con motivo).' + NCHAR(10) + N'• Solicitudes de baja pendientes: Atender cada pedido de Ventas con una respuesta; si estás de acuerdo, la promoción se da de baja, si no, sigue vigente.' + NCHAR(10) + N'• Promociones: crear (siempre para un plan), reformular las rechazadas por Contabilidad, o Dar de baja una vigente o una rechazada (con motivo).' + NCHAR(10) + N'• Imprimir ▾: ficha, dictamen, solicitud de baja, respuesta a la solicitud y constancia de baja.' + NCHAR(10) + N'La fecha de fin no puede haber pasado.'),
    (N'ES', N'ayuda.PromocionesVigentesForm', N'Consulta de las promociones vigentes. Ventas puede solicitar la baja de una (con motivo); la resuelve Administración. Mientras la baja está solicitada, la promoción no se aplica en los cobros.', N'Consulta de las promociones vigentes. Ventas puede solicitar la baja de una (con motivo); la columna Baja solicitada muestra si ya hay una pendiente. Administración atiende la solicitud: mientras tanto la promoción sigue vigente y se aplica en los cobros.'),
    (N'EN', N'ayuda.SugerirPromocionForm', N'1. Choose the period and Analyse metrics: churn by plan, rotation by category and impact of promotions on the period''s payments.' + NCHAR(10) + N'2. If there is an opportunity, use the proposed idea (the form is filled in) or enter it manually.' + NCHAR(10) + N'3. Send to Administration. Print the suggestion with Print suggestion.', N'1. Choose the period and Analyse metrics: churn by plan, rotation by category and impact of promotions on the period''s payments.' + NCHAR(10) + N'2. If there is a plan opportunity, use the proposed idea (the form is filled in) or enter it manually. Category opportunities are informative only: promotions are always by plan.' + NCHAR(10) + N'3. Send to Administration. Print the suggestion with Print suggestion.'),
    (N'EN', N'ayuda.PromocionesAdministracionForm', N'• Suggestions: create the promotion from a suggestion or discard it (with a reason).' + NCHAR(10) + N'• Promotions: create, rework those rejected by Accounting or discard them.' + NCHAR(10) + N'• Withdrawals: resolve withdrawal requests from Sales, or deactivate directly.' + NCHAR(10) + N'• Print ▾: sheet, opinion, withdrawal request and resolution, discard certificate.' + NCHAR(10) + N'The end date cannot be in the past. Category promotions are informative: the discount applies to plan promotions.', N'• Suggestions: create the promotion from a suggestion or discard it (with a reason).' + NCHAR(10) + N'• Pending withdrawal requests: Handle each Sales request with a response; if you agree, the promotion is withdrawn, otherwise it remains active.' + NCHAR(10) + N'• Promotions: create (always for a plan), rework those rejected by Accounting, or Withdraw an active or rejected one (with a reason).' + NCHAR(10) + N'• Print ▾: sheet, opinion, withdrawal request, response to the request and withdrawal certificate.' + NCHAR(10) + N'The end date cannot be in the past.'),
    (N'EN', N'ayuda.PromocionesVigentesForm', N'View of active promotions. Sales can request withdrawal of one (with a reason); Administration resolves it. While withdrawal is requested, the promotion is not applied to payments.', N'View of active promotions. Sales can request withdrawal of one (with a reason); the Withdrawal requested column shows whether one is already pending. Administration handles the request: meanwhile the promotion remains active and is applied to payments.'),
    (N'RU', N'ayuda.SugerirPromocionForm', N'1. Выберите период и «Анализировать метрики»: отток по тарифам, оборачиваемость по категориям и влияние акций на оплаты за период.' + NCHAR(10) + N'2. Если есть возможность, используйте предложенную идею (форма заполнится) или введите вручную.' + NCHAR(10) + N'3. «Отправить в администрацию». Предложение печатается кнопкой «Печать предложения».', N'1. Выберите период и «Анализировать метрики»: отток по тарифам, оборачиваемость по категориям и влияние акций на оплаты за период.' + NCHAR(10) + N'2. Если есть возможность по тарифу, используйте предложенную идею (форма заполнится) или введите вручную. Возможности по категориям только информационные: акции всегда действуют по тарифам.' + NCHAR(10) + N'3. «Отправить в администрацию». Предложение печатается кнопкой «Печать предложения».'),
    (N'RU', N'ayuda.PromocionesAdministracionForm', N'• Предложения: создать акцию по предложению или отклонить его (с причиной).' + NCHAR(10) + N'• Акции: создать, переработать отклонённые бухгалтерией или отклонить.' + NCHAR(10) + N'• Отмены: рассмотреть запросы отдела продаж на отмену или деактивировать напрямую.' + NCHAR(10) + N'• Печать ▾: карточка, заключение, запрос и решение об отмене, справка об отклонении.' + NCHAR(10) + N'Дата окончания не может быть в прошлом. Акции по категориям информационные: скидка применяется к акциям по тарифам.', N'• Предложения: создать акцию по предложению или отклонить его (с причиной).' + NCHAR(10) + N'• Ожидающие запросы на отмену: рассмотрите каждый запрос отдела продаж и дайте ответ; если вы согласны, акция снимается, если нет — остаётся действующей.' + NCHAR(10) + N'• Акции: создать (всегда для тарифа), переработать отклонённые бухгалтерией или снять действующую либо отклонённую (с причиной).' + NCHAR(10) + N'• Печать ▾: карточка, заключение, запрос на отмену, ответ на запрос и справка о снятии.' + NCHAR(10) + N'Дата окончания не может быть в прошлом.'),
    (N'RU', N'ayuda.PromocionesVigentesForm', N'Просмотр действующих акций. Отдел продаж может запросить отмену (с причиной); решение принимает администрация. Пока отмена запрошена, акция не применяется к оплатам.', N'Просмотр действующих акций. Отдел продаж может запросить отмену (с причиной); столбец «Запрошена отмена» показывает, есть ли уже ожидающий запрос. Администрация рассматривает запрос: до этого акция остаётся действующей и применяется к оплатам.'),
    (N'PT', N'ayuda.SugerirPromocionForm', N'1. Escolha o período e Analisar métricas: abandono por plano, rotação por categoria e impacto das promoções nas cobranças do período.' + NCHAR(10) + N'2. Se houver oportunidade, use a ideia proposta (o formulário é preenchido) ou digite-a.' + NCHAR(10) + N'3. Enviar à Administração. A sugestão é impressa com Imprimir sugestão.', N'1. Escolha o período e Analisar métricas: abandono por plano, rotação por categoria e impacto das promoções nas cobranças do período.' + NCHAR(10) + N'2. Se houver oportunidade por plano, use a ideia proposta (o formulário é preenchido) ou digite-a. As oportunidades por categoria são apenas informativas: as promoções são sempre por plano.' + NCHAR(10) + N'3. Enviar à Administração. A sugestão é impressa com Imprimir sugestão.'),
    (N'PT', N'ayuda.PromocionesAdministracionForm', N'• Sugestões: criar a promoção a partir de uma sugestão ou descartá-la (com motivo).' + NCHAR(10) + N'• Promoções: criar, reformular as rejeitadas pela Contabilidade ou descartá-las.' + NCHAR(10) + N'• Baixas: resolver os pedidos de baixa de Vendas, ou desativar diretamente.' + NCHAR(10) + N'• Imprimir ▾: ficha, parecer, pedido e resolução de baixa, comprovante de descarte.' + NCHAR(10) + N'A data de fim não pode ter passado. As promoções por categoria são informativas: o desconto se aplica às promoções por plano.', N'• Sugestões: criar a promoção a partir de uma sugestão ou descartá-la (com motivo).' + NCHAR(10) + N'• Pedidos de baixa pendentes: Atender cada pedido de Vendas com uma resposta; se você concordar, a promoção é baixada, se não, continua vigente.' + NCHAR(10) + N'• Promoções: criar (sempre para um plano), reformular as rejeitadas pela Contabilidade, ou Dar baixa em uma vigente ou rejeitada (com motivo).' + NCHAR(10) + N'• Imprimir ▾: ficha, parecer, pedido de baixa, resposta ao pedido e comprovante de baixa.' + NCHAR(10) + N'A data de fim não pode ter passado.'),
    (N'PT', N'ayuda.PromocionesVigentesForm', N'Consulta das promoções vigentes. Vendas pode pedir a baixa de uma (com motivo); a Administração resolve. Enquanto a baixa estiver pedida, a promoção não se aplica nas cobranças.', N'Consulta das promoções vigentes. Vendas pode pedir a baixa de uma (com motivo); a coluna Baixa solicitada mostra se já há um pedido pendente. A Administração atende o pedido: enquanto isso a promoção continua vigente e se aplica nas cobranças.');

    UPDATE t SET t.Texto = f.TextoNuevo
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @TextosPN03 f ON f.Clave = c.Clave AND f.Codigo = i.Codigo
    WHERE t.Texto COLLATE Latin1_General_BIN2 = f.TextoAnterior COLLATE Latin1_General_BIN2; -- exacto: no pisa ediciones
    PRINT CONCAT('PN03: textos de fábrica actualizados: ', @@ROWCOUNT, ' fila(s).');

    DECLARE @ClavesObsoletasPN03 TABLE (Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    INSERT INTO @ClavesObsoletasPN03 (Clave) VALUES
        (N'msg.promo.desactivada'), (N'inputdlg.rechazarbaja.titulo'), (N'inputdlg.rechazarbaja.prompt'), (N'msg.promo.bajarechazada'),
        (N'err.bll.promocion.destino_invalido'), (N'err.bll.promocion.desactivar_estado'), (N'err.bll.promocion.resolverbaja_estado'), (N'err.bll.promocion.motivorechazobaja_requerido'),
        (N'err.bll.sugerenciapromocion.destino_invalido'), (N'promocion.categoria'), (N'promocion.btn.desactivar'), (N'promocion.btn.aprobarbaja'),
        (N'promocion.btn.rechazarbaja'), (N'col.promo.motivobaja'), (N'dlg.promo.descartar.titulo'), (N'dlg.promo.descartar.prompt'),
        (N'msg.promo.descartada'), (N'dlg.promo.desactivar.titulo'), (N'dlg.promo.desactivar.prompt'), (N'dlg.promo.aprobarbaja.titulo'),
        (N'dlg.promo.aprobarbaja.prompt'), (N'promo.analisis.calc.informativa'), (N'menu.promo.resolucion'), (N'est.promo.bajasolicitada'),
        (N'est.promo.desactivada'), (N'est.promo.descartada'), (N'doc.promo.categoria'), (N'doc.promo.notacategoria'),
        (N'doc.promo.solicitud.nota'), (N'doc.promo.agerencia'), (N'doc.promo.bajaaprobada'), (N'doc.promo.bajarechazada'),
        (N'doc.promo.motivoresolucion'), (N'doc.promo.resolvio'), (N'doc.promo.resolucion.gerencia'), (N'doc.promo.resolucion.ventas'),
        (N'doc.promo.descarteprom'), (N'doc.promo.descartadapor'), (N'err.bll.promocion.descartar_estado'), (N'err.bll.promocion.solicitud_inexistente'),
        (N'err.bll.promocion.motivodescarte_requerido'), (N'err.bll.promocion.motivodesactivar_requerido'), (N'promocion.btn.descartar');
    DELETE t FROM Traduccion t JOIN Control c ON c.IdControl = t.IdControl JOIN @ClavesObsoletasPN03 o ON o.Clave = c.Clave;
    DELETE c FROM Control c JOIN @ClavesObsoletasPN03 o ON o.Clave = c.Clave
    WHERE NOT EXISTS (SELECT 1 FROM Traduccion t WHERE t.IdControl = c.IdControl);
    DECLARE @obsoletas INT = @@ROWCOUNT;
    IF @obsoletas > 0 PRINT CONCAT('PN03: claves de idioma obsoletas borradas: ', @obsoletas, '.');
END
GO
PRINT 'Sección 20g: promociones solo por plan y baja simplificada (PN03) verificadas.';
GO

-- ============================================================
-- WardrobeFlow — 20f. ORIGEN DEL MANTENIMIENTO DE UNA PRENDA (PN04)
-- ------------------------------------------------------------
--   La devolución de un pedido (DAL.Pedido.RegistrarDevolucion) abría el mantenimiento con
--   Actor = 'Devolución', y esa MARCA en una columna de texto libre decidía qué prendas van a la
--   Inspección de Devolución (y a quién se le cobra el daño). Un usuario llamado "Devolución" que
--   mandara una prenda a limpieza a mano la hacía pasar por devuelta y se le cobraba al último
--   cliente. Ahora el origen es un dato propio: MantenimientoPrenda.Origen (0 = manual desde
--   Prendas/Stock, 1 = devolución de un pedido, BE.OrigenMantenimiento) y Actor queda solo como
--   quién lo abrió. Migración UNA sola vez, al crear la columna: Actor = 'Devolución' → Origen = 1
--   y Actor = NULL. Va antes de la 21z3, que usa el origen. Idempotente.
-- ============================================================
IF COL_LENGTH('MantenimientoPrenda', 'Origen') IS NULL
BEGIN
    ALTER TABLE MantenimientoPrenda ADD Origen TINYINT NOT NULL
        CONSTRAINT DF_MantenimientoPrenda_Origen DEFAULT 0
        CONSTRAINT CHK_MantenimientoPrenda_Origen CHECK (Origen IN (0, 1));
    EXEC(N'UPDATE MantenimientoPrenda SET Origen = 1, Actor = NULL WHERE Actor = N''Devolución'';
           DECLARE @n INT = @@ROWCOUNT;
           PRINT ''MantenimientoPrenda: columna Origen agregada; '' + CAST(@n AS NVARCHAR(10)) + '' mantenimiento(s) de devolución migrados.'';');
END
GO
PRINT 'Sección 20f: origen del mantenimiento de prendas verificado.';
GO
-- ============================================================
-- WardrobeFlow — 20h. PN03: TASA DE RETENCIÓN ESPERADA EN LA SUGERENCIA
-- ------------------------------------------------------------
--   El beneficio estimado de una sugerencia suponía que la promoción retenía al 100 % de los
--   clientes en riesgo. Ahora Gerencia declara la tasa de retención esperada (0-100], por
--   defecto 30 %), y el beneficio es ingreso mensual en riesgo × tasa / 100. Las sugerencias ya
--   registradas quedan con 100: es lo que suponían (su beneficio no cambia).
--   SugerenciaPromocion no lleva dígito verificador. Idempotente.
-- ============================================================
IF COL_LENGTH('SugerenciaPromocion', 'TasaRetencion') IS NULL
BEGIN
    ALTER TABLE SugerenciaPromocion ADD TasaRetencion DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_SugerenciaPromocion_TasaRetencion DEFAULT 30
        CONSTRAINT CHK_SugerenciaPromocion_TasaRetencion CHECK (TasaRetencion > 0 AND TasaRetencion <= 100);
    EXEC(N'UPDATE SugerenciaPromocion SET TasaRetencion = 100;
           DECLARE @n INT = @@ROWCOUNT;
           PRINT ''SugerenciaPromocion: columna TasaRetencion agregada; '' + CAST(@n AS NVARCHAR(10)) + '' sugerencia(s) previas con 100 %.'';');
END
GO
-- Ingreso mensual en riesgo que cargó Gerencia: se guarda para que la sugerencia impresa y el análisis
-- de Contabilidad muestren lo ingresado (antes se reconstruía desde el beneficio redondeado). El
-- beneficio sigue guardado (lo leen Contabilidad y los reportes) y un CHECK garantiza que es
-- exactamente ROUND(ingreso × tasa / 100, 2): no puede quedar desfasado. Las previas: ingreso = beneficio × 100 / tasa.
IF COL_LENGTH('SugerenciaPromocion', 'IngresoEnRiesgo') IS NULL
BEGIN
    ALTER TABLE SugerenciaPromocion ADD IngresoEnRiesgo DECIMAL(12,2) NULL;
    EXEC(N'UPDATE SugerenciaPromocion SET IngresoEnRiesgo = ROUND(BeneficioEstimado * 100 / TasaRetencion, 2) WHERE IngresoEnRiesgo IS NULL;
           PRINT ''SugerenciaPromocion: columna IngresoEnRiesgo agregada.'';');
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_SugerenciaPromocion_Beneficio')
    EXEC(N'ALTER TABLE SugerenciaPromocion ADD CONSTRAINT CHK_SugerenciaPromocion_Beneficio
           CHECK (IngresoEnRiesgo IS NULL OR BeneficioEstimado = ROUND(IngresoEnRiesgo * TasaRetencion / 100, 2))');
GO
-- Claves de idioma sin uso: promo.analisis.calc.beneficiosug (reemplazada por .beneficiotasa, que
-- muestra la tasa) y est.pendiente (Pedidos Realizados usa las etiquetas de GUI/EstadosPedido).
IF OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Traduccion') IS NOT NULL
BEGIN
    DELETE t FROM Traduccion t JOIN Control c ON c.IdControl = t.IdControl
    WHERE c.Clave IN (N'promo.analisis.calc.beneficiosug', N'est.pendiente');
    DELETE FROM Control WHERE Clave IN (N'promo.analisis.calc.beneficiosug', N'est.pendiente');
END
GO
PRINT 'Sección 20h: tasa de retención de las sugerencias (PN03) verificada.';
GO
-- ============================================================
-- WardrobeFlow — 20i. ANALÍTICA DE NEGOCIO EN UN SOLO FORMULARIO
-- ------------------------------------------------------------
-- Los 6 reportes de decisión (Análisis de Abandono, Ventas por Vendedor, Rotación de Prendas,
-- Tiempos de Mantenimiento, Escasez de Stock y Recomendación de Prendas) dejan de ser 6
-- formularios con un ítem de menú cada uno: ahora son UNA entrada de menú, "Analítica de
-- Negocio" (analiticaNegocioToolStripMenuItem), que abre GUI.AnaliticaNegocioForm con un
-- selector de reporte.
--   · Patentes: NO cambian. Cada reporte conserva la suya (mnuAnalisisAbandono,
--     mnuVentasVendedor, mnuAnalisisRotacion, mnuAnalisisMantenimiento, mnuAnalisisEscasez,
--     mnuRecomendacionPrendas) con los mismos roles: el selector ofrece solo los reportes cuya
--     patente tiene el usuario, y la entrada del menú se ve si tiene AL MENOS UNA (la decisión
--     vive en BLL.MenuVisibilidad: Resolver + ReportesVisibles).
--   · ControlMapeado: se borran los mapeos del Menú a los 6 ítems que ya no existen. La entrada
--     única no se mapea a una patente: su visibilidad es el OR de las 6 y el Menú no se gobierna
--     por ControlMapeado (GUI.ManejadorSeguridad lo excluye; usa BLL.MenuVisibilidad).
--   · Textos de idiomas: se borran los rótulos de los 6 ítems de menú (mnu.abandono,
--     mnu.ventasvendedor, mnu.rotacion, mnu.mantanalisis, mnu.escasez, mnu.recomendacion); el
--     selector usa los títulos de cada reporte, que ya existían. Las claves nuevas (selector,
--     ayuda) las carga la aplicación desde traducciones.tsv.
-- Idempotente: en una base ya migrada no cambia nada.
-- ============================================================
IF OBJECT_ID('ControlMapeado') IS NOT NULL
BEGIN
    DELETE FROM ControlMapeado
    WHERE Formulario = 'Menu'
      AND NombreControl IN ('analisisAbandonoToolStripMenuItem', 'ventasVendedorToolStripMenuItem',
                            'analisisRotacionToolStripMenuItem', 'analisisMantenimientoToolStripMenuItem',
                            'analisisEscasezToolStripMenuItem', 'recomendacionPrendasToolStripMenuItem');
    DECLARE @mapeos INT = @@ROWCOUNT;
    IF @mapeos > 0 PRINT CONCAT('Analítica de Negocio: mapeos de menú obsoletos borrados: ', @mapeos, '.');
END
GO

IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL
BEGIN
    DECLARE @ClavesObsoletasAnalitica TABLE (Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    INSERT INTO @ClavesObsoletasAnalitica (Clave) VALUES
        (N'mnu.abandono'), (N'mnu.ventasvendedor'), (N'mnu.rotacion'),
        (N'mnu.mantanalisis'), (N'mnu.escasez'), (N'mnu.recomendacion');
    DELETE t FROM Traduccion t JOIN Control c ON c.IdControl = t.IdControl JOIN @ClavesObsoletasAnalitica o ON o.Clave = c.Clave;
    DELETE c FROM Control c JOIN @ClavesObsoletasAnalitica o ON o.Clave = c.Clave
    WHERE NOT EXISTS (SELECT 1 FROM Traduccion t WHERE t.IdControl = c.IdControl);
    DECLARE @obsoletas INT = @@ROWCOUNT;
    IF @obsoletas > 0 PRINT CONCAT('Analítica de Negocio: claves de idioma obsoletas borradas: ', @obsoletas, '.');
END
GO
PRINT 'Sección 20i: Analítica de Negocio en un solo formulario verificada.';
GO
-- ============================================================
-- WardrobeFlow — 20j. P2: CONSOLIDACIÓN DE PANTALLAS DE SEGURIDAD
-- ------------------------------------------------------------
-- Consolidación de pantallas de seguridad (GUI, sin cambios de esquema ni de patentes):
--   · "Cuentas de Usuario" (GUI.Usuarios) y "Administración de Usuarios"
--     (AdministracionUsuariosForm, eliminado) quedan en UN formulario, GUI.Usuarios, con las
--     pestañas Cuentas y Datos administrativos. Las dos pantallas ya estaban gobernadas por la
--     misma patente (mnuUsuarios), así que ningún rol gana ni pierde acceso. El alta de usuarios
--     queda en un único lugar (Datos administrativos › Nuevo usuario). El ítem de menú que
--     sobrevive es usuariosToolStripMenuItem, el que ya está en ControlMapeado: no hay mapeos que
--     agregar ni quitar.
--   · La "Vista completa del sistema" (ExploradorCompositeForm, eliminado) pasa a ser la pestaña
--     Vista completa de Perfiles y Permisos (GestorPermisos). Solo se abría desde esa pantalla,
--     así que sigue gobernada por mnuUsuarios; no tenía ítem de menú ni mapeo propio.
--   · Textos de idiomas: se actualiza la ayuda en línea de las dos pantallas (sin pisar textos
--     editados en Idiomas) y se borran las claves que la aplicación ya no usa (las nuevas las
--     carga la aplicación desde traducciones.tsv).
-- Idempotente: en una base ya migrada no cambia nada.
-- ============================================================
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @TextosP2 TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                             TextoAnterior NVARCHAR(2000) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(2000) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @TextosP2 (Codigo, Clave, TextoAnterior, TextoNuevo) VALUES
    (N'ES', N'ayuda.Usuarios', N'Alta, edición, desbloqueo y archivo de usuarios (solo Administrador).' + NCHAR(10) + NCHAR(10) + N'• Cada usuario nuevo queda vinculado a su legajo de empleado, para poder vender, cobrar o controlar stock.' + NCHAR(10) + N'• Resetear contraseña: genera una nueva y la exporta a un archivo (pide confirmación).' + NCHAR(10) + N'• Archivar: el usuario no puede ingresar y sale de la lista, pero se conserva su historial.' + NCHAR(10) + N'• Purgar: elimina definitivamente a los archivados hace más de un año, salvo los que firmaron registros de promociones.', N'Administración de usuarios en dos pestañas (solo Administrador).' + NCHAR(10) + NCHAR(10) + N'Cuentas:' + NCHAR(10) + N'• Resetear contraseña: genera una nueva y la exporta a un archivo (pide confirmación).' + NCHAR(10) + N'• Desbloquear: habilita una cuenta bloqueada por intentos fallidos de ingreso.' + NCHAR(10) + N'• Archivar: el usuario no puede ingresar y sale de la lista, pero se conserva su historial.' + NCHAR(10) + N'• Purgar: elimina definitivamente a los archivados hace más de un año, salvo los que firmaron registros de promociones.' + NCHAR(10) + NCHAR(10) + N'Datos administrativos:' + NCHAR(10) + N'• Nuevo usuario: el único alta de usuarios. Cada usuario nuevo queda vinculado a su legajo de empleado, para poder vender, cobrar o controlar stock; la contraseña se genera y se exporta a un archivo.' + NCHAR(10) + N'• Guardar cambios / Cambiar rol: nombre, apellido, usuario, email, fecha de nacimiento y rol (el sistema no puede quedar sin administrador).' + NCHAR(10) + N'• Ver historial de cambios: las versiones anteriores del usuario.'),
    (N'EN', N'ayuda.Usuarios', N'Create, edit, unlock and archive users (Administrator only).' + NCHAR(10) + NCHAR(10) + N'• Each new user is linked to their employee record, so they can sell, collect payments or control stock.' + NCHAR(10) + N'• Reset password: generates a new one and exports it to a file (asks for confirmation).' + NCHAR(10) + N'• Archive: the user cannot sign in and leaves the list, but their history is kept.' + NCHAR(10) + N'• Purge: permanently deletes users archived more than a year ago, except those who signed promotion records.', N'User administration in two tabs (Administrator only).' + NCHAR(10) + NCHAR(10) + N'Accounts:' + NCHAR(10) + N'• Reset password: generates a new one and exports it to a file (asks for confirmation).' + NCHAR(10) + N'• Unlock: re-enables an account locked after failed sign-in attempts.' + NCHAR(10) + N'• Archive: the user cannot sign in and leaves the list, but their history is kept.' + NCHAR(10) + N'• Purge: permanently deletes users archived more than a year ago, except those who signed promotion records.' + NCHAR(10) + NCHAR(10) + N'Administrative data:' + NCHAR(10) + N'• New user: the only way to create users. Each new user is linked to their employee record, so they can sell, collect payments or control stock; the password is generated and exported to a file.' + NCHAR(10) + N'• Save changes / Change role: first name, last name, username, email, date of birth and role (the system cannot be left without an administrator).' + NCHAR(10) + N'• View change history: the user''s previous versions.'),
    (N'RU', N'ayuda.Usuarios', N'Создание, редактирование, разблокировка и архивирование пользователей (только администратор).' + NCHAR(10) + NCHAR(10) + N'• Каждый новый пользователь связывается со своей карточкой сотрудника, чтобы продавать, принимать оплату или контролировать наличие.' + NCHAR(10) + N'• «Сбросить пароль»: создаёт новый и сохраняет его в файл (с подтверждением).' + NCHAR(10) + N'• «Архивировать»: пользователь не может войти и исчезает из списка, но история сохраняется.' + NCHAR(10) + N'• «Очистить»: окончательно удаляет архивированных более года назад, кроме подписавших записи об акциях.', N'Администрирование пользователей на двух вкладках (только администратор).' + NCHAR(10) + NCHAR(10) + N'Учётные записи:' + NCHAR(10) + N'• «Сбросить пароль»: создаёт новый и сохраняет его в файл (с подтверждением).' + NCHAR(10) + N'• «Разблокировать»: снова включает учётную запись, заблокированную после неудачных попыток входа.' + NCHAR(10) + N'• «Архивировать»: пользователь не может войти и исчезает из списка, но история сохраняется.' + NCHAR(10) + N'• «Очистить»: окончательно удаляет архивированных более года назад, кроме подписавших записи об акциях.' + NCHAR(10) + NCHAR(10) + N'Административные данные:' + NCHAR(10) + N'• «Новый пользователь»: единственный способ создать пользователя. Каждый новый пользователь связывается со своей карточкой сотрудника, чтобы продавать, принимать оплату или контролировать наличие; пароль создаётся и сохраняется в файл.' + NCHAR(10) + N'• «Сохранить изменения» / «Сменить роль»: имя, фамилия, имя пользователя, email, дата рождения и роль (систему нельзя оставить без администратора).' + NCHAR(10) + N'• «История изменений»: предыдущие версии пользователя.'),
    (N'PT', N'ayuda.Usuarios', N'Cadastro, edição, desbloqueio e arquivamento de usuários (só Administrador).' + NCHAR(10) + NCHAR(10) + N'• Cada usuário novo fica vinculado à sua ficha de funcionário, para poder vender, cobrar ou controlar estoque.' + NCHAR(10) + N'• Redefinir senha: gera uma nova e a exporta para um arquivo (pede confirmação).' + NCHAR(10) + N'• Arquivar: o usuário não pode entrar e sai da lista, mas seu histórico é mantido.' + NCHAR(10) + N'• Expurgar: exclui definitivamente os arquivados há mais de um ano, exceto os que assinaram registros de promoções.', N'Administração de usuários em duas abas (só Administrador).' + NCHAR(10) + NCHAR(10) + N'Contas:' + NCHAR(10) + N'• Redefinir senha: gera uma nova e a exporta para um arquivo (pede confirmação).' + NCHAR(10) + N'• Desbloquear: reativa uma conta bloqueada por tentativas de acesso falhas.' + NCHAR(10) + N'• Arquivar: o usuário não pode entrar e sai da lista, mas seu histórico é mantido.' + NCHAR(10) + N'• Expurgar: exclui definitivamente os arquivados há mais de um ano, exceto os que assinaram registros de promoções.' + NCHAR(10) + NCHAR(10) + N'Dados administrativos:' + NCHAR(10) + N'• Novo usuário: o único cadastro de usuários. Cada usuário novo fica vinculado à sua ficha de funcionário, para poder vender, cobrar ou controlar estoque; a senha é gerada e exportada para um arquivo.' + NCHAR(10) + N'• Salvar alterações / Alterar perfil: nome, sobrenome, usuário, email, data de nascimento e perfil (o sistema não pode ficar sem administrador).' + NCHAR(10) + N'• Ver histórico de alterações: as versões anteriores do usuário.'),
    (N'ES', N'ayuda.GestorPermisos', N'Roles y permisos (patentes) del sistema. Un rol con usuarios asignados no se puede eliminar ni renombrar.', N'Roles y permisos (patentes) del sistema. Un rol con usuarios asignados no se puede eliminar ni renombrar.' + NCHAR(10) + NCHAR(10) + N'• Gestión de roles: crear, renombrar y eliminar roles, y asignar o quitar permisos y sub-roles.' + NCHAR(10) + N'• Vista completa: el árbol completo de roles, familias y permisos, en solo lectura.'),
    (N'EN', N'ayuda.GestorPermisos', N'System roles and permissions. A role with assigned users cannot be deleted or renamed.', N'System roles and permissions. A role with assigned users cannot be deleted or renamed.' + NCHAR(10) + NCHAR(10) + N'• Role management: create, rename and delete roles, and assign or remove permissions and sub-roles.' + NCHAR(10) + N'• Full view: the complete tree of roles, families and permissions, read-only.'),
    (N'RU', N'ayuda.GestorPermisos', N'Роли и права системы. Роль с назначенными пользователями нельзя удалить или переименовать.', N'Роли и права системы. Роль с назначенными пользователями нельзя удалить или переименовать.' + NCHAR(10) + NCHAR(10) + N'• Управление ролями: создание, переименование и удаление ролей, назначение и снятие прав и подролей.' + NCHAR(10) + N'• Полный обзор: всё дерево ролей, семейств и прав, только для чтения.'),
    (N'PT', N'ayuda.GestorPermisos', N'Perfis e permissões do sistema. Um perfil com usuários atribuídos não pode ser excluído nem renomeado.', N'Perfis e permissões do sistema. Um perfil com usuários atribuídos não pode ser excluído nem renomeado.' + NCHAR(10) + NCHAR(10) + N'• Gestão de perfis: criar, renomear e excluir perfis, e atribuir ou remover permissões e subperfis.' + NCHAR(10) + N'• Visão completa: a árvore completa de perfis, famílias e permissões, somente leitura.');

    UPDATE t SET t.Texto = f.TextoNuevo
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @TextosP2 f ON f.Clave = c.Clave AND f.Codigo = i.Codigo
    WHERE t.Texto COLLATE Latin1_General_BIN2 = f.TextoAnterior COLLATE Latin1_General_BIN2; -- exacto: no pisa ediciones
    PRINT CONCAT('P2: textos de fábrica actualizados: ', @@ROWCOUNT, ' fila(s).');

    DECLARE @ClavesObsoletasP2 TABLE (Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT PRIMARY KEY);
    INSERT INTO @ClavesObsoletasP2 (Clave) VALUES
        -- Usuarios: el ítem "Cuentas de Usuario", el título viejo y el alta oculta de la pestaña Cuentas.
        (N'mnu.cuentas'), (N'frm.gestion'), (N'lbl.nuevousuario'), (N'lbl.nombreusuario'), (N'lbl.perfilrol'),
        (N'btn.agregar'), (N'msg.usr.creado.exportado'),
        -- Perfiles: el botón y el formulario aparte de la Vista completa (hoy es una pestaña).
        (N'btn.explorador'), (N'frm.explorador'), (N'lbl.explorador.titulo'), (N'btn.explorador.cerrar');
    DELETE t FROM Traduccion t JOIN Control c ON c.IdControl = t.IdControl JOIN @ClavesObsoletasP2 o ON o.Clave = c.Clave;
    DELETE c FROM Control c JOIN @ClavesObsoletasP2 o ON o.Clave = c.Clave
    WHERE NOT EXISTS (SELECT 1 FROM Traduccion t WHERE t.IdControl = c.IdControl);
    DECLARE @obsoletasP2 INT = @@ROWCOUNT;
    IF @obsoletasP2 > 0 PRINT CONCAT('P2: claves de idioma obsoletas borradas: ', @obsoletasP2, '.');
END
GO
PRINT 'Sección 20j: pantallas de seguridad consolidadas (P2) verificadas.';
GO
-- ============================================================
-- WardrobeFlow — 20k. HISTORIAL DE PEDIDOS: UNA OPERACIÓN, UN NÚMERO
-- ------------------------------------------------------------
-- El historial de un pedido (PedidoHistorial) agrupa los campos de cada evento con IdOperacion.
-- Antes el número se calculaba como MAX + 1 fuera de toda transacción y el historial se escribía
-- en otra transacción, después del cambio: dos eventos simultáneos podían recibir el mismo número.
-- Ahora DAL.Pedido asienta el historial en la misma transacción del cambio y numera con
-- UPDLOCK + HOLDLOCK; esta sección agrega la restricción que lo garantiza en la base:
--   · UQ_PedidoHistorial_Operacion UNIQUE (IdPedido, IdOperacion, Campo).
--   · Antes de crearla se renumeran las operaciones repetidas que pudiera haber. Un evento se
--     reconoce por (IdOperacion, Accion) y por la "vuelta" de cada campo: la 1.ª fila de un campo
--     es del primer evento que usó ese número, la 2.ª del segundo, etc. Solo en los pedidos con
--     números repetidos, las operaciones se vuelven a numerar 1, 2, 3... respetando el orden en
--     que se registraron (IdOperacion y después IdHistorial). Los demás pedidos no se tocan.
-- PedidoHistorial no lleva dígito verificador. Idempotente: con la restricción creada no hace nada.
-- ============================================================
IF OBJECT_ID('PedidoHistorial') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.key_constraints
                   WHERE name = 'UQ_PedidoHistorial_Operacion' AND parent_object_id = OBJECT_ID('PedidoHistorial'))
BEGIN
    SELECT IdHistorial, IdPedido, IdOperacion,
           MIN(IdHistorial) OVER (PARTITION BY IdPedido, IdOperacion, Accion, Vuelta) AS PrimeraFila
    INTO #EventosHistorial
    FROM (SELECT IdHistorial, IdPedido, IdOperacion, Accion,
                 ROW_NUMBER() OVER (PARTITION BY IdPedido, IdOperacion, Accion, Campo ORDER BY IdHistorial) AS Vuelta
          FROM PedidoHistorial) f;

    ;WITH Repetidos AS (
        SELECT DISTINCT IdPedido FROM #EventosHistorial
        GROUP BY IdPedido, IdOperacion HAVING COUNT(DISTINCT PrimeraFila) > 1
    ), Renumerado AS (
        SELECT e.IdHistorial,
               DENSE_RANK() OVER (PARTITION BY e.IdPedido ORDER BY e.IdOperacion, e.PrimeraFila) AS Nuevo
        FROM #EventosHistorial e JOIN Repetidos r ON r.IdPedido = e.IdPedido
    )
    UPDATE h SET h.IdOperacion = n.Nuevo
    FROM PedidoHistorial h JOIN Renumerado n ON n.IdHistorial = h.IdHistorial
    WHERE h.IdOperacion <> n.Nuevo;
    DECLARE @renumeradas INT = @@ROWCOUNT;
    IF @renumeradas > 0 PRINT CONCAT('PedidoHistorial: filas con IdOperacion repetido renumeradas: ', @renumeradas, '.');
    DROP TABLE #EventosHistorial;

    ALTER TABLE PedidoHistorial ADD CONSTRAINT UQ_PedidoHistorial_Operacion UNIQUE (IdPedido, IdOperacion, Campo);
    PRINT 'PedidoHistorial: restricción UQ_PedidoHistorial_Operacion creada.';
END
GO
PRINT 'Sección 20k: numeración de operaciones del historial de pedidos verificada.';
GO

-- ============================================================
-- WardrobeFlow — 20n. UNICIDAD DE DNI, LEGAJO Y USUARIO DEL EMPLEADO
-- ------------------------------------------------------------
-- La unicidad la validaba solo la aplicación (BLL.Cliente: "ya existe un cliente con DNI ..."),
-- sin respaldo en la base. Índices únicos filtrados:
--   · Cliente(DNI) entre los clientes ACTIVOS (un cliente dado de baja conserva su DNI y se puede
--     volver a dar de alta otro con el mismo, igual que valida BLL.Cliente).
--   · Empleado(DNI) cuando está cargado (el empleado creado junto con un usuario arranca con DNI vacío).
--   · Empleado(Legajo) cuando está cargado.
--   · Empleado(IdUsuario): un usuario tiene a lo sumo un empleado (DAL.Empleado.CrearParaUsuario).
-- Si una base ya tiene duplicados, el índice no se crea y se avisa (la instalación sigue): hay que
-- corregir los datos y volver a correr el script. Idempotente.
-- ============================================================
IF OBJECT_ID('Cliente') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Cliente_DNI_Activo' AND object_id = OBJECT_ID('Cliente'))
BEGIN
    IF EXISTS (SELECT DNI FROM Cliente WHERE Activo = 1 GROUP BY DNI HAVING COUNT(*) > 1)
        PRINT 'AVISO 20n: hay clientes activos con el mismo DNI; no se crea UX_Cliente_DNI_Activo.';
    ELSE
        EXEC('CREATE UNIQUE NONCLUSTERED INDEX UX_Cliente_DNI_Activo ON Cliente(DNI) WHERE Activo = 1;');
END
GO
IF OBJECT_ID('Empleado') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Empleado_DNI' AND object_id = OBJECT_ID('Empleado'))
BEGIN
    IF EXISTS (SELECT DNI FROM Empleado WHERE DNI <> N'' GROUP BY DNI HAVING COUNT(*) > 1)
        PRINT 'AVISO 20n: hay empleados con el mismo DNI; no se crea UX_Empleado_DNI.';
    ELSE
        EXEC('CREATE UNIQUE NONCLUSTERED INDEX UX_Empleado_DNI ON Empleado(DNI) WHERE DNI <> N'''';');
END
GO
IF OBJECT_ID('Empleado') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Empleado_Legajo' AND object_id = OBJECT_ID('Empleado'))
BEGIN
    IF EXISTS (SELECT Legajo FROM Empleado WHERE Legajo IS NOT NULL GROUP BY Legajo HAVING COUNT(*) > 1)
        PRINT 'AVISO 20n: hay empleados con el mismo legajo; no se crea UX_Empleado_Legajo.';
    ELSE
        EXEC('CREATE UNIQUE NONCLUSTERED INDEX UX_Empleado_Legajo ON Empleado(Legajo) WHERE Legajo IS NOT NULL;');
END
GO
IF OBJECT_ID('Empleado') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Empleado_IdUsuario' AND object_id = OBJECT_ID('Empleado'))
BEGIN
    IF EXISTS (SELECT IdUsuario FROM Empleado WHERE IdUsuario IS NOT NULL GROUP BY IdUsuario HAVING COUNT(*) > 1)
        PRINT 'AVISO 20n: hay usuarios con más de un empleado; no se crea UX_Empleado_IdUsuario.';
    ELSE
        EXEC('CREATE UNIQUE NONCLUSTERED INDEX UX_Empleado_IdUsuario ON Empleado(IdUsuario) WHERE IdUsuario IS NOT NULL;');
END
GO
PRINT 'Sección 20n: unicidad de DNI, legajo y usuario del empleado verificada.';
GO

-- ============================================================
-- WardrobeFlow — 20o. PN03: INTELIGENCIA COMERCIAL — CENTRO DE DECISIONES COMERCIALES
-- ------------------------------------------------------------
-- PN03 pasa a ser «Inteligencia comercial y promociones»: analizar → recomendar/decidir →
-- simular → aplicar → medir (GUI.CentroDecisionesForm, BLL.Inteligencia).
--   · Tabla DecisionComercial: lo que Gerencia decidió sobre cada recomendación del motor de
--     reglas (BLL.Inteligencia.MotorRecomendaciones). Estado (BE.EstadoDecision): 0=Pendiente,
--     1=Aceptada, 2=Descartada (motivo obligatorio), 3=Pospuesta (con PospuestaHasta),
--     4=Ejecutada. Prioridad (BE.PrioridadRecomendacion): 0=Alta, 1=Media, 2=Baja.
--     AccionElegida: la opción elegida (EsAlternativa = 1 si no fue la principal).
--     TipoReferencia/Referencia: lo que generó la decisión (Sugerencia, Promocion o
--     SolicitudBaja + su ID). FechaEvaluacion/ResultadoReal: el resultado real medido o cargado,
--     que se compara con ImpactoEstimado (cierre del ciclo). No lleva dígito verificador (como
--     SugerenciaPromocion); cada cambio de estado es un claim atómico en DAL.DecisionComercial.
--   · ParametroSistema "Comercial.*": umbrales del motor (BLL.Inteligencia.CatalogoUmbrales), con
--     sus valores de fábrica. Solo se agregan los que faltan: un valor editado por Gerencia
--     desde el diálogo "Umbrales" no se pisa.
--   · Permisos: sin patente nueva. El Centro de decisiones lo gobierna mnuSugerenciaPromocion
--     (Gerencia: GerenteComercial y Administrador), la misma patente que "Sugerir promoción".
--     Se mapea el ítem de menú nuevo en ControlMapeado (pantalla Perfiles y Permisos).
--   · Textos: la ayuda de Analítica de Negocio decía "PDF (vista previa)", pero el PDF se guarda
--     y se abre (no hay vista previa). Se corrige solo esa frase (REPLACE: no pisa otras
--     ediciones). Las claves nuevas las carga la aplicación desde traducciones.tsv.
-- Idempotente: en una base ya migrada no cambia nada.
-- ============================================================
IF OBJECT_ID('DecisionComercial', 'U') IS NULL
BEGIN
    CREATE TABLE DecisionComercial (
        IdDecision         INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_DecisionComercial PRIMARY KEY,
        Fecha              DATETIME       NOT NULL CONSTRAINT DF_DecisionComercial_Fecha DEFAULT GETDATE(),
        IdUsuario          INT            NOT NULL CONSTRAINT FK_DecisionComercial_Usuario REFERENCES Usuario(IdUsuario),
        TipoRegla          NVARCHAR(50)   NOT NULL,
        ClaveRecomendacion NVARCHAR(100)  NOT NULL,
        Prioridad          INT            NOT NULL CONSTRAINT CHK_DecisionComercial_Prioridad CHECK (Prioridad IN (0,1,2)),
        Titulo             NVARCHAR(300)  NOT NULL,
        Diagnostico        NVARCHAR(1000) NULL,
        AccionElegida      NVARCHAR(500)  NULL,
        EsAlternativa      BIT            NOT NULL CONSTRAINT DF_DecisionComercial_EsAlternativa DEFAULT 0,
        ImpactoEstimado    DECIMAL(14,2)  NOT NULL CONSTRAINT DF_DecisionComercial_Impacto DEFAULT 0,
        Estado             INT            NOT NULL CONSTRAINT CHK_DecisionComercial_Estado CHECK (Estado IN (0,1,2,3,4)),
        Motivo             NVARCHAR(500)  NULL,
        TipoReferencia     NVARCHAR(30)   NULL,
        Referencia         INT            NULL,
        PospuestaHasta     DATE           NULL,
        FechaEvaluacion    DATETIME       NULL,
        ResultadoReal      DECIMAL(14,2)  NULL,
        -- Descartar exige motivo; posponer exige la fecha hasta la que se pospone (y solo entonces).
        CONSTRAINT CHK_DecisionComercial_Descarte  CHECK (Estado <> 2 OR Motivo IS NOT NULL),
        CONSTRAINT CHK_DecisionComercial_Pospuesta CHECK ((Estado = 3 AND PospuestaHasta IS NOT NULL) OR (Estado <> 3 AND PospuestaHasta IS NULL)),
        CONSTRAINT CHK_DecisionComercial_Referencia CHECK ((TipoReferencia IS NULL AND Referencia IS NULL) OR (TipoReferencia IS NOT NULL AND Referencia IS NOT NULL)),
        CONSTRAINT CHK_DecisionComercial_TipoReferencia CHECK (TipoReferencia IS NULL OR TipoReferencia IN (N'Sugerencia', N'Promocion', N'SolicitudBaja'))
    );
    PRINT 'Tabla DecisionComercial creada.';
END
GO
IF OBJECT_ID('DecisionComercial', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DecisionComercial_Clave' AND object_id = OBJECT_ID('DecisionComercial'))
    CREATE NONCLUSTERED INDEX IX_DecisionComercial_Clave ON DecisionComercial(ClaveRecomendacion, Fecha DESC);
GO
IF OBJECT_ID('DecisionComercial', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DecisionComercial_Fecha' AND object_id = OBJECT_ID('DecisionComercial'))
    CREATE NONCLUSTERED INDEX IX_DecisionComercial_Fecha ON DecisionComercial(Fecha);
GO

-- Umbrales del motor de recomendaciones (valores de fábrica; ver BLL.Inteligencia.CatalogoUmbrales).
IF OBJECT_ID('ParametroSistema', 'U') IS NOT NULL
BEGIN
    INSERT INTO ParametroSistema (Clave, Valor, Fecha)
    SELECT v.Clave, v.Valor, GETDATE()
    FROM (VALUES
        (N'Comercial.ChurnAltoPct', N'5'),             (N'Comercial.SubidaChurnPp', N'2'),
        (N'Comercial.DescuentoBasePct', N'10'),        (N'Comercial.DescuentoMaximoPct', N'30'),
        (N'Comercial.TasaRetencionDefecto', N'30'),    (N'Comercial.HorizonteMeses', N'6'),
        (N'Comercial.RoiMinimoPct', N'20'),            (N'Comercial.MargenSeguridadPp', N'10'),
        (N'Comercial.DiasPorVencer', N'7'),            (N'Comercial.TasaRecuperoCobroPct', N'60'),
        (N'Comercial.EscasezMinimo', N'3'),            (N'Comercial.DemandaEscasez', N'2'),
        (N'Comercial.DiasSinRotacion', N'60'),         (N'Comercial.MinPrendasLiquidar', N'3'),
        (N'Comercial.RecuperoLiquidacionPct', N'40'),  (N'Comercial.RendimientoMinimoPct', N'50'),
        (N'Comercial.DiasMinimosEvaluacion', N'14'),   (N'Comercial.DiasAvisoVencimientoPromo', N'15'),
        (N'Comercial.ConversionUpgradePct', N'20'),    (N'Comercial.DiasSilencioDescarte', N'30')
    ) AS v(Clave, Valor)
    WHERE NOT EXISTS (SELECT 1 FROM ParametroSistema p WHERE p.Clave = v.Clave);
    DECLARE @umbrales INT = @@ROWCOUNT;
    IF @umbrales > 0 PRINT CONCAT('Centro de decisiones: umbrales comerciales sembrados: ', @umbrales, '.');
END
GO

-- Ítem de menú del Centro de decisiones → patente de Gerencia (Perfiles y Permisos lo muestra mapeado).
IF OBJECT_ID('ControlMapeado', 'U') IS NOT NULL
BEGIN
    INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
    SELECT ISNULL(MIN(CASE WHEN p.Estado = 1 THEN p.IdPermiso END), MIN(p.IdPermiso)), N'Menu', N'centroDecisionesToolStripMenuItem'
    FROM Permiso p
    WHERE p.NombreMenu = 'mnuSugerenciaPromocion' AND ISNULL(p.EsFamilia, 0) = 0 AND ISNULL(p.EsRol, 0) = 0
      AND NOT EXISTS (SELECT 1 FROM ControlMapeado c WHERE c.Formulario = N'Menu' AND c.NombreControl = N'centroDecisionesToolStripMenuItem')
    HAVING COUNT(*) > 0;
END
GO

-- Ayuda de Analítica de Negocio: el PDF no tiene vista previa (se guarda y se abre).
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @FrasesPdf TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Anterior NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                              Nueva NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @FrasesPdf (Codigo, Anterior, Nueva) VALUES
        (N'ES', N'Exportar a PDF (vista previa)', N'Exportar a PDF (se guarda y se abre)'),
        (N'EN', N'Export to PDF (preview)', N'Export to PDF (saved and opened)'),
        (N'RU', N'«Экспорт в PDF» (предпросмотр)', N'«Экспорт в PDF» (файл сохраняется и открывается)'),
        (N'PT', N'Exportar para PDF (pré-visualização)', N'Exportar para PDF (salvo e aberto)');
    UPDATE t SET t.Texto = REPLACE(t.Texto, f.Anterior, f.Nueva)
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @FrasesPdf f ON f.Codigo = i.Codigo
    WHERE c.Clave = N'ayuda.AnaliticaNegocioForm'
      AND CHARINDEX(f.Anterior COLLATE Latin1_General_BIN2, t.Texto COLLATE Latin1_General_BIN2) > 0;
    DECLARE @ayudas INT = @@ROWCOUNT;
    IF @ayudas > 0 PRINT CONCAT('Ayuda de Analítica de Negocio corregida (PDF sin vista previa): ', @ayudas, ' idioma(s).');
END
GO
PRINT 'Sección 20o: Centro de decisiones comerciales (PN03) verificado.';
GO

-- ============================================================
-- WardrobeFlow — 20p. PN03: METAS COMERCIALES, DETALLE DE LAS DECISIONES Y REGLAS RETIRADAS
-- ------------------------------------------------------------
--   · Tabla MetaComercial: el objetivo mensual de Gerencia por indicador (BLL.Inteligencia.MetasComerciales).
--     Periodo = primer día del mes. Indicador (BE.IndicadorMeta): 0=MRR ($), 1=% de bajas, 2=ocupación
--     del stock (%); el catálogo admite más (BLL.Inteligencia.CatalogoIndicadores). Sentido
--     (BE.SentidoMeta): 0=mayor es mejor, 1=menor es mejor. Una meta por indicador y mes
--     (UQ_MetaComercial_PeriodoIndicador). Sin dígito verificador, como DecisionComercial.
--   · Tabla DecisionComercialDetalle: qué tocó una decisión ejecutada (Tipo 'Cliente' = clientes
--     contactados en una campaña, para medir cuántos renovaron; 'Prenda' = prendas dadas de baja o
--     mandadas a revisión). Se borra con su decisión (ON DELETE CASCADE).
--   · Umbrales de las reglas retiradas del motor (Desempeño de vendedores y Ocupación del stock: solo
--     registraban la decisión, sin operación que ejecutar): se borran de ParametroSistema.
--   · Permisos: sin patente nueva; las metas las edita Gerencia con mnuSugerenciaPromocion (como los
--     umbrales del motor).
-- Idempotente: en una base ya migrada no cambia nada.
-- ============================================================
IF OBJECT_ID('MetaComercial', 'U') IS NULL
BEGIN
    CREATE TABLE MetaComercial (
        IdMeta        INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_MetaComercial PRIMARY KEY,
        Periodo       DATE          NOT NULL,
        Indicador     INT           NOT NULL,
        ValorObjetivo DECIMAL(14,2) NOT NULL,
        Sentido       INT           NOT NULL,
        IdUsuario     INT           NOT NULL CONSTRAINT FK_MetaComercial_Usuario REFERENCES Usuario(IdUsuario),
        Fecha         DATETIME      NOT NULL CONSTRAINT DF_MetaComercial_Fecha DEFAULT GETDATE(),
        CONSTRAINT UQ_MetaComercial_PeriodoIndicador UNIQUE (Periodo, Indicador),
        CONSTRAINT CHK_MetaComercial_Periodo   CHECK (DAY(Periodo) = 1),
        CONSTRAINT CHK_MetaComercial_Indicador CHECK (Indicador >= 0),
        CONSTRAINT CHK_MetaComercial_Sentido   CHECK (Sentido IN (0, 1)),
        CONSTRAINT CHK_MetaComercial_Valor     CHECK (ValorObjetivo >= 0)
    );
    PRINT 'Tabla MetaComercial creada.';
END
GO

IF OBJECT_ID('DecisionComercialDetalle', 'U') IS NULL AND OBJECT_ID('DecisionComercial', 'U') IS NOT NULL
BEGIN
    CREATE TABLE DecisionComercialDetalle (
        IdDecision INT          NOT NULL CONSTRAINT FK_DecisionComercialDetalle_Decision
                                REFERENCES DecisionComercial(IdDecision) ON DELETE CASCADE,
        Tipo       NVARCHAR(20) NOT NULL CONSTRAINT CHK_DecisionComercialDetalle_Tipo CHECK (Tipo IN (N'Cliente', N'Prenda')),
        IdObjeto   INT          NOT NULL,
        CONSTRAINT PK_DecisionComercialDetalle PRIMARY KEY (IdDecision, Tipo, IdObjeto)
    );
    PRINT 'Tabla DecisionComercialDetalle creada.';
END
GO

-- Fecha de ejecución de la decisión: desde cuándo rige y desde cuándo se mide su resultado (una
-- decisión pospuesta 90 días y ejecutada hoy rige desde hoy, no desde que se creó).
IF OBJECT_ID('DecisionComercial', 'U') IS NOT NULL AND COL_LENGTH('DecisionComercial', 'FechaEjecucion') IS NULL
BEGIN
    ALTER TABLE DecisionComercial ADD FechaEjecucion DATETIME NULL;
    PRINT 'Columna DecisionComercial.FechaEjecucion agregada.';
END
GO

-- Una sola decisión abierta (Aceptada = 1 o Pospuesta = 3) por recomendación. Lo valida la BLL; el índice
-- filtrado lo respalda. Si una base vieja ya tuviera dos abiertas para la misma clave, no se crea (y se avisa).
IF OBJECT_ID('DecisionComercial', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_DecisionComercial_Abierta' AND object_id = OBJECT_ID('DecisionComercial'))
BEGIN
    IF EXISTS (SELECT ClaveRecomendacion FROM DecisionComercial WHERE Estado IN (1, 3) GROUP BY ClaveRecomendacion HAVING COUNT(*) > 1)
        PRINT 'AVISO: hay recomendaciones con más de una decisión abierta; no se crea UX_DecisionComercial_Abierta.';
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_DecisionComercial_Abierta ON DecisionComercial(ClaveRecomendacion) WHERE Estado IN (1, 3);
        PRINT 'Índice UX_DecisionComercial_Abierta creado.';
    END
END
GO

-- Umbrales de las reglas retiradas (ReglaVendedores y ReglaOcupacionStock).
IF OBJECT_ID('ParametroSistema', 'U') IS NOT NULL
BEGIN
    DELETE FROM ParametroSistema
    WHERE Clave IN (N'Comercial.ConcentracionVendedorPct', N'Comercial.BajoPromedioVendedorPct',
                    N'Comercial.OcupacionMinimaPct', N'Comercial.OcupacionMaximaPct');
    IF @@ROWCOUNT > 0 PRINT 'Umbrales de las reglas retiradas (vendedores, ocupación) borrados.';
END
GO

PRINT 'Sección 20p: metas comerciales y detalle de las decisiones (PN03) verificados.';
GO

-- ============================================================
-- WardrobeFlow — 21b. DEPÓSITO PUEDE OPERAR PEDIDOS REALIZADOS
-- ------------------------------------------------------------
-- Despachar, registrar la entrega y registrar la devolución (que abre PN04) se hacen desde Pedidos
-- Realizados y exigen mnuPedidosRealizadosEditar. El rol Deposito solo tenía la patente de VER, así
-- que no podía registrar una devolución. Idempotente; corre también sobre bases ya creadas.
-- ============================================================
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT r.IdPermiso, e.IdPermiso
FROM   Permiso r
JOIN   Permiso e ON e.NombreMenu = 'mnuPedidosRealizadosEditar'
WHERE  r.EsRol = 1 AND r.Estado = 1 AND r.Nombre = 'Deposito'
  AND  NOT EXISTS (SELECT 1 FROM PermisoRelacion x WHERE x.IdPadre = r.IdPermiso AND x.IdHijo = e.IdPermiso);
PRINT 'Permiso de edición de Pedidos Realizados asegurado para el rol Deposito.';
GO

-- ============================================================
-- WardrobeFlow — 21c. PN01: CONTROL DE STOCK (diagrama de actividad de Armar pedido)
-- ------------------------------------------------------------
-- El Vendedor envía la selección a control de stock (sin reservar prendas); Depósito revisa
-- el stock y emite el informe de faltantes con alternativas, o confirma las prendas y las
-- separa (recién ahí pasan a En uso); el Vendedor formaliza el pedido. Si el cliente no
-- ajusta la selección, se asienta el desistimiento.
-- Estados nuevos de Pedido: 4 EnControlStock, 5 ConFaltantes, 6 Separado, 7 Desistido.
-- Idempotente; corre también sobre bases ya creadas.
-- ============================================================
IF COL_LENGTH('Pedido', 'FechaEnvioControl') IS NULL
    ALTER TABLE Pedido ADD FechaEnvioControl DATETIME NULL;
IF COL_LENGTH('Pedido', 'FechaControl') IS NULL
    ALTER TABLE Pedido ADD FechaControl DATETIME NULL;
IF COL_LENGTH('Pedido', 'IdEmpleadoControl') IS NULL
    ALTER TABLE Pedido ADD IdEmpleadoControl INT NULL
        CONSTRAINT FK_Pedido_EmpleadoControl REFERENCES Empleado(IdEmpleado);
IF COL_LENGTH('Pedido', 'FechaSeparacion') IS NULL
    ALTER TABLE Pedido ADD FechaSeparacion DATETIME NULL;
IF COL_LENGTH('Pedido', 'FechaFormalizacion') IS NULL
    ALTER TABLE Pedido ADD FechaFormalizacion DATETIME NULL;
IF COL_LENGTH('Pedido', 'MotivoDesistimiento') IS NULL
    ALTER TABLE Pedido ADD MotivoDesistimiento NVARCHAR(500) NULL;
IF COL_LENGTH('Pedido', 'EtapaDesistimiento') IS NULL
    ALTER TABLE Pedido ADD EtapaDesistimiento NVARCHAR(20) NULL;
IF COL_LENGTH('PedidoPrenda', 'Confirmada') IS NULL
    ALTER TABLE PedidoPrenda ADD Confirmada BIT NOT NULL
        CONSTRAINT DF_PedidoPrenda_Confirmada DEFAULT 0;
GO

-- Estado admite los 8 valores (antes 0..3).
IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = 'CHK_Pedido_Estado' AND definition NOT LIKE '%7%')
    ALTER TABLE Pedido DROP CONSTRAINT CHK_Pedido_Estado;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Pedido_Estado')
    ALTER TABLE Pedido ADD CONSTRAINT CHK_Pedido_Estado CHECK (Estado IN (0,1,2,3,4,5,6,7));

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Pedido_EtapaDesistimiento')
    ALTER TABLE Pedido ADD CONSTRAINT CHK_Pedido_EtapaDesistimiento
        CHECK (EtapaDesistimiento IS NULL OR EtapaDesistimiento IN ('Cupo', 'Disponibilidad'));

-- Un pedido desistido siempre tiene etapa y motivo (Aviso de desistimiento).
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Pedido_Desistido')
    ALTER TABLE Pedido ADD CONSTRAINT CHK_Pedido_Desistido
        CHECK (Estado <> 7 OR (EtapaDesistimiento IS NOT NULL AND MotivoDesistimiento IS NOT NULL));
GO

-- Backfill: los pedidos que ya estaban Pendientes/Despachados/Entregados antes de PN01 se
-- formalizaron al crearlos (no pasaban por control de stock): FechaFormalizacion = FechaPedido.
-- Idempotente (solo completa los NULL). FechaFormalizacion no forma parte del DVH de Pedido.
UPDATE Pedido SET FechaFormalizacion = FechaPedido
WHERE Estado IN (0,1,2) AND FechaFormalizacion IS NULL;
IF @@ROWCOUNT > 0 PRINT 'Pedidos existentes: FechaFormalizacion completada con FechaPedido.';
GO

-- Informe de disponibilidad: una fila por prenda faltante del pedido...
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PedidoFaltante')
BEGIN
    CREATE TABLE PedidoFaltante (
        IdPedido          INT NOT NULL,
        IdPrenda          INT NOT NULL,
        EstadoAlRevisar   INT NOT NULL,                 -- estado de la prenda al revisar el stock
        ReservadaParaOtro BIT NOT NULL DEFAULT 0,       -- reservada por Lista de Espera para otro cliente
        CONSTRAINT PK_PedidoFaltante PRIMARY KEY (IdPedido, IdPrenda),
        CONSTRAINT FK_PedidoFaltante_Linea FOREIGN KEY (IdPedido, IdPrenda)
            REFERENCES PedidoPrenda(IdPedido, IdPrenda)
    );
    PRINT 'Tabla PedidoFaltante creada.';
END
ELSE
    PRINT 'Tabla PedidoFaltante ya existe — sin cambios.';
GO

-- ...y una fila por alternativa que propone el sistema para cada faltante.
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PedidoFaltanteAlternativa')
BEGIN
    CREATE TABLE PedidoFaltanteAlternativa (
        IdPedido            INT NOT NULL,
        IdPrenda            INT NOT NULL,
        IdPrendaAlternativa INT NOT NULL REFERENCES Prenda(IdPrenda),
        CONSTRAINT PK_PedidoFaltanteAlternativa PRIMARY KEY (IdPedido, IdPrenda, IdPrendaAlternativa),
        CONSTRAINT FK_PedidoFaltanteAlternativa_Faltante FOREIGN KEY (IdPedido, IdPrenda)
            REFERENCES PedidoFaltante(IdPedido, IdPrenda),
        CONSTRAINT CHK_PedidoFaltanteAlternativa_Distinta CHECK (IdPrendaAlternativa <> IdPrenda)
    );
    PRINT 'Tabla PedidoFaltanteAlternativa creada.';
END
ELSE
    PRINT 'Tabla PedidoFaltanteAlternativa ya existe — sin cambios.';
GO

-- Patentes de Control de Stock: ver (menú) y editar (acciones de Depósito).
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT v.Nombre, v.NombreMenu, v.Tipo, 1, 0, 0
FROM (VALUES
    ('Control de Stock',                'mnuControlStock',       'Inventario'),
    ('Configurar Control de Stock',     'mnuControlStockEditar', N'Acción')
) AS v(Nombre, NombreMenu, Tipo)
WHERE NOT EXISTS (SELECT 1 FROM Permiso p
                  WHERE p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0);
GO

-- Asignación: Deposito (cumple el carril "Controlador de Stock" del diagrama) y Administrador.
-- GerenteInventario las hereda porque contiene al rol Deposito.
INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador', 'mnuControlStock'),
    ('Administrador', 'mnuControlStockEditar'),
    ('Deposito',      'mnuControlStock'),
    ('Deposito',      'mnuControlStockEditar')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permisos de Control de Stock asignados a Administrador y Deposito.';
GO

-- Mapeo del ítem de menú (pantalla "Perfiles y Permisos").
INSERT INTO ControlMapeado (IdPermiso, Formulario, NombreControl)
SELECT ISNULL(MIN(CASE WHEN p.Estado = 1 THEN p.IdPermiso END), MIN(p.IdPermiso)), v.Formulario, v.NombreControl
FROM (VALUES
    ('mnuControlStock', 'Menu', 'controlStockToolStripMenuItem')
) AS v(NombreMenu, Formulario, NombreControl)
JOIN Permiso p ON p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM ControlMapeado c
                  WHERE c.Formulario = v.Formulario AND c.NombreControl = v.NombreControl)
GROUP BY v.Formulario, v.NombreControl;
GO

-- ============================================================
-- WardrobeFlow — 21c2. PATENTE DE ESCRITURA DE LISTA DE ESPERA
-- ------------------------------------------------------------
-- BLL.ListaEspera.Anotar/Cancelar exigían mnuStockEditar (patente de Depósito),
-- así que el Vendedor —que ve Lista de Espera para anotar al cliente— recibía
-- "sin permiso". Ahora exigen mnuListaEsperaEditar (fallback: mnuListaEspera,
-- ver BLL.PermisosAccion). Se asigna a Administrador, Vendedor y Deposito
-- (GerenteComercial/GerenteInventario la heredan por Composite). Idempotente.
-- ============================================================
INSERT INTO Permiso (Nombre, NombreMenu, TipoComponente, Estado, EsFamilia, EsRol)
SELECT 'Configurar Lista de Espera', 'mnuListaEsperaEditar', 'Acción', 1, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM Permiso p
                  WHERE p.NombreMenu = 'mnuListaEsperaEditar' AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0);
GO

INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES
    ('Administrador', 'mnuListaEsperaEditar'),
    ('Vendedor',      'mnuListaEsperaEditar'),
    ('Deposito',      'mnuListaEsperaEditar')
) AS v(Rol, NombreMenu)
JOIN Permiso rol ON rol.Nombre = v.Rol AND rol.EsRol = 1
JOIN Permiso pat ON pat.NombreMenu = v.NombreMenu AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x
                  WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
PRINT 'Permiso mnuListaEsperaEditar asignado a Administrador, Vendedor y Deposito.';
GO

-- ============================================================
-- WardrobeFlow — 21e. DATOS DE DEMO: ¿CORRESPONDE CARGARLOS?
-- ------------------------------------------------------------
-- Los datos de demo (escenarios de PN01–PN04 y 6 meses de historia para la analítica) los carga
-- el procedimiento dbo.RestablecerDatosDemo, que se crea más abajo (sección 21z3b: necesita las
-- columnas que agregan las secciones 21z y 21z3). Acá solo se DECIDE si corresponde cargarlos y se
-- deja la marca ParametroSistema 'DemoCargaPendiente' = '1'; la sección 21z3b la atiende.
-- Corresponde UNA sola vez: en una instalación NUEVA, o cuando se pidió restablecer la demo con
-- BD/Reset_Datos_Demo.sql (marca 'ResetDatosDemo' = '1'). La app también puede restablecerla en
-- cualquier momento (Administración → Backup → "Restablecer datos de demo"), con backup previo.
-- Nunca se mezclan con datos reales: con datos propios y sin pedido de restablecer, no hace nada.
-- El detalle de cada escenario está en la sección 21z3b y en Instalador/Credenciales_Iniciales.txt.
-- ============================================================
DECLARE @sembrar21 BIT = 0;
DECLARE @reset21   BIT = CASE WHEN EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'ResetDatosDemo' AND Valor = N'1') THEN 1 ELSE 0 END;
IF @reset21 = 1
    SET @sembrar21 = 1;
ELSE IF EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SeedDemo21')
    PRINT 'Datos de demo (sección 21e) ya aplicados — sin cambios.';
ELSE IF EXISTS (SELECT 1 FROM Cliente WHERE Nombre = N'Julieta' AND Apellido = N'Navarro')
BEGIN
    -- Base de una versión anterior que ya tenía los datos de prueba viejos (marca vieja).
    INSERT INTO ParametroSistema (Clave, Valor, Fecha) VALUES (N'SeedDemo21', N'Aplicada por una versión anterior', GETDATE());
    PRINT 'Datos de prueba (sección 21e) ya aplicados por una versión anterior — sin cambios.';
END
ELSE IF NOT EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'SemillaUsuarios' AND Valor = N'Pendiente')
     OR EXISTS (SELECT 1 FROM Contratacion)
     OR (SELECT COUNT(*) FROM Pedido) > 3
BEGIN
    INSERT INTO ParametroSistema (Clave, Valor, Fecha) VALUES (N'SeedDemo21', N'Omitida: la base ya tenía datos', GETDATE());
    PRINT 'AVISO: la base ya tiene datos de negocio; no se cargan los datos de demo (sección 21e).';
END
ELSE
    SET @sembrar21 = 1;

IF @sembrar21 = 1
BEGIN
    MERGE ParametroSistema AS t
    USING (VALUES (N'DemoCargaPendiente', N'1')) AS s(Clave, Valor) ON t.Clave = s.Clave
    WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
    WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
    DELETE FROM ParametroSistema WHERE Clave = N'ResetDatosDemo';
    PRINT 'Datos de demo: se cargan en la sección 21z3b (procedimiento dbo.RestablecerDatosDemo).';
END
GO

-- ============================================================
-- WardrobeFlow — 21d. AJUSTES FINALES DE PERMISOS Y MARCAS DE INSTALACIÓN
-- ------------------------------------------------------------
-- (1) GerenteComercial solo VE Pedidos Realizados: el "grandfather" de la sección 03 le copia
--     mnuPedidosRealizadosEditar porque tiene la patente de Ver asignada directo. Se quita esa
--     arista después del grandfather (en cada corrida, porque el grandfather corre siempre).
-- (2) Deposito termina con Ver + Editar de Pedidos Realizados (despacha, entrega y registra la
--     devolución que abre PN04). Idempotente.
-- (3) Cierra la marca de siembra de usuarios: a partir de acá ninguna corrida del script
--     vuelve a crear cuentas semilla/demo.
-- ============================================================
DELETE r FROM PermisoRelacion r
JOIN Permiso rol ON rol.IdPermiso = r.IdPadre AND rol.Nombre = 'GerenteComercial' AND rol.EsRol = 1
JOIN Permiso e   ON e.IdPermiso   = r.IdHijo  AND e.NombreMenu = 'mnuPedidosRealizadosEditar'
                AND ISNULL(e.EsFamilia,0) = 0 AND ISNULL(e.EsRol,0) = 0;
IF @@ROWCOUNT > 0 PRINT 'GerenteComercial: se quitó mnuPedidosRealizadosEditar (solo consulta Pedidos Realizados).';

INSERT INTO PermisoRelacion (IdPadre, IdHijo)
SELECT rol.IdPermiso, pat.IdPermiso
FROM (VALUES ('mnuPedidosRealizados'), ('mnuPedidosRealizadosEditar')) AS v(NombreMenu)
JOIN Permiso rol ON rol.Nombre = 'Deposito' AND rol.EsRol = 1 AND rol.Estado = 1
JOIN Permiso pat ON pat.IdPermiso = (SELECT TOP 1 p.IdPermiso FROM Permiso p
                                     WHERE p.NombreMenu = v.NombreMenu AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0
                                     ORDER BY p.Estado DESC, p.IdPermiso)
WHERE NOT EXISTS (SELECT 1 FROM PermisoRelacion x WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);

UPDATE ParametroSistema SET Valor = N'Aplicada', Fecha = GETDATE()
WHERE Clave = N'SemillaUsuarios' AND Valor = N'Pendiente';
GO

-- (3b) Reinstalación: se restaura la configuración de permisos del Administrador (ver la foto al
--      principio del script). Se quitan las relaciones entre roles/patentes que ya existían y que
--      esta corrida volvió a agregar; las de componentes nuevos de esta versión se conservan.
IF OBJECT_ID('ReinstalacionPermisos', 'U') IS NOT NULL
BEGIN
    DECLARE @maxIdAntes INT = (SELECT TRY_CONVERT(INT, Valor) FROM ParametroSistema WHERE Clave = N'ReinstalacionMaxIdPermiso');
    DECLARE @restituidas INT = 0;
    IF @maxIdAntes IS NOT NULL
    BEGIN
        DELETE r FROM PermisoRelacion r
        WHERE r.IdPadre <= @maxIdAntes AND r.IdHijo <= @maxIdAntes
          AND NOT EXISTS (SELECT 1 FROM ReinstalacionPermisos s WHERE s.IdPadre = r.IdPadre AND s.IdHijo = r.IdHijo);
        SET @restituidas = @@ROWCOUNT;
    END
    -- Las filas quitadas son las que esta corrida acababa de insertar: la tabla vuelve a su estado
    -- anterior y sus dígitos verificadores siguen valiendo (no se pide recálculo, que "lavaría" una
    -- manipulación previa).
    IF @restituidas > 0
        PRINT 'Reinstalación: se respetaron los permisos que el Administrador había quitado (' + CONVERT(NVARCHAR(10), @restituidas) + ' relación/es).';
    DROP TABLE ReinstalacionPermisos;
    DELETE FROM ParametroSistema WHERE Clave = N'ReinstalacionMaxIdPermiso';
END
GO

-- (4) Cobro recurrente N01 → rol Caja (quien vende no cobra; mismo criterio que PN02).
--     Migración de una sola vez (marca 'CobroN01Caja'): se quita la patente al Vendedor y
--     se le da a Caja. Después, el Administrador puede reasignarla desde el Gestor de Perfiles
--     sin que una reinstalación se lo deshaga. Si la base ya tenía dígitos verificadores
--     calculados (formato 2), se pide el recálculo para que el cambio no dé falsa alarma.
IF NOT EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'CobroN01Caja')
BEGIN
    DECLARE @cambios INT = 0;
    DELETE r FROM PermisoRelacion r
    JOIN Permiso rol ON rol.IdPermiso = r.IdPadre AND rol.Nombre = 'Vendedor' AND rol.EsRol = 1
    JOIN Permiso pat ON pat.IdPermiso = r.IdHijo  AND pat.NombreMenu = 'mnuCobroSuscripcion'
                    AND ISNULL(pat.EsFamilia,0) = 0 AND ISNULL(pat.EsRol,0) = 0;
    SET @cambios = @cambios + @@ROWCOUNT;
    -- Fila legacy (RolPermiso ya no es fuente de verdad, pero se deja coherente).
    DELETE rp FROM RolPermiso rp
    JOIN Permiso pat ON pat.IdPermiso = rp.IdPermiso AND pat.NombreMenu = 'mnuCobroSuscripcion'
    WHERE rp.Rol = 'Vendedor';

    INSERT INTO PermisoRelacion (IdPadre, IdHijo)
    SELECT rol.IdPermiso, pat.IdPermiso
    FROM Permiso rol
    JOIN Permiso pat ON pat.IdPermiso = (SELECT TOP 1 p.IdPermiso FROM Permiso p
                                         WHERE p.NombreMenu = 'mnuCobroSuscripcion' AND ISNULL(p.EsFamilia,0) = 0 AND ISNULL(p.EsRol,0) = 0
                                         ORDER BY p.Estado DESC, p.IdPermiso)
    WHERE rol.Nombre = 'Caja' AND rol.EsRol = 1
      AND NOT EXISTS (SELECT 1 FROM PermisoRelacion x WHERE x.IdPadre = rol.IdPermiso AND x.IdHijo = pat.IdPermiso);
    SET @cambios = @cambios + @@ROWCOUNT;

    IF @cambios > 0 AND EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'FormatoDV' AND TRY_CONVERT(INT, Valor) >= 2)
    BEGIN
        MERGE ParametroSistema AS t
        USING (VALUES (N'DVReinicializar', N'1')) AS s(Clave, Valor) ON t.Clave = s.Clave
        WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
        WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
    END
    INSERT INTO ParametroSistema (Clave, Valor, Fecha) VALUES (N'CobroN01Caja', N'Aplicada', GETDATE());
    PRINT 'Cobro de suscripción (N01): la patente pasó del Vendedor a Caja.';
END
GO

-- ============================================================
-- WardrobeFlow — 21z. CAMBIO DE PLAN PROGRAMADO (PN02, nodo a11)
-- ------------------------------------------------------------
-- Pasar a un plan igual o más barato con el período vigente: el plan nuevo rige al vencer el
-- período pagado. Cliente guarda el plan siguiente y desde cuándo rige (la app lo aplica ese día).
-- Las dos columnas entran en el dígito verificador de Cliente: si se agregan a una base ya
-- instalada, se pide el recálculo (sección 22, 'DVReinicializar'). Idempotente.
-- ============================================================
IF COL_LENGTH('Cliente', 'IdPlanSiguiente') IS NULL
BEGIN
    ALTER TABLE Cliente ADD
        IdPlanSiguiente INT      NULL CONSTRAINT FK_Cliente_PlanSiguiente REFERENCES PlanSuscripcion(IdPlan),
        FechaCambioPlan DATETIME NULL;
    MERGE ParametroSistema AS t
    USING (VALUES (N'DVReinicializar', N'1')) AS s(Clave, Valor) ON t.Clave = s.Clave
    WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
    WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
    PRINT 'Cliente: cambio de plan programado (IdPlanSiguiente, FechaCambioPlan); DV a recalcular.';
END
GO

-- ============================================================
-- WardrobeFlow — 21z2. DV DE CLIENTE: TAMBIÉN EL BENEFICIO POR REFERIDO
-- ------------------------------------------------------------
-- BeneficioReferidoOtorgado pasa a formar parte del dígito verificador de Cliente. En una base ya
-- instalada se pide UNA vez el recálculo (marca 'DVClienteBeneficio'). Idempotente.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'DVClienteBeneficio')
BEGIN
    MERGE ParametroSistema AS t
    USING (VALUES (N'DVReinicializar', N'1')) AS s(Clave, Valor) ON t.Clave = s.Clave
    WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
    WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
    INSERT INTO ParametroSistema (Clave, Valor, Fecha) VALUES (N'DVClienteBeneficio', N'Aplicada', GETDATE());
    PRINT 'DV de Cliente: incluye el beneficio por referido; DV a recalcular.';
END
GO

-- ============================================================
-- WardrobeFlow — 21z3. FECHA DE DEVOLUCIÓN DEL PEDIDO (PN04)
-- ------------------------------------------------------------
-- Tras "Registrar devolución" el pedido sigue Entregado (no hay estado nuevo: la máquina de
-- estados y los diagramas aprobados no cambian); Pedido.FechaDevolucion distingue un pedido
-- devuelto de uno con las prendas todavía en poder del cliente, y permite listar los atrasados
-- (Entregado, sin devolución, con 30 días o más desde la entrega: compra tácita).
-- La columna entra en el dígito verificador de Pedido: al agregarla se pide el recálculo
-- (sección 22, 'DVReinicializar').
-- Datos previos: un Entregado al que ya no le queda ninguna prenda En uso a nombre de su cliente
-- se tomó como devuelto; la fecha sale del mantenimiento que abrió la devolución (o, si no hay,
-- de la entrega). Se completa una sola vez (marca 'PedidoFechaDevolucion'); la sección 21e borra
-- la marca al restablecer la demo, para que su historial entregado quede devuelto. Idempotente.
-- ============================================================
IF COL_LENGTH('Pedido', 'FechaDevolucion') IS NULL
BEGIN
    ALTER TABLE Pedido ADD FechaDevolucion DATETIME NULL;
    PRINT 'Pedido: columna FechaDevolucion agregada.';
END
GO

IF NOT EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'PedidoFechaDevolucion')
BEGIN
    UPDATE ped
       SET FechaDevolucion = ISNULL(
               (SELECT MAX(m.FechaEntrada)
                  FROM MantenimientoPrenda m
                  INNER JOIN PedidoPrenda pp ON pp.IdPrenda = m.IdPrenda AND pp.IdPedido = ped.IdPedido
                 WHERE m.Origen = 1 AND m.FechaEntrada >= ped.FechaEntrega),   -- 1 = devolución (sección 20f)
               ped.FechaEntrega)
      FROM Pedido ped
     WHERE ped.Estado = 2                       -- Entregado
       AND ped.FechaEntrega IS NOT NULL
       AND ped.FechaDevolucion IS NULL
       AND NOT EXISTS (SELECT 1
                         FROM PedidoPrenda pp
                         INNER JOIN Prenda pr ON pr.IdPrenda = pp.IdPrenda
                        WHERE pp.IdPedido = ped.IdPedido
                          AND pr.Estado = 1     -- En uso
                          AND pr.IdClienteActual = ped.IdCliente
                          -- La prenda cuenta para ESTE pedido solo si no la volvió a pedir el cliente
                          -- en un pedido posterior (si no, el pedido viejo quedaría "atrasado" para siempre).
                          AND NOT EXISTS (SELECT 1
                                            FROM PedidoPrenda pp2
                                            INNER JOIN Pedido p2 ON p2.IdPedido = pp2.IdPedido
                                           WHERE pp2.IdPrenda = pp.IdPrenda
                                             AND p2.IdPedido <> ped.IdPedido
                                             AND p2.FechaPedido > ped.FechaPedido
                                             AND p2.Estado IN (0, 1, 2, 4, 5, 6)));
    DECLARE @devueltos INT = @@ROWCOUNT;

    -- Recálculo del DV de Pedido: la columna nueva entra en el DVH (y cambian las filas completadas).
    MERGE ParametroSistema AS t
    USING (VALUES (N'DVReinicializar', N'1')) AS s(Clave, Valor) ON t.Clave = s.Clave
    WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
    WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
    INSERT INTO ParametroSistema (Clave, Valor, Fecha) VALUES (N'PedidoFechaDevolucion', N'Aplicada', GETDATE());
    PRINT 'Pedido: FechaDevolucion completada en ' + CAST(@devueltos AS VARCHAR(10)) + ' pedido(s) ya devuelto(s); DV a recalcular.';
END
GO

-- ============================================================
-- WardrobeFlow — 21z3b. DATOS DE DEMO: PROCEDIMIENTO dbo.RestablecerDatosDemo
-- ------------------------------------------------------------
-- Un solo código para cargar la demo: lo usa la instalación nueva (marca 'DemoCargaPendiente' de la
-- sección 21e), BD/Reset_Datos_Demo.sql y la app (Administración → Backup → "Restablecer datos de
-- demo", que antes hace un backup). Va después de 21z y 21z3 porque usa sus columnas.
-- Borra los datos de negocio (clientes, prendas, pedidos, contrataciones, cobros, renovaciones,
-- promociones, decisiones comerciales, lista de espera, cargos y bitácora de negocio) y carga los
-- escenarios con TODAS las
-- fechas relativas a GETDATE(): la demo tiene sentido el día que se restablece (hacerlo como máximo
-- 2 días antes de la defensa: la reserva de la lista de espera dura 48 h). Usuarios, planes,
-- permisos, traducciones y bitácora del sistema no se tocan. Todo o nada (una transacción).
-- El APELLIDO de cada cliente de escenario dice qué caso muestra:
--   PN01 Armar pedido (vendedor + deposito)
--     Juan PedidoFeliz ............ vigente, sin pedido en curso (con historial): camino feliz
--     Ana PrendasNoDisponibles .... pedido En control de stock con una prenda que ya tiene Pedro
--     Nicolas ExcedeCupo .......... plan Básico (5 prendas): elegir 6 → exceso de cupo / desistir
--     Sofia ListaParaFormalizar ... pedido Separado → el Vendedor lo formaliza
--     Pedro PedidoActivo .......... pedido formalizado (Pendiente de despacho) → aviso de pedido activo
--     Lucia SuscripcionVencida .... vencida hace 10 días → aviso de suscripción no vigente (y Caja la cobra)
--     Tomas SuscripcionPausada .... pausada → aviso de suscripción no vigente
--     Valeria ConFaltantes ........ pedido Con faltantes (alternativas) → el Vendedor ajusta o desiste
--   PN02 Comercialización y cobro (vendedor + caja)
--     Laura SinPlan ............... registrada sin plan (desistió ayer) → contratar
--     Rocio ReferidaPorJuan ....... sin plan, referida por Juan → al cobrar se acredita el beneficio
--     Diego PagoPendiente ......... contratación Estándar trimestral esperando a Caja
--     Elena TercerIntentoFallido .. contratación con 2 intentos fallidos → el 3.º la cancela
--     Sergio PagoRechazado ........ contratación ya cancelada por el 3.er intento fallido
--     Martina PorVencer ........... vence en 5 días → alerta "por vencer", renovación y cobro
--     Carla EnGracia .............. cobro fallido: en período de gracia (3 días)
--     Raul Suspendido ............. gracia vencida: suspendido por falta de pago
--     Federico CambioProgramado ... Premium con cambio a Estándar programado al vencer
--     Gustavo DadoDeBaja .......... cliente dado de baja (no aparece en las listas; sí en historiales)
--     (DNI 40000099: no existe → rama "¿Registrado? No")
--   PN03 Promociones: sugerencias pendientes, evaluada y descartada; promociones En revisión,
--     Vigente, Rechazada, Vigente con solicitud de baja, Dada de baja y Vencida.
--     Centro de decisiones comerciales (todo relativo a hoy, con el período por defecto de 90 días):
--       · Analizar: KPI con variación (% de bajas del período ~17 % contra ~33 % del anterior), 6 meses
--         de tendencia, ingreso en riesgo por plan, stock por categoría y rotación.
--       · Cohortes: 6 cohortes comparables; la peor (la de hace ~5 meses) la explica el plan Básico →
--         "Actuar sobre la peor cohorte" lleva a su recomendación de bajas.
--       · Recomendaciones: bajas del Básico (33 % de bajas, 10 clientes en riesgo: la opción ganadora
--         es una promoción de retención 30 % / 60 días → "Simular esta opción"), extender la promoción
--         Vigente del Estándar, dar de baja la de Premium con solicitud, upgrade de plan (Bruno
--         UsaTodoElCupo), escasez, prendas sin rotación, mantenimiento excesivo y la renovación pospuesta.
--       · Historial (paso 9b): 4 decisiones — Ejecutada con sugerencia (promoción Vencida del Básico,
--         estimado vs real), campaña de renovación Ejecutada "en medición" (clientes contactados en
--         DecisionComercialDetalle), Pospuesta y Descartada con motivo.
--       · Medir resultado: Mantener (Vencida del Básico), Extender (Vigente del Estándar, vence en 10
--         días) y Dar de baja (Premium con solicitud y Dada de baja).
--       · Metas (paso 9b2): MRR del mes cumplida, % de bajas en camino, ocupación en riesgo; MRR del mes
--         anterior no cumplida y % de bajas del mes anterior cumplido.
--     Clientes del Centro (DNI 4100xxxx): 12 del plan Básico (en riesgo, antiguos, bajas viejas y Bruno
--     UsaTodoElCupo, con las 5 prendas de su cupo) y 8 fundadores de Estándar y Premium.
--   Usuarios: 2 versiones (Memento) del usuario vendedor para la serialización a XML/JSON (paso 9c).
--   PN04 Devoluciones (deposito) y logística (logistico)
--     Julia EnInspeccion .......... devolvió el "Blazer Azul M" ayer → cola de Inspección de devolución
--     Hernan DevolucionPendiente .. pedido Entregado hace 10 días → Registrar devolución
--     Paula CompraTacita .......... Entregado hace 35 días sin devolver → atrasado / compra tácita
--     Ines PedidoDespachado ....... pedido Despachado → "para entregar"
--     Marcos CargoPendiente ....... cargo por prenda dañada pendiente → se suma a su próximo cobro
-- Historia de 6 meses (analítica y centro de decisiones): 44 clientes más con altas mes a mes y
-- bajas (el plan Básico concentra las bajas del último mes), cobros y renovaciones mensuales,
-- ~40 pedidos de 3 vendedores, mantenimientos cerrados, lista de espera, cargos, historial de
-- pedidos y bitácora de negocio coherentes con los pedidos.
-- Dígitos verificadores: las filas nuevas quedan con DVH = 0. Si la base ya tenía los dígitos
-- calculados (formato 2), las tablas que cambia quedan "sin calcular" con la marca
-- 'DVInicializacionPendiente' = 1: la app las inicializa (y lo asienta en la bitácora) al arrancar
-- o, desde "Restablecer datos de demo", enseguida. En una instalación nueva lo resuelve la sección 22.
-- Idempotente: el procedimiento se recrea en cada corrida; la carga solo corre con la marca.
-- ============================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
IF OBJECT_ID('dbo.RestablecerDatosDemo', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RestablecerDatosDemo;
GO
CREATE PROCEDURE dbo.RestablecerDatosDemo
    @Confirmacion NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;   -- ante cualquier error se revierte todo

    IF @Confirmacion IS NULL OR @Confirmacion <> N'RESTABLECER'
        THROW 50001, N'Para restablecer los datos de demo hay que pasar @Confirmacion = N''RESTABLECER''.', 1;

    DECLARE @ahora DATETIME = GETDATE();
    DECLARE @hoy   DATE     = CAST(@ahora AS DATE);
    DECLARE @base  DATETIME = CAST(@hoy AS DATETIME);   -- hoy a las 00:00

    BEGIN TRANSACTION;

    -- ── 1. Se borran los datos de negocio ──────────────────────────────────────
    -- PN03: las decisiones comerciales sobre las recomendaciones (las de la demo se vuelven a
    -- sembrar en el paso 9b; sin esto, una decisión vieja seguiría asociada a las claves nuevas).
    -- Las metas comerciales de la demo también se vuelven a sembrar (paso 9b2, relativas a hoy); el
    -- detalle de las decisiones (clientes contactados, prendas) se va con ellas.
    IF OBJECT_ID('MetaComercial', 'U') IS NOT NULL
        DELETE FROM MetaComercial;
    IF OBJECT_ID('DecisionComercialDetalle', 'U') IS NOT NULL
        DELETE FROM DecisionComercialDetalle;
    IF OBJECT_ID('DecisionComercial', 'U') IS NOT NULL
        DELETE FROM DecisionComercial;
    DELETE FROM PedidoFaltanteAlternativa;
    DELETE FROM PedidoFaltante;
    DELETE FROM PedidoHistorial;
    DELETE FROM PedidoPrenda;
    DELETE FROM BitacoraNegocio;
    DELETE FROM CargoPrenda;
    DELETE FROM ListaEspera;
    DELETE FROM MantenimientoPrenda;
    DELETE FROM ContratacionIntentoPago;
    DELETE FROM Contratacion;
    DELETE FROM DesistimientoContratacion;
    DELETE FROM HistorialCobro;
    DELETE FROM HistorialRenovacion;
    DELETE FROM PromocionHistorial;
    DELETE FROM DictamenContable;
    DELETE FROM SolicitudBajaPromocion;
    DELETE FROM Promocion;
    DELETE FROM SugerenciaPromocion;
    DELETE FROM Pedido;
    DELETE FROM Prenda;
    UPDATE Cliente SET IdClienteReferente = NULL;
    DELETE FROM Cliente;

    -- ── 2. Empleados: Caja, Administrador y dos vendedores más (sin usuario del sistema) ──
    --    Vendedor (L-001) y Depósito (L-002) vienen de la sección 01.
    INSERT INTO Empleado (Nombre, Apellido, DNI, Email, FechaIngreso, Puesto, Legajo, IdUsuario, DVH)
    SELECT v.Nombre, v.Apellido, v.DNI, v.Email, DATEADD(DAY, -v.DiasIngreso, @base), v.Puesto, v.Legajo,
           (SELECT TOP 1 IdUsuario FROM Usuario u WHERE u.Username = v.Username), 0
    FROM (VALUES
        (N'Carolina', N'Ibáñez',  '34555666', 'caja@wardrobeflow.com',      N'Caja',          'L-003', 'caja',  0),
        (N'Admin',    N'Sistema', '30000001', 'admin@wardrobeflow.com',     N'Administrador', 'L-004', 'admin', 0),
        (N'Martín',   N'Herrera', '32555777', 'mherrera@wardrobeflow.com',  N'Vendedor',      'L-005', NULL,  400),
        (N'Lorena',   N'Sosa',    '36777888', 'lsosa@wardrobeflow.com',     N'Vendedora',     'L-006', NULL,  300)
    ) AS v(Nombre, Apellido, DNI, Email, Puesto, Legajo, Username, DiasIngreso)
    WHERE NOT EXISTS (SELECT 1 FROM Empleado e WHERE e.Legajo = v.Legajo OR e.DNI = v.DNI);

    DECLARE @depo INT = (SELECT TOP 1 IdEmpleado FROM Empleado WHERE Legajo = 'L-002');
    DECLARE @caja INT = (SELECT TOP 1 IdEmpleado FROM Empleado WHERE Legajo = 'L-003');
    -- Vendedores: V1 = vendedor (usuario 'vendedor'), V2 = Martín Herrera, V3 = Lorena Sosa.
    CREATE TABLE #Vend (K CHAR(2) PRIMARY KEY, IdEmpleado INT, Usr NVARCHAR(50));
    INSERT INTO #Vend VALUES
        ('V1', (SELECT TOP 1 IdEmpleado FROM Empleado WHERE Legajo = 'L-001'), N'vendedor'),
        ('V2', (SELECT TOP 1 IdEmpleado FROM Empleado WHERE Legajo = 'L-005'), N'mherrera'),
        ('V3', (SELECT TOP 1 IdEmpleado FROM Empleado WHERE Legajo = 'L-006'), N'lsosa');
    -- Si falta el vendedor o el depósito de la sección 01, los otros cubren (la demo no se corta).
    UPDATE #Vend SET IdEmpleado = (SELECT TOP 1 IdEmpleado FROM #Vend WHERE IdEmpleado IS NOT NULL ORDER BY K) WHERE IdEmpleado IS NULL;
    IF @depo IS NULL SET @depo = (SELECT TOP 1 IdEmpleado FROM #Vend);
    IF @caja IS NULL SET @caja = (SELECT TOP 1 IdEmpleado FROM Empleado WHERE IdEmpleado NOT IN (SELECT IdEmpleado FROM #Vend) ORDER BY IdEmpleado);

    CREATE TABLE #Plan (K CHAR(1) PRIMARY KEY, IdPlan INT, Precio DECIMAL(18,2));
    INSERT INTO #Plan (K, IdPlan, Precio)
    SELECT v.K, p.IdPlan, p.Precio
    FROM (VALUES ('B', N'Básico'), ('E', N'Estándar'), ('P', N'Premium')) AS v(K, Nombre)
    CROSS APPLY (SELECT TOP 1 IdPlan, Precio FROM PlanSuscripcion pl WHERE pl.Nombre = v.Nombre ORDER BY pl.Estado DESC, pl.IdPlan) p;

    -- ── 3. Clientes ────────────────────────────────────────────────────────────
    -- Vence: días hasta el vencimiento (NULL = ciclo mensual desde el alta). Ancla: vencimiento
    -- "viejo" del que salen los cobros (si difiere). Baja: días desde la baja de la suscripción
    -- (queda sin plan). BajaLogica: días desde la baja lógica (Activo = 0).
    CREATE TABLE #Cli (
        DNI NVARCHAR(20) PRIMARY KEY, Nombre NVARCHAR(60), Apellido NVARCHAR(60), Email NVARCHAR(120),
        Medio NVARCHAR(60), PlanK CHAR(1), PlanIniK CHAR(1), DiasCambio INT, DiasAlta INT, FechaNac DATE,
        Vence INT, Ancla INT, Pausa INT, Gracia INT, Baja INT, BajaLogica INT, PlanSigK CHAR(1),
        IdCliente INT, IdPlan INT, Venc DATE, Fin DATE, Orden INT IDENTITY(1,1));
    INSERT INTO #Cli (DNI, Nombre, Apellido, Email, Medio, PlanK, PlanIniK, DiasCambio, DiasAlta, FechaNac,
                      Vence, Ancla, Pausa, Gracia, Baja, BajaLogica, PlanSigK)
    VALUES
    -- PN01
    ('40000001', N'Juan',     N'PedidoFeliz',          'juan.pedidofeliz@demo.com',           'medio.tarjeta_credito', 'P', NULL, NULL, 120, '1990-03-12',   30, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000002', N'Ana',      N'PrendasNoDisponibles', 'ana.prendasnodisponibles@demo.com',   'medio.tarjeta_debito',  'E', NULL, NULL,  90, '1992-07-21',   25, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000003', N'Nicolas',  N'ExcedeCupo',           'nicolas.excedecupo@demo.com',         'medio.tarjeta_debito',  'B', NULL, NULL,  60, '1997-04-14',   20, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000004', N'Sofia',    N'ListaParaFormalizar',  'sofia.listaparaformalizar@demo.com',  'medio.tarjeta_credito', 'P', NULL, NULL,  75, '1994-11-02',   28, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000005', N'Pedro',    N'PedidoActivo',         'pedro.pedidoactivo@demo.com',         'medio.transferencia',   'E', NULL, NULL, 150, '1988-02-09',   15, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000006', N'Lucia',    N'SuscripcionVencida',   'lucia.suscripcionvencida@demo.com',   'medio.tarjeta_credito', 'E', NULL, NULL, 200, '1991-09-30',  -10, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000007', N'Tomas',    N'SuscripcionPausada',   'tomas.suscripcionpausada@demo.com',   'medio.tarjeta_debito',  'B', NULL, NULL, 100, '1995-09-03',   40,   10,   20, NULL, NULL, NULL, NULL),
    ('40000022', N'Valeria',  N'ConFaltantes',         'valeria.confaltantes@demo.com',       'medio.tarjeta_debito',  'E', NULL, NULL, 115, '1993-05-17', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    -- PN02
    ('40000008', N'Laura',    N'SinPlan',              'laura.sinplan@demo.com',              'medio.efectivo',        NULL, NULL, NULL,   3, '1999-12-21', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000009', N'Rocio',    N'ReferidaPorJuan',      'rocio.referidaporjuan@demo.com',      'medio.efectivo',        NULL, NULL, NULL,   1, '2000-06-15', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000010', N'Diego',    N'PagoPendiente',        'diego.pagopendiente@demo.com',        'medio.transferencia',   NULL, NULL, NULL,   2, '1993-08-27', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000011', N'Elena',    N'TercerIntentoFallido', 'elena.tercerintentofallido@demo.com', 'medio.tarjeta_credito', NULL, NULL, NULL,   4, '1996-01-19', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000023', N'Sergio',   N'PagoRechazado',        'sergio.pagorechazado@demo.com',       'medio.tarjeta_credito', NULL, NULL, NULL,   8, '1989-10-05', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000012', N'Martina',  N'PorVencer',            'martina.porvencer@demo.com',          'medio.tarjeta_debito',  'E', NULL, NULL,  85, '1996-02-28',    5, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000013', N'Carla',    N'EnGracia',             'carla.engracia@demo.com',             'medio.efectivo',        'B', NULL, NULL,  60, '1998-08-08',   -2, NULL, NULL,    3, NULL, NULL, NULL),
    ('40000014', N'Raul',     N'Suspendido',           'raul.suspendido@demo.com',            'medio.tarjeta_debito',  'B', NULL, NULL, 100, '1985-12-01',  -12, NULL, NULL,   -7, NULL, NULL, NULL),
    ('40000015', N'Federico', N'CambioProgramado',     'federico.cambioprogramado@demo.com',  'medio.tarjeta_credito', 'P', NULL, NULL,  80, '1987-06-23',   40,   10, NULL, NULL, NULL, NULL, 'E'),
    ('40000016', N'Gustavo',  N'DadoDeBaja',           'gustavo.dadodebaja@demo.com',         'medio.transferencia',   'B', NULL, NULL, 150, '1983-04-11', NULL, NULL, NULL, NULL,   40,   30, NULL),
    -- PN04 / logística
    ('40000017', N'Hernan',   N'DevolucionPendiente',  'hernan.devolucionpendiente@demo.com', 'medio.tarjeta_debito',  'E', NULL, NULL, 110, '1990-10-30', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000018', N'Paula',    N'CompraTacita',         'paula.compratacita@demo.com',         'medio.tarjeta_credito', 'P', NULL, NULL, 130, '1992-01-14', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000019', N'Julia',    N'EnInspeccion',         'julia.eninspeccion@demo.com',         'medio.tarjeta_debito',  'E', NULL, NULL,  95, '1997-07-07', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000020', N'Marcos',   N'CargoPendiente',       'marcos.cargopendiente@demo.com',      'medio.efectivo',        'B', NULL, NULL,  70, '1994-03-03', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('40000021', N'Ines',     N'PedidoDespachado',     'ines.pedidodespachado@demo.com',      'medio.tarjeta_credito', 'P', NULL, NULL,  45, '1991-11-19', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    -- Historia de 6 meses (clientes "reales"): altas mes a mes y bajas de suscripción
    ('30111001', N'Camila',    N'Rodríguez', 'camila.rodriguez@mail.com',   'medio.tarjeta_credito', 'P', NULL, NULL, 185, '1990-05-02', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111002', N'Agustina',  N'López',     'agustina.lopez@mail.com',     'medio.tarjeta_debito',  'E', NULL, NULL, 182, '1988-09-13', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111003', N'Florencia', N'Díaz',      'florencia.diaz@mail.com',     'medio.transferencia',   'E', NULL, NULL, 170, '1993-02-25', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111004', N'Joaquín',   N'Pérez',     'joaquin.perez@mail.com',      'medio.efectivo',        'B', NULL, NULL, 165, '1999-07-19', NULL, NULL, NULL, NULL,  100, NULL, NULL),
    ('30111005', N'Micaela',   N'Fernández', 'micaela.fernandez@mail.com',  'medio.tarjeta_credito', 'P', NULL, NULL, 150, '1986-12-08', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111006', N'Lautaro',   N'Romero',    'lautaro.romero@mail.com',     'medio.tarjeta_debito',  'E', NULL, NULL, 140, '1995-04-30', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111007', N'Victoria',  N'Álvarez',   'victoria.alvarez@mail.com',   'medio.efectivo',        'B', NULL, NULL, 132, '2000-01-22', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111008', N'Santiago',  N'Torres',    'santiago.torres@mail.com',    'medio.transferencia',   'E', NULL, NULL, 125, '1984-06-15', NULL, NULL, NULL, NULL,   70, NULL, NULL),
    ('30111009', N'Julieta',   N'Ruiz',      'julieta.ruiz@mail.com',       'medio.tarjeta_credito', 'P', NULL, NULL, 118, '1992-10-03', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111010', N'Ramiro',    N'Acosta',    'ramiro.acosta@mail.com',      'medio.tarjeta_debito',  'B', NULL, NULL, 105, '1997-03-27', NULL, NULL, NULL, NULL,   25, NULL, NULL),
    ('30111011', N'Luciana',   N'Medina',    'luciana.medina@mail.com',     'medio.tarjeta_credito', 'E', NULL, NULL,  95, '1989-08-11', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111012', N'Facundo',   N'Molina',    'facundo.molina@mail.com',     'medio.efectivo',        'B', NULL, NULL,  90, '1998-11-29', NULL, NULL, NULL, NULL,   12, NULL, NULL),
    ('30111013', N'Abril',     N'Castro',    'abril.castro@mail.com',       'medio.tarjeta_credito', 'P', NULL, NULL,  75, '1996-05-05', NULL, NULL, NULL, NULL,   15, NULL, NULL),
    ('30111014', N'Emilia',    N'Ortiz',     'emilia.ortiz@mail.com',       'medio.tarjeta_debito',  'E',  'B',   35,  65, '1994-09-21', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111015', N'Ignacio',   N'Silva',     'ignacio.silva@mail.com',      'medio.efectivo',        'B', NULL, NULL,  55, '2001-02-14', NULL, NULL, NULL, NULL,    8, NULL, NULL),
    ('30111016', N'Delfina',   N'Rojas',     'delfina.rojas@mail.com',      'medio.transferencia',   'E', NULL, NULL,  50, '1991-07-09', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111017', N'Milagros',  N'Núñez',     'milagros.nunez@mail.com',     'medio.tarjeta_credito', 'P', NULL, NULL,  40, '1993-12-12', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111018', N'Valentín',  N'Ríos',      'valentin.rios@mail.com',      'medio.efectivo',        'B', NULL, NULL,  30, '2002-04-18', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111019', N'Renata',    N'Vega',      'renata.vega@mail.com',        'medio.tarjeta_debito',  'B', NULL, NULL, 170, '1987-01-31', NULL, NULL, NULL, NULL,  120, NULL, NULL),
    ('30111020', N'Gonzalo',   N'Moreno',    'gonzalo.moreno@mail.com',     'medio.transferencia',   'E', NULL, NULL, 100, '1985-10-26', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111021', N'Sol',       N'Gutiérrez', 'sol.gutierrez@mail.com',      'medio.efectivo',        'B', NULL, NULL,  60, '1999-06-06', NULL, NULL, NULL, NULL,   20, NULL, NULL),
    ('30111022', N'Pilar',     N'Ibarra',    'pilar.ibarra@mail.com',       'medio.tarjeta_debito',  'B', NULL, NULL,  20, '1995-08-24', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111023', N'Mateo',     N'Cabrera',   'mateo.cabrera@mail.com',      'medio.tarjeta_credito', 'P', NULL, NULL,  12, '1990-02-02', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('30111024', N'Bianca',    N'Ponce',     'bianca.ponce@mail.com',       'medio.transferencia',   'E', NULL, NULL,   8, '1997-09-15', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    -- PN03 Centro de decisiones (DNI 4100xxxx): plan Básico con muchos clientes en riesgo (vencen en
    -- 15 días o menos y nunca pidieron prendas), clientes antiguos que sostienen el % de bajas del
    -- período en un valor creíble, bajas viejas (antes de la promoción Vencida del Básico: su
    -- resultado medido es bueno → "Mantener" y alimenta la retención histórica del plan) y un cliente
    -- que usa todo su cupo (regla de upgrade de plan). Vence NULL = ciclo mensual desde el alta.
    ('41000001', N'Bruno',     N'UsaTodoElCupo', 'bruno.usatodoelcupo@demo.com', 'medio.tarjeta_debito', 'B', NULL, NULL,  96, '1993-04-08', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000002', N'Carolina',  N'Paz',       'carolina.paz@mail.com',       'medio.efectivo',        'B', NULL, NULL, 108, '1996-03-19', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000003', N'Tobías',    N'Luna',      'tobias.luna@mail.com',        'medio.tarjeta_debito',  'B', NULL, NULL, 117, '1998-10-07', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000004', N'Josefina',  N'Benítez',   'josefina.benitez@mail.com',   'medio.transferencia',   'B', NULL, NULL, 139, '1992-12-30', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000005', N'Thiago',    N'Sánchez',   'thiago.sanchez@mail.com',     'medio.efectivo',        'B', NULL, NULL, 147, '2000-05-11', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000006', N'Catalina',  N'Godoy',     'catalina.godoy@mail.com',     'medio.tarjeta_debito',  'B', NULL, NULL, 166, '1995-01-25', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000007', N'Benjamín',  N'Aguirre',   'benjamin.aguirre@mail.com',   'medio.efectivo',        'B', NULL, NULL, 172, '1989-06-02', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000008', N'Olivia',    N'Peralta',   'olivia.peralta@mail.com',     'medio.tarjeta_credito', 'B', NULL, NULL, 167, '1991-08-16', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000009', N'Felipe',    N'Domínguez', 'felipe.dominguez@mail.com',   'medio.transferencia',   'B', NULL, NULL, 152, '1986-11-04', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000010', N'Emma',      N'Quiroga',   'emma.quiroga@mail.com',       'medio.tarjeta_debito',  'B', NULL, NULL, 124, '1994-02-21', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000011', N'Lucas',     N'Correa',    'lucas.correa@mail.com',       'medio.efectivo',        'B', NULL, NULL, 175, '1997-07-13', NULL, NULL, NULL, NULL,  130, NULL, NULL),
    ('41000012', N'Alma',      N'Ledesma',   'alma.ledesma@mail.com',       'medio.tarjeta_debito',  'B', NULL, NULL, 160, '1999-09-27', NULL, NULL, NULL, NULL,  108, NULL, NULL),
    -- Clientes fundadores de los planes Estándar y Premium (más de 6 meses): dan base al % de bajas del
    -- período anterior (clientes al inicio de ese período).
    ('41000013', N'Ezequiel',  N'Ramos',     'ezequiel.ramos@mail.com',     'medio.tarjeta_credito', 'E', NULL, NULL, 181, '1988-05-14', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000014', N'Morena',    N'Villalba',  'morena.villalba@mail.com',    'medio.transferencia',   'E', NULL, NULL, 184, '1993-03-08', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000015', N'Nahuel',    N'Suárez',    'nahuel.suarez@mail.com',      'medio.tarjeta_debito',  'E', NULL, NULL, 188, '1990-12-01', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000016', N'Agustín',   N'Ferreyra',  'agustin.ferreyra@mail.com',   'medio.efectivo',        'E', NULL, NULL, 193, '1987-07-29', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000017', N'Lara',      N'Méndez',    'lara.mendez@mail.com',        'medio.tarjeta_credito', 'E', NULL, NULL, 199, '1995-10-17', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000018', N'Bautista',  N'Giménez',   'bautista.gimenez@mail.com',   'medio.tarjeta_credito', 'P', NULL, NULL, 182, '1984-09-09', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000019', N'Zoe',       N'Carrizo',   'zoe.carrizo@mail.com',        'medio.transferencia',   'P', NULL, NULL, 187, '1992-06-26', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    ('41000020', N'Ian',       N'Toledo',    'ian.toledo@mail.com',         'medio.tarjeta_credito', 'P', NULL, NULL, 191, '1989-01-12', NULL, NULL, NULL, NULL, NULL, NULL, NULL);

    -- Vencimiento: el indicado o el del ciclo mensual desde el alta; sin plan o con baja, ninguno.
    -- Fin = hasta cuándo se cobró: el vencimiento "viejo" (ancla) o, si se dio de baja, la fecha de baja.
    UPDATE #Cli SET
        Venc = CASE WHEN PlanK IS NULL OR Baja IS NOT NULL THEN NULL
                    WHEN Vence IS NOT NULL THEN DATEADD(DAY, Vence, @hoy)
                    ELSE DATEADD(DAY, 30 - DiasAlta % 30, @hoy) END;
    UPDATE #Cli SET
        Fin = CASE WHEN PlanK IS NULL THEN NULL
                   WHEN Baja IS NOT NULL THEN DATEADD(DAY, -Baja, @hoy)
                   WHEN Ancla IS NOT NULL THEN DATEADD(DAY, Ancla, @hoy)
                   ELSE Venc END;

    INSERT INTO Cliente (Nombre, Apellido, DNI, Email, IdMedioPagoPreferido, IdPlan, FechaAlta, FechaNacimiento,
                         FechaVencimiento, FechaLimiteGracia, FechaPausaHasta, Activo, IdPlanSiguiente, FechaCambioPlan, DVH)
    SELECT c.Nombre, c.Apellido, c.DNI, c.Email,
           (SELECT TOP 1 mp.IdMedioPago FROM MedioPago mp WHERE mp.ClaveTraduccion = c.Medio AND mp.Activo = 1),
           CASE WHEN c.Baja IS NULL THEN p.IdPlan END,
           DATEADD(MINUTE, 10 * 60 + c.Orden * 7, DATEADD(DAY, -c.DiasAlta, @base)),
           c.FechaNac, c.Venc,
           CASE WHEN c.Gracia IS NULL THEN NULL ELSE DATEADD(DAY, c.Gracia, @hoy) END,
           CASE WHEN c.Pausa  IS NULL THEN NULL ELSE DATEADD(DAY, c.Pausa, @ahora) END,
           CASE WHEN c.BajaLogica IS NULL THEN 1 ELSE 0 END,
           ps.IdPlan,
           CASE WHEN c.PlanSigK IS NULL THEN NULL ELSE CAST(c.Fin AS DATETIME) END,
           0
    FROM #Cli c
    LEFT JOIN #Plan p  ON p.K  = c.PlanK
    LEFT JOIN #Plan ps ON ps.K = c.PlanSigK
    ORDER BY c.Orden;

    UPDATE c SET IdCliente = cl.IdCliente, IdPlan = p.IdPlan
    FROM #Cli c JOIN Cliente cl ON cl.DNI = c.DNI LEFT JOIN #Plan p ON p.K = c.PlanK;
    UPDATE Cliente SET IdClienteReferente = (SELECT IdCliente FROM #Cli WHERE DNI = '40000001')
    WHERE DNI = '40000009';   -- Rocío llegó por Juan

    -- ── 4. Prendas ─────────────────────────────────────────────────────────────
    INSERT INTO Prenda (Nombre, Descripcion, Talle, Color, Categoria, Estado, IdClienteActual, IdUltimoCliente, PrecioReposicion, FechaAlta)
    SELECT v.Nombre, v.Descripcion, v.Talle, v.Color, v.Categoria, 0, NULL, NULL, v.Precio, DATEADD(DAY, -v.DiasAlta, @base)
    FROM (VALUES
        (N'Vestido Azul M',    N'Vestido midi de gasa',         'M',  N'Azul',    N'Vestido',  42000.00, 200),
        (N'Vestido Verde M',   N'Vestido midi de gasa',         'M',  N'Verde',   N'Vestido',  42000.00, 200),
        (N'Vestido Negro M',   N'Vestido largo de noche',       'M',  N'Negro',   N'Vestido',  48000.00, 200),
        (N'Vestido Rojo S',    N'Vestido corto de fiesta',      'S',  N'Rojo',    N'Vestido',  45000.00, 200),
        (N'Vestido Coral S',   N'Vestido corto de lino',        'S',  N'Coral',   N'Vestido',  30000.00, 200),
        (N'Vestido Lila S',    N'Vestido cóctel (ingreso nuevo)', 'S', N'Lila',   N'Vestido',  44000.00,  12),
        (N'Falda Plisada S',   N'Falda midi plisada',           'S',  N'Beige',   N'Falda',    20000.00, 200),
        (N'Falda Negra M',     N'Falda lápiz',                  'M',  N'Negro',   N'Falda',    21000.00, 200),
        (N'Saco Negro M',      N'Blazer de vestir',             'M',  N'Negro',   N'Saco',     38000.00, 200),
        (N'Saco Gris M',       N'Blazer de vestir',             'M',  N'Gris',    N'Saco',     38000.00, 200),
        (N'Saco Beige M',      N'Blazer de lino',               'M',  N'Beige',   N'Saco',     36000.00, 200),
        (N'Blazer Azul M',     N'Blazer cruzado (en limpieza)', 'M',  N'Azul',    N'Saco',     38000.00, 200),
        (N'Camisa Blanca S',   N'Camisa de algodón',            'S',  N'Blanco',  N'Camisa',   18000.00, 200),
        (N'Camisa Celeste S',  N'Camisa de algodón',            'S',  N'Celeste', N'Camisa',   18000.00, 200),
        (N'Camisa Rayada S',   N'Camisa de viscosa',            'S',  N'Rayado',  N'Camisa',   19000.00, 200),
        (N'Camisa Negra M',    N'Camisa de seda',               'M',  N'Negro',   N'Camisa',   19000.00,  95),
        (N'Camisa Lino M',     N'Camisa de lino (dada de baja)', 'M', N'Blanco',  N'Camisa',   17000.00, 200),
        (N'Pantalon Negro 40', N'Pantalón sastrero',            '40', N'Negro',   N'Pantalón', 24000.00, 200),
        (N'Pantalon Beige 40', N'Pantalón de lino',             '40', N'Beige',   N'Pantalón', 26000.00, 200),
        (N'Jean Celeste 40',   N'Jean recto',                   '40', N'Celeste', N'Pantalón', 25000.00, 120),
        (N'Tapado Camel L',    N'Tapado de paño',               'L',  N'Camel',   N'Abrigo',   55000.00, 200),
        (N'Campera Cuero L',   N'Campera biker de cuero',       'L',  N'Negro',   N'Abrigo',   60000.00, 200),
        (N'Sweater Gris M',    N'Sweater de lana',              'M',  N'Gris',    N'Sweater',  22000.00, 200),
        (N'Sweater Bordo M',   N'Sweater de lana',              'M',  N'Bordó',   N'Sweater',  22000.00, 200),
        (N'Sweater Crema L',   N'Sweater de lana trenzado',     'L',  N'Crema',   N'Sweater',  23000.00, 200),
        -- PN03: las 5 prendas que tiene Bruno UsaTodoElCupo (todo el cupo del plan Básico → upgrade).
        (N'Blusa Seda S',      N'Blusa de seda',                'S',  N'Marfil',  N'Camisa',   16000.00, 150),
        (N'Jean Negro 38',     N'Jean chupín',                  '38', N'Negro',   N'Pantalón', 24000.00, 150),
        (N'Falda Jean S',      N'Falda corta de jean',          'S',  N'Celeste', N'Falda',    18000.00, 150),
        (N'Cardigan Gris S',   N'Cárdigan de hilo',             'S',  N'Gris',    N'Sweater',  21000.00, 150),
        (N'Vestido Floreado S', N'Vestido camisero estampado',  'S',  N'Floreado', N'Vestido', 35000.00, 150)
    ) AS v(Nombre, Descripcion, Talle, Color, Categoria, Precio, DiasAlta);

    -- ── 5. PN03: sugerencias y promociones en cada estado ─────────────────────────
    -- Gerencia (gcomercial) sugiere, Administración (admcomercial) crea, Contabilidad (contable)
    -- dictamina y Ventas (vendedor) pide la baja: quien crea no dictamina.
    DECLARE @uGer  INT = (SELECT TOP 1 IdUsuario FROM Usuario WHERE Username = 'gcomercial');
    DECLARE @uAdm  INT = (SELECT TOP 1 IdUsuario FROM Usuario WHERE Username = 'admcomercial');
    DECLARE @uCont INT = (SELECT TOP 1 IdUsuario FROM Usuario WHERE Username = 'contable');
    DECLARE @uVend INT = (SELECT TOP 1 IdUsuario FROM Usuario WHERE Username = 'vendedor');
    DECLARE @pB INT = (SELECT IdPlan FROM #Plan WHERE K = 'B');
    DECLARE @pE INT = (SELECT IdPlan FROM #Plan WHERE K = 'E');
    DECLARE @pP INT = (SELECT IdPlan FROM #Plan WHERE K = 'P');

    -- TasaRetencion = 100 en las dos pendientes (beneficio = ingreso en riesgo completo, como las
    -- anteriores a la 20h); la evaluada y la descartada usan el 30 % que propone Gerencia.
    INSERT INTO SugerenciaPromocion (IdPlan, Motivo, TipoDescuentoSugerido, BeneficioEstimado, TasaRetencion, IngresoEnRiesgo,
                                     Estado, FechaAlta, OrigenMetrica, IdUsuarioAlta, MotivoDescarte, FechaEvaluacion)
    SELECT v.IdPlan, v.Motivo, v.Tipo, ROUND(v.Ingreso * v.Tasa / 100, 2), v.Tasa, v.Ingreso, v.Estado,
           DATEADD(DAY, -v.Dias, @ahora), v.Origen, @uGer, v.Descarte,
           CASE WHEN v.DiasEval IS NULL THEN NULL ELSE DATEADD(DAY, -v.DiasEval, @ahora) END
    FROM (VALUES
        (@pB, N'SUGERENCIA PendienteDeEvaluar: el plan Básico tiene la mayor tasa de abandono del último mes; un descuento de retención puede sostenerlo.', 0, 12000.00, 100.00, 0,   3, 0, NULL, NULL),
        (@pP, N'SUGERENCIA PendienteDeEvaluar: el plan Premium suma pocos clientes nuevos; un monto fijo de captación puede atraerlos.',                 1,  5000.00, 100.00, 0,   2, 2, NULL, NULL),
        (@pB, N'SUGERENCIA Evaluada: 5 clientes del plan Básico en riesgo de abandono; se propone un 15 % de retención.',                                0, 40000.00,  30.00, 1, 106, 0, NULL, 103),
        (@pE, N'SUGERENCIA Descartada: baja rotación de sweaters; se propuso un descuento en el plan Estándar.',                                         0, 30000.00,  30.00, 2,  25, 1,
              N'Ya hay una promoción vigente en el plan Estándar; se reevalúa cuando venza.', 20)
    ) AS v(IdPlan, Motivo, Tipo, Ingreso, Tasa, Estado, Dias, Origen, Descarte, DiasEval)
    WHERE v.IdPlan IS NOT NULL AND @uGer IS NOT NULL;
    DECLARE @sugEval INT = (SELECT TOP 1 IdSugerencia FROM SugerenciaPromocion WHERE Estado = 1);

    INSERT INTO Promocion (Nombre, Descripcion, TipoDescuento, Valor, FechaInicio, FechaFin, Estado, IdPlan, MargenEstimado,
                           ImpactoEconomico, IdSugerenciaOrigen, IdUsuarioAlta, FechaAlta)
    SELECT v.Nombre, v.Descripcion, v.TipoDescuento, v.Valor, DATEADD(DAY, v.Inicio, @hoy), DATEADD(DAY, v.Fin, @hoy), v.Estado, v.IdPlan,
           v.Margen, v.Impacto, CASE WHEN v.DeSugerencia = 1 THEN @sugEval END, @uAdm, DATEADD(DAY, -v.DiasAlta, @ahora)
    FROM (VALUES
        (N'PROMO Vigente Estandar -10%',          N'10% en el plan Estándar. Vigente: Ventas puede pedir la baja y se aplica en el cobro.', 0, 10.00,
         -45,  10, 1, @pE, 8000.00, N'Reducción compensada por mayor retención.', 0, 47),
        (N'PROMO ParaDictaminar Premium -15%',    N'15% en el plan Premium. Espera el dictamen de Contabilidad.',                         0, 15.00,
           0,  30, 0, @pP, 6000.00, N'Descuento de captación para nuevos clientes.', 0, 1),
        (N'PROMO Rechazada Basico -20%',          N'20% en el plan Básico. Contabilidad la rechazó: Administración la reformula o la da de baja.', 0, 20.00,
           0,  30, 2, @pB, 3000.00, N'El margen no cubre la reducción.', 0, 5),
        (N'PROMO ConSolicitudBaja Premium $3000', N'$3000 menos en el plan Premium. Vigente con una solicitud de baja de Ventas: Administración la atiende.', 1, 3000.00,
         -20,  40, 1, @pP, 4500.00, N'Impacto bajo: pocos clientes en el plan.', 0, 22),
        (N'PROMO DadaDeBaja Premium -10%',        N'10% en el plan Premium. Administración la dio de baja a pedido de Ventas.', 0, 10.00,
         -75,  15, 4, @pP, 5000.00, N'Se esperaba captar clientes Premium; no tuvo uso.', 0, 78),
        (N'PROMO Vencida Basico -15%',            N'15% de retención en el plan Básico. Llegó a su fecha de fin.', 0, 15.00,
         -97, -41, 6, @pB, 9000.00, N'Retención de clientes Básico en riesgo de abandono.', 1, 103)
    ) AS v(Nombre, Descripcion, TipoDescuento, Valor, Inicio, Fin, Estado, IdPlan, Margen, Impacto, DeSugerencia, DiasAlta)
    WHERE v.IdPlan IS NOT NULL AND @uAdm IS NOT NULL;

    DECLARE @prVig  INT = (SELECT IdPromocion FROM Promocion WHERE Nombre = N'PROMO Vigente Estandar -10%');
    DECLARE @prDict INT = (SELECT IdPromocion FROM Promocion WHERE Nombre = N'PROMO ParaDictaminar Premium -15%');
    DECLARE @prRech INT = (SELECT IdPromocion FROM Promocion WHERE Nombre = N'PROMO Rechazada Basico -20%');
    DECLARE @prBaja INT = (SELECT IdPromocion FROM Promocion WHERE Nombre = N'PROMO ConSolicitudBaja Premium $3000');
    DECLARE @prDDB  INT = (SELECT IdPromocion FROM Promocion WHERE Nombre = N'PROMO DadaDeBaja Premium -10%');
    DECLARE @prVenc INT = (SELECT IdPromocion FROM Promocion WHERE Nombre = N'PROMO Vencida Basico -15%');

    IF @uCont IS NOT NULL
        INSERT INTO DictamenContable (IdPromocion, IdUsuario, Aprobada, Observacion, Fecha)
        SELECT v.IdPromocion, @uCont, v.Aprobada, v.Observacion, DATEADD(DAY, -v.Dias, @ahora)
        FROM (VALUES
            (@prVig,  1, N'Aprobada: el margen estimado cubre la reducción de ingresos.', 46),
            (@prRech, 0, N'Rechazada: un 20% en el plan más barato no cubre el costo.',    4),
            (@prBaja, 1, N'Aprobada: impacto bajo, el plan tiene pocos clientes.',       21),
            (@prDDB,  1, N'Aprobada: el margen de Premium lo permite.',                  76),
            (@prVenc, 1, N'Aprobada: retener clientes Básico cuesta menos que captar nuevos.', 101)
        ) AS v(IdPromocion, Aprobada, Observacion, Dias)
        WHERE v.IdPromocion IS NOT NULL;

    INSERT INTO SolicitudBajaPromocion (IdPromocion, IdUsuarioSolicita, Motivo, FechaSolicitud, Estado,
                                        IdUsuarioResuelve, FechaResolucion, Respuesta, DioDeBaja)
    SELECT v.IdPromocion, @uVend, v.Motivo, DATEADD(DAY, -v.Dias, @ahora), v.Estado,
           CASE WHEN v.Estado = 1 THEN @uAdm END,
           CASE WHEN v.Estado = 1 THEN DATEADD(DAY, -v.DiasRes, @ahora) END, v.Respuesta, v.Estado
    FROM (VALUES
        (@prBaja, N'Los clientes no la usan y confunde en el mostrador.',        1, 0, NULL, NULL),
        (@prDDB,  N'Ningún cliente Premium la pidió en un mes; complica la caja.', 47, 1, 45, N'Se da de baja: no tuvo uso.')
    ) AS v(IdPromocion, Motivo, Dias, Estado, DiasRes, Respuesta)
    WHERE v.IdPromocion IS NOT NULL AND @uVend IS NOT NULL;

    INSERT INTO PromocionHistorial (IdPromocion, EstadoAnterior, EstadoNuevo, IdUsuario, Fecha, Observacion)
    SELECT v.IdPromocion, v.Anterior, v.Nuevo, v.IdUsuario, DATEADD(DAY, -v.Dias, @ahora), v.Observacion
    FROM (VALUES
        (@prVig,  NULL, 0, @uAdm,   47, N'Alta manual'),
        (@prVig,  0,    1, @uCont,  46, N'Aprobada por Contabilidad'),
        (@prDict, NULL, 0, @uAdm,    1, N'Alta manual'),
        (@prRech, NULL, 0, @uAdm,    5, N'Alta manual'),
        (@prRech, 0,    2, @uCont,   4, N'Rechazada por Contabilidad'),
        (@prBaja, NULL, 0, @uAdm,   22, N'Alta manual'),
        (@prBaja, 0,    1, @uCont,  21, N'Aprobada por Contabilidad'),
        (@prDDB,  NULL, 0, @uAdm,   78, N'Alta manual'),
        (@prDDB,  0,    1, @uCont,  76, N'Aprobada por Contabilidad'),
        (@prDDB,  1,    4, @uAdm,   45, N'Baja a pedido de Ventas: no tuvo uso.'),
        (@prVenc, NULL, 0, @uAdm,  103, N'Alta desde una sugerencia de Gerencia'),
        (@prVenc, 0,    1, @uCont, 101, N'Aprobada por Contabilidad'),
        (@prVenc, 1,    6, NULL,    40, N'Llegó la fecha de fin de la promoción.')
    ) AS v(IdPromocion, Anterior, Nuevo, IdUsuario, Dias, Observacion)
    WHERE v.IdPromocion IS NOT NULL;

    -- Ventanas en que cada promoción se aplicó en los cobros (vigente, de su aprobación a su fin o baja).
    CREATE TABLE #PromoVentana (IdPromocion INT, IdPlan INT, Desde DATE, Hasta DATE, Tipo INT, Valor DECIMAL(18,2));
    INSERT INTO #PromoVentana
    SELECT IdPromocion, IdPlan, FechaInicio,
           CASE WHEN IdPromocion = @prDDB THEN DATEADD(DAY, -46, @hoy)
                WHEN FechaFin < @hoy THEN FechaFin ELSE @hoy END,
           TipoDescuento, Valor
    FROM Promocion WHERE Estado IN (1, 4, 6);

    -- ── 6. PN02: contrataciones, cobros y renovaciones de 6 meses ──────────────────
    -- Alta: contratación Pagada (vendedores rotando, cobra Caja). Después, cada mes: renovación
    -- (Renovada) y cobro (Cobrado) hasta el vencimiento vigente o hasta la baja.
    CREATE TABLE #Mov (IdCliente INT, Fecha DATETIME, IdPlan INT, Precio DECIMAL(18,2), IdPromocion INT, Descuento DECIMAL(18,2),
                       IdMedio INT, EsAlta BIT, Vendedor CHAR(2));
    -- Altas
    INSERT INTO #Mov (IdCliente, Fecha, IdPlan, Precio, IdMedio, EsAlta, Vendedor)
    SELECT c.IdCliente, cl.FechaAlta, pi.IdPlan, pi.Precio, cl.IdMedioPagoPreferido, 1,
           CASE c.Orden % 3 WHEN 0 THEN 'V1' WHEN 1 THEN 'V2' ELSE 'V3' END
    FROM #Cli c
    JOIN Cliente cl ON cl.IdCliente = c.IdCliente
    JOIN #Plan pi ON pi.K = ISNULL(c.PlanIniK, c.PlanK);
    -- Renovaciones mensuales: Fin - 30, Fin - 60... mientras sean posteriores al alta y no futuras.
    INSERT INTO #Mov (IdCliente, Fecha, IdPlan, Precio, IdMedio, EsAlta)
    SELECT c.IdCliente,
           CASE WHEN DATEADD(MINUTE, 9 * 60 + 30 + c.Orden, CAST(DATEADD(DAY, -30 * n.j, c.Fin) AS DATETIME)) > @ahora
                THEN DATEADD(MINUTE, -c.Orden, @ahora)
                ELSE DATEADD(MINUTE, 9 * 60 + 30 + c.Orden, CAST(DATEADD(DAY, -30 * n.j, c.Fin) AS DATETIME)) END,
           p.IdPlan, p.Precio, cl.IdMedioPagoPreferido, 0
    FROM #Cli c
    JOIN Cliente cl ON cl.IdCliente = c.IdCliente
    CROSS JOIN (VALUES (1), (2), (3), (4), (5), (6), (7), (8)) AS n(j)
    CROSS APPLY (SELECT CASE WHEN c.PlanIniK IS NOT NULL AND DATEADD(DAY, -30 * n.j, c.Fin) < DATEADD(DAY, -c.DiasCambio, @hoy)
                             THEN c.PlanIniK ELSE c.PlanK END AS K) k
    JOIN #Plan p ON p.K = k.K
    WHERE c.Fin IS NOT NULL
      AND DATEADD(DAY, -30 * n.j, c.Fin) > DATEADD(DAY, -c.DiasAlta, @hoy)
      AND DATEADD(DAY, -30 * n.j, c.Fin) <= @hoy;
    -- Promoción vigente del plan en esa fecha (descuento sobre el precio mensual).
    UPDATE m SET IdPromocion = pv.IdPromocion,
                 Descuento = CASE WHEN pv.Tipo = 0 THEN ROUND(m.Precio * pv.Valor / 100, 2) ELSE pv.Valor END
    FROM #Mov m
    CROSS APPLY (SELECT TOP 1 * FROM #PromoVentana v
                 WHERE v.IdPlan = m.IdPlan AND CAST(m.Fecha AS DATE) BETWEEN v.Desde AND v.Hasta) pv;

    INSERT INTO Contratacion (IdCliente, IdPlan, IdVendedor, IdCaja, Modalidad, Estado, FechaAlta, FechaResolucion,
                              IdMedioPago, NumeroComprobante, FechaComprobante, Importe, DescuentoAplicado, IdPromocion,
                              VigenciaDesde, VigenciaHasta, PrecioMensual)
    SELECT m.IdCliente, m.IdPlan, v.IdEmpleado, @caja, 0, 1, DATEADD(MINUTE, -20, m.Fecha), m.Fecha,
           m.IdMedio, CONVERT(NVARCHAR(36), NEWID()), m.Fecha, m.Precio - ISNULL(m.Descuento, 0), ISNULL(m.Descuento, 0), m.IdPromocion,
           CAST(m.Fecha AS DATE), DATEADD(DAY, 30, CAST(m.Fecha AS DATE)), m.Precio
    FROM #Mov m JOIN #Vend v ON v.K = m.Vendedor
    WHERE m.EsAlta = 1;

    INSERT INTO HistorialCobro (IdCliente, Importe, FechaDeteccion, FechaResolucion, Resultado, Actor, IdMedioPago,
                                NumeroComprobante, Modalidad, DescuentoAplicado, IdPromocion)
    SELECT m.IdCliente, m.Precio - ISNULL(m.Descuento, 0), m.Fecha, m.Fecha, 1, N'caja', m.IdMedio,
           NULL, 0, NULLIF(m.Descuento, 0), m.IdPromocion
    FROM #Mov m WHERE m.EsAlta = 0;

    INSERT INTO HistorialRenovacion (IdCliente, IdPlanAnterior, IdPlanNuevo, FechaDeteccion, FechaResolucion, Resultado, Actor)
    SELECT m.IdCliente, m.IdPlan, m.IdPlan, DATEADD(MINUTE, -30, m.Fecha), DATEADD(MINUTE, -25, m.Fecha), 1, N'vendedor'
    FROM #Mov m WHERE m.EsAlta = 0;

    -- Cambio de plan (Emilia: Básico → Estándar), pausa (Tomás) y bajas de suscripción.
    INSERT INTO HistorialRenovacion (IdCliente, IdPlanAnterior, IdPlanNuevo, FechaDeteccion, FechaResolucion, Resultado, Actor)
    SELECT c.IdCliente, pi.IdPlan, p.IdPlan, DATEADD(HOUR, 11, CAST(DATEADD(DAY, -c.DiasCambio, @hoy) AS DATETIME)),
           DATEADD(HOUR, 11, CAST(DATEADD(DAY, -c.DiasCambio, @hoy) AS DATETIME)), 2, N'admin'
    FROM #Cli c JOIN #Plan pi ON pi.K = c.PlanIniK JOIN #Plan p ON p.K = c.PlanK
    WHERE c.PlanIniK IS NOT NULL;

    INSERT INTO HistorialRenovacion (IdCliente, IdPlanAnterior, IdPlanNuevo, FechaDeteccion, FechaResolucion, Resultado, Actor)
    SELECT c.IdCliente, c.IdPlan, c.IdPlan, DATEADD(DAY, c.Pausa - 30, @ahora), DATEADD(DAY, c.Pausa - 30, @ahora), 4, N'vendedor'
    FROM #Cli c WHERE c.Pausa IS NOT NULL;

    INSERT INTO HistorialRenovacion (IdCliente, IdPlanAnterior, IdPlanNuevo, FechaDeteccion, FechaResolucion, Resultado, Actor)
    SELECT c.IdCliente, p.IdPlan, NULL, DATEADD(HOUR, 12, CAST(c.Fin AS DATETIME)), DATEADD(HOUR, 12, CAST(c.Fin AS DATETIME)), 3, N'vendedor'
    FROM #Cli c JOIN #Plan p ON p.K = c.PlanK
    WHERE c.Baja IS NOT NULL;

    -- Cobros fallidos: Carla en gracia; Raúl, gracia vencida y suspendido.
    INSERT INTO HistorialCobro (IdCliente, Importe, FechaDeteccion, FechaResolucion, Resultado, Actor)
    SELECT c.IdCliente, 0, DATEADD(HOUR, 10, CAST(c.Venc AS DATETIME)), DATEADD(HOUR, 10, CAST(c.Venc AS DATETIME)), 2, N'caja'
    FROM #Cli c WHERE c.Gracia IS NOT NULL
    UNION ALL
    SELECT c.IdCliente, 0, DATEADD(HOUR, 10, CAST(DATEADD(DAY, c.Gracia + 1, @hoy) AS DATETIME)),
           DATEADD(HOUR, 10, CAST(DATEADD(DAY, c.Gracia + 1, @hoy) AS DATETIME)), 3, N'caja'
    FROM #Cli c WHERE c.Gracia < 0;

    UPDATE HistorialCobro
       SET NumeroComprobante = N'CBR-' + RIGHT(N'000000' + CAST(IdCobro AS NVARCHAR(10)), 6) + N'-' + CONVERT(NVARCHAR(8), FechaResolucion, 112)
     WHERE Resultado = 1;

    -- Federico: contrató Estándar hace 3 días con la Premium vigente → cambio programado al vencer.
    INSERT INTO Contratacion (IdCliente, IdPlan, IdVendedor, IdCaja, Modalidad, Estado, FechaAlta, FechaResolucion,
                              IdMedioPago, NumeroComprobante, FechaComprobante, Importe, DescuentoAplicado, IdPromocion,
                              VigenciaDesde, VigenciaHasta, PrecioMensual)
    SELECT c.IdCliente, p.IdPlan, (SELECT IdEmpleado FROM #Vend WHERE K = 'V1'), @caja, 0, 1,
           DATEADD(HOUR, 11, DATEADD(DAY, -3, @base)), DATEADD(HOUR, 12, DATEADD(DAY, -3, @base)),
           cl.IdMedioPagoPreferido, CONVERT(NVARCHAR(36), NEWID()), DATEADD(HOUR, 12, DATEADD(DAY, -3, @base)),
           p.Precio - ROUND(p.Precio * 0.10, 2), ROUND(p.Precio * 0.10, 2), @prVig,
           c.Fin, c.Venc, p.Precio
    FROM #Cli c JOIN Cliente cl ON cl.IdCliente = c.IdCliente JOIN #Plan p ON p.K = c.PlanSigK
    WHERE c.PlanSigK IS NOT NULL;

    -- Pendientes de pago (Diego trimestral con la promo vigente del Estándar; Elena con 2 intentos),
    -- Sergio cancelada por el 3.er intento fallido y Gonzalo con una anulada antes de su alta.
    INSERT INTO Contratacion (IdCliente, IdPlan, IdVendedor, IdCaja, Modalidad, Estado, FechaAlta, FechaResolucion, PrecioMensual, MotivoAnulacion)
    SELECT c.IdCliente, p.IdPlan, (SELECT IdEmpleado FROM #Vend WHERE K = v.Vend), v.Caja, v.Modalidad, v.Estado,
           DATEADD(MINUTE, -v.Minutos, @ahora),
           CASE WHEN v.Estado = 2 THEN DATEADD(MINUTE, -v.Minutos + 120, @ahora) END, p.Precio, v.Motivo
    FROM (VALUES
        ('40000010', 'E', 'V1', 1, 0,     180, NULL, NULL),
        ('40000011', 'B', 'V2', 0, 0,    1440, NULL, NULL),
        ('40000023', 'B', 'V1', 0, 2,    8640, NULL, NULL),
        ('30111020', 'E', 'V3', 0, 2,  145440, @caja, N'El cliente pidió anularla: volvió al día siguiente con otro medio de pago.')
    ) AS v(DNI, PlanK, Vend, Modalidad, Estado, Minutos, Caja, Motivo)
    JOIN #Cli c ON c.DNI = v.DNI JOIN #Plan p ON p.K = v.PlanK;

    UPDATE Contratacion
       SET NumeroComprobante = N'CMP-' + RIGHT(N'000000' + CAST(IdContratacion AS NVARCHAR(10)), 6) + N'-' + CONVERT(NVARCHAR(8), FechaComprobante, 112)
     WHERE Estado = 1;

    DECLARE @mCredito INT = (SELECT TOP 1 IdMedioPago FROM MedioPago WHERE ClaveTraduccion = 'medio.tarjeta_credito' AND Activo = 1);
    DECLARE @mDebito  INT = (SELECT TOP 1 IdMedioPago FROM MedioPago WHERE ClaveTraduccion = 'medio.tarjeta_debito'  AND Activo = 1);
    INSERT INTO ContratacionIntentoPago (IdContratacion, NroIntento, Fecha, IdMedioPago, Motivo, IdCaja)
    SELECT ct.IdContratacion, v.Nro, DATEADD(MINUTE, v.Minutos, ct.FechaAlta), v.Medio, v.Motivo, @caja
    FROM Contratacion ct
    JOIN #Cli c ON c.IdCliente = ct.IdCliente
    JOIN (VALUES ('40000011', 1,  240, @mDebito,  N'Tarjeta rechazada'),
                 ('40000011', 2, 1200, @mDebito,  N'Fondos insuficientes'),
                 ('40000023', 1,   30, @mCredito, N'Tarjeta rechazada'),
                 ('40000023', 2,   60, @mCredito, N'Fondos insuficientes'),
                 ('40000023', 3,  120, @mCredito, N'Tarjeta vencida')) AS v(DNI, Nro, Minutos, Medio, Motivo)
      ON v.DNI = c.DNI
    WHERE ct.Estado IN (0, 2) AND ct.MotivoAnulacion IS NULL;

    INSERT INTO DesistimientoContratacion (IdCliente, IdPlan, Modalidad, Motivo, Fecha, IdVendedor)
    SELECT c.IdCliente, p.IdPlan, CASE WHEN p.IdPlan IS NULL THEN NULL ELSE 0 END, v.Motivo,
           DATEADD(DAY, -v.Dias, @ahora), (SELECT IdEmpleado FROM #Vend WHERE K = v.Vend)
    FROM (VALUES ('40000008', 'E',  1, 'V1', N'Quiere consultarlo en casa antes de contratar.'),
                 ('30111004', NULL, 90, 'V2', N'Por ahora no le interesa volver a suscribirse.'),
                 ('30111019', 'P', 110, 'V3', N'Le pareció caro el plan Premium; lo va a pensar.')) AS v(DNI, PlanK, Dias, Vend, Motivo)
    JOIN #Cli c ON c.DNI = v.DNI LEFT JOIN #Plan p ON p.K = v.PlanK;

    -- ── 7. PN01/PN04: pedidos de 3 vendedores en todos los estados ──────────────────
    -- Horas: antigüedad del pedido. Uso: días que el cliente tuvo las prendas (NULL = todavía las tiene).
    -- Ciclo: control +2 h, separación +3 h, formalización +5 h, despacho +22 h, entrega +53 h,
    -- devolución = entrega + uso. Las prendas de cada pedido no se superponen en el tiempo.
    CREATE TABLE #Ped (Clave VARCHAR(5) PRIMARY KEY, DNI NVARCHAR(20), Vend CHAR(2), Horas INT, Estado INT, Uso INT,
                       Etapa NVARCHAR(20), Motivo NVARCHAR(200), IdPedido INT);
    INSERT INTO #Ped (Clave, DNI, Vend, Horas, Estado, Uso, Etapa, Motivo) VALUES
    ('O01', '30111001', 'V1', 178*24, 2, 12, NULL, NULL), ('O02', '30111002', 'V2', 172*24, 2, 12, NULL, NULL),
    ('O03', '30111003', 'V1', 165*24, 2, 12, NULL, NULL), ('O04', '30111004', 'V3', 158*24, 2, 10, NULL, NULL),
    ('O05', '30111001', 'V1', 148*24, 2, 12, NULL, NULL), ('O06', '30111005', 'V2', 142*24, 2, 12, NULL, NULL),
    ('O07', '30111006', 'V1', 135*24, 2, 14, NULL, NULL), ('O08', '30111007', 'V3', 128*24, 2, 12, NULL, NULL),
    ('O09', '30111002', 'V2', 121*24, 2, 12, NULL, NULL), ('O10', '30111008', 'V1', 117*24, 2, 12, NULL, NULL),
    ('O11', '30111009', 'V2', 112*24, 2, 12, NULL, NULL), ('O12', '30111003', 'V1', 104*24, 2, 12, NULL, NULL),
    ('O13', '30111010', 'V3',  97*24, 2, 12, NULL, NULL), ('O14', '40000005', 'V1',  90*24, 2, 12, NULL, NULL),
    ('O15', '30111011', 'V2',  86*24, 2, 12, NULL, NULL), ('O16', '40000006', 'V1',  80*24, 2, 12, NULL, NULL),
    ('O17', '30111012', 'V2',  76*24, 2, 12, NULL, NULL), ('O18', '30111006', 'V1',  70*24, 2, 12, NULL, NULL),
    ('O19', '30111013', 'V3',  64*24, 2, 12, NULL, NULL), ('O20', '40000001', 'V1',  60*24, 2, 12, NULL, NULL),
    ('O21', '30111014', 'V2',  57*24, 2, 12, NULL, NULL), ('O22', '40000006', 'V1',  50*24, 2, 12, NULL, NULL),
    ('O23', '30111015', 'V3',  45*24, 2, 12, NULL, NULL), ('O24', '30111016', 'V2',  41*24, 2, 12, NULL, NULL),
    ('O25', '40000020', 'V1',  40*24, 2, 23, NULL, NULL), ('O26', '40000018', 'V2',  37*24, 2, NULL, NULL, NULL),
    ('O27', '40000001', 'V1',  30*24, 2, 12, NULL, NULL), ('O28', '30111017', 'V1',  26*24, 2, 12, NULL, NULL),
    ('O29', '40000019', 'V3',  22*24, 2, 19, NULL, NULL), ('O30', '30111018', 'V2',  20*24, 2,  8, NULL, NULL),
    ('O31', '40000017', 'V1',  12*24, 2, NULL, NULL, NULL), ('O32', '40000021', 'V2', 2*24, 1, NULL, NULL, NULL),
    ('O38', '30111007', 'V3',  90*24, 2, 12, NULL, NULL),
    ('O39', '41000001', 'V2',   8*24, 2, NULL, NULL, NULL),   -- PN03: Bruno tiene las 5 prendas de su cupo
    ('O33', '30111019', 'V2', 130*24, 3, NULL, NULL, N'El cliente se mudó al interior antes del despacho.'),
    ('O34', '30111020', 'V3',  95*24, 3, NULL, NULL, N'Cambió de opinión antes del despacho.'),
    ('O35', '30111012', 'V1',  33*24, 3, NULL, NULL, N'Talle equivocado: se rehízo el pedido con otra selección.'),
    ('O36', '30111021', 'V1',  55*24, 7, NULL, N'Cupo', N'No quiso pasar al plan Estándar para llevar 6 prendas.'),
    ('O37', '30111009', 'V2',  18*24, 7, NULL, N'Disponibilidad', N'No le interesaron las alternativas ofrecidas.'),
    -- En curso (uno por cliente)
    ('P01', '40000005', 'V1', 72, 0, NULL, NULL, NULL),   -- Pedro: formalizado, para despachar
    ('P02', '40000004', 'V1',  6, 6, NULL, NULL, NULL),   -- Sofía: separado, para formalizar
    ('P03', '40000002', 'V1',  1, 4, NULL, NULL, NULL),   -- Ana: en control de stock
    ('P04', '40000022', 'V3', 26, 5, NULL, NULL, NULL);   -- Valeria: con faltantes

    -- Limp: días de limpieza tras la devolución (-1 = sigue en limpieza; 0 = dada de baja en la inspección).
    CREATE TABLE #PP (Clave VARCHAR(5), Prenda NVARCHAR(60), Limp INT);
    INSERT INTO #PP (Clave, Prenda, Limp) VALUES
    ('O01', N'Vestido Negro M', 7), ('O01', N'Camisa Blanca S', 2),
    ('O02', N'Saco Gris M', 3),
    ('O03', N'Tapado Camel L', 8), ('O03', N'Pantalon Negro 40', 3),
    ('O04', N'Campera Cuero L', 4), ('O04', N'Vestido Azul M', 3),
    ('O05', N'Vestido Negro M', 6),
    ('O06', N'Saco Gris M', 3), ('O06', N'Saco Negro M', 4),
    ('O07', N'Falda Negra M', 2), ('O07', N'Camisa Celeste S', 2),
    ('O08', N'Saco Beige M', 3),
    ('O09', N'Camisa Blanca S', 2), ('O09', N'Vestido Rojo S', 5), ('O09', N'Falda Plisada S', 3),
    ('O10', N'Vestido Negro M', 8),
    ('O11', N'Saco Gris M', 3), ('O11', N'Blazer Azul M', 4),
    ('O12', N'Pantalon Negro 40', 3), ('O12', N'Vestido Verde M', 4),
    ('O13', N'Jean Celeste 40', 2),
    ('O14', N'Camisa Blanca S', 2),
    ('O15', N'Vestido Negro M', 6), ('O15', N'Camisa Negra M', 2),
    ('O16', N'Tapado Camel L', 8),
    ('O17', N'Saco Gris M', 3), ('O17', N'Pantalon Beige 40', 3),
    ('O18', N'Camisa Celeste S', 2), ('O18', N'Falda Negra M', 2),
    ('O19', N'Saco Negro M', 3),
    ('O20', N'Camisa Blanca S', 2),
    ('O21', N'Camisa Rayada S', 2), ('O21', N'Vestido Negro M', 7),
    ('O22', N'Vestido Azul M', 3),
    ('O23', N'Jean Celeste 40', 2),
    ('O24', N'Saco Gris M', 3),
    ('O25', N'Vestido Coral S', 0),
    ('O26', N'Vestido Verde M', NULL),
    ('O27', N'Tapado Camel L', 8),
    ('O28', N'Vestido Negro M', 6),
    ('O29', N'Blazer Azul M', -1),
    ('O30', N'Camisa Negra M', 2),
    ('O31', N'Pantalon Negro 40', NULL), ('O31', N'Camisa Celeste S', NULL),
    ('O32', N'Saco Beige M', NULL),
    ('O38', N'Camisa Lino M', 0),
    ('O39', N'Blusa Seda S', NULL), ('O39', N'Jean Negro 38', NULL), ('O39', N'Falda Jean S', NULL),
    ('O39', N'Cardigan Gris S', NULL), ('O39', N'Vestido Floreado S', NULL),
    ('O33', N'Campera Cuero L', NULL),
    ('O34', N'Pantalon Beige 40', NULL),
    ('O35', N'Camisa Rayada S', NULL),
    ('O36', N'Vestido Azul M', NULL), ('O36', N'Saco Gris M', NULL), ('O36', N'Camisa Blanca S', NULL),
    ('O36', N'Falda Negra M', NULL), ('O36', N'Pantalon Beige 40', NULL), ('O36', N'Jean Celeste 40', NULL),
    ('O37', N'Vestido Verde M', NULL),
    ('P01', N'Saco Negro M', NULL),
    ('P02', N'Vestido Rojo S', NULL), ('P02', N'Falda Plisada S', NULL),
    ('P03', N'Vestido Azul M', NULL), ('P03', N'Saco Negro M', NULL),
    ('P04', N'Vestido Verde M', NULL), ('P04', N'Camisa Blanca S', NULL);

    DECLARE @k VARCHAR(5), @cli INT, @emp INT, @h INT, @est INT, @uso INT, @etapa NVARCHAR(20), @motivo NVARCHAR(200), @t0 DATETIME;
    DECLARE curPed CURSOR LOCAL FAST_FORWARD FOR
        SELECT p.Clave, c.IdCliente, v.IdEmpleado, p.Horas, p.Estado, p.Uso, p.Etapa, p.Motivo
        FROM #Ped p JOIN #Cli c ON c.DNI = p.DNI JOIN #Vend v ON v.K = p.Vend
        ORDER BY p.Horas DESC;
    OPEN curPed;
    FETCH NEXT FROM curPed INTO @k, @cli, @emp, @h, @est, @uso, @etapa, @motivo;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @t0 = CASE WHEN @h < 48 THEN DATEADD(HOUR, -@h, @ahora)
                       ELSE DATEADD(HOUR, 11, DATEADD(DAY, -(@h / 24), @base)) END;
        INSERT INTO Pedido (IdCliente, IdEmpleado, Estado, FechaPedido, FechaEnvioControl, FechaControl, IdEmpleadoControl,
                            FechaSeparacion, FechaFormalizacion, FechaDespacho, FechaEntrega, FechaDevolucion,
                            MotivoCancelacion, MotivoDesistimiento, EtapaDesistimiento, DVH)
        VALUES (@cli, @emp, @est, @t0,
                CASE WHEN @est = 7 AND @etapa = N'Cupo' THEN NULL ELSE @t0 END,
                CASE WHEN @est IN (0, 1, 2, 3, 5, 6) OR (@est = 7 AND @etapa = N'Disponibilidad') THEN DATEADD(HOUR, 2, @t0) END,
                CASE WHEN @est IN (0, 1, 2, 3, 5, 6) OR (@est = 7 AND @etapa = N'Disponibilidad') THEN @depo END,
                CASE WHEN @est IN (0, 1, 2, 3, 6) THEN DATEADD(HOUR, 3, @t0) END,
                CASE WHEN @est IN (0, 1, 2, 3) THEN DATEADD(HOUR, 5, @t0) END,
                CASE WHEN @est IN (1, 2) THEN DATEADD(HOUR, 22, @t0) END,
                CASE WHEN @est = 2 THEN DATEADD(HOUR, 53, @t0) END,
                CASE WHEN @est = 2 AND @uso IS NOT NULL THEN DATEADD(HOUR, 1, DATEADD(DAY, 2 + @uso, @t0)) END,
                CASE WHEN @est = 3 THEN @motivo END,
                CASE WHEN @est = 7 THEN @motivo END,
                CASE WHEN @est = 7 THEN @etapa END,
                0);
        UPDATE #Ped SET IdPedido = SCOPE_IDENTITY() WHERE Clave = @k;
        FETCH NEXT FROM curPed INTO @k, @cli, @emp, @h, @est, @uso, @etapa, @motivo;
    END
    CLOSE curPed;
    DEALLOCATE curPed;

    INSERT INTO PedidoPrenda (IdPedido, IdPrenda, Confirmada)
    SELECT p.IdPedido, pr.IdPrenda, CASE WHEN p.Estado IN (0, 1, 2, 3, 6) THEN 1 ELSE 0 END
    FROM #PP x JOIN #Ped p ON p.Clave = x.Clave JOIN Prenda pr ON pr.Nombre = x.Prenda;

    -- Faltantes: el "Vestido Verde M" lo tiene Paula (Valeria y el desistimiento de Julieta).
    INSERT INTO PedidoFaltante (IdPedido, IdPrenda, EstadoAlRevisar, ReservadaParaOtro)
    SELECT p.IdPedido, pr.IdPrenda, 1, 0
    FROM #Ped p JOIN Prenda pr ON pr.Nombre = N'Vestido Verde M'
    WHERE p.Clave IN ('P04', 'O37');
    INSERT INTO PedidoFaltanteAlternativa (IdPedido, IdPrenda, IdPrendaAlternativa)
    SELECT p.IdPedido, pr.IdPrenda, alt.IdPrenda
    FROM #Ped p JOIN Prenda pr ON pr.Nombre = N'Vestido Verde M'
    JOIN Prenda alt ON alt.Nombre IN (N'Vestido Azul M', N'Vestido Negro M')
    WHERE p.Clave IN ('P04', 'O37');

    -- Estado actual de cada prenda: en uso por el cliente del pedido en curso (o entregado sin
    -- devolver), en limpieza (Blazer de Julia) o de baja (dañadas, con cargo); el resto disponible.
    UPDATE pr SET Estado = 1, IdClienteActual = ped.IdCliente
    FROM Prenda pr JOIN PedidoPrenda pp ON pp.IdPrenda = pr.IdPrenda JOIN Pedido ped ON ped.IdPedido = pp.IdPedido
    WHERE ped.Estado IN (0, 1, 6) OR (ped.Estado = 2 AND ped.FechaDevolucion IS NULL);
    UPDATE pr SET Estado = CASE WHEN x.Limp = -1 THEN 2 ELSE 3 END, IdClienteActual = NULL
    FROM Prenda pr JOIN #PP x ON x.Prenda = pr.Nombre
    WHERE x.Limp IN (-1, 0);
    UPDATE pr SET IdUltimoCliente = (SELECT TOP 1 ped.IdCliente FROM PedidoPrenda pp JOIN Pedido ped ON ped.IdPedido = pp.IdPedido
                                     WHERE pp.IdPrenda = pr.IdPrenda AND ped.Estado IN (0, 1, 2, 6)
                                     ORDER BY ped.FechaPedido DESC)
    FROM Prenda pr;

    -- Mantenimientos: uno por prenda devuelta (Origen 1) y dos manuales del depósito (Origen 0).
    -- Quedan abiertos el del Blazer (devolución: lo muestra la cola de Inspección de devolución) y
    -- el de la Campera (limpieza manual: sale de limpieza desde Prendas, no por la Inspección).
    INSERT INTO MantenimientoPrenda (IdPrenda, FechaEntrada, FechaSalida, Actor, Origen)
    SELECT pr.IdPrenda, ped.FechaDevolucion,
           CASE WHEN x.Limp = -1 THEN NULL
                WHEN x.Limp = 0 THEN DATEADD(HOUR, 2, ped.FechaDevolucion)
                ELSE DATEADD(HOUR, 3, DATEADD(DAY, x.Limp, ped.FechaDevolucion)) END,
           N'deposito', 1
    FROM #PP x JOIN #Ped p ON p.Clave = x.Clave JOIN Pedido ped ON ped.IdPedido = p.IdPedido
    JOIN Prenda pr ON pr.Nombre = x.Prenda
    WHERE ped.FechaDevolucion IS NOT NULL;
    INSERT INTO MantenimientoPrenda (IdPrenda, FechaEntrada, FechaSalida, Actor, Origen)
    SELECT pr.IdPrenda, DATEADD(HOUR, 10, DATEADD(DAY, -v.Entrada, @base)),
           CASE WHEN v.Salida IS NULL THEN NULL ELSE DATEADD(HOUR, 16, DATEADD(DAY, -v.Salida, @base)) END, N'deposito', 0
    FROM (VALUES (N'Sweater Gris M', 60, 57), (N'Campera Cuero L', 2, NULL)) AS v(Prenda, Entrada, Salida)
    JOIN Prenda pr ON pr.Nombre = v.Prenda;
    UPDATE Prenda SET Estado = 2 WHERE Nombre = N'Campera Cuero L';

    -- Cargos por prenda dañada: Marcos (pendiente: se suma a su próximo cobro) y Victoria (ya cobrado).
    INSERT INTO CargoPrenda (IdPrenda, IdCliente, Motivo, Monto, FechaRegistro, FechaCobro, Actor, Estado)
    SELECT pr.IdPrenda, ped.IdCliente, v.Motivo, pr.PrecioReposicion, DATEADD(HOUR, 2, ped.FechaDevolucion),
           CASE WHEN v.Cobrado = 1 THEN (SELECT MIN(hc.FechaResolucion) FROM HistorialCobro hc
                                         WHERE hc.IdCliente = ped.IdCliente AND hc.Resultado = 1 AND hc.FechaResolucion > ped.FechaDevolucion) END,
           N'deposito', v.Cobrado
    FROM (VALUES ('O25', N'Vestido Coral S', N'Daño irreparable detectado en la inspección de devolución (rotura en la costura lateral).', 0),
                 ('O38', N'Camisa Lino M',   N'Mancha permanente detectada en la inspección de devolución.', 1)) AS v(Clave, Prenda, Motivo, Cobrado)
    JOIN #Ped p ON p.Clave = v.Clave JOIN Pedido ped ON ped.IdPedido = p.IdPedido
    JOIN Prenda pr ON pr.Nombre = v.Prenda;
    -- El cargo cobrado se sumó al importe de ese cobro.
    UPDATE hc SET Importe = hc.Importe + cg.Monto
    FROM HistorialCobro hc JOIN CargoPrenda cg ON cg.IdCliente = hc.IdCliente AND cg.FechaCobro = hc.FechaResolucion
    WHERE cg.Estado = 1;

    -- Lista de espera: pendiente (Martina espera el vestido que tiene Paula), reservada (48 h para
    -- retirarla), convertida en pedido y cancelada por vencer la reserva.
    INSERT INTO ListaEspera (IdPrenda, IdCliente, FechaAlta, Estado, FechaLimiteReserva, FechaResolucion, Actor)
    SELECT pr.IdPrenda, c.IdCliente, DATEADD(DAY, -v.DiasAlta, @ahora), v.Estado,
           CASE WHEN v.Estado = 1 THEN DATEADD(HOUR, 47, @ahora)
                WHEN v.Estado IN (2, 3) THEN DATEADD(DAY, -v.DiasRes, @ahora) END,
           CASE WHEN v.Estado IN (2, 3) THEN DATEADD(DAY, -v.DiasRes, @ahora) END,
           v.Actor
    FROM (VALUES ('40000012', N'Vestido Verde M', 6,  0, NULL, N'vendedor'),
                 ('30111001', N'Camisa Rayada S', 3,  1, NULL, N'vendedor'),
                 ('30111016', N'Saco Gris M',     48, 2, 41,   N'vendedor'),
                 ('30111013', N'Vestido Negro M', 40, 3, 36,   N'sistema')) AS v(DNI, Prenda, DiasAlta, Estado, DiasRes, Actor)
    JOIN #Cli c ON c.DNI = v.DNI JOIN Prenda pr ON pr.Nombre = v.Prenda;

    -- ── 8. Historial de cada pedido (mismo formato que BLL.Pedido) ──────────────────
    CREATE TABLE #Ops (IdPedido INT, Orden INT, Fecha DATETIME, Accion NVARCHAR(50), Campo NVARCHAR(100),
                       Anterior NVARCHAR(1000), Nuevo NVARCHAR(1000), Usr NVARCHAR(50));
    CREATE TABLE #PedInfo (IdPedido INT PRIMARY KEY, Ids NVARCHAR(400), Cant INT, IdsFaltantes NVARCHAR(400), Vendedor NVARCHAR(50),
                           Cliente NVARCHAR(150), IdCliente INT, Etapa NVARCHAR(20), Motivo NVARCHAR(200), Estado INT,
                           FechaPedido DATETIME, FechaEnvioControl DATETIME, FechaControl DATETIME, FechaSeparacion DATETIME,
                           FechaFormalizacion DATETIME, FechaDespacho DATETIME, FechaEntrega DATETIME, FechaDevolucion DATETIME, PlanNombre NVARCHAR(50));
    INSERT INTO #PedInfo
    SELECT ped.IdPedido,
           STUFF((SELECT ',' + CAST(pp.IdPrenda AS NVARCHAR(10)) FROM PedidoPrenda pp WHERE pp.IdPedido = ped.IdPedido
                  ORDER BY pp.IdPrenda FOR XML PATH('')), 1, 1, ''),
           (SELECT COUNT(*) FROM PedidoPrenda pp WHERE pp.IdPedido = ped.IdPedido),
           STUFF((SELECT ',' + CAST(pf.IdPrenda AS NVARCHAR(10)) FROM PedidoFaltante pf WHERE pf.IdPedido = ped.IdPedido
                  ORDER BY pf.IdPrenda FOR XML PATH('')), 1, 1, ''),
           v.Usr, cl.Nombre + N' ' + cl.Apellido, ped.IdCliente, ped.EtapaDesistimiento, ISNULL(ped.MotivoCancelacion, ped.MotivoDesistimiento), ped.Estado,
           ped.FechaPedido, ped.FechaEnvioControl, ped.FechaControl, ped.FechaSeparacion, ped.FechaFormalizacion,
           ped.FechaDespacho, ped.FechaEntrega, ped.FechaDevolucion,
           (SELECT pl.Nombre FROM PlanSuscripcion pl JOIN #Cli c ON c.IdPlan = pl.IdPlan WHERE c.IdCliente = ped.IdCliente)
    FROM Pedido ped
    JOIN Cliente cl ON cl.IdCliente = ped.IdCliente
    JOIN #Vend v ON v.IdEmpleado = ped.IdEmpleado;

    INSERT INTO #Ops (IdPedido, Orden, Fecha, Accion, Campo, Anterior, Nuevo, Usr)
    -- Envío a control de stock (vendedor)
    SELECT IdPedido, 1, FechaEnvioControl, N'ENVIAR_CONTROL', x.Campo, NULL, x.Nuevo, Vendedor
    FROM #PedInfo i
    CROSS APPLY (VALUES (N'Estado', N'EnControlStock'), (N'FechaPedido', CONVERT(NVARCHAR(19), i.FechaPedido, 120)), (N'Prendas', i.Ids)) x(Campo, Nuevo)
    WHERE i.FechaEnvioControl IS NOT NULL
    UNION ALL
    -- Desistimiento por cupo (sin pasar por control)
    SELECT IdPedido, 1, FechaPedido, N'DESISTIR', x.Campo, NULL, x.Nuevo, Vendedor
    FROM #PedInfo i
    CROSS APPLY (VALUES (N'Estado', N'Desistido'), (N'EtapaDesistimiento', i.Etapa), (N'MotivoDesistimiento', i.Motivo), (N'Prendas', i.Ids)) x(Campo, Nuevo)
    WHERE i.Estado = 7 AND i.Etapa = N'Cupo'
    UNION ALL
    -- Informe de faltantes (depósito)
    SELECT IdPedido, 2, FechaControl, N'INFORMAR_FALTANTES', x.Campo, x.Anterior, x.Nuevo, N'deposito'
    FROM #PedInfo i
    CROSS APPLY (VALUES (N'Estado', N'EnControlStock', N'ConFaltantes'), (N'Faltantes', NULL, i.IdsFaltantes)) x(Campo, Anterior, Nuevo)
    WHERE i.IdsFaltantes IS NOT NULL
    UNION ALL
    -- Desistimiento después de los faltantes (vendedor)
    SELECT IdPedido, 3, DATEADD(HOUR, 1, FechaControl), N'DESISTIR', x.Campo, x.Anterior, x.Nuevo, Vendedor
    FROM #PedInfo i
    CROSS APPLY (VALUES (N'Estado', N'ConFaltantes', N'Desistido'), (N'EtapaDesistimiento', NULL, i.Etapa), (N'MotivoDesistimiento', NULL, i.Motivo)) x(Campo, Anterior, Nuevo)
    WHERE i.Estado = 7 AND i.Etapa = N'Disponibilidad'
    UNION ALL
    -- Confirmación y separación (depósito)
    SELECT IdPedido, 2, FechaControl, N'CONFIRMAR_PRENDAS', N'PrendasConfirmadas', NULL, Ids, N'deposito'
    FROM #PedInfo WHERE FechaSeparacion IS NOT NULL
    UNION ALL
    SELECT IdPedido, 3, FechaSeparacion, N'SEPARAR', x.Campo, x.Anterior, x.Nuevo, N'deposito'
    FROM #PedInfo i
    CROSS APPLY (VALUES (N'Estado', N'EnControlStock', N'Separado'), (N'FechaSeparacion', NULL, CONVERT(NVARCHAR(19), i.FechaSeparacion, 120))) x(Campo, Anterior, Nuevo)
    WHERE i.FechaSeparacion IS NOT NULL
    UNION ALL
    -- Formalización (vendedor)
    SELECT IdPedido, 4, FechaFormalizacion, N'FORMALIZAR', x.Campo, x.Anterior, x.Nuevo, Vendedor
    FROM #PedInfo i
    CROSS APPLY (VALUES (N'Estado', N'Separado', N'Pendiente'), (N'FechaFormalizacion', NULL, CONVERT(NVARCHAR(19), i.FechaFormalizacion, 120))) x(Campo, Anterior, Nuevo)
    WHERE i.FechaFormalizacion IS NOT NULL
    UNION ALL
    -- Cancelación (vendedor), al día siguiente de formalizar
    SELECT IdPedido, 5, DATEADD(HOUR, 20, FechaFormalizacion), N'CANCELAR', x.Campo, x.Anterior, x.Nuevo, Vendedor
    FROM #PedInfo i
    CROSS APPLY (VALUES (N'Estado', N'Pendiente', N'Cancelado'), (N'MotivoCancelacion', NULL, i.Motivo)) x(Campo, Anterior, Nuevo)
    WHERE i.Estado = 3
    UNION ALL
    -- Despacho y entrega (operador logístico)
    SELECT IdPedido, 5, FechaDespacho, N'DESPACHAR', x.Campo, x.Anterior, x.Nuevo, N'logistico'
    FROM #PedInfo i
    CROSS APPLY (VALUES (N'Estado', N'Pendiente', N'Despachado'), (N'FechaDespacho', NULL, CONVERT(NVARCHAR(19), i.FechaDespacho, 120))) x(Campo, Anterior, Nuevo)
    WHERE i.FechaDespacho IS NOT NULL
    UNION ALL
    SELECT IdPedido, 6, FechaEntrega, N'ENTREGAR', x.Campo, x.Anterior, x.Nuevo, N'logistico'
    FROM #PedInfo i
    CROSS APPLY (VALUES (N'Estado', N'Despachado', N'Entregado'), (N'FechaEntrega', NULL, CONVERT(NVARCHAR(19), i.FechaEntrega, 120))) x(Campo, Anterior, Nuevo)
    WHERE i.FechaEntrega IS NOT NULL
    UNION ALL
    -- Devolución (depósito): las prendas pasan a limpieza
    SELECT IdPedido, 7, FechaDevolucion, N'DEVOLUCION', x.Campo, x.Anterior, x.Nuevo, N'deposito'
    FROM #PedInfo i
    CROSS APPLY (VALUES (N'Prendas', N'EnUso', N'EnLimpieza (' + CAST(i.Cant AS NVARCHAR(10)) + N' prenda(s))'),
                        (N'FechaDevolucion', NULL, CONVERT(NVARCHAR(19), i.FechaDevolucion, 120))) x(Campo, Anterior, Nuevo)
    WHERE i.FechaDevolucion IS NOT NULL;

    INSERT INTO PedidoHistorial (IdPedido, IdOperacion, Fecha, IdUsuario, NombreUsuario, Accion, Campo, ValorAnterior, ValorNuevo)
    SELECT o.IdPedido, DENSE_RANK() OVER (PARTITION BY o.IdPedido ORDER BY o.Orden), o.Fecha,
           (SELECT TOP 1 IdUsuario FROM Usuario u WHERE u.Username = o.Usr), o.Usr, o.Accion, o.Campo, o.Anterior, o.Nuevo
    FROM #Ops o;

    -- ── 9. Bitácora de negocio coherente con lo anterior ─────────────────────────
    INSERT INTO BitacoraNegocio (Fecha, Tipo, IdUsuario, IdPedido, IdPrenda, IdCliente, Descripcion)
    SELECT e.Fecha, e.Tipo, (SELECT TOP 1 IdUsuario FROM Usuario u WHERE u.Username = e.Usr), e.IdPedido, e.IdPrenda, e.IdCliente, e.Descripcion
    FROM (
        -- Pedidos: un evento por operación
        SELECT o.Fecha, o.Usr, o.IdPedido, CAST(NULL AS INT) AS IdPrenda, i.IdCliente,
               CASE o.Accion WHEN N'ENVIAR_CONTROL' THEN N'EnvioControlStock' WHEN N'INFORMAR_FALTANTES' THEN N'InformeFaltantes'
                             WHEN N'SEPARAR' THEN N'SeparacionPrendas' WHEN N'FORMALIZAR' THEN N'Venta' WHEN N'DESPACHAR' THEN N'Despacho'
                             WHEN N'ENTREGAR' THEN N'Entrega' WHEN N'DEVOLUCION' THEN N'Devolucion' WHEN N'CANCELAR' THEN N'Cancelacion'
                             ELSE N'Desistimiento' END AS Tipo,
               CASE o.Accion
                    WHEN N'ENVIAR_CONTROL'     THEN N'Pedido #' + CAST(o.IdPedido AS NVARCHAR(10)) + N' enviado a control de stock — ' + i.Cliente + N' — ' + CAST(i.Cant AS NVARCHAR(10)) + N' prenda(s)'
                    WHEN N'INFORMAR_FALTANTES' THEN N'Pedido #' + CAST(o.IdPedido AS NVARCHAR(10)) + N': 1 prenda(s) faltante(s) — Vestido Verde M'
                    WHEN N'SEPARAR'            THEN N'Pedido #' + CAST(o.IdPedido AS NVARCHAR(10)) + N': ' + CAST(i.Cant AS NVARCHAR(10)) + N' prenda(s) separadas para ' + i.Cliente
                    WHEN N'FORMALIZAR'         THEN N'Pedido #' + CAST(o.IdPedido AS NVARCHAR(10)) + N' — ' + i.Cliente + N' — ' + CAST(i.Cant AS NVARCHAR(10)) + N' prenda(s) — Plan ' + ISNULL(i.PlanNombre, N'—') + N' — ' + FORMAT(o.Fecha, 'dd/MM/yyyy HH:mm')
                    WHEN N'DESPACHAR'          THEN N'Pedido #' + CAST(o.IdPedido AS NVARCHAR(10)) + N' despachado — Cliente: ' + i.Cliente + N' — ' + CAST(i.Cant AS NVARCHAR(10)) + N' prenda(s)'
                    WHEN N'ENTREGAR'           THEN N'Pedido #' + CAST(o.IdPedido AS NVARCHAR(10)) + N' entregado — Cliente: ' + i.Cliente
                    WHEN N'DEVOLUCION'         THEN N'Devolución pedido #' + CAST(o.IdPedido AS NVARCHAR(10)) + N' — Cliente: ' + i.Cliente + N' — ' + CAST(i.Cant AS NVARCHAR(10)) + N' prenda(s) pasan a EnLimpieza'
                    WHEN N'CANCELAR'           THEN N'Pedido #' + CAST(o.IdPedido AS NVARCHAR(10)) + N' cancelado — Cliente: ' + i.Cliente + N' — Motivo: ' + i.Motivo
                    ELSE N'Pedido #' + CAST(o.IdPedido AS NVARCHAR(10)) + N': el cliente ' + i.Cliente + N' desistió (' + i.Etapa + N') — ' + i.Motivo END AS Descripcion
        FROM (SELECT DISTINCT IdPedido, Orden, Fecha, Accion, Usr FROM #Ops WHERE Accion <> N'CONFIRMAR_PRENDAS') o
        JOIN #PedInfo i ON i.IdPedido = o.IdPedido
        UNION ALL
        -- Altas de clientes
        SELECT cl.FechaAlta, N'vendedor', NULL, NULL, cl.IdCliente, N'AltaCliente',
               N'Alta de cliente ' + cl.Nombre + N' ' + cl.Apellido + N' — DNI ' + cl.DNI
        FROM Cliente cl
        UNION ALL
        -- Baja lógica de Gustavo
        SELECT DATEADD(HOUR, 15, DATEADD(DAY, -c.BajaLogica, @base)), N'admin', NULL, NULL, c.IdCliente, N'BajaCliente',
               N'Baja de cliente ' + c.Nombre + N' ' + c.Apellido + N' — DNI ' + c.DNI
        FROM #Cli c WHERE c.BajaLogica IS NOT NULL
        UNION ALL
        -- Cobros de suscripción
        SELECT hc.FechaResolucion, hc.Actor, NULL, NULL, hc.IdCliente, N'CobroSuscripcion',
               N'Cobro de suscripción ' + hc.NumeroComprobante + N' — ' + cl.Nombre + N' ' + cl.Apellido + N' — $' + FORMAT(hc.Importe, 'N2', 'es-AR')
        FROM HistorialCobro hc JOIN Cliente cl ON cl.IdCliente = hc.IdCliente
        WHERE hc.Resultado = 1
        UNION ALL
        -- Lista de espera
        SELECT le.FechaAlta, N'vendedor', NULL, le.IdPrenda, le.IdCliente, N'ListaEspera',
               cl.Nombre + N' ' + cl.Apellido + N' se anotó en la lista de espera de ''' + pr.Nombre + N''''
        FROM ListaEspera le JOIN Cliente cl ON cl.IdCliente = le.IdCliente JOIN Prenda pr ON pr.IdPrenda = le.IdPrenda
        UNION ALL
        -- Prendas: alta y bajas con cargo
        SELECT pr.FechaAlta, N'deposito', NULL, pr.IdPrenda, NULL, N'AltaPrenda', N'Alta de prenda ''' + pr.Nombre + N''' (' + pr.Categoria + N', talle ' + pr.Talle + N')'
        FROM Prenda pr
        UNION ALL
        SELECT cg.FechaRegistro, N'deposito', NULL, cg.IdPrenda, cg.IdCliente, N'CambioEstadoPrenda',
               N'''' + pr.Nombre + N''' dada de baja en la inspección — cargo de $' + FORMAT(cg.Monto, 'N2', 'es-AR') + N' a ' + cl.Nombre + N' ' + cl.Apellido
        FROM CargoPrenda cg JOIN Prenda pr ON pr.IdPrenda = cg.IdPrenda JOIN Cliente cl ON cl.IdCliente = cg.IdCliente
    ) e;

    -- ── 9b. PN03: decisiones comerciales del Centro de decisiones ─────────────────────
    -- ClaveRecomendacion con el formato de las reglas (BLL.Inteligencia.Reglas: Codigo[:Id[:Caso]]),
    -- así se asocian a las recomendaciones del análisis por defecto (últimos 90 días):
    --   · Ejecutada hace 107 días: la recomendación de bajas del plan Básico que originó la sugerencia
    --     evaluada → promoción Vencida. Estimado contra el resultado real cargado al cerrar la promoción
    --     (225.360 = lo que mide "Medir resultado" para esa promoción: 17 clientes en juego, +33,3 pp de
    --     retención × $ 8.000 × 6 meses − $ 46.800 descontados): se ve "estimado vs real" en el
    --     Historial. Es anterior al período analizado, así que la
    --     recomendación de hoy vuelve a poder decidirse (BLL DecisionesComerciales.DecisionVigente).
    --   · Pospuesta hasta dentro de 5 días: la recomendación queda oculta hasta esa fecha
    --     ("Mostrar ocultas" la muestra).
    --   · Descartada hace 45 días (pasado el silencio de 30): la liquidación de las prendas sin
    --     rotación se muestra de nuevo para decidir, con el descarte anterior como dato.
    IF OBJECT_ID('DecisionComercial', 'U') IS NOT NULL AND @uGer IS NOT NULL
    BEGIN
        INSERT INTO DecisionComercial (Fecha, IdUsuario, TipoRegla, ClaveRecomendacion, Prioridad, Titulo, Diagnostico,
                                       AccionElegida, EsAlternativa, ImpactoEstimado, Estado, Motivo, TipoReferencia, Referencia,
                                       PospuestaHasta, FechaEjecucion, FechaEvaluacion, ResultadoReal)
        SELECT DATEADD(DAY, -v.Dias, @ahora), @uGer, v.TipoRegla, v.Clave, v.Prioridad, v.Titulo, v.Diagnostico,
               v.Accion, 0, v.Impacto, v.Estado, v.Motivo, v.TipoRef, v.Ref,
               CASE WHEN v.PospHasta IS NULL THEN NULL ELSE DATEADD(DAY, v.PospHasta, @hoy) END,
               CASE WHEN v.Estado = 4 THEN DATEADD(DAY, -v.Dias, @ahora) END,   -- la Ejecutada: se ejecutó al aceptarla
               CASE WHEN v.DiasEval IS NULL THEN NULL ELSE DATEADD(DAY, -v.DiasEval, @ahora) END, v.Real
        FROM (VALUES
            (N'ChurnPlan', N'ChurnPlan:' + CAST(@pB AS NVARCHAR(10)), 0,
             N'Bajas altas en el plan Básico: actuar para retener a los clientes en riesgo',
             N'El plan Básico tenía el % de bajas más alto de los planes y 5 cliente(s) en riesgo de abandono.',
             N'Lanzar una promoción de retención en el plan Básico con 15 % de descuento durante 60 días',
             12000.00, 4, CAST(NULL AS NVARCHAR(500)), N'Sugerencia', @sugEval, CAST(NULL AS INT), 107, 38, CAST(225360.00 AS DECIMAL(14,2))),
            (N'Renovaciones', N'Renovaciones', 1,
             N'Gestionar la renovación de los clientes con la suscripción por vencer o vencida',
             N'Clientes con la suscripción por vencer o vencida hace poco: conviene gestionar el cobro antes de perderlos.',
             N'Campaña de renovación: exportar los contactos de los clientes para llamarlos o escribirles antes de perderlos',
             0.00, 3, N'Se retoma después del cierre de mes.', NULL, NULL, 5, 2, NULL, NULL),
            (N'BajaRotacion', N'BajaRotacion', 1,
             N'Liquidar o dar de baja las prendas sin rotación',
             N'Prendas disponibles sin pedidos en el período y con más de 60 días en el catálogo inmovilizaban capital.',
             N'Dar de baja las prendas para liquidarlas (pasan de Disponible a Baja)',
             0.00, 2, N'Se esperaba el cambio de temporada antes de liquidar.', NULL, NULL, NULL, 45, NULL, NULL)
        ) AS v(TipoRegla, Clave, Prioridad, Titulo, Diagnostico, Accion, Impacto, Estado, Motivo, TipoRef, Ref, PospHasta, Dias, DiasEval, Real)
        WHERE v.Clave IS NOT NULL AND (v.TipoRef IS NULL OR v.Ref IS NOT NULL);

        --   · Ejecutada hace 20 días: la campaña de renovación anterior (opción "exportar contactos").
        --     DecisionComercialDetalle guarda a quiénes se contactó: hasta 4 que ya renovaron después de la
        --     campaña y hasta 2 que vencen en los próximos días. Su ventana de medición (30 días) sigue abierta:
        --     el Historial la muestra "en medición" con cuántos respondieron y cuándo cierra.
        CREATE TABLE #Contactados (IdCliente INT PRIMARY KEY, Precio DECIMAL(18,2));
        INSERT INTO #Contactados (IdCliente, Precio)
        SELECT TOP 4 c.IdCliente, p.Precio
        FROM #Cli c JOIN #Plan p ON p.K = c.PlanK
        WHERE c.Baja IS NULL AND c.DNI LIKE '30111%'
          AND EXISTS (SELECT 1 FROM HistorialRenovacion h WHERE h.IdCliente = c.IdCliente AND h.Resultado = 1
                      AND h.FechaResolucion >= DATEADD(DAY, -19, @base) AND h.FechaResolucion < @base)
        ORDER BY c.Orden;
        INSERT INTO #Contactados (IdCliente, Precio)
        SELECT TOP 2 c.IdCliente, p.Precio
        FROM #Cli c JOIN #Plan p ON p.K = c.PlanK
        WHERE c.Baja IS NULL AND c.Pausa IS NULL AND c.Gracia IS NULL AND c.DNI LIKE '30111%'
          AND c.Venc BETWEEN DATEADD(DAY, 1, @hoy) AND DATEADD(DAY, 9, @hoy)
          AND c.IdCliente NOT IN (SELECT IdCliente FROM #Contactados)
          AND NOT EXISTS (SELECT 1 FROM HistorialRenovacion h WHERE h.IdCliente = c.IdCliente
                          AND h.FechaResolucion >= DATEADD(DAY, -20, @base))
        ORDER BY c.Venc, c.Orden;
        IF EXISTS (SELECT 1 FROM #Contactados)
        BEGIN
            DECLARE @decCampania INT, @cantContactados INT = (SELECT COUNT(*) FROM #Contactados);
            INSERT INTO DecisionComercial (Fecha, IdUsuario, TipoRegla, ClaveRecomendacion, Prioridad, Titulo, Diagnostico,
                                           AccionElegida, EsAlternativa, ImpactoEstimado, Estado, Motivo, TipoReferencia, Referencia,
                                           PospuestaHasta, FechaEjecucion, FechaEvaluacion, ResultadoReal)
            SELECT DATEADD(HOUR, 10, DATEADD(DAY, -20, @base)), @uGer, N'Renovaciones', N'Renovaciones', 2,
                   N'Gestionar la renovación de ' + CAST(@cantContactados AS NVARCHAR(10)) + N' cliente(s)',
                   N'Suscripciones por vencer en los próximos 7 días: conviene gestionar el cobro antes de perderlos.',
                   N'Campaña de renovación: exportar los contactos de los ' + CAST(@cantContactados AS NVARCHAR(10)) +
                   N' cliente(s) para llamarlos o escribirles antes de perderlos',
                   0, ROUND(SUM(Precio) * 0.60, 2), 4, NULL, NULL, NULL, NULL,
                   DATEADD(MINUTE, 5, DATEADD(HOUR, 10, DATEADD(DAY, -20, @base))), NULL, NULL
            FROM #Contactados;
            SET @decCampania = SCOPE_IDENTITY();
            INSERT INTO DecisionComercialDetalle (IdDecision, Tipo, IdObjeto)
            SELECT @decCampania, N'Cliente', IdCliente FROM #Contactados;
        END
    END

    -- ── 9b2. PN03: metas comerciales (relativas a hoy) ─────────────────────────────
    -- Para que la pestaña Metas, el semáforo y los avisos al iniciar sesión tengan qué mostrar el día
    -- que se instala o restablece la demo, sin depender del día del mes:
    --   · MRR del mes en curso: la mitad del ingreso de los clientes activos con plan → Cumplida.
    --   · Ocupación del stock del mes en curso: la de hoy + 15 pp (tope 100) → En riesgo (es una foto del
    --     día). Para quien puede ejecutarlas (patente de edición de stock), sube la prioridad de dar de baja
    --     las prendas sin rotación o con mantenimiento excesivo (menos prendas activas) y la del upgrade de
    --     plan (más prendas en uso). Mandar prendas a revisión no cambia la ocupación (siguen activas).
    --   · % de bajas del mes en curso: máximo 30 % (se ve el ritmo del mes y su proyección).
    --   · MRR del mes anterior: 1,5 veces el ingreso actual → No cumplida (mes cerrado).
    --   · % de bajas del mes anterior: máximo 15 % → Cumplida (mes cerrado; la demo nunca pierde más del 10 % en un mes).
    --   · MRR de hace dos meses: 40 % del ingreso actual → Cumplida (historia del gráfico).
    IF OBJECT_ID('MetaComercial', 'U') IS NOT NULL AND @uGer IS NOT NULL
    BEGIN
        DECLARE @mesActual DATE = DATEFROMPARTS(YEAR(@hoy), MONTH(@hoy), 1);
        DECLARE @mrrBase DECIMAL(14,2) = (SELECT ISNULL(SUM(p.Precio), 0) FROM Cliente c
                                          JOIN PlanSuscripcion p ON p.IdPlan = c.IdPlan WHERE c.Activo = 1);
        DECLARE @ocupHoy DECIMAL(14,2) = (SELECT CASE WHEN COUNT(*) = 0 THEN 0
                                                      ELSE 100.0 * SUM(CASE WHEN Estado = 1 THEN 1 ELSE 0 END) / COUNT(*) END
                                          FROM Prenda WHERE Estado <> 3);
        IF @mrrBase > 0
            INSERT INTO MetaComercial (Periodo, Indicador, ValorObjetivo, Sentido, IdUsuario, Fecha)
            SELECT v.Periodo, v.Indicador, v.Valor, v.Sentido, @uGer, DATEADD(DAY, -v.Dias, @ahora)
            FROM (VALUES
                (@mesActual,                     0, ROUND(@mrrBase * 0.5, -3), 0, 2),
                (@mesActual,                     2, CASE WHEN @ocupHoy + 15 > 100 THEN 100 ELSE ROUND(@ocupHoy + 15, 0) END, 0, 2),
                (@mesActual,                     1, CAST(30 AS DECIMAL(14,2)), 1, 2),
                (DATEADD(MONTH, -1, @mesActual), 0, ROUND(@mrrBase * 1.5, -3), 0, 35),
                (DATEADD(MONTH, -1, @mesActual), 1, CAST(15 AS DECIMAL(14,2)), 1, 35),
                (DATEADD(MONTH, -2, @mesActual), 0, ROUND(@mrrBase * 0.4, -3), 0, 65)
            ) AS v(Periodo, Indicador, Valor, Sentido, Dias)
            WHERE v.Valor > 0;
    END

    -- ── 9c. Versiones (Memento) del usuario 'vendedor' ─────────────────────────────
    -- Para mostrar la exportación/importación a XML/JSON y la restauración sin editar antes un usuario:
    -- una versión inicial con el email anterior y otra con el cambio al email actual (fechas
    -- relativas a hoy). Se recrean solo si el vendedor no tiene otras versiones que estas dos (no se
    -- pisa un historial real). HistorialUsuario no lleva dígito verificador.
    IF OBJECT_ID('HistorialUsuario', 'U') IS NOT NULL AND @uVend IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM HistorialUsuario WHERE IdUsuario = @uVend
                       AND Detalle NOT IN (N'Estado administrativo inicial.', N'Modificación de usuario: email actualizado.'))
    BEGIN
        DELETE FROM HistorialUsuario WHERE IdUsuario = @uVend;
        INSERT INTO HistorialUsuario (IdUsuario, Fecha, Actor, Detalle, UsernameSnap, NombreSnap, ApellidoSnap,
                                      FechaNacSnap, EmailSnap, ClaveSnap, EstadoSnap, IntentosSnap)
        SELECT u.IdUsuario, DATEADD(DAY, -60, @ahora), N'admin', N'Estado administrativo inicial.',
               u.Username, u.Nombre, u.Apellido, u.FechaNacimiento, N'vmorana.ventas@wardrobeflow.com', N'', 1, 0
        FROM Usuario u WHERE u.IdUsuario = @uVend
        UNION ALL
        SELECT u.IdUsuario, DATEADD(DAY, -20, @ahora), N'admin', N'Modificación de usuario: email actualizado.',
               u.Username, u.Nombre, u.Apellido, u.FechaNacimiento, u.Email, N'', 1, 0
        FROM Usuario u WHERE u.IdUsuario = @uVend;
    END

    -- ── 9d. Bitácora del sistema: un mes de actividad de ejemplo ──────────────────
    -- Para que la Bitácora del Sistema y el Reporte de Jornada muestren datos desde la instalación:
    -- ingresos y salidas de cada rol, intentos fallidos con bloqueo y desbloqueo, el cambio de email
    -- del vendedor (el mismo de su versión en 9c), backups y los errores inesperados que la app deja
    -- como ejemplo en su registro serializado (Servicios.Serializacion.RegistroErrores.SembrarEjemplos).
    -- Fechas relativas a hoy, con el mismo formato de detalle que escribe Servicios.Bitacora. SOLO si
    -- la bitácora está vacía (instalación nueva): restablecer la demo nunca toca la auditoría real.
    -- Bitacora no lleva dígito verificador.
    IF OBJECT_ID('Bitacora', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Bitacora)
    BEGIN
        INSERT INTO Bitacora (fecha, usuario, modulo, actividad, detalle, criticidad, ip)
        SELECT f.Fecha, u.IdUsuario, v.Modulo, v.Actividad,
               COALESCE(v.Detalle,
                   CONCAT(N'Usuario ''', u.Username, N''' (ID: ', u.IdUsuario, N') realizó ''', v.Actividad,
                          N''' en ''', v.Modulo, N''' [Criticidad: ',
                          CASE v.Criticidad WHEN 0 THEN N'None' WHEN 1 THEN N'Baja' WHEN 2 THEN N'Media'
                                            WHEN 3 THEN N'Alta' WHEN 4 THEN N'IntentosLogin'
                                            WHEN 5 THEN N'RecuperacionClave' ELSE N'BloqueosCuenta' END,
                          N'] desde ', v.Ip, N' a las ', CONVERT(VARCHAR(8), f.Fecha, 108), N'.')),
               v.Criticidad, v.Ip
        FROM (VALUES
            (30,  540, 'admin',       N'WardrobeFlow',          N'Inicio Sesion',                    0, N'192.168.0.10', NULL),
            (30,  548, 'admin',       N'Backup y Restauración', N'Backup cifrado generado: ''backup_manual.wfbak''', 2, N'192.168.0.10', NULL),
            (30,  560, 'admin',       N'WardrobeFlow',          N'Cierre Sesion',                    0, N'192.168.0.10', NULL),
            (29,  545, 'vendedor',    N'Login',                 N'Intento Fallido Login',            4, N'192.168.0.21',
                N'Intento fallido #1/3 del usuario ''vendedor'': contraseña incorrecta.'),
            (29,  547, 'vendedor',    N'WardrobeFlow',          N'Inicio Sesion',                    0, N'192.168.0.21', NULL),
            (29, 1080, 'vendedor',    N'WardrobeFlow',          N'Cierre Sesion',                    0, N'192.168.0.21', NULL),
            (26,  600, 'deposito',    N'WardrobeFlow',          N'Inicio Sesion',                    0, N'192.168.0.31', NULL),
            (26,  612, 'deposito',    N'ControlStockForm',      N'Excepción: SqlException',          3, N'192.168.0.31',
                N'Usuario ''deposito'' - ControlStockForm: Se agotó el tiempo de espera de la conexión con el servidor SQL al consultar el stock. | Causa: Win32Exception: Tiempo de espera de la operación agotado'),
            (26, 1050, 'deposito',    N'WardrobeFlow',          N'Cierre Sesion',                    0, N'192.168.0.31', NULL),
            (20,  615, 'admin',       N'WardrobeFlow',          N'Inicio Sesion',                    0, N'192.168.0.10', NULL),
            (20,  622, 'admin',       N'Administración de Usuarios', N'Modificación de Usuario',     2, N'192.168.0.10',
                N'Usuario ''vendedor'': Email: ''vmorana.ventas@wardrobeflow.com'' → email actual.'),
            (18, 1115, 'admin',       N'WardrobeFlow',          N'Inicio Sesion',                    0, N'192.168.0.10', NULL),
            (18, 1120, 'admin',       N'BackupForm',            N'Excepción: IOException',           3, N'192.168.0.10',
                N'Usuario ''admin'' - BackupForm: No se pudo escribir el archivo de backup: no hay espacio suficiente en el disco.'),
            (14,  530, 'logistico',   N'Login',                 N'Intento Fallido Login',            4, N'192.168.0.41',
                N'Intento fallido #1/3 del usuario ''logistico'': contraseña incorrecta.'),
            (14,  531, 'logistico',   N'Login',                 N'Intento Fallido Login',            4, N'192.168.0.41',
                N'Intento fallido #2/3 del usuario ''logistico'': contraseña incorrecta.'),
            (14,  532, 'logistico',   N'Login',                 N'Intento Fallido Login',            4, N'192.168.0.41',
                N'Intento fallido #3/3 del usuario ''logistico'': contraseña incorrecta.'),
            (14,  532, 'logistico',   N'Login',                 N'Bloqueo de Cuenta',                6, N'192.168.0.41',
                N'La cuenta ''logistico'' fue bloqueada automáticamente tras 3 intentos fallidos.'),
            (14,  560, 'admin',       N'Administración de Usuarios', N'Desbloqueo de Cuenta: ''logistico''', 3, N'192.168.0.10', NULL),
            (14,  575, 'logistico',   N'WardrobeFlow',          N'Inicio Sesion',                    0, N'192.168.0.41', NULL),
            (11,  660, 'vendedor',    N'WardrobeFlow',          N'Inicio Sesion',                    0, N'192.168.0.21', NULL),
            (11,  665, 'vendedor',    N'PedidosVenta',          N'Excepción: InvalidOperationException', 3, N'192.168.0.21',
                N'Usuario ''vendedor'' - PedidosVenta: La prenda seleccionada dejó de estar disponible mientras se armaba el pedido.'),
            (10,  600, 'auditor',     N'WardrobeFlow',          N'Inicio Sesion',                    0, N'192.168.0.50', NULL),
            (10,  640, 'auditor',     N'WardrobeFlow',          N'Cierre Sesion',                    0, N'192.168.0.50', NULL),
            ( 7,  555, 'ginventario', N'WardrobeFlow',          N'Inicio Sesion',                    0, N'192.168.0.32', NULL),
            ( 6,  565, 'gcomercial',  N'WardrobeFlow',          N'Inicio Sesion',                    0, N'192.168.0.22', NULL),
            ( 6,  571, 'gcomercial',  N'CentroDecisionesForm',  N'Excepción: SqlException',          3, N'192.168.0.22',
                N'Usuario ''gcomercial'' - CentroDecisionesForm: Error de red al ejecutar la consulta del análisis comercial. | Causa: Win32Exception: Se forzó la interrupción de una conexión existente por el host remoto'),
            ( 5,  541, 'admin',       N'Backup automático',     N'Backup automático generado: ''auto_lunes.bak'' (origen: Login)', 2, N'192.168.0.10', NULL),
            ( 3, 1202, 'admin',       N'Aplicación',            N'Excepción: UnauthorizedAccessException', 3, N'192.168.0.10',
                N'Usuario ''admin'' - Aplicación: Acceso denegado a la ruta de la carpeta de backups.'),
            ( 2,  600, 'caja',        N'WardrobeFlow',          N'Inicio Sesion',                    0, N'192.168.0.60', NULL),
            ( 1,  590, 'vendedor',    N'Mi Perfil',             N'Cambio de Contrasena Propia',      2, N'192.168.0.21', NULL),
            ( 1, 1008, 'caja',        N'ContratacionesPendientesForm', N'Excepción: FormatException', 3, N'192.168.0.60',
                N'Usuario ''caja'' - ContratacionesPendientesForm: El importe ingresado no tiene un formato numérico válido.'),
            ( 1, 1080, 'caja',        N'WardrobeFlow',          N'Cierre Sesion',                    0, N'192.168.0.60', NULL)
        ) AS v(Dias, Minuto, Username, Modulo, Actividad, Criticidad, Ip, Detalle)
        JOIN Usuario u ON u.Username = v.Username
        CROSS APPLY (SELECT DATEADD(MINUTE, v.Minuto, CAST(CAST(DATEADD(DAY, -v.Dias, @ahora) AS DATE) AS DATETIME)) AS Fecha) f;
    END

    -- ── 10. Marcas: demo aplicada y dígitos verificadores pendientes ─────────────────
    DELETE FROM ParametroSistema WHERE Clave IN (N'SeedDemo21', N'ResetDatosDemo', N'DemoCargaPendiente');
    INSERT INTO ParametroSistema (Clave, Valor, Fecha) VALUES (N'SeedDemo21', N'Aplicada', GETDATE());
    -- Con los dígitos ya calculados alguna vez (formato 2), las tablas que cambió este procedimiento
    -- quedan "sin calcular" (DVH = 0 y sin DVV) y se pide la inicialización a la app: Usuario y
    -- PermisoRelacion no se tocan y se siguen verificando normalmente.
    IF EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'FormatoDV' AND TRY_CONVERT(INT, Valor) >= 2)
    BEGIN
        UPDATE Empleado SET DVH = 0;
        DELETE FROM DVVertical WHERE NombreTabla IN ('Cliente', 'Empleado', 'Pedido', 'Contratacion');
        MERGE ParametroSistema AS t
        USING (VALUES (N'DVInicializacionPendiente', N'1')) AS s(Clave, Valor) ON t.Clave = s.Clave
        WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
        WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
    END

    COMMIT TRANSACTION;
END
GO

IF EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'DemoCargaPendiente' AND Valor = N'1')
BEGIN
    EXEC dbo.RestablecerDatosDemo @Confirmacion = N'RESTABLECER';
    PRINT 'Datos de demo (sección 21z3b) cargados: escenarios de PN01–PN04 y 6 meses de historia.';
END
GO

-- Las 2 versiones (Memento) del usuario 'vendedor' de la demo se cargan dentro de
-- dbo.RestablecerDatosDemo (paso 9c): solo en una instalación nueva o al restablecer la demo, no
-- en cualquier base donde se corra el script.

-- ============================================================
-- WardrobeFlow — 21z4. TEXTOS DE FÁBRICA CORREGIDOS (terminología EN, montos {n:C2}, "En limpieza")
-- ------------------------------------------------------------
-- La carga de traducciones.tsv a la base es solo-inserción (DAL.Traduccion.InsertarSiNoExiste):
-- una base ya instalada no ve los textos corregidos. Acá se actualizan SOLO las filas cuyo texto
-- sigue siendo el de fábrica anterior; si el Administrador lo editó en Idiomas, no se pisa.
-- Terminología EN: Customer (no Client), Salesperson, Deactivate para la baja lógica de clientes,
-- Role (no Profile) en permisos, Dashboard. Montos con "$" fijo pasan a {n:C2}. Idempotente.
-- ============================================================
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @TextosFabrica TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                                  TextoAnterior NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @TextosFabrica (Codigo, Clave, TextoAnterior, TextoNuevo) VALUES
    (N'ES', N'msg.ped.devolucion', N'Devolución registrada — {0} prenda(s) pasan a EnLimpieza.', N'Devolución registrada — {0} prenda(s) pasan a En limpieza.'),
    (N'ES', N'conf.devolucion.body', N'¿Registrar devolución del Pedido #{0}?' + NCHAR(10) + N'' + NCHAR(10) + N'Cliente: {1}' + NCHAR(10) + N'Prendas: {2}' + NCHAR(10) + N'' + NCHAR(10) + N'Las prendas pasarán a estado EnLimpieza.', N'¿Registrar devolución del Pedido #{0}?' + NCHAR(10) + N'' + NCHAR(10) + N'Cliente: {1}' + NCHAR(10) + N'Prendas: {2}' + NCHAR(10) + N'' + NCHAR(10) + N'Las prendas pasarán a estado En limpieza.'),
    (N'ES', N'msg.cli.eliminado', N'Cliente ''{0}'' eliminado.', N'Cliente ''{0}'' dado de baja.'),
    (N'ES', N'err.bll.cliente.baja_prendas', N'No se puede eliminar a {0}: tiene {1} prenda(s) en uso. Registrá la devolución primero.', N'No se puede dar de baja a {0}: tiene {1} prenda(s) en uso. Registrá la devolución primero.'),
    (N'EN', N'err.bll.usuario.perfil_requerido', N'The profile/role is required.', N'The role is required.'),
    (N'EN', N'err.bll.prenda.baja_requiere_flujoperdida', N'A garment in use can only be discontinued through ''Report Lost Garment'' (with a replacement charge to the client), not directly.', N'A garment in use can only be retired through ''Report Lost Garment'' (with a replacement charge to the customer), not directly.'),
    (N'EN', N'help.permisos.rol', N'Role = profile assigned to a user. Can contain patents, families and other roles (role-in-role).', N'Role = set of permissions assigned to a user. Can contain patents, families and other roles (role-in-role).'),
    (N'EN', N'mnu.clientes', N'Clients', N'Customers'),
    (N'EN', N'mnu.perfiles', N'Profiles & Permissions', N'Roles & Permissions'),
    (N'EN', N'frm.clientes', N'Client Management', N'Customer Management'),
    (N'EN', N'btn.nuevocliente', N'+ New Client', N'+ New Customer'),
    (N'EN', N'btn.darbaja', N'Delete', N'Deactivate'),
    (N'EN', N'lbl.clienteenuso', N'Client in use:', N'Customer in use:'),
    (N'EN', N'lbl.perfilrol', N'Profile (role):', N'Role:'),
    (N'EN', N'lbl.idcliente', N'Client ID:', N'Customer ID:'),
    (N'EN', N'col.ped.cliente', N'Client', N'Customer'),
    (N'EN', N'col.ped.vendedor', N'Seller', N'Salesperson'),
    (N'EN', N'col.neg.cliente', N'Client', N'Customer'),
    (N'EN', N'col.neg.idcliente', N'Client Id', N'Customer ID'),
    (N'EN', N'col.prenda.cliente', N'Client', N'Customer'),
    (N'EN', N'frm.nuevocliente', N'New Client', N'New Customer'),
    (N'EN', N'frm.editarcliente', N'Edit Client', N'Edit Customer'),
    (N'EN', N'btn.registrar.cliente', N'Register Client', N'Register Customer'),
    (N'EN', N'msg.cli.cargados', N'{0} client(s) registered.', N'{0} customer(s) registered.'),
    (N'EN', N'lbl.ped.selcliente', N'Select the client for this order:', N'Select the customer for this order:'),
    (N'EN', N'err.prenda.enuso', N'Cannot change status: garment is currently in use by a client.', N'Cannot change status: garment is currently in use by a customer.'),
    (N'EN', N'tevt.altacliente', N'New Client', N'New Customer'),
    (N'EN', N'tevt.modcliente', N'Client Edit', N'Customer Edit'),
    (N'EN', N'tevt.bajacliente', N'Client Removed', N'Customer Deactivated'),
    (N'EN', N'perfil.vendedor', N'Sales Rep', N'Salesperson'),
    (N'EN', N'lbl.ped.infoplan', N'Client: {0}' + NCHAR(10) + N'Plan: {1}' + NCHAR(10) + N'Garments currently in use: {2}' + NCHAR(10) + N'Payment method: {3}' + NCHAR(10) + N'Since: {4}', N'Customer: {0}' + NCHAR(10) + N'Plan: {1}' + NCHAR(10) + N'Garments currently in use: {2}' + NCHAR(10) + N'Payment method: {3}' + NCHAR(10) + N'Since: {4}'),
    (N'EN', N'err.ped.sinplan', N'{0} has no plan assigned.' + NCHAR(10) + N'Assign a plan in the Clients module before creating an order.', N'{0} has no plan assigned.' + NCHAR(10) + N'Assign a plan in the Customers module before creating an order.'),
    (N'EN', N'frm.gestorpermisos', N'Profile and Permission Manager', N'Role and Permission Manager'),
    (N'EN', N'lbl.permisos.titulo', N'Profiles and Permissions', N'Roles and Permissions'),
    (N'EN', N'perm.pat.gestionarclientes', N'Manage Clients', N'Manage Customers'),
    (N'EN', N'perfil.perfil', N'Profile / Role:', N'Role:'),
    (N'EN', N'frm.dashboard', N'Control Panel', N'Dashboard'),
    (N'EN', N'dash.clientes', N'Clients' + NCHAR(10) + N'registered', N'Customers' + NCHAR(10) + N'registered'),
    (N'EN', N'rpt.kpi.clientes', N'Registered clients', N'Registered customers'),
    (N'EN', N'rpt.txt.clientes', N'Registered clients', N'Registered customers'),
    (N'EN', N'rpt.txt.cliente', N'Client', N'Customer'),
    (N'EN', N'msg.cli.registrado', N'Client ''{0}'' registered successfully.', N'Customer ''{0}'' registered successfully.'),
    (N'EN', N'msg.cli.actualizado', N'Client ''{0}'' updated.', N'Customer ''{0}'' updated.'),
    (N'EN', N'msg.cli.eliminado', N'Client ''{0}'' deleted.', N'Customer ''{0}'' deactivated.'),
    (N'EN', N'conf.baja.cli.titulo', N'Confirm Deletion', N'Confirm Deactivation'),
    (N'EN', N'conf.planes.desat.msg', N'Deactivate plan ''{0}''?' + NCHAR(10) + N'' + NCHAR(10) + N'Existing clients with this plan will not be affected.', N'Deactivate plan ''{0}''?' + NCHAR(10) + N'' + NCHAR(10) + N'Existing customers with this plan will not be affected.'),
    (N'EN', N'err.ped.suscvencida', N'{0}''s subscription expired on {1}.' + NCHAR(10) + N'Update in the Clients module.', N'{0}''s subscription expired on {1}.' + NCHAR(10) + N'Update in the Customers module.'),
    (N'EN', N'lbl.ped.proxvencer', N'Subscription expires in {0} day(s). Remind the client to renew.', N'Subscription expires in {0} day(s). Remind the customer to renew.'),
    (N'EN', N'err.bll.cliente.baja_prendas', N'Cannot delete {0}: they have {1} garment(s) in use. Register the return first.', N'Cannot deactivate {0}: they have {1} garment(s) in use. Register the return first.'),
    (N'EN', N'err.bll.listaespera.cliente_inexistente', N'The client does not exist.', N'The customer does not exist.'),
    (N'EN', N'err.bll.pedido.prenda_reservada', N'The garment ''{0}'' is reserved on the Waiting List for another client. Update the selection.', N'The garment ''{0}'' is reserved on the Waiting List for another customer. Update the selection.'),
    (N'RU', N'btn.darbaja', N'Удалить', N'Деактивировать'),
    (N'RU', N'tevt.bajacliente', N'Удаление клиента', N'Деактивация клиента'),
    (N'RU', N'msg.cli.eliminado', N'Клиент ''{0}'' удалён.', N'Клиент ''{0}'' деактивирован.'),
    (N'RU', N'conf.baja.cli.msg', N'Удалить клиента {0} (ИНН {1})?' + NCHAR(10) + N'' + NCHAR(10) + N'Это действие нельзя отменить.', N'Деактивировать клиента {0} (ИНН {1})?' + NCHAR(10) + N'' + NCHAR(10) + N'Это действие нельзя отменить.'),
    (N'RU', N'conf.baja.cli.titulo', N'Подтверждение удаления', N'Подтверждение деактивации'),
    (N'RU', N'err.bll.cliente.baja_prendas', N'Невозможно удалить {0}: у них {1} вещь(ей) в использовании. Сначала зарегистрируйте возврат.', N'Невозможно деактивировать {0}: у них {1} вещь(ей) в использовании. Сначала зарегистрируйте возврат.'),
    (N'PT', N'btn.darbaja', N'Excluir', N'Desativar'),
    (N'PT', N'tevt.bajacliente', N'Exclusão Cliente', N'Desativação Cliente'),
    (N'PT', N'msg.cli.eliminado', N'Cliente ''{0}'' excluído.', N'Cliente ''{0}'' desativado.'),
    (N'PT', N'conf.baja.cli.msg', N'Excluir {0} (CPF {1})?' + NCHAR(10) + N'' + NCHAR(10) + N'Esta ação não pode ser desfeita.', N'Desativar {0} (CPF {1})?' + NCHAR(10) + N'' + NCHAR(10) + N'Esta ação não pode ser desfeita.'),
    (N'PT', N'conf.baja.cli.titulo', N'Confirmar Exclusão', N'Confirmar Desativação'),
    (N'PT', N'err.bll.cliente.baja_prendas', N'Não é possível excluir {0}: possui {1} roupa(s) em uso. Registre a devolução primeiro.', N'Não é possível desativar {0}: possui {1} roupa(s) em uso. Registre a devolução primeiro.'),
    (N'EN', N'err.bll.cliente.plan_solo_admin', N'Only an Administrator can change a client''s plan or expiration date directly. To activate a new subscription, use the Contracting module.', N'Only an Administrator can change a customer''s plan or expiration date directly. To activate a new subscription, use the Contracting module.'),
    (N'EN', N'err.bll.cliente.menor_edad', N'The client must be over 18 years old.', N'The customer must be over 18 years old.'),
    (N'EN', N'err.bll.renovacion.plan_insuficiente', N'Cannot switch to plan ''{0}'': the client has {1} garment(s) in use and that plan only allows {2}.', N'Cannot switch to plan ''{0}'': the customer has {1} garment(s) in use and that plan only allows {2}.'),
    (N'ES', N'cobro.msg.cobrado', N'Cobro registrado (${0}). Renovación confirmada: nueva vigencia hasta {1:d}.', N'Cobro registrado ({0:C2}). Renovación confirmada: nueva vigencia hasta {1:d}.'),
    (N'EN', N'renov.cliente', N'Client:', N'Customer:'),
    (N'EN', N'renov.decision', N'Decision (after contacting the client)', N'Decision (after contacting the customer)'),
    (N'EN', N'cobro.cliente', N'Client:', N'Customer:'),
    (N'EN', N'renov.msg.baja', N'Subscription cancelled. The client won''t be able to place new orders.', N'Subscription cancelled. The customer won''t be able to place new orders.'),
    (N'EN', N'renov.msg.baja_conprendas', N'Subscription cancelled. The client won''t be able to place new orders. They have {0} garment(s) in use — request the return.', N'Subscription cancelled. The customer won''t be able to place new orders. They have {0} garment(s) in use — request the return.'),
    (N'EN', N'cobro.sinelegibles', N'No clients have a charge pending to process.', N'No customers have a charge pending to process.'),
    (N'EN', N'cobro.msg.cobrado', N'Payment recorded (${0}). Renewal confirmed: new validity until {1:d}.', N'Payment recorded ({0:C2}). Renewal confirmed: new validity until {1:d}.'),
    (N'EN', N'cobro.msg.suspendido', N'The grace period expired without regularizing the payment. The client won''t be able to place new orders until a successful payment is recorded.', N'The grace period expired without regularizing the payment. The customer won''t be able to place new orders until a successful payment is recorded.'),
    (N'EN', N'abandono.col.cliente', N'Client', N'Customer'),
    (N'EN', N'abandono.resultado', N'{0} client(s) at risk under ''{1}''.', N'{0} customer(s) at risk under ''{1}''.'),
    (N'RU', N'cobro.msg.cobrado', N'Платёж зафиксирован (${0}). Продление подтверждено: новый срок действия до {1:d}.', N'Платёж зафиксирован ({0:C2}). Продление подтверждено: новый срок действия до {1:d}.'),
    (N'PT', N'cobro.msg.cobrado', N'Cobrança registrada (${0}). Renovação confirmada: nova vigência até {1:d}.', N'Cobrança registrada ({0:C2}). Renovação confirmada: nova vigência até {1:d}.'),
    (N'EN', N'recom.sinhistorial', N'The client doesn''t have enough order history to generate a recommendation.', N'The customer doesn''t have enough order history to generate a recommendation.'),
    (N'EN', N'recom.motivo.ambos', N'{0} matches the client''s preferred category ({1}) and color ({2}).', N'{0} matches the customer''s preferred category ({1}) and color ({2}).'),
    (N'EN', N'recom.motivo.categoria', N'{0} matches the client''s preferred category ({1}).', N'{0} matches the customer''s preferred category ({1}).'),
    (N'EN', N'recom.motivo.color', N'{0} matches the client''s preferred color ({1}).', N'{0} matches the customer''s preferred color ({1}).'),
    (N'ES', N'msg.cargoprenda.registrado', N'Cargo de ${0} registrado — se sumará al próximo cobro de {1}.', N'Cargo de {0:C2} registrado — se sumará al próximo cobro de {1}.'),
    (N'ES', N'cobro.msg.cobrado.descuento', N'Cobro registrado (${0}). Renovación confirmada: nueva vigencia hasta {1:d}. Incluye descuento por referido de ${2}.', N'Cobro registrado ({0:C2}). Renovación confirmada: nueva vigencia hasta {1:d}. Incluye descuento por referido de {2:C2}.'),
    (N'ES', N'cobro.msg.cobrado.cargos', N'Cobro registrado (${0}). Renovación confirmada: nueva vigencia hasta {1:d}. Incluye {2} cargo(s) por daño/pérdida (${3}).', N'Cobro registrado ({0:C2}). Renovación confirmada: nueva vigencia hasta {1:d}. Incluye {2} cargo(s) por daño/pérdida ({3:C2}).'),
    (N'ES', N'cobro.msg.cobrado.descuentoycargos', N'Cobro registrado (${0}). Renovación confirmada: nueva vigencia hasta {1:d}. Incluye descuento por referido de ${2} y {3} cargo(s) por daño/pérdida (${4}).', N'Cobro registrado ({0:C2}). Renovación confirmada: nueva vigencia hasta {1:d}. Incluye descuento por referido de {2:C2} y {3} cargo(s) por daño/pérdida ({4:C2}).'),
    (N'ES', N'cobro.msg.cobrado.promo', N'Cobro registrado (${0}). Renovación confirmada: nueva vigencia hasta {1:d}. Incluye la promoción ''{2}'' (-${3}).', N'Cobro registrado ({0:C2}). Renovación confirmada: nueva vigencia hasta {1:d}. Incluye la promoción ''{2}'' (-{3:C2}).'),
    (N'ES', N'cobro.msg.cobrado.promoycargos', N'Cobro registrado (${0}). Renovación confirmada: nueva vigencia hasta {1:d}. Incluye la promoción ''{2}'' (-${3}) y {4} cargo(s) por daño/pérdida (${5}).', N'Cobro registrado ({0:C2}). Renovación confirmada: nueva vigencia hasta {1:d}. Incluye la promoción ''{2}'' (-{3:C2}) y {4} cargo(s) por daño/pérdida ({5:C2}).'),
    (N'EN', N'renov.msg.pausada', N'Subscription paused until {0:d}. The client won''t be able to place new orders until it''s resumed.', N'Subscription paused until {0:d}. The customer won''t be able to place new orders until it''s resumed.'),
    (N'EN', N'renov.sinelegibles', N'No clients are eligible to process this decision.', N'No customers are eligible to process this decision.'),
    (N'EN', N'lbl.cargoprenda.info', N'Garment: {0}' + NCHAR(10) + N'Last client: {1}', N'Garment: {0}' + NCHAR(10) + N'Last customer: {1}'),
    (N'EN', N'msg.cargoprenda.registrado', N'Charge of ${0} registered — it will be added to {1}''s next payment.', N'Charge of {0:C2} registered — it will be added to {1}''s next payment.'),
    (N'EN', N'err.bll.cargoprenda.sin_cliente', N'Garment ''{0}'' has no last known client; there''s no one to charge for it.', N'Garment ''{0}'' has no last known customer; there''s no one to charge for it.'),
    (N'EN', N'cobro.msg.cobrado.descuento', N'Payment recorded (${0}). Renewal confirmed: new validity until {1:d}. Includes a referral discount of ${2}.', N'Payment recorded ({0:C2}). Renewal confirmed: new validity until {1:d}. Includes a referral discount of {2:C2}.'),
    (N'EN', N'cobro.msg.cobrado.cargos', N'Payment recorded (${0}). Renewal confirmed: new validity until {1:d}. Includes {2} damage/loss charge(s) (${3}).', N'Payment recorded ({0:C2}). Renewal confirmed: new validity until {1:d}. Includes {2} damage/loss charge(s) ({3:C2}).'),
    (N'EN', N'cobro.msg.cobrado.descuentoycargos', N'Payment recorded (${0}). Renewal confirmed: new validity until {1:d}. Includes a referral discount of ${2} and {3} damage/loss charge(s) (${4}).', N'Payment recorded ({0:C2}). Renewal confirmed: new validity until {1:d}. Includes a referral discount of {2:C2} and {3} damage/loss charge(s) ({4:C2}).'),
    (N'EN', N'cobro.msg.cobrado.promo', N'Payment recorded (${0}). Renewal confirmed: new validity until {1:d}. Includes the promotion ''{2}'' (-${3}).', N'Payment recorded ({0:C2}). Renewal confirmed: new validity until {1:d}. Includes the promotion ''{2}'' (-{3:C2}).'),
    (N'EN', N'cobro.msg.cobrado.promoycargos', N'Payment recorded (${0}). Renewal confirmed: new validity until {1:d}. Includes the promotion ''{2}'' (-${3}) and {4} damage/loss charge(s) (${5}).', N'Payment recorded ({0:C2}). Renewal confirmed: new validity until {1:d}. Includes the promotion ''{2}'' (-{3:C2}) and {4} damage/loss charge(s) ({5:C2}).'),
    (N'RU', N'msg.cargoprenda.registrado', N'Штраф на сумму ${0} зарегистрирован — будет добавлен к следующему платежу {1}.', N'Штраф на сумму {0:C2} зарегистрирован — будет добавлен к следующему платежу {1}.'),
    (N'RU', N'cobro.msg.cobrado.descuento', N'Платёж зафиксирован (${0}). Продление подтверждено: новый срок действия до {1:d}. Включает реферальную скидку ${2}.', N'Платёж зафиксирован ({0:C2}). Продление подтверждено: новый срок действия до {1:d}. Включает реферальную скидку {2:C2}.'),
    (N'RU', N'cobro.msg.cobrado.cargos', N'Платёж зафиксирован (${0}). Продление подтверждено: новый срок действия до {1:d}. Включает {2} штраф(ов) за повреждение/утрату (${3}).', N'Платёж зафиксирован ({0:C2}). Продление подтверждено: новый срок действия до {1:d}. Включает {2} штраф(ов) за повреждение/утрату ({3:C2}).'),
    (N'RU', N'cobro.msg.cobrado.descuentoycargos', N'Платёж зафиксирован (${0}). Продление подтверждено: новый срок действия до {1:d}. Включает реферальную скидку ${2} и {3} штраф(ов) за повреждение/утрату (${4}).', N'Платёж зафиксирован ({0:C2}). Продление подтверждено: новый срок действия до {1:d}. Включает реферальную скидку {2:C2} и {3} штраф(ов) за повреждение/утрату ({4:C2}).'),
    (N'RU', N'cobro.msg.cobrado.promo', N'Платёж зафиксирован (${0}). Продление подтверждено: новый срок действия до {1:d}. Включает акцию ''{2}'' (-${3}).', N'Платёж зафиксирован ({0:C2}). Продление подтверждено: новый срок действия до {1:d}. Включает акцию ''{2}'' (-{3:C2}).'),
    (N'RU', N'cobro.msg.cobrado.promoycargos', N'Платёж зафиксирован (${0}). Продление подтверждено: новый срок действия до {1:d}. Включает акцию ''{2}'' (-${3}) и {4} штраф(ов) за повреждение/утрату (${5}).', N'Платёж зафиксирован ({0:C2}). Продление подтверждено: новый срок действия до {1:d}. Включает акцию ''{2}'' (-{3:C2}) и {4} штраф(ов) за повреждение/утрату ({5:C2}).'),
    (N'PT', N'msg.cargoprenda.registrado', N'Cobrança de ${0} registrada — será somada à próxima cobrança de {1}.', N'Cobrança de {0:C2} registrada — será somada à próxima cobrança de {1}.'),
    (N'PT', N'cobro.msg.cobrado.descuento', N'Cobrança registrada (${0}). Renovação confirmada: nova vigência até {1:d}. Inclui desconto por indicação de ${2}.', N'Cobrança registrada ({0:C2}). Renovação confirmada: nova vigência até {1:d}. Inclui desconto por indicação de {2:C2}.'),
    (N'PT', N'cobro.msg.cobrado.cargos', N'Cobrança registrada (${0}). Renovação confirmada: nova vigência até {1:d}. Inclui {2} cobrança(s) por dano/perda (${3}).', N'Cobrança registrada ({0:C2}). Renovação confirmada: nova vigência até {1:d}. Inclui {2} cobrança(s) por dano/perda ({3:C2}).'),
    (N'PT', N'cobro.msg.cobrado.descuentoycargos', N'Cobrança registrada (${0}). Renovação confirmada: nova vigência até {1:d}. Inclui desconto por indicação de ${2} e {3} cobrança(s) por dano/perda (${4}).', N'Cobrança registrada ({0:C2}). Renovação confirmada: nova vigência até {1:d}. Inclui desconto por indicação de {2:C2} e {3} cobrança(s) por dano/perda ({4:C2}).'),
    (N'PT', N'cobro.msg.cobrado.promo', N'Cobrança registrada (${0}). Renovação confirmada: nova vigência até {1:d}. Inclui a promoção ''{2}'' (-${3}).', N'Cobrança registrada ({0:C2}). Renovação confirmada: nova vigência até {1:d}. Inclui a promoção ''{2}'' (-{3:C2}).'),
    (N'PT', N'cobro.msg.cobrado.promoycargos', N'Cobrança registrada (${0}). Renovação confirmada: nova vigência até {1:d}. Inclui a promoção ''{2}'' (-${3}) e {4} cobrança(s) por dano/perda (${5}).', N'Cobrança registrada ({0:C2}). Renovação confirmada: nova vigência até {1:d}. Inclui a promoção ''{2}'' (-{3:C2}) e {4} cobrança(s) por dano/perda ({5:C2}).'),
    (N'EN', N'contratacion.cliente', N'Client:', N'Customer:'),
    (N'EN', N'err.bll.contratacion.pendiente_existente', N'This client already has a contract pending payment. It must be resolved (charged or cancelled) before registering a new one.', N'This customer already has a contract pending payment. It must be resolved (charged or cancelled) before registering a new one.'),
    (N'EN', N'err.bll.contratacion.cliente_inexistente', N'The selected client does not exist.', N'The selected customer does not exist.'),
    (N'EN', N'err.contratacion.faltandatos', N'Select a client and a plan to continue.', N'Select a customer and a plan to continue.'),
    (N'ES', N'msg.insp.baja_ok', N'''{0}'' dada de baja — cargo de ${1} registrado.', N'''{0}'' dada de baja — cargo de {1:C2} registrado.'),
    (N'EN', N'msg.insp.baja_ok', N'''{0}'' discarded — charge of ${1} recorded.', N'''{0}'' retired — charge of {1:C2} recorded.'),
    (N'RU', N'msg.insp.baja_ok', N'''{0}'' списана — зарегистрирован платёж на ${1}.', N'''{0}'' списана — зарегистрирован платёж на {1:C2}.'),
    (N'PT', N'msg.insp.baja_ok', N'''{0}'' baixada — cobrança de ${1} registrada.', N'''{0}'' baixada — cobrança de {1:C2} registrada.'),
    (N'ES', N'msg.ped.perdida_ok', N'''{0}'' reportada como perdida — cargo de ${1} registrado.', N'''{0}'' reportada como perdida — cargo de {1:C2} registrado.'),
    (N'EN', N'msg.ped.perdida_ok', N'''{0}'' reported as lost — charge of ${1} recorded.', N'''{0}'' reported as lost — charge of {1:C2} recorded.'),
    (N'RU', N'msg.ped.perdida_ok', N'''{0}'' отмечена как утерянная — зарегистрирован платёж на ${1}.', N'''{0}'' отмечена как утерянная — зарегистрирован платёж на {1:C2}.'),
    (N'PT', N'msg.ped.perdida_ok', N'''{0}'' reportada como perdida — cobrança de ${1} registrada.', N'''{0}'' reportada como perdida — cobrança de {1:C2} registrada.'),
    (N'EN', N'lbl.listaespera.elegircliente', N'Client waiting for ''{0}'':', N'Customer waiting for ''{0}'':'),
    (N'EN', N'col.contr.cliente', N'Client', N'Customer'),
    (N'EN', N'col.insp.ultimocliente', N'Last Client', N'Last Customer'),
    (N'EN', N'paso1.identificar', N'Step 1 of 2 — Identify the client', N'Step 1 of 2 — Identify the customer'),
    (N'EN', N'lbl.ped.identificacion', N'Client identification (ID number, first or last name):', N'Customer identification (ID number, first or last name):'),
    (N'EN', N'err.ped.noencontrado', N'No client was found with that identification. Check the ID number, first name or last name.', N'No customer was found with that identification. Check the ID number, first name or last name.'),
    (N'EN', N'lbl.ped.variascoinc', N'Several clients match: choose the right one from the list.', N'Several customers match: choose the right one from the list.'),
    (N'EN', N'lbl.ped.ficha', N'Client: {0}  —  ID {1}' + NCHAR(10) + N'Plan: {2} (up to {3} garments)  —  Expires: {4}' + NCHAR(10) + N'Garments in use: {5}  —  Last order: {6}' + NCHAR(10) + N'Payment method: {7}  —  Joined: {8}', N'Customer: {0}  —  ID {1}' + NCHAR(10) + N'Plan: {2} (up to {3} garments)  —  Expires: {4}' + NCHAR(10) + N'Garments in use: {5}  —  Last order: {6}' + NCHAR(10) + N'Payment method: {7}  —  Joined: {8}'),
    (N'EN', N'lbl.ped.res.ajustar', N'The client adjusts the selection or withdraws.', N'The customer adjusts the selection or withdraws.'),
    (N'EN', N'dlg.desistir.prompt', N'Withdrawal reason given by the client:', N'Withdrawal reason given by the customer:'),
    (N'EN', N'msg.cs.faltantes', N'Missing-items report for Order #{0} issued: {1} garment(s). The seller will tell the client.', N'Missing-items report for Order #{0} issued: {1} garment(s). The salesperson will tell the customer.'),
    (N'EN', N'msg.cs.separadas', N'Order #{0}: garments set aside. The seller can now finalize the order.', N'Order #{0}: garments set aside. The salesperson can now finalize the order.'),
    (N'EN', N'conf.cs.separar', N'Set aside the {0} garment(s) of Order #{1} for {2}?' + NCHAR(10) + N'' + NCHAR(10) + N'They become reserved (in use) in the client''s name.', N'Set aside the {0} garment(s) of Order #{1} for {2}?' + NCHAR(10) + N'' + NCHAR(10) + N'They become reserved (in use) in the customer''s name.'),
    (N'EN', N'doc.ped.cliente', N'Client', N'Customer'),
    (N'EN', N'doc.ped.vendedor', N'Seller', N'Salesperson'),
    (N'EN', N'doc.faltantes.reservada', N'reserved by the Waiting List for another client', N'reserved by the Waiting List for another customer'),
    (N'EN', N'doc.separacion.lista', N'Garments set aside for the order (now in use in the client''s name):', N'Garments set aside for the order (now in use in the customer''s name):'),
    (N'EN', N'doc.desist.motivo', N'Reason given by the client', N'Reason given by the customer'),
    (N'EN', N'alert.pedidos.faltantes', N'{0} order(s) with missing items to tell the client about.', N'{0} order(s) with missing items to tell the customer about.'),
    (N'EN', N'alert.listaespera.reservadas', N'{0} garment(s) reserved by the Waiting List waiting for the client to pick them up.', N'{0} garment(s) reserved by the Waiting List waiting for the customer to pick them up.'),
    (N'EN', N'err.bll.pedido.desistir_sin_motivo', N'You must enter the withdrawal reason given by the client.', N'You must enter the withdrawal reason given by the customer.'),
    (N'EN', N'err.bll.pedido.separar_faltantes', N'While setting aside, one or more garments of Order #{0} were no longer available. Nothing was reserved and a missing-items report was issued for the seller.', N'While setting aside, one or more garments of Order #{0} were no longer available. Nothing was reserved and a missing-items report was issued for the salesperson.'),
    (N'EN', N'doc.cupo.opciones', N'The client can adjust the selection or withdraw.', N'The customer can adjust the selection or withdraw.'),
    (N'EN', N'err.bll.cliente.baja_contratacion', N'{0} cannot be removed: they have a contract pending payment. Cashier must charge or cancel it first.', N'{0} cannot be deactivated: they have a contract pending payment. Cashier must charge or cancel it first.'),
    (N'EN', N'err.bll.cliente.baja_pedido', N'{0} cannot be removed: they have an order in progress. Wait for it to finish or cancel it first.', N'{0} cannot be deactivated: they have an order in progress. Wait for it to finish or cancel it first.'),
    (N'EN', N'err.bll.contratacion.desistir_sin_motivo', N'You must enter the withdrawal reason given by the client.', N'You must enter the withdrawal reason given by the customer.'),
    (N'EN', N'msg.contr.desistimiento', N'Withdrawal recorded: the client did not sign up for any plan.', N'Withdrawal recorded: the customer did not sign up for any plan.'),
    (N'EN', N'msg.contr.noregistrado', N'The client is not registered. Register them to continue with the contract.', N'The customer is not registered. Register them to continue with the contract.'),
    (N'EN', N'lbl.contr.ficha', N'Client: {0}  —  ID {1}' + NCHAR(10) + N'Current plan: {2}  —  Expires: {3}  —  Garments in use: {4}', N'Customer: {0}  —  ID {1}' + NCHAR(10) + N'Current plan: {2}  —  Expires: {3}  —  Garments in use: {4}'),
    (N'EN', N'btn.contr.registrarcliente', N'Register client', N'Register customer'),
    (N'EN', N'btn.contr.desistir', N'Client withdraws', N'Customer withdraws'),
    (N'EN', N'conf.cli.contratar', N'Continue with a plan contract for this client?', N'Continue with a plan contract for this customer?'),
    (N'EN', N'dash.kan.faltantes', N'Missing items: tell the client', N'Missing items: tell the customer'),
    (N'RU', N'err.bll.cliente.baja_contratacion', N'Нельзя удалить клиента {0}: у него есть договор, ожидающий оплаты. Касса должна сначала принять оплату или отменить его.', N'Нельзя деактивировать клиента {0}: у него есть договор, ожидающий оплаты. Касса должна сначала принять оплату или отменить его.'),
    (N'RU', N'err.bll.cliente.baja_pedido', N'Нельзя удалить клиента {0}: у него есть заказ в работе. Дождитесь его завершения или сначала отмените его.', N'Нельзя деактивировать клиента {0}: у него есть заказ в работе. Дождитесь его завершения или сначала отмените его.'),
    (N'PT', N'err.bll.cliente.baja_contratacion', N'Não é possível dar baixa em {0}: há uma contratação pendente de pagamento. O caixa precisa cobrá-la ou cancelá-la primeiro.', N'Não é possível desativar {0}: há uma contratação pendente de pagamento. O caixa precisa cobrá-la ou cancelá-la primeiro.'),
    (N'PT', N'err.bll.cliente.baja_pedido', N'Não é possível dar baixa em {0}: há um pedido em andamento. Aguarde a conclusão ou cancele-o primeiro.', N'Não é possível desativar {0}: há um pedido em andamento. Aguarde a conclusão ou cancele-o primeiro.');

    UPDATE t SET t.Texto = f.TextoNuevo
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @TextosFabrica f ON f.Clave = c.Clave AND f.Codigo = i.Codigo
    WHERE t.Texto COLLATE Latin1_General_BIN2 = f.TextoAnterior COLLATE Latin1_General_BIN2; -- exacto: no pisa ediciones
    PRINT CONCAT('Traducciones de fábrica corregidas: ', @@ROWCOUNT, ' fila(s).');
END
GO

-- ============================================================
-- WardrobeFlow — 21z5. RESTRICCIONES DE NORMALIZACIÓN SOBRE SEGURIDAD (3FN)
-- ------------------------------------------------------------
-- Sin cambios de código: el motor respalda lo que la aplicación ya cumple.
--   · UX_Permiso_NombreRol: el nombre de un rol es único (índice filtrado EsRol = 1). Hace de
--     Permiso.Nombre una clave candidata para los roles, que es lo que referencia Usuario.Rol.
--   · CK_Usuario_PerfilEsRol: Perfil es siempre el mismo valor que Rol (DAL.Usuario los escribe
--     juntos); el CHECK impide que diverjan por SQL directo.
--   · FK_Usuario_Idioma: Usuario.IdIdioma (código 'ES', 'EN'...) referencia Idioma(Codigo), que es
--     UNIQUE. Antes se normalizan los códigos que no existen al idioma por defecto (IdIdioma no
--     entra en el dígito verificador de Usuario: no hace falta recalcularlo).
--   Si los datos existentes no cumplen una restricción, no se crea y queda un AVISO.
--   · RolPermiso (asignación plana rol→patente) es una tabla DE PASO del instalador: las
--     secciones 01, 05 y 21d la usan para sembrar los nodos-rol y las aristas de PermisoRelacion,
--     que es la única fuente de verdad. Se borra al final (después de su último uso, 21d-4), así la
--     base instalada no guarda la asignación duplicada. Cada corrida del script la vuelve a crear.
-- Idempotente.
-- ============================================================
SET QUOTED_IDENTIFIER ON;   -- el índice filtrado lo exige (sqlcmd lo trae en OFF)
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Permiso_NombreRol' AND object_id = OBJECT_ID('Permiso'))
BEGIN
    IF EXISTS (SELECT 1 FROM Permiso WHERE EsRol = 1 GROUP BY Nombre HAVING COUNT(*) > 1)
        PRINT 'AVISO: UX_Permiso_NombreRol no creado: hay roles con el mismo nombre en Permiso (EsRol = 1).';
    ELSE
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_Permiso_NombreRol ON Permiso(Nombre) WHERE EsRol = 1;
        PRINT 'Permiso: índice único UX_Permiso_NombreRol creado (nombre de rol único).';
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Usuario_PerfilEsRol')
BEGIN
    IF EXISTS (SELECT 1 FROM Usuario WHERE Rol IS NOT NULL AND Perfil IS NOT NULL AND Rol <> Perfil)
        PRINT 'AVISO: CK_Usuario_PerfilEsRol no creado: hay usuarios con Perfil distinto de Rol.';
    ELSE
    BEGIN
        ALTER TABLE Usuario ADD CONSTRAINT CK_Usuario_PerfilEsRol CHECK (Rol IS NULL OR Perfil IS NULL OR Rol = Perfil);
        PRINT 'Usuario: CHECK CK_Usuario_PerfilEsRol creado (Perfil = Rol).';
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Usuario_Idioma')
BEGIN
    -- La FK necesita una clave única en Idioma.Codigo (UQ_Idioma_Codigo desde la creación de la tabla).
    IF NOT EXISTS (SELECT 1 FROM sys.indexes i
                   JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                   JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                   WHERE i.object_id = OBJECT_ID('Idioma') AND i.is_unique = 1 AND i.has_filter = 0
                     AND c.name = 'Codigo'
                     AND (SELECT COUNT(*) FROM sys.index_columns x WHERE x.object_id = i.object_id AND x.index_id = i.index_id) = 1)
    BEGIN
        IF EXISTS (SELECT 1 FROM Idioma GROUP BY Codigo HAVING COUNT(*) > 1)
            PRINT 'AVISO: FK_Usuario_Idioma no creada: Idioma.Codigo tiene códigos repetidos.';
        ELSE
            ALTER TABLE Idioma ADD CONSTRAINT UQ_Idioma_Codigo UNIQUE (Codigo);
    END
    IF EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
               JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
               WHERE i.object_id = OBJECT_ID('Idioma') AND i.is_unique = 1 AND i.has_filter = 0 AND c.name = 'Codigo')
    BEGIN
        DECLARE @idiomaDefault VARCHAR(5) = COALESCE(
            (SELECT TOP 1 Codigo FROM Idioma WHERE EsDefault = 1 ORDER BY IdIdioma),
            (SELECT TOP 1 Codigo FROM Idioma WHERE Codigo = 'ES'),
            (SELECT TOP 1 Codigo FROM Idioma ORDER BY IdIdioma));
        UPDATE u SET u.IdIdioma = @idiomaDefault
        FROM Usuario u
        WHERE u.IdIdioma IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Idioma i WHERE i.Codigo = u.IdIdioma);
        IF @@ROWCOUNT > 0 PRINT 'Usuario: preferencias de idioma con un código inexistente pasaron al idioma por defecto.';
        ALTER TABLE Usuario ADD CONSTRAINT FK_Usuario_Idioma FOREIGN KEY (IdIdioma) REFERENCES Idioma(Codigo);
        PRINT 'Usuario: FK_Usuario_Idioma creada (IdIdioma → Idioma.Codigo).';
    END
END
GO

IF OBJECT_ID('RolPermiso', 'U') IS NOT NULL
BEGIN
    DROP TABLE RolPermiso;
    PRINT 'RolPermiso (tabla de paso del instalador) eliminada: la composición vive en PermisoRelacion.';
END
PRINT 'Sección 21z5: restricciones de normalización verificadas.';
GO

-- ============================================================
-- WardrobeFlow — 21z6. TEXTOS EN ESPAÑOL EN USTED (sin voseo)
-- ------------------------------------------------------------
-- La interfaz en español pasa del voseo ("Seleccioná", "Volvé a intentar", "tu usuario") al usted
-- ("Seleccione", "Vuelva a intentar", "su usuario"), el mismo registro que el instalador. Los textos
-- nuevos están en traducciones.tsv y en los textos de respaldo del código; acá se actualizan los que
-- la base ya tiene. Solo se cambia un texto que sigue EXACTAMENTE igual al de fábrica: una traducción
-- editada en Idiomas se respeta. Idempotente: en una base ya migrada no cambia nada.
-- ============================================================
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @Usted TABLE (TextoAnterior NVARCHAR(4000) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(4000) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @Usted (TextoAnterior, TextoNuevo) VALUES
    (N'La prenda ''{0}'' no viene de una devolución: no se le puede cobrar al último cliente. Si no sirve más, dala de baja desde Prendas.', N'La prenda ''{0}'' no viene de una devolución: no se le puede cobrar al último cliente. Si no sirve más, dela de baja desde Prendas.'),
    (N'No podés archivar tu propio usuario mientras tenés la sesión abierta.', N'No puede archivar su propio usuario mientras tiene la sesión abierta.'),
    (N'No se puede archivar al último Administrador activo del sistema. Creá o activá otro Administrador antes de archivar este.', N'No se puede archivar al último Administrador activo del sistema. Cree o active otro Administrador antes de archivar este.'),
    (N'La sesión expiró. Volvé a iniciar sesión.', N'La sesión expiró. Vuelva a iniciar sesión.'),
    (N'La sesión no está iniciada. Iniciá sesión primero.', N'La sesión no está iniciada. Inicie sesión primero.'),
    (N'Ya hay una sesión iniciada. Cerrá la sesión actual antes de iniciar otra.', N'Ya hay una sesión iniciada. Cierre la sesión actual antes de iniciar otra.'),
    (N'Operación cancelada: se detectó una posible manipulación de los datos de usuarios (dígito verificador inválido). Reiniciá el sistema para reparar la integridad antes de continuar.', N'Operación cancelada: se detectó una posible manipulación de los datos de usuarios (dígito verificador inválido). Reinicie el sistema para reparar la integridad antes de continuar.'),
    (N'No se puede eliminar el rol ''{0}'': está asignado a {1} usuario(s) ({2}). Reasigná esos usuarios a otro rol antes de eliminarlo.', N'No se puede eliminar el rol ''{0}'': está asignado a {1} usuario(s) ({2}). Reasigne esos usuarios a otro rol antes de eliminarlo.'),
    (N'El rol ''{0}'' no existe como nodo del árbol de permisos. Volvé a abrir la pantalla.', N'El rol ''{0}'' no existe como nodo del árbol de permisos. Vuelva a abrir la pantalla.'),
    (N'No se puede asignar el plan ''{0}'': el cliente tiene {1} prenda(s) en uso y ese plan solo permite {2}. Registrá las devoluciones primero.', N'No se puede asignar el plan ''{0}'': el cliente tiene {1} prenda(s) en uso y ese plan solo permite {2}. Registre las devoluciones primero.'),
    (N'El usuario ''{0}'' no tiene un Empleado vinculado. Pedíle al Administrador que configure el vínculo.', N'El usuario ''{0}'' no tiene un Empleado vinculado. Pídale al Administrador que configure el vínculo.'),
    (N'El cliente tiene el pedido #{0} despachado pendiente de entrega. Confirmá la entrega antes de crear uno nuevo.', N'El cliente tiene el pedido #{0} despachado pendiente de entrega. Confirme la entrega antes de crear uno nuevo.'),
    (N'No se puede desactivar el plan: tiene {0} cliente(s) activo(s) asignado(s). Reasignalos a otro plan antes de desactivarlo.', N'No se puede desactivar el plan: tiene {0} cliente(s) activo(s) asignado(s). Reasígnelos a otro plan antes de desactivarlo.'),
    (N'Seleccioná una Familia o Rol para agregarle hijos.', N'Seleccione una Familia o Rol para agregarle hijos.'),
    (N'Seleccioná un idioma de la grilla.', N'Seleccione un idioma de la grilla.'),
    (N'Idioma ''{0}'' creado (inactivo — activalo cuando cargues sus traducciones).', N'Idioma ''{0}'' creado (inactivo — actívelo cuando cargue sus traducciones).'),
    (N'Este idioma tiene {0} control(es) sin traducir.' + NCHAR(10) + N'Si lo activás, esos textos se mostrarán en el idioma por defecto.' + NCHAR(10) + N'' + NCHAR(10) + N'¿Activar de todos modos?', N'Este idioma tiene {0} control(es) sin traducir.' + NCHAR(10) + N'Si lo activa, esos textos se mostrarán en el idioma por defecto.' + NCHAR(10) + N'' + NCHAR(10) + N'¿Activar de todos modos?'),
    (N'ORGANIZÁ • GESTIONÁ • POTENCIÁ', N'ORGANICE • GESTIONE • POTENCIE'),
    (N'Ingresá tus credenciales para continuar', N'Ingrese sus credenciales para continuar'),
    (N'La cuenta está bloqueada.' + NCHAR(10) + N'Contactá al Administrador para reactivarla.', N'La cuenta está bloqueada.' + NCHAR(10) + N'Contacte al Administrador para reactivarla.'),
    (N'Seleccioná un usuario' + NCHAR(10) + N'en la lista y presioná:', N'Seleccione un usuario' + NCHAR(10) + N'en la lista y presione:'),
    (N'Seleccioná un usuario' + NCHAR(10) + N'bloqueado y presioná:', N'Seleccione un usuario' + NCHAR(10) + N'bloqueado y presione:'),
    (N'Ingresá tu nombre de usuario. Un administrador' + NCHAR(10) + N'podrá resetear tu contraseña desde el sistema.', N'Ingrese su nombre de usuario. Un administrador' + NCHAR(10) + N'podrá resetear su contraseña desde el sistema.'),
    (N'Seleccioná el cliente para este pedido:', N'Seleccione el cliente para este pedido:'),
    (N'Seleccioná las prendas para incluir en el pedido (checkbox):', N'Seleccione las prendas para incluir en el pedido (checkbox):'),
    (N'Este pedido ya no existe. Actualizá la grilla.', N'Este pedido ya no existe. Actualice la grilla.'),
    (N'Seleccioná una opción.', N'Seleccione una opción.'),
    (N'Seleccioná al menos una prenda.', N'Seleccione al menos una prenda.'),
    (N'Seleccioná una fila del historial para restaurar.', N'Seleccione una fila del historial para restaurar.'),
    (N'{0} no tiene plan asignado.' + NCHAR(10) + N'Asigná un plan en el módulo de Clientes antes de crear un pedido.', N'{0} no tiene plan asignado.' + NCHAR(10) + N'Asigne un plan en el módulo de Clientes antes de crear un pedido.'),
    (N'Ingresá tu nombre de usuario.', N'Ingrese su nombre de usuario.'),
    (N'Si el usuario existe, contactá al administrador para que resetee' + NCHAR(10) + N'tu contraseña desde Administrar -> Usuarios.', N'Si el usuario existe, contacte al administrador para que resetee' + NCHAR(10) + N'su contraseña desde Administrar -> Usuarios.'),
    (N'Seleccioná un usuario de la lista.', N'Seleccione un usuario de la lista.'),
    (N'Seleccioná un usuario bloqueado de la lista.', N'Seleccione un usuario bloqueado de la lista.'),
    (N'No se puede generar el backup: la base tiene problemas de integridad.' + NCHAR(10) + N'Reparalos desde Administrar → Diagnóstico de Integridad antes de respaldar.', N'No se puede generar el backup: la base tiene problemas de integridad.' + NCHAR(10) + N'Repárelos desde Administrar → Diagnóstico de Integridad antes de respaldar.'),
    (N'Ingresá tu usuario y una de tus claves de emergencia de un solo uso para desbloquear la cuenta.', N'Ingrese su usuario y una de sus claves de emergencia de un solo uso para desbloquear la cuenta.'),
    (N'Cuenta desbloqueada con éxito.' + NCHAR(10) + N'Ya podés iniciar sesión normalmente.', N'Cuenta desbloqueada con éxito.' + NCHAR(10) + N'Ya puede iniciar sesión normalmente.'),
    (N'Ingresá tu usuario y una clave de emergencia.', N'Ingrese su usuario y una clave de emergencia.'),
    (N'La cuenta no está bloqueada: podés iniciar sesión normalmente.', N'La cuenta no está bloqueada: puede iniciar sesión normalmente.'),
    (N'No quedan claves de emergencia. Pedile a otro Administrador que genere un nuevo set.', N'No quedan claves de emergencia. Pídale a otro Administrador que genere un nuevo set.'),
    (N'Sin backups. Generá uno desde Administrar → Backup.', N'Sin backups. Genere uno desde Administrar → Backup.'),
    (N'Demasiados intentos fallidos. Cerrá esta ventana e intentá de nuevo más tarde.', N'Demasiados intentos fallidos. Cierre esta ventana e intente de nuevo más tarde.'),
    (N'Seleccioná una versión de la grilla.', N'Seleccione una versión de la grilla.'),
    (N'La suscripción vence en {0} día(s). Avisale al cliente para renovarla.', N'La suscripción vence en {0} día(s). Avísele al cliente para renovarla.'),
    (N'El plan ''{0}'' permite {1} prenda(s). Estás superando el límite por {2}.', N'El plan ''{0}'' permite {1} prenda(s). Está superando el límite por {2}.'),
    (N'Podés agregar hasta {0} prenda(s) (plan {1}).', N'Puede agregar hasta {0} prenda(s) (plan {1}).'),
    (N'El plan ''{0}'' permite {1}. Estás eligiendo {2} de {3} posibles — podés agregar más.', N'El plan ''{0}'' permite {1}. Está eligiendo {2} de {3} posibles — puede agregar más.'),
    (N'Alcanzás el máximo del plan ''{0}'' ({1} prendas).', N'Alcanza el máximo del plan ''{0}'' ({1} prendas).'),
    (N'No se puede dar de baja a {0}: tiene {1} prenda(s) en uso. Registrá la devolución primero.', N'No se puede dar de baja a {0}: tiene {1} prenda(s) en uso. Registre la devolución primero.'),
    (N'El cliente {0} no tiene plan asignado. Asignale un plan antes de crear un pedido.', N'El cliente {0} no tiene plan asignado. Asígnele un plan antes de crear un pedido.'),
    (N'La prenda ''{0}'' está reservada por Lista de Espera para otro cliente. Actualizá la selección.', N'La prenda ''{0}'' está reservada por Lista de Espera para otro cliente. Actualice la selección.'),
    (N'Seleccioná un idioma primero.', N'Seleccione un idioma primero.'),
    (N'DV inválido — usá «Recalcular Todo»', N'DV inválido — use «Recalcular Todo»'),
    (N'Seleccioná un rol.', N'Seleccione un rol.'),
    (N'Cargá los datos del nuevo usuario y presioná Crear.', N'Cargue los datos del nuevo usuario y presione Crear.'),
    (N'Ingresá la contraseña con la que se cifró este backup:', N'Ingrese la contraseña con la que se cifró este backup:'),
    (N'Ingresá una contraseña para CIFRAR el backup.' + NCHAR(10) + N'La vas a necesitar para restaurarlo (no se puede recuperar).', N'Ingrese una contraseña para CIFRAR el backup.' + NCHAR(10) + N'La va a necesitar para restaurarlo (no se puede recuperar).'),
    (N'Repetí la contraseña para confirmar:', N'Repita la contraseña para confirmar:'),
    (N'Ingresá una contraseña nueva.', N'Ingrese una contraseña nueva.'),
    (N'Debés cambiar tu contraseña temporal para ingresar.', N'Debe cambiar su contraseña temporal para ingresar.'),
    (N'Tu contraseña es temporal. Definí una nueva para continuar.', N'Su contraseña es temporal. Defina una nueva para continuar.'),
    (N'Contraseña actualizada. Ya podés usar el sistema.', N'Contraseña actualizada. Ya puede usar el sistema.'),
    (N'Seleccioná un ROL del árbol para eliminar (los permisos no se eliminan).', N'Seleccione un ROL del árbol para eliminar (los permisos no se eliminan).'),
    (N'Seleccioná un ROL del árbol para renombrar (los permisos no se editan).', N'Seleccione un ROL del árbol para renombrar (los permisos no se editan).'),
    (N'Esta acción dejaría al sistema SIN NINGÚN usuario con acceso de Gestión de Usuarios. Asigná la gestión a otro rol o usuario antes de continuar.', N'Esta acción dejaría al sistema SIN NINGÚN usuario con acceso de Gestión de Usuarios. Asigne la gestión a otro rol o usuario antes de continuar.'),
    (N'No podés quitarte a vos mismo el acceso de Gestión de Usuarios de tu propio rol.', N'No puede quitarse a sí mismo el acceso de Gestión de Usuarios de su propio rol.'),
    (N'No podés agregarte a vos mismo permisos que tu rol no tiene hoy, editando tu propio rol. Pedile a otro administrador que lo haga.', N'No puede agregarse a sí mismo permisos que su rol no tiene hoy, editando su propio rol. Pídale a otro administrador que lo haga.'),
    (N'El sistema no está disponible en este momento por tareas de mantenimiento. Reintentá más tarde o contactá al Administrador.', N'El sistema no está disponible en este momento por tareas de mantenimiento. Reintente más tarde o contacte al Administrador.'),
    (N'No se puede quitar el rol de Administrador al último administrador activo. Asigná el rol Administrador a otro usuario antes de cambiar este.', N'No se puede quitar el rol de Administrador al último administrador activo. Asigne el rol Administrador a otro usuario antes de cambiar este.'),
    (N'Solo un Administrador puede modificar el plan o el vencimiento de un cliente directamente. Para activar una suscripción nueva, usá el módulo de Contratación.', N'Solo un Administrador puede modificar el plan o el vencimiento de un cliente directamente. Para activar una suscripción nueva, use el módulo de Contratación.'),
    (N'Renovar o cambiar el plan extiende la suscripción y tiene que pasar por Caja: registrá una Nueva Contratación. Desde Renovación solo puede hacerlo un Administrador.', N'Renovar o cambiar el plan extiende la suscripción y tiene que pasar por Caja: registre una Nueva Contratación. Desde Renovación solo puede hacerlo un Administrador.'),
    (N'No hay un espejo de integridad disponible para reparar. Usá «Recalcular» o restaurá un backup.', N'No hay un espejo de integridad disponible para reparar. Use «Recalcular» o restaure un backup.'),
    (N'El daño detectado no se puede reparar desde el espejo (hay registros eliminados físicamente). Restaurá un backup para recuperarlos.', N'El daño detectado no se puede reparar desde el espejo (hay registros eliminados físicamente). Restaure un backup para recuperarlos.'),
    (N'Debés ingresar una contraseña para cifrar el backup.', N'Debe ingresar una contraseña para cifrar el backup.'),
    (N'No tenés permiso para consultar la bitácora del sistema.', N'No tiene permiso para consultar la bitácora del sistema.'),
    (N'No tenés permiso para ejecutar operaciones sobre los Dígitos Verificadores. Esta acción es exclusiva del Administrador y quedó registrada.', N'No tiene permiso para ejecutar operaciones sobre los Dígitos Verificadores. Esta acción es exclusiva del Administrador y quedó registrada.'),
    (N'La prenda ''{0}'' ya no está disponible. Actualizá la selección e intentá de nuevo.', N'La prenda ''{0}'' ya no está disponible. Actualice la selección e intente de nuevo.'),
    (N'El cliente ya tiene otro pedido en curso: otro operador lo registró recién. Actualizá la lista.', N'El cliente ya tiene otro pedido en curso: otro operador lo registró recién. Actualice la lista.'),
    (N'Seleccioná un Rol del árbol para crearle un sub-rol.', N'Seleccione un Rol del árbol para crearle un sub-rol.'),
    (N'El sub-rol ''{0}'' se creó pero no se pudo vincular a ''{1}'' ({2}), y tampoco se pudo deshacer automáticamente. Revisalo manualmente en la lista de Roles.', N'El sub-rol ''{0}'' se creó pero no se pudo vincular a ''{1}'' ({2}), y tampoco se pudo deshacer automáticamente. Revíselo manualmente en la lista de Roles.'),
    (N'Elegí un permiso o rol de la lista para asignar.', N'Elija un permiso o rol de la lista para asignar.'),
    (N'Seleccioná un ítem que cuelgue de un rol para quitarlo.', N'Seleccione un ítem que cuelgue de un rol para quitarlo.'),
    (N'La suscripción ya está pausada. Reanudala antes de pedir una nueva pausa.', N'La suscripción ya está pausada. Reanúdela antes de pedir una nueva pausa.'),
    (N'Los cargos pendientes del cliente ya fueron cobrados por otra sesión. Actualizá y reintentá.', N'Los cargos pendientes del cliente ya fueron cobrados por otra sesión. Actualice y reintente.'),
    (N'El crédito por referido del cliente ya se aplicó en otro cobro. Actualizá y volvé a calcular el importe.', N'El crédito por referido del cliente ya se aplicó en otro cobro. Actualice y vuelva a calcular el importe.'),
    (N'Otra sesión de Caja ya resolvió esta contratación. Actualizá la cola de Caja.', N'Otra sesión de Caja ya resolvió esta contratación. Actualice la cola de Caja.'),
    (N'El cobro de la contratación #{0} quedó registrado pero la suscripción NO se activó. Avisá al Administrador para completar el alta del plan.', N'El cobro de la contratación #{0} quedó registrado pero la suscripción NO se activó. Avise al Administrador para completar el alta del plan.'),
    (N'Seleccioná un cliente y un plan para continuar.', N'Seleccione un cliente y un plan para continuar.'),
    (N'Seleccioná el medio de pago antes de cobrar.', N'Seleccione el medio de pago antes de cobrar.'),
    (N'No se pudo guardar el archivo: puede estar abierto en otro programa (por ejemplo, Excel). Cerralo e intentá de nuevo.', N'No se pudo guardar el archivo: puede estar abierto en otro programa (por ejemplo, Excel). Ciérrelo e intente de nuevo.'),
    (N'No se pudo guardar el archivo: no tenés permisos para escribir en esa ubicación.', N'No se pudo guardar el archivo: no tiene permisos para escribir en esa ubicación.'),
    (N'La promoción cambió de estado desde otra sesión. Actualizá la lista e intentá de nuevo.', N'La promoción cambió de estado desde otra sesión. Actualice la lista e intente de nuevo.'),
    (N'La sugerencia seleccionada ya fue evaluada. Actualizá la lista de sugerencias.', N'La sugerencia seleccionada ya fue evaluada. Actualice la lista de sugerencias.'),
    (N'El estado de la prenda cambió desde que se consultó. Actualizá la pantalla e intentá de nuevo.', N'El estado de la prenda cambió desde que se consultó. Actualice la pantalla e intente de nuevo.'),
    (N'No se encontró ningún cliente con esa identificación. Verificá el DNI, el nombre o el apellido.', N'No se encontró ningún cliente con esa identificación. Verifique el DNI, el nombre o el apellido.'),
    (N'Hay varios clientes que coinciden: elegí el correcto en la lista.', N'Hay varios clientes que coinciden: elija el correcto en la lista.'),
    (N'Ajustá la selección: las prendas faltantes ya no figuran y las alternativas sugeridas están resaltadas.', N'Ajuste la selección: las prendas faltantes ya no figuran y las alternativas sugeridas están resaltadas.'),
    (N'Planilla de control de existencias — seleccioná un pedido de la cola', N'Planilla de control de existencias — seleccione un pedido de la cola'),
    (N'Pedido #{0}: {1} prenda(s) confirmadas como disponibles. Separalas para reservarlas.', N'Pedido #{0}: {1} prenda(s) confirmadas como disponibles. Sepárelas para reservarlas.'),
    (N'Todas las prendas del Pedido #{0} están disponibles: confirmalas en lugar de informar faltantes.', N'Todas las prendas del Pedido #{0} están disponibles: confírmelas en lugar de informar faltantes.'),
    (N'La prenda ''{0}'' no está disponible: informá los faltantes en lugar de confirmar.', N'La prenda ''{0}'' no está disponible: informe los faltantes en lugar de confirmar.'),
    (N'Confirmá las prendas disponibles del Pedido #{0} antes de separarlas.', N'Confirme las prendas disponibles del Pedido #{0} antes de separarlas.'),
    (N'El Pedido #{0} cambió de estado en otra sesión. Actualizá la lista y volvé a intentarlo.', N'El Pedido #{0} cambió de estado en otra sesión. Actualice la lista y vuelva a intentarlo.'),
    (N'No se puede dar de baja a {0}: tiene un pedido en curso. Esperá a que termine su ciclo o cancelalo primero.', N'No se puede dar de baja a {0}: tiene un pedido en curso. Espere a que termine su ciclo o cancélelo primero.'),
    (N'Indicá el motivo por el que no se concretó el pago.', N'Indique el motivo por el que no se concretó el pago.'),
    (N'El cliente no está registrado. Registralo para continuar con la contratación.', N'El cliente no está registrado. Regístrelo para continuar con la contratación.'),
    (N'El importe a cobrar cambió desde la liquidación ({0:C2} → {1:C2}). Volvé a calcular el importe y confirmá de nuevo.', N'El importe a cobrar cambió desde la liquidación ({0:C2} → {1:C2}). Vuelva a calcular el importe y confirme de nuevo.'),
    (N'Elegí un ítem:', N'Elija un ítem:'),
    (N'Otra sesión ya procesó el cobro de este cliente. Actualizá la lista y verificá su vencimiento.', N'Otra sesión ya procesó el cobro de este cliente. Actualice la lista y verifique su vencimiento.'),
    (N'Demasiados intentos fallidos en esta sesión. Reiniciá la aplicación para volver a intentarlo.', N'Demasiados intentos fallidos en esta sesión. Reinicie la aplicación para volver a intentarlo.'),
    (N'La anotación cambió en otra sesión. Actualizá la lista y volvé a intentarlo.', N'La anotación cambió en otra sesión. Actualice la lista y vuelva a intentarlo.'),
    (N'La operación ''{0}'' no se puede revertir desde el historial: usá la acción correspondiente del pedido (reactivar, cancelar o registrar la devolución).', N'La operación ''{0}'' no se puede revertir desde el historial: use la acción correspondiente del pedido (reactivar, cancelar o registrar la devolución).'),
    (N'No se pudo verificar tu usuario contra la base de datos. La operación no se ejecutó.', N'No se pudo verificar su usuario contra la base de datos. La operación no se ejecutó.'),
    (N'Tu usuario fue desactivado o cambió de rol. Cerrá la sesión y volvé a ingresar.', N'Su usuario fue desactivado o cambió de rol. Cierre la sesión y vuelva a ingresar.'),
    (N'No se puede pausar ni dar de baja la suscripción: el cliente tiene un pedido en curso. Esperá a que termine su ciclo o cancelalo.', N'No se puede pausar ni dar de baja la suscripción: el cliente tiene un pedido en curso. Espere a que termine su ciclo o cancélelo.'),
    (N'Solo podés cambiar tu propia preferencia de idioma.', N'Solo puede cambiar su propia preferencia de idioma.'),
    (N'Seleccioná una promoción para analizar su margen e impacto.', N'Seleccione una promoción para analizar su margen e impacto.'),
    (N'Creaste esta promoción: la tiene que dictaminar otro usuario de Contabilidad.', N'Usted creó esta promoción: la tiene que dictaminar otro usuario de Contabilidad.'),
    (N'Indicá el motivo por el que se anula la contratación.', N'Indique el motivo por el que se anula la contratación.'),
    (N'La fecha de fin ({0:d}) ya pasó. Elegí una fecha de hoy en adelante.', N'La fecha de fin ({0:d}) ya pasó. Elija una fecha de hoy en adelante.'),
    (N'La promoción terminó el {0:d}: no se puede aprobar. Rechazala para que Administración la reformule con fechas nuevas.', N'La promoción terminó el {0:d}: no se puede aprobar. Recházela para que Administración la reformule con fechas nuevas.'),
    (N'Indicá el medio de pago con el que abonó el cliente.', N'Indique el medio de pago con el que abonó el cliente.'),
    (N'No se puede renombrar el rol ''{0}'': está asignado a {1} usuario(s) ({2}). Reasigná esos usuarios a otro rol antes de renombrarlo.', N'No se puede renombrar el rol ''{0}'': está asignado a {1} usuario(s) ({2}). Reasigne esos usuarios a otro rol antes de renombrarlo.'),
    (N'Para la ayuda general, presioná F1 en el menú principal.', N'Para la ayuda general, presione F1 en el menú principal.'),
    (N'WardrobeFlow gestiona el alquiler de prendas por suscripción.' + NCHAR(10) + N'' + NCHAR(10) + N'• El menú muestra solo las opciones que tu rol tiene permitidas.' + NCHAR(10) + N'• En cualquier pantalla, F1 (o Sesión › Ayuda) abre la ayuda de esa pantalla.' + NCHAR(10) + N'• Los mensajes de resultado aparecen en la barra inferior de cada pantalla (verde: correcto, rojo: error).' + NCHAR(10) + N'• Las confirmaciones de acciones que no se pueden deshacer traen "No" como opción por defecto.' + NCHAR(10) + N'• Mi Perfil permite cambiar el idioma, la fuente, el tema y el formato de fecha.' + NCHAR(10) + N'' + NCHAR(10) + N'Procesos principales:' + NCHAR(10) + N'• PN01 Armar pedido: Vendedor (Pedidos de venta) y Depósito (Control de stock).' + NCHAR(10) + N'• PN02 Comercialización: Vendedor (Nueva contratación) y Caja (Cobro de contrataciones).' + NCHAR(10) + N'• PN03 Promociones: Gerencia sugiere, Administración Comercial crea, Contabilidad aprueba.' + NCHAR(10) + N'• PN04 Devoluciones: Depósito inspecciona las prendas devueltas y reporta las perdidas.' + NCHAR(10) + N'• N01 Suscripciones: Caja registra el cobro de las renovaciones (Cobro de renovaciones).', N'WardrobeFlow gestiona el alquiler de prendas por suscripción.' + NCHAR(10) + N'' + NCHAR(10) + N'• El menú muestra solo las opciones que su rol tiene permitidas.' + NCHAR(10) + N'• En cualquier pantalla, F1 (o Sesión › Ayuda) abre la ayuda de esa pantalla.' + NCHAR(10) + N'• Los mensajes de resultado aparecen en la barra inferior de cada pantalla (verde: correcto, rojo: error).' + NCHAR(10) + N'• Las confirmaciones de acciones que no se pueden deshacer traen "No" como opción por defecto.' + NCHAR(10) + N'• Mi Perfil permite cambiar el idioma, la fuente, el tema y el formato de fecha.' + NCHAR(10) + N'' + NCHAR(10) + N'Procesos principales:' + NCHAR(10) + N'• PN01 Armar pedido: Vendedor (Pedidos de venta) y Depósito (Control de stock).' + NCHAR(10) + N'• PN02 Comercialización: Vendedor (Nueva contratación) y Caja (Cobro de contrataciones).' + NCHAR(10) + N'• PN03 Promociones: Gerencia sugiere, Administración Comercial crea, Contabilidad aprueba.' + NCHAR(10) + N'• PN04 Devoluciones: Depósito inspecciona las prendas devueltas y reporta las perdidas.' + NCHAR(10) + N'• N01 Suscripciones: Caja registra el cobro de las renovaciones (Cobro de renovaciones).'),
    (N'Completá nombre, apellido, DNI y, opcionalmente, email y cliente que lo refirió.' + NCHAR(10) + N'Al guardar se valida todo; si hay un error se muestra abajo y el formulario sigue abierto.', N'Complete nombre, apellido, DNI y, opcionalmente, email y cliente que lo refirió.' + NCHAR(10) + N'Al guardar se valida todo; si hay un error se muestra abajo y el formulario sigue abierto.'),
    (N'Circuito del pedido desde el lado del Vendedor.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Nuevo pedido: elegís el cliente (debe tener la suscripción vigente y ningún pedido en curso) y las prendas dentro de su cupo. Se envía a control de stock.' + NCHAR(10) + N'2. Depósito controla: si faltan prendas, el pedido vuelve "Con faltantes": Ver faltantes, y el cliente ajusta la selección o desiste.' + NCHAR(10) + N'3. Cuando Depósito separa las prendas: Formalizar.' + NCHAR(10) + N'' + NCHAR(10) + N'• Documentos ▾: planilla, informe de faltantes, constancias, confirmación del pedido y aviso de desistimiento.' + NCHAR(10) + N'• Cancelar: pedidos en control de stock, separados o pendientes de despacho (con motivo).' + NCHAR(10) + N'• El buscador filtra por número, cliente, vendedor o estado.', N'Circuito del pedido desde el lado del Vendedor.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Nuevo pedido: elija el cliente (debe tener la suscripción vigente y ningún pedido en curso) y las prendas dentro de su cupo. Se envía a control de stock.' + NCHAR(10) + N'2. Depósito controla: si faltan prendas, el pedido vuelve "Con faltantes": Ver faltantes, y el cliente ajusta la selección o desiste.' + NCHAR(10) + N'3. Cuando Depósito separa las prendas: Formalizar.' + NCHAR(10) + N'' + NCHAR(10) + N'• Documentos ▾: planilla, informe de faltantes, constancias, confirmación del pedido y aviso de desistimiento.' + NCHAR(10) + N'• Cancelar: pedidos en control de stock, separados o pendientes de despacho (con motivo).' + NCHAR(10) + N'• El buscador filtra por número, cliente, vendedor o estado.'),
    (N'1. Buscá al cliente: se verifica que la suscripción esté vigente y que no tenga otro pedido en curso.' + NCHAR(10) + N'2. Elegí las prendas disponibles; el cupo del plan (prendas en uso + seleccionadas) se controla en el momento.' + NCHAR(10) + N'3. Enviar a control de stock. Si la selección excede el cupo, el cliente puede corregirla o desistir.', N'1. Busque al cliente: se verifica que la suscripción esté vigente y que no tenga otro pedido en curso.' + NCHAR(10) + N'2. Elija las prendas disponibles; el cupo del plan (prendas en uso + seleccionadas) se controla en el momento.' + NCHAR(10) + N'3. Enviar a control de stock. Si la selección excede el cupo, el cliente puede corregirla o desistir.'),
    (N'1. Identificá al cliente por DNI o nombre. Si no está registrado, Registrar cliente.' + NCHAR(10) + N'2. Elegí el plan y la modalidad (mensual, trimestral o anual): se muestra el importe a abonar con el descuento y los cargos pendientes que correspondan.' + NCHAR(10) + N'3. Registrar: la contratación queda pendiente de pago hasta que Caja la cobre.' + NCHAR(10) + N'' + NCHAR(10) + N'• Si el cliente pasa a un plan más caro con su plan vigente, el plan nuevo rige desde hoy y se descuentan los días no usados.' + NCHAR(10) + N'• A un plan igual o más barato se cambia al vencer el período pagado.' + NCHAR(10) + N'• Si el cliente no contrata: Asentar desistimiento (con motivo).', N'1. Identifique al cliente por DNI o nombre. Si no está registrado, Registrar cliente.' + NCHAR(10) + N'2. Elija el plan y la modalidad (mensual, trimestral o anual): se muestra el importe a abonar con el descuento y los cargos pendientes que correspondan.' + NCHAR(10) + N'3. Registrar: la contratación queda pendiente de pago hasta que Caja la cobre.' + NCHAR(10) + N'' + NCHAR(10) + N'• Si el cliente pasa a un plan más caro con su plan vigente, el plan nuevo rige desde hoy y se descuentan los días no usados.' + NCHAR(10) + N'• A un plan igual o más barato se cambia al vencer el período pagado.' + NCHAR(10) + N'• Si el cliente no contrata: Asentar desistimiento (con motivo).'),
    (N'Cola de contrataciones pendientes de pago.' + NCHAR(10) + N'' + NCHAR(10) + N'• Elegí el medio de pago (con tarjeta de crédito, las cuotas) y Cobrar: se emite el comprobante y la suscripción queda activa. El total incluye los cargos por daño o pérdida pendientes del cliente.' + NCHAR(10) + N'• Intento fallido: registra el intento con motivo; al tercero la contratación se cancela.' + NCHAR(10) + N'• Anular: cancela una contratación sin cobrarla (el cliente se arrepintió o hubo un error de carga), con motivo.' + NCHAR(10) + N'• Quien registró la contratación no puede cobrarla (salvo el Administrador).' + NCHAR(10) + N'• En la vista de resueltas se reimprimen el comprobante y las constancias.', N'Cola de contrataciones pendientes de pago.' + NCHAR(10) + N'' + NCHAR(10) + N'• Elija el medio de pago (con tarjeta de crédito, las cuotas) y Cobrar: se emite el comprobante y la suscripción queda activa. El total incluye los cargos por daño o pérdida pendientes del cliente.' + NCHAR(10) + N'• Intento fallido: registra el intento con motivo; al tercero la contratación se cancela.' + NCHAR(10) + N'• Anular: cancela una contratación sin cobrarla (el cliente se arrepintió o hubo un error de carga), con motivo.' + NCHAR(10) + N'• Quien registró la contratación no puede cobrarla (salvo el Administrador).' + NCHAR(10) + N'• En la vista de resueltas se reimprimen el comprobante y las constancias.'),
    (N'Cobro recurrente de las suscripciones vencidas o próximas a vencer.' + NCHAR(10) + N'' + NCHAR(10) + N'• Elegí el cliente y la modalidad: se muestra el total a cobrar (período − descuento + cargos por daño o pérdida).' + NCHAR(10) + N'• Cobrado: indicá el medio de pago; se emite el comprobante y se extiende la vigencia.' + NCHAR(10) + N'• Pago fallido: el cliente entra en período de gracia y, si ya estaba en gracia, se suspende.', N'Cobro recurrente de las suscripciones vencidas o próximas a vencer.' + NCHAR(10) + N'' + NCHAR(10) + N'• Elija el cliente y la modalidad: se muestra el total a cobrar (período − descuento + cargos por daño o pérdida).' + NCHAR(10) + N'• Cobrado: indique el medio de pago; se emite el comprobante y se extiende la vigencia.' + NCHAR(10) + N'• Pago fallido: el cliente entra en período de gracia y, si ya estaba en gracia, se suspende.'),
    (N'Renovar o cambiar de plan sin pasar por Caja es solo para el Administrador (correcciones). El Vendedor puede pausar o dar de baja; para cambiar de plan, registrá una Nueva contratación.', N'Renovar o cambiar de plan sin pasar por Caja es solo para el Administrador (correcciones). El Vendedor puede pausar o dar de baja; para cambiar de plan, registre una Nueva contratación.'),
    (N'Alta y edición de planes (nombre, límite de prendas y precio mensual).' + NCHAR(10) + N'' + NCHAR(10) + N'• Elegí un plan y Editar plan (o doble clic) para cambiarlo.' + NCHAR(10) + N'• Desactivar: no se permite si hay clientes activos con el plan o contrataciones pendientes de pago.', N'Alta y edición de planes (nombre, límite de prendas y precio mensual).' + NCHAR(10) + N'' + NCHAR(10) + N'• Elija un plan y Editar plan (o doble clic) para cambiarlo.' + NCHAR(10) + N'• Desactivar: no se permite si hay clientes activos con el plan o contrataciones pendientes de pago.'),
    (N'1. Elegí el período y Analizar métricas: abandono por plan, rotación por categoría e impacto de las promociones en los cobros del período.' + NCHAR(10) + N'2. Si hay oportunidad por plan, usá la idea propuesta (se completa el formulario) o cargala a mano. Las oportunidades por categoría son solo informativas: las promociones son siempre por plan.' + NCHAR(10) + N'3. Enviar a Administración. La sugerencia se imprime con Imprimir sugerencia.' + NCHAR(10) + N'4. La retención esperada (30 % por defecto) define el beneficio estimado: ingreso en riesgo × retención. El reporte incluye el Resultado de las promociones: bajas, pausas y renovaciones del plan durante la vigencia contra el período anterior, y la retención real contra la esperada.', N'1. Elija el período y Analizar métricas: abandono por plan, rotación por categoría e impacto de las promociones en los cobros del período.' + NCHAR(10) + N'2. Si hay oportunidad por plan, use la idea propuesta (se completa el formulario) o cárguela a mano. Las oportunidades por categoría son solo informativas: las promociones son siempre por plan.' + NCHAR(10) + N'3. Enviar a Administración. La sugerencia se imprime con Imprimir sugerencia.' + NCHAR(10) + N'4. La retención esperada (30 % por defecto) define el beneficio estimado: ingreso en riesgo × retención. El reporte incluye el Resultado de las promociones: bajas, pausas y renovaciones del plan durante la vigencia contra el período anterior, y la retención real contra la esperada.'),
    (N'• Sugerencias: crear la promoción a partir de una sugerencia o descartarla (con motivo).' + NCHAR(10) + N'• Solicitudes de baja pendientes: Atender cada pedido de Ventas con una respuesta; si estás de acuerdo, la promoción se da de baja, si no, sigue vigente.' + NCHAR(10) + N'• Promociones: crear (siempre para un plan), reformular las rechazadas por Contabilidad, o Dar de baja una vigente o una rechazada (con motivo).' + NCHAR(10) + N'• Imprimir ▾: ficha, dictamen, solicitud de baja, respuesta a la solicitud y constancia de baja.' + NCHAR(10) + N'La fecha de fin no puede haber pasado.', N'• Sugerencias: crear la promoción a partir de una sugerencia o descartarla (con motivo).' + NCHAR(10) + N'• Solicitudes de baja pendientes: Atender cada pedido de Ventas con una respuesta; si está de acuerdo, la promoción se da de baja, si no, sigue vigente.' + NCHAR(10) + N'• Promociones: crear (siempre para un plan), reformular las rechazadas por Contabilidad, o Dar de baja una vigente o una rechazada (con motivo).' + NCHAR(10) + N'• Imprimir ▾: ficha, dictamen, solicitud de baja, respuesta a la solicitud y constancia de baja.' + NCHAR(10) + N'La fecha de fin no puede haber pasado.'),
    (N'Avisos que necesitan atención según tu rol: suscripciones vencidas o por vencer, pedidos esperando control de stock o con faltantes, prendas en limpieza, contrataciones pendientes de pago, integridad de datos y copias de seguridad. Actualizar las vuelve a calcular.', N'Avisos que necesitan atención según su rol: suscripciones vencidas o por vencer, pedidos esperando control de stock o con faltantes, prendas en limpieza, contrataciones pendientes de pago, integridad de datos y copias de seguridad. Actualizar las vuelve a calcular.'),
    (N'Elegí el medio de pago preferido del cliente.', N'Elija el medio de pago preferido del cliente.'),
    (N'El medio de pago elegido no está disponible. Elegí otro del catálogo.', N'El medio de pago elegido no está disponible. Elija otro del catálogo.'),
    (N'Elegí el plan al que aplica la promoción.', N'Elija el plan al que aplica la promoción.'),
    (N'La promoción cambió desde otra sesión (ya no está Vigente o ya tiene una solicitud de baja pendiente). Actualizá la lista.', N'La promoción cambió desde otra sesión (ya no está Vigente o ya tiene una solicitud de baja pendiente). Actualice la lista.'),
    (N'La solicitud de baja ya fue atendida. Actualizá la lista de solicitudes.', N'La solicitud de baja ya fue atendida. Actualice la lista de solicitudes.'),
    (N'Elegí el plan al que apunta la sugerencia.', N'Elija el plan al que apunta la sugerencia.'),
    (N'Ventas pide dar de baja ''{0}''.' + NCHAR(10) + N'Motivo: {1}' + NCHAR(10) + N'' + NCHAR(10) + N'¿Estás de acuerdo con darla de baja?' + NCHAR(10) + N'' + NCHAR(10) + N'Sí: se da de baja.  No: sigue vigente.', N'Ventas pide dar de baja ''{0}''.' + NCHAR(10) + N'Motivo: {1}' + NCHAR(10) + N'' + NCHAR(10) + N'¿Está de acuerdo con darla de baja?' + NCHAR(10) + N'' + NCHAR(10) + N'Sí: se da de baja.  No: sigue vigente.'),
    (N'No podés resolver esta contratación: la registraste vos (la resuelve otra persona de Caja) o tu usuario no tiene un Empleado vinculado.', N'No puede resolver esta contratación: la registró usted (la resuelve otra persona de Caja) o su usuario no tiene un Empleado vinculado.'),
    (N'No tenés permiso para ningún reporte de analítica.', N'No tiene permiso para ningún reporte de analítica.'),
    (N'Reportes para la toma de decisiones. El selector muestra solo los reportes que tu rol tiene permitidos.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Elegí el reporte y, si tiene, su filtro: criterio de riesgo (Análisis de Abandono), umbral mínimo (Escasez de Stock) o cliente (Recomendación de Prendas).' + NCHAR(10) + N'2. Generar: el resultado se muestra en la grilla, con un resumen arriba.' + NCHAR(10) + N'3. Exportar a PDF (vista previa) o Guardar como .CSV.' + NCHAR(10) + N'' + NCHAR(10) + N'• Gerencia Comercial: Análisis de Abandono y Ventas por Vendedor.' + NCHAR(10) + N'• Gerencia de Inventario: Rotación de Prendas, Tiempos de Mantenimiento y Escasez de Stock.' + NCHAR(10) + N'• Ventas: Recomendación de Prendas.', N'Reportes para la toma de decisiones. El selector muestra solo los reportes que su rol tiene permitidos.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Elija el reporte y, si tiene, su filtro: criterio de riesgo (Análisis de Abandono), umbral mínimo (Escasez de Stock) o cliente (Recomendación de Prendas).' + NCHAR(10) + N'2. Generar: el resultado se muestra en la grilla, con un resumen arriba.' + NCHAR(10) + N'3. Exportar a PDF (vista previa) o Guardar como .CSV.' + NCHAR(10) + N'' + NCHAR(10) + N'• Gerencia Comercial: Análisis de Abandono y Ventas por Vendedor.' + NCHAR(10) + N'• Gerencia de Inventario: Rotación de Prendas, Tiempos de Mantenimiento y Escasez de Stock.' + NCHAR(10) + N'• Ventas: Recomendación de Prendas.'),
    (N'   • {0}: no se pudo calcular su resultado. Volvé a analizar las métricas.', N'   • {0}: no se pudo calcular su resultado. Vuelva a analizar las métricas.'),
    (N'No se pudieron leer las promociones: volvé a analizar las métricas.', N'No se pudieron leer las promociones: vuelva a analizar las métricas.'),
    (N'Se cargaron los filtros del reporte: revisalos y pulsá Generar.', N'Se cargaron los filtros del reporte: revíselos y pulse Generar.'),
    (N'Reparalos desde Administrar → Diagnóstico de Integridad antes de respaldar.' + NCHAR(10) + N'' + NCHAR(10) + N'', N'Repárelos desde Administrar → Diagnóstico de Integridad antes de respaldar.' + NCHAR(10) + N'' + NCHAR(10) + N''),
    (N'Pedíle al Administrador que configure el vínculo.', N'Pídale al Administrador que configure el vínculo.'),
    (N'No tenés permiso para gestionar usuarios y permisos.', N'No tiene permiso para gestionar usuarios y permisos.'),
    (N'Para activar una suscripción nueva, usá el módulo de Contratación.', N'Para activar una suscripción nueva, use el módulo de Contratación.'),
    (N'y ese plan solo permite {2}. Registrá las devoluciones primero.', N'y ese plan solo permite {2}. Registre las devoluciones primero.'),
    (N'Ejecutá el instalador (script de la base) antes de usar el sistema.', N'Ejecute el instalador (script de la base) antes de usar el sistema.'),
    (N'(dígito verificador inválido). Reiniciá el sistema para reparar la integridad antes de continuar.', N'(dígito verificador inválido). Reinicie el sistema para reparar la integridad antes de continuar.'),
    (N'No tenés permiso para ejecutar operaciones sobre los Dígitos Verificadores. ', N'No tiene permiso para ejecutar operaciones sobre los Dígitos Verificadores. '),
    (N'Avisá al Administrador para completar el alta del plan.', N'Avise al Administrador para completar el alta del plan.'),
    (N'Asigná la gestión a otro rol o usuario antes de continuar.', N'Asigne la gestión a otro rol o usuario antes de continuar.'),
    (N'No podés agregarte a vos mismo permisos que tu rol no tiene hoy, editando ', N'No puede agregarse a sí mismo permisos que su rol no tiene hoy, editando '),
    (N'tu propio rol. Pedile a otro administrador que lo haga.', N'su propio rol. Pídale a otro administrador que lo haga.'),
    (N'Reasigná esos usuarios a otro rol antes de renombrarlo.', N'Reasigne esos usuarios a otro rol antes de renombrarlo.'),
    (N'Reasigná esos usuarios a otro rol antes de eliminarlo.', N'Reasigne esos usuarios a otro rol antes de eliminarlo.'),
    (N'Esperá a que termine su ciclo o cancelalo.', N'Espere a que termine su ciclo o cancélelo.'),
    (N'Confirmá la entrega antes de crear uno nuevo.', N'Confirme la entrega antes de crear uno nuevo.'),
    (N'La operación ''{0}'' no se puede revertir desde el historial: usá la acción correspondiente ', N'La operación ''{0}'' no se puede revertir desde el historial: use la acción correspondiente '),
    (N'Reasignalos a otro plan antes de desactivarlo.', N'Reasígnelos a otro plan antes de desactivarlo.'),
    (N'La fecha de fin ({0:dd/MM/yyyy}) ya pasó. Elegí una fecha de hoy en adelante.', N'La fecha de fin ({0:dd/MM/yyyy}) ya pasó. Elija una fecha de hoy en adelante.'),
    (N'La promoción terminó el {0:dd/MM/yyyy}: no se puede aprobar. Rechazala para que Administración la reformule con fechas nuevas.', N'La promoción terminó el {0:dd/MM/yyyy}: no se puede aprobar. Recházela para que Administración la reformule con fechas nuevas.'),
    (N'usá "Asumir pérdida" para recalcularlos.', N'use "Asumir pérdida" para recalcularlos.'),
    (N'Restaurá un backup para recuperarlos.', N'Restaure un backup para recuperarlos.'),
    (N'registrá una Nueva Contratación. Desde Renovación solo puede hacerlo un Administrador.', N'registre una Nueva Contratación. Desde Renovación solo puede hacerlo un Administrador.'),
    (N'Asigná el rol Administrador a otro usuario antes de cambiar este.', N'Asigne el rol Administrador a otro usuario antes de cambiar este.'),
    (N'Creá o activá otro Administrador antes de archivar este.', N'Cree o active otro Administrador antes de archivar este.'),
    (N'Reiniciá la aplicación para volver a intentarlo.', N'Reinicie la aplicación para volver a intentarlo.'),
    (N'Contactá al Administrador (o usá una clave de emergencia) para reactivarla.', N'Contacte al Administrador (o use una clave de emergencia) para reactivarla.'),
    (N'Reintentá en {minutos} minuto(s) o usá una clave de emergencia.', N'Reintente en {minutos} minuto(s) o use una clave de emergencia.'),
    (N'Verificá que la sección <connectionStrings> esté correctamente configurada.', N'Verifique que la sección <connectionStrings> esté correctamente configurada.'),
    (N'• El menú muestra solo las opciones que tu rol tiene permitidas.' + NCHAR(10) + N'', N'• El menú muestra solo las opciones que su rol tiene permitidas.' + NCHAR(10) + N''),
    (N'Completá nombre, apellido, DNI y, opcionalmente, email y cliente que lo refirió.' + NCHAR(10) + N'', N'Complete nombre, apellido, DNI y, opcionalmente, email y cliente que lo refirió.' + NCHAR(10) + N''),
    (N'1. Nuevo pedido: elegís el cliente (debe tener la suscripción vigente y ningún pedido en curso) y las prendas dentro de su cupo. Se envía a control de stock.' + NCHAR(10) + N'', N'1. Nuevo pedido: elija el cliente (debe tener la suscripción vigente y ningún pedido en curso) y las prendas dentro de su cupo. Se envía a control de stock.' + NCHAR(10) + N''),
    (N'1. Buscá al cliente: se verifica que la suscripción esté vigente y que no tenga otro pedido en curso.' + NCHAR(10) + N'', N'1. Busque al cliente: se verifica que la suscripción esté vigente y que no tenga otro pedido en curso.' + NCHAR(10) + N''),
    (N'2. Elegí las prendas disponibles; el cupo del plan (prendas en uso + seleccionadas) se controla en el momento.' + NCHAR(10) + N'', N'2. Elija las prendas disponibles; el cupo del plan (prendas en uso + seleccionadas) se controla en el momento.' + NCHAR(10) + N''),
    (N'1. Identificá al cliente por DNI o nombre. Si no está registrado, Registrar cliente.' + NCHAR(10) + N'', N'1. Identifique al cliente por DNI o nombre. Si no está registrado, Registrar cliente.' + NCHAR(10) + N''),
    (N'2. Elegí el plan y la modalidad (mensual, trimestral o anual): se muestra el importe a abonar con el descuento y los cargos pendientes que correspondan.' + NCHAR(10) + N'', N'2. Elija el plan y la modalidad (mensual, trimestral o anual): se muestra el importe a abonar con el descuento y los cargos pendientes que correspondan.' + NCHAR(10) + N''),
    (N'• Elegí el medio de pago (con tarjeta de crédito, las cuotas) y Cobrar: se emite el comprobante y la suscripción queda activa. El total incluye los cargos por daño o pérdida pendientes del cliente.' + NCHAR(10) + N'', N'• Elija el medio de pago (con tarjeta de crédito, las cuotas) y Cobrar: se emite el comprobante y la suscripción queda activa. El total incluye los cargos por daño o pérdida pendientes del cliente.' + NCHAR(10) + N''),
    (N'• Elegí el cliente y la modalidad: se muestra el total a cobrar (período − descuento + cargos por daño o pérdida).' + NCHAR(10) + N'', N'• Elija el cliente y la modalidad: se muestra el total a cobrar (período − descuento + cargos por daño o pérdida).' + NCHAR(10) + N''),
    (N'• Cobrado: indicá el medio de pago; se emite el comprobante y se extiende la vigencia.' + NCHAR(10) + N'', N'• Cobrado: indique el medio de pago; se emite el comprobante y se extiende la vigencia.' + NCHAR(10) + N''),
    (N'• Elegí un plan y Editar plan (o doble clic) para cambiarlo.' + NCHAR(10) + N'', N'• Elija un plan y Editar plan (o doble clic) para cambiarlo.' + NCHAR(10) + N''),
    (N'1. Elegí el período y Analizar métricas: abandono por plan, rotación por categoría e impacto de las promociones en los cobros del período.' + NCHAR(10) + N'', N'1. Elija el período y Analizar métricas: abandono por plan, rotación por categoría e impacto de las promociones en los cobros del período.' + NCHAR(10) + N''),
    (N'2. Si hay oportunidad por plan, usá la idea propuesta (se completa el formulario) o cargala a mano. Las oportunidades por categoría son solo informativas: las promociones son siempre por plan.' + NCHAR(10) + N'', N'2. Si hay oportunidad por plan, use la idea propuesta (se completa el formulario) o cárguela a mano. Las oportunidades por categoría son solo informativas: las promociones son siempre por plan.' + NCHAR(10) + N''),
    (N'• Solicitudes de baja pendientes: Atender cada pedido de Ventas con una respuesta; si estás de acuerdo, la promoción se da de baja, si no, sigue vigente.' + NCHAR(10) + N'', N'• Solicitudes de baja pendientes: Atender cada pedido de Ventas con una respuesta; si está de acuerdo, la promoción se da de baja, si no, sigue vigente.' + NCHAR(10) + N''),
    (N'Reportes para la toma de decisiones. El selector muestra solo los reportes que tu rol tiene permitidos.' + NCHAR(10) + N'', N'Reportes para la toma de decisiones. El selector muestra solo los reportes que su rol tiene permitidos.' + NCHAR(10) + N''),
    (N'1. Elegí el reporte y, si tiene, su filtro: criterio de riesgo (Análisis de Abandono), umbral mínimo (Escasez de Stock) o cliente (Recomendación de Prendas).' + NCHAR(10) + N'', N'1. Elija el reporte y, si tiene, su filtro: criterio de riesgo (Análisis de Abandono), umbral mínimo (Escasez de Stock) o cliente (Recomendación de Prendas).' + NCHAR(10) + N''),
    (N'Escribí un nombre para el rol.', N'Escriba un nombre para el rol.'),
    (N'Escribí un nombre para el sub-rol.', N'Escriba un nombre para el sub-rol.'),
    (N'Seleccioná un Rol para asignarle un permiso o rol.', N'Seleccione un Rol para asignarle un permiso o rol.'),
    (N'Gestioná tu flujo de' + NCHAR(13) + N'' + NCHAR(10) + N'prendas e información' + NCHAR(13) + N'' + NCHAR(10) + N'de manera simple y eficiente.', N'Gestione su flujo de' + NCHAR(13) + N'' + NCHAR(10) + N'prendas e información' + NCHAR(13) + N'' + NCHAR(10) + N'de manera simple y eficiente.'),
    (N'Ingresá para continuar en tu cuenta.', N'Ingrese para continuar en su cuenta.'),
    (N'¿Olvidaste tu contraseña?', N'¿Olvidó su contraseña?'),
    (N'Ingresá tu nombre de usuario. Un administrador' + NCHAR(10) + N'podrá resetear tu contraseña desde', N'Ingrese su nombre de usuario. Un administrador' + NCHAR(10) + N'podrá resetear su contraseña desde'),
    (N'' + NCHAR(10) + N'' + NCHAR(10) + N'Guardá ese archivo en un lugar seguro.', N'' + NCHAR(10) + N'' + NCHAR(10) + N'Guarde ese archivo en un lugar seguro.'),
    (N'Cada clave es de UN SOLO USO. Guardá este archivo en un lugar seguro.', N'Cada clave es de UN SOLO USO. Guarde este archivo en un lugar seguro.'),
    (N'1. Elegí el período y Analizar métricas: abandono por plan, rotación por categoría e impacto de las promociones en los cobros del período.', N'1. Elija el período y Analizar métricas: abandono por plan, rotación por categoría e impacto de las promociones en los cobros del período.'),
    (N'2. Si hay oportunidad, usá la idea propuesta (se completa el formulario) o cargala a mano.', N'2. Si hay oportunidad, use la idea propuesta (se completa el formulario) o cárguela a mano.'),
    (N'2. Si hay oportunidad por plan, usá la idea propuesta (se completa el formulario) o cargala a mano. Las oportunidades por categoría son solo informativas: las promociones son siempre por plan.', N'2. Si hay oportunidad por plan, use la idea propuesta (se completa el formulario) o cárguela a mano. Las oportunidades por categoría son solo informativas: las promociones son siempre por plan.'),
    (N'• Solicitudes de baja pendientes: Atender cada pedido de Ventas con una respuesta; si estás de acuerdo, la promoción se da de baja, si no, sigue vigente.', N'• Solicitudes de baja pendientes: Atender cada pedido de Ventas con una respuesta; si está de acuerdo, la promoción se da de baja, si no, sigue vigente.'),
    (N'No se puede eliminar a {0}: tiene {1} prenda(s) en uso. Registrá la devolución primero.', N'No se puede eliminar a {0}: tiene {1} prenda(s) en uso. Registre la devolución primero.'),
    (N'Si lo activás, esos textos se mostrarán en el idioma por defecto.', N'Si lo activa, esos textos se mostrarán en el idioma por defecto.'),
    (N'Contactá al Administrador para reactivarla.', N'Contacte al Administrador para reactivarla.'),
    (N'Seleccioná un usuario', N'Seleccione un usuario'),
    (N'en la lista y presioná:', N'en la lista y presione:'),
    (N'bloqueado y presioná:', N'bloqueado y presione:'),
    (N'Ingresá tu nombre de usuario. Un administrador', N'Ingrese su nombre de usuario. Un administrador'),
    (N'podrá resetear tu contraseña desde el sistema.', N'podrá resetear su contraseña desde el sistema.'),
    (N'Asigná un plan en el módulo de Clientes antes de crear un pedido.', N'Asigne un plan en el módulo de Clientes antes de crear un pedido.'),
    (N'Si el usuario existe, contactá al administrador para que resetee', N'Si el usuario existe, contacte al administrador para que resetee'),
    (N'tu contraseña desde Administrar -> Usuarios.', N'su contraseña desde Administrar -> Usuarios.'),
    (N'Reparalos desde Administrar → Diagnóstico de Integridad antes de respaldar.', N'Repárelos desde Administrar → Diagnóstico de Integridad antes de respaldar.'),
    (N'Ya podés iniciar sesión normalmente.', N'Ya puede iniciar sesión normalmente.'),
    (N'Ingresá una contraseña para CIFRAR el backup.', N'Ingrese una contraseña para CIFRAR el backup.'),
    (N'La vas a necesitar para restaurarlo (no se puede recuperar).', N'La va a necesitar para restaurarlo (no se puede recuperar).'),
    (N'• El menú muestra solo las opciones que tu rol tiene permitidas.', N'• El menú muestra solo las opciones que su rol tiene permitidas.'),
    (N'Completá nombre, apellido, DNI y, opcionalmente, email y cliente que lo refirió.', N'Complete nombre, apellido, DNI y, opcionalmente, email y cliente que lo refirió.'),
    (N'1. Nuevo pedido: elegís el cliente (debe tener la suscripción vigente y ningún pedido en curso) y las prendas dentro de su cupo. Se envía a control de stock.', N'1. Nuevo pedido: elija el cliente (debe tener la suscripción vigente y ningún pedido en curso) y las prendas dentro de su cupo. Se envía a control de stock.'),
    (N'1. Buscá al cliente: se verifica que la suscripción esté vigente y que no tenga otro pedido en curso.', N'1. Busque al cliente: se verifica que la suscripción esté vigente y que no tenga otro pedido en curso.'),
    (N'2. Elegí las prendas disponibles; el cupo del plan (prendas en uso + seleccionadas) se controla en el momento.', N'2. Elija las prendas disponibles; el cupo del plan (prendas en uso + seleccionadas) se controla en el momento.'),
    (N'1. Identificá al cliente por DNI o nombre. Si no está registrado, Registrar cliente.', N'1. Identifique al cliente por DNI o nombre. Si no está registrado, Registrar cliente.'),
    (N'2. Elegí el plan y la modalidad (mensual, trimestral o anual): se muestra el importe a abonar con el descuento y los cargos pendientes que correspondan.', N'2. Elija el plan y la modalidad (mensual, trimestral o anual): se muestra el importe a abonar con el descuento y los cargos pendientes que correspondan.'),
    (N'• Elegí el medio de pago (con tarjeta de crédito, las cuotas) y Cobrar: se emite el comprobante y la suscripción queda activa. El total incluye los cargos por daño o pérdida pendientes del cliente.', N'• Elija el medio de pago (con tarjeta de crédito, las cuotas) y Cobrar: se emite el comprobante y la suscripción queda activa. El total incluye los cargos por daño o pérdida pendientes del cliente.'),
    (N'• Elegí el cliente y la modalidad: se muestra el total a cobrar (período − descuento + cargos por daño o pérdida).', N'• Elija el cliente y la modalidad: se muestra el total a cobrar (período − descuento + cargos por daño o pérdida).'),
    (N'• Cobrado: indicá el medio de pago; se emite el comprobante y se extiende la vigencia.', N'• Cobrado: indique el medio de pago; se emite el comprobante y se extiende la vigencia.'),
    (N'• Elegí un plan y Editar plan (o doble clic) para cambiarlo.', N'• Elija un plan y Editar plan (o doble clic) para cambiarlo.'),
    (N'¿Estás de acuerdo con darla de baja?', N'¿Está de acuerdo con darla de baja?'),
    (N'Reportes para la toma de decisiones. El selector muestra solo los reportes que tu rol tiene permitidos.', N'Reportes para la toma de decisiones. El selector muestra solo los reportes que su rol tiene permitidos.'),
    (N'1. Elegí el reporte y, si tiene, su filtro: criterio de riesgo (Análisis de Abandono), umbral mínimo (Escasez de Stock) o cliente (Recomendación de Prendas).', N'1. Elija el reporte y, si tiene, su filtro: criterio de riesgo (Análisis de Abandono), umbral mínimo (Escasez de Stock) o cliente (Recomendación de Prendas).'),
    (N'Gestioná tu flujo de', N'Gestione su flujo de'),
    (N'podrá resetear tu contraseña desde', N'podrá resetear su contraseña desde'),
    (N'Guardá ese archivo en un lugar seguro.', N'Guarde ese archivo en un lugar seguro.'),
    (N'— Seleccioná un cliente —', N'— Seleccione un cliente —'),
    (N'Abrí los formularios que quieras mapear y volvé a entrar acá.', N'Abra los formularios que quiera mapear y vuelva a entrar acá.'),
    (N'Indicá el motivo del cargo.', N'Indique el motivo del cargo.'),
    (N'Ingresá una observación antes de aprobar.', N'Ingrese una observación antes de aprobar.'),
    (N'Ingresá una observación antes de rechazar.', N'Ingrese una observación antes de rechazar.'),
    (N'La prenda ''{0}'' ya no está disponible (estado: {1}). Actualizá la selección.', N'La prenda ''{0}'' ya no está disponible (estado: {1}). Actualice la selección.'),
    (N'No se encontró el usuario ''{0}''.' + NCHAR(10) + N'Verificá que escribiste tu nombre correctamente.', N'No se encontró el usuario ''{0}''.' + NCHAR(10) + N'Verifique que escribió su nombre correctamente.'),
    (N'WardrobeFlow — Tu guardarropa, sin límites.', N'WardrobeFlow — Su guardarropa, sin límites.');

    UPDATE t SET t.Texto = u.TextoNuevo
    FROM Traduccion t
    JOIN Idioma i ON i.IdIdioma = t.IdIdioma
    JOIN @Usted u ON t.Texto COLLATE Latin1_General_BIN2 = u.TextoAnterior COLLATE Latin1_General_BIN2
    WHERE i.Codigo = 'ES';
    PRINT CONCAT('Textos en usted: ', @@ROWCOUNT, ' traducción(es) actualizada(s).');
END
GO
PRINT 'Sección 21z6: textos en español en usted verificados.';
GO

-- ============================================================
-- WardrobeFlow — 21z7. AYUDA DE BACKUP: BACKUPS AUTOMÁTICOS
-- ------------------------------------------------------------
-- La ayuda en línea de la pantalla de Backup ahora explica los backups automáticos (al iniciar
-- sesión según el intervalo configurable y cada 24 h, sin cifrar, rotando auto_domingo.bak …
-- auto_sabado.bak). Las claves nuevas las inserta la aplicación desde traducciones.tsv; acá se
-- actualiza ayuda.BackupForm en una base ya instalada, SOLO si sigue con el texto de fábrica
-- anterior (una edición hecha en Idiomas se respeta). Idempotente.
-- ============================================================
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @AyudaBackup TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                                TextoAnterior NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @AyudaBackup (Codigo, Clave, TextoAnterior, TextoNuevo) VALUES
    (N'ES', N'ayuda.BackupForm', N'Crear, restaurar y eliminar copias de seguridad de la base (solo Administrador). Antes de crear una copia se verifica la integridad de los datos.',
        N'Crear, restaurar y eliminar copias de seguridad de la base (solo Administrador). Antes de crear una copia se verifica la integridad de los datos.' + NCHAR(10) + N'' + NCHAR(10) + N'Backups automáticos (sin cifrar):' + NCHAR(10) + N'• Al iniciar sesión (cualquier rol), si el último backup es más antiguo que el intervalo configurado (60 minutos por defecto; el Administrador lo cambia con "Backup automático al iniciar sesión...").' + NCHAR(10) + N'• Cada 24 horas mientras la aplicación esté abierta.' + NCHAR(10) + N'• Rotan entre auto_domingo.bak … auto_sabado.bak (uno por día de la semana) y se restauran sin contraseña.'),
    (N'EN', N'ayuda.BackupForm', N'Create, restore and delete database backups (Administrator only). Data integrity is checked before creating a backup.',
        N'Create, restore and delete database backups (Administrator only). Data integrity is checked before creating a backup.' + NCHAR(10) + N'' + NCHAR(10) + N'Automatic backups (unencrypted):' + NCHAR(10) + N'• At login (any role), if the last backup is older than the configured interval (60 minutes by default; the Administrator changes it with "Automatic backup at login...").' + NCHAR(10) + N'• Every 24 hours while the application is open.' + NCHAR(10) + N'• They rotate over auto_domingo.bak … auto_sabado.bak (one per day of the week) and are restored without a password.'),
    (N'RU', N'ayuda.BackupForm', N'Создание, восстановление и удаление резервных копий базы (только администратор). Перед созданием копии проверяется целостность данных.',
        N'Создание, восстановление и удаление резервных копий базы (только администратор). Перед созданием копии проверяется целостность данных.' + NCHAR(10) + N'' + NCHAR(10) + N'Автоматические бэкапы (без шифрования):' + NCHAR(10) + N'• При входе в систему (любая роль), если последний бэкап старше настроенного интервала (по умолчанию 60 минут; администратор меняет его кнопкой «Автоматический бэкап при входе...»).' + NCHAR(10) + N'• Каждые 24 часа, пока приложение открыто.' + NCHAR(10) + N'• Чередуются в файлах auto_domingo.bak … auto_sabado.bak (по одному на день недели) и восстанавливаются без пароля.'),
    (N'PT', N'ayuda.BackupForm', N'Criar, restaurar e excluir cópias de segurança do banco (só Administrador). Antes de criar uma cópia, a integridade dos dados é verificada.',
        N'Criar, restaurar e excluir cópias de segurança do banco (só Administrador). Antes de criar uma cópia, a integridade dos dados é verificada.' + NCHAR(10) + N'' + NCHAR(10) + N'Backups automáticos (sem criptografia):' + NCHAR(10) + N'• Ao iniciar sessão (qualquer função), se o último backup for mais antigo que o intervalo configurado (60 minutos por padrão; o Administrador o altera com "Backup automático ao iniciar sessão...").' + NCHAR(10) + N'• A cada 24 horas enquanto a aplicação estiver aberta.' + NCHAR(10) + N'• Alternam entre auto_domingo.bak … auto_sabado.bak (um por dia da semana) e são restaurados sem senha.');

    UPDATE t SET t.Texto = f.TextoNuevo
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @AyudaBackup f ON f.Clave = c.Clave AND f.Codigo = i.Codigo
    WHERE t.Texto COLLATE Latin1_General_BIN2 = f.TextoAnterior COLLATE Latin1_General_BIN2; -- exacto: no pisa ediciones
    PRINT CONCAT('Ayuda de backup: ', @@ROWCOUNT, ' traducción(es) actualizada(s).');
END
GO
PRINT 'Sección 21z7: ayuda de backups automáticos verificada.';
GO

-- ============================================================
-- WardrobeFlow — 21z8. TEXTOS DE LA AUDITORÍA DE LA 1.6.0
-- ------------------------------------------------------------
-- · ayuda.BackupForm: explica "Restablecer datos de demo..." y su backup previo
--   (WardrobeFlow_PrevioDemo_<fecha>_<usuario>.bak).
-- · msg.demo.confirmar: incluye las decisiones comerciales entre lo que se borra y ya no trae
--   el consejo de la defensa (queda solo en Instalador/Credenciales_Iniciales.txt).
-- · err.bll.demo.integridad: la ruta real del menú (Administrar → Sistema → Diagnóstico de Integridad).
-- · ayuda.CentroDecisionesForm y el glosario (glos.mrr/churn/proyeccion.formula): MRR solo de los
--   clientes que pagan hoy (sin pausa, vencimiento ni suspensión), clientes y MRR reconstruidos al
--   fin del período, mes parcial fuera de la proyección y el botón "Simular esta opción".
-- Las claves nuevas las inserta la aplicación desde traducciones.tsv; acá se actualizan las que ya
-- existen en una base instalada, SOLO si siguen con el texto de fábrica anterior (una edición hecha
-- en Idiomas se respeta). Idempotente.
-- ============================================================
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @Textos160 TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                              TextoAnterior NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @Textos160 (Codigo, Clave, TextoAnterior, TextoNuevo) VALUES
    (N'ES', N'ayuda.BackupForm',
        N'Crear, restaurar y eliminar copias de seguridad de la base (solo Administrador). Antes de crear una copia se verifica la integridad de los datos.' + NCHAR(10) + N'' + NCHAR(10) + N'Backups automáticos (sin cifrar):' + NCHAR(10) + N'• Al iniciar sesión (cualquier rol), si el último backup es más antiguo que el intervalo configurado (60 minutos por defecto; el Administrador lo cambia con "Backup automático al iniciar sesión...").' + NCHAR(10) + N'• Cada 24 horas mientras la aplicación esté abierta.' + NCHAR(10) + N'• Rotan entre auto_domingo.bak … auto_sabado.bak (uno por día de la semana) y se restauran sin contraseña.',
        N'Crear, restaurar y eliminar copias de seguridad de la base (solo Administrador). Antes de crear una copia se verifica la integridad de los datos.' + NCHAR(10) + N'' + NCHAR(10) + N'Backups automáticos (sin cifrar):' + NCHAR(10) + N'• Al iniciar sesión (cualquier rol), si el último backup es más antiguo que el intervalo configurado (60 minutos por defecto; el Administrador lo cambia con "Backup automático al iniciar sesión...").' + NCHAR(10) + N'• Cada 24 horas mientras la aplicación esté abierta.' + NCHAR(10) + N'• Rotan entre auto_domingo.bak … auto_sabado.bak (uno por día de la semana) y se restauran sin contraseña.' + NCHAR(10) + N'' + NCHAR(10) + N'Restablecer datos de demo... (solo Administrador): borra los datos de negocio y vuelve a cargar los de prueba con las fechas corridas a hoy. Antes genera una copia sin cifrar WardrobeFlow_PrevioDemo_<fecha>_<usuario>.bak en la carpeta de backups (se restaura como cualquier otra) y, al terminar, verifica la integridad y reinicia la aplicación.'),
    (N'EN', N'ayuda.BackupForm',
        N'Create, restore and delete database backups (Administrator only). Data integrity is checked before creating a backup.' + NCHAR(10) + N'' + NCHAR(10) + N'Automatic backups (unencrypted):' + NCHAR(10) + N'• At login (any role), if the last backup is older than the configured interval (60 minutes by default; the Administrator changes it with "Automatic backup at login...").' + NCHAR(10) + N'• Every 24 hours while the application is open.' + NCHAR(10) + N'• They rotate over auto_domingo.bak … auto_sabado.bak (one per day of the week) and are restored without a password.',
        N'Create, restore and delete database backups (Administrator only). Data integrity is checked before creating a backup.' + NCHAR(10) + N'' + NCHAR(10) + N'Automatic backups (unencrypted):' + NCHAR(10) + N'• At login (any role), if the last backup is older than the configured interval (60 minutes by default; the Administrator changes it with "Automatic backup at login...").' + NCHAR(10) + N'• Every 24 hours while the application is open.' + NCHAR(10) + N'• They rotate over auto_domingo.bak … auto_sabado.bak (one per day of the week) and are restored without a password.' + NCHAR(10) + N'' + NCHAR(10) + N'Reset demo data... (Administrator only): deletes the business data and reloads the sample data with the dates moved to today. It first creates an unencrypted backup WardrobeFlow_PrevioDemo_<date>_<user>.bak in the backups folder (restored like any other) and, when it finishes, checks integrity and restarts the application.'),
    (N'RU', N'ayuda.BackupForm',
        N'Создание, восстановление и удаление резервных копий базы (только администратор). Перед созданием копии проверяется целостность данных.' + NCHAR(10) + N'' + NCHAR(10) + N'Автоматические бэкапы (без шифрования):' + NCHAR(10) + N'• При входе в систему (любая роль), если последний бэкап старше настроенного интервала (по умолчанию 60 минут; администратор меняет его кнопкой «Автоматический бэкап при входе...»).' + NCHAR(10) + N'• Каждые 24 часа, пока приложение открыто.' + NCHAR(10) + N'• Чередуются в файлах auto_domingo.bak … auto_sabado.bak (по одному на день недели) и восстанавливаются без пароля.',
        N'Создание, восстановление и удаление резервных копий базы (только администратор). Перед созданием копии проверяется целостность данных.' + NCHAR(10) + N'' + NCHAR(10) + N'Автоматические бэкапы (без шифрования):' + NCHAR(10) + N'• При входе в систему (любая роль), если последний бэкап старше настроенного интервала (по умолчанию 60 минут; администратор меняет его кнопкой «Автоматический бэкап при входе...»).' + NCHAR(10) + N'• Каждые 24 часа, пока приложение открыто.' + NCHAR(10) + N'• Чередуются в файлах auto_domingo.bak … auto_sabado.bak (по одному на день недели) и восстанавливаются без пароля.' + NCHAR(10) + N'' + NCHAR(10) + N'Сбросить демо-данные... (только администратор): удаляет бизнес-данные и заново загружает тестовые данные с датами, сдвинутыми на сегодня. Сначала создаётся резервная копия без шифрования WardrobeFlow_PrevioDemo_<дата>_<пользователь>.bak в папке бэкапов (восстанавливается как любая другая), а по завершении проверяется целостность и приложение перезапускается.'),
    (N'PT', N'ayuda.BackupForm',
        N'Criar, restaurar e excluir cópias de segurança do banco (só Administrador). Antes de criar uma cópia, a integridade dos dados é verificada.' + NCHAR(10) + N'' + NCHAR(10) + N'Backups automáticos (sem criptografia):' + NCHAR(10) + N'• Ao iniciar sessão (qualquer função), se o último backup for mais antigo que o intervalo configurado (60 minutos por padrão; o Administrador o altera com "Backup automático ao iniciar sessão...").' + NCHAR(10) + N'• A cada 24 horas enquanto a aplicação estiver aberta.' + NCHAR(10) + N'• Alternam entre auto_domingo.bak … auto_sabado.bak (um por dia da semana) e são restaurados sem senha.',
        N'Criar, restaurar e excluir cópias de segurança do banco (só Administrador). Antes de criar uma cópia, a integridade dos dados é verificada.' + NCHAR(10) + N'' + NCHAR(10) + N'Backups automáticos (sem criptografia):' + NCHAR(10) + N'• Ao iniciar sessão (qualquer função), se o último backup for mais antigo que o intervalo configurado (60 minutos por padrão; o Administrador o altera com "Backup automático ao iniciar sessão...").' + NCHAR(10) + N'• A cada 24 horas enquanto a aplicação estiver aberta.' + NCHAR(10) + N'• Alternam entre auto_domingo.bak … auto_sabado.bak (um por dia da semana) e são restaurados sem senha.' + NCHAR(10) + N'' + NCHAR(10) + N'Restabelecer dados de demonstração... (só Administrador): exclui os dados de negócio e carrega novamente os dados de exemplo com as datas ajustadas para hoje. Antes gera uma cópia sem criptografia WardrobeFlow_PrevioDemo_<data>_<usuário>.bak na pasta de backups (restaurada como qualquer outra) e, ao terminar, verifica a integridade e reinicia a aplicação.'),
    (N'ES', N'ayuda.CentroDecisionesForm',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar promoción → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: elija el período (o un atajo) y el plan. Cada indicador se compara con el período anterior de igual duración (▲/▼). Los gráficos muestran la tendencia con 3 meses proyectados (punteado), el ingreso en riesgo, el stock y la rotación.' + NCHAR(10) + N'2. Recomendaciones: qué está pasando, por qué, qué hacer y cuánto vale (con la fórmula). Elija una opción y pulse Aceptar y ejecutar (abre el flujo existente), Descartar (con motivo) o Posponer.' + NCHAR(10) + N'3. Historial: las decisiones tomadas, con el impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? con la retención de equilibrio; compare hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción (mantener, extender o dar de baja).' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: los valores que disparan cada regla. Informe ejecutivo: PDF con todo lo anterior.',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar promoción → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: elija el período (o un atajo) y el plan. Cada indicador se compara con el período anterior de igual duración (▲/▼). Los gráficos muestran la tendencia con 3 meses proyectados (punteado), el ingreso en riesgo, el stock y la rotación.' + NCHAR(10) + N'2. Recomendaciones: qué está pasando, por qué, qué hacer y cuánto vale (con la fórmula). Elija una opción y pulse Aceptar y ejecutar (abre el flujo existente), Descartar (con motivo) o Posponer; Simular esta opción la carga en el simulador.' + NCHAR(10) + N'3. Historial: las decisiones tomadas, con el impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? con la retención de equilibrio; compare hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción (mantener, extender o dar de baja).' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: los valores que disparan cada regla. Informe ejecutivo: PDF con todo lo anterior.'),
    (N'EN', N'ayuda.CentroDecisionesForm',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply promotion → measure results.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: choose the period (or a shortcut) and the plan. Each indicator is compared with the previous period of the same length (▲/▼). The charts show the trend with 3 projected months (dashed), revenue at risk, stock and turnover.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and how much it is worth (with the formula). Choose an option and press Accept and execute (opens the existing flow), Discard (with a reason) or Postpone.' + NCHAR(10) + N'3. History: the decisions taken, with estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? with the break-even retention; compare up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion (keep, extend or withdraw).' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: the values that trigger each rule. Executive report: PDF with all of the above.',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply promotion → measure results.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: choose the period (or a shortcut) and the plan. Each indicator is compared with the previous period of the same length (▲/▼). The charts show the trend with 3 projected months (dashed), revenue at risk, stock and turnover.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and how much it is worth (with the formula). Choose an option and press Accept and execute (opens the existing flow), Discard (with a reason) or Postpone; Simulate this option loads it into the simulator.' + NCHAR(10) + N'3. History: the decisions taken, with estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? with the break-even retention; compare up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion (keep, extend or withdraw).' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: the values that trigger each rule. Executive report: PDF with all of the above.'),
    (N'RU', N'ayuda.CentroDecisionesForm',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → запуск акции → оценка результата.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: выберите период (или быстрый выбор) и тариф. Каждый показатель сравнивается с предыдущим периодом той же длины (▲/▼). Графики: тенденция с прогнозом на 3 месяца (пунктир), доход под риском, запасы и оборот.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и сколько это стоит (с формулой). Выберите вариант и нажмите «Принять и выполнить» (открывает существующий процесс), «Отклонить» (с причиной) или «Отложить».' + NCHAR(10) + N'3. История: принятые решения, оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? с безубыточным удержанием; сравнение до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по каждой акции (сохранить, продлить или отменить).' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: значения, запускающие правила. Отчёт для руководства: PDF со всем перечисленным.',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → запуск акции → оценка результата.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: выберите период (или быстрый выбор) и тариф. Каждый показатель сравнивается с предыдущим периодом той же длины (▲/▼). Графики: тенденция с прогнозом на 3 месяца (пунктир), доход под риском, запасы и оборот.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и сколько это стоит (с формулой). Выберите вариант и нажмите «Принять и выполнить» (открывает существующий процесс), «Отклонить» (с причиной) или «Отложить»; «Смоделировать этот вариант» загружает его в симулятор.' + NCHAR(10) + N'3. История: принятые решения, оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? с безубыточным удержанием; сравнение до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по каждой акции (сохранить, продлить или отменить).' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: значения, запускающие правила. Отчёт для руководства: PDF со всем перечисленным.'),
    (N'PT', N'ayuda.CentroDecisionesForm',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar promoção → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: escolha o período (ou um atalho) e o plano. Cada indicador é comparado com o período anterior de mesma duração (▲/▼). Os gráficos mostram a tendência com 3 meses projetados (tracejado), a receita em risco, o estoque e a rotatividade.' + NCHAR(10) + N'2. Recomendações: o que está acontecendo, por quê, o que fazer e quanto vale (com a fórmula). Escolha uma opção e clique em Aceitar e executar (abre o fluxo existente), Descartar (com motivo) ou Adiar.' + NCHAR(10) + N'3. Histórico: as decisões tomadas, com o impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? com a retenção de equilíbrio; compare até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção (manter, estender ou dar baixa).' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: os valores que disparam cada regra. Relatório executivo: PDF com tudo isso.',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar promoção → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: escolha o período (ou um atalho) e o plano. Cada indicador é comparado com o período anterior de mesma duração (▲/▼). Os gráficos mostram a tendência com 3 meses projetados (tracejado), a receita em risco, o estoque e a rotatividade.' + NCHAR(10) + N'2. Recomendações: o que está acontecendo, por quê, o que fazer e quanto vale (com a fórmula). Escolha uma opção e clique em Aceitar e executar (abre o fluxo existente), Descartar (com motivo) ou Adiar; Simular esta opção a carrega no simulador.' + NCHAR(10) + N'3. Histórico: as decisões tomadas, com o impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? com a retenção de equilíbrio; compare até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção (manter, estender ou dar baixa).' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: os valores que disparam cada regra. Relatório executivo: PDF com tudo isso.'),
    (N'ES', N'msg.demo.confirmar',
        N'¿Restablecer los datos de demo?' + NCHAR(10) + N'' + NCHAR(10) + N'Se BORRARÁN todos los datos de negocio (clientes, prendas, pedidos, contrataciones, cobros, promociones y bitácora de negocio) y se volverán a cargar los datos de prueba de la instalación, con las fechas corridas a hoy. Usuarios, permisos, planes, idiomas y la bitácora del sistema se conservan.' + NCHAR(10) + N'' + NCHAR(10) + N'Antes se generará una copia de seguridad (sin cifrar) en la carpeta de backups. Al terminar, la aplicación se reiniciará.' + NCHAR(10) + N'' + NCHAR(10) + N'Úsela como máximo 2 días antes de la defensa.',
        N'¿Restablecer los datos de demo?' + NCHAR(10) + N'' + NCHAR(10) + N'Se BORRARÁN todos los datos de negocio (clientes, prendas, pedidos, contrataciones, cobros, promociones, decisiones comerciales y bitácora de negocio) y se volverán a cargar los datos de prueba de la instalación, con las fechas corridas a hoy. Usuarios, permisos, planes, idiomas y la bitácora del sistema se conservan.' + NCHAR(10) + N'' + NCHAR(10) + N'Antes se generará una copia de seguridad (sin cifrar) en la carpeta de backups. Al terminar, la aplicación se reiniciará.'),
    (N'EN', N'msg.demo.confirmar',
        N'Reset the demo data?' + NCHAR(10) + N'' + NCHAR(10) + N'ALL business data (customers, garments, orders, contracts, payments, promotions and business log) will be DELETED and the installation''s sample data will be loaded again, with the dates moved to today. Users, permissions, plans, languages and the system log are kept.' + NCHAR(10) + N'' + NCHAR(10) + N'A backup (unencrypted) is generated first in the backups folder. When it finishes, the application will restart.' + NCHAR(10) + N'' + NCHAR(10) + N'Use it at most 2 days before the defense.',
        N'Reset the demo data?' + NCHAR(10) + N'' + NCHAR(10) + N'ALL business data (customers, garments, orders, contracts, payments, promotions, business decisions and business log) will be DELETED and the installation''s sample data will be loaded again, with the dates moved to today. Users, permissions, plans, languages and the system log are kept.' + NCHAR(10) + N'' + NCHAR(10) + N'A backup (unencrypted) is generated first in the backups folder. When it finishes, the application will restart.'),
    (N'RU', N'msg.demo.confirmar',
        N'Сбросить демо-данные?' + NCHAR(10) + N'' + NCHAR(10) + N'ВСЕ бизнес-данные (клиенты, вещи, заказы, договоры, платежи, акции и бизнес-журнал) будут УДАЛЕНЫ, и тестовые данные установки будут загружены заново с датами, сдвинутыми на сегодня. Пользователи, права, планы, языки и системный журнал сохраняются.' + NCHAR(10) + N'' + NCHAR(10) + N'Сначала в папке бэкапов создаётся резервная копия (без шифрования). По завершении приложение перезапустится.' + NCHAR(10) + N'' + NCHAR(10) + N'Используйте не ранее чем за 2 дня до защиты.',
        N'Сбросить демо-данные?' + NCHAR(10) + N'' + NCHAR(10) + N'ВСЕ бизнес-данные (клиенты, вещи, заказы, договоры, платежи, акции, коммерческие решения и бизнес-журнал) будут УДАЛЕНЫ, и тестовые данные установки будут загружены заново с датами, сдвинутыми на сегодня. Пользователи, права, планы, языки и системный журнал сохраняются.' + NCHAR(10) + N'' + NCHAR(10) + N'Сначала в папке бэкапов создаётся резервная копия (без шифрования). По завершении приложение перезапустится.'),
    (N'PT', N'msg.demo.confirmar',
        N'Restabelecer os dados de demonstração?' + NCHAR(10) + N'' + NCHAR(10) + N'TODOS os dados de negócio (clientes, roupas, pedidos, contratações, cobranças, promoções e log de negócio) serão EXCLUÍDOS e os dados de exemplo da instalação serão carregados novamente, com as datas ajustadas para hoje. Usuários, permissões, planos, idiomas e o log do sistema são mantidos.' + NCHAR(10) + N'' + NCHAR(10) + N'Antes será gerada uma cópia de segurança (sem criptografia) na pasta de backups. Ao terminar, a aplicação será reiniciada.' + NCHAR(10) + N'' + NCHAR(10) + N'Use no máximo 2 dias antes da defesa.',
        N'Restabelecer os dados de demonstração?' + NCHAR(10) + N'' + NCHAR(10) + N'TODOS os dados de negócio (clientes, roupas, pedidos, contratações, cobranças, promoções, decisões comerciais e log de negócio) serão EXCLUÍDOS e os dados de exemplo da instalação serão carregados novamente, com as datas ajustadas para hoje. Usuários, permissões, planos, idiomas e o log do sistema são mantidos.' + NCHAR(10) + N'' + NCHAR(10) + N'Antes será gerada uma cópia de segurança (sem criptografia) na pasta de backups. Ao terminar, a aplicação será reiniciada.'),
    (N'ES', N'err.bll.demo.integridad',
        N'Los datos de demo se cargaron, pero la verificación de integridad no cerró. Revise Administrar → Diagnóstico de Integridad.',
        N'Los datos de demo se cargaron, pero la verificación de integridad no cerró. Revise Administrar → Sistema → Diagnóstico de Integridad.'),
    (N'EN', N'err.bll.demo.integridad',
        N'The demo data was loaded, but the integrity check did not pass. Check Manage → Integrity Diagnostics.',
        N'The demo data was loaded, but the integrity check did not pass. Check Administration → System → Integrity Diagnostics.'),
    (N'RU', N'err.bll.demo.integridad',
        N'Демо-данные загружены, но проверка целостности не пройдена. Проверьте Управление → Диагностика целостности.',
        N'Демо-данные загружены, но проверка целостности не пройдена. Проверьте Администрирование → Система → Диагностика целостности.'),
    (N'PT', N'err.bll.demo.integridad',
        N'Os dados de demonstração foram carregados, mas a verificação de integridade não fechou. Verifique Administrar → Diagnóstico de Integridade.',
        N'Os dados de demonstração foram carregados, mas a verificação de integridade não fechou. Verifique Administrar → Sistema → Diagnóstico de Integridade.'),
    (N'ES', N'glos.mrr.formula',
        N'Σ precio mensual del plan de cada cliente activo; el del período anterior se reconstruye: MRR − altas + bajas. Fuente: Cliente, PlanSuscripcion, HistorialRenovacion.',
        N'Σ precio mensual del plan de los clientes que pagan hoy (con plan, sin pausa vigente, sin la suscripción vencida y sin suspensión por falta de pago). Al fin del período: MRR de hoy − altas + bajas posteriores al período; al inicio: MRR al fin − altas + bajas del período. Fuente: Cliente, PlanSuscripcion, HistorialRenovacion.'),
    (N'EN', N'glos.mrr.formula',
        N'Σ monthly plan price of each active customer; the previous period''s is rebuilt: MRR − sign-ups + cancellations. Source: Cliente, PlanSuscripcion, HistorialRenovacion.',
        N'Σ monthly plan price of the customers who pay today (with a plan, not paused, subscription not expired and not suspended for non-payment). At the end of the period: today''s MRR − sign-ups + cancellations after the period; at the start: end MRR − sign-ups + cancellations in the period. Source: Cliente, PlanSuscripcion, HistorialRenovacion.'),
    (N'RU', N'glos.mrr.formula',
        N'Σ месячной цены тарифа каждого активного клиента; прошлый период восстанавливается: MRR − подписки + отказы. Источник: Cliente, PlanSuscripcion, HistorialRenovacion.',
        N'Σ месячной цены тарифа клиентов, которые платят сегодня (с тарифом, без действующей паузы, с непросроченной подпиской и без приостановки за неуплату). На конец периода: MRR сегодня − подписки + отказы после периода; на начало: MRR на конец − подписки + отказы за период. Источник: Cliente, PlanSuscripcion, HistorialRenovacion.'),
    (N'PT', N'glos.mrr.formula',
        N'Σ preço mensal do plano de cada cliente ativo; o do período anterior é reconstruído: MRR − adesões + cancelamentos. Fonte: Cliente, PlanSuscripcion, HistorialRenovacion.',
        N'Σ preço mensal do plano dos clientes que pagam hoje (com plano, sem pausa vigente, sem assinatura vencida e sem suspensão por falta de pagamento). No fim do período: MRR de hoje − adesões + cancelamentos posteriores ao período; no início: MRR no fim − adesões + cancelamentos do período. Fonte: Cliente, PlanSuscripcion, HistorialRenovacion.'),
    (N'ES', N'glos.churn.formula',
        N'Bajas del período / clientes al inicio × 100 (clientes al inicio = activos − altas + bajas). Fuente: HistorialRenovacion.',
        N'Bajas del período / clientes al inicio × 100. Clientes al fin del período = activos hoy − altas + bajas posteriores al período; al inicio = clientes al fin − altas + bajas del período. Fuente: HistorialRenovacion.'),
    (N'EN', N'glos.churn.formula',
        N'Cancellations in the period / customers at start × 100 (customers at start = active − sign-ups + cancellations). Source: HistorialRenovacion.',
        N'Cancellations in the period / customers at start × 100. Customers at the end of the period = active today − sign-ups + cancellations after the period; at start = customers at the end − sign-ups + cancellations in the period. Source: HistorialRenovacion.'),
    (N'RU', N'glos.churn.formula',
        N'Отказы за период / клиенты на начало × 100 (клиенты на начало = активные − подписки + отказы). Источник: HistorialRenovacion.',
        N'Отказы за период / клиенты на начало × 100. Клиенты на конец периода = активные сегодня − подписки + отказы после периода; на начало = клиенты на конец − подписки + отказы за период. Источник: HistorialRenovacion.'),
    (N'PT', N'glos.churn.formula',
        N'Cancelamentos do período / clientes no início × 100 (clientes no início = ativos − adesões + cancelamentos). Fonte: HistorialRenovacion.',
        N'Cancelamentos do período / clientes no início × 100. Clientes no fim do período = ativos hoje − adesões + cancelamentos posteriores ao período; no início = clientes no fim − adesões + cancelamentos do período. Fonte: HistorialRenovacion.'),
    (N'ES', N'glos.proyeccion.formula',
        N'Regresión lineal por mínimos cuadrados sobre los últimos 6 meses (y = a + b·x); R² indica qué tan confiable es.',
        N'Regresión lineal por mínimos cuadrados sobre los últimos 6 meses (y = a + b·x); R² indica qué tan confiable es. Un mes parcial (el período termina antes de fin de mes, p. ej. el mes en curso) se muestra con marcador hueco y asterisco, pero no entra en la regresión.'),
    (N'EN', N'glos.proyeccion.formula',
        N'Least-squares linear regression over the last 6 months (y = a + b·x); R² shows how reliable it is.',
        N'Least-squares linear regression over the last 6 months (y = a + b·x); R² shows how reliable it is. A partial month (the period ends before the month does, e.g. the current month) is shown with a hollow marker and an asterisk, but is left out of the regression.'),
    (N'RU', N'glos.proyeccion.formula',
        N'Линейная регрессия методом наименьших квадратов за 6 месяцев (y = a + b·x); R² показывает надёжность.',
        N'Линейная регрессия методом наименьших квадратов за 6 месяцев (y = a + b·x); R² показывает надёжность. Неполный месяц (период заканчивается до конца месяца, например текущий) показывается полым маркером со звёздочкой, но не входит в регрессию.'),
    (N'PT', N'glos.proyeccion.formula',
        N'Regressão linear por mínimos quadrados sobre os últimos 6 meses (y = a + b·x); R² indica quão confiável é.',
        N'Regressão linear por mínimos quadrados sobre os últimos 6 meses (y = a + b·x); R² indica quão confiável é. Um mês parcial (o período termina antes do fim do mês, p. ex. o mês em curso) é exibido com marcador vazado e asterisco, mas não entra na regressão.');

    UPDATE t SET t.Texto = f.TextoNuevo
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @Textos160 f ON f.Clave = c.Clave AND f.Codigo = i.Codigo
    WHERE t.Texto COLLATE Latin1_General_BIN2 = f.TextoAnterior COLLATE Latin1_General_BIN2; -- exacto: no pisa ediciones
    PRINT CONCAT('Textos de la 1.6.0: ', @@ROWCOUNT, ' traducción(es) actualizada(s).');
END
GO
PRINT 'Sección 21z8: textos de la auditoría de la 1.6.0 verificados.';
GO

-- ============================================================
-- WardrobeFlow — 21z9. SERIALIZACIÓN EN XML Y JSON
-- ------------------------------------------------------------
-- La exportación/importación de versiones de usuario y de errores serializados ahora admite XML o
-- JSON (el formato lo da la extensión del archivo). Se actualizan los textos que decían solo "XML":
-- botón y pantalla de errores (btn.errxml, frm.errxml*, btn.errxml.importar/exportar), el aviso de
-- exportación (msg.errxml.exportado, ahora con el formato en {1}), los errores err.serial.vacio y
-- err.serial.formato (formato en {1}) y la ayuda en línea de Bitácora, Errores e Historial de
-- Cambios. Las claves nuevas (err.serial.extension, dlg.filtro.serial.*) las inserta la aplicación
-- desde traducciones.tsv; acá se actualizan las que ya existen en una base instalada, SOLO si siguen
-- con el texto de fábrica anterior (una edición hecha en Idiomas se respeta). Idempotente.
-- ============================================================
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @TextosSerial TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                                 TextoAnterior NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @TextosSerial (Codigo, Clave, TextoAnterior, TextoNuevo) VALUES
    (N'ES', N'ayuda.Bitacora',
        N'Consulta de los eventos del sistema con filtros por usuario, fecha, módulo y criticidad. Se puede exportar a PDF.' + NCHAR(10) + N'' + NCHAR(10) + N'• Errores (XML): los errores inesperados quedan serializados en XML; se pueden ver, importar desde otra PC y exportar.',
        N'Consulta de los eventos del sistema con filtros por usuario, fecha, módulo y criticidad. Se puede exportar a PDF.' + NCHAR(10) + N'' + NCHAR(10) + N'• Errores (XML/JSON): los errores inesperados quedan serializados en XML; se pueden ver, importar desde otra PC y exportar, en XML o JSON.'),
    (N'EN', N'ayuda.Bitacora',
        N'View of system events with filters by user, date, module and severity. It can be exported to PDF.' + NCHAR(10) + N'' + NCHAR(10) + N'• Errors (XML): unexpected errors are serialized to XML; you can view them, import them from another PC and export them.',
        N'View of system events with filters by user, date, module and severity. It can be exported to PDF.' + NCHAR(10) + N'' + NCHAR(10) + N'• Errors (XML/JSON): unexpected errors are serialized to XML; you can view them, import them from another PC and export them, in XML or JSON.'),
    (N'RU', N'ayuda.Bitacora',
        N'Просмотр событий системы с фильтрами по пользователю, дате, модулю и критичности. Можно экспортировать в PDF.' + NCHAR(10) + N'' + NCHAR(10) + N'• «Ошибки (XML)»: непредвиденные ошибки сериализуются в XML; их можно просматривать, импортировать с другого ПК и экспортировать.',
        N'Просмотр событий системы с фильтрами по пользователю, дате, модулю и критичности. Можно экспортировать в PDF.' + NCHAR(10) + N'' + NCHAR(10) + N'• «Ошибки (XML/JSON)»: непредвиденные ошибки сериализуются в XML; их можно просматривать, импортировать с другого ПК и экспортировать в XML или JSON.'),
    (N'PT', N'ayuda.Bitacora',
        N'Consulta dos eventos do sistema com filtros por usuário, data, módulo e criticidade. Pode ser exportada para PDF.' + NCHAR(10) + N'' + NCHAR(10) + N'• Erros (XML): os erros inesperados ficam serializados em XML; é possível vê-los, importá-los de outro PC e exportá-los.',
        N'Consulta dos eventos do sistema com filtros por usuário, data, módulo e criticidade. Pode ser exportada para PDF.' + NCHAR(10) + N'' + NCHAR(10) + N'• Erros (XML/JSON): os erros inesperados ficam serializados em XML; é possível vê-los, importá-los de outro PC e exportá-los, em XML ou JSON.'),
    (N'ES', N'ayuda.ErroresXmlForm',
        N'Errores inesperados de la aplicación, serializados en XML (un archivo por mes en esta PC).' + NCHAR(10) + N'' + NCHAR(10) + N'• Ver los de esta PC: lee los archivos locales.' + NCHAR(10) + N'• Importar XML: abre un archivo de errores, por ejemplo traído de otra PC.' + NCHAR(10) + N'• Exportar XML: guarda la lista que se está viendo en un archivo.',
        N'Errores inesperados de la aplicación, serializados en XML (un archivo por mes en esta PC).' + NCHAR(10) + N'' + NCHAR(10) + N'• Ver los de esta PC: lee los archivos locales.' + NCHAR(10) + N'• Importar XML/JSON: abre un archivo de errores en XML o JSON, por ejemplo traído de otra PC.' + NCHAR(10) + N'• Exportar XML/JSON: guarda la lista que se está viendo en un archivo; el formato lo da el tipo elegido (.xml o .json).' + NCHAR(10) + N'Los dos diálogos abren en %LOCALAPPDATA%\WardrobeFlow\Errores\Exportados, aparte del registro mensual (la carpeta se crea si no existe); la importación puede abrir un archivo de cualquier carpeta.'),
    (N'EN', N'ayuda.ErroresXmlForm',
        N'Unexpected application errors, serialized to XML (one file per month on this PC).' + NCHAR(10) + N'' + NCHAR(10) + N'• Show this PC''s: reads the local files.' + NCHAR(10) + N'• Import XML: opens an error file, for example brought from another PC.' + NCHAR(10) + N'• Export XML: saves the list being shown to a file.',
        N'Unexpected application errors, serialized to XML (one file per month on this PC).' + NCHAR(10) + N'' + NCHAR(10) + N'• Show this PC''s: reads the local files.' + NCHAR(10) + N'• Import XML/JSON: opens an XML or JSON error file, for example brought from another PC.' + NCHAR(10) + N'• Export XML/JSON: saves the list being shown to a file; the format is given by the chosen type (.xml or .json).' + NCHAR(10) + N'Both dialogs open in %LOCALAPPDATA%\WardrobeFlow\Errores\Exportados, separate from the monthly log (the folder is created if it does not exist); import can open a file from any folder.'),
    (N'RU', N'ayuda.ErroresXmlForm',
        N'Непредвиденные ошибки приложения, сериализованные в XML (один файл в месяц на этом ПК).' + NCHAR(10) + N'' + NCHAR(10) + N'• «Ошибки этого ПК»: читает локальные файлы.' + NCHAR(10) + N'• «Импорт XML»: открывает файл ошибок, например с другого ПК.' + NCHAR(10) + N'• «Экспорт XML»: сохраняет показанный список в файл.',
        N'Непредвиденные ошибки приложения, сериализованные в XML (один файл в месяц на этом ПК).' + NCHAR(10) + N'' + NCHAR(10) + N'• «Ошибки этого ПК»: читает локальные файлы.' + NCHAR(10) + N'• «Импорт XML/JSON»: открывает файл ошибок в формате XML или JSON, например с другого ПК.' + NCHAR(10) + N'• «Экспорт XML/JSON»: сохраняет показанный список в файл; формат определяется выбранным типом (.xml или .json).' + NCHAR(10) + N'Оба диалога открываются в %LOCALAPPDATA%\WardrobeFlow\Errores\Exportados, отдельно от ежемесячного журнала (папка создаётся, если её нет); при импорте можно открыть файл из любой папки.'),
    (N'PT', N'ayuda.ErroresXmlForm',
        N'Erros inesperados da aplicação, serializados em XML (um arquivo por mês neste PC).' + NCHAR(10) + N'' + NCHAR(10) + N'• Ver os deste PC: lê os arquivos locais.' + NCHAR(10) + N'• Importar XML: abre um arquivo de erros, por exemplo trazido de outro PC.' + NCHAR(10) + N'• Exportar XML: salva a lista exibida em um arquivo.',
        N'Erros inesperados da aplicação, serializados em XML (um arquivo por mês neste PC).' + NCHAR(10) + N'' + NCHAR(10) + N'• Ver os deste PC: lê os arquivos locais.' + NCHAR(10) + N'• Importar XML/JSON: abre um arquivo de erros em XML ou JSON, por exemplo trazido de outro PC.' + NCHAR(10) + N'• Exportar XML/JSON: salva a lista exibida em um arquivo; o formato é dado pelo tipo escolhido (.xml ou .json).' + NCHAR(10) + N'Os dois diálogos abrem em %LOCALAPPDATA%\WardrobeFlow\Errores\Exportados, separada do registro mensal (a pasta é criada se não existir); a importação pode abrir um arquivo de qualquer pasta.'),
    (N'ES', N'ayuda.ErroresXmlForm.titulo',
        N'Errores (XML)',
        N'Errores (XML/JSON)'),
    (N'EN', N'ayuda.ErroresXmlForm.titulo',
        N'Errors (XML)',
        N'Errors (XML/JSON)'),
    (N'RU', N'ayuda.ErroresXmlForm.titulo',
        N'Ошибки (XML)',
        N'Ошибки (XML/JSON)'),
    (N'PT', N'ayuda.ErroresXmlForm.titulo',
        N'Erros (XML)',
        N'Erros (XML/JSON)'),
    (N'ES', N'ayuda.VersionHistorialForm',
        N'Versiones (patrón Memento) de los datos administrativos de cada usuario: usuario, nombre, apellido, email y fecha de nacimiento. La contraseña nunca se versiona.' + NCHAR(10) + N'' + NCHAR(10) + N'• Cargar: muestra las versiones del usuario elegido guardadas en la base.' + NCHAR(10) + N'• Restaurar Versión Seleccionada (solo Administrador): deja al usuario exactamente en ese estado. Queda registrado como un cambio más, así que también se puede deshacer.' + NCHAR(10) + N'• Exportar versiones…: serializa a XML las versiones del usuario elegido, sin la clave ni el estado de bloqueo. Por defecto se guardan en %LOCALAPPDATA%\WardrobeFlow\Versiones (la carpeta se crea si no existe).' + NCHAR(10) + N'• Importar versiones…: lee un archivo exportado, verifica su formato y que pertenezca a un usuario existente, y muestra sus versiones marcadas como "importada". Elija una y presione Restaurar: se aplican los mismos controles que en una restauración normal.',
        N'Versiones (patrón Memento) de los datos administrativos de cada usuario: usuario, nombre, apellido, email y fecha de nacimiento. La contraseña nunca se versiona.' + NCHAR(10) + N'' + NCHAR(10) + N'• Cargar: muestra las versiones del usuario elegido guardadas en la base.' + NCHAR(10) + N'• Restaurar Versión Seleccionada (solo Administrador): deja al usuario exactamente en ese estado. Queda registrado como un cambio más, así que también se puede deshacer.' + NCHAR(10) + N'• Exportar versiones…: serializa las versiones del usuario elegido a XML o JSON (según el tipo elegido), sin la clave ni el estado de bloqueo. Por defecto se guardan en %LOCALAPPDATA%\WardrobeFlow\Versiones (la carpeta se crea si no existe).' + NCHAR(10) + N'• Importar versiones…: lee un archivo exportado (XML o JSON), verifica su formato y que pertenezca a un usuario existente, y muestra sus versiones marcadas como "importada". Elija una y presione Restaurar: se aplican los mismos controles que en una restauración normal.'),
    (N'EN', N'ayuda.VersionHistorialForm',
        N'Versions (Memento pattern) of each user''s administrative data: username, first name, last name, email and date of birth. The password is never versioned.' + NCHAR(10) + N'' + NCHAR(10) + N'• Load: shows the selected user''s versions stored in the database.' + NCHAR(10) + N'• Restore Selected Version (Administrator only): leaves the user exactly in that state. It is recorded as one more change, so it can also be undone.' + NCHAR(10) + N'• Export versions…: serializes the selected user''s versions to XML, without the password or the lock status. By default they are saved in %LOCALAPPDATA%\WardrobeFlow\Versiones (the folder is created if it does not exist).' + NCHAR(10) + N'• Import versions…: reads an exported file, checks its format and that it belongs to an existing user, and shows its versions marked as "imported". Choose one and press Restore: the same checks as in a normal restore apply.',
        N'Versions (Memento pattern) of each user''s administrative data: username, first name, last name, email and date of birth. The password is never versioned.' + NCHAR(10) + N'' + NCHAR(10) + N'• Load: shows the selected user''s versions stored in the database.' + NCHAR(10) + N'• Restore Selected Version (Administrator only): leaves the user exactly in that state. It is recorded as one more change, so it can also be undone.' + NCHAR(10) + N'• Export versions…: serializes the selected user''s versions to XML or JSON (depending on the chosen type), without the password or the lock status. By default they are saved in %LOCALAPPDATA%\WardrobeFlow\Versiones (the folder is created if it does not exist).' + NCHAR(10) + N'• Import versions…: reads an exported file (XML or JSON), checks its format and that it belongs to an existing user, and shows its versions marked as "imported". Choose one and press Restore: the same checks as in a normal restore apply.'),
    (N'RU', N'ayuda.VersionHistorialForm',
        N'Версии (паттерн Memento) административных данных каждого пользователя: имя пользователя, имя, фамилия, email и дата рождения. Пароль никогда не версионируется.' + NCHAR(10) + N'' + NCHAR(10) + N'• Загрузить: показывает версии выбранного пользователя, сохранённые в базе данных.' + NCHAR(10) + N'• Восстановить выбранную версию (только администратор): возвращает пользователя точно в это состояние. Это записывается как ещё одно изменение, поэтому его тоже можно отменить.' + NCHAR(10) + N'• Экспорт версий…: сериализует версии выбранного пользователя в XML без пароля и состояния блокировки. По умолчанию файлы сохраняются в %LOCALAPPDATA%\WardrobeFlow\Versiones (папка создаётся, если её нет).' + NCHAR(10) + N'• Импорт версий…: читает экспортированный файл, проверяет его формат и принадлежность существующему пользователю и показывает его версии с пометкой «импортирована». Выберите версию и нажмите «Восстановить»: применяются те же проверки, что и при обычном восстановлении.',
        N'Версии (паттерн Memento) административных данных каждого пользователя: имя пользователя, имя, фамилия, email и дата рождения. Пароль никогда не версионируется.' + NCHAR(10) + N'' + NCHAR(10) + N'• Загрузить: показывает версии выбранного пользователя, сохранённые в базе данных.' + NCHAR(10) + N'• Восстановить выбранную версию (только администратор): возвращает пользователя точно в это состояние. Это записывается как ещё одно изменение, поэтому его тоже можно отменить.' + NCHAR(10) + N'• Экспорт версий…: сериализует версии выбранного пользователя в XML или JSON (по выбранному типу) без пароля и состояния блокировки. По умолчанию файлы сохраняются в %LOCALAPPDATA%\WardrobeFlow\Versiones (папка создаётся, если её нет).' + NCHAR(10) + N'• Импорт версий…: читает экспортированный файл (XML или JSON), проверяет его формат и принадлежность существующему пользователю и показывает его версии с пометкой «импортирована». Выберите версию и нажмите «Восстановить»: применяются те же проверки, что и при обычном восстановлении.'),
    (N'PT', N'ayuda.VersionHistorialForm',
        N'Versões (padrão Memento) dos dados administrativos de cada usuário: usuário, nome, sobrenome, email e data de nascimento. A senha nunca é versionada.' + NCHAR(10) + N'' + NCHAR(10) + N'• Carregar: mostra as versões do usuário escolhido salvas no banco de dados.' + NCHAR(10) + N'• Restaurar Versão Selecionada (somente Administrador): deixa o usuário exatamente nesse estado. Fica registrado como mais uma alteração, então também pode ser desfeito.' + NCHAR(10) + N'• Exportar versões…: serializa em XML as versões do usuário escolhido, sem a senha nem o estado de bloqueio. Por padrão são salvas em %LOCALAPPDATA%\WardrobeFlow\Versiones (a pasta é criada se não existir).' + NCHAR(10) + N'• Importar versões…: lê um arquivo exportado, verifica o formato e se pertence a um usuário existente, e mostra suas versões marcadas como "importada". Escolha uma e pressione Restaurar: aplicam-se os mesmos controles de uma restauração normal.',
        N'Versões (padrão Memento) dos dados administrativos de cada usuário: usuário, nome, sobrenome, email e data de nascimento. A senha nunca é versionada.' + NCHAR(10) + N'' + NCHAR(10) + N'• Carregar: mostra as versões do usuário escolhido salvas no banco de dados.' + NCHAR(10) + N'• Restaurar Versão Selecionada (somente Administrador): deixa o usuário exatamente nesse estado. Fica registrado como mais uma alteração, então também pode ser desfeito.' + NCHAR(10) + N'• Exportar versões…: serializa as versões do usuário escolhido em XML ou JSON (conforme o tipo escolhido), sem a senha nem o estado de bloqueio. Por padrão são salvas em %LOCALAPPDATA%\WardrobeFlow\Versiones (a pasta é criada se não existir).' + NCHAR(10) + N'• Importar versões…: lê um arquivo exportado (XML ou JSON), verifica o formato e se pertence a um usuário existente, e mostra suas versões marcadas como "importada". Escolha uma e pressione Restaurar: aplicam-se os mesmos controles de uma restauração normal.'),
    (N'ES', N'btn.errxml',
        N'Errores (XML)',
        N'Errores (XML/JSON)'),
    (N'EN', N'btn.errxml',
        N'Errors (XML)',
        N'Errors (XML/JSON)'),
    (N'RU', N'btn.errxml',
        N'Ошибки (XML)',
        N'Ошибки (XML/JSON)'),
    (N'PT', N'btn.errxml',
        N'Erros (XML)',
        N'Erros (XML/JSON)'),
    (N'ES', N'btn.errxml.exportar',
        N'Exportar XML…',
        N'Exportar XML/JSON…'),
    (N'EN', N'btn.errxml.exportar',
        N'Export XML…',
        N'Export XML/JSON…'),
    (N'RU', N'btn.errxml.exportar',
        N'Экспорт XML…',
        N'Экспорт XML/JSON…'),
    (N'PT', N'btn.errxml.exportar',
        N'Exportar XML…',
        N'Exportar XML/JSON…'),
    (N'ES', N'btn.errxml.importar',
        N'Importar XML…',
        N'Importar XML/JSON…'),
    (N'EN', N'btn.errxml.importar',
        N'Import XML…',
        N'Import XML/JSON…'),
    (N'RU', N'btn.errxml.importar',
        N'Импорт XML…',
        N'Импорт XML/JSON…'),
    (N'PT', N'btn.errxml.importar',
        N'Importar XML…',
        N'Importar XML/JSON…'),
    (N'ES', N'err.serial.formato',
        N'El archivo no tiene el formato XML esperado: {0}',
        N'El archivo no tiene el formato {1} esperado: {0}'),
    (N'EN', N'err.serial.formato',
        N'The file does not have the expected XML format: {0}',
        N'The file does not have the expected {1} format: {0}'),
    (N'RU', N'err.serial.formato',
        N'Файл не соответствует ожидаемому формату XML: {0}',
        N'Файл не соответствует ожидаемому формату {1}: {0}'),
    (N'PT', N'err.serial.formato',
        N'O arquivo não tem o formato XML esperado: {0}',
        N'O arquivo não tem o formato {1} esperado: {0}'),
    (N'ES', N'err.serial.vacio',
        N'El archivo XML está vacío.',
        N'El archivo está vacío.'),
    (N'EN', N'err.serial.vacio',
        N'The XML file is empty.',
        N'The file is empty.'),
    (N'RU', N'err.serial.vacio',
        N'Файл XML пуст.',
        N'Файл пуст.'),
    (N'PT', N'err.serial.vacio',
        N'O arquivo XML está vazio.',
        N'O arquivo está vazio.'),
    (N'ES', N'frm.errxml',
        N'Errores (XML)',
        N'Errores (XML/JSON)'),
    (N'EN', N'frm.errxml',
        N'Errors (XML)',
        N'Errors (XML/JSON)'),
    (N'RU', N'frm.errxml',
        N'Ошибки (XML)',
        N'Ошибки (XML/JSON)'),
    (N'PT', N'frm.errxml',
        N'Erros (XML)',
        N'Erros (XML/JSON)'),
    (N'ES', N'frm.errxml.titulo',
        N'Errores inesperados serializados en XML',
        N'Errores inesperados serializados (XML/JSON)'),
    (N'EN', N'frm.errxml.titulo',
        N'Unexpected errors serialized to XML',
        N'Unexpected errors serialized (XML/JSON)'),
    (N'RU', N'frm.errxml.titulo',
        N'Непредвиденные ошибки, сериализованные в XML',
        N'Непредвиденные ошибки, сериализованные (XML/JSON)'),
    (N'PT', N'frm.errxml.titulo',
        N'Erros inesperados serializados em XML',
        N'Erros inesperados serializados (XML/JSON)'),
    (N'ES', N'msg.errxml.exportado',
        N'Se exportaron {0} error(es) a XML.',
        N'Se exportaron {0} error(es) a {1}.'),
    (N'EN', N'msg.errxml.exportado',
        N'{0} error(s) exported to XML.',
        N'{0} error(s) exported to {1}.'),
    (N'RU', N'msg.errxml.exportado',
        N'Экспортировано ошибок в XML: {0}.',
        N'Экспортировано ошибок в {1}: {0}.'),
    (N'PT', N'msg.errxml.exportado',
        N'{0} erro(s) exportado(s) para XML.',
        N'{0} erro(s) exportado(s) para {1}.');

    UPDATE t SET t.Texto = f.TextoNuevo
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @TextosSerial f ON f.Clave = c.Clave AND f.Codigo = i.Codigo
    WHERE t.Texto COLLATE Latin1_General_BIN2 = f.TextoAnterior COLLATE Latin1_General_BIN2; -- exacto: no pisa ediciones
    PRINT CONCAT('Textos de serialización XML/JSON: ', @@ROWCOUNT, ' traducción(es) actualizada(s).');
END
GO
PRINT 'Sección 21z9: textos de serialización XML/JSON verificados.';
GO

-- ============================================================
-- WardrobeFlow — 21z10. TEXTOS DEL CENTRO DE DECISIONES: ACCIONES EJECUTABLES Y METAS
-- ------------------------------------------------------------
-- · ayuda.CentroDecisionesForm: cada recomendación ejecuta algo al aceptarla (sin opciones que solo
--   registran), la pestaña Metas y los avisos al iniciar sesión.
-- · glos.ocupacion.decision: la ocupación ya no tiene una regla de rango (30–85 %); se le fija una meta.
-- · rec.renov.opcion / rec.rotacion.opcion / rec.mant.opcion.revisar: lo que hace ahora cada opción
--   (campaña de renovación con los contactos, baja de las prendas, revisión en limpieza).
-- Las claves nuevas las inserta la aplicación desde traducciones.tsv; acá se actualizan las que ya
-- existen en una base instalada, SOLO si siguen con el texto de fábrica anterior (una edición hecha en
-- Idiomas se respeta). Idempotente.
-- ============================================================
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @TextosPN03 TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                               TextoAnterior NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @TextosPN03 (Codigo, Clave, TextoAnterior, TextoNuevo) VALUES
    (N'ES', N'ayuda.CentroDecisionesForm',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar promoción → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: elija el período (o un atajo) y el plan. Cada indicador se compara con el período anterior de igual duración (▲/▼). Los gráficos muestran la tendencia con 3 meses proyectados (punteado), el ingreso en riesgo, el stock y la rotación.' + NCHAR(10) + N'2. Recomendaciones: qué está pasando, por qué, qué hacer y cuánto vale (con la fórmula). Elija una opción y pulse Aceptar y ejecutar (abre el flujo existente), Descartar (con motivo) o Posponer; Simular esta opción la carga en el simulador.' + NCHAR(10) + N'3. Historial: las decisiones tomadas, con el impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? con la retención de equilibrio; compare hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción (mantener, extender o dar de baja).' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: los valores que disparan cada regla. Informe ejecutivo: PDF con todo lo anterior.',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar → medir.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: período y plan; indicadores contra el período anterior (▲/▼) y gráficos con 3 meses proyectados.' + NCHAR(10) + N'2. Recomendaciones: qué pasa, por qué, qué hacer y su impacto. Aceptar y ejecutar siempre hace algo: sugiere, extiende o pide la baja de una promoción, exporta los contactos de una campaña (se mide después), genera la orden de compra o da de baja / manda a revisión prendas. Descartar pide motivo; Posponer la oculta N días.' + NCHAR(10) + N'3. Historial: impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? y hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción.' + NCHAR(10) + N'Metas: objetivo mensual por indicador con real, proyección y semáforo; en riesgo, sube la prioridad de lo que la mueve y se avisa al iniciar sesión.' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: lo que dispara cada regla. Informe ejecutivo: PDF con todo.'),
    (N'EN', N'ayuda.CentroDecisionesForm',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply promotion → measure results.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: choose the period (or a shortcut) and the plan. Each indicator is compared with the previous period of the same length (▲/▼). The charts show the trend with 3 projected months (dashed), revenue at risk, stock and turnover.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and how much it is worth (with the formula). Choose an option and press Accept and execute (opens the existing flow), Discard (with a reason) or Postpone; Simulate this option loads it into the simulator.' + NCHAR(10) + N'3. History: the decisions taken, with estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? with the break-even retention; compare up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion (keep, extend or withdraw).' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: the values that trigger each rule. Executive report: PDF with all of the above.',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply → measure.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: period and plan; indicators against the previous period (▲/▼) and charts with 3 projected months.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and its impact. Accept and execute always does something: it suggests, extends or asks to withdraw a promotion, exports a campaign''s contacts (measured later), generates the purchase order or writes off / sends garments for review. Discard asks for a reason; Postpone hides it N days.' + NCHAR(10) + N'3. History: estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? and up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion.' + NCHAR(10) + N'Goals: monthly target per indicator with actual, projection and traffic light; at risk, it raises the priority of what moves it and is announced at login.' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: what triggers each rule. Executive report: PDF with everything.'),
    (N'RU', N'ayuda.CentroDecisionesForm',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → запуск акции → оценка результата.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: выберите период (или быстрый выбор) и тариф. Каждый показатель сравнивается с предыдущим периодом той же длины (▲/▼). Графики: тенденция с прогнозом на 3 месяца (пунктир), доход под риском, запасы и оборот.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и сколько это стоит (с формулой). Выберите вариант и нажмите «Принять и выполнить» (открывает существующий процесс), «Отклонить» (с причиной) или «Отложить»; «Смоделировать этот вариант» загружает его в симулятор.' + NCHAR(10) + N'3. История: принятые решения, оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? с безубыточным удержанием; сравнение до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по каждой акции (сохранить, продлить или отменить).' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: значения, запускающие правила. Отчёт для руководства: PDF со всем перечисленным.',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → применение → оценка.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: период и тариф; показатели против прошлого периода (▲/▼) и графики с прогнозом на 3 месяца.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и эффект. «Принять и выполнить» всегда что-то делает: предлагает, продлевает или отменяет акцию, выгружает контакты кампании (результат измеряется позже), формирует заказ на закупку или списывает / отправляет вещи на проверку. «Отклонить» требует причины; «Отложить» скрывает на N дней.' + NCHAR(10) + N'3. История: оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? и до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по акции.' + NCHAR(10) + N'Цели: месячная цель по показателю с фактом, прогнозом и светофором; под угрозой — растёт приоритет того, что на неё влияет, и о ней сообщается при входе.' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: что запускает правила. Отчёт для руководства: PDF со всем.'),
    (N'PT', N'ayuda.CentroDecisionesForm',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar promoção → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: escolha o período (ou um atalho) e o plano. Cada indicador é comparado com o período anterior de mesma duração (▲/▼). Os gráficos mostram a tendência com 3 meses projetados (tracejado), a receita em risco, o estoque e a rotatividade.' + NCHAR(10) + N'2. Recomendações: o que está acontecendo, por quê, o que fazer e quanto vale (com a fórmula). Escolha uma opção e clique em Aceitar e executar (abre o fluxo existente), Descartar (com motivo) ou Adiar; Simular esta opção a carrega no simulador.' + NCHAR(10) + N'3. Histórico: as decisões tomadas, com o impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? com a retenção de equilíbrio; compare até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção (manter, estender ou dar baixa).' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: os valores que disparam cada regra. Relatório executivo: PDF com tudo isso.',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar → medir.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: período e plano; indicadores contra o período anterior (▲/▼) e gráficos com 3 meses projetados.' + NCHAR(10) + N'2. Recomendações: o que acontece, por quê, o que fazer e seu impacto. Aceitar e executar sempre faz algo: sugere, estende ou pede a baixa de uma promoção, exporta os contatos de uma campanha (medida depois), gera a ordem de compra ou dá baixa / envia peças para revisão. Descartar pede motivo; Adiar a oculta N dias.' + NCHAR(10) + N'3. Histórico: impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? e até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção.' + NCHAR(10) + N'Metas: objetivo mensal por indicador com real, projeção e semáforo; em risco, aumenta a prioridade do que a move e é avisada ao iniciar a sessão.' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: o que dispara cada regra. Relatório executivo: PDF com tudo.'),
    (N'ES', N'glos.ocupacion.decision',
        N'Rango 30–85 %: debajo, frenar compras; arriba, priorizar la reposición.',
        N'Se le puede fijar una meta mensual: si está en riesgo, suben de prioridad dar de baja las prendas sin rotación o con mantenimiento excesivo (menos prendas activas) y el upgrade de plan (más prendas en uso). Mandarlas a revisión no la cambia.'),
    (N'EN', N'glos.ocupacion.decision',
        N'Range 30–85 %: below, hold purchases; above, prioritise restocking.',
        N'A monthly goal can be set: if it is at risk, writing off garments with no turnover or excessive maintenance (fewer active garments) and the plan upgrade (more garments in use) go up in priority. Sending them for review does not change it.'),
    (N'RU', N'glos.ocupacion.decision',
        N'Диапазон 30–85 %: ниже — приостановить закупки, выше — пополнять в первую очередь.',
        N'Можно задать месячную цель: если она под угрозой, растёт приоритет списания вещей без оборота или с избыточным обслуживанием (меньше активных вещей) и перехода на старший тариф (больше вещей в использовании). Отправка на проверку её не меняет.'),
    (N'PT', N'glos.ocupacion.decision',
        N'Faixa 30–85 %: abaixo, frear compras; acima, priorizar a reposição.',
        N'Pode-se definir uma meta mensal: se estiver em risco, sobem de prioridade dar baixa nas peças sem rotatividade ou com manutenção excessiva (menos peças ativas) e o upgrade de plano (mais peças em uso). Enviá-las para revisão não a altera.'),
    (N'ES', N'rec.renov.opcion',
        N'Gestionar el cobro de las {0} renovaciones desde Cobro de renovaciones',
        N'Campaña de renovación: exportar los contactos de los {0} cliente(s) para llamarlos o escribirles antes de perderlos'),
    (N'EN', N'rec.renov.opcion',
        N'Manage the payment of the {0} renewals from Renewal payments',
        N'Renewal campaign: export the contacts of the {0} customer(s) to call or write to them before losing them'),
    (N'RU', N'rec.renov.opcion',
        N'Провести оплату {0} продлений в разделе «Оплата продлений»',
        N'Кампания продления: выгрузить контакты клиентов ({0}), чтобы позвонить или написать им, пока они не ушли'),
    (N'PT', N'rec.renov.opcion',
        N'Gerenciar a cobrança das {0} renovações em Cobrança de renovações',
        N'Campanha de renovação: exportar os contatos dos {0} cliente(s) para ligar ou escrever antes de perdê-los'),
    (N'ES', N'rec.rotacion.opcion',
        N'Liquidar o dar de baja las {0} prenda(s) (abre Prendas filtrada)',
        N'Dar de baja las {0} prenda(s) para liquidarlas (pasan de Disponible a Baja)'),
    (N'EN', N'rec.rotacion.opcion',
        N'Clear or write off the {0} garment(s) (opens Garments filtered)',
        N'Write off the {0} garment(s) to clear them (from Available to Written off)'),
    (N'RU', N'rec.rotacion.opcion',
        N'Распродать или списать {0} вещ. (открывает отфильтрованный каталог)',
        N'Списать вещи ({0}) для распродажи (из «Доступна» в «Списана»)'),
    (N'PT', N'rec.rotacion.opcion',
        N'Liquidar ou dar baixa nas {0} peça(s) (abre Peças filtrada)',
        N'Dar baixa nas {0} peça(s) para liquidá-las (passam de Disponível a Baixa)'),
    (N'ES', N'rec.mant.opcion.revisar',
        N'Revisar su historial de mantenimiento con el proveedor de limpieza',
        N'Mandar las prendas disponibles a revisión (En limpieza) y revisar el proceso con el proveedor de limpieza'),
    (N'EN', N'rec.mant.opcion.revisar',
        N'Review their maintenance history with the cleaning supplier',
        N'Send the available garments for review (In cleaning) and review the process with the cleaning supplier'),
    (N'RU', N'rec.mant.opcion.revisar',
        N'Разобрать историю обслуживания с поставщиком чистки',
        N'Отправить доступные вещи на проверку («В чистке») и разобрать процесс с поставщиком чистки'),
    (N'PT', N'rec.mant.opcion.revisar',
        N'Revisar o histórico de manutenção com o fornecedor de limpeza',
        N'Enviar as peças disponíveis para revisão (Em limpeza) e revisar o processo com o fornecedor de limpeza');

    UPDATE t SET t.Texto = f.TextoNuevo
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @TextosPN03 f ON f.Clave = c.Clave AND f.Codigo = i.Codigo
    WHERE t.Texto COLLATE Latin1_General_BIN2 = f.TextoAnterior COLLATE Latin1_General_BIN2; -- exacto: no pisa ediciones
    PRINT CONCAT('Textos del Centro de decisiones (acciones y metas): ', @@ROWCOUNT, ' traducción(es) actualizada(s).');
END
GO
PRINT 'Sección 21z10: textos del Centro de decisiones (acciones ejecutables y metas) verificados.';
GO

-- ============================================================
-- WardrobeFlow — 21z12. CENTRO DE DECISIONES: PESTAÑA COHORTES (PN03)
-- ------------------------------------------------------------
-- La pestaña «1b. Cohortes» (retención por mes de alta, peor cohorte y su acción) se calcula con las
-- tablas existentes (Contratacion: primer pago; HistorialRenovacion: bajas): no hay tablas ni columnas
-- nuevas. Sus textos (cdc.cohorte.*) y la entrada de glosario «Retención por cohorte» (glos.cohorte.*)
-- son claves nuevas: las inserta la aplicación desde traducciones.tsv. Acá solo se actualiza la ayuda
-- F1 del Centro (ayuda.CentroDecisionesForm, una línea para la pestaña) en una base ya instalada, SOLO
-- si sigue con el texto de fábrica de la 1.6.0 (sección 21z8; una edición hecha en Idiomas se
-- respeta). El texto nuevo deja margen bajo los 1000 caracteres de Traduccion.Texto. Idempotente.
-- ============================================================
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @AyudaCohortes TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                                  TextoAnterior NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @AyudaCohortes (Codigo, Clave, TextoAnterior, TextoNuevo) VALUES
    (N'ES', N'ayuda.CentroDecisionesForm',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar promoción → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: elija el período (o un atajo) y el plan. Cada indicador se compara con el período anterior de igual duración (▲/▼). Los gráficos muestran la tendencia con 3 meses proyectados (punteado), el ingreso en riesgo, el stock y la rotación.' + NCHAR(10) + N'2. Recomendaciones: qué está pasando, por qué, qué hacer y cuánto vale (con la fórmula). Elija una opción y pulse Aceptar y ejecutar (abre el flujo existente), Descartar (con motivo) o Posponer; Simular esta opción la carga en el simulador.' + NCHAR(10) + N'3. Historial: las decisiones tomadas, con el impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? con la retención de equilibrio; compare hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción (mantener, extender o dar de baja).' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: los valores que disparan cada regla. Informe ejecutivo: PDF con todo lo anterior.',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar promoción → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: elija el período (o un atajo) y el plan. Cada indicador se compara con el período anterior de igual duración (▲/▼). Los gráficos muestran la tendencia con 3 meses proyectados (punteado), el ingreso en riesgo, el stock y la rotación.' + NCHAR(10) + N'1b. Cohortes: retención por mes de alta y peor cohorte.' + NCHAR(10) + N'2. Recomendaciones: qué está pasando, por qué, qué hacer y cuánto vale (con la fórmula). Elija una opción y pulse Aceptar y ejecutar, Descartar (con motivo) o Posponer; Simular esta opción la carga en el simulador.' + NCHAR(10) + N'3. Historial: las decisiones tomadas, con el impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? con la retención de equilibrio; compare hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción (mantener, extender o dar de baja).' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: los valores que disparan cada regla. Informe ejecutivo: PDF con todo lo anterior.'),
    (N'EN', N'ayuda.CentroDecisionesForm',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply promotion → measure results.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: choose the period (or a shortcut) and the plan. Each indicator is compared with the previous period of the same length (▲/▼). The charts show the trend with 3 projected months (dashed), revenue at risk, stock and turnover.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and how much it is worth (with the formula). Choose an option and press Accept and execute (opens the existing flow), Discard (with a reason) or Postpone; Simulate this option loads it into the simulator.' + NCHAR(10) + N'3. History: the decisions taken, with estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? with the break-even retention; compare up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion (keep, extend or withdraw).' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: the values that trigger each rule. Executive report: PDF with all of the above.',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply promotion → measure results.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: choose the period (or a shortcut) and the plan. Each indicator is compared with the previous period of the same length (▲/▼). The charts show the trend with 3 projected months (dashed), revenue at risk, stock and turnover.' + NCHAR(10) + N'1b. Cohorts: retention by sign-up month and worst cohort.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and how much it is worth (with the formula). Choose an option and press Accept and execute, Discard (with a reason) or Postpone; Simulate this option loads it into the simulator.' + NCHAR(10) + N'3. History: the decisions taken, with estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? with the break-even retention; compare up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion (keep, extend or withdraw).' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: the values that trigger each rule. Executive report: PDF with all of the above.'),
    (N'RU', N'ayuda.CentroDecisionesForm',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → запуск акции → оценка результата.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: выберите период (или быстрый выбор) и тариф. Каждый показатель сравнивается с предыдущим периодом той же длины (▲/▼). Графики: тенденция с прогнозом на 3 месяца (пунктир), доход под риском, запасы и оборот.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и сколько это стоит (с формулой). Выберите вариант и нажмите «Принять и выполнить» (открывает существующий процесс), «Отклонить» (с причиной) или «Отложить»; «Смоделировать этот вариант» загружает его в симулятор.' + NCHAR(10) + N'3. История: принятые решения, оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? с безубыточным удержанием; сравнение до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по каждой акции (сохранить, продлить или отменить).' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: значения, запускающие правила. Отчёт для руководства: PDF со всем перечисленным.',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → запуск акции → оценка результата.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: выберите период (или быстрый выбор) и тариф. Каждый показатель сравнивается с предыдущим периодом той же длины (▲/▼). Графики: тенденция с прогнозом на 3 месяца (пунктир), доход под риском, запасы и оборот.' + NCHAR(10) + N'1b. Когорты: удержание по месяцу подписки и худшая когорта.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и сколько это стоит (с формулой). Выберите вариант и нажмите «Принять и выполнить», «Отклонить» (с причиной) или «Отложить»; «Смоделировать этот вариант» загружает его в симулятор.' + NCHAR(10) + N'3. История: принятые решения, оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? с безубыточным удержанием; сравнение до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по каждой акции (сохранить, продлить или отменить).' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: значения, запускающие правила. Отчёт для руководства: PDF со всем перечисленным.'),
    (N'PT', N'ayuda.CentroDecisionesForm',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar promoção → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: escolha o período (ou um atalho) e o plano. Cada indicador é comparado com o período anterior de mesma duração (▲/▼). Os gráficos mostram a tendência com 3 meses projetados (tracejado), a receita em risco, o estoque e a rotatividade.' + NCHAR(10) + N'2. Recomendações: o que está acontecendo, por quê, o que fazer e quanto vale (com a fórmula). Escolha uma opção e clique em Aceitar e executar (abre o fluxo existente), Descartar (com motivo) ou Adiar; Simular esta opção a carrega no simulador.' + NCHAR(10) + N'3. Histórico: as decisões tomadas, com o impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? com a retenção de equilíbrio; compare até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção (manter, estender ou dar baixa).' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: os valores que disparam cada regra. Relatório executivo: PDF com tudo isso.',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar promoção → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: escolha o período (ou um atalho) e o plano. Cada indicador é comparado com o período anterior de mesma duração (▲/▼). Os gráficos mostram a tendência com 3 meses projetados (tracejado), a receita em risco, o estoque e a rotatividade.' + NCHAR(10) + N'1b. Coortes: retenção por mês de adesão e pior coorte.' + NCHAR(10) + N'2. Recomendações: o que está acontecendo, por quê, o que fazer e quanto vale (com a fórmula). Escolha uma opção e clique em Aceitar e executar, Descartar (com motivo) ou Adiar; Simular esta opção a carrega no simulador.' + NCHAR(10) + N'3. Histórico: as decisões tomadas, com o impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? com a retenção de equilíbrio; compare até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção (manter, estender ou dar baixa).' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: os valores que disparam cada regra. Relatório executivo: PDF com tudo isso.');

    UPDATE t SET t.Texto = f.TextoNuevo
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @AyudaCohortes f ON f.Clave = c.Clave AND f.Codigo = i.Codigo
    WHERE t.Texto COLLATE Latin1_General_BIN2 = f.TextoAnterior COLLATE Latin1_General_BIN2; -- exacto: no pisa ediciones
    PRINT CONCAT('Ayuda del Centro (cohortes): ', @@ROWCOUNT, ' traducción(es) actualizada(s).');
END
GO
PRINT 'Sección 21z12: ayuda del Centro de decisiones con la pestaña Cohortes verificada.';
GO

-- ============================================================
-- WardrobeFlow — 21z13. AYUDA DEL CENTRO DE DECISIONES: COHORTES + ACCIONES Y METAS (TEXTO FINAL)
-- ------------------------------------------------------------
-- ayuda.CentroDecisionesForm termina siempre en el mismo texto (acciones ejecutables, Metas, avisos y
-- la línea «1b. Cohortes»), sin importar qué secciones anteriores ya corrieron sobre la base:
--   · texto de fábrica de la 1.6.0 (21z8)           → texto final
--   · texto de la sección 21z10 (acciones y metas)  → texto final
--   · texto de la sección 21z12 (solo cohortes)      → texto final
-- Solo se actualiza si el texto es exactamente uno de esos (una edición hecha en Idiomas se respeta).
-- El texto final mide menos de 1000 caracteres (Traduccion.Texto) en los 4 idiomas. Idempotente.
-- ============================================================
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @AyudaFinal TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                               TextoAnterior NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(MAX) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @AyudaFinal (Codigo, Clave, TextoAnterior, TextoNuevo) VALUES
    (N'ES', N'ayuda.CentroDecisionesForm',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar promoción → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: elija el período (o un atajo) y el plan. Cada indicador se compara con el período anterior de igual duración (▲/▼). Los gráficos muestran la tendencia con 3 meses proyectados (punteado), el ingreso en riesgo, el stock y la rotación.' + NCHAR(10) + N'2. Recomendaciones: qué está pasando, por qué, qué hacer y cuánto vale (con la fórmula). Elija una opción y pulse Aceptar y ejecutar (abre el flujo existente), Descartar (con motivo) o Posponer; Simular esta opción la carga en el simulador.' + NCHAR(10) + N'3. Historial: las decisiones tomadas, con el impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? con la retención de equilibrio; compare hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción (mantener, extender o dar de baja).' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: los valores que disparan cada regla. Informe ejecutivo: PDF con todo lo anterior.',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar → medir.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: período y plan; indicadores contra el período anterior (▲/▼) y gráficos con 3 meses proyectados.' + NCHAR(10) + N'1b. Cohortes: retención por mes de alta y peor cohorte.' + NCHAR(10) + N'2. Recomendaciones: qué pasa, por qué, qué hacer y su impacto. Aceptar y ejecutar siempre hace algo: sugiere, extiende o pide la baja de una promoción, exporta los contactos de una campaña (se mide después), genera la orden de compra o da de baja / manda a revisión prendas. Descartar pide motivo; Posponer la oculta N días.' + NCHAR(10) + N'3. Historial: impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? y hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción.' + NCHAR(10) + N'Metas: objetivo mensual por indicador con real, proyección y semáforo; en riesgo, sube la prioridad de lo que la mueve y se avisa al iniciar sesión.' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: lo que dispara cada regla. Informe ejecutivo: PDF con todo.'),
    (N'ES', N'ayuda.CentroDecisionesForm',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar → medir.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: período y plan; indicadores contra el período anterior (▲/▼) y gráficos con 3 meses proyectados.' + NCHAR(10) + N'2. Recomendaciones: qué pasa, por qué, qué hacer y su impacto. Aceptar y ejecutar siempre hace algo: sugiere, extiende o pide la baja de una promoción, exporta los contactos de una campaña (se mide después), genera la orden de compra o da de baja / manda a revisión prendas. Descartar pide motivo; Posponer la oculta N días.' + NCHAR(10) + N'3. Historial: impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? y hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción.' + NCHAR(10) + N'Metas: objetivo mensual por indicador con real, proyección y semáforo; en riesgo, sube la prioridad de lo que la mueve y se avisa al iniciar sesión.' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: lo que dispara cada regla. Informe ejecutivo: PDF con todo.',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar → medir.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: período y plan; indicadores contra el período anterior (▲/▼) y gráficos con 3 meses proyectados.' + NCHAR(10) + N'1b. Cohortes: retención por mes de alta y peor cohorte.' + NCHAR(10) + N'2. Recomendaciones: qué pasa, por qué, qué hacer y su impacto. Aceptar y ejecutar siempre hace algo: sugiere, extiende o pide la baja de una promoción, exporta los contactos de una campaña (se mide después), genera la orden de compra o da de baja / manda a revisión prendas. Descartar pide motivo; Posponer la oculta N días.' + NCHAR(10) + N'3. Historial: impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? y hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción.' + NCHAR(10) + N'Metas: objetivo mensual por indicador con real, proyección y semáforo; en riesgo, sube la prioridad de lo que la mueve y se avisa al iniciar sesión.' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: lo que dispara cada regla. Informe ejecutivo: PDF con todo.'),
    (N'ES', N'ayuda.CentroDecisionesForm',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar promoción → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: elija el período (o un atajo) y el plan. Cada indicador se compara con el período anterior de igual duración (▲/▼). Los gráficos muestran la tendencia con 3 meses proyectados (punteado), el ingreso en riesgo, el stock y la rotación.' + NCHAR(10) + N'1b. Cohortes: retención por mes de alta y peor cohorte.' + NCHAR(10) + N'2. Recomendaciones: qué está pasando, por qué, qué hacer y cuánto vale (con la fórmula). Elija una opción y pulse Aceptar y ejecutar, Descartar (con motivo) o Posponer; Simular esta opción la carga en el simulador.' + NCHAR(10) + N'3. Historial: las decisiones tomadas, con el impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? con la retención de equilibrio; compare hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción (mantener, extender o dar de baja).' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: los valores que disparan cada regla. Informe ejecutivo: PDF con todo lo anterior.',
        N'Inteligencia comercial y promociones: analizar → recomendar/decidir → simular → aplicar → medir.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analizar: período y plan; indicadores contra el período anterior (▲/▼) y gráficos con 3 meses proyectados.' + NCHAR(10) + N'1b. Cohortes: retención por mes de alta y peor cohorte.' + NCHAR(10) + N'2. Recomendaciones: qué pasa, por qué, qué hacer y su impacto. Aceptar y ejecutar siempre hace algo: sugiere, extiende o pide la baja de una promoción, exporta los contactos de una campaña (se mide después), genera la orden de compra o da de baja / manda a revisión prendas. Descartar pide motivo; Posponer la oculta N días.' + NCHAR(10) + N'3. Historial: impacto estimado contra el real.' + NCHAR(10) + N'4. Simulador: ¿qué pasa si…? y hasta 3 escenarios.' + NCHAR(10) + N'5. Medir resultado: veredicto por promoción.' + NCHAR(10) + N'Metas: objetivo mensual por indicador con real, proyección y semáforo; en riesgo, sube la prioridad de lo que la mueve y se avisa al iniciar sesión.' + NCHAR(10) + N'' + NCHAR(10) + N'Umbrales: lo que dispara cada regla. Informe ejecutivo: PDF con todo.'),
    (N'EN', N'ayuda.CentroDecisionesForm',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply promotion → measure results.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: choose the period (or a shortcut) and the plan. Each indicator is compared with the previous period of the same length (▲/▼). The charts show the trend with 3 projected months (dashed), revenue at risk, stock and turnover.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and how much it is worth (with the formula). Choose an option and press Accept and execute (opens the existing flow), Discard (with a reason) or Postpone; Simulate this option loads it into the simulator.' + NCHAR(10) + N'3. History: the decisions taken, with estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? with the break-even retention; compare up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion (keep, extend or withdraw).' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: the values that trigger each rule. Executive report: PDF with all of the above.',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply → measure.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: period and plan; indicators against the previous period (▲/▼) and charts with 3 projected months.' + NCHAR(10) + N'1b. Cohorts: retention by sign-up month and worst cohort.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and its impact. Accept and execute always does something: it suggests, extends or asks to withdraw a promotion, exports a campaign''s contacts (measured later), generates the purchase order or writes off / sends garments for review. Discard asks for a reason; Postpone hides it N days.' + NCHAR(10) + N'3. History: estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? and up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion.' + NCHAR(10) + N'Goals: monthly target per indicator with actual, projection and traffic light; at risk, it raises the priority of what moves it and is announced at login.' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: what triggers each rule. Executive report: PDF with everything.'),
    (N'EN', N'ayuda.CentroDecisionesForm',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply → measure.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: period and plan; indicators against the previous period (▲/▼) and charts with 3 projected months.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and its impact. Accept and execute always does something: it suggests, extends or asks to withdraw a promotion, exports a campaign''s contacts (measured later), generates the purchase order or writes off / sends garments for review. Discard asks for a reason; Postpone hides it N days.' + NCHAR(10) + N'3. History: estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? and up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion.' + NCHAR(10) + N'Goals: monthly target per indicator with actual, projection and traffic light; at risk, it raises the priority of what moves it and is announced at login.' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: what triggers each rule. Executive report: PDF with everything.',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply → measure.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: period and plan; indicators against the previous period (▲/▼) and charts with 3 projected months.' + NCHAR(10) + N'1b. Cohorts: retention by sign-up month and worst cohort.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and its impact. Accept and execute always does something: it suggests, extends or asks to withdraw a promotion, exports a campaign''s contacts (measured later), generates the purchase order or writes off / sends garments for review. Discard asks for a reason; Postpone hides it N days.' + NCHAR(10) + N'3. History: estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? and up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion.' + NCHAR(10) + N'Goals: monthly target per indicator with actual, projection and traffic light; at risk, it raises the priority of what moves it and is announced at login.' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: what triggers each rule. Executive report: PDF with everything.'),
    (N'EN', N'ayuda.CentroDecisionesForm',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply promotion → measure results.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: choose the period (or a shortcut) and the plan. Each indicator is compared with the previous period of the same length (▲/▼). The charts show the trend with 3 projected months (dashed), revenue at risk, stock and turnover.' + NCHAR(10) + N'1b. Cohorts: retention by sign-up month and worst cohort.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and how much it is worth (with the formula). Choose an option and press Accept and execute, Discard (with a reason) or Postpone; Simulate this option loads it into the simulator.' + NCHAR(10) + N'3. History: the decisions taken, with estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? with the break-even retention; compare up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion (keep, extend or withdraw).' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: the values that trigger each rule. Executive report: PDF with all of the above.',
        N'Business intelligence and promotions: analyse → recommend/decide → simulate → apply → measure.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analyse: period and plan; indicators against the previous period (▲/▼) and charts with 3 projected months.' + NCHAR(10) + N'1b. Cohorts: retention by sign-up month and worst cohort.' + NCHAR(10) + N'2. Recommendations: what is happening, why, what to do and its impact. Accept and execute always does something: it suggests, extends or asks to withdraw a promotion, exports a campaign''s contacts (measured later), generates the purchase order or writes off / sends garments for review. Discard asks for a reason; Postpone hides it N days.' + NCHAR(10) + N'3. History: estimated against actual impact.' + NCHAR(10) + N'4. Simulator: what if…? and up to 3 scenarios.' + NCHAR(10) + N'5. Measure results: verdict per promotion.' + NCHAR(10) + N'Goals: monthly target per indicator with actual, projection and traffic light; at risk, it raises the priority of what moves it and is announced at login.' + NCHAR(10) + N'' + NCHAR(10) + N'Thresholds: what triggers each rule. Executive report: PDF with everything.'),
    (N'RU', N'ayuda.CentroDecisionesForm',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → запуск акции → оценка результата.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: выберите период (или быстрый выбор) и тариф. Каждый показатель сравнивается с предыдущим периодом той же длины (▲/▼). Графики: тенденция с прогнозом на 3 месяца (пунктир), доход под риском, запасы и оборот.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и сколько это стоит (с формулой). Выберите вариант и нажмите «Принять и выполнить» (открывает существующий процесс), «Отклонить» (с причиной) или «Отложить»; «Смоделировать этот вариант» загружает его в симулятор.' + NCHAR(10) + N'3. История: принятые решения, оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? с безубыточным удержанием; сравнение до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по каждой акции (сохранить, продлить или отменить).' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: значения, запускающие правила. Отчёт для руководства: PDF со всем перечисленным.',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → применение → оценка.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: период и тариф; показатели против прошлого периода (▲/▼) и графики с прогнозом на 3 месяца.' + NCHAR(10) + N'1b. Когорты: удержание по месяцу подписки и худшая когорта.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и эффект. «Принять и выполнить» всегда что-то делает: предлагает, продлевает или отменяет акцию, выгружает контакты кампании (результат измеряется позже), формирует заказ на закупку или списывает / отправляет вещи на проверку. «Отклонить» требует причины; «Отложить» скрывает на N дней.' + NCHAR(10) + N'3. История: оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? и до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по акции.' + NCHAR(10) + N'Цели: месячная цель по показателю с фактом, прогнозом и светофором; под угрозой — растёт приоритет того, что на неё влияет, и о ней сообщается при входе.' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: что запускает правила. Отчёт для руководства: PDF со всем.'),
    (N'RU', N'ayuda.CentroDecisionesForm',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → применение → оценка.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: период и тариф; показатели против прошлого периода (▲/▼) и графики с прогнозом на 3 месяца.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и эффект. «Принять и выполнить» всегда что-то делает: предлагает, продлевает или отменяет акцию, выгружает контакты кампании (результат измеряется позже), формирует заказ на закупку или списывает / отправляет вещи на проверку. «Отклонить» требует причины; «Отложить» скрывает на N дней.' + NCHAR(10) + N'3. История: оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? и до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по акции.' + NCHAR(10) + N'Цели: месячная цель по показателю с фактом, прогнозом и светофором; под угрозой — растёт приоритет того, что на неё влияет, и о ней сообщается при входе.' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: что запускает правила. Отчёт для руководства: PDF со всем.',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → применение → оценка.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: период и тариф; показатели против прошлого периода (▲/▼) и графики с прогнозом на 3 месяца.' + NCHAR(10) + N'1b. Когорты: удержание по месяцу подписки и худшая когорта.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и эффект. «Принять и выполнить» всегда что-то делает: предлагает, продлевает или отменяет акцию, выгружает контакты кампании (результат измеряется позже), формирует заказ на закупку или списывает / отправляет вещи на проверку. «Отклонить» требует причины; «Отложить» скрывает на N дней.' + NCHAR(10) + N'3. История: оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? и до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по акции.' + NCHAR(10) + N'Цели: месячная цель по показателю с фактом, прогнозом и светофором; под угрозой — растёт приоритет того, что на неё влияет, и о ней сообщается при входе.' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: что запускает правила. Отчёт для руководства: PDF со всем.'),
    (N'RU', N'ayuda.CentroDecisionesForm',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → запуск акции → оценка результата.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: выберите период (или быстрый выбор) и тариф. Каждый показатель сравнивается с предыдущим периодом той же длины (▲/▼). Графики: тенденция с прогнозом на 3 месяца (пунктир), доход под риском, запасы и оборот.' + NCHAR(10) + N'1b. Когорты: удержание по месяцу подписки и худшая когорта.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и сколько это стоит (с формулой). Выберите вариант и нажмите «Принять и выполнить», «Отклонить» (с причиной) или «Отложить»; «Смоделировать этот вариант» загружает его в симулятор.' + NCHAR(10) + N'3. История: принятые решения, оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? с безубыточным удержанием; сравнение до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по каждой акции (сохранить, продлить или отменить).' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: значения, запускающие правила. Отчёт для руководства: PDF со всем перечисленным.',
        N'Коммерческая аналитика и акции: анализ → рекомендация/решение → моделирование → применение → оценка.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Анализ: период и тариф; показатели против прошлого периода (▲/▼) и графики с прогнозом на 3 месяца.' + NCHAR(10) + N'1b. Когорты: удержание по месяцу подписки и худшая когорта.' + NCHAR(10) + N'2. Рекомендации: что происходит, почему, что делать и эффект. «Принять и выполнить» всегда что-то делает: предлагает, продлевает или отменяет акцию, выгружает контакты кампании (результат измеряется позже), формирует заказ на закупку или списывает / отправляет вещи на проверку. «Отклонить» требует причины; «Отложить» скрывает на N дней.' + NCHAR(10) + N'3. История: оценка эффекта против факта.' + NCHAR(10) + N'4. Симулятор: что если…? и до 3 сценариев.' + NCHAR(10) + N'5. Оценка результата: вердикт по акции.' + NCHAR(10) + N'Цели: месячная цель по показателю с фактом, прогнозом и светофором; под угрозой — растёт приоритет того, что на неё влияет, и о ней сообщается при входе.' + NCHAR(10) + N'' + NCHAR(10) + N'Пороги: что запускает правила. Отчёт для руководства: PDF со всем.'),
    (N'PT', N'ayuda.CentroDecisionesForm',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar promoção → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: escolha o período (ou um atalho) e o plano. Cada indicador é comparado com o período anterior de mesma duração (▲/▼). Os gráficos mostram a tendência com 3 meses projetados (tracejado), a receita em risco, o estoque e a rotatividade.' + NCHAR(10) + N'2. Recomendações: o que está acontecendo, por quê, o que fazer e quanto vale (com a fórmula). Escolha uma opção e clique em Aceitar e executar (abre o fluxo existente), Descartar (com motivo) ou Adiar; Simular esta opção a carrega no simulador.' + NCHAR(10) + N'3. Histórico: as decisões tomadas, com o impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? com a retenção de equilíbrio; compare até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção (manter, estender ou dar baixa).' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: os valores que disparam cada regra. Relatório executivo: PDF com tudo isso.',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar → medir.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: período e plano; indicadores contra o período anterior (▲/▼) e gráficos com 3 meses projetados.' + NCHAR(10) + N'1b. Coortes: retenção por mês de adesão e pior coorte.' + NCHAR(10) + N'2. Recomendações: o que acontece, por quê, o que fazer e seu impacto. Aceitar e executar sempre faz algo: sugere, estende ou pede a baixa de uma promoção, exporta os contatos de uma campanha (medida depois), gera a ordem de compra ou dá baixa / envia peças para revisão. Descartar pede motivo; Adiar a oculta N dias.' + NCHAR(10) + N'3. Histórico: impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? e até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção.' + NCHAR(10) + N'Metas: objetivo mensal por indicador com real, projeção e semáforo; em risco, aumenta a prioridade do que a move e é avisada ao iniciar a sessão.' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: o que dispara cada regra. Relatório executivo: PDF com tudo.'),
    (N'PT', N'ayuda.CentroDecisionesForm',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar → medir.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: período e plano; indicadores contra o período anterior (▲/▼) e gráficos com 3 meses projetados.' + NCHAR(10) + N'2. Recomendações: o que acontece, por quê, o que fazer e seu impacto. Aceitar e executar sempre faz algo: sugere, estende ou pede a baixa de uma promoção, exporta os contatos de uma campanha (medida depois), gera a ordem de compra ou dá baixa / envia peças para revisão. Descartar pede motivo; Adiar a oculta N dias.' + NCHAR(10) + N'3. Histórico: impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? e até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção.' + NCHAR(10) + N'Metas: objetivo mensal por indicador com real, projeção e semáforo; em risco, aumenta a prioridade do que a move e é avisada ao iniciar a sessão.' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: o que dispara cada regra. Relatório executivo: PDF com tudo.',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar → medir.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: período e plano; indicadores contra o período anterior (▲/▼) e gráficos com 3 meses projetados.' + NCHAR(10) + N'1b. Coortes: retenção por mês de adesão e pior coorte.' + NCHAR(10) + N'2. Recomendações: o que acontece, por quê, o que fazer e seu impacto. Aceitar e executar sempre faz algo: sugere, estende ou pede a baixa de uma promoção, exporta os contatos de uma campanha (medida depois), gera a ordem de compra ou dá baixa / envia peças para revisão. Descartar pede motivo; Adiar a oculta N dias.' + NCHAR(10) + N'3. Histórico: impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? e até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção.' + NCHAR(10) + N'Metas: objetivo mensal por indicador com real, projeção e semáforo; em risco, aumenta a prioridade do que a move e é avisada ao iniciar a sessão.' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: o que dispara cada regra. Relatório executivo: PDF com tudo.'),
    (N'PT', N'ayuda.CentroDecisionesForm',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar promoção → medir resultado.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: escolha o período (ou um atalho) e o plano. Cada indicador é comparado com o período anterior de mesma duração (▲/▼). Os gráficos mostram a tendência com 3 meses projetados (tracejado), a receita em risco, o estoque e a rotatividade.' + NCHAR(10) + N'1b. Coortes: retenção por mês de adesão e pior coorte.' + NCHAR(10) + N'2. Recomendações: o que está acontecendo, por quê, o que fazer e quanto vale (com a fórmula). Escolha uma opção e clique em Aceitar e executar, Descartar (com motivo) ou Adiar; Simular esta opção a carrega no simulador.' + NCHAR(10) + N'3. Histórico: as decisões tomadas, com o impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? com a retenção de equilíbrio; compare até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção (manter, estender ou dar baixa).' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: os valores que disparam cada regra. Relatório executivo: PDF com tudo isso.',
        N'Inteligência comercial e promoções: analisar → recomendar/decidir → simular → aplicar → medir.' + NCHAR(10) + N'' + NCHAR(10) + N'1. Analisar: período e plano; indicadores contra o período anterior (▲/▼) e gráficos com 3 meses projetados.' + NCHAR(10) + N'1b. Coortes: retenção por mês de adesão e pior coorte.' + NCHAR(10) + N'2. Recomendações: o que acontece, por quê, o que fazer e seu impacto. Aceitar e executar sempre faz algo: sugere, estende ou pede a baixa de uma promoção, exporta os contatos de uma campanha (medida depois), gera a ordem de compra ou dá baixa / envia peças para revisão. Descartar pede motivo; Adiar a oculta N dias.' + NCHAR(10) + N'3. Histórico: impacto estimado contra o real.' + NCHAR(10) + N'4. Simulador: e se…? e até 3 cenários.' + NCHAR(10) + N'5. Medir resultado: veredito por promoção.' + NCHAR(10) + N'Metas: objetivo mensal por indicador com real, projeção e semáforo; em risco, aumenta a prioridade do que a move e é avisada ao iniciar a sessão.' + NCHAR(10) + N'' + NCHAR(10) + N'Limites: o que dispara cada regra. Relatório executivo: PDF com tudo.');

    UPDATE t SET t.Texto = f.TextoNuevo
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @AyudaFinal f ON f.Clave = c.Clave AND f.Codigo = i.Codigo
    WHERE t.Texto COLLATE Latin1_General_BIN2 = f.TextoAnterior COLLATE Latin1_General_BIN2; -- exacto: no pisa ediciones
    PRINT CONCAT('Ayuda del Centro de decisiones (texto final): ', @@ROWCOUNT, ' traducción(es) actualizada(s).');
END
GO
PRINT 'Sección 21z13: ayuda final del Centro de decisiones verificada.';
GO

-- ============================================================
-- WardrobeFlow — 21z14. CICLO DEL INFORME EJECUTIVO, MESES DE LAS METAS E HISTORIAL EN XML O JSON
-- ------------------------------------------------------------
-- · cdc.informe.ciclo (portada del Informe ejecutivo): el mismo ciclo que la ayuda F1 del Centro
--   (analizar → recomendar/decidir → simular → aplicar → medir), sin «aplicar promoción → medir resultado».
-- · cdc.meta.ninguna / cdc.meta.guardada / cdc.meta.confirmareliminar: el mes se muestra como «yyyy-MM»
--   (igual que las cohortes); con {0:MMMM yyyy} el nombre del mes salía en español en EN, PT y RU
--   porque la cultura de la aplicación es fija (es-AR).
-- · ux.hint.VersionHistorialForm: las versiones se exportan a XML o JSON, no solo a XML.
-- · glos.campania.formula (glosario del Centro de decisiones): una campaña de contacto con la ventana de
--   30 días abierta ya no muestra un importe «parcial» (restaba la tasa base antes de que nadie respondiera
--   y daba negativo): muestra el avance (respondieron de contactados y el día en que cierra), no se suma
--   en los totales y al cerrar se muestra el resultado, aunque sea negativo. Además, la tasa base se
--   resta sobre los COMPARABLES (los contactados que resolvieron una renovación en la ventana), la
--   misma población con la que se calcula la base en la ventana anterior: antes se restaba sobre todos
--   los contactados y una campaña igual a la rutina daba negativo. Cadena (cada texto anterior → final):
--     · texto «parcial hasta que cierran»       → texto final
--     · texto «sin importe mientras está abierta» → texto final
-- · ayuda.UmbralesComercialesDialog (F1 de los umbrales): sin la frase de la ocupación mínima y máxima
--   (esos umbrales ya no existen).
-- Las claves ya existen en una base instalada: se actualizan SOLO si siguen con el texto de fábrica
-- anterior (una edición hecha en Idiomas se respeta). Idempotente.
-- ============================================================
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @Textos21z14 TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                                TextoAnterior NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @Textos21z14 (Codigo, Clave, TextoAnterior, TextoNuevo) VALUES
    (N'ES', N'cdc.informe.ciclo',
        N'Analizar → recomendar/decidir → simular → aplicar promoción → medir resultado',
        N'Analizar → recomendar/decidir → simular → aplicar → medir'),
    (N'EN', N'cdc.informe.ciclo',
        N'Analyse → recommend/decide → simulate → apply promotion → measure results',
        N'Analyse → recommend/decide → simulate → apply → measure'),
    (N'RU', N'cdc.informe.ciclo',
        N'Анализ → рекомендация/решение → моделирование → запуск акции → оценка результата',
        N'Анализ → рекомендация/решение → моделирование → применение → оценка'),
    (N'PT', N'cdc.informe.ciclo',
        N'Analisar → recomendar/decidir → simular → aplicar promoção → medir resultado',
        N'Analisar → recomendar/decidir → simular → aplicar → medir'),
    (N'ES', N'cdc.meta.ninguna',
        N'No hay metas para {0:MMMM yyyy}. Elija un indicador, escriba el objetivo y pulse Guardar meta.',
        N'No hay metas para {0:yyyy-MM}. Elija un indicador, escriba el objetivo y pulse Guardar meta.'),
    (N'EN', N'cdc.meta.ninguna',
        N'There are no goals for {0:MMMM yyyy}. Choose an indicator, enter the target and press Save goal.',
        N'There are no goals for {0:yyyy-MM}. Choose an indicator, enter the target and press Save goal.'),
    (N'RU', N'cdc.meta.ninguna',
        N'На {0:MMMM yyyy} целей нет. Выберите показатель, введите цель и нажмите «Сохранить цель».',
        N'На {0:yyyy-MM} целей нет. Выберите показатель, введите цель и нажмите «Сохранить цель».'),
    (N'PT', N'cdc.meta.ninguna',
        N'Não há metas para {0:MMMM yyyy}. Escolha um indicador, digite o objetivo e clique em Salvar meta.',
        N'Não há metas para {0:yyyy-MM}. Escolha um indicador, digite o objetivo e clique em Salvar meta.'),
    (N'ES', N'cdc.meta.guardada', N'Meta guardada: {0} para {1:MMMM yyyy}.', N'Meta guardada: {0} para {1:yyyy-MM}.'),
    (N'EN', N'cdc.meta.guardada', N'Goal saved: {0} for {1:MMMM yyyy}.', N'Goal saved: {0} for {1:yyyy-MM}.'),
    (N'RU', N'cdc.meta.guardada', N'Цель сохранена: {0} на {1:MMMM yyyy}.', N'Цель сохранена: {0} на {1:yyyy-MM}.'),
    (N'PT', N'cdc.meta.guardada', N'Meta salva: {0} para {1:MMMM yyyy}.', N'Meta salva: {0} para {1:yyyy-MM}.'),
    (N'ES', N'cdc.meta.confirmareliminar', N'¿Eliminar la meta de {0} para {1:MMMM yyyy}?', N'¿Eliminar la meta de {0} para {1:yyyy-MM}?'),
    (N'EN', N'cdc.meta.confirmareliminar', N'Delete the {0} goal for {1:MMMM yyyy}?', N'Delete the {0} goal for {1:yyyy-MM}?'),
    (N'RU', N'cdc.meta.confirmareliminar', N'Удалить цель «{0}» на {1:MMMM yyyy}?', N'Удалить цель «{0}» на {1:yyyy-MM}?'),
    (N'PT', N'cdc.meta.confirmareliminar', N'Excluir a meta de {0} para {1:MMMM yyyy}?', N'Excluir a meta de {0} para {1:yyyy-MM}?'),
    (N'ES', N'ux.hint.VersionHistorialForm',
        N'Elija un usuario y presione Cargar para ver sus versiones anteriores; puede restaurar una o exportarlas a XML.',
        N'Elija un usuario y presione Cargar para ver sus versiones anteriores; puede restaurar una o exportarlas a XML o JSON.'),
    (N'EN', N'ux.hint.VersionHistorialForm',
        N'Select a user and press Load to see their previous versions; you can restore one or export them to XML.',
        N'Select a user and press Load to see their previous versions; you can restore one or export them to XML or JSON.'),
    (N'RU', N'ux.hint.VersionHistorialForm',
        N'Выберите пользователя и нажмите «Загрузить», чтобы увидеть его предыдущие версии; одну из них можно восстановить или выгрузить все в XML.',
        N'Выберите пользователя и нажмите «Загрузить», чтобы увидеть его предыдущие версии; одну из них можно восстановить или выгрузить все в XML или JSON.'),
    (N'PT', N'ux.hint.VersionHistorialForm',
        N'Escolha um usuário e pressione Carregar para ver suas versões anteriores; você pode restaurar uma ou exportá-las para XML.',
        N'Escolha um usuário e pressione Carregar para ver suas versões anteriores; você pode restaurar uma ou exportá-las para XML ou JSON.'),
    (N'ES', N'glos.campania.formula',
        N'(respondieron − tasa base × contactados) × valor por respuesta, en los 30 días desde la ejecución (parcial hasta que cierran). Responde: renovó o cambió de plan (upgrade: pasó a un plan más caro). Tasa base: la de los planes contactados en los 30 días anteriores. Valor: precio actual del plan (bajas: × horizonte; upgrade: diferencia × horizonte). Cada cliente cuenta para la primera campaña que lo contactó.',
        N'(respondieron − tasa base × comparables) × valor por respuesta, en los 30 días desde la ejecución. Comparables: los contactados que resolvieron una renovación en la ventana (renovaron, cambiaron de plan o se dieron de baja); un contactado sin movimiento no cuenta ni a favor ni en contra. Responde: renovó o cambió de plan (upgrade: pasó a un plan más caro). Tasa base: con la misma regla, en los 30 días anteriores, de los clientes de los planes contactados que resolvieron una renovación, la parte que respondió. Mientras la ventana está abierta no hay importe: se muestra el avance (respondieron de contactados y el día en que cierra) y no se suma en los totales; al cerrar se muestra el resultado, aunque sea negativo. Valor: precio actual del plan (bajas: × horizonte; upgrade: diferencia × horizonte). Cada cliente cuenta para la primera campaña que lo contactó.'),
    (N'ES', N'glos.campania.formula',
        N'(respondieron − tasa base × contactados) × valor por respuesta, en los 30 días desde la ejecución. Mientras la ventana está abierta no hay importe: se muestra el avance (respondieron de contactados y el día en que cierra) y no se suma en los totales; al cerrar se muestra el resultado, aunque sea negativo. Responde: renovó o cambió de plan (upgrade: pasó a un plan más caro). Tasa base: la de los planes contactados en los 30 días anteriores. Valor: precio actual del plan (bajas: × horizonte; upgrade: diferencia × horizonte). Cada cliente cuenta para la primera campaña que lo contactó.',
        N'(respondieron − tasa base × comparables) × valor por respuesta, en los 30 días desde la ejecución. Comparables: los contactados que resolvieron una renovación en la ventana (renovaron, cambiaron de plan o se dieron de baja); un contactado sin movimiento no cuenta ni a favor ni en contra. Responde: renovó o cambió de plan (upgrade: pasó a un plan más caro). Tasa base: con la misma regla, en los 30 días anteriores, de los clientes de los planes contactados que resolvieron una renovación, la parte que respondió. Mientras la ventana está abierta no hay importe: se muestra el avance (respondieron de contactados y el día en que cierra) y no se suma en los totales; al cerrar se muestra el resultado, aunque sea negativo. Valor: precio actual del plan (bajas: × horizonte; upgrade: diferencia × horizonte). Cada cliente cuenta para la primera campaña que lo contactó.'),
    (N'EN', N'glos.campania.formula',
        N'(responded − base rate × contacted) × value per response, over the 30 days from execution (partial until they close). Responds: renewed or changed plan (upgrade: moved to a more expensive plan). Base rate: that of the contacted plans in the previous 30 days. Value: current plan price (churn: × horizon; upgrade: difference × horizon). Each customer counts for the first campaign that contacted them.',
        N'(responded − base rate × comparable) × value per response, over the 30 days from execution. Comparable: the contacted customers who resolved a renewal in the window (renewed, changed plan or cancelled); a contacted customer with no movement counts neither for nor against. Responds: renewed or changed plan (upgrade: moved to a more expensive plan). Base rate: with the same rule, in the previous 30 days, of the customers on the contacted plans who resolved a renewal, the share that responded. While the window is open there is no amount: the progress is shown (responded out of contacted and the day it closes) and it is not added to the totals; at closing the result is shown, even if negative. Value: current plan price (churn: × horizon; upgrade: difference × horizon). Each customer counts for the first campaign that contacted them.'),
    (N'EN', N'glos.campania.formula',
        N'(responded − base rate × contacted) × value per response, over the 30 days from execution. While the window is open there is no amount: the progress is shown (responded out of contacted and the day it closes) and it is not added to the totals; at closing the result is shown, even if negative. Responds: renewed or changed plan (upgrade: moved to a more expensive plan). Base rate: that of the contacted plans in the previous 30 days. Value: current plan price (churn: × horizon; upgrade: difference × horizon). Each customer counts for the first campaign that contacted them.',
        N'(responded − base rate × comparable) × value per response, over the 30 days from execution. Comparable: the contacted customers who resolved a renewal in the window (renewed, changed plan or cancelled); a contacted customer with no movement counts neither for nor against. Responds: renewed or changed plan (upgrade: moved to a more expensive plan). Base rate: with the same rule, in the previous 30 days, of the customers on the contacted plans who resolved a renewal, the share that responded. While the window is open there is no amount: the progress is shown (responded out of contacted and the day it closes) and it is not added to the totals; at closing the result is shown, even if negative. Value: current plan price (churn: × horizon; upgrade: difference × horizon). Each customer counts for the first campaign that contacted them.'),
    (N'RU', N'glos.campania.formula',
        N'(ответили − базовая доля × охвачено) × ценность ответа, за 30 дней с выполнения (частично, пока окно не закрыто). Ответ: продлил или сменил тариф (апгрейд: перешёл на более дорогой). Базовая доля — у тех же тарифов за 30 дней до этого. Ценность: текущая цена тарифа (отказы: × горизонт; апгрейд: разница × горизонт). Каждый клиент учитывается в первой кампании, которая с ним связалась.',
        N'(ответили − базовая доля × сопоставимые) × ценность ответа, за 30 дней с выполнения. Сопоставимые: охваченные клиенты, у которых в окне было решение по продлению (продлили, сменили тариф или отказались); охваченный клиент без движения не учитывается ни в плюс, ни в минус. Ответ: продлил или сменил тариф (апгрейд: перешёл на более дорогой). Базовая доля: по тому же правилу, за 30 дней до этого, из клиентов охваченных тарифов, у которых было решение по продлению, доля ответивших. Пока окно открыто, суммы нет: показан ход измерения (сколько из охваченных ответили и день закрытия), и в итоги она не входит; при закрытии показывается результат, даже отрицательный. Ценность: текущая цена тарифа (отказы: × горизонт; апгрейд: разница × горизонт). Каждый клиент учитывается в первой кампании, которая с ним связалась.'),
    (N'RU', N'glos.campania.formula',
        N'(ответили − базовая доля × охвачено) × ценность ответа, за 30 дней с выполнения. Пока окно открыто, суммы нет: показан ход измерения (сколько из охваченных ответили и день закрытия), и в итоги она не входит; при закрытии показывается результат, даже отрицательный. Ответ: продлил или сменил тариф (апгрейд: перешёл на более дорогой). Базовая доля — у тех же тарифов за 30 дней до этого. Ценность: текущая цена тарифа (отказы: × горизонт; апгрейд: разница × горизонт). Каждый клиент учитывается в первой кампании, которая с ним связалась.',
        N'(ответили − базовая доля × сопоставимые) × ценность ответа, за 30 дней с выполнения. Сопоставимые: охваченные клиенты, у которых в окне было решение по продлению (продлили, сменили тариф или отказались); охваченный клиент без движения не учитывается ни в плюс, ни в минус. Ответ: продлил или сменил тариф (апгрейд: перешёл на более дорогой). Базовая доля: по тому же правилу, за 30 дней до этого, из клиентов охваченных тарифов, у которых было решение по продлению, доля ответивших. Пока окно открыто, суммы нет: показан ход измерения (сколько из охваченных ответили и день закрытия), и в итоги она не входит; при закрытии показывается результат, даже отрицательный. Ценность: текущая цена тарифа (отказы: × горизонт; апгрейд: разница × горизонт). Каждый клиент учитывается в первой кампании, которая с ним связалась.'),
    (N'PT', N'glos.campania.formula',
        N'(responderam − taxa base × contatados) × valor por resposta, nos 30 dias desde a execução (parcial até fecharem). Responde: renovou ou mudou de plano (upgrade: passou a um plano mais caro). Taxa base: a dos planos contatados nos 30 dias anteriores. Valor: preço atual do plano (cancelamentos: × horizonte; upgrade: diferença × horizonte). Cada cliente conta para a primeira campanha que o contatou.',
        N'(responderam − taxa base × comparáveis) × valor por resposta, nos 30 dias desde a execução. Comparáveis: os contatados que resolveram uma renovação na janela (renovaram, mudaram de plano ou cancelaram); um contatado sem movimento não conta nem a favor nem contra. Responde: renovou ou mudou de plano (upgrade: passou a um plano mais caro). Taxa base: com a mesma regra, nos 30 dias anteriores, dos clientes dos planos contatados que resolveram uma renovação, a parte que respondeu. Enquanto a janela está aberta não há valor: mostra-se o andamento (responderam de contatados e o dia em que fecha) e não é somado nos totais; ao fechar mostra-se o resultado, mesmo que negativo. Valor: preço atual do plano (cancelamentos: × horizonte; upgrade: diferença × horizonte). Cada cliente conta para a primeira campanha que o contatou.'),
    (N'PT', N'glos.campania.formula',
        N'(responderam − taxa base × contatados) × valor por resposta, nos 30 dias desde a execução. Enquanto a janela está aberta não há valor: mostra-se o andamento (responderam de contatados e o dia em que fecha) e não é somado nos totais; ao fechar mostra-se o resultado, mesmo que negativo. Responde: renovou ou mudou de plano (upgrade: passou a um plano mais caro). Taxa base: a dos planos contatados nos 30 dias anteriores. Valor: preço atual do plano (cancelamentos: × horizonte; upgrade: diferença × horizonte). Cada cliente conta para a primeira campanha que o contatou.',
        N'(responderam − taxa base × comparáveis) × valor por resposta, nos 30 dias desde a execução. Comparáveis: os contatados que resolveram uma renovação na janela (renovaram, mudaram de plano ou cancelaram); um contatado sem movimento não conta nem a favor nem contra. Responde: renovou ou mudou de plano (upgrade: passou a um plano mais caro). Taxa base: com a mesma regra, nos 30 dias anteriores, dos clientes dos planos contatados que resolveram uma renovação, a parte que respondeu. Enquanto a janela está aberta não há valor: mostra-se o andamento (responderam de contatados e o dia em que fecha) e não é somado nos totais; ao fechar mostra-se o resultado, mesmo que negativo. Valor: preço atual do plano (cancelamentos: × horizonte; upgrade: diferença × horizonte). Cada cliente conta para a primeira campanha que o contatou.'),
    (N'ES', N'ayuda.UmbralesComercialesDialog',
        N'Cada umbral decide cuándo el motor del Centro de decisiones propone una acción. Edite la columna Valor (dentro del rango indicado) y pulse Guardar; los cambios rigen desde el próximo análisis y quedan en la bitácora.' + NCHAR(10) + N'' + NCHAR(10) + N'• Restablecer valores de fábrica: vuelve a los valores por defecto (sin guardar todavía).' + NCHAR(10) + N'• El descuento máximo no puede ser menor que el base, y la ocupación mínima tiene que ser menor que la máxima.',
        N'Cada umbral decide cuándo el motor del Centro de decisiones propone una acción. Edite la columna Valor (dentro del rango indicado) y pulse Guardar; los cambios rigen desde el próximo análisis y quedan en la bitácora.' + NCHAR(10) + N'' + NCHAR(10) + N'• Restablecer valores de fábrica: vuelve a los valores por defecto (sin guardar todavía).' + NCHAR(10) + N'• El descuento máximo no puede ser menor que el base.'),
    (N'EN', N'ayuda.UmbralesComercialesDialog',
        N'Each threshold decides when the decision center engine proposes an action. Edit the Value column (within the stated range) and press Save; changes apply from the next analysis and are logged.' + NCHAR(10) + N'' + NCHAR(10) + N'• Restore default values: goes back to the default values (not saved yet).' + NCHAR(10) + N'• The maximum discount cannot be lower than the base one, and the minimum occupancy must be lower than the maximum.',
        N'Each threshold decides when the decision center engine proposes an action. Edit the Value column (within the stated range) and press Save; changes apply from the next analysis and are logged.' + NCHAR(10) + N'' + NCHAR(10) + N'• Restore default values: goes back to the default values (not saved yet).' + NCHAR(10) + N'• The maximum discount cannot be lower than the base one.'),
    (N'RU', N'ayuda.UmbralesComercialesDialog',
        N'Каждый порог определяет, когда система центра решений предлагает действие. Измените столбец «Значение» (в указанном диапазоне) и нажмите «Сохранить»; изменения действуют со следующего анализа и попадают в журнал.' + NCHAR(10) + N'' + NCHAR(10) + N'• «Вернуть значения по умолчанию»: восстанавливает исходные значения (пока без сохранения).' + NCHAR(10) + N'• Максимальная скидка не может быть меньше базовой, а минимальная загрузка должна быть меньше максимальной.',
        N'Каждый порог определяет, когда система центра решений предлагает действие. Измените столбец «Значение» (в указанном диапазоне) и нажмите «Сохранить»; изменения действуют со следующего анализа и попадают в журнал.' + NCHAR(10) + N'' + NCHAR(10) + N'• «Вернуть значения по умолчанию»: восстанавливает исходные значения (пока без сохранения).' + NCHAR(10) + N'• Максимальная скидка не может быть меньше базовой.'),
    (N'PT', N'ayuda.UmbralesComercialesDialog',
        N'Cada limite decide quando o motor da Central de decisões propõe uma ação. Edite a coluna Valor (dentro da faixa indicada) e clique em Salvar; as alterações valem a partir da próxima análise e ficam no registro.' + NCHAR(10) + N'' + NCHAR(10) + N'• Restaurar valores padrão: volta aos valores padrão (ainda sem salvar).' + NCHAR(10) + N'• O desconto máximo não pode ser menor que o base, e a ocupação mínima deve ser menor que a máxima.',
        N'Cada limite decide quando o motor da Central de decisões propõe uma ação. Edite a coluna Valor (dentro da faixa indicada) e clique em Salvar; as alterações valem a partir da próxima análise e ficam no registro.' + NCHAR(10) + N'' + NCHAR(10) + N'• Restaurar valores padrão: volta aos valores padrão (ainda sem salvar).' + NCHAR(10) + N'• O desconto máximo não pode ser menor que o base.');

    UPDATE t SET t.Texto = f.TextoNuevo
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @Textos21z14 f ON f.Clave = c.Clave AND f.Codigo = i.Codigo
    WHERE t.Texto COLLATE Latin1_General_BIN2 = f.TextoAnterior COLLATE Latin1_General_BIN2; -- exacto: no pisa ediciones
    PRINT CONCAT('Ciclo del informe, meses de las metas e historial XML/JSON: ', @@ROWCOUNT, ' traducción(es) actualizada(s).');
END
GO
PRINT 'Sección 21z14: textos del ciclo del informe, de las metas y del historial verificados.';
GO
-- ============================================================
-- WardrobeFlow — 21z15. GUÍA DE PRIMER USO: REPORTES EN EL MENÚ DE CADA ROL
-- ------------------------------------------------------------
-- · ux.guia.gerentecomercial.2 / ux.guia.gerenteinventario.2: el menú «Analítica de Negocio» ya no
--   existe (los reportes están en Ventas e Inventario, cada uno en el menú de su rol); la guía de
--   primer uso nombra los ítems reales.
-- Las claves ya existen en una base instalada: se actualizan SOLO si siguen con el texto de fábrica
-- anterior (una edición hecha en Idiomas se respeta). Idempotente.
-- ============================================================
IF OBJECT_ID('Traduccion') IS NOT NULL AND OBJECT_ID('Control') IS NOT NULL AND OBJECT_ID('Idioma') IS NOT NULL
BEGIN
    DECLARE @Textos21z15 TABLE (Codigo VARCHAR(5) COLLATE DATABASE_DEFAULT NOT NULL, Clave NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
                                TextoAnterior NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL, TextoNuevo NVARCHAR(1000) COLLATE DATABASE_DEFAULT NOT NULL);
    INSERT INTO @Textos21z15 (Codigo, Clave, TextoAnterior, TextoNuevo) VALUES
    (N'ES', N'ux.guia.gerentecomercial.2',
        N'Empiece por Promociones → Centro de decisiones comerciales. Los reportes de abandono y de ventas por vendedor están en Analítica de Negocio.',
        N'Empiece por Promociones → Centro de decisiones comerciales. Los reportes de abandono y de ventas por vendedor están en Ventas → Análisis de Abandono y Ventas → Ventas por Vendedor.'),
    (N'EN', N'ux.guia.gerentecomercial.2',
        N'Start with Promotions → Commercial decision center. The churn and sales-by-salesperson reports are in Business Analytics.',
        N'Start with Promotions → Commercial decision center. The churn and sales-by-salesperson reports are in Sales → Churn Analysis and Sales → Sales by Salesperson.'),
    (N'RU', N'ux.guia.gerentecomercial.2',
        N'Начните с Акции → Центр коммерческих решений. Отчёты об оттоке и о продажах по продавцам находятся в разделе Бизнес-аналитика.',
        N'Начните с Акции → Центр коммерческих решений. Отчёты об оттоке и о продажах по продавцам находятся в Продажи → Анализ оттока и Продажи → Продажи по продавцам.'),
    (N'PT', N'ux.guia.gerentecomercial.2',
        N'Comece por Promoções → Central de decisões comerciais. Os relatórios de abandono e de vendas por vendedor estão em Análise de Negócio.',
        N'Comece por Promoções → Central de decisões comerciais. Os relatórios de abandono e de vendas por vendedor estão em Vendas → Análise de Abandono e Vendas → Vendas por Vendedor.'),
    (N'ES', N'ux.guia.gerenteinventario.2',
        N'Empiece por Analítica de Negocio para los reportes de inventario. La operación diaria está en Inventario y en Ventas → Pedidos Realizados.',
        N'Empiece por Inventario: ahí están sus reportes Rotación de Prendas, Tiempos de Mantenimiento y Escasez de Stock. La operación diaria está en Inventario y en Ventas → Pedidos Realizados.'),
    (N'EN', N'ux.guia.gerenteinventario.2',
        N'Start with Business Analytics for the inventory reports. Day-to-day work is in Inventory and in Sales → Fulfilled Orders.',
        N'Start with Inventory: your Garment Turnover, Maintenance Times and Stock Shortage reports are there. Day-to-day work is in Inventory and in Sales → Fulfilled Orders.'),
    (N'RU', N'ux.guia.gerenteinventario.2',
        N'Начните с раздела Бизнес-аналитика — там отчёты по запасам. Повседневная работа — в разделах Склад и Продажи → Выполненные заказы.',
        N'Начните с раздела Склад: там ваши отчёты Оборот одежды, Сроки обслуживания и Дефицит запасов. Повседневная работа — в разделах Склад и Продажи → Выполненные заказы.'),
    (N'PT', N'ux.guia.gerenteinventario.2',
        N'Comece por Análise de Negócio para os relatórios de estoque. A operação diária está em Estoque e em Vendas → Pedidos Realizados.',
        N'Comece por Estoque: lá estão os seus relatórios Rotatividade de Peças, Tempos de Manutenção e Escassez de Estoque. A operação diária está em Estoque e em Vendas → Pedidos Realizados.');

    UPDATE t SET t.Texto = f.TextoNuevo
    FROM Traduccion t
    JOIN Control c ON c.IdControl = t.IdControl
    JOIN Idioma  i ON i.IdIdioma  = t.IdIdioma
    JOIN @Textos21z15 f ON f.Clave = c.Clave AND f.Codigo = i.Codigo
    WHERE t.Texto COLLATE Latin1_General_BIN2 = f.TextoAnterior COLLATE Latin1_General_BIN2; -- exacto: no pisa ediciones
    PRINT CONCAT('Guía de primer uso (reportes en el menú de cada rol): ', @@ROWCOUNT, ' traducción(es) actualizada(s).');
END
GO
PRINT 'Sección 21z15: textos de la guía de primer uso verificados.';
GO
-- ============================================================
-- WardrobeFlow — 22. DÍGITOS VERIFICADORES: FORMATO 2 Y MIGRACIÓN ÚNICA
-- ------------------------------------------------------------
-- Formato 2 de los dígitos verificadores (DAL.DigitoVerificador.FormatoActual = 2):
--   • Usuario  también protege Activo, RequiereCambioClave, CantidadBloqueos y FechaBloqueo.
--   • Cliente  también protege plan, vencimientos, gracia, pausa, crédito, referente y Activo.
--   • Pedido   también protege las columnas del circuito PN01 y la confirmación de cada línea.
--   • Empleado también protege IdUsuario.
--   • Nuevas tablas protegidas: Contratacion (dinero, vendedor y cajero) y PermisoRelacion (roles).
--   • Fechas y decimales en formato invariante (no dependen de la configuración regional).
--
-- MIGRACIÓN ÚNICA, controlada por marca (ParametroSistema 'FormatoDV'):
--   Si la base no tiene la marca (instalación nueva o base de una versión anterior) o la marca es
--   menor que 2, se dejan TODAS las tablas protegidas "sin calcular" (DVH = 0, sin fila en
--   DVVertical, espejo vacío) y se registra 'DVInicializacionPendiente' = 1. En el próximo arranque
--   la app calcula los dígitos, lo asienta en la bitácora y baja la marca.
--   Con la marca ya en 2, este bloque NO toca nada: correr el script de nuevo no "lava" una
--   manipulación previa (la verificación de arranque la sigue detectando). Cualquier otra tabla en
--   cero o sin DVV es una anomalía y va a la consola de recuperación.
--
-- Si una actualización futura del script MODIFICA filas de tablas protegidas en una base ya
-- instalada (por ejemplo, mover una patente de un rol a otro), debe pedir el recálculo con:
--     UPDATE/INSERT ParametroSistema 'DVReinicializar' = '1'
-- (o subir FormatoActual y esta marca). Este bloque lo atiende una sola vez y borra el pedido.
-- Idempotente.
-- ============================================================
IF OBJECT_ID('ParametroSistema') IS NULL
    CREATE TABLE ParametroSistema (
        Clave NVARCHAR(100) NOT NULL PRIMARY KEY,
        Valor NVARCHAR(400) NULL,
        Fecha DATETIME      NULL DEFAULT GETDATE()
    );
GO

-- Columnas DVH de las tablas que se protegen desde el formato 2.
IF COL_LENGTH('Contratacion', 'DVH') IS NULL
    ALTER TABLE Contratacion ADD DVH INT NULL;
IF COL_LENGTH('PermisoRelacion', 'DVH') IS NULL
    ALTER TABLE PermisoRelacion ADD DVH INT NULL;

-- El espejo de integridad (Usuario_Seguridad) guarda los mismos campos que el DVH de Usuario.
IF COL_LENGTH('Usuario_Seguridad', 'Activo') IS NULL
    ALTER TABLE Usuario_Seguridad ADD Activo BIT NOT NULL CONSTRAINT DF_UsuarioSeg_Activo DEFAULT 1;
IF COL_LENGTH('Usuario_Seguridad', 'RequiereCambioClave') IS NULL
    ALTER TABLE Usuario_Seguridad ADD RequiereCambioClave BIT NOT NULL CONSTRAINT DF_UsuarioSeg_RCC DEFAULT 0;
IF COL_LENGTH('Usuario_Seguridad', 'CantidadBloqueos') IS NULL
    ALTER TABLE Usuario_Seguridad ADD CantidadBloqueos INT NOT NULL CONSTRAINT DF_UsuarioSeg_CB DEFAULT 0;
IF COL_LENGTH('Usuario_Seguridad', 'FechaBloqueo') IS NULL
    ALTER TABLE Usuario_Seguridad ADD FechaBloqueo DATETIME NULL;
GO

DECLARE @formato INT = TRY_CONVERT(INT, (SELECT Valor FROM ParametroSistema WHERE Clave = 'FormatoDV'));
DECLARE @pedido  NVARCHAR(400) = (SELECT Valor FROM ParametroSistema WHERE Clave = 'DVReinicializar');
IF ISNULL(@formato, 0) < 2 OR @pedido = '1'
BEGIN
    BEGIN TRANSACTION;
    UPDATE Usuario         SET DVH = 0;
    UPDATE Cliente         SET DVH = 0;
    UPDATE Empleado        SET DVH = 0;
    UPDATE Pedido          SET DVH = 0;
    UPDATE Contratacion    SET DVH = 0;
    UPDATE PermisoRelacion SET DVH = 0;
    DELETE FROM DVVertical
    WHERE NombreTabla IN ('Usuario', 'Cliente', 'Empleado', 'Pedido', 'Contratacion', 'PermisoRelacion',
                          '__FormatoDVUsuario__');
    DELETE FROM Usuario_Seguridad;   -- la app lo reconstruye al inicializar

    MERGE ParametroSistema AS t
    USING (VALUES ('FormatoDV', '2'), ('DVInicializacionPendiente', '1')) AS s(Clave, Valor)
       ON t.Clave = s.Clave
    WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
    WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());
    DELETE FROM ParametroSistema WHERE Clave = 'DVReinicializar';
    COMMIT TRANSACTION;
    PRINT 'Dígitos verificadores: formato 2 aplicado; la app los inicializa en el próximo arranque.';
END
ELSE IF EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = 'DVInicializacionPendiente' AND Valor = '1')
BEGIN
    -- El script se volvió a correr ANTES de que la app inicializara los dígitos (por ejemplo, dos
    -- corridas seguidas del instalador): alguna sección vieja pudo volver a crear un DVV en 0
    -- (sección de usuarios iniciales). Las tablas todavía sin calcular vuelven a quedar sin DVV.
    DELETE FROM DVVertical WHERE NombreTabla = 'Usuario'         AND NOT EXISTS (SELECT 1 FROM Usuario         WHERE ISNULL(DVH, 0) <> 0);
    DELETE FROM DVVertical WHERE NombreTabla = 'Cliente'         AND NOT EXISTS (SELECT 1 FROM Cliente         WHERE ISNULL(DVH, 0) <> 0);
    DELETE FROM DVVertical WHERE NombreTabla = 'Empleado'        AND NOT EXISTS (SELECT 1 FROM Empleado        WHERE ISNULL(DVH, 0) <> 0);
    DELETE FROM DVVertical WHERE NombreTabla = 'Pedido'          AND NOT EXISTS (SELECT 1 FROM Pedido          WHERE ISNULL(DVH, 0) <> 0);
    DELETE FROM DVVertical WHERE NombreTabla = 'Contratacion'    AND NOT EXISTS (SELECT 1 FROM Contratacion    WHERE ISNULL(DVH, 0) <> 0);
    DELETE FROM DVVertical WHERE NombreTabla = 'PermisoRelacion' AND NOT EXISTS (SELECT 1 FROM PermisoRelacion WHERE ISNULL(DVH, 0) <> 0);
    PRINT 'Dígitos verificadores: inicialización todavía pendiente (la hace la app en el próximo arranque).';
END
ELSE
    PRINT 'Dígitos verificadores: formato 2 ya aplicado — sin cambios.';
GO
