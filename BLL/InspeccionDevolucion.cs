using System;

namespace BLL
{
    /// <summary>
    /// PN04 — Casos de uso de Depósito que terminan una prenda con cargo al cliente:
    ///   • CU-DEP-01 Inspeccionar Devolución: la prenda devuelta (En Limpieza) reingresa sin
    ///     cargo (<see cref="AprobarReingreso"/>) o se da de baja cobrando la reposición
    ///     (<see cref="DarDeBajaConCargo"/>).
    ///   • CU-DEP-02 Reportar Prenda Perdida: la prenda En Uso se da de baja con cargo
    ///     (<see cref="ReportarPerdida"/>), solo si el pedido se entregó hace 30 días o más
    ///     (compra tácita, <see cref="Politicas.PoliticaCompraTacita"/>).
    ///
    /// Patrón State: la baja no asigna el estado a mano; el estado actual de la prenda
    /// (BE.Estados) decide si la transición a Baja es válida y la aplica (ControlarEstado).
    ///
    /// Antes la GUI orquestaba BLL.CargoPrenda.RegistrarCargo + BLL.Prenda.CambiarEstado(Baja)
    /// como dos llamadas sueltas, sin transacción. Ahora cargo y baja se persisten en UNA
    /// transacción (DAL.InspeccionDevolucion) y la GUI solo llama a estos métodos.
    /// Permisos: los mismos que ya exigían esos dos servicios (StockEditar, fallback Stock).
    /// </summary>
    public class InspeccionDevolucion
    {
        private readonly DAL.Interfaces.IInspeccionDevolucionDAL _dal;
        private readonly Interfaces.IPrendaService _prenda;
        private readonly CargoPrenda _cargo;
        private readonly Func<DateTime> _hoy;

        private Servicios.IRegistroBitacora        _bitacoraLazy;
        private Servicios.IRegistroBitacoraNegocio _bitacoraNegLazy;
        private Servicios.IRegistroBitacora        Bitacora    => _bitacoraLazy    ?? (_bitacoraLazy    = Servicios.FabricaBitacora.CrearSistema());
        private Servicios.IRegistroBitacoraNegocio BitacoraNeg => _bitacoraNegLazy ?? (_bitacoraNegLazy = Servicios.FabricaBitacora.CrearNegocio());

        public InspeccionDevolucion() : this(new DAL.InspeccionDevolucion(), new Prenda()) { }

        public InspeccionDevolucion(DAL.Interfaces.IInspeccionDevolucionDAL dal, Interfaces.IPrendaService prenda,
                                    Func<DateTime> hoy = null)
        {
            _hoy    = hoy ?? (() => DateTime.Today);
            _dal    = dal    ?? throw new ArgumentNullException(nameof(dal));
            _prenda = prenda ?? throw new ArgumentNullException(nameof(prenda));
            _cargo  = new CargoPrenda(new DAL.CargoPrenda());   // solo para ValidarDatos (sin acceso a datos)
        }

        /// <summary>Camino A — reingresa a Disponible sin cargo (mismo CambiarEstado de siempre).</summary>
        public void AprobarReingreso(string modulo, BE.Prenda prenda, string actor = null)
        {
            if (prenda == null) throw new ArgumentNullException(nameof(prenda));
            _prenda.CambiarEstado(modulo, prenda, BE.EstadoPrenda.Disponible, actor ?? ActorEnSesion());
        }

        /// <summary>Camino B — En Limpieza → Baja + cargo de reposición, atómico.</summary>
        public int DarDeBajaConCargo(string modulo, BE.Prenda prenda, string motivo, decimal monto, string actor = null)
            => BajaConCargo(modulo, prenda, motivo, monto, actor, BE.EstadoPrenda.EnLimpieza,
                            "err.bll.insp.no_en_limpieza",
                            "Solo se puede dar de baja con cargo una prenda En Limpieza pendiente de inspección.");

        /// <summary>
        /// CU-DEP-02 — En Uso → Baja + cargo de reposición, atómico. Solo para una prenda de un
        /// pedido Entregado hace 30 días o más (compra tácita): si el cliente todavía no la recibió,
        /// o el plazo no venció, se rechaza.
        /// </summary>
        public int ReportarPerdida(string modulo, BE.Prenda prenda, string motivo, decimal monto, string actor = null)
        {
            PermisosAccion.Exigir(BE.Patentes.StockEditar, BE.Patentes.Stock);
            if (prenda == null) throw new ArgumentNullException(nameof(prenda));
            if (prenda.Estado != BE.EstadoPrenda.EnUso)
                throw new BE.AppException("err.bll.insp.no_en_uso", "Solo se puede reportar como perdida una prenda En Uso.");

            // Se relee el pedido desde la base (no se confía en lo que muestra la pantalla).
            Politicas.PoliticaCompraTacita.Exigir(_dal.ObtenerPedidoEnCurso(prenda.IdPrenda), _hoy());

            return BajaConCargo(modulo, prenda, motivo, monto, actor, BE.EstadoPrenda.EnUso,
                                "err.bll.insp.no_en_uso",
                                "Solo se puede reportar como perdida una prenda En Uso.");
        }

