-- ============================================================
-- WardrobeFlow — ESQUEMA FINAL (DDL limpio)
-- ------------------------------------------------------------
-- Documentación del esquema final de la base para la entrega: solo la
-- estructura (tablas, columnas, tipos, nulabilidad, valores por defecto,
-- claves primarias, restricciones UNIQUE y CHECK, índices únicos/filtrados
-- y de búsqueda, y claves foráneas). No incluye datos ni migraciones.
--
-- Generado el 13/10/2026 a partir de la base construida con
-- BD/00_Instalacion_Completa.sql, leyendo el catálogo de SQL Server
-- (sys.tables, sys.columns, sys.indexes, sys.check_constraints,
-- sys.foreign_keys...). El esquema no tiene vistas, procedimientos
-- almacenados, funciones ni triggers.
--
-- IMPORTANTE: 00_Instalacion_Completa.sql sigue siendo el ÚNICO script que
-- ejecuta el instalador (crea la base, migra versiones anteriores y carga
-- datos semilla y de prueba). Este archivo es solo de referencia: si se
-- cambia el esquema, se edita 00_Instalacion_Completa.sql y este DDL se
-- vuelve a generar.
--
-- Corre sobre una base vacía: primero las tablas, después los índices y al
-- final las claves foráneas (ALTER TABLE), así el orden no importa.
-- Las restricciones que en la base real tienen nombre generado por SQL
-- Server (FK__..., PK__..., DF__...) se escriben sin nombre, igual que en el
-- script de instalación; el resto conserva su nombre explícito.
--
-- Uso:  sqlcmd -S .\SQLEXPRESS -E -I -f 65001 -d <base vacía> -i 01_Esquema_Final_DDL.sql
-- ============================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================
-- 1. TABLAS (columnas, PK, UNIQUE, DEFAULT y CHECK)
-- ============================================================

CREATE TABLE [dbo].[Bitacora] (
    [Id]         int IDENTITY(1, 1) NOT NULL,
    [fecha]      datetime NOT NULL DEFAULT (getdate()),
    [usuario]    int NULL,
    [modulo]     nvarchar(100) NULL,
    [actividad]  nvarchar(200) NULL,
    [detalle]    nvarchar(1000) NULL,
    [criticidad] int NOT NULL DEFAULT ((0)),
    [ip]         nvarchar(50) NULL,
    PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CHK_Bitacora_criticidad] CHECK ([criticidad]=(6) OR [criticidad]=(5) OR [criticidad]=(4) OR [criticidad]=(3) OR [criticidad]=(2) OR [criticidad]=(1) OR [criticidad]=(0))
);
GO

CREATE TABLE [dbo].[BitacoraNegocio] (
    [IdEvento]    int IDENTITY(1, 1) NOT NULL,
    [Fecha]       datetime NOT NULL DEFAULT (getdate()),
    [Tipo]        nvarchar(100) NOT NULL,
    [IdUsuario]   int NULL,
    [IdPedido]    int NULL,
    [IdPrenda]    int NULL,
    [IdCliente]   int NULL,
    [Descripcion] nvarchar(500) NOT NULL,
    PRIMARY KEY CLUSTERED ([IdEvento])
);
GO

CREATE TABLE [dbo].[CargoPrenda] (
    [IdCargo]       int IDENTITY(1, 1) NOT NULL,
    [IdPrenda]      int NOT NULL,
    [IdCliente]     int NOT NULL,
    [Motivo]        nvarchar(200) NOT NULL,
    [Monto]         decimal(10, 2) NOT NULL,
    [FechaRegistro] datetime NOT NULL DEFAULT (getdate()),
    [FechaCobro]    datetime NULL,
    [Actor]         nvarchar(100) NULL,
    [Estado]        int NOT NULL DEFAULT ((0)),
    PRIMARY KEY CLUSTERED ([IdCargo]),
    CONSTRAINT [CHK_CargoPrenda_Estado] CHECK ([Estado]=(1) OR [Estado]=(0)),
    CONSTRAINT [CK_CargoPrenda_Monto] CHECK ([Monto]>(0))
);
GO

CREATE TABLE [dbo].[Categoria] (
    [IdCategoria] int IDENTITY(1, 1) NOT NULL,
    [Nombre]      nvarchar(100) NOT NULL,
    [Activo]      bit NOT NULL CONSTRAINT [DF_Categoria_Activo] DEFAULT ((1)),
    CONSTRAINT [PK_Categoria] PRIMARY KEY CLUSTERED ([IdCategoria]),
    CONSTRAINT [UX_Categoria_Nombre] UNIQUE NONCLUSTERED ([Nombre]),
    CONSTRAINT [CHK_Categoria_Nombre] CHECK (len(ltrim(rtrim([Nombre])))>(0))
);
GO

CREATE TABLE [dbo].[ClaveRecuperacion] (
    [IdClave]       int IDENTITY(1, 1) NOT NULL,
    [ClaveHash]     nvarchar(500) NOT NULL,
    [Usada]         bit NOT NULL DEFAULT ((0)),
    [UsadaPor]      nvarchar(100) NULL,
    [FechaUso]      datetime NULL,
    [FechaCreacion] datetime NOT NULL DEFAULT (getdate()),
    PRIMARY KEY CLUSTERED ([IdClave])
);
GO

CREATE TABLE [dbo].[Cliente] (
    [IdCliente]                 int IDENTITY(1, 1) NOT NULL,
    [Nombre]                    nvarchar(100) NOT NULL,
    [Apellido]                  nvarchar(100) NOT NULL,
    [DNI]                       nvarchar(200) NOT NULL,
    [Email]                     nvarchar(200) NULL,
    [IdPlan]                    int NULL,
    [FechaAlta]                 datetime NOT NULL DEFAULT (getdate()),
    [FechaNacimiento]           date NOT NULL,
    [Activo]                    bit NOT NULL DEFAULT ((1)),
    [DVH]                       int NULL,
    [FechaVencimiento]          date NULL,
    [FechaLimiteGracia]         date NULL,
    [FechaPausaHasta]           datetime NULL,
    [IdClienteReferente]        int NULL,
    [DescuentoProximoCobro]     decimal(10, 2) NOT NULL DEFAULT ((0)),
    [BeneficioReferidoOtorgado] bit NOT NULL DEFAULT ((0)),
    [IdMedioPagoPreferido]      int NULL,
    [IdPlanSiguiente]           int NULL,
    [FechaCambioPlan]           datetime NULL,
    PRIMARY KEY CLUSTERED ([IdCliente])
);
GO

CREATE TABLE [dbo].[Contratacion] (
    [IdContratacion]        int IDENTITY(1, 1) NOT NULL,
    [IdCliente]             int NOT NULL,
    [IdPlan]                int NOT NULL,
    [IdVendedor]            int NOT NULL,
    [IdCaja]                int NULL,
    [Modalidad]             int NOT NULL,
    [Estado]                int NOT NULL DEFAULT ((0)),
    [FechaAlta]             datetime NOT NULL DEFAULT (getdate()),
    [FechaResolucion]       datetime NULL,
    [NumeroComprobante]     nvarchar(50) NULL,
    [FechaComprobante]      datetime NULL,
    [Importe]               decimal(10, 2) NULL,
    [DescuentoAplicado]     decimal(10, 2) NULL,
    [IdPromocion]           int NULL,
    [IdMedioPago]           int NULL,
    [VigenciaDesde]         date NULL,
    [VigenciaHasta]         date NULL,
    [PrecioMensual]         decimal(10, 2) NULL,
    [IdReferenteAcreditado] int NULL,
    [IdPlanCuotas]          int NULL,
    [RecargoCuotas]         decimal(10, 2) NULL,
    [CreditoCambioPlan]     decimal(10, 2) NULL,
    [MotivoAnulacion]       nvarchar(200) NULL,
    [DVH]                   int NULL,
    PRIMARY KEY CLUSTERED ([IdContratacion]),
    CONSTRAINT [CHK_Contratacion_CreditoCambioPlan] CHECK ([CreditoCambioPlan] IS NULL OR [CreditoCambioPlan]>(0)),
    CONSTRAINT [CHK_Contratacion_Cuotas] CHECK ([IdPlanCuotas] IS NULL AND [RecargoCuotas] IS NULL OR [IdPlanCuotas] IS NOT NULL AND [RecargoCuotas]>=(0)),
    CONSTRAINT [CHK_Contratacion_Estado] CHECK ([Estado]=(2) OR [Estado]=(1) OR [Estado]=(0)),
    CONSTRAINT [CHK_Contratacion_Modalidad] CHECK ([Modalidad]=(2) OR [Modalidad]=(1) OR [Modalidad]=(0)),
    CONSTRAINT [CHK_Contratacion_MotivoAnulacion] CHECK ([MotivoAnulacion] IS NULL OR [Estado]=(2)),
    CONSTRAINT [CHK_Contratacion_Pagada] CHECK ([Estado]<>(1) OR [IdMedioPago] IS NOT NULL AND [NumeroComprobante] IS NOT NULL),
    CONSTRAINT [CK_Contratacion_Importe] CHECK (([Importe] IS NULL OR [Importe]>=(0)) AND ([DescuentoAplicado] IS NULL OR [DescuentoAplicado]>=(0)))
);
GO

