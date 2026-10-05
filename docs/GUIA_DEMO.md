# Guía de la demo — PN01, PN02 y PN03

Datos que carga el script (sección 21e de `BD/00_Instalacion_Completa.sql`): en una instalación nueva, o
después de `BD/Reset_Datos_Demo.sql`. El **apellido** de cada cliente y el **nombre** de cada promoción
dicen qué rama del diagrama de actividad muestran.

**Usuarios** (`Instalador/Credenciales_Iniciales.txt`):

| Usuario | Clave | Rol | Carril |
|---|---|---|---|
| `vendedor` | `vendedor1!` | Vendedor | PN01, PN02, PN03 (pide la baja) |
| `deposito` | `deposito1!` | Depósito | PN01 (control de stock) |
| `caja` | `usuario1!` | Caja | PN02 (cobro) |
| `gcomercial` | `gcomercial1!` | Gerente comercial | PN03 (sugiere) |
| `admcomercial` | `usuario1!` | Administración comercial | PN03 (crea, reformula, resuelve bajas) |
| `contable` | `usuario1!` | Contabilidad | PN03 (dictamina) |
| `admin` | `administrador1!` | Administrador | ve todo |

Se puede abrir la aplicación dos veces (una por usuario) o alternar con **Sesión → Cerrar Sesion** (menú a
la derecha). Cada documento del proceso se imprime o se guarda como PDF con "Microsoft Print to PDF".

---

## PN01 — Armar pedido (Vendedor + Depósito)

| Cliente | DNI | Qué muestra |
|---|---|---|
| Juan PedidoFeliz | 40000001 | Camino feliz completo (Premium, sin pedidos en curso) |
| Ana PrendasNoDisponibles | 40000002 | Ya tiene un pedido **En control de stock** con el "Saco Negro M", que tiene Pedro → faltantes |
| Nicolas ExcedeCupo | 40000003 | Plan Básico (5 prendas): elegir 6 → exceso de cupo |
| Sofia ListaParaFormalizar | 40000004 | Pedido **Separado** → el Vendedor lo formaliza |
| Pedro PedidoActivo | 40000005 | Ya tiene un pedido formalizado → aviso de pedido activo |
| Lucia SuscripcionVencida | 40000006 | Vencida → aviso de suscripción no vigente |
| Tomas SuscripcionPausada | 40000007 | Pausada → aviso de suscripción no vigente |

**1. Camino feliz (Juan)**
1. `vendedor` → **Ventas → Pedidos de Venta → + Nuevo Pedido**. Buscar DNI `40000001`: se ve la ficha →
   **Siguiente →** (paso 2 de 2).
2. Tildar 2 o 3 prendas (por ejemplo "Vestido Verde M" y "Camisa Celeste S") → **Enviar a control de stock**.
   Se ofrece la planilla. El pedido queda *En control de stock*; las prendas siguen Disponibles.
3. `deposito` → **Inventario → Control de Stock**: elegir el pedido → **Confirmar prendas disponibles** →
   **Separar prendas** (se ofrece la constancia; las prendas pasan a En uso).
4. `vendedor` → **Pedidos de Venta**: elegir el pedido → **Formalizar pedido** (se ofrece la confirmación).
   Queda *Pendiente* de despacho; `deposito` lo ve en **Ventas → Pedidos Realizados** (el Vendedor no ve ese menú).

**2. Prendas no disponibles (Ana)**
1. `deposito` → **Control de Stock**: el pedido de Ana ya está en la cola. La planilla marca el
   **Saco Negro M** como *En Uso* (¿Disponible? No — lo tiene Pedro): **Confirmar prendas disponibles** queda
   deshabilitado y solo se habilita **Informar faltantes**.
2. **Informar faltantes**: el pedido queda *Con faltantes* y el informe propone **Saco Gris M** y
   **Saco Beige M** (misma categoría y talle, disponibles).
3. `vendedor` → **Pedidos de Venta** → **Ver faltantes**. Después, una de dos:
   - **Ajustar selección**: cambiar el saco por una alternativa → **Reenviar a control de stock** → Depósito
     confirma y separa → Vendedor formaliza.
   - **Registrar desistimiento** (con motivo): el pedido queda *Desistido*.

**3. Exceso de cupo (Nicolas)**: nuevo pedido con DNI `40000003`, tildar 6 prendas → se informa el exceso y
"Enviar a control de stock" se deshabilita. Destildar una lo vuelve a habilitar; o **Registrar desistimiento**
(etapa Cupo).

