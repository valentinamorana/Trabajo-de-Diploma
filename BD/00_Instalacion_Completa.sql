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
        DNI          NVARCHAR(200) NOT NULL,  -- T03: almacena el DNI CIFRADO (AES Base64)
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
        DNI              NVARCHAR(200) NOT NULL,  -- T03: almacena el DNI CIFRADO (AES Base64)
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
    ('vendedor',    N'Valentina', N'Bolívar',      'vendedor@wardrobeflow.com',    '1995-06-20'),
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
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Promocion_Destino')
    ALTER TABLE Promocion ADD CONSTRAINT CHK_Promocion_Destino CHECK (
        (IdPlan IS NOT NULL AND CategoriaPrenda IS NULL) OR
        (IdPlan IS NULL AND CategoriaPrenda IS NOT NULL));
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
-- (CU-DEP-02), habilitado en BE.Estados.EstadoEnUso.
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
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Promocion_Categoria')
BEGIN
    UPDATE Promocion SET CategoriaPrenda = ISNULL(NULLIF(LTRIM(RTRIM(CategoriaPrenda)), N''), N'Otro')
    WHERE CategoriaPrenda IS NOT NULL AND (DATALENGTH(CategoriaPrenda) <> DATALENGTH(LTRIM(RTRIM(CategoriaPrenda))) OR LTRIM(RTRIM(CategoriaPrenda)) = N'');
    UPDATE p SET p.CategoriaPrenda = m.Singular FROM Promocion p JOIN @plural m ON m.Plural = p.CategoriaPrenda;
END
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SugerenciaPromocion_Categoria')
BEGIN
    UPDATE SugerenciaPromocion SET CategoriaPrenda = ISNULL(NULLIF(LTRIM(RTRIM(CategoriaPrenda)), N''), N'Otro')
    WHERE CategoriaPrenda IS NOT NULL AND (DATALENGTH(CategoriaPrenda) <> DATALENGTH(LTRIM(RTRIM(CategoriaPrenda))) OR LTRIM(RTRIM(CategoriaPrenda)) = N'');
    UPDATE s SET s.CategoriaPrenda = m.Singular FROM SugerenciaPromocion s JOIN @plural m ON m.Plural = s.CategoriaPrenda;
END

-- Textos fuera del catálogo: se agregan como categorías (no se pierde el dato).
INSERT INTO Categoria (Nombre)
SELECT DISTINCT x.Nombre
FROM (SELECT Categoria AS Nombre FROM Prenda WHERE Categoria IS NOT NULL
      UNION SELECT CategoriaPrenda FROM Promocion WHERE CategoriaPrenda IS NOT NULL
      UNION SELECT CategoriaPrenda FROM SugerenciaPromocion WHERE CategoriaPrenda IS NOT NULL) x
WHERE NOT EXISTS (SELECT 1 FROM Categoria c WHERE c.Nombre = x.Nombre);
IF @@ROWCOUNT > 0 PRINT 'Categoria: se agregaron al catálogo categorías que ya usaban las prendas o promociones.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Prenda_Categoria')
    ALTER TABLE Prenda ADD CONSTRAINT FK_Prenda_Categoria
        FOREIGN KEY (Categoria) REFERENCES Categoria(Nombre) ON UPDATE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Promocion_Categoria')
    ALTER TABLE Promocion ADD CONSTRAINT FK_Promocion_Categoria
        FOREIGN KEY (CategoriaPrenda) REFERENCES Categoria(Nombre) ON UPDATE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SugerenciaPromocion_Categoria')
    ALTER TABLE SugerenciaPromocion ADD CONSTRAINT FK_SugerenciaPromocion_Categoria
        FOREIGN KEY (CategoriaPrenda) REFERENCES Categoria(Nombre) ON UPDATE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Promocion_CategoriaPrenda' AND object_id = OBJECT_ID('Promocion'))
    CREATE NONCLUSTERED INDEX IX_Promocion_CategoriaPrenda ON Promocion(CategoriaPrenda);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SugerenciaPromocion_CategoriaPrenda' AND object_id = OBJECT_ID('SugerenciaPromocion'))
    CREATE NONCLUSTERED INDEX IX_SugerenciaPromocion_CategoriaPrenda ON SugerenciaPromocion(CategoriaPrenda);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Prenda_Categoria' AND object_id = OBJECT_ID('Prenda'))
    CREATE NONCLUSTERED INDEX IX_Prenda_Categoria ON Prenda(Categoria);