CREATE TABLE [dbo].[ContratacionIntentoPago] (
    [IdIntento]      int IDENTITY(1, 1) NOT NULL,
    [IdContratacion] int NOT NULL,
    [NroIntento]     int NOT NULL,
    [Fecha]          datetime NOT NULL DEFAULT (getdate()),
    [IdMedioPago]    int NULL,
    [Motivo]         nvarchar(200) NOT NULL,
    [IdCaja]         int NULL,
    PRIMARY KEY CLUSTERED ([IdIntento]),
    CONSTRAINT [UX_IntentoPago_Nro] UNIQUE NONCLUSTERED ([IdContratacion], [NroIntento]),
    CONSTRAINT [CHK_IntentoPago_Nro] CHECK ([NroIntento]>=(1) AND [NroIntento]<=(3))
);
GO

CREATE TABLE [dbo].[Control] (
    [IdControl]  int IDENTITY(1, 1) NOT NULL,
    [Clave]      varchar(100) NOT NULL,
    [Formulario] varchar(50) NOT NULL DEFAULT ('General'),
    PRIMARY KEY CLUSTERED ([IdControl]),
    CONSTRAINT [UQ_Control_Clave] UNIQUE NONCLUSTERED ([Clave])
);
GO

CREATE TABLE [dbo].[ControlMapeado] (
    [IdControlMapeado] int IDENTITY(1, 1) NOT NULL,
    [IdPermiso]        int NOT NULL,
    [Formulario]       nvarchar(100) NOT NULL,
    [NombreControl]    nvarchar(100) NOT NULL,
    PRIMARY KEY CLUSTERED ([IdControlMapeado]),
    CONSTRAINT [UQ_ControlMapeado] UNIQUE NONCLUSTERED ([Formulario], [NombreControl])
);
GO

CREATE TABLE [dbo].[DecisionComercial] (
    [IdDecision]         int IDENTITY(1, 1) NOT NULL,
    [Fecha]              datetime NOT NULL CONSTRAINT [DF_DecisionComercial_Fecha] DEFAULT (getdate()),
    [IdUsuario]          int NOT NULL,
    [TipoRegla]          nvarchar(50) NOT NULL,
    [ClaveRecomendacion] nvarchar(100) NOT NULL,
    [Prioridad]          int NOT NULL,
    [Titulo]             nvarchar(300) NOT NULL,
    [Diagnostico]        nvarchar(1000) NULL,
    [AccionElegida]      nvarchar(500) NULL,
    [EsAlternativa]      bit NOT NULL CONSTRAINT [DF_DecisionComercial_EsAlternativa] DEFAULT ((0)),
    [ImpactoEstimado]    decimal(14, 2) NOT NULL CONSTRAINT [DF_DecisionComercial_Impacto] DEFAULT ((0)),
    [Estado]             int NOT NULL,
    [Motivo]             nvarchar(500) NULL,
    [TipoReferencia]     nvarchar(30) NULL,
    [Referencia]         int NULL,
    [PospuestaHasta]     date NULL,
    [FechaEjecucion]     datetime NULL,
    [FechaEvaluacion]    datetime NULL,
    [ResultadoReal]      decimal(14, 2) NULL,
    CONSTRAINT [PK_DecisionComercial] PRIMARY KEY CLUSTERED ([IdDecision]),
    CONSTRAINT [CHK_DecisionComercial_Descarte] CHECK ([Estado]<>(2) OR [Motivo] IS NOT NULL),
    CONSTRAINT [CHK_DecisionComercial_Estado] CHECK ([Estado]=(4) OR [Estado]=(3) OR [Estado]=(2) OR [Estado]=(1) OR [Estado]=(0)),
    CONSTRAINT [CHK_DecisionComercial_Pospuesta] CHECK ([Estado]=(3) AND [PospuestaHasta] IS NOT NULL OR [Estado]<>(3) AND [PospuestaHasta] IS NULL),
    CONSTRAINT [CHK_DecisionComercial_Prioridad] CHECK ([Prioridad]=(2) OR [Prioridad]=(1) OR [Prioridad]=(0)),
    CONSTRAINT [CHK_DecisionComercial_Referencia] CHECK ([TipoReferencia] IS NULL AND [Referencia] IS NULL OR [TipoReferencia] IS NOT NULL AND [Referencia] IS NOT NULL),
    CONSTRAINT [CHK_DecisionComercial_TipoReferencia] CHECK ([TipoReferencia] IS NULL OR ([TipoReferencia]=N'SolicitudBaja' OR [TipoReferencia]=N'Promocion' OR [TipoReferencia]=N'Sugerencia'))
);
GO

-- PN03: una sola decisión abierta (Aceptada / Pospuesta) por recomendación (sección 20p).
CREATE UNIQUE NONCLUSTERED INDEX [UX_DecisionComercial_Abierta] ON [dbo].[DecisionComercial]([ClaveRecomendacion]) WHERE ([Estado] IN ((1), (3)));
GO

-- PN03: qué tocó una decisión ejecutada (clientes contactados o prendas cambiadas). Sección 20p.
CREATE TABLE [dbo].[DecisionComercialDetalle] (
    [IdDecision] int NOT NULL,
    [Tipo]       nvarchar(20) NOT NULL,
    [IdObjeto]   int NOT NULL,
    CONSTRAINT [PK_DecisionComercialDetalle] PRIMARY KEY CLUSTERED ([IdDecision], [Tipo], [IdObjeto]),
    CONSTRAINT [CHK_DecisionComercialDetalle_Tipo] CHECK ([Tipo]=N'Prenda' OR [Tipo]=N'Cliente'),
    CONSTRAINT [FK_DecisionComercialDetalle_Decision] FOREIGN KEY ([IdDecision]) REFERENCES [dbo].[DecisionComercial] ([IdDecision]) ON DELETE CASCADE
);
GO

