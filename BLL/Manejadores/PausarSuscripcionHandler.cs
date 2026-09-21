using System;

namespace BLL.Manejadores
{
    /// <summary>
    /// Eslabón que atiende Decision.Pausar: pausa la suscripción hasta la fecha indicada
    /// SIN tocar FechaVencimiento — al reanudar (ver BLL.Cliente.ReanudarPausa), el cliente
    /// retoma exactamente el mismo plazo que tenía, no se le regala tiempo extra. Mientras
    /// está pausada, BLL.Pedido.ObtenerClienteValidado bloquea nuevos pedidos (ver
    /// BE.Cliente.EstaPausada). Debe insertarse ANTES de BajaSuscripcionHandler en la
    /// cadena (ver BLL.Renovacion) — mismo motivo que el resto de los eslabones no
    /// terminales (ver advertencia en DecisionRenovacion).
    /// </summary>
    public sealed class PausarSuscripcionHandler : ManejadorRenovacion
    {
        private readonly DAL.Interfaces.IClienteDAL dalCliente;
        private readonly DAL.Interfaces.IRenovacionDAL dalRenovacion;
        private readonly DAL.Interfaces.IPrendaDAL dalPrenda;

        // NUULY 4.8: la pausa tiene un máximo de 3 meses.
        public const int MaxMesesPausa = 3;

        public PausarSuscripcionHandler(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.IRenovacionDAL dalRenovacion,
                                        DAL.Interfaces.IPrendaDAL dalPrenda = null)
        {
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalRenovacion = dalRenovacion ?? throw new ArgumentNullException(nameof(dalRenovacion));
            this.dalPrenda = dalPrenda;
        }

        public override ResultadoRenovacion Procesar(ContextoRenovacion contexto)
        {
            if (contexto.Decision != DecisionRenovacion.Pausar)
                return DelegarASucesor(contexto);

            if (!contexto.FechaPausaHasta.HasValue)
                throw new BE.AppException("err.bll.renovacion.pausa_sin_fecha",
                    "Debe indicar hasta cuándo queda pausada la suscripción.");

            if (contexto.FechaPausaHasta.Value.Date < DateTime.Today)
                throw new BE.AppException("err.bll.renovacion.pausa_fecha_pasada",
                    "La fecha de reanudación no puede ser anterior a hoy.");

            // Sin re-pausar: si ya está pausada hay que reanudar primero; si no, se podría
            // encadenar pausas día a día y superar el tope de 3 meses.
            if (contexto.Cliente.EstaPausada)
                throw new BE.AppException("err.bll.renovacion.ya_pausada",
                    "La suscripción ya está pausada. Reanudala antes de pedir una nueva pausa.");

            if (contexto.FechaPausaHasta.Value.Date > DateTime.Today.AddMonths(MaxMesesPausa))
                throw new BE.AppException("err.bll.renovacion.pausa_excede_tope",
                    "La pausa no puede superar los {0} meses (hasta el {1:d}).",
                    new object[] { MaxMesesPausa, DateTime.Today.AddMonths(MaxMesesPausa) });

            // NUULY 4.8: no se puede pausar con prendas pendientes de devolución.
            if (dalPrenda != null && dalPrenda.ObtenerPorCliente(contexto.Cliente.IdCliente).Count > 0)
                throw new BE.AppException("err.bll.renovacion.pausa_con_prendas",
                    "No se puede pausar: el cliente tiene prendas en uso pendientes de devolución.");

            var cliente = contexto.Cliente;
            cliente.FechaPausaHasta = contexto.FechaPausaHasta;

            // UPDATE de Cliente + INSERT del historial en una única transacción (ver
            // IntentarRenovarHandler para el porqué).
            var ahora = DateTime.Now;
            int idRenovacion = 0;
            dalCliente.EjecutarTransaccion((conexion, tx) =>
            {
                dalCliente.ModificarEnTx(conexion, tx, cliente);
                idRenovacion = dalRenovacion.AltaEnTx(conexion, tx, new BE.Renovacion
                {
                    IdCliente = cliente.IdCliente,
                    IdPlanAnterior = cliente.IdPlan,
                    IdPlanNuevo = cliente.IdPlan,
                    FechaDeteccion = ahora,
                    FechaResolucion = ahora,
                    Resultado = BE.EstadoRenovacion.Pausada,
                    Actor = contexto.Actor
                });
            });
            dalCliente.RecalcularDV();

            return new ResultadoRenovacion
            {
                Resuelto = true,
                Estado = BE.EstadoRenovacion.Pausada,
                IdRenovacion = idRenovacion,
                Mensaje = $"Suscripción pausada hasta {contexto.FechaPausaHasta.Value:d}. El cliente no podrá generar nuevos pedidos hasta reanudarla.",
                Clave = "renov.msg.pausada",
                Args = new object[] { contexto.FechaPausaHasta.Value }
            };
        }
    }
}
