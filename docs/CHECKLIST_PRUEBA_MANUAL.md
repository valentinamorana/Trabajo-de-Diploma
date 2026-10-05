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
- [ ] **Pedidos de venta**: cancelar pide motivo; des-cancelar funciona (y se rechaza si el cliente ya tiene otro pedido en curso).

## 1b. PN01 — Circuito completo del pedido (`vendedor` + `deposito`, en dos sesiones o alternando)

Sigue el diagrama de actividad de EA. Usar un cliente con suscripción vigente, sin pedidos en curso ni prendas sin devolver.

**Identificación y avisos (Vendedor → Nuevo pedido)**
- [ ] Buscar por DNI exacto encuentra al cliente; buscar por parte del nombre o del apellido muestra la lista de coincidencias.
- [ ] Al elegirlo se ve la ficha: plan, vencimiento, prendas en uso, último pedido, método de pago y fecha de alta.
- [ ] Con un cliente vencido, pausado o sin plan aparece el aviso de suscripción no vigente y no deja avanzar.
- [ ] Con un cliente que ya tiene un pedido en curso aparece el aviso de pedido activo con su número y estado, y no deja avanzar.

**Selección y cupo**
- [ ] El catálogo solo muestra prendas Disponibles. El detalle agrupa por prenda, talle y color, con su cantidad.
- [ ] Si se pasa del cupo, se informa el exceso, "Enviar a control de stock" queda deshabilitado y aparece "Registrar desistimiento".
- [ ] Destildar prendas hasta entrar en el cupo vuelve a habilitar el envío.
- [ ] Registrar desistimiento exige un motivo, crea el pedido "Desistido" y ofrece imprimir el aviso. Las prendas siguen Disponibles.
- [ ] Enviar a control de stock crea el pedido "En control de stock" y ofrece imprimir la planilla. Las prendas siguen **Disponibles** en Prendas.

**Control de stock (Depósito → Inventario → Control de Stock)**
- [ ] El Vendedor no ve el menú Control de Stock; Depósito y Gerente de inventario sí.
- [ ] La cola muestra el pedido. Al elegirlo, la planilla indica para cada prenda si está disponible.
- [ ] Rama de faltantes: pasar una prenda del pedido a En limpieza (Prendas → Estado) y Actualizar. La planilla la marca como no disponible y "Confirmar" se rechaza.
- [ ] "Informar faltantes" pasa el pedido a "Con faltantes" y ofrece imprimir el informe con alternativas de la misma categoría y talle.

**Faltantes (Vendedor → Pedidos de venta)**
- [ ] "Ver faltantes" muestra el informe con las alternativas.
- [ ] "Ajustar selección" abre el asistente sin la prenda faltante y con las alternativas resaltadas. "Reenviar a control de stock" devuelve el pedido a la cola de Depósito.
- [ ] Con otro pedido con faltantes, "Registrar desistimiento" pide motivo y lo deja "Desistido".

**Confirmar, separar y formalizar**
- [ ] Depósito: con todo disponible, "Confirmar prendas disponibles" y luego "Separar prendas". El pedido queda "Separado", se ofrece la constancia y las prendas pasan a **En uso**.
- [ ] Separar sin confirmar antes se rechaza.
- [ ] Vendedor: "Formalizar pedido" pasa el pedido a "Pendiente" y ofrece la confirmación del pedido. A partir de ahí no se puede ajustar ni desistir.
- [ ] El pedido formalizado aparece en Pedidos realizados para despachar. Los pedidos en control, con faltantes, separados o desistidos no aparecen ahí.
- [ ] Alertas: aparecen "pedidos esperando el control de stock", "pedidos con faltantes" y "listos para formalizar" según el estado de los pedidos (el panel de alertas es común a todos los roles).
- [ ] Historial del pedido: muestra cada paso (ENVIAR_CONTROL, INFORMAR_FALTANTES, AJUSTAR_SELECCION, CONFIRMAR_PRENDAS, SEPARAR, FORMALIZAR) y "Restaurar" sobre esos pasos se rechaza.
- [ ] Bitácora de negocio: aparecen los eventos Envío a control de stock, Informe de faltantes, Separación de prendas, Desistimiento y Venta.
- [ ] Cambiar el idioma a EN/RU/PT en Control de Stock y en el asistente: no quedan claves crudas.

## 1c. PN02 — Contratación (`vendedor`) y cobro (`caja`)

- [ ] Suscriptores → Nueva contratación: buscar por DNI. Un DNI inexistente muestra "Registrar cliente"; al registrarlo se continúa con la contratación.
- [ ] La grilla de planes muestra precio mensual y límite. "Imprimir planes" abre la vista previa.
- [ ] Elegir un plan que no alcanza para las prendas en uso informa el motivo y deja "Registrar" deshabilitado.
- [ ] Al elegir plan y modalidad se muestra el importe a abonar en Caja (con el descuento, si corresponde).
- [ ] "El cliente desiste" pide motivo y ofrece el aviso. "Registrar contratación" ofrece imprimir la orden de cobro.
- [ ] Caja: la cola muestra la contratación y su monto. Cobrar con Tarjeta muestra el comprobante y la vigencia, y ofrece imprimir el comprobante y la constancia.
- [ ] Un cliente referido: al cobrar avisa que se acreditó el beneficio al referente.
- [ ] "Intento fallido" pide motivo. Al tercero avisa que se canceló y ofrece la constancia. "Ver intentos" lista los 3.
- [ ] Vista "Resueltas": reimprimir comprobante, constancia de suscripción y constancia de cancelación.
- [ ] Clientes: dar de baja a un cliente con contratación pendiente se rechaza. Renovación de ese cliente también se rechaza.
- [ ] Editar un cliente como Vendedor no muestra plan ni vencimiento; como Administrador sí.

## 2. Caja (`caja`)

- [ ] Solo ve el menú Caja. En Contrataciones pendientes se ve el importe a cobrar (con el descuento).
- [ ] Cobrar una contratación trimestral muestra el importe de 3 meses del precio mensual y el comprobante `CMP-…`.
- [ ] Tres intentos fallidos cancelan la contratación.

## 3. Depósito (`deposito`) y Operador Logístico (`logistico`)

- [ ] Pedidos realizados: **Despachar**, **Marcar entregado** y **Registrar devolución** funcionan con ambos usuarios (antes fallaban por permisos).
- [ ] Depósito: Prendas (alta, editar, estado), Lista de espera, Control de Stock, Inspección de devolución.
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