-- PN03: metas comerciales mensuales de Gerencia (una por indicador y mes). Sección 20p.
CREATE TABLE [dbo].[MetaComercial] (
    [IdMeta]        int IDENTITY(1, 1) NOT NULL,
    [Periodo]       date NOT NULL,
    [Indicador]     int NOT NULL,
    [ValorObjetivo] decimal(14, 2) NOT NULL,
    [Sentido]       int NOT NULL,
    [IdUsuario]     int NOT NULL,
    [Fecha]         datetime NOT NULL CONSTRAINT [DF_MetaComercial_Fecha] DEFAULT (getdate()),
    CONSTRAINT [PK_MetaComercial] PRIMARY KEY CLUSTERED ([IdMeta]),
    CONSTRAINT [UQ_MetaComercial_PeriodoIndicador] UNIQUE ([Periodo], [Indicador]),
    CONSTRAINT [CHK_MetaComercial_Periodo] CHECK (datepart(day,[Periodo])=(1)),
    CONSTRAINT [CHK_MetaComercial_Indicador] CHECK ([Indicador]>=(0)),
    CONSTRAINT [CHK_MetaComercial_Sentido] CHECK ([Sentido]=(1) OR [Sentido]=(0)),
    CONSTRAINT [CHK_MetaComercial_Valor] CHECK ([ValorObjetivo]>=(0)),
    CONSTRAINT [FK_MetaComercial_Usuario] FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuario] ([IdUsuario])
);
GO
CREATE TABLE [dbo].[DesistimientoContratacion] (
    [IdDesistimiento] int IDENTITY(1, 1) NOT NULL,
    [IdCliente]       int NOT NULL,
    [IdPlan]          int NULL,
    [Modalidad]       int NULL,
    [Motivo]          nvarchar(200) NOT NULL,
    [Fecha]           datetime NOT NULL DEFAULT (getdate()),
    [IdVendedor]      int NOT NULL,
    PRIMARY KEY CLUSTERED ([IdDesistimiento]),
    CONSTRAINT [CHK_DesistContr_Modalidad] CHECK ([Modalidad]=(2) OR [Modalidad]=(1) OR [Modalidad]=(0)),
    CONSTRAINT [CHK_DesistContr_ModalidadConPlan] CHECK ([IdPlan] IS NOT NULL OR [Modalidad] IS NULL)
);
GO

CREATE TABLE [dbo].[DictamenContable] (
    [IdDictamen]  int IDENTITY(1, 1) NOT NULL,
    [IdPromocion] int NOT NULL,
    [IdUsuario]   int NOT NULL,
    [Aprobada]    bit NOT NULL,
    [Observacion] nvarchar(500) NOT NULL,
    [Fecha]       datetime NOT NULL DEFAULT (getdate()),
    PRIMARY KEY CLUSTERED ([IdDictamen])
);
GO

CREATE TABLE [dbo].[DVVertical] (
    [Id]           int IDENTITY(1, 1) NOT NULL,
    [NombreTabla]  varchar(100) NOT NULL,
    [DVV]          int NOT NULL,
    [FechaCalculo] datetime NOT NULL DEFAULT (getdate()),
    PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_DVVertical_Tabla] UNIQUE NONCLUSTERED ([NombreTabla])
);
GO

CREATE TABLE [dbo].[Empleado] (
    [IdEmpleado]   int IDENTITY(1, 1) NOT NULL,
    [Nombre]       nvarchar(100) NOT NULL,
    [Apellido]     nvarchar(100) NOT NULL,
    [DNI]          nvarchar(200) NOT NULL,
    [Email]        nvarchar(200) NULL,
    [FechaIngreso] datetime NOT NULL DEFAULT (getdate()),
    [Puesto]       nvarchar(100) NULL,
    [Legajo]       nvarchar(50) NULL,
    [IdUsuario]    int NULL,
    [DVH]          int NULL,
    PRIMARY KEY CLUSTERED ([IdEmpleado])
);
GO

CREATE TABLE [dbo].[HistorialCobro] (
    [IdCobro]           int IDENTITY(1, 1) NOT NULL,
    [IdCliente]         int NOT NULL,
    [Importe]           decimal(10, 2) NOT NULL DEFAULT ((0)),
    [FechaDeteccion]    datetime NOT NULL DEFAULT (getdate()),
    [FechaResolucion]   datetime NULL,
    [Resultado]         int NOT NULL,
    [Actor]             nvarchar(100) NULL,
    [IdMedioPago]       int NULL,
    [NumeroComprobante] nvarchar(50) NULL,
    [Modalidad]         int NULL,
    [DescuentoAplicado] decimal(10, 2) NULL,
    [IdPromocion]       int NULL,
    PRIMARY KEY CLUSTERED ([IdCobro]),
    CONSTRAINT [CHK_HistorialCobro_Descuento] CHECK ([DescuentoAplicado] IS NULL OR [DescuentoAplicado]>(0)),
    CONSTRAINT [CHK_HistorialCobro_Modalidad] CHECK ([Modalidad] IS NULL OR ([Modalidad]=(2) OR [Modalidad]=(1) OR [Modalidad]=(0))),
    CONSTRAINT [CHK_HistorialCobro_Resultado] CHECK ([Resultado]=(3) OR [Resultado]=(2) OR [Resultado]=(1) OR [Resultado]=(0))
);
GO

CREATE TABLE [dbo].[HistorialIntegridad] (
    [Id]                int IDENTITY(1, 1) NOT NULL,
    [NombreTabla]       varchar(100) NOT NULL,
    [DVVAlmacenado]     int NULL,
    [DVVCalculado]      int NOT NULL,
    [Resultado]         bit NOT NULL,
    [FilasCorruptas]    int NOT NULL DEFAULT ((0)),
    [FechaVerificacion] datetime NOT NULL DEFAULT (getdate()),
    [DisparadoPor]      varchar(50) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [CHK_HistInteg_Origen] CHECK ([DisparadoPor]='Manual' OR [DisparadoPor]='Timer' OR [DisparadoPor]='Arranque')
);
GO

CREATE TABLE [dbo].[HistorialRenovacion] (
    [IdRenovacion]    int IDENTITY(1, 1) NOT NULL,
    [IdCliente]       int NOT NULL,
    [IdPlanAnterior]  int NULL,
    [IdPlanNuevo]     int NULL,
    [FechaDeteccion]  datetime NOT NULL DEFAULT (getdate()),
    [FechaResolucion] datetime NULL,
    [Resultado]       int NOT NULL,
    [Actor]           nvarchar(100) NULL,
    PRIMARY KEY CLUSTERED ([IdRenovacion]),
    CONSTRAINT [CHK_HistorialRenovacion_Resultado] CHECK ([Resultado]=(4) OR [Resultado]=(3) OR [Resultado]=(2) OR [Resultado]=(1) OR [Resultado]=(0))
);
GO

CREATE TABLE [dbo].[HistorialUsuario] (
    [IdVersion]    int IDENTITY(1, 1) NOT NULL,
    [IdUsuario]    int NOT NULL,
    [Fecha]        datetime NOT NULL DEFAULT (getdate()),
    [Actor]        nvarchar(100) NOT NULL,
    [Detalle]      nvarchar(500) NOT NULL,
    [UsernameSnap] nvarchar(100) NOT NULL,
    [NombreSnap]   nvarchar(100) NULL,
    [ApellidoSnap] nvarchar(100) NULL,
    [FechaNacSnap] date NULL,
    [EmailSnap]    nvarchar(200) NULL,
    [ClaveSnap]    nvarchar(500) NOT NULL,
    [EstadoSnap]   bit NOT NULL,
    [IntentosSnap] int NOT NULL,
    PRIMARY KEY CLUSTERED ([IdVersion])
);
GO

CREATE TABLE [dbo].[Idioma] (
    [IdIdioma]  int IDENTITY(1, 1) NOT NULL,
    [Codigo]    varchar(5) NOT NULL,
    [Nombre]    nvarchar(50) NOT NULL,
    [Activo]    bit NOT NULL DEFAULT ((1)),
    [EsDefault] bit NOT NULL DEFAULT ((0)),
    PRIMARY KEY CLUSTERED ([IdIdioma]),
    CONSTRAINT [UQ_Idioma_Codigo] UNIQUE NONCLUSTERED ([Codigo])
);
GO

CREATE TABLE [dbo].[ListaEspera] (
    [IdListaEspera]      int IDENTITY(1, 1) NOT NULL,
    [IdPrenda]           int NOT NULL,
    [IdCliente]          int NOT NULL,
    [FechaAlta]          datetime NOT NULL DEFAULT (getdate()),
    [Estado]             int NOT NULL DEFAULT ((0)),
    [FechaLimiteReserva] datetime NULL,
    [FechaResolucion]    datetime NULL,
    [Actor]              nvarchar(100) NULL,
    PRIMARY KEY CLUSTERED ([IdListaEspera]),
    CONSTRAINT [CHK_ListaEspera_Estado] CHECK ([Estado]=(3) OR [Estado]=(2) OR [Estado]=(1) OR [Estado]=(0))
);
GO

