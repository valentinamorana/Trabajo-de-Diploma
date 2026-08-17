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

        public PausarSuscripcionHandler(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.IRenovacionDAL dalRenovacion)
        {
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalRenovacion = dalRenovacion ?? throw new ArgumentNullException(nameof(dalRenovacion));
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
