using System;

namespace BLL.Manejadores
{
    /// <summary>
    /// Tercer eslabón: atiende el caso en que el cobro falló Y el cliente todavía tiene
    /// margen (no está en gracia, o está en gracia pero el plazo no venció). Si la
    /// decisión pedida no es PagoFallido, delega al siguiente eslabón. Si YA venció el
    /// plazo de gracia otorgado, tampoco atiende: delega a SuspenderHandler — el mismo
    /// criterio de "cada eslabón evalúa inline si le corresponde" que CambioPlanHandler
    /// usa para su propia decisión.
    /// </summary>
    public sealed class AplicarGraciaHandler : ManejadorCobro
    {
        /// <summary>Días de margen otorgados desde el primer cobro fallido de un ciclo.</summary>
        public const int DiasDeGracia = 5;

        private readonly DAL.Interfaces.IClienteDAL dalCliente;
        private readonly DAL.Interfaces.ICobroDAL dalCobro;

        public AplicarGraciaHandler(DAL.Interfaces.IClienteDAL dalCliente, DAL.Interfaces.ICobroDAL dalCobro)
        {
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
            this.dalCobro = dalCobro ?? throw new ArgumentNullException(nameof(dalCobro));
        }

        public override ResultadoCobro Procesar(ContextoCobro contexto)
        {
            if (contexto.Decision != DecisionCobro.PagoFallido)
                return _sucesor.Procesar(contexto);

            var cliente = contexto.Cliente;

            // El plazo de gracia ya otorgado venció sin que se haya regularizado: no
            // corresponde reabrir un nuevo plazo, es el último eslabón el que atiende.
            if (cliente.EstaSuspendidoPorPago)
                return _sucesor.Procesar(contexto);

            // Primer cobro fallido del ciclo (o todavía dentro del plazo ya otorgado):
            // fija (o mantiene) la fecha límite sin extenderla en cada reintento.
            if (!cliente.EstaEnGracia)
            {
                cliente.FechaLimiteGracia = DateTime.Today.AddDays(DiasDeGracia);
                dalCliente.Modificar(cliente);
            }

            var ahora = DateTime.Now;
            int idCobro = dalCobro.Alta(new BE.Cobro
            {
                IdCliente = cliente.IdCliente,
                Importe = 0,
                FechaDeteccion = ahora,
                FechaResolucion = ahora,
                Resultado = BE.EstadoCobro.Gracia,
                Actor = contexto.Actor
            });

            return new ResultadoCobro
            {
                Resuelto = true,
                Estado = BE.EstadoCobro.Gracia,
                IdCobro = idCobro,
                Mensaje = $"Cobro fallido. Período de gracia hasta {cliente.FechaLimiteGracia:d} " +
                          "para regularizar antes de suspender nuevos pedidos.",
                Clave = "cobro.msg.gracia",
                Args = new object[] { cliente.FechaLimiteGracia }
            };
        }
    }
}