CREATE TABLE [dbo].[MantenimientoPrenda] (
    [IdMantenimiento] int IDENTITY(1, 1) NOT NULL,
    [IdPrenda]        int NOT NULL,
    [FechaEntrada]    datetime NOT NULL DEFAULT (getdate()),
    [FechaSalida]     datetime NULL,
    [Actor]           nvarchar(100) NULL,
    [Origen]          tinyint NOT NULL CONSTRAINT [DF_MantenimientoPrenda_Origen] DEFAULT ((0)),
    PRIMARY KEY CLUSTERED ([IdMantenimiento]),
    CONSTRAINT [CHK_MantenimientoPrenda_Origen] CHECK ([Origen]=(1) OR [Origen]=(0))
);
GO

CREATE TABLE [dbo].[MedioPago] (
    [IdMedioPago]     int NOT NULL,
    [Nombre]          nvarchar(50) NOT NULL,
    [ClaveTraduccion] nvarchar(100) NOT NULL,
    [PermiteCuotas]   bit NOT NULL CONSTRAINT [DF_MedioPago_PermiteCuotas] DEFAULT ((0)),
    [Activo]          bit NOT NULL CONSTRAINT [DF_MedioPago_Activo] DEFAULT ((1)),
    PRIMARY KEY CLUSTERED ([IdMedioPago]),
    CONSTRAINT [UX_MedioPago_Nombre] UNIQUE NONCLUSTERED ([Nombre])
);
GO

CREATE TABLE [dbo].[ParametroSistema] (
    [Clave] nvarchar(100) NOT NULL,
    [Valor] nvarchar(400) NULL,
    [Fecha] datetime NOT NULL DEFAULT (getdate()),
    PRIMARY KEY CLUSTERED ([Clave])
);
GO

CREATE TABLE [dbo].[Pedido] (
    [IdPedido]            int IDENTITY(1, 1) NOT NULL,
    [IdCliente]           int NOT NULL,
    [IdEmpleado]          int NOT NULL,
    [Estado]              int NOT NULL DEFAULT ((0)),
    [FechaPedido]         datetime NOT NULL DEFAULT (getdate()),
    [FechaDespacho]       datetime NULL,
    [FechaEntrega]        datetime NULL,
    [MotivoCancelacion]   nvarchar(500) NULL,
    [DVH]                 int NULL,
    [FechaEnvioControl]   datetime NULL,
    [FechaControl]        datetime NULL,
    [IdEmpleadoControl]   int NULL,
    [FechaSeparacion]     datetime NULL,
    [FechaFormalizacion]  datetime NULL,
    [MotivoDesistimiento] nvarchar(500) NULL,
    [EtapaDesistimiento]  nvarchar(20) NULL,
    [FechaDevolucion]     datetime NULL,
    PRIMARY KEY CLUSTERED ([IdPedido]),
    CONSTRAINT [CHK_Pedido_Desistido] CHECK ([Estado]<>(7) OR [EtapaDesistimiento] IS NOT NULL AND [MotivoDesistimiento] IS NOT NULL),
    CONSTRAINT [CHK_Pedido_Estado] CHECK ([Estado]=(7) OR [Estado]=(6) OR [Estado]=(5) OR [Estado]=(4) OR [Estado]=(3) OR [Estado]=(2) OR [Estado]=(1) OR [Estado]=(0)),
    CONSTRAINT [CHK_Pedido_EtapaDesistimiento] CHECK ([EtapaDesistimiento] IS NULL OR ([EtapaDesistimiento]='Disponibilidad' OR [EtapaDesistimiento]='Cupo'))
);
GO

CREATE TABLE [dbo].[PedidoFaltante] (
    [IdPedido]          int NOT NULL,
    [IdPrenda]          int NOT NULL,
    [EstadoAlRevisar]   int NOT NULL,
    [ReservadaParaOtro] bit NOT NULL DEFAULT ((0)),
    CONSTRAINT [PK_PedidoFaltante] PRIMARY KEY CLUSTERED ([IdPedido], [IdPrenda])
);
GO

CREATE TABLE [dbo].[PedidoFaltanteAlternativa] (
    [IdPedido]            int NOT NULL,
    [IdPrenda]            int NOT NULL,
    [IdPrendaAlternativa] int NOT NULL,
    CONSTRAINT [PK_PedidoFaltanteAlternativa] PRIMARY KEY CLUSTERED ([IdPedido], [IdPrenda], [IdPrendaAlternativa]),
    CONSTRAINT [CHK_PedidoFaltanteAlternativa_Distinta] CHECK ([IdPrendaAlternativa]<>[IdPrenda])
);
GO

CREATE TABLE [dbo].[PedidoHistorial] (
    [IdHistorial]   int IDENTITY(1, 1) NOT NULL,
    [IdPedido]      int NOT NULL,
    [IdOperacion]   int NOT NULL,
    [Fecha]         datetime NOT NULL DEFAULT (getdate()),
    [IdUsuario]     int NULL,
    [NombreUsuario] nvarchar(200) NULL,
    [Accion]        nvarchar(200) NOT NULL,
    [Campo]         nvarchar(100) NOT NULL,
    [ValorAnterior] nvarchar(1000) NULL,
    [ValorNuevo]    nvarchar(1000) NULL,
    PRIMARY KEY CLUSTERED ([IdHistorial]),
    CONSTRAINT [UQ_PedidoHistorial_Operacion] UNIQUE NONCLUSTERED ([IdPedido], [IdOperacion], [Campo])
);
GO

CREATE TABLE [dbo].[PedidoPrenda] (
    [IdPedido]   int NOT NULL,
    [IdPrenda]   int NOT NULL,
    [Confirmada] bit NOT NULL CONSTRAINT [DF_PedidoPrenda_Confirmada] DEFAULT ((0)),
    CONSTRAINT [PK_PedidoPrenda] PRIMARY KEY CLUSTERED ([IdPedido], [IdPrenda])
);
GO

CREATE TABLE [dbo].[Permiso] (
    [IdPermiso]      int IDENTITY(1, 1) NOT NULL,
    [Nombre]         nvarchar(100) NOT NULL,
    [NombreMenu]     nvarchar(100) NULL,
    [TipoComponente] nvarchar(100) NULL,
    [Estado]         bit NOT NULL DEFAULT ((1)),
    [EsFamilia]      bit NOT NULL DEFAULT ((0)),
    [EsRol]          bit NOT NULL DEFAULT ((0)),
    PRIMARY KEY CLUSTERED ([IdPermiso])
);
GO

CREATE TABLE [dbo].[PermisoRelacion] (
    [IdPadre] int NOT NULL,
    [IdHijo]  int NOT NULL,
    [DVH]     int NULL,
    CONSTRAINT [PK_PermisoRelacion] PRIMARY KEY CLUSTERED ([IdPadre], [IdHijo]),
    CONSTRAINT [CK_PermisoRelacion_NoAutoref] CHECK ([IdPadre]<>[IdHijo])
);
GO

CREATE TABLE [dbo].[PlanCuotas] (
    [IdPlanCuotas]      int NOT NULL,
    [CantidadCuotas]    int NOT NULL,
    [RecargoPorcentaje] decimal(5, 2) NOT NULL,
    [Activo]            bit NOT NULL CONSTRAINT [DF_PlanCuotas_Activo] DEFAULT ((1)),
    PRIMARY KEY CLUSTERED ([IdPlanCuotas]),
    CONSTRAINT [UX_PlanCuotas_Cantidad] UNIQUE NONCLUSTERED ([CantidadCuotas]),
    CONSTRAINT [CHK_PlanCuotas_Cantidad] CHECK ([CantidadCuotas]>=(1)),
    CONSTRAINT [CHK_PlanCuotas_Recargo] CHECK ([RecargoPorcentaje]>=(0) AND [RecargoPorcentaje]<=(100))
);
GO