        /// <summary>
        /// Para habilitar "Reportar pérdida" en la pantalla: prenda En Uso de un pedido Entregado
        /// hace 30 días o más. La regla la vuelve a validar <see cref="ReportarPerdida"/> contra la base.
        /// </summary>
        public bool PuedeReportarPerdida(BE.Prenda prenda, BE.Pedido pedido) =>
            prenda != null && prenda.PuedeReportarsePerdida() &&
            Politicas.PoliticaCompraTacita.PlazoVencido(pedido, _hoy());

        private int BajaConCargo(string modulo, BE.Prenda prenda, string motivo, decimal monto, string actor,
                                 BE.EstadoPrenda estadoEsperado, string claveEstado, string fallbackEstado)
        {
            PermisosAccion.Exigir(BE.Patentes.StockEditar, BE.Patentes.Stock);
            if (prenda == null) throw new ArgumentNullException(nameof(prenda));

            // Patrón State: el estado actual decide si puede pasar a Baja (EnUso y EnLimpieza sí).
            if (prenda.Estado != estadoEsperado || !prenda.TransicionPermitida(BE.EstadoPrenda.Baja))
                throw new BE.AppException(claveEstado, fallbackEstado);

            if (!prenda.IdUltimoCliente.HasValue)
                throw new BE.AppException("err.bll.cargoprenda.sin_cliente",
                    "La prenda '{0}' no tiene un último cliente registrado; no se le puede cargar el costo a nadie.",
                    prenda.Nombre);

            _cargo.ValidarDatos(motivo, monto);

            string quien = actor ?? ActorEnSesion();
            var cargo = new BE.CargoPrenda
            {
                IdPrenda      = prenda.IdPrenda,
                IdCliente     = prenda.IdUltimoCliente.Value,
                Motivo        = motivo,
                Monto         = monto,
                FechaRegistro = DateTime.Now,
                Actor         = quien,
                Estado        = BE.EstadoCargo.Pendiente
            };

            // Cargo + baja en una sola transacción: si cualquiera falla no queda ninguno.
            int idCargo = _dal.DarDeBajaConCargo(cargo, estadoEsperado);
            cargo.IdCargo = idCargo;

            // Reflejar en el objeto en memoria lo que ya quedó persistido, a través del State
            // (el estado actual aplica la transición; ya se validó arriba).
            prenda.ControlarEstado(BE.EstadoPrenda.Baja);
            prenda.IdClienteActual = null;

            // Auditoría DESPUÉS del commit: su falla no debe deshacer la operación de negocio.
            try
            {
                Bitacora.Registrar(modulo,
                    $"Cargo por daño/pérdida — Prenda ID {prenda.IdPrenda} '{prenda.Nombre}': ${monto} ({motivo})",
                    BE.Criticidad.Media);
                Bitacora.Registrar(modulo,
                    $"Estado Prenda ID {prenda.IdPrenda} '{prenda.Nombre}': {estadoEsperado} → {BE.EstadoPrenda.Baja}",
                    BE.Criticidad.Media);
                BitacoraNeg.Registrar(BE.TipoEventoNegocio.CambioEstadoPrenda,
                    $"Cargo por daño/pérdida: prenda '{prenda.Nombre}' — ${monto} — {motivo} — se sumará al próximo cobro de " +
                    (prenda.NombreUltimoCliente ?? "cliente ID " + prenda.IdUltimoCliente),
                    idPrenda: prenda.IdPrenda, idCliente: prenda.IdUltimoCliente);
                BitacoraNeg.Registrar(BE.TipoEventoNegocio.CambioEstadoPrenda,
                    $"Prenda '{prenda.Nombre}' (ID {prenda.IdPrenda}): {estadoEsperado} → {BE.EstadoPrenda.Baja}",
                    idPrenda: prenda.IdPrenda);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[BLL.InspeccionDevolucion] Bitácora: " + ex.Message);
            }

            return idCargo;
        }

        private static string ActorEnSesion() =>
            Seguridad.SessionManager.IsLoggedIn ? Seguridad.SessionManager.GetInstance().Usuario.Username : null;
    }
}
