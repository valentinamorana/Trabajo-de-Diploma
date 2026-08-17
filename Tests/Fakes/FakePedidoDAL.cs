using System;
using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IPedidoDAL (sin base de datos). Configurable sobre los
    /// valores de retorno que BLL.Pedido necesita para ejercitar sus distintas ramas; espía
    /// sobre las escrituras.</summary>
    public class FakePedidoDAL : IPedidoDAL
    {
        // ── Configuración ─────────────────────────────────────────────────────
        public List<BE.Pedido> PedidosDevueltos { get; set; } = new List<BE.Pedido>();
        public int AltaIdGenerado { get; set; }
        public int RegistrarDevolucionRespuesta { get; set; } = 1;
        public bool DesCancelarRespuesta { get; set; } = true;
        public Dictionary<int, DateTime> FechaUltimoPedidoPorCliente { get; set; } = new Dictionary<int, DateTime>();
        public List<BE.DesempenoVendedor> EstadisticasPorEmpleado { get; set; } = new List<BE.DesempenoVendedor>();
        public Dictionary<int, int> CantidadPedidosPorPrenda { get; set; } = new Dictionary<int, int>();
        public List<BE.Prenda> PrendasHistoricasPorCliente { get; set; } = new List<BE.Prenda>();

        // ── Espías ────────────────────────────────────────────────────────────
        public int AltaVeces { get; private set; }
        public BE.Pedido UltimoAlta { get; private set; }
        public int DespacharVeces { get; private set; }
        public int MarcarEntregadoVeces { get; private set; }
        public int RegistrarDevolucionVeces { get; private set; }
        public int CancelarVeces { get; private set; }
        public string UltimoMotivoCancelar { get; private set; }
        public int DesCancelarVeces { get; private set; }
        public int RestaurarOperacionAtomicaVeces { get; private set; }
        public int RecalcularDVVeces { get; private set; }

        public List<BE.Pedido> ObtenerTodos() => PedidosDevueltos;
        public List<BE.Pedido> ObtenerPendientes() => PedidosDevueltos.FindAll(p => p.Estado == BE.EstadoPedido.Pendiente);
        public Dictionary<int, DateTime> ObtenerFechaUltimoPedidoPorCliente() => FechaUltimoPedidoPorCliente;
        public List<BE.DesempenoVendedor> ObtenerEstadisticasPorEmpleado() => EstadisticasPorEmpleado;
        public Dictionary<int, int> ObtenerCantidadPedidosPorPrenda() => CantidadPedidosPorPrenda;
        public List<BE.Prenda> ObtenerPrendasHistoricasPorCliente(int idCliente) => PrendasHistoricasPorCliente;
        public BE.Pedido ObtenerPorId(int idPedido) => PedidosDevueltos.Find(p => p.IdPedido == idPedido);

        public int Alta(BE.Pedido pedido)
        {
            AltaVeces++;
            UltimoAlta = pedido;
            return AltaIdGenerado;
        }

        public void Despachar(int idPedido) => DespacharVeces++;
        public void MarcarEntregado(int idPedido) => MarcarEntregadoVeces++;

        public int RegistrarDevolucion(int idPedido, int idCliente)
        {
            RegistrarDevolucionVeces++;
            return RegistrarDevolucionRespuesta;
        }

        public void ReconciliarPrendasConEstado(int idPedido) { }

        public void RestaurarOperacionAtomica(int idPedido, IList<(string Campo, string ValorAnterior)> campos)
            => RestaurarOperacionAtomicaVeces++;

        public void Cancelar(int idPedido, string motivo)
        {
            CancelarVeces++;
            UltimoMotivoCancelar = motivo;
        }

        public bool DesCancelar(int idPedido, int idCliente)
        {
            DesCancelarVeces++;
            return DesCancelarRespuesta;
        }

        public List<BE.FilaDV> ObtenerFilasDV() => new List<BE.FilaDV>();
        public void RecalcularDV() => RecalcularDVVeces++;
    }
}