CREATE TABLE [dbo].[PlanSuscripcion] (
    [IdPlan]        int IDENTITY(1, 1) NOT NULL,
    [Nombre]        nvarchar(100) NOT NULL,
    [LimitePrendas] int NOT NULL DEFAULT ((0)),
    [Precio]        decimal(10, 2) NOT NULL DEFAULT ((0)),
    [Estado]        bit NOT NULL DEFAULT ((1)),
    PRIMARY KEY CLUSTERED ([IdPlan])
);
GO

CREATE TABLE [dbo].[Preferencia] (
    [IdUsuario]      int NOT NULL,
    [FuenteFamilia]  nvarchar(50) NULL,
    [FuenteTamano]   varchar(10) NULL,
    [Tema]           varchar(20) NULL,
    [FormatoFecha]   varchar(20) NULL,
    [Notificaciones] bit NULL,
    PRIMARY KEY CLUSTERED ([IdUsuario])
);
GO

CREATE TABLE [dbo].[Prenda] (
    [IdPrenda]         int IDENTITY(1, 1) NOT NULL,
    [Nombre]           nvarchar(100) NOT NULL,
    [Descripcion]      nvarchar(500) NULL,
    [Talle]            nvarchar(20) NULL,
    [Color]            nvarchar(50) NULL,
    [Categoria]        nvarchar(100) NULL,
    [Estado]           int NOT NULL DEFAULT ((0)),
    [IdClienteActual]  int NULL,
    [FechaAlta]        datetime NOT NULL DEFAULT (getdate()),
    [IdUltimoCliente]  int NULL,
    [PrecioReposicion] decimal(10, 2) NULL,
    PRIMARY KEY CLUSTERED ([IdPrenda]),
    CONSTRAINT [CHK_Prenda_Estado] CHECK ([Estado]=(3) OR [Estado]=(2) OR [Estado]=(1) OR [Estado]=(0)),
    CONSTRAINT [CHK_Prenda_PrecioReposicion] CHECK ([PrecioReposicion] IS NULL OR [PrecioReposicion]>(0))
);
GO

CREATE TABLE [dbo].[Promocion] (
    [IdPromocion]        int IDENTITY(1, 1) NOT NULL,
    [Nombre]             nvarchar(150) NOT NULL,
    [Descripcion]        nvarchar(500) NULL,
    [TipoDescuento]      int NOT NULL,
    [Valor]              decimal(10, 2) NOT NULL,
    [FechaInicio]        date NOT NULL,
    [FechaFin]           date NOT NULL,
    [Estado]             int NOT NULL DEFAULT ((0)),
    [IdPlan]             int NOT NULL,
    [MargenEstimado]     decimal(10, 2) NOT NULL DEFAULT ((0)),
    [ImpactoEconomico]   nvarchar(500) NULL,
    [IdSugerenciaOrigen] int NULL,
    [FechaAlta]          datetime NOT NULL DEFAULT (getdate()),
    [IdUsuarioAlta]      int NULL,
    PRIMARY KEY CLUSTERED ([IdPromocion]),
    CONSTRAINT [CHK_Promocion_Estado] CHECK ([Estado]=(6) OR [Estado]=(4) OR [Estado]=(2) OR [Estado]=(1) OR [Estado]=(0)),
    CONSTRAINT [CHK_Promocion_Fechas] CHECK ([FechaFin]>=[FechaInicio]),
    CONSTRAINT [CHK_Promocion_Porcentaje] CHECK ([TipoDescuento]<>(0) OR [Valor]<=(100)),
    CONSTRAINT [CHK_Promocion_Tipo] CHECK ([TipoDescuento]=(2) OR [TipoDescuento]=(1) OR [TipoDescuento]=(0)),
    CONSTRAINT [CHK_Promocion_Valor] CHECK ([Valor]>(0))
);
GO

CREATE TABLE [dbo].[PromocionHistorial] (
    [IdHistorial]    int IDENTITY(1, 1) NOT NULL,
    [IdPromocion]    int NOT NULL,
    [EstadoAnterior] int NULL,
    [EstadoNuevo]    int NOT NULL,
    [IdUsuario]      int NULL,
    [Fecha]          datetime NOT NULL DEFAULT (getdate()),
    [Observacion]    nvarchar(500) NULL,
    PRIMARY KEY CLUSTERED ([IdHistorial]),
    CONSTRAINT [CHK_PromocionHistorial_Anterior] CHECK ([EstadoAnterior]=(6) OR [EstadoAnterior]=(4) OR [EstadoAnterior]=(2) OR [EstadoAnterior]=(1) OR [EstadoAnterior]=(0)),
    CONSTRAINT [CHK_PromocionHistorial_Nuevo] CHECK ([EstadoNuevo]=(6) OR [EstadoNuevo]=(4) OR [EstadoNuevo]=(2) OR [EstadoNuevo]=(1) OR [EstadoNuevo]=(0))
);
GO

CREATE TABLE [dbo].[SolicitudBajaPromocion] (
    [IdSolicitud]       int IDENTITY(1, 1) NOT NULL,
    [IdPromocion]       int NOT NULL,
    [IdUsuarioSolicita] int NOT NULL,
    [Motivo]            nvarchar(500) NOT NULL,
    [FechaSolicitud]    datetime NOT NULL DEFAULT (getdate()),
    [Estado]            int NOT NULL DEFAULT ((0)),
    [IdUsuarioResuelve] int NULL,
    [FechaResolucion]   datetime NULL,
    [Respuesta]         nvarchar(500) NULL,
    [DioDeBaja]         bit NOT NULL CONSTRAINT [DF_SolicitudBaja_DioDeBaja] DEFAULT ((0)),
    PRIMARY KEY CLUSTERED ([IdSolicitud]),
    CONSTRAINT [CHK_SolicitudBaja_Estado] CHECK ([Estado]=(1) OR [Estado]=(0)),
    CONSTRAINT [CHK_SolicitudBaja_Resolucion] CHECK ([Estado]=(0) AND [IdUsuarioResuelve] IS NULL AND [FechaResolucion] IS NULL OR [Estado]<>(0) AND [IdUsuarioResuelve] IS NOT NULL AND [FechaResolucion] IS NOT NULL),
    CONSTRAINT [CHK_SolicitudBaja_Respuesta] CHECK ([Estado]=(0) AND [Respuesta] IS NULL AND [DioDeBaja]=(0) OR [Estado]=(1) AND [Respuesta] IS NOT NULL)
);
GO

CREATE TABLE [dbo].[SugerenciaPromocion] (
    [IdSugerencia]          int IDENTITY(1, 1) NOT NULL,
    [IdPlan]                int NULL,
    [Motivo]                nvarchar(500) NOT NULL,
    [TipoDescuentoSugerido] int NOT NULL,
    [BeneficioEstimado]     decimal(10, 2) NOT NULL,
    [Estado]                int NOT NULL DEFAULT ((0)),
    [FechaAlta]             datetime NOT NULL DEFAULT (getdate()),
    [OrigenMetrica]         int NOT NULL CONSTRAINT [DF_SugerenciaPromocion_Origen] DEFAULT ((2)),
    [IdUsuarioAlta]         int NULL,
    [MotivoDescarte]        nvarchar(500) NULL,
    [FechaEvaluacion]       datetime NULL,
    [TasaRetencion]         decimal(5, 2) NOT NULL CONSTRAINT [DF_SugerenciaPromocion_TasaRetencion] DEFAULT ((30)),
    [IngresoEnRiesgo]       decimal(12, 2) NULL,
    PRIMARY KEY CLUSTERED ([IdSugerencia]),
    CONSTRAINT [CHK_SugerenciaPromocion_Beneficio] CHECK ([IngresoEnRiesgo] IS NULL OR [BeneficioEstimado]=round(([IngresoEnRiesgo]*[TasaRetencion])/(100),(2))),
    CONSTRAINT [CHK_SugerenciaPromocion_Descarte] CHECK ([Estado]<>(2) OR [MotivoDescarte] IS NOT NULL AND [FechaEvaluacion] IS NOT NULL),
    CONSTRAINT [CHK_SugerenciaPromocion_Estado] CHECK ([Estado]=(2) OR [Estado]=(1) OR [Estado]=(0)),
    CONSTRAINT [CHK_SugerenciaPromocion_Origen] CHECK ([OrigenMetrica]=(2) OR [OrigenMetrica]=(1) OR [OrigenMetrica]=(0)),
    CONSTRAINT [CHK_SugerenciaPromocion_TasaRetencion] CHECK ([TasaRetencion]>(0) AND [TasaRetencion]<=(100)),
    CONSTRAINT [CHK_SugerenciaPromocion_Tipo] CHECK ([TipoDescuentoSugerido]=(2) OR [TipoDescuentoSugerido]=(1) OR [TipoDescuentoSugerido]=(0))
);
GO

