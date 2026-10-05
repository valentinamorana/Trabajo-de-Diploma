using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio para gestión del ciclo de vida de pedidos.
    /// </summary>
    public class Pedido : Interfaces.IPedidoService
    {
        private readonly DAL.Interfaces.IPedidoDAL dalPedido;
        private readonly DAL.Interfaces.IClienteDAL dalCliente;
        private readonly DAL.Interfaces.IEmpleadoDAL dalEmpleado;
        private readonly DAL.Interfaces.IPlanSuscripcionDAL dalPlan;
        private readonly Servicios.IRegistroBitacora bitacora;
        private readonly Servicios.IRegistroBitacoraNegocio bitacoraNeg;
        private readonly DAL.Interfaces.IPedidoHistorialDAL dalHistorial;

        // Lista de Espera (mejora opcional) — composición lazy, mismo criterio que
        // BLL.Prenda.listaEsperaBLL / BLL.Usuario.perfilesBLL.
        private Interfaces.IListaEsperaService _listaEsperaLazy;
        private Interfaces.IListaEsperaService listaEsperaBLL => _listaEsperaLazy ?? (_listaEsperaLazy = new ListaEspera());

        // PN01 (split lógico Depósito) — composición lazy, mismo criterio que listaEsperaBLL:
        // BLL.Prenda es dueña de VerificarDisponibilidad, BLL.Pedido solo la consume.
        private Interfaces.IPrendaService _prendaBLLLazy;
        private Interfaces.IPrendaService prendaBLL => _prendaBLLLazy ?? (_prendaBLLLazy = new Prenda());

        // DI: el constructor por defecto usa los DAL reales; el otro permite inyectar dobles
        // de prueba (mismo criterio que BLL.Cliente/BLL.Renovacion/BLL.Cobro). Los colaboradores
        // opcionales (Lista de Espera, BLL.Prenda, bitácoras) se crean por defecto si no se pasan.
        public Pedido() : this(new DAL.Pedido(), new DAL.Cliente(), new DAL.Empleado(),
                                new DAL.PlanSuscripcion(), new DAL.PedidoHistorial()) { }

        public Pedido(DAL.Interfaces.IPedidoDAL dalPedido, DAL.Interfaces.IClienteDAL dalCliente,
                       DAL.Interfaces.IEmpleadoDAL dalEmpleado, DAL.Interfaces.IPlanSuscripcionDAL dalPlan,
                       DAL.Interfaces.IPedidoHistorialDAL dalHistorial,
                       Interfaces.IListaEsperaService listaEsperaBLL = null,
                       Interfaces.IPrendaService prendaBLL = null,
                       Servicios.IRegistroBitacora bitacora = null,
                       Servicios.IRegistroBitacoraNegocio bitacoraNegocio = null)
        {
            this.dalPedido = dalPedido ?? throw new ArgumentNullException(nameof(dalPedido));
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalEmpleado = dalEmpleado ?? throw new ArgumentNullException(nameof(dalEmpleado));
            this.dalPlan = dalPlan ?? throw new ArgumentNullException(nameof(dalPlan));
            this.dalHistorial = dalHistorial ?? throw new ArgumentNullException(nameof(dalHistorial));
            _listaEsperaLazy = listaEsperaBLL;
            _prendaBLLLazy   = prendaBLL;
            this.bitacora    = bitacora ?? Servicios.FabricaBitacora.CrearSistema();
            this.bitacoraNeg = bitacoraNegocio ?? Servicios.FabricaBitacora.CrearNegocio();
        }

        // Consultas
        public List<BE.Pedido> ObtenerTodos() => dalPedido.ObtenerTodos();
        public List<BE.Pedido> ObtenerPendientes() => dalPedido.ObtenerPendientes();
        public BE.Pedido ObtenerPorId(int id) => dalPedido.ObtenerPorId(id);

        public List<BE.Pedido> ObtenerPorEstado(BE.EstadoPedido estado) => dalPedido.ObtenerPorEstado(estado);

        // ══════════════════════════════════════════════════════════════════════
        // PN01 — Armar pedido de prendas. Cada método público corresponde a una actividad del
        // diagrama de actividad (carriles Vendedor y Depósito = "Controlador de Stock"):
        //
        //   Vendedor  Verificar la vigencia de la suscripción ......... VerificarVigencia
        //             Revisar existencia de un pedido activo ............ RevisarPedidoActivo
        //             Comprobar el cupo del plan ........................ ComprobarCupo
        //             Enviar selección para control stock .............. EnviarAControlStock
        //             Asentar desistimiento ............................ AsentarDesistimiento
        //             Recibir selección ajustada por disponibilidad .... AjustarSeleccion
        //             Formalizar el pedido ............................. FormalizarPedido
        //             Preparar la confirmación ......................... PrepararConfirmacion
        //   Depósito  Revisar stock de las prendas ..................... RevisarStock
        //             Informe de prendas faltantes (y alternativas) .... InformarFaltantes
        //             Confirmar prendas disponibles .................... ConfirmarPrendasDisponibles
        //             Separar prendas del pedido ....................... SepararPrendas
        //
        // Las prendas recién se reservan (EnUso) al separarlas; hasta ahí siguen Disponibles.
        // ══════════════════════════════════════════════════════════════════════

        // "Verificar la vigencia de la suscripción" → ¿Suscripción vigente? Vigente = existe, tiene
        // plan, no está suspendida por falta de pago, no está pausada y no venció. Si no, lanza el
        // motivo (Informar imposibilidad de continuar).
        public BE.Cliente VerificarVigencia(int idCliente)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta);
            try
            {
                return ObtenerClienteValidado(idCliente);
            }
            catch (BE.AppException ex)
            {
                // Aviso de suscripción no vigente: queda registrado aunque no se arme el pedido.
                RegistrarAviso("Aviso de suscripción no vigente", idCliente, ex);
                throw;
            }
        }

        private const string ModuloArmado = "Armar pedido";

        // Máquina de estados del diagrama (BE.Pedido.TransicionValida): ninguna operación del
        // armado puede mover el pedido por una flecha que el diagrama no tiene.
        private static void ExigirTransicion(BE.Pedido pedido, BE.EstadoPedido destino)
        {
            if (!pedido.TransicionValida(destino))
                throw new BE.AppException("err.bll.pedido.transicion_invalida",
                    "El Pedido #{0} no puede pasar de '{1}' a '{2}'.",
                    pedido.IdPedido, pedido.Estado, destino);
        }

        // Los avisos del diagrama que cortan el circuito (suscripción no vigente, pedido activo)
        // no generan pedido: se asientan en la bitácora del sistema con el motivo.
        private void RegistrarAviso(string aviso, int idCliente, BE.AppException ex)
        {
            try
            {
                bitacora.Registrar(ModuloArmado,
                    $"{aviso} — Cliente #{idCliente} — {ex.Clave}: {ex.Message}",
                    BE.Criticidad.Baja);
            }
            catch (Exception e) { System.Diagnostics.Trace.TraceError($"[BLL.Pedido] Aviso: {e.Message}"); }
        }

        // "Revisar existencia de un pedido activo" → ¿Posee pedido activo? Si lo tiene, lanza el
        // motivo (Informar existencia de pedido activo). Un pedido está activo mientras no terminó
        // su ciclo (en control de stock, con faltantes, separado, formalizado o despachado). Además,
        // igual que NUULY (4.2/4.6), la cuenta sigue bloqueada mientras el cliente tenga prendas en
        // uso sin devolver (pedido Entregado): PN04 (RegistrarDevolucion) la desbloquea.
        public void RevisarPedidoActivo(BE.Cliente cliente)
        {
            try
            {
                RevisarPedidoActivo(cliente, null);
            }
            catch (BE.AppException ex) when (ex.Clave != "err.bll.sin_permiso" && ex.Clave != "err.bll.sesion_expirada")
            {
                // Aviso de pedido activo.
                RegistrarAviso("Aviso de pedido activo", cliente.IdCliente, ex);
                throw;
            }
        }

        private void RevisarPedidoActivo(BE.Cliente cliente, int? idPedidoExcluido)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta);

            var activos = ObtenerTodos().FindAll(p =>
                p.IdCliente == cliente.IdCliente && p.EsActivo() && p.IdPedido != idPedidoExcluido);

            var despachado = activos.Find(p => p.Estado == BE.EstadoPedido.Despachado);
            if (despachado != null)
                throw new BE.AppException("err.bll.pedido.ya_despachado",
                    "El cliente tiene el pedido #{0} despachado pendiente de entrega. " +
                    "Confirmá la entrega antes de crear uno nuevo.",
                    despachado.IdPedido);

            if (activos.Count > 0)
                throw new BE.AppException("err.bll.pedido.pedido_activo",
                    "{0} ya tiene el pedido #{1} en curso (estado: {2}). No se puede armar otro " +
                    "pedido hasta que ese termine su ciclo.",
                    cliente.NombreCompleto, activos[0].IdPedido, activos[0].Estado);

            int enUso = prendaBLL.ObtenerPorCliente(cliente.IdCliente).Count;
            if (enUso > 0)
                throw new BE.AppException("err.bll.pedido.cuenta_bloqueada",
                    "{0} tiene {1} prenda(s) pendientes de devolución. La cuenta se desbloquea cuando " +
                    "se registra la devolución del pedido anterior.",
                    cliente.NombreCompleto, enUso);
        }

        // Paso 1 del asistente: las dos verificaciones del cliente, en el orden del diagrama.
        // Público para que la GUI avise al elegir el cliente y no recién al enviar la selección.
        public BE.Cliente ValidarPuedeArmarPedido(int idCliente)
        {
            var cliente = VerificarVigencia(idCliente);
            RevisarPedidoActivo(cliente);
            return cliente;
        }

        // "Enviar selección para control stock" (Planilla de control de existencias + detalle de la
        // selección dentro del cupo). Revalida todo lo anterior, registra el pedido en estado
        // EnControlStock SIN reservar prendas y lo deja en la cola de Depósito. Devuelve el ID.
        public int EnviarAControlStock(string modulo, int idCliente, List<BE.Prenda> prendas)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta);
            ValidarParametrosEntrada(prendas);

            var cliente = ValidarPuedeArmarPedido(idCliente);
            var plan    = ComprobarCupo(cliente, prendas.Count);
            int idEmpleado = BLLHelper.ResolverEmpleadoActivo(dalEmpleado);

            var ahora = DateTime.Now;
            var pedido = new BE.Pedido
            {
                IdCliente         = idCliente,
                IdEmpleado        = idEmpleado,
                Estado            = BE.EstadoPedido.EnControlStock,
                FechaPedido       = ahora,
                FechaEnvioControl = ahora,
                Prendas           = prendas
            };
            int idNuevo = dalPedido.AltaSinReserva(pedido);

            RegistrarHistorial(idNuevo, "ENVIAR_CONTROL", new List<(string, string, string)>
            {
                ("Estado",            null, BE.EstadoPedido.EnControlStock.ToString()),
                ("FechaPedido",       null, ahora.ToString("yyyy-MM-dd HH:mm:ss")),
                ("Prendas",           null, IdsDe(prendas))
            });

            bitacora.Registrar(modulo,
                $"Enviar a control de stock Pedido #{idNuevo} — Cliente: {cliente.NombreCompleto} — " +
                $"{prendas.Count} prenda(s) — Plan: {plan.Nombre}",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.EnvioControlStock,
                $"Pedido #{idNuevo} enviado a control de stock — {cliente.NombreCompleto} — {prendas.Count} prenda(s)",
                idPedido:  idNuevo,
                idCliente: idCliente);

            return idNuevo;
        }

        // "Asentar desistimiento" cuando la selección excede el cupo del plan y el cliente no la
        // ajusta (todavía no hay pedido): registra el pedido Desistido con la selección, sin
        // reservar nada (Aviso de desistimiento). Solo procede si la selección efectivamente excede
        // el cupo: es la única rama del diagrama que llega acá. Devuelve el ID.
        public int AsentarDesistimiento(string modulo, int idCliente, List<BE.Prenda> prendas, string motivo)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta);
            ValidarParametrosEntrada(prendas);
            ValidarMotivoDesistimiento(motivo);

            // Se llega acá después de "¿Suscripción vigente? Sí" y "¿Posee pedido activo? No".
            var cliente = ValidarPuedeArmarPedido(idCliente);
            if (cliente.PuedeSolicitarPrendas(prendas.Count))
                throw new BE.AppException("err.bll.pedido.desistimiento_sin_exceso",
                    "La selección está dentro del cupo del plan: el desistimiento por cupo solo se " +
                    "asienta cuando la selección lo excede.");

            int idEmpleado = BLLHelper.ResolverEmpleadoActivo(dalEmpleado);
            var pedido = new BE.Pedido
            {
                IdCliente           = idCliente,
                IdEmpleado          = idEmpleado,
                Estado              = BE.EstadoPedido.Desistido,
                FechaPedido         = DateTime.Now,
                MotivoDesistimiento = motivo.Trim(),
                EtapaDesistimiento  = BE.EtapaDesistimiento.Cupo,
                Prendas             = prendas
            };
            int idNuevo = dalPedido.AltaSinReserva(pedido);

            RegistrarHistorial(idNuevo, "DESISTIR", new List<(string, string, string)>
            {
                ("Estado",              null, BE.EstadoPedido.Desistido.ToString()),
                ("EtapaDesistimiento",  null, BE.EtapaDesistimiento.Cupo.ToString()),
                ("MotivoDesistimiento", null, motivo.Trim()),
                ("Prendas",             null, IdsDe(prendas))
            });
            LogDesistimiento(modulo, idNuevo, cliente.IdCliente, cliente.NombreCompleto,
                             BE.EtapaDesistimiento.Cupo, motivo.Trim());
            return idNuevo;
        }

        // "Asentar desistimiento" de un pedido con faltantes informados:
        //   • etapa Disponibilidad: el cliente no ajusta la selección (¿Ajustar selección? No);
        //   • etapa Cupo: al ajustarla excede el cupo y no la corrige (¿Desea ajustar? No). En ese
        //     caso se pasa la selección ajustada: tiene que exceder el cupo y queda guardada como
        //     la selección desistida (es la que figura en el Aviso de desistimiento).
        // No hay prendas que liberar: nunca se reservaron.
        public void AsentarDesistimiento(string modulo, BE.Pedido pedido, string motivo,
                                         BE.EtapaDesistimiento etapa, List<BE.Prenda> seleccionAjustada = null)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta);
            if (!pedido.PuedeDesistirse())
                throw new BE.AppException("err.bll.pedido.desistir_estado",
                    "Solo se asienta el desistimiento de un pedido con faltantes informados. Este pedido está '{0}'.",
                    pedido.Estado);
            ExigirTransicion(pedido, BE.EstadoPedido.Desistido);
            ValidarMotivoDesistimiento(motivo);

            if (etapa == BE.EtapaDesistimiento.Cupo)
            {
                ValidarParametrosEntrada(seleccionAjustada);
                var cliente = dalCliente.ObtenerPorId(pedido.IdCliente);
                if (cliente == null || cliente.PuedeSolicitarPrendas(seleccionAjustada.Count))
                    throw new BE.AppException("err.bll.pedido.desistimiento_sin_exceso",
                        "La selección está dentro del cupo del plan: el desistimiento por cupo solo se " +
                        "asienta cuando la selección lo excede.");
            }
            else
            {
                seleccionAjustada = null;   // por disponibilidad se conserva la selección informada
            }

            dalPedido.RegistrarDesistimiento(pedido.IdPedido, motivo.Trim(), etapa, seleccionAjustada);

            var cambios = new List<(string, string, string)>
            {
                ("Estado",              pedido.Estado.ToString(), BE.EstadoPedido.Desistido.ToString()),
                ("EtapaDesistimiento",  null,                     etapa.ToString()),
                ("MotivoDesistimiento", null,                     motivo.Trim())
            };
            if (seleccionAjustada != null)
                cambios.Add(("Prendas", IdsDe(pedido.Prendas), IdsDe(seleccionAjustada)));
            RegistrarHistorial(pedido.IdPedido, "DESISTIR", cambios);
            LogDesistimiento(modulo, pedido.IdPedido, pedido.IdCliente, pedido.NombreCliente, etapa, motivo.Trim());
        }

        // "Recibir selección ajustada por disponibilidad": el cliente ajustó la selección después
        // del informe de faltantes. Vuelve al punto de unión del diagrama: se comprueba otra vez el
        // cupo y la selección vuelve a control de stock (EnControlStock), con el informe anterior
        // descartado.
        public void AjustarSeleccion(string modulo, BE.Pedido pedido, List<BE.Prenda> prendas)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta);
            if (!pedido.PuedeAjustarse())
                throw new BE.AppException("err.bll.pedido.ajustar_estado",
                    "Solo se ajusta la selección de un pedido con faltantes informados. Este pedido está '{0}'.",
                    pedido.Estado);
            ExigirTransicion(pedido, BE.EstadoPedido.EnControlStock);
            ValidarParametrosEntrada(prendas);

            // El flujo vuelve al punto de unión anterior a "Anotar la selección ∥ Comprobar el
            // cupo": se comprueba solo el cupo (la vigencia y el pedido activo ya se verificaron
            // al armar el pedido, que sigue siendo el mismo).
            var cliente = dalCliente.ObtenerPorId(pedido.IdCliente)
                ?? throw new BE.AppException("err.bll.pedido.cliente_inexistente", "El cliente seleccionado no existe.");
            ComprobarCupo(cliente, prendas.Count);

            dalPedido.ReemplazarSeleccion(pedido.IdPedido, prendas);

            RegistrarHistorial(pedido.IdPedido, "AJUSTAR_SELECCION", new List<(string, string, string)>
            {
                ("Estado",  pedido.Estado.ToString(), BE.EstadoPedido.EnControlStock.ToString()),
                ("Prendas", IdsDe(pedido.Prendas),    IdsDe(prendas))
            });

            bitacora.Registrar(modulo,
                $"Ajustar selección Pedido #{pedido.IdPedido} — Cliente: {cliente.NombreCompleto} — " +
                $"{prendas.Count} prenda(s) — vuelve a control de stock",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.EnvioControlStock,
                $"Pedido #{pedido.IdPedido} reenviado a control de stock con la selección ajustada — " +
                $"{cliente.NombreCompleto} — {prendas.Count} prenda(s)",
                idPedido:  pedido.IdPedido,
                idCliente: pedido.IdCliente);
        }

        // "Revisar stock de las prendas" (Depósito) → ¿Selección disponible? Relee el estado real de
        // cada prenda de la planilla y si quedó reservada por Lista de Espera para otro cliente.
        // Solo lectura.
        public List<BE.LineaControlStock> RevisarStock(BE.Pedido pedido)
        {
            PermisosAccion.Exigir(BE.Patentes.ControlStockEditar, BE.Patentes.ControlStock);
            if (!pedido.PuedeControlarse())
                throw new BE.AppException("err.bll.pedido.control_estado",
                    "Solo se revisa el stock de un pedido enviado a control. Este pedido está '{0}'.",
                    pedido.Estado);

            var (_, noDisponibles) = prendaBLL.VerificarDisponibilidad(pedido.Prendas);

            var lineas = new List<BE.LineaControlStock>();
            foreach (var p in pedido.Prendas)
            {
                var actual = noDisponibles.Find(n => n.IdPrenda == p.IdPrenda);
                var linea = new BE.LineaControlStock
                {
                    Prenda       = p,
                    EstadoActual = actual?.Estado ?? BE.EstadoPrenda.Disponible,
                    Confirmada   = pedido.PrendasConfirmadas.Contains(p.IdPrenda)
                };
                if (linea.EstadoActual == BE.EstadoPrenda.Disponible)
                    linea.ReservadaParaOtro = EstaReservadaParaOtro(p.IdPrenda, pedido.IdCliente);
                lineas.Add(linea);
            }
            return lineas;
        }

        // "Informe de prendas faltantes" (Informe de disponibilidad: prendas faltantes y
        // alternativas). Para cada prenda no disponible el sistema propone alternativas
        // Disponibles de la misma categoría y talle. El pedido pasa a ConFaltantes y el Vendedor
        // comunica los faltantes al cliente. Devuelve el informe.
        public List<BE.PedidoFaltante> InformarFaltantes(string modulo, BE.Pedido pedido)
        {
            var lineas = RevisarStock(pedido);   // exige permiso y estado EnControlStock

            var faltantes = lineas.Where(l => !l.Disponible).Select(l => new BE.PedidoFaltante
            {
                IdPedido          = pedido.IdPedido,
                Prenda            = l.Prenda,
                EstadoAlRevisar   = l.EstadoActual,
                ReservadaParaOtro = l.ReservadaParaOtro,
                Alternativas      = SugerirAlternativas(l.Prenda, pedido)
            }).ToList();

            if (faltantes.Count == 0)
                throw new BE.AppException("err.bll.pedido.sin_faltantes",
                    "Todas las prendas del Pedido #{0} están disponibles: confirmalas en lugar de informar faltantes.",
                    pedido.IdPedido);
            ExigirTransicion(pedido, BE.EstadoPedido.ConFaltantes);

            int idEmpleado = BLLHelper.ResolverEmpleadoActivo(dalEmpleado);
            dalPedido.RegistrarFaltantes(pedido.IdPedido, idEmpleado, faltantes);

            RegistrarHistorial(pedido.IdPedido, "INFORMAR_FALTANTES", new List<(string, string, string)>
            {
                ("Estado",     pedido.Estado.ToString(), BE.EstadoPedido.ConFaltantes.ToString()),
                ("Faltantes",  null,                     IdsDe(faltantes.Select(f => f.Prenda).ToList()))
            });

            bitacora.Registrar(modulo,
                $"Informe de faltantes Pedido #{pedido.IdPedido} — Cliente: {pedido.NombreCliente} — " +
                $"{faltantes.Count} prenda(s) no disponible(s)",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.InformeFaltantes,
                $"Pedido #{pedido.IdPedido}: {faltantes.Count} prenda(s) faltante(s) — " +
                string.Join(", ", faltantes.Select(f => f.Prenda.Nombre)),
                idPedido:  pedido.IdPedido,
                idCliente: pedido.IdCliente);

            return faltantes;
        }

        // "Confirmar prendas disponibles" (Detalle de prendas confirmadas): Depósito confirma
        // que toda la selección está disponible. El pedido sigue en control hasta separarla.
        public void ConfirmarPrendasDisponibles(string modulo, BE.Pedido pedido)
        {
            var lineas = RevisarStock(pedido);   // exige permiso y estado EnControlStock

            var faltante = lineas.Find(l => !l.Disponible);
            if (faltante != null)
                throw new BE.AppException("err.bll.pedido.hay_faltantes",
                    "La prenda '{0}' no está disponible: informá los faltantes en lugar de confirmar.",
                    faltante.Prenda.Nombre);

            int idEmpleado = BLLHelper.ResolverEmpleadoActivo(dalEmpleado);
            dalPedido.ConfirmarPrendas(pedido.IdPedido, idEmpleado);

            RegistrarHistorial(pedido.IdPedido, "CONFIRMAR_PRENDAS", new List<(string, string, string)>
            {
                ("PrendasConfirmadas", null, IdsDe(pedido.Prendas))
            });

            bitacora.Registrar(modulo,
                $"Confirmar prendas disponibles Pedido #{pedido.IdPedido} — Cliente: {pedido.NombreCliente} — " +
                $"{pedido.CantidadPrendas} prenda(s)",
                BE.Criticidad.Baja);
        }

        // "Separar prendas del pedido" (Constancia de prendas separadas): reserva las prendas
        // (EnUso a nombre del cliente) y el pedido pasa a Separado. Si al separar alguna prenda ya
        // no está disponible (otra operación la tomó desde la revisión), no se reserva nada y se
        // vuelve a la decisión "¿Selección disponible? → No": se emite el informe de faltantes.
        public void SepararPrendas(string modulo, BE.Pedido pedido)
        {
            PermisosAccion.Exigir(BE.Patentes.ControlStockEditar, BE.Patentes.ControlStock);
            if (!pedido.PuedeControlarse())
                throw new BE.AppException("err.bll.pedido.control_estado",
                    "Solo se revisa el stock de un pedido enviado a control. Este pedido está '{0}'.",
                    pedido.Estado);
            if (!pedido.PuedeSepararse())
                throw new BE.AppException("err.bll.pedido.sin_confirmar",
                    "Confirmá las prendas disponibles del Pedido #{0} antes de separarlas.",
                    pedido.IdPedido);
            ExigirTransicion(pedido, BE.EstadoPedido.Separado);
            // El cliente pudo pausar, vencer o quedar suspendido mientras el pedido esperaba en la
            // cola de Depósito: se revalida antes de reservar las prendas.
            ObtenerClienteValidado(pedido.IdCliente);

            bool todasDisponibles = RevisarStock(pedido).TrueForAll(l => l.Disponible);
            if (todasDisponibles)
            {
                try
                {
                    dalPedido.SepararPrendas(pedido.IdPedido, pedido.IdCliente);
                }
                catch (BE.AppException ex) when (ex.Clave == "err.dal.pedido.prenda_tomada")
                {
                    todasDisponibles = false;
                }
            }

            if (!todasDisponibles)
            {
                InformarFaltantes(modulo, dalPedido.ObtenerPorId(pedido.IdPedido) ?? pedido);
                throw new BE.AppException("err.bll.pedido.separar_faltantes",
                    "Al separar, una o más prendas del Pedido #{0} ya no estaban disponibles. No se " +
                    "reservó nada y se emitió el informe de faltantes para el Vendedor.",
                    pedido.IdPedido);
            }

            RegistrarHistorial(pedido.IdPedido, "SEPARAR", new List<(string, string, string)>
            {
                ("Estado",          pedido.Estado.ToString(), BE.EstadoPedido.Separado.ToString()),
                ("FechaSeparacion", null,                     DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            });

            // Lista de Espera (mejora opcional): si alguna prenda separada estaba reservada para
            // este cliente, cierra el ciclo (Convertida).
            string actorEspera = Seguridad.SessionManager.IsLoggedIn
                ? Seguridad.SessionManager.GetInstance().Usuario.Username : null;
            foreach (var p in pedido.Prendas)
            {
                try { listaEsperaBLL.CerrarSiReservada(modulo, p.IdPrenda, pedido.IdCliente, actorEspera); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceError($"[BLL.Pedido] Lista de Espera: {ex.Message}"); }
            }

            bitacora.Registrar(modulo,
                $"Separar prendas Pedido #{pedido.IdPedido} — Cliente: {pedido.NombreCliente} — " +
                $"{pedido.CantidadPrendas} prenda(s) reservadas",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.SeparacionPrendas,
                $"Pedido #{pedido.IdPedido}: {pedido.CantidadPrendas} prenda(s) separadas para {pedido.NombreCliente}",
                idPedido:  pedido.IdPedido,
                idCliente: pedido.IdCliente);
        }

        // "Formalizar el pedido": punto de cierre. Desde acá la selección queda formalizada y no
        // admite modificaciones; el pedido queda Pendiente de despacho (ciclo logístico).
        public void FormalizarPedido(string modulo, BE.Pedido pedido)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta);
            if (!pedido.PuedeFormalizarse())
                throw new BE.AppException("err.bll.pedido.formalizar_estado",
                    "Solo se formaliza un pedido con las prendas ya separadas por Depósito. Este pedido está '{0}'.",
                    pedido.Estado);
            ExigirTransicion(pedido, BE.EstadoPedido.Pendiente);
            // Se revalida la vigencia del cliente al cerrar el pedido (pudo cambiar desde el armado).
            ObtenerClienteValidado(pedido.IdCliente);

            dalPedido.Formalizar(pedido.IdPedido);

            var ahora = DateTime.Now;
            RegistrarHistorial(pedido.IdPedido, "FORMALIZAR", new List<(string, string, string)>
            {
                ("Estado",             pedido.Estado.ToString(), BE.EstadoPedido.Pendiente.ToString()),
                ("FechaFormalizacion", null,                     ahora.ToString("yyyy-MM-dd HH:mm:ss"))
            });

            var cliente = dalCliente.ObtenerPorId(pedido.IdCliente);
            var plan    = cliente?.IdPlan != null ? dalPlan.ObtenerPorId(cliente.IdPlan.Value) : null;
            bitacora.Registrar(modulo,
                $"Formalizar Pedido #{pedido.IdPedido} — Cliente: {pedido.NombreCliente} — " +
                $"{pedido.CantidadPrendas} prenda(s) — Plan: {plan?.Nombre ?? "—"}",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.Venta,
                $"Pedido #{pedido.IdPedido} — {pedido.NombreCliente} — {pedido.CantidadPrendas} prenda(s) — " +
                $"Plan {plan?.Nombre ?? "—"} — {ahora:dd/MM/yyyy HH:mm}",
                idPedido:  pedido.IdPedido,
                idCliente: pedido.IdCliente);
        }

        // "Preparar la confirmación" (Confirmación y constancia del pedido): devuelve el pedido
        // formalizado completo para emitir la constancia que recibe el cliente.
        public BE.Pedido PrepararConfirmacion(string modulo, int idPedido)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta);
            var pedido = dalPedido.ObtenerPorId(idPedido);
            if (pedido == null)
                throw new BE.AppException("err.bll.pedido.inexistente", "El Pedido #{0} no existe.", idPedido);
            if (!pedido.EstaFormalizado())
                throw new BE.AppException("err.bll.pedido.confirmacion_estado",
                    "Solo se prepara la confirmación de un pedido formalizado. Este pedido está '{0}'.",
                    pedido.Estado);

            bitacora.Registrar(modulo,
                $"Confirmación del Pedido #{pedido.IdPedido} emitida — Cliente: {pedido.NombreCliente}",
                BE.Criticidad.Baja);
            return pedido;
        }

        // Cola de Depósito: pedidos enviados a control de stock, del más antiguo al más nuevo.
        public List<BE.Pedido> ObtenerColaControlStock() => dalPedido.ObtenerPorEstado(BE.EstadoPedido.EnControlStock);

        // Informe de faltantes y alternativas de un pedido (para "Comunicar faltantes").
        public List<BE.PedidoFaltante> ObtenerInformeFaltantes(int idPedido) => dalPedido.ObtenerFaltantes(idPedido);

        // Alternativas para una prenda faltante: Disponibles (sin reserva de Lista de Espera para
        // otro cliente), de la misma categoría y talle, que no estén ya en el pedido. Hasta 3.
        private List<BE.Prenda> SugerirAlternativas(BE.Prenda faltante, BE.Pedido pedido)
        {
            var enPedido = new HashSet<int>(pedido.Prendas.Select(p => p.IdPrenda));
            return prendaBLL.ObtenerDisponibles(pedido.IdCliente)
                .Where(p => !enPedido.Contains(p.IdPrenda)
                         && MismoValor(p.Categoria, faltante.Categoria)
                         && MismoValor(p.Talle,     faltante.Talle))
                .Take(MaxAlternativasPorFaltante)
                .ToList();
        }

        private const int MaxAlternativasPorFaltante = 3;

        private static bool MismoValor(string a, string b) =>
            string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

        private bool EstaReservadaParaOtro(int idPrenda, int idCliente)
        {
            try { return listaEsperaBLL.EstaReservadaParaOtro(idPrenda, idCliente); }
            catch (Exception ex)
            {
                // BD sin migrar (tabla ListaEspera inexistente) → no bloquea, pero se loguea.
                System.Diagnostics.Trace.TraceWarning($"[BLL.Pedido] Lista de Espera (validación de reserva): {ex.Message}");
                return false;
            }
        }

        private static void ValidarMotivoDesistimiento(string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.pedido.desistir_sin_motivo",
                    "Es obligatorio indicar el motivo del desistimiento que comunicó el cliente.");
            ValidarLargoMotivo(motivo);
        }

        // Pedido.MotivoCancelacion y Pedido.MotivoDesistimiento son NVARCHAR(500)
        // (BD/00_Instalacion_Completa.sql): un texto más largo haría fallar la escritura.
        public const int LargoMaximoMotivo = 500;

        private static void ValidarLargoMotivo(string motivo)
        {
            if (motivo != null && motivo.Trim().Length > LargoMaximoMotivo)
                throw new BE.AppException("err.bll.pedido.motivo_largo",
                    "El motivo no puede superar los {0} caracteres.", LargoMaximoMotivo);
        }

        private static string IdsDe(List<BE.Prenda> prendas) =>
            prendas == null ? null : string.Join(",", prendas.Select(p => p.IdPrenda).OrderBy(id => id));

        private void LogDesistimiento(string modulo, int idPedido, int idCliente, string nombreCliente,
                                      BE.EtapaDesistimiento etapa, string motivo)
        {
            bitacora.Registrar(modulo,
                $"Asentar desistimiento Pedido #{idPedido} — Cliente: {nombreCliente} — Etapa: {etapa} — Motivo: {motivo}",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.Desistimiento,
                $"Pedido #{idPedido}: el cliente {nombreCliente} desistió ({etapa}) — {motivo}",
                idPedido:  idPedido,
                idCliente: idCliente);
        }

        // Despachar
        // Marca el pedido como Despachado.
        public void Despachar(string modulo, BE.Pedido pedido)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosRealizadosEditar, BE.Patentes.PedidosRealizados);
            if (!pedido.PuedeDespachar())
                throw new BE.AppException("err.bll.pedido.despachar_estado",
                    "Solo se pueden despachar pedidos Pendientes. Este pedido está '{0}'.",
                    pedido.Estado);

            dalPedido.Despachar(pedido.IdPedido);

            RegistrarHistorial(pedido.IdPedido, "DESPACHAR", new List<(string, string, string)>
            {
                ("Estado",       pedido.Estado.ToString(),                          BE.EstadoPedido.Despachado.ToString()),
                ("FechaDespacho", pedido.FechaDespacho?.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            });

            bitacora.Registrar(modulo,
                $"Despachar Pedido #{pedido.IdPedido} — Cliente: {pedido.NombreCliente} — " +
                $"{pedido.CantidadPrendas} prenda(s)",
                BE.Criticidad.Media);

            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.Despacho,
                $"Pedido #{pedido.IdPedido} despachado — Cliente: {pedido.NombreCliente} — " +
                $"{pedido.CantidadPrendas} prenda(s)",
                idPedido:  pedido.IdPedido,
                idCliente: pedido.IdCliente);
        }

        // Marcar Entregado
        // Marca el pedido como Entregado.
        public void MarcarEntregado(string modulo, BE.Pedido pedido)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosRealizadosEditar, BE.Patentes.PedidosRealizados);
            if (!pedido.PuedeEntregarse())
                throw new BE.AppException("err.bll.pedido.entregar_estado",
                    "Solo se pueden marcar como entregados los pedidos Despachados. Este pedido está '{0}'.",
                    pedido.Estado);

            dalPedido.MarcarEntregado(pedido.IdPedido);

            RegistrarHistorial(pedido.IdPedido, "ENTREGAR", new List<(string, string, string)>
            {
                ("Estado",      pedido.Estado.ToString(),                         BE.EstadoPedido.Entregado.ToString()),
                ("FechaEntrega", pedido.FechaEntrega?.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            });

            bitacora.Registrar(modulo,
                $"Entrega Pedido #{pedido.IdPedido} — Cliente: {pedido.NombreCliente}",
                BE.Criticidad.Baja);

            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.Entrega,
                $"Pedido #{pedido.IdPedido} entregado — Cliente: {pedido.NombreCliente}",
                idPedido:  pedido.IdPedido,
                idCliente: pedido.IdCliente);
        }

        // Registrar Devolución
        // Registra la devolución de prendas de un pedido Entregado.
        public void RegistrarDevolucion(string modulo, BE.Pedido pedido)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosRealizadosEditar, BE.Patentes.PedidosRealizados);
            if (pedido.Estado != BE.EstadoPedido.Entregado)
                throw new BE.AppException("err.bll.pedido.devolucion_estado",
                    "Solo se puede registrar la devolución de pedidos ya Entregados. Este pedido está '{0}'.",
                    pedido.Estado);

            int devueltas = dalPedido.RegistrarDevolucion(pedido.IdPedido, pedido.IdCliente);
            if (devueltas == 0)
                throw new BE.AppException("err.bll.pedido.devolucion_ya_hecha",
                    "El Pedido #{0} no tiene prendas en uso para devolver " +
                    "(es posible que la devolución ya se haya registrado).",
                    pedido.IdPedido);

            RegistrarHistorial(pedido.IdPedido, "DEVOLUCION", new List<(string, string, string)>
            {
                ("Prendas", "EnUso", $"EnLimpieza ({devueltas} prenda(s))")
            });

            bitacora.Registrar(modulo,
                $"Devolución Pedido #{pedido.IdPedido} — Cliente: {pedido.NombreCliente} — " +
                $"{devueltas} prenda(s) devuelta(s)",
                BE.Criticidad.Baja);

            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.Devolucion,
                $"Devolución pedido #{pedido.IdPedido} — Cliente: {pedido.NombreCliente} — " +
                $"{devueltas} prenda(s) pasan a EnLimpieza",
                idPedido:  pedido.IdPedido,
                idCliente: pedido.IdCliente);
        }

        // Cancelar
        // Cancela un pedido formalizado (Pendiente) o con las prendas ya separadas por Depósito
        // (Separado: "Cancelar pedido separado", su única salida además de formalizar). Requiere
        // motivo. Libera las prendas que el pedido tenía reservadas.
        public void Cancelar(string modulo, BE.Pedido pedido, string motivo)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta);
            if (!pedido.PuedeCancelarse())
                throw new BE.AppException("err.bll.pedido.cancelar_estado_separado",
                    "Solo se pueden cancelar pedidos Pendientes o con las prendas separadas. Este pedido está '{0}'.",
                    pedido.Estado);
            ExigirTransicion(pedido, BE.EstadoPedido.Cancelado);

            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.pedido.cancelar_sin_motivo",
                    "Es obligatorio ingresar un motivo de cancelación.");
            ValidarLargoMotivo(motivo);

            bool separado = pedido.Estado == BE.EstadoPedido.Separado;
            dalPedido.Cancelar(pedido.IdPedido, pedido.IdCliente, pedido.Estado, motivo.Trim());

            RegistrarHistorial(pedido.IdPedido, "CANCELAR", new List<(string, string, string)>
            {
                ("Estado",             pedido.Estado.ToString(),            BE.EstadoPedido.Cancelado.ToString()),
                ("MotivoCancelacion",  pedido.MotivoCancelacion,           motivo.Trim())
            });

            string accion = separado ? "Cancelar pedido separado" : "Cancelar";
            bitacora.Registrar(modulo,
                $"{accion} Pedido #{pedido.IdPedido} — Cliente: {pedido.NombreCliente} — Motivo: {motivo.Trim()}",
                BE.Criticidad.Media);

            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.Cancelacion,
                $"Pedido #{pedido.IdPedido} cancelado{(separado ? " (con las prendas separadas, liberadas)" : "")} — " +
                $"Cliente: {pedido.NombreCliente} — Motivo: {motivo.Trim()}",
                idPedido:  pedido.IdPedido,
                idCliente: pedido.IdCliente);
        }

        // Des-Cancelar
        // Revierte la cancelación si todas las prendas siguen Disponibles.
        public void DesCancelar(string modulo, BE.Pedido pedido)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta);
            if (!pedido.PuedeDesCancelarse())
                throw new BE.AppException("err.bll.pedido.descancelar_estado",
                    "Solo se pueden des-cancelar pedidos Cancelados. Este pedido está '{0}'.",
                    pedido.Estado);

            ExigirTransicion(pedido, BE.EstadoPedido.EnControlStock);

            // Reactivar = volver a "Enviar selección para control stock": se exigen las mismas
            // verificaciones del carril Vendedor (vigencia, sin otro pedido activo ni prendas sin
            // devolver, y cupo del plan). No se reserva nada: Depósito revisa el stock de nuevo.
            var cliente = VerificarVigencia(pedido.IdCliente);
            RevisarPedidoActivo(cliente, pedido.IdPedido);
            var completo = dalPedido.ObtenerPorId(pedido.IdPedido) ?? pedido;
            ComprobarCupo(cliente, completo.CantidadPrendas);

            if (!dalPedido.DesCancelar(pedido.IdPedido, pedido.IdCliente))
                throw new BE.AppException("err.bll.pedido.estado_cambiado",
                    "El Pedido #{0} cambió de estado en otra sesión. Actualizá la lista y volvé a intentarlo.",
                    pedido.IdPedido);

            RegistrarHistorial(pedido.IdPedido, "DESCANCELAR", new List<(string, string, string)>
            {
                ("Estado",            pedido.Estado.ToString(),  BE.EstadoPedido.EnControlStock.ToString()),
                ("MotivoCancelacion", pedido.MotivoCancelacion, null)
            });

            bitacora.Registrar(modulo,
                $"Reactivar Pedido #{pedido.IdPedido} — vuelve a control de stock — Cliente: {pedido.NombreCliente}",
                BE.Criticidad.Media);

            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.Reactivacion,
                $"Pedido #{pedido.IdPedido} reactivado — vuelve a control de stock — Cliente: {pedido.NombreCliente}",
                idPedido:  pedido.IdPedido,
                idCliente: pedido.IdCliente);
        }

        // Valida que la lista de prendas no sea nula ni vacía.
        private void ValidarParametrosEntrada(List<BE.Prenda> prendas)
        {
            if (prendas == null || prendas.Count == 0)
                throw new BE.AppException("err.bll.pedido.sin_prendas",
                    "Debe seleccionar al menos una prenda.");
        }

        // Busca el cliente y verifica que exista y tenga plan asignado.
        private BE.Cliente ObtenerClienteValidado(int idCliente)
        {
            var cliente = dalCliente.ObtenerPorId(idCliente);

            if (cliente == null)
                throw new BE.AppException("err.bll.pedido.cliente_inexistente",
                    "El cliente seleccionado no existe.");

            if (!cliente.TienePlan())
                throw new BE.AppException("err.bll.pedido.sin_plan",
                    "El cliente {0} no tiene plan asignado. Asignale un plan antes de crear un pedido.",
                    cliente.NombreCompleto);

            // PdN6 — venció el período de gracia sin regularizar el cobro (BLL.Manejadores.
            // SuspenderHandler): se bloquean nuevos pedidos hasta que se registre un cobro exitoso.
            // Va ANTES que el chequeo de vencimiento genérico de abajo a propósito: un cliente
            // suspendido por pago casi siempre tiene también FechaVencimiento en el pasado (la
            // gracia solo se abre sobre una suscripción ya vencida/por vencer — DetectarCobroHandler),
            // así que si el chequeo genérico fuera primero, este nunca se alcanzaría en la práctica
            // y el usuario vería "Renovar en el módulo de Clientes" en vez del motivo real (pago).
            if (cliente.EstaSuspendidoPorPago)
                throw new BE.AppException("err.bll.pedido.pago_suspendido",
                    "La suscripción de {0} está suspendida por falta de pago desde el {1}. " +
                    "Regularizar el cobro en el módulo de Suscriptores antes de generar un nuevo pedido.",
                    cliente.NombreCompleto, cliente.FechaLimiteGracia.Value.ToString("dd/MM/yyyy"));

            // Bloque 1 — Pausa de suscripción: mientras está pausada no se pueden generar
            // pedidos nuevos, aunque la fecha de vencimiento todavía no haya llegado.
            if (cliente.EstaPausada)
                throw new BE.AppException("err.bll.pedido.suscripcion_pausada",
                    "La suscripción de {0} está pausada hasta el {1}. Reanudarla en el módulo de Clientes antes de generar un nuevo pedido.",
                    cliente.NombreCompleto, cliente.FechaPausaHasta.Value.ToString("dd/MM/yyyy"));

            if (!cliente.SuscripcionVigente())
                throw new BE.AppException("err.bll.pedido.suscripcion_vencida",
                    "La suscripción de {0} venció el {1}. Renovar en el módulo de Clientes.",
                    cliente.NombreCompleto, cliente.FechaVencimiento.Value.ToString("dd/MM/yyyy"));

            return cliente;
        }

        // "Comprobar el cupo del plan" → ¿Excede el cupo disponible? Verifica que el plan del
        // cliente permita la cantidad de prendas seleccionadas y devuelve el plan consultado. Si
        // excede, lanza el motivo (Informar exceso de cupo, con el detalle de las restricciones).
        public BE.PlanSuscripcion ComprobarCupo(BE.Cliente cliente, int cantidadPrendas)
        {
            PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta);

            if (!cliente.IdPlan.HasValue)
                throw new BE.AppException("err.bll.pedido.sin_plan",
                    "El cliente {0} no tiene plan asignado. Asignale un plan antes de crear un pedido.",
                    cliente.NombreCompleto);

            var plan = dalPlan.ObtenerPorId(cliente.IdPlan.Value);

            if (plan == null)
                throw new BE.AppException("err.bll.pedido.cliente_inexistente",
                    "No se pudo obtener el plan del cliente.");

            if (!cliente.PuedeSolicitarPrendas(cantidadPrendas))
                throw new BE.AppException("err.bll.pedido.limite_plan",
                    "El plan '{0}' permite {1} prenda(s). El cliente ya tiene {2} en uso y agrega {3} más. Máximo posible: {4}.",
                    plan.Nombre, plan.LimitePrendas, cliente.StockUtilizado, cantidadPrendas, cliente.PrendasDisponiblesEnPlan());

            return plan;
        }

        // ── Historial ─────────────────────────────────────────────────────────

        /// <summary>
        /// Crea y persiste registros de historial para todos los campos cambiados en un evento.
        /// Obtiene el usuario de la sesión activa; si no hay sesión registra como anónimo.
        /// </summary>
        private void RegistrarHistorial(int idPedido, string accion,
                                        List<(string Campo, string Anterior, string Nuevo)> campos)
        {
            int?   idUsuario     = null;
            string nombreUsuario = null;

            if (Seguridad.SessionManager.IsLoggedIn)
            {
                var u = Seguridad.SessionManager.GetInstance().Usuario;
                idUsuario     = u.Id;
                nombreUsuario = u.Username;
            }

            int idOp = dalHistorial.ObtenerSiguienteIdOperacion(idPedido);

            var registros = campos.Select(c => new BE.PedidoHistorial
            {
                IdPedido      = idPedido,
                IdOperacion   = idOp,
                Fecha         = DateTime.Now,
                IdUsuario     = idUsuario,
                NombreUsuario = nombreUsuario,
                Accion        = accion,
                Campo         = c.Campo,
                ValorAnterior = c.Anterior,
                ValorNuevo    = c.Nuevo
            }).ToList();

            dalHistorial.RegistrarCambios(registros);
        }

        /// <summary>
        /// Calcula el nivel de urgencia de un pedido según el tiempo transcurrido.
        /// Pendiente: Urgente > 3 días, Normal 1-3 días, Reciente &lt; 1 día.
        /// Despachado: Urgente > 5 días, Normal 2-5 días, Reciente &lt; 2 días.
        /// </summary>
        public BE.NivelUrgencia CalcularNivelUrgencia(BE.Pedido p)
        {
            if (p.Estado == BE.EstadoPedido.Entregado || p.Estado == BE.EstadoPedido.Cancelado)
                return BE.NivelUrgencia.NoAplica;

            double dias = (DateTime.Now - p.FechaPedido).TotalDays;

            if (p.Estado == BE.EstadoPedido.Pendiente)
            {
                if (dias > 3) return BE.NivelUrgencia.Urgente;
                if (dias > 1) return BE.NivelUrgencia.Normal;
                return BE.NivelUrgencia.Reciente;
            }

            if (p.Estado == BE.EstadoPedido.Despachado)
            {
                double diasDespacho = p.FechaDespacho.HasValue
                    ? (DateTime.Now - p.FechaDespacho.Value).TotalDays : dias;
                if (diasDespacho > 5) return BE.NivelUrgencia.Urgente;
                if (diasDespacho > 2) return BE.NivelUrgencia.Normal;
                return BE.NivelUrgencia.Reciente;
            }

            return BE.NivelUrgencia.NoAplica;
        }

        /// <summary>Devuelve el historial de cambios de un pedido con filtros opcionales.</summary>
        public System.Data.DataTable ObtenerHistorial(
            int idPedido, string accion = null,
            DateTime? desde = null, DateTime? hasta = null)
            => dalHistorial.ObtenerPorPedido(idPedido, accion, desde, hasta);

        // T06b — Qué operaciones se pueden revertir desde el historial. Lista BLANCA: una
        // operación nueva queda bloqueada por defecto hasta que se analice si revertirla es seguro.
        //   • Los pasos del armado (PN01) tienen su propia operación en el proceso.
        //   • CANCELAR / DESCANCELAR / DEVOLUCION / RESTAURAR mueven prendas o reabren el circuito:
        //     tienen acciones explícitas (Reactivar, Cancelar, Registrar devolución) y revertirlas
        //     desde el historial reasignaría prendas que pudieron volver a circular.
        //   • DESPACHAR y ENTREGAR solo cambian el estado logístico y sus fechas, con las prendas
        //     reservadas a nombre del cliente durante todo el tramo: se pueden deshacer (por
        //     ejemplo un despacho registrado por error) siempre que sea la ÚLTIMA operación del
        //     pedido y el pedido siga en el estado que esa operación dejó.
        private static readonly HashSet<string> OperacionesRestaurables = new HashSet<string>
        {
            "DESPACHAR", "ENTREGAR"
        };

        private static readonly HashSet<string> OperacionesDelArmado = new HashSet<string>
        {
            "CREAR", "ENVIAR_CONTROL", "AJUSTAR_SELECCION", "INFORMAR_FALTANTES",
            "CONFIRMAR_PRENDAS", "SEPARAR", "FORMALIZAR", "DESISTIR"
        };

        /// <summary>
        /// Restaura el pedido al estado previo a la operación indicada (por IdOperacion).
        /// Revierte cada campo al ValorAnterior registrado y escribe un evento RESTAURAR.
        /// Solo se admite la ÚLTIMA operación del pedido, de un tipo restaurable, y solo si el
        /// pedido sigue en el estado que esa operación dejó (claim atómico en el DAL).
        /// </summary>
        public void RestaurarOperacion(string modulo, int idPedido, int idOperacion)
        {
            // Revierte campos del pedido: exige permiso de edición. El historial se abre desde Pedidos de
            // Venta y desde Pedidos Realizados, así que alcanza con poder editar cualquiera de los dos.
            try { PermisosAccion.Exigir(BE.Patentes.PedidosVentaEditar, BE.Patentes.PedidosVenta); }
            catch (BE.AppException)
            {
                PermisosAccion.Exigir(BE.Patentes.PedidosRealizadosEditar, BE.Patentes.PedidosRealizados);
            }

            var cambios = dalHistorial.ObtenerPorOperacion(idPedido, idOperacion);
            if (cambios == null || cambios.Count == 0)
                throw new BE.AppException("err.bll.pedido.historial_vacio",
                    "No se encontraron cambios para la operación #{0} del Pedido #{1}.",
                    idOperacion, idPedido);

            string accionOriginal = cambios[0].Accion;

            // PN01: los pasos del circuito de control de stock no se revierten desde el historial.
            if (OperacionesDelArmado.Contains(accionOriginal))
                throw new BE.AppException("err.bll.pedido.restaurar_no_permitido",
                    "La operación '{0}' es parte del armado del pedido (control de stock) y no se puede " +
                    "revertir desde el historial.", accionOriginal);
            if (!OperacionesRestaurables.Contains(accionOriginal))
                throw new BE.AppException("err.bll.pedido.restaurar_no_restaurable",
                    "La operación '{0}' no se puede revertir desde el historial: usá la acción correspondiente " +
                    "del pedido (reactivar, cancelar o registrar la devolución).", accionOriginal);

            // Solo la última operación: revertir una intermedia dejaría al pedido en un estado que
            // ignora lo que pasó después (por ejemplo, una devolución ya registrada).
            int ultima = dalHistorial.ObtenerSiguienteIdOperacion(idPedido) - 1;
            if (idOperacion != ultima)
                throw new BE.AppException("err.bll.pedido.restaurar_no_ultima",
                    "Solo se puede revertir la última operación del Pedido #{0} (la #{1}).", idPedido, ultima);

            var cambioEstado = cambios.Find(c => c.Campo == "Estado");
            if (cambioEstado == null
                || !Enum.TryParse(cambioEstado.ValorNuevo, out BE.EstadoPedido estadoQueDejo)
                || !Enum.TryParse(cambioEstado.ValorAnterior, out BE.EstadoPedido _))
                throw new BE.AppException("err.bll.pedido.restaurar_no_restaurable",
                    "La operación '{0}' no se puede revertir desde el historial: usá la acción correspondiente " +
                    "del pedido (reactivar, cancelar o registrar la devolución).", accionOriginal);

            var actual = dalPedido.ObtenerPorId(idPedido);
            if (actual == null || actual.Estado != estadoQueDejo)
                throw new BE.AppException("err.dal.pedido.estado_cambiado",
                    "El Pedido #{0} cambió de estado en otra sesión. Actualizá la lista y volvé a intentarlo.",
                    idPedido);

            // Restauración ATÓMICA: el DAL vuelve a exigir el estado esperado (WHERE Estado=...),
            // revierte los campos y verifica las prendas en UNA sola transacción; también
            // recalcula el DV del pedido después de confirmar.
            dalPedido.RestaurarOperacionAtomica(idPedido, estadoQueDejo,
                cambios.Select(c => (c.Campo, c.ValorAnterior)).ToList());

            RegistrarHistorial(idPedido, "RESTAURAR",
                cambios.Select(c => (c.Campo, c.ValorNuevo, c.ValorAnterior)).ToList());

            bitacora.Registrar(modulo,
                $"Restaurar Pedido #{idPedido} — Revertida operación '{accionOriginal}' (op. #{idOperacion})",
                BE.Criticidad.Alta);

            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.Reactivacion,
                $"Pedido #{idPedido} restaurado — operación '{accionOriginal}' #{idOperacion} revertida",
                idPedido: idPedido);
        }
    }
}
