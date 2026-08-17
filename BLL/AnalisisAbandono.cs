using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio — Análisis de Abandono (PdN10). Contexto del patrón Strategy:
    /// mantiene la BLL.Estrategias.EstrategiaRiesgo activa en un campo y la intercambia
    /// vía CambiarEstrategia — igual que Jugador._estrategia en el ejemplo de cátedra.
    /// Cruza Cliente (suscripción/vencimiento) con la fecha del último pedido (DAL.Pedido)
    /// para darle a cada estrategia los datos que necesita evaluar.
    /// </summary>
    public class AnalisisAbandono : Interfaces.IAnalisisAbandonoService
    {
        private readonly DAL.Interfaces.IClienteDAL dalCliente;
        private readonly DAL.Pedido dalPedido;

        // Criterio por defecto: el que describe PdN10 literalmente (vencimiento + inactividad).
        private Estrategias.EstrategiaRiesgo _estrategia = new Estrategias.EstrategiaVencimientoInactividad();

        public AnalisisAbandono() : this(new DAL.Cliente(), new DAL.Pedido()) { }

        public AnalisisAbandono(DAL.Interfaces.IClienteDAL dalCliente, DAL.Pedido dalPedido)
        {
            this.dalCliente = dalCliente ?? throw new System.ArgumentNullException(nameof(dalCliente));
            this.dalPedido = dalPedido ?? throw new System.ArgumentNullException(nameof(dalPedido));
        }

        public void CambiarEstrategia(Estrategias.EstrategiaRiesgo estrategia)
        {
            if (estrategia == null) throw new System.ArgumentNullException(nameof(estrategia));
            _estrategia = estrategia;
        }

        public List<BE.ClienteEnRiesgo> Detectar()
        {
            var ultimoPedidoPorCliente = dalPedido.ObtenerFechaUltimoPedidoPorCliente();

            var resultado = new List<BE.ClienteEnRiesgo>();
            foreach (var cliente in dalCliente.ObtenerTodos().Where(c => c.TienePlan()))
            {
                var datos = new BE.DatosClienteRiesgo
                {
                    Cliente = cliente,
                    FechaUltimoPedido = ultimoPedidoPorCliente.TryGetValue(cliente.IdCliente, out var fecha) ? fecha : (System.DateTime?)null
                };

                var evaluacion = _estrategia.Evaluar(datos);
                if (!evaluacion.EnRiesgo) continue;

                resultado.Add(new BE.ClienteEnRiesgo
                {
                    IdCliente = cliente.IdCliente,
                    NombreCliente = cliente.NombreCompleto,
                    NombrePlan = cliente.NombrePlan,
                    FechaVencimiento = cliente.FechaVencimiento,
                    FechaUltimoPedido = datos.FechaUltimoPedido,
                    Estrategia = _estrategia.ToString(),
                    Motivo = evaluacion.Motivo,
                    Clave = evaluacion.Clave,
                    Args = evaluacion.Args
                });
            }

            return resultado;
        }
    }
}