CREATE TABLE [dbo].[Traduccion] (
    [IdControl] int NOT NULL,
    [IdIdioma]  int NOT NULL,
    [Texto]     nvarchar(1000) NOT NULL DEFAULT (''),
    CONSTRAINT [PK_Traduccion] PRIMARY KEY CLUSTERED ([IdControl], [IdIdioma])
);
GO

CREATE TABLE [dbo].[Usuario] (
    [IdUsuario]           int IDENTITY(1, 1) NOT NULL,
    [Username]            nvarchar(100) NOT NULL,
    [Clave]               nvarchar(500) NOT NULL,
    [Rol]                 nvarchar(100) NULL,
    [Perfil]              nvarchar(100) NULL,
    [Estado]              bit NOT NULL DEFAULT ((1)),
    [IntentosFallidos]    int NOT NULL DEFAULT ((0)),
    [DVH]                 int NULL,
    [IdIdioma]            varchar(5) NULL,
    [Activo]              bit NOT NULL DEFAULT ((1)),
    [FechaBaja]           datetime NULL,
    [CantidadBloqueos]    int NOT NULL DEFAULT ((0)),
    [FechaBloqueo]        datetime NULL,
    [RequiereCambioClave] bit NOT NULL DEFAULT ((0)),
    [Nombre]              nvarchar(100) NULL,
    [Apellido]            nvarchar(100) NULL,
    [FechaNacimiento]     date NULL,
    [Email]               nvarchar(200) NULL,
    PRIMARY KEY CLUSTERED ([IdUsuario]),
    CONSTRAINT [UQ_Usuario_Username] UNIQUE NONCLUSTERED ([Username]),
    CONSTRAINT [CK_Usuario_PerfilEsRol] CHECK ([Rol] IS NULL OR [Perfil] IS NULL OR [Rol]=[Perfil])
);
GO

CREATE TABLE [dbo].[Usuario_Seguridad] (
    [IdUsuario]           int NOT NULL,
    [Username]            nvarchar(100) NOT NULL,
    [Clave]               nvarchar(500) NOT NULL,
    [Rol]                 nvarchar(100) NULL,
    [Perfil]              nvarchar(100) NULL,
    [Estado]              bit NOT NULL DEFAULT ((1)),
    [IntentosFallidos]    int NOT NULL DEFAULT ((0)),
    [DVH]                 int NULL,
    [FechaActualizacion]  datetime NOT NULL DEFAULT (getdate()),
    [Activo]              bit NOT NULL CONSTRAINT [DF_UsuarioSeg_Activo] DEFAULT ((1)),
    [RequiereCambioClave] bit NOT NULL CONSTRAINT [DF_UsuarioSeg_RCC] DEFAULT ((0)),
    [CantidadBloqueos]    int NOT NULL CONSTRAINT [DF_UsuarioSeg_CB] DEFAULT ((0)),
    [FechaBloqueo]        datetime NULL,
    PRIMARY KEY CLUSTERED ([IdUsuario])
);
GO

-- ============================================================
-- 2. ÃNDICES (Ãºnicos, filtrados y de bÃºsqueda)
-- ============================================================

CREATE NONCLUSTERED INDEX [IX_Bitacora_fecha] ON [dbo].[Bitacora] ([fecha]);
CREATE NONCLUSTERED INDEX [IX_Bitacora_usuario] ON [dbo].[Bitacora] ([usuario]);
CREATE NONCLUSTERED INDEX [IX_BitacoraNegocio_Fecha] ON [dbo].[BitacoraNegocio] ([Fecha]);
CREATE NONCLUSTERED INDEX [IX_CargoPrenda_IdCliente] ON [dbo].[CargoPrenda] ([IdCliente]);
CREATE NONCLUSTERED INDEX [IX_Cliente_IdClienteReferente] ON [dbo].[Cliente] ([IdClienteReferente]);
CREATE NONCLUSTERED INDEX [IX_Cliente_IdMedioPagoPreferido] ON [dbo].[Cliente] ([IdMedioPagoPreferido]);
CREATE NONCLUSTERED INDEX [IX_Cliente_IdPlan] ON [dbo].[Cliente] ([IdPlan]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_Cliente_DNI_Activo] ON [dbo].[Cliente] ([DNI]) WHERE ([Activo]=(1));
CREATE NONCLUSTERED INDEX [IX_Contratacion_Estado] ON [dbo].[Contratacion] ([Estado]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_Contratacion_Comprobante] ON [dbo].[Contratacion] ([NumeroComprobante]) WHERE ([NumeroComprobante] IS NOT NULL);
CREATE UNIQUE NONCLUSTERED INDEX [UX_Contratacion_UnaPendientePorCliente] ON [dbo].[Contratacion] ([IdCliente]) WHERE ([Estado]=(0));
CREATE NONCLUSTERED INDEX [IX_IntentoPago_Contratacion] ON [dbo].[ContratacionIntentoPago] ([IdContratacion]);
CREATE NONCLUSTERED INDEX [IX_DecisionComercial_Clave] ON [dbo].[DecisionComercial] ([ClaveRecomendacion], [Fecha] DESC);
CREATE NONCLUSTERED INDEX [IX_DecisionComercial_Fecha] ON [dbo].[DecisionComercial] ([Fecha]);
CREATE NONCLUSTERED INDEX [IX_DictamenContable_Promocion] ON [dbo].[DictamenContable] ([IdPromocion]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_Empleado_DNI] ON [dbo].[Empleado] ([DNI]) WHERE ([DNI]<>N'');
CREATE UNIQUE NONCLUSTERED INDEX [UX_Empleado_IdUsuario] ON [dbo].[Empleado] ([IdUsuario]) WHERE ([IdUsuario] IS NOT NULL);
CREATE UNIQUE NONCLUSTERED INDEX [UX_Empleado_Legajo] ON [dbo].[Empleado] ([Legajo]) WHERE ([Legajo] IS NOT NULL);
CREATE NONCLUSTERED INDEX [IX_HistorialCobro_IdPromocion] ON [dbo].[HistorialCobro] ([IdPromocion]);
CREATE NONCLUSTERED INDEX [IX_HistorialUsuario_IdUsuario] ON [dbo].[HistorialUsuario] ([IdUsuario]);
CREATE NONCLUSTERED INDEX [IX_ListaEspera_Prenda_Estado] ON [dbo].[ListaEspera] ([IdPrenda], [Estado]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_ListaEspera_AnotacionActiva] ON [dbo].[ListaEspera] ([IdPrenda], [IdCliente]) WHERE ([Estado] IN ((0), (1)));
CREATE NONCLUSTERED INDEX [IX_MantenimientoPrenda_IdPrenda] ON [dbo].[MantenimientoPrenda] ([IdPrenda]);
CREATE NONCLUSTERED INDEX [IX_Pedido_Estado] ON [dbo].[Pedido] ([Estado]);
CREATE NONCLUSTERED INDEX [IX_Pedido_FechaPedido] ON [dbo].[Pedido] ([FechaPedido]);
CREATE NONCLUSTERED INDEX [IX_Pedido_IdCliente] ON [dbo].[Pedido] ([IdCliente]);
CREATE NONCLUSTERED INDEX [IX_Pedido_IdEmpleado] ON [dbo].[Pedido] ([IdEmpleado]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_Pedido_ClienteActivo] ON [dbo].[Pedido] ([IdCliente]) WHERE ([Estado] IN ((0), (1), (4), (5), (6)));
CREATE NONCLUSTERED INDEX [IX_PedidoHistorial_IdPedido] ON [dbo].[PedidoHistorial] ([IdPedido]);
CREATE NONCLUSTERED INDEX [IX_PedidoPrenda_IdPrenda] ON [dbo].[PedidoPrenda] ([IdPrenda]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_Permiso_NombreRol] ON [dbo].[Permiso] ([Nombre]) WHERE ([EsRol]=(1));
CREATE NONCLUSTERED INDEX [IX_PermisoRelacion_IdHijo] ON [dbo].[PermisoRelacion] ([IdHijo]);
CREATE NONCLUSTERED INDEX [IX_Prenda_Categoria] ON [dbo].[Prenda] ([Categoria]);
CREATE NONCLUSTERED INDEX [IX_Prenda_Estado] ON [dbo].[Prenda] ([Estado]) INCLUDE ([Categoria], [Talle]);
CREATE NONCLUSTERED INDEX [IX_Prenda_IdClienteActual] ON [dbo].[Prenda] ([IdClienteActual]);
CREATE NONCLUSTERED INDEX [IX_Prenda_IdUltimoCliente] ON [dbo].[Prenda] ([IdUltimoCliente]);
CREATE NONCLUSTERED INDEX [IX_Promocion_Estado] ON [dbo].[Promocion] ([Estado]);
CREATE NONCLUSTERED INDEX [IX_Promocion_IdPlan] ON [dbo].[Promocion] ([IdPlan]);
CREATE NONCLUSTERED INDEX [IX_PromocionHistorial_Promocion] ON [dbo].[PromocionHistorial] ([IdPromocion]);
CREATE NONCLUSTERED INDEX [IX_SolicitudBaja_Promocion] ON [dbo].[SolicitudBajaPromocion] ([IdPromocion]);
CREATE UNIQUE NONCLUSTERED INDEX [UX_SolicitudBaja_UnaPendiente] ON [dbo].[SolicitudBajaPromocion] ([IdPromocion]) WHERE ([Estado]=(0));
CREATE NONCLUSTERED INDEX [IX_Traduccion_IdIdioma] ON [dbo].[Traduccion] ([IdIdioma]);
GO