**4. Formalizar (Sofia)**: `vendedor` → Pedidos de Venta → el pedido de Sofia (*Separado*) → **Formalizar pedido**.

**5. Avisos**: nuevo pedido con DNI `40000005` (Pedro: pedido activo), `40000006` (Lucia: vencida) o
`40000007` (Tomas: pausada). No dejan avanzar.

---

## PN02 — Comercialización de la suscripción (Vendedor + Caja)

| Cliente | DNI | Qué muestra |
|---|---|---|
| (no existe) | 40000099 | ¿Registrado? **No** → registrar al cliente y seguir |
| Laura SinPlan | 40000008 | Contratar un plan, o desistir (con motivo) |
| Rocio ReferidaPorJuan | 40000009 | Al cobrarle, se acredita el beneficio a Juan |
| Diego PagoPendiente | 40000010 | Contratación Estándar trimestral esperando a Caja, con la promoción vigente del plan Estándar |
| Elena TercerIntentoFallido | 40000011 | Ya tiene 2 intentos fallidos: el 3.º cancela la contratación |

1. `vendedor` → **Suscriptores → Nueva Contratación**: buscar el DNI → elegir plan y modalidad (se ve el
   importe) → **Registrar contratación** (se ofrece la orden de cobro). O **El cliente desiste** (pide motivo).
2. `caja` → **Caja → Contrataciones Pendientes**:
   - **Diego**: se ve el importe con el 10% de la promoción → **Cobrar** con un medio de pago → comprobante
     y constancia; la suscripción queda activa. **Cobrarle antes** de pedir la baja de "PROMO Vigente
     Estandar -10%" en PN03: el descuento solo se aplica mientras la promoción está Vigente.
   - **Elena**: **Intento Fallido** (pide motivo) → avisa que se canceló y ofrece la constancia. Elena sale
     de la cola; en la vista **Resueltas**, elegirla → **Ver intentos** lista los 3.
   - **Rocio** (después de registrarle la contratación): al cobrar, avisa que se acreditó el beneficio a Juan.
   - Vista **Resueltas**: también está la contratación cobrada de Juan, para reimprimir el comprobante.

El plan **no** se asigna desde la ficha del cliente: queda asignado cuando Caja confirma el cobro (solo el
Administrador puede corregirlo a mano, como excepción).

---

## PN03 — Métricas, promociones y decisiones

| Objeto | Estado | Quién actúa |
|---|---|---|
| 2 sugerencias "SUGERENCIA PendienteDeEvaluar" | Pendientes | `admcomercial`: **Alta desde Sugerencia** o **Descartar sugerencia** (con motivo) |
| PROMO ParaDictaminar Premium -15% | En revisión contable | `contable`: **Aprobar y Activar** o **Rechazar** (con observación) |
| PROMO Rechazada Basico -20% | Rechazada por Contabilidad | `admcomercial`: **Reformular** (vuelve a revisión) o **Descartar** |
| PROMO Vigente Estandar -10% | Vigente | `vendedor`: **Solicitar baja**; mientras esté vigente se aplica en el cobro de Caja |
| PROMO BajaSolicitada Abrigos $3000 | Baja solicitada | `admcomercial`: **Aprobar Baja** (queda Desactivada) o **Rechazar Baja** |

1. `gcomercial` → **Promociones → Sugerir Promoción**: **Analizar métricas…** muestra el reporte (abandono y
   rotación) → **Usar esta idea** precarga los datos → **Enviar Sugerencia**.
2. `admcomercial` → **Promociones → Gestión de Promociones**: alta desde una sugerencia o **Alta Manual**,
   reformular la rechazada, resolver la baja solicitada.
3. `contable` → **Promociones → Revisión Contable**: dictaminar la "ParaDictaminar". Quien creó una
   promoción no puede dictaminarla.
4. `vendedor` → **Promociones → Promociones Vigentes**: **Solicitar baja** de la vigente (con motivo),
   después de haberle cobrado a Diego en PN02.

---

## Volver a empezar

1. Backup de la base (`admin` → **Administrar → Sistema → Backup y Restauración**, o desde SQL Server).
2. Ejecutar `BD/Reset_Datos_Demo.sql` y después `BD/00_Instalacion_Completa.sql`.
3. Abrir la aplicación (recalcula los dígitos verificadores al arrancar).
