# Lista de comprobación manual (antes de firmar el instalador)

Las pruebas automáticas cubren la lógica, no las pantallas. Esta pasada verifica lo que ellas no ven. Usuarios y claves: `Instalador/Credenciales_Iniciales.txt`. Conviene hacerla con la base recién instalada (con datos de demo) y con el instalador nuevo.

Marcar cada punto: OK / falla (anotar qué se vio).

## 0. General (con cualquier usuario)

- [ ] El Login muestra los íconos (usuario, candado, ojo para mostrar la clave, cruz de cerrar) y "Bienvenido de nuevo".
- [ ] El menú de Analítica de Negocio y de Auditoría no muestra emojis ni íconos.
- [ ] Cambiar el idioma (ES, EN, RU, PT) cambia menús, botones y mensajes, sin textos en blanco ni claves crudas (por ejemplo `err.bll...`).
- [ ] Los botones "Actualizar" (Prendas, Clientes, Pedidos, Promociones, Lista de espera, Contrataciones, Inspección) se leen completos y no tapan otros controles ni el contador.
- [ ] Cerrar sesión y volver a entrar funciona; el panel de control abre según el rol.

## 1. Vendedor (`vendedor`)

- [ ] Menús visibles: Suscriptores, Inventario (Prendas, Lista de espera), Ventas (Pedidos de venta), Promociones (vigentes), Analítica (Recomendación). No ve Caja, Auditoría ni Administrar.
- [ ] **Clientes**: alta con "Referido por"; DNI repetido se rechaza; baja de un cliente con prendas en uso se rechaza.
- [ ] **Nueva contratación**: el combo de **modalidad** aparece con Mensual, Trimestral y Anual, sin error. Con un cliente que ya tiene una contratación pendiente, la rechaza.
- [ ] **Renovación**: el combo de modalidad funciona. Pausar: la fecha no deja pasar de 3 meses. Reanudar ahora devuelve los días no usados.
- [ ] **Cobro de suscripción**: el combo de modalidad funciona; cobrar y pago fallido muestran mensajes claros.
- [ ] **Nuevo pedido**: un cliente con prendas sin devolver se avisa al elegirlo; excede el cupo se rechaza.
- [ ] **Pedidos de venta**: cancelar pide motivo; des-cancelar funciona.

## 2. Caja (`caja`)

- [ ] Solo ve el menú Caja. En Contrataciones pendientes se ve el importe a cobrar (con el descuento).
- [ ] Cobrar una contratación trimestral muestra el importe de 3 meses del precio mensual y el comprobante `CMP-…`.
- [ ] Tres intentos fallidos cancelan la contratación.

## 3. Depósito (`deposito`) y Operador Logístico (`logistico`)

- [ ] Pedidos realizados: **Despachar**, **Marcar entregado** y **Registrar devolución** funcionan con ambos usuarios (antes fallaban por permisos).
- [ ] Depósito: Prendas (alta, editar, estado), Lista de espera, Inspección de devolución.
- [ ] Después de **Registrar devolución**, las prendas aparecen en Inspección (En limpieza) y en el historial de mantenimiento de la prenda.
- [ ] Inspección: aprobar reingreso deja la prenda Disponible; dar de baja y cobrar registra el cargo antes de la baja.
- [ ] Logístico solo ve Pedidos realizados.

## 4. Gerentes (`gcomercial`, `ginventario`)

- [ ] Gerente comercial: además de lo del Vendedor, ve Sugerir promoción, Análisis de abandono y Ventas por vendedor. Sugerir promoción desde el análisis precarga el formulario.
- [ ] Gerente de inventario: ve Rotación, Mantenimiento y Escasez, y lo de Depósito y Logístico.
- [ ] Cada reporte genera, muestra resultados y exporta a PDF y CSV.

## 5. Promociones (`admcomercial`, `contable`)

- [ ] Administración: alta desde sugerencia y manual, reformular una rechazada, desactivar, resolver bajas. Un precio promocional se interpreta como mensual.
- [ ] Contabilidad: aprobar o rechazar exige observación.
- [ ] Una promoción vigente reduce el importe en Caja (un solo descuento por cobro).

## 6. Auditor (`auditor`) y Administrador (`admin`)

- [ ] Auditor: solo Bitácoras y Reporte de jornada, sin botones de edición.
- [ ] Administrador: ve todo. Usuarios, Perfiles, Idiomas, Backup e Integridad abren y funcionan.
- [ ] Integridad: diagnóstico sin advertencias en una base recién instalada.
- [ ] Historial de un pedido: **Restaurar** funciona para quien puede editar pedidos y se rechaza para quien no.

## 7. Instalador (en otra computadora)

- [ ] Instala con SQL Server o LocalDB; crea la base con datos de demo; el acceso directo abre la aplicación.
- [ ] Ingresa con cada usuario del archivo de credenciales.
- [ ] Desinstalar no deja errores.