-- ============================================================
-- 3. CLAVES FORÃNEAS
-- ============================================================

ALTER TABLE [dbo].[Bitacora] ADD FOREIGN KEY ([usuario]) REFERENCES [dbo].[Usuario] ([IdUsuario]);

ALTER TABLE [dbo].[BitacoraNegocio] ADD FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Cliente] ([IdCliente]);
ALTER TABLE [dbo].[BitacoraNegocio] ADD FOREIGN KEY ([IdPedido]) REFERENCES [dbo].[Pedido] ([IdPedido]);
ALTER TABLE [dbo].[BitacoraNegocio] ADD FOREIGN KEY ([IdPrenda]) REFERENCES [dbo].[Prenda] ([IdPrenda]);
ALTER TABLE [dbo].[BitacoraNegocio] ADD FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuario] ([IdUsuario]);

ALTER TABLE [dbo].[CargoPrenda] ADD FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Cliente] ([IdCliente]);
ALTER TABLE [dbo].[CargoPrenda] ADD FOREIGN KEY ([IdPrenda]) REFERENCES [dbo].[Prenda] ([IdPrenda]);

ALTER TABLE [dbo].[Cliente] ADD FOREIGN KEY ([IdClienteReferente]) REFERENCES [dbo].[Cliente] ([IdCliente]);
ALTER TABLE [dbo].[Cliente] ADD FOREIGN KEY ([IdPlan]) REFERENCES [dbo].[PlanSuscripcion] ([IdPlan]);
ALTER TABLE [dbo].[Cliente] ADD CONSTRAINT [FK_Cliente_MedioPagoPreferido] FOREIGN KEY ([IdMedioPagoPreferido]) REFERENCES [dbo].[MedioPago] ([IdMedioPago]);
ALTER TABLE [dbo].[Cliente] ADD CONSTRAINT [FK_Cliente_PlanSiguiente] FOREIGN KEY ([IdPlanSiguiente]) REFERENCES [dbo].[PlanSuscripcion] ([IdPlan]);

ALTER TABLE [dbo].[Contratacion] ADD FOREIGN KEY ([IdCaja]) REFERENCES [dbo].[Empleado] ([IdEmpleado]);
ALTER TABLE [dbo].[Contratacion] ADD FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Cliente] ([IdCliente]);
ALTER TABLE [dbo].[Contratacion] ADD FOREIGN KEY ([IdPlan]) REFERENCES [dbo].[PlanSuscripcion] ([IdPlan]);
ALTER TABLE [dbo].[Contratacion] ADD FOREIGN KEY ([IdVendedor]) REFERENCES [dbo].[Empleado] ([IdEmpleado]);
ALTER TABLE [dbo].[Contratacion] ADD CONSTRAINT [FK_Contratacion_MedioPago] FOREIGN KEY ([IdMedioPago]) REFERENCES [dbo].[MedioPago] ([IdMedioPago]);
ALTER TABLE [dbo].[Contratacion] ADD CONSTRAINT [FK_Contratacion_PlanCuotas] FOREIGN KEY ([IdPlanCuotas]) REFERENCES [dbo].[PlanCuotas] ([IdPlanCuotas]);
ALTER TABLE [dbo].[Contratacion] ADD CONSTRAINT [FK_Contratacion_Promocion] FOREIGN KEY ([IdPromocion]) REFERENCES [dbo].[Promocion] ([IdPromocion]);
ALTER TABLE [dbo].[Contratacion] ADD CONSTRAINT [FK_Contratacion_Referente] FOREIGN KEY ([IdReferenteAcreditado]) REFERENCES [dbo].[Cliente] ([IdCliente]);

ALTER TABLE [dbo].[ContratacionIntentoPago] ADD CONSTRAINT [FK_IntentoPago_Caja] FOREIGN KEY ([IdCaja]) REFERENCES [dbo].[Empleado] ([IdEmpleado]);
ALTER TABLE [dbo].[ContratacionIntentoPago] ADD CONSTRAINT [FK_IntentoPago_Contratacion] FOREIGN KEY ([IdContratacion]) REFERENCES [dbo].[Contratacion] ([IdContratacion]);
ALTER TABLE [dbo].[ContratacionIntentoPago] ADD CONSTRAINT [FK_IntentoPago_MedioPago] FOREIGN KEY ([IdMedioPago]) REFERENCES [dbo].[MedioPago] ([IdMedioPago]);

ALTER TABLE [dbo].[ControlMapeado] ADD CONSTRAINT [FK_ControlMapeado_Permiso] FOREIGN KEY ([IdPermiso]) REFERENCES [dbo].[Permiso] ([IdPermiso]);

ALTER TABLE [dbo].[DesistimientoContratacion] ADD CONSTRAINT [FK_DesistContr_Cliente] FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Cliente] ([IdCliente]);
ALTER TABLE [dbo].[DesistimientoContratacion] ADD CONSTRAINT [FK_DesistContr_Plan] FOREIGN KEY ([IdPlan]) REFERENCES [dbo].[PlanSuscripcion] ([IdPlan]);
ALTER TABLE [dbo].[DesistimientoContratacion] ADD CONSTRAINT [FK_DesistContr_Vendedor] FOREIGN KEY ([IdVendedor]) REFERENCES [dbo].[Empleado] ([IdEmpleado]);

ALTER TABLE [dbo].[DecisionComercial] ADD CONSTRAINT [FK_DecisionComercial_Usuario] FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuario] ([IdUsuario]);
ALTER TABLE [dbo].[DictamenContable] ADD CONSTRAINT [FK_DictamenContable_Promocion] FOREIGN KEY ([IdPromocion]) REFERENCES [dbo].[Promocion] ([IdPromocion]);
ALTER TABLE [dbo].[DictamenContable] ADD CONSTRAINT [FK_DictamenContable_Usuario] FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuario] ([IdUsuario]);

