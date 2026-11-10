# WardrobeFlow

Aplicación de escritorio para gestionar un servicio de **alquiler de indumentaria por suscripción**: clientes, prendas, planes, pedidos, contrataciones cobradas, promociones comerciales e inspección de prendas devueltas.

Desarrollada en **C# sobre .NET Framework 4.7.2** con **Windows Forms** (interfaz MDI) y **SQL Server**, como Trabajo de Diploma de la UAI (2026).

[![Versión](https://img.shields.io/github/v/release/valentinamorana/Trabajo-de-Diploma?label=versi%C3%B3n)](https://github.com/valentinamorana/Trabajo-de-Diploma/releases/latest)

**Autora:** Valentina Morana · **Documentación:** [Tesis (PDF)](docs/tesis.pdf) · [Diagramas UML](docs/diagramas) · [Referencia técnica](docs/referencia-tecnica.md)

---

## Funcionalidades

| Área | Qué resuelve |
|------|--------------|
| **Suscripciones** | Alta de clientes con planes mensuales, trimestrales o anuales (patrón Builder), renovaciones, cambios de plan y bajas en cadena de responsabilidad |
| **Contratación y cobro** | Venta por Vendedor y cobro por Caja, con cobro atómico, período de gracia ante pagos fallidos y suspensión de pedidos por falta de pago |
| **Pedidos y prendas** | Armado de pedidos respetando el cupo del plan, despacho, entrega, devoluciones y cancelaciones (patrón Command); estados de prenda con patrón State |
| **Inspección de devoluciones** | Reingreso o baja de prendas dañadas con cargo al cliente |
| **Inteligencia comercial** | Indicadores con variación, tendencias con proyección lineal, análisis de cohortes de retención, recomendaciones por reglas (patrón Strategy) con simulador de promociones y medición de resultados |
| **Reportes** | Seis reportes de decisión comercial y un informe ejecutivo, exportables a PDF (iTextSharp) y CSV |
| **Administración y seguridad** | Roles y permisos por árbol Composite, bitácora, historial de cambios con rollback (Memento), dígitos verificadores de integridad y backups cifrados |
| **Serialización** | Exportación e importación de versiones de usuario y de errores en XML y JSON |
| **Multiidioma** | Español, English, Русский y Português, con cambio en tiempo de ejecución |
| **Usabilidad** | Panel de control por rol, ayuda contextual con F1, tooltips y guía de primer uso |

Hay 10 roles de usuario con permisos acotados a su función (Administrador, Auditor, Vendedor, Gerente Comercial, Caja, Depósito, Operador logístico, Gerente de Inventario, Administración comercial y Contabilidad).

![Arquitectura por capas de la solución](docs/diagramas/Global%20-%20Soluci%C3%B3n%20por%20capas/Arquitectura%20%E2%80%94%20capas%20de%20la%20soluci%C3%B3n%20y%20sus%20dependencias.png)

---

## Tecnologías

| Componente | Tecnología |
|------------|------------|
| Lenguaje y plataforma | C# · .NET Framework 4.7.2 |
| Interfaz | Windows Forms (MDI) |
| Base de datos | SQL Server (LocalDB, Express o instancia existente) con ADO.NET, sin ORM |
| Seguridad | PBKDF2-SHA256 para contraseñas · AES-128 + HMAC-SHA256 para backups cifrados |
| Instalador | Inno Setup con cliente SQL embebido |
| Pruebas | MSTest, con más de 1.200 pruebas unitarias y de integración |

---

## Arquitectura

El sistema se organiza en capas. La interfaz nunca accede a datos ni a seguridad directamente: toda la lógica de negocio y las validaciones viven en la capa BLL.

```
GUI (Windows Forms MDI)
 └── BLL (lógica de negocio)
      ├── DAL (ADO.NET → SQL Server)
      ├── BE  (entidades de dominio, DTOs y patrones)
      ├── Servicios (bitácora, multiidioma, serialización, exportación)
      └── Seguridad (sesión, encriptado, dígitos verificadores)
```

Los patrones de diseño aplicados (Builder, State, Command, Chain of Responsibility, Strategy, Composite, Factory Method, Memento, Observer y Singleton) están documentados con su ubicación en el código en la [referencia técnica](docs/referencia-tecnica.md#patrones-de-diseño-implementados).

---

## Estructura del repositorio

```
BE/ BLL/ DAL/ GUI/ Seguridad/ Servicios/   Capas de la aplicación
Tests/                                    Pruebas unitarias (MSTest) con fakes de DAL
BD/                                       Scripts de base de datos
Instalador/                               Instalador (Inno Setup) y tutoriales de instalación
docs/                                     Tesis, diagramas UML y referencia técnica
WardrobeFlow.slnx                         Solución de Visual Studio
```

---

## Instalación

### Opción 1: instalador (recomendada)

Descargá el instalador `Instalador_WardrobeFlow_1.6.1.exe` desde la [última versión publicada](https://github.com/valentinamorana/Trabajo-de-Diploma/releases/latest). Incluye la aplicación, la base de datos con datos de prueba y SQL Server LocalDB. Si la PC no tiene ningún motor SQL Server, el instalador lo resuelve solo.

Para varios usuarios de Windows que comparten la base, se recomienda SQL Server Express. El paso a paso está en el [tutorial de instalación de SQL Server](Instalador/Tutorial_SQL_Server.html).

### Opción 2: desde el código fuente

Requisitos: Visual Studio 2022 (o superior), .NET Framework 4.7.2 y SQL Server 2012 o posterior.

1. Clonar el repositorio.
2. Ejecutar `BD/00_Instalacion_Completa.sql` en SQL Server (es idempotente y crea la base completa con datos de prueba).
3. Configurar la cadena de conexión en `GUI/App.config`.
4. Abrir `WardrobeFlow.slnx`, compilar y ejecutar `GUI` como proyecto de inicio.

El detalle de la cadena de conexión, la compilación desde línea de comandos y la generación del instalador está en la [referencia técnica](docs/referencia-tecnica.md#instalación-y-configuración).

### Pruebas

Las pruebas son una suite MSTest sobre `Tests.dll`, ejecutable desde el Explorador de pruebas de Visual Studio. Las de integración usan una base separada (`WardrobeFlowDB_Tests`) y nunca tocan la base de la aplicación.

---

## Documentación

- **[Tesis (PDF)](docs/tesis.pdf)**: documento completo del Trabajo de Diploma, versión 1.6.1.
- **[Diagramas UML](docs/diagramas)**: casos de uso, actividades, clases, modelo de datos y diagramas de secuencia por proceso y módulo, exportados de Enterprise Architect.
- **[Referencia técnica y funcional](docs/referencia-tecnica.md)**: detalle de cada módulo, reglas de negocio, fórmulas de los indicadores, serialización, backups, usabilidad y multiidioma.

---

## Módulos

| Módulo | Descripción |
|--------|-------------|
| **Acceso** | Login con bloqueo progresivo tras intentos fallidos, claves de emergencia y bloqueo de sesión |
| **Administración de usuarios** | Alta, cambio de rol, reset de contraseña, archivado e historial de cambios con rollback |
| **Perfiles y permisos** | Árbol de roles y permisos (Composite), editable en tiempo real |
| **Clientes y planes** | ABM de suscriptores y planes, con vencimientos y control de límites |
| **Prendas** | Inventario con estados gestionados por State; lista de espera opcional por prenda |
| **Pedidos** | Creación, despacho, entrega y devolución de pedidos (Command) |
| **Renovación y cobro** | Renovaciones y cobros resueltos con cadenas de responsabilidad, con período de gracia |
| **Contratación (PN02)** | Alta de la contratación por Vendedor y cobro por Caja |
| **Promociones (PN03)** | Circuito de sugerencia, administración y revisión contable |
| **Inspección de devoluciones (PN04)** | Reingreso o baja de prendas devueltas |
| **Centro de decisiones comerciales (PN03)** | Análisis, recomendaciones, simulador, metas y medición de resultados |
| **Reportes** | Seis reportes de solo lectura por rol, exportables a PDF y CSV |
| **Bitácora y auditoría** | Eventos del sistema y de negocio con filtros y exportación |
| **Backup y restauración** | Copias manuales cifradas (`.wfbak`) y automáticas al iniciar sesión y cada 24 h |
| **Integridad** | Dígitos verificadores por fila y por tabla, con diagnóstico y reparación asistida |
| **Serialización** | Versiones de usuario y errores en XML o JSON |
| **Idiomas** | ABM de traducciones en la base de datos |

---

## Procesos de negocio del Trabajo de Diploma

| Proceso | Descripción | Roles |
|---------|-------------|-------|
| **PN01** Armar pedido de prendas | Valida suscripción vigente y cuenta sin prendas pendientes; la reserva de stock es atómica | Vendedor, Depósito |
| **PN02** Comercialización de la suscripción | Contratación y cobro separados: quien vende no cobra. El cobro es atómico y se compensa si la activación falla | Vendedor, Caja |
| **PN03** Inteligencia comercial y promociones | Analizar, recomendar, simular, aplicar y medir. Circuito: reporte → sugerencia → administración → aprobación contable → vigente | Gerente comercial, Administración comercial, Contabilidad |
| **PN04** Inspeccionar prendas devueltas | Reingreso sin cargo o baja con cargo por el precio de reposición | Depósito |

La especificación completa de PN03 (reglas, fórmulas, umbrales configurables y glosario de métricas) está en la [referencia técnica](docs/referencia-tecnica.md#procesos-de-negocio-del-trabajo-de-diploma-pn01-pn04).

---

## Seguridad

- Contraseñas con PBKDF2-SHA256, salt aleatorio y 100.000 iteraciones, con comparación en tiempo constante.
- Bloqueo progresivo de login (1, 5, 15 y 60 minutos).
- Dígitos verificadores (DVH por fila y DVV por tabla) actualizados en la misma transacción que cada escritura, para detectar cambios hechos por fuera de la aplicación.
- Backups manuales cifrados con AES-128 y verificados con HMAC-SHA256 (encrypt-then-MAC).
- Manejo global de excepciones: el detalle técnico queda en la bitácora y el usuario ve un mensaje genérico.

> Las cuentas de demostración del instalador tienen claves fijas, a propósito, para la defensa del trabajo. Un despliegue real debe cambiarlas. Ver la [referencia técnica](docs/referencia-tecnica.md#características-de-seguridad).