PRINT 'Sección 20e: catálogo de categorías de prenda verificado.';
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
-- WardrobeFlow — 21e. DATOS DE DEMO: ESCENARIOS DE PN01, PN02 Y PN03
-- ------------------------------------------------------------
-- Escenarios listos para mostrar cada proceso de negocio. El APELLIDO de cada cliente y el
-- NOMBRE de cada promoción dicen qué rama del diagrama de actividad muestran:
--   PN01 Armar pedido (vendedor + deposito)
--     Juan PedidoFeliz ............ suscripción vigente, sin pedidos: camino feliz completo
--     Ana PrendasNoDisponibles .... pedido En control de stock con una prenda que ya tiene otro
--                                   cliente → Depósito informa faltantes con alternativas
--     Nicolas ExcedeCupo .......... plan Básico (5 prendas): elegir 6 → exceso de cupo / desistir
--     Sofia ListaParaFormalizar ... pedido Separado → el Vendedor lo formaliza
--     Pedro PedidoActivo .......... ya tiene un pedido formalizado → aviso de pedido activo
--     Lucia SuscripcionVencida .... vencida → aviso de suscripción no vigente
--     Tomas SuscripcionPausada .... pausada → aviso de suscripción no vigente
--   PN02 Comercialización de la suscripción (vendedor + caja)
--     Laura SinPlan ............... registrada sin plan → contratar
--     Rocio ReferidaPorJuan ....... sin plan, referida por Juan → al cobrar se acredita el beneficio
--     Diego PagoPendiente ......... contratación Estándar trimestral esperando a Caja
--                                   (tiene la promoción vigente del plan Estándar)
--     Elena TercerIntentoFallido .. contratación con 2 intentos fallidos → el 3.º la cancela
--     (DNI 40000099: no existe → rama "¿Registrado? No")
--   PN03 Promociones (gcomercial, admcomercial, contable, vendedor)
--     2 sugerencias pendientes, y promociones En revisión contable, Vigente, Rechazada por
--     Contabilidad y con Baja solicitada.
-- Se carga UNA sola vez: en una instalación NUEVA, o cuando se pidió restablecer la demo con
-- BD/Reset_Datos_Demo.sql (marca ParametroSistema 'ResetDatosDemo' = '1'). En ese caso primero se
-- BORRAN los datos de negocio (clientes, prendas, pedidos, contrataciones, promociones y la
-- bitácora de negocio); usuarios, planes, permisos, traducciones y bitácora del sistema se conservan.
-- Nunca se mezclan con datos reales: con datos propios y sin pedido de restablecer, no hace nada.
-- DVH = 0: se pide el recálculo de los dígitos verificadores (la app los calcula al arrancar).
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
    SET XACT_ABORT ON;   -- ante cualquier error se revierte todo el bloque
    BEGIN TRANSACTION;

    -- ── Se borran los datos de negocio (en una instalación nueva: los ejemplos de la sección 01) ──
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

    -- ── Empleados de Caja y Administrador (Vendedor y Depósito vienen de la sección 01) ──
    INSERT INTO Empleado (Nombre, Apellido, DNI, Email, FechaIngreso, Puesto, Legajo, IdUsuario, DVH)
    SELECT v.Nombre, v.Apellido, v.DNI, v.Email, GETDATE(), v.Puesto, v.Legajo,
           (SELECT TOP 1 IdUsuario FROM Usuario u WHERE u.Username = v.Username), 0
    FROM (VALUES
        (N'Carolina', N'Ibáñez',  '34555666', 'caja@wardrobeflow.com',  N'Caja',          'L-003', 'caja'),
        (N'Admin',    N'Sistema', '30000001', 'admin@wardrobeflow.com', N'Administrador', 'L-004', 'admin')
    ) AS v(Nombre, Apellido, DNI, Email, Puesto, Legajo, Username)
    WHERE NOT EXISTS (SELECT 1 FROM Empleado e WHERE e.Legajo = v.Legajo);

    DECLARE @vend INT = (SELECT TOP 1 IdEmpleado FROM Empleado WHERE Legajo = 'L-001');
    DECLARE @depo INT = (SELECT TOP 1 IdEmpleado FROM Empleado WHERE Legajo = 'L-002');
    DECLARE @caja INT = (SELECT TOP 1 IdEmpleado FROM Empleado WHERE Legajo = 'L-003');
    IF @vend IS NULL OR @depo IS NULL
        PRINT 'AVISO: faltan los empleados L-001 (vendedor) o L-002 (deposito); no se cargan los pedidos ni las contrataciones de demo.';
    DECLARE @hoy  DATE = CAST(GETDATE() AS DATE);
    DECLARE @pBasico  INT = (SELECT TOP 1 IdPlan FROM PlanSuscripcion WHERE Nombre = N'Básico'   AND Estado = 1);
    DECLARE @pEstand  INT = (SELECT TOP 1 IdPlan FROM PlanSuscripcion WHERE Nombre = N'Estándar' AND Estado = 1);
    DECLARE @pPremium INT = (SELECT TOP 1 IdPlan FROM PlanSuscripcion WHERE Nombre = N'Premium'  AND Estado = 1);
    -- «Tarjeta de débito» es el Id 2 o el 5 según la base (sección 20c2): se busca por su clave.
    DECLARE @mDebito  INT = (SELECT TOP 1 IdMedioPago FROM MedioPago WHERE ClaveTraduccion = 'medio.tarjeta_debito' AND Activo = 1);

    -- ── Clientes: el apellido dice qué escenario muestran ────────────────────
    INSERT INTO Cliente (Nombre, Apellido, DNI, Email, IdMedioPagoPreferido, IdPlan, FechaAlta, FechaNacimiento,
                         FechaVencimiento, FechaPausaHasta, Activo, DVH)
    SELECT v.Nombre, v.Apellido, v.DNI, v.Email,
           (SELECT TOP 1 mp.IdMedioPago FROM MedioPago mp WHERE mp.ClaveTraduccion = v.ClaveMedio AND mp.Activo = 1), v.IdPlan,
           DATEADD(DAY, -v.DiasAlta, GETDATE()), v.FechaNac,
           CASE WHEN v.DiasVence IS NULL THEN NULL ELSE DATEADD(DAY, v.DiasVence, @hoy) END,
           CASE WHEN v.DiasPausa IS NULL THEN NULL ELSE DATEADD(DAY, v.DiasPausa, GETDATE()) END,
           1, 0
    FROM (VALUES
        -- PN01
        (N'Juan',    N'PedidoFeliz',          '40000001', 'juan.pedidofeliz@demo.com',          'medio.tarjeta_credito', @pPremium, 120, CONVERT(date,'1990-03-12'),   30, NULL),
        (N'Ana',     N'PrendasNoDisponibles', '40000002', 'ana.prendasnodisponibles@demo.com',  'medio.tarjeta_debito',  @pEstand,   90, CONVERT(date,'1992-07-21'),   25, NULL),
        (N'Nicolas', N'ExcedeCupo',           '40000003', 'nicolas.excedecupo@demo.com',        'medio.tarjeta_debito',  @pBasico,   60, CONVERT(date,'1997-04-14'),   20, NULL),
        (N'Sofia',   N'ListaParaFormalizar',  '40000004', 'sofia.listaparaformalizar@demo.com', 'medio.tarjeta_credito', @pPremium,  75, CONVERT(date,'1994-11-02'),   28, NULL),
        (N'Pedro',   N'PedidoActivo',         '40000005', 'pedro.pedidoactivo@demo.com',        'medio.transferencia',   @pEstand,  150, CONVERT(date,'1988-02-09'),   15, NULL),
        (N'Lucia',   N'SuscripcionVencida',   '40000006', 'lucia.suscripcionvencida@demo.com',  'medio.tarjeta_credito', @pEstand,  200, CONVERT(date,'1991-09-30'),  -10, NULL),
        (N'Tomas',   N'SuscripcionPausada',   '40000007', 'tomas.suscripcionpausada@demo.com',  'medio.tarjeta_debito',  @pBasico,  100, CONVERT(date,'1995-09-03'),   40,   20),
        -- PN02
        (N'Laura',   N'SinPlan',              '40000008', 'laura.sinplan@demo.com',             'medio.efectivo',        NULL,        3, CONVERT(date,'1999-12-21'), NULL, NULL),
        (N'Rocio',   N'ReferidaPorJuan',      '40000009', 'rocio.referidaporjuan@demo.com',     'medio.efectivo',        NULL,        1, CONVERT(date,'2000-06-15'), NULL, NULL),
        (N'Diego',   N'PagoPendiente',        '40000010', 'diego.pagopendiente@demo.com',       'medio.transferencia',   NULL,        2, CONVERT(date,'1993-08-27'), NULL, NULL),
        (N'Elena',   N'TercerIntentoFallido', '40000011', 'elena.tercerintentofallido@demo.com', 'medio.tarjeta_credito',NULL,        4, CONVERT(date,'1996-01-19'), NULL, NULL)
    ) AS v(Nombre, Apellido, DNI, Email, ClaveMedio, IdPlan, DiasAlta, FechaNac, DiasVence, DiasPausa);

    DECLARE @cJuan   INT = (SELECT IdCliente FROM Cliente WHERE DNI = '40000001');
    DECLARE @cAna    INT = (SELECT IdCliente FROM Cliente WHERE DNI = '40000002');
    DECLARE @cSofia  INT = (SELECT IdCliente FROM Cliente WHERE DNI = '40000004');
    DECLARE @cPedro  INT = (SELECT IdCliente FROM Cliente WHERE DNI = '40000005');
    DECLARE @cLucia  INT = (SELECT IdCliente FROM Cliente WHERE DNI = '40000006');
    DECLARE @cRocio  INT = (SELECT IdCliente FROM Cliente WHERE DNI = '40000009');
    DECLARE @cDiego  INT = (SELECT IdCliente FROM Cliente WHERE DNI = '40000010');
    DECLARE @cElena  INT = (SELECT IdCliente FROM Cliente WHERE DNI = '40000011');
    UPDATE Cliente SET IdClienteReferente = @cJuan WHERE IdCliente = @cRocio;   -- Rocío llegó por Juan

    -- ── Prendas: por cada categoría y talle hay varias, para que existan alternativas ──
    INSERT INTO Prenda (Nombre, Descripcion, Talle, Color, Categoria, Estado, IdClienteActual, IdUltimoCliente, PrecioReposicion, FechaAlta)
    SELECT v.Nombre, v.Descripcion, v.Talle, v.Color, v.Categoria, v.Estado, v.Cli, v.Cli, v.Precio, DATEADD(DAY, -100, GETDATE())
    FROM (VALUES
        (N'Vestido Azul M',       N'Vestido midi de gasa',        'M',  N'Azul',    N'Vestido',  0, NULL,    42000.00),
        (N'Vestido Verde M',      N'Vestido midi de gasa',        'M',  N'Verde',   N'Vestido',  0, NULL,    42000.00),
        (N'Vestido Negro M',      N'Vestido largo de noche',      'M',  N'Negro',   N'Vestido',  0, NULL,    48000.00),
        (N'Vestido Rojo S',       N'Vestido corto de fiesta',     'S',  N'Rojo',    N'Vestido',  1, @cSofia, 45000.00),
        (N'Falda Plisada S',      N'Falda midi plisada',          'S',  N'Beige',   N'Falda',    1, @cSofia, 20000.00),
        (N'Saco Negro M',         N'Blazer de vestir',            'M',  N'Negro',   N'Saco',     1, @cPedro, 38000.00),
        (N'Saco Gris M',          N'Blazer de vestir',            'M',  N'Gris',    N'Saco',     0, NULL,    38000.00),
        (N'Saco Beige M',         N'Blazer de lino',              'M',  N'Beige',   N'Saco',     0, NULL,    36000.00),
        (N'Camisa Blanca S',      N'Camisa de algodón',           'S',  N'Blanco',  N'Camisa',   0, NULL,    18000.00),
        (N'Camisa Celeste S',     N'Camisa de algodón',           'S',  N'Celeste', N'Camisa',   0, NULL,    18000.00),
        (N'Camisa Rayada S',      N'Camisa de viscosa',           'S',  N'Rayado',  N'Camisa',   0, NULL,    19000.00),
        (N'Pantalon Negro 40',    N'Pantalón sastrero',           '40', N'Negro',   N'Pantalón', 0, NULL,    24000.00),
        (N'Pantalon Beige 40',    N'Pantalón de lino',            '40', N'Beige',   N'Pantalón', 0, NULL,    26000.00),
        (N'Tapado Camel L',       N'Tapado de paño',              'L',  N'Camel',   N'Abrigo',   0, NULL,    55000.00),
        (N'Campera Cuero L',      N'Campera biker de cuero',      'L',  N'Negro',   N'Abrigo',   0, NULL,    60000.00),
        (N'Sweater Gris M',       N'Sweater de lana',             'M',  N'Gris',    N'Sweater',  0, NULL,    22000.00),
        (N'Sweater Bordo M',      N'Sweater de lana',             'M',  N'Bordó',   N'Sweater',  0, NULL,    22000.00),
        (N'Blazer Azul M',        N'Blazer cruzado (en limpieza)', 'M', N'Azul',    N'Saco',     2, NULL,    38000.00)
    ) AS v(Nombre, Descripcion, Talle, Color, Categoria, Estado, Cli, Precio);

    -- La prenda en limpieza tiene su mantenimiento abierto (pantalla de Inspección).
    INSERT INTO MantenimientoPrenda (IdPrenda, FechaEntrada, FechaSalida, Actor)
    SELECT IdPrenda, DATEADD(DAY, -1, GETDATE()), NULL, 'deposito' FROM Prenda WHERE Nombre = N'Blazer Azul M';

    DECLARE @pzVestAzul  INT = (SELECT IdPrenda FROM Prenda WHERE Nombre = N'Vestido Azul M');
    DECLARE @pzVestRojo  INT = (SELECT IdPrenda FROM Prenda WHERE Nombre = N'Vestido Rojo S');
    DECLARE @pzFalda     INT = (SELECT IdPrenda FROM Prenda WHERE Nombre = N'Falda Plisada S');
    DECLARE @pzSacoNegro INT = (SELECT IdPrenda FROM Prenda WHERE Nombre = N'Saco Negro M');
    DECLARE @pzCamisa    INT = (SELECT IdPrenda FROM Prenda WHERE Nombre = N'Camisa Blanca S');
    DECLARE @pzTapado    INT = (SELECT IdPrenda FROM Prenda WHERE Nombre = N'Tapado Camel L');
    DECLARE @idp INT;

    IF @vend IS NOT NULL AND @depo IS NOT NULL
    BEGIN
        -- ── PN01: Pedro ya tiene un pedido FORMALIZADO (Pendiente de despacho) → "pedido activo" ──
        INSERT INTO Pedido (IdCliente, IdEmpleado, Estado, FechaPedido, FechaEnvioControl, FechaControl, IdEmpleadoControl,
                            FechaSeparacion, FechaFormalizacion, DVH)
        VALUES (@cPedro, @vend, 0, DATEADD(DAY, -3, GETDATE()), DATEADD(DAY, -3, GETDATE()), DATEADD(DAY, -2, GETDATE()), @depo,
                DATEADD(DAY, -2, GETDATE()), DATEADD(DAY, -1, GETDATE()), 0);
        SET @idp = SCOPE_IDENTITY();
        INSERT INTO PedidoPrenda (IdPedido, IdPrenda, Confirmada) VALUES (@idp, @pzSacoNegro, 1);

        -- ── PN01: Sofía tiene un pedido SEPARADO (prendas ya En uso) → el Vendedor lo formaliza ──
        INSERT INTO Pedido (IdCliente, IdEmpleado, Estado, FechaPedido, FechaEnvioControl, FechaControl, IdEmpleadoControl,
                            FechaSeparacion, DVH)
        VALUES (@cSofia, @vend, 6, DATEADD(HOUR, -6, GETDATE()), DATEADD(HOUR, -6, GETDATE()), DATEADD(HOUR, -2, GETDATE()), @depo,
                DATEADD(HOUR, -2, GETDATE()), 0);
        SET @idp = SCOPE_IDENTITY();
        INSERT INTO PedidoPrenda (IdPedido, IdPrenda, Confirmada) VALUES (@idp, @pzVestRojo, 1), (@idp, @pzFalda, 1);

        -- ── PN01: Ana tiene un pedido EN CONTROL DE STOCK con el "Saco Negro M", que ya tiene Pedro ──
        --    Depósito lo ve como no disponible → Informar faltantes (alternativas: Saco Gris M y Saco Beige M).
        INSERT INTO Pedido (IdCliente, IdEmpleado, Estado, FechaPedido, FechaEnvioControl, DVH)
        VALUES (@cAna, @vend, 4, DATEADD(HOUR, -1, GETDATE()), DATEADD(HOUR, -1, GETDATE()), 0);
        SET @idp = SCOPE_IDENTITY();
        INSERT INTO PedidoPrenda (IdPedido, IdPrenda, Confirmada) VALUES (@idp, @pzVestAzul, 0), (@idp, @pzSacoNegro, 0);

        -- ── Historial entregado (para que las métricas de PN03 tengan datos) ──
        DECLARE @hist TABLE (Cli INT, Dias INT, Prenda INT);
        INSERT INTO @hist VALUES (@cJuan, 60, @pzCamisa), (@cJuan, 30, @pzTapado), (@cLucia, 50, @pzVestAzul),
                                 (@cPedro, 90, @pzCamisa), (@cLucia, 80, @pzTapado);
        DECLARE @cli INT, @dias INT, @pz INT;
        DECLARE curHist CURSOR LOCAL FAST_FORWARD FOR SELECT Cli, Dias, Prenda FROM @hist WHERE Cli IS NOT NULL AND Prenda IS NOT NULL;
        OPEN curHist;
        FETCH NEXT FROM curHist INTO @cli, @dias, @pz;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            INSERT INTO Pedido (IdCliente, IdEmpleado, Estado, FechaPedido, FechaEnvioControl, FechaControl, IdEmpleadoControl,
                                FechaSeparacion, FechaFormalizacion, FechaDespacho, FechaEntrega, DVH)
            VALUES (@cli, @vend, 2, DATEADD(DAY, -@dias, GETDATE()), DATEADD(DAY, -@dias, GETDATE()),
                    DATEADD(DAY, -@dias, GETDATE()), @depo, DATEADD(DAY, -@dias, GETDATE()), DATEADD(DAY, -@dias, GETDATE()),
                    DATEADD(DAY, -@dias + 1, GETDATE()), DATEADD(DAY, -@dias + 2, GETDATE()), 0);
            SET @idp = SCOPE_IDENTITY();
            INSERT INTO PedidoPrenda (IdPedido, IdPrenda, Confirmada) VALUES (@idp, @pz, 1);
            FETCH NEXT FROM curHist INTO @cli, @dias, @pz;
        END
        CLOSE curHist;
        DEALLOCATE curHist;
    END

    -- ── PN02: contrataciones ─────────────────────────────────────────────────
    IF @vend IS NOT NULL
    BEGIN
        -- Juan: su suscripción vino de una contratación ya cobrada (vista "Resueltas" de Caja).
        INSERT INTO Contratacion (IdCliente, IdPlan, IdVendedor, IdCaja, Modalidad, Estado, FechaAlta, FechaResolucion,
                                  IdMedioPago, NumeroComprobante, FechaComprobante, Importe, DescuentoAplicado,
                                  VigenciaDesde, VigenciaHasta, PrecioMensual)
        SELECT @cJuan, pl.IdPlan, @vend, @caja, 0, 1, DATEADD(DAY, -120, GETDATE()), DATEADD(DAY, -120, GETDATE()),
               @mDebito, 'CMP-0001-' + FORMAT(DATEADD(DAY, -120, GETDATE()), 'yyyyMMdd'), DATEADD(DAY, -120, GETDATE()),
               pl.Precio, 0, DATEADD(DAY, -120, @hoy), DATEADD(DAY, 30, @hoy),   -- misma vigencia que la suscripción de Juan
               pl.Precio
        FROM PlanSuscripcion pl WHERE pl.IdPlan = @pPremium AND @caja IS NOT NULL AND @cJuan IS NOT NULL;

        -- Diego: Estándar trimestral esperando a Caja (con la promoción vigente del plan Estándar).
        -- Elena: Básico mensual esperando a Caja, ya con 2 intentos fallidos.
        INSERT INTO Contratacion (IdCliente, IdPlan, IdVendedor, IdCaja, Modalidad, Estado, FechaAlta, PrecioMensual)
        SELECT v.Cli, pl.IdPlan, @vend, NULL, v.Modalidad, 0, v.FechaAlta, pl.Precio
        FROM (VALUES (@cDiego, @pEstand, 1, DATEADD(HOUR, -3, GETDATE())),
                     (@cElena, @pBasico, 0, DATEADD(DAY,  -1, GETDATE()))) AS v(Cli, IdPlan, Modalidad, FechaAlta)
        JOIN PlanSuscripcion pl ON pl.IdPlan = v.IdPlan
        WHERE v.Cli IS NOT NULL;

        INSERT INTO ContratacionIntentoPago (IdContratacion, NroIntento, Fecha, IdMedioPago, Motivo, IdCaja)
        SELECT c.IdContratacion, v.Nro, DATEADD(HOUR, -v.Horas, GETDATE()), @mDebito, v.Motivo, @caja
        FROM Contratacion c
        CROSS JOIN (VALUES (1, 20, N'Tarjeta rechazada'), (2, 4, N'Fondos insuficientes')) AS v(Nro, Horas, Motivo)
        WHERE c.IdCliente = @cElena AND c.Estado = 0 AND @caja IS NOT NULL;
    END

    -- ── PN03: sugerencias y promociones en cada estado, con sus objetos e historial ──
    -- Gerencia (gcomercial) sugiere, Administración (admcomercial) crea, Contabilidad (contable)
    -- dictamina y Ventas (vendedor) pide la baja: quien crea no dictamina.
    DECLARE @uGer  INT = (SELECT TOP 1 IdUsuario FROM Usuario WHERE Username = 'gcomercial');
    DECLARE @uAdm  INT = (SELECT TOP 1 IdUsuario FROM Usuario WHERE Username = 'admcomercial');
    DECLARE @uCont INT = (SELECT TOP 1 IdUsuario FROM Usuario WHERE Username = 'contable');
    DECLARE @uVend INT = (SELECT TOP 1 IdUsuario FROM Usuario WHERE Username = 'vendedor');

    INSERT INTO SugerenciaPromocion (IdPlan, CategoriaPrenda, Motivo, TipoDescuentoSugerido, BeneficioEstimado, Estado, FechaAlta, OrigenMetrica, IdUsuarioAlta)
    SELECT v.IdPlan, v.Categoria, v.Motivo, v.Tipo, v.Beneficio, 0, v.FechaAlta, v.Origen, @uGer
    FROM (VALUES
        (@pBasico, NULL,      N'SUGERENCIA PendienteDeEvaluar: el plan Básico tiene la mayor tasa de abandono; un descuento de retención puede sostenerlo.', 0, 12000.00, DATEADD(DAY, -3, GETDATE()), 0),
        (NULL,     N'Abrigo', N'SUGERENCIA PendienteDeEvaluar: los abrigos rotan poco fuera de temporada; conviene incentivar su alquiler.',               1,  5000.00, DATEADD(DAY, -2, GETDATE()), 1)
    ) AS v(IdPlan, Categoria, Motivo, Tipo, Beneficio, FechaAlta, Origen)
    WHERE (v.IdPlan IS NOT NULL OR v.Categoria IS NOT NULL) AND @uGer IS NOT NULL;

    INSERT INTO Promocion (Nombre, Descripcion, TipoDescuento, Valor, FechaInicio, FechaFin, Estado, IdPlan, CategoriaPrenda, MargenEstimado, ImpactoEconomico, IdUsuarioAlta, FechaAlta)
    SELECT v.Nombre, v.Descripcion, v.TipoDescuento, v.Valor, v.FechaInicio, v.FechaFin, v.Estado, v.IdPlan, v.Categoria,
           v.Margen, v.Impacto, @uAdm, v.FechaAlta
    FROM (VALUES
        (N'PROMO Vigente Estandar -10%',        N'10% en el plan Estándar. Vigente: Ventas puede pedir la baja y se aplica en el cobro.', 0, 10.00,
         DATEADD(DAY, -10, @hoy), DATEADD(DAY, 50, @hoy), 1, @pEstand, NULL, 8000.00, N'Reducción compensada por mayor retención.', DATEADD(DAY, -12, GETDATE())),
        (N'PROMO ParaDictaminar Premium -15%',  N'15% en el plan Premium. Espera el dictamen de Contabilidad.',                         0, 15.00,
         @hoy, DATEADD(DAY, 30, @hoy), 0, @pPremium, NULL, 6000.00, N'Descuento de captación para nuevos clientes.', DATEADD(DAY, -1, GETDATE())),
        (N'PROMO Rechazada Basico -20%',        N'20% en el plan Básico. Contabilidad la rechazó: Administración la reformula o descarta.', 0, 20.00,
         @hoy, DATEADD(DAY, 30, @hoy), 2, @pBasico, NULL, 3000.00, N'El margen no cubre la reducción.', DATEADD(DAY, -5, GETDATE())),
        (N'PROMO BajaSolicitada Abrigos $3000', N'$3000 menos por prenda Abrigo. Ventas pidió la baja: Administración la resuelve.',  1, 3000.00,
         DATEADD(DAY, -20, @hoy), DATEADD(DAY, 40, @hoy), 3, NULL, N'Abrigo', 4500.00, N'Impacto bajo: categoría de baja rotación.', DATEADD(DAY, -22, GETDATE()))
    ) AS v(Nombre, Descripcion, TipoDescuento, Valor, FechaInicio, FechaFin, Estado, IdPlan, Categoria, Margen, Impacto, FechaAlta)
    WHERE (v.IdPlan IS NOT NULL OR v.Categoria IS NOT NULL) AND @uAdm IS NOT NULL;

    DECLARE @prVig  INT = (SELECT IdPromocion FROM Promocion WHERE Nombre = N'PROMO Vigente Estandar -10%');
    DECLARE @prDict INT = (SELECT IdPromocion FROM Promocion WHERE Nombre = N'PROMO ParaDictaminar Premium -15%');
    DECLARE @prRech INT = (SELECT IdPromocion FROM Promocion WHERE Nombre = N'PROMO Rechazada Basico -20%');
    DECLARE @prBaja INT = (SELECT IdPromocion FROM Promocion WHERE Nombre = N'PROMO BajaSolicitada Abrigos $3000');

    IF @uCont IS NOT NULL
        INSERT INTO DictamenContable (IdPromocion, IdUsuario, Aprobada, Observacion, Fecha)
        SELECT v.IdPromocion, @uCont, v.Aprobada, v.Observacion, v.Fecha
        FROM (VALUES
            (@prVig,  1, N'Aprobada: el margen estimado cubre la reducción de ingresos.',      DATEADD(DAY, -11, GETDATE())),
            (@prRech, 0, N'Rechazada: un 20% en el plan más barato no cubre el costo.',         DATEADD(DAY,  -4, GETDATE())),
            (@prBaja, 1, N'Aprobada: impacto bajo en una categoría de baja rotación.',          DATEADD(DAY, -21, GETDATE()))
        ) AS v(IdPromocion, Aprobada, Observacion, Fecha)
        WHERE v.IdPromocion IS NOT NULL;

    INSERT INTO SolicitudBajaPromocion (IdPromocion, IdUsuarioSolicita, Motivo, FechaSolicitud, Estado)
    SELECT @prBaja, @uVend, N'Los clientes no la usan y confunde en el mostrador.', DATEADD(DAY, -1, GETDATE()), 0
    WHERE @prBaja IS NOT NULL AND @uVend IS NOT NULL;

    INSERT INTO PromocionHistorial (IdPromocion, EstadoAnterior, EstadoNuevo, IdUsuario, Fecha, Observacion)
    SELECT v.IdPromocion, v.Anterior, v.Nuevo, v.IdUsuario, v.Fecha, v.Observacion
    FROM (VALUES
        (@prVig,  NULL, 0, @uAdm,  DATEADD(DAY, -12, GETDATE()), N'Alta manual'),
        (@prVig,  0,    1, @uCont, DATEADD(DAY, -11, GETDATE()), N'Aprobada por Contabilidad'),
        (@prDict, NULL, 0, @uAdm,  DATEADD(DAY,  -1, GETDATE()), N'Alta manual'),
        (@prRech, NULL, 0, @uAdm,  DATEADD(DAY,  -5, GETDATE()), N'Alta manual'),
        (@prRech, 0,    2, @uCont, DATEADD(DAY,  -4, GETDATE()), N'Rechazada por Contabilidad'),
        (@prBaja, NULL, 0, @uAdm,  DATEADD(DAY, -22, GETDATE()), N'Alta manual'),
        (@prBaja, 0,    1, @uCont, DATEADD(DAY, -21, GETDATE()), N'Aprobada por Contabilidad'),
        (@prBaja, 1,    3, @uVend, DATEADD(DAY,  -1, GETDATE()), N'Ventas solicitó la baja')
    ) AS v(IdPromocion, Anterior, Nuevo, IdUsuario, Fecha, Observacion)
    WHERE v.IdPromocion IS NOT NULL;

    -- ── Marcas: datos de demo aplicados y recálculo de los dígitos verificadores ──
    DELETE FROM ParametroSistema WHERE Clave IN (N'SeedDemo21', N'ResetDatosDemo');
    -- PN04: la sección 21z3 vuelve a completar la FechaDevolucion del historial entregado de la demo.
    DELETE FROM ParametroSistema WHERE Clave = N'PedidoFechaDevolucion';
    INSERT INTO ParametroSistema (Clave, Valor, Fecha) VALUES (N'SeedDemo21', N'Aplicada', GETDATE());
    IF EXISTS (SELECT 1 FROM ParametroSistema WHERE Clave = N'FormatoDV' AND TRY_CONVERT(INT, Valor) >= 2)
        MERGE ParametroSistema AS t
        USING (VALUES (N'DVReinicializar', N'1')) AS s(Clave, Valor) ON t.Clave = s.Clave
        WHEN MATCHED THEN UPDATE SET Valor = s.Valor, Fecha = GETDATE()
        WHEN NOT MATCHED THEN INSERT (Clave, Valor, Fecha) VALUES (s.Clave, s.Valor, GETDATE());

    COMMIT TRANSACTION;
    PRINT 'Datos de demo (sección 21e) aplicados: escenarios de PN01, PN02 y PN03.';
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
                          AND pr.IdClienteActual = ped.IdCliente);
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