ALTER TABLE [dbo].[Empleado] ADD FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuario] ([IdUsuario]);

ALTER TABLE [dbo].[HistorialCobro] ADD FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Cliente] ([IdCliente]);
ALTER TABLE [dbo].[HistorialCobro] ADD CONSTRAINT [FK_HistorialCobro_MedioPago] FOREIGN KEY ([IdMedioPago]) REFERENCES [dbo].[MedioPago] ([IdMedioPago]);
ALTER TABLE [dbo].[HistorialCobro] ADD CONSTRAINT [FK_HistorialCobro_Promocion] FOREIGN KEY ([IdPromocion]) REFERENCES [dbo].[Promocion] ([IdPromocion]);

ALTER TABLE [dbo].[HistorialRenovacion] ADD FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Cliente] ([IdCliente]);
ALTER TABLE [dbo].[HistorialRenovacion] ADD FOREIGN KEY ([IdPlanAnterior]) REFERENCES [dbo].[PlanSuscripcion] ([IdPlan]);
ALTER TABLE [dbo].[HistorialRenovacion] ADD FOREIGN KEY ([IdPlanNuevo]) REFERENCES [dbo].[PlanSuscripcion] ([IdPlan]);

ALTER TABLE [dbo].[HistorialUsuario] ADD FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuario] ([IdUsuario]);

ALTER TABLE [dbo].[ListaEspera] ADD FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Cliente] ([IdCliente]);
ALTER TABLE [dbo].[ListaEspera] ADD FOREIGN KEY ([IdPrenda]) REFERENCES [dbo].[Prenda] ([IdPrenda]);

ALTER TABLE [dbo].[MantenimientoPrenda] ADD FOREIGN KEY ([IdPrenda]) REFERENCES [dbo].[Prenda] ([IdPrenda]);

ALTER TABLE [dbo].[Pedido] ADD FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Cliente] ([IdCliente]);
ALTER TABLE [dbo].[Pedido] ADD FOREIGN KEY ([IdEmpleado]) REFERENCES [dbo].[Empleado] ([IdEmpleado]);
ALTER TABLE [dbo].[Pedido] ADD CONSTRAINT [FK_Pedido_EmpleadoControl] FOREIGN KEY ([IdEmpleadoControl]) REFERENCES [dbo].[Empleado] ([IdEmpleado]);

ALTER TABLE [dbo].[PedidoFaltante] ADD CONSTRAINT [FK_PedidoFaltante_Linea] FOREIGN KEY ([IdPedido], [IdPrenda]) REFERENCES [dbo].[PedidoPrenda] ([IdPedido], [IdPrenda]);

ALTER TABLE [dbo].[PedidoFaltanteAlternativa] ADD FOREIGN KEY ([IdPrendaAlternativa]) REFERENCES [dbo].[Prenda] ([IdPrenda]);
ALTER TABLE [dbo].[PedidoFaltanteAlternativa] ADD CONSTRAINT [FK_PedidoFaltanteAlternativa_Faltante] FOREIGN KEY ([IdPedido], [IdPrenda]) REFERENCES [dbo].[PedidoFaltante] ([IdPedido], [IdPrenda]);

ALTER TABLE [dbo].[PedidoHistorial] ADD FOREIGN KEY ([IdPedido]) REFERENCES [dbo].[Pedido] ([IdPedido]);
ALTER TABLE [dbo].[PedidoHistorial] ADD FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuario] ([IdUsuario]);

ALTER TABLE [dbo].[PedidoPrenda] ADD FOREIGN KEY ([IdPedido]) REFERENCES [dbo].[Pedido] ([IdPedido]);
ALTER TABLE [dbo].[PedidoPrenda] ADD FOREIGN KEY ([IdPrenda]) REFERENCES [dbo].[Prenda] ([IdPrenda]);

ALTER TABLE [dbo].[PermisoRelacion] ADD FOREIGN KEY ([IdHijo]) REFERENCES [dbo].[Permiso] ([IdPermiso]);
ALTER TABLE [dbo].[PermisoRelacion] ADD FOREIGN KEY ([IdPadre]) REFERENCES [dbo].[Permiso] ([IdPermiso]);

ALTER TABLE [dbo].[Preferencia] ADD CONSTRAINT [FK_Preferencia_Usuario] FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuario] ([IdUsuario]) ON DELETE CASCADE;

ALTER TABLE [dbo].[Prenda] ADD FOREIGN KEY ([IdClienteActual]) REFERENCES [dbo].[Cliente] ([IdCliente]);
ALTER TABLE [dbo].[Prenda] ADD FOREIGN KEY ([IdUltimoCliente]) REFERENCES [dbo].[Cliente] ([IdCliente]);
ALTER TABLE [dbo].[Prenda] ADD CONSTRAINT [FK_Prenda_Categoria] FOREIGN KEY ([Categoria]) REFERENCES [dbo].[Categoria] ([Nombre]) ON UPDATE CASCADE;

ALTER TABLE [dbo].[Promocion] ADD FOREIGN KEY ([IdSugerenciaOrigen]) REFERENCES [dbo].[SugerenciaPromocion] ([IdSugerencia]);
ALTER TABLE [dbo].[Promocion] ADD CONSTRAINT [FK_Promocion_Plan] FOREIGN KEY ([IdPlan]) REFERENCES [dbo].[PlanSuscripcion] ([IdPlan]);
ALTER TABLE [dbo].[Promocion] ADD CONSTRAINT [FK_Promocion_UsuarioAlta] FOREIGN KEY ([IdUsuarioAlta]) REFERENCES [dbo].[Usuario] ([IdUsuario]);

ALTER TABLE [dbo].[PromocionHistorial] ADD CONSTRAINT [FK_PromocionHistorial_Promocion] FOREIGN KEY ([IdPromocion]) REFERENCES [dbo].[Promocion] ([IdPromocion]);
ALTER TABLE [dbo].[PromocionHistorial] ADD CONSTRAINT [FK_PromocionHistorial_Usuario] FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuario] ([IdUsuario]);

ALTER TABLE [dbo].[SolicitudBajaPromocion] ADD CONSTRAINT [FK_SolicitudBaja_Promocion] FOREIGN KEY ([IdPromocion]) REFERENCES [dbo].[Promocion] ([IdPromocion]);
ALTER TABLE [dbo].[SolicitudBajaPromocion] ADD CONSTRAINT [FK_SolicitudBaja_Resuelve] FOREIGN KEY ([IdUsuarioResuelve]) REFERENCES [dbo].[Usuario] ([IdUsuario]);
ALTER TABLE [dbo].[SolicitudBajaPromocion] ADD CONSTRAINT [FK_SolicitudBaja_Solicita] FOREIGN KEY ([IdUsuarioSolicita]) REFERENCES [dbo].[Usuario] ([IdUsuario]);

ALTER TABLE [dbo].[SugerenciaPromocion] ADD FOREIGN KEY ([IdPlan]) REFERENCES [dbo].[PlanSuscripcion] ([IdPlan]);
ALTER TABLE [dbo].[SugerenciaPromocion] ADD CONSTRAINT [FK_SugerenciaPromocion_UsuarioAlta] FOREIGN KEY ([IdUsuarioAlta]) REFERENCES [dbo].[Usuario] ([IdUsuario]);

ALTER TABLE [dbo].[Traduccion] ADD FOREIGN KEY ([IdControl]) REFERENCES [dbo].[Control] ([IdControl]);
ALTER TABLE [dbo].[Traduccion] ADD FOREIGN KEY ([IdIdioma]) REFERENCES [dbo].[Idioma] ([IdIdioma]);

ALTER TABLE [dbo].[Usuario] ADD CONSTRAINT [FK_Usuario_Idioma] FOREIGN KEY ([IdIdioma]) REFERENCES [dbo].[Idioma] ([Codigo]);
GO

-- Fin del esquema.
