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
        public List<BE.PedidoFaltante> FaltantesDevueltos { get; set; } = new List<BE.PedidoFaltante>();
        // Si se configura, SepararPrendas la lanza (por ej. la de concurrencia "prenda_tomada").
        public Exception SepararPrendasLanza { get; set; }
        // Se ejecuta al entrar a SepararPrendas: permite simular que otra operación tomó una
        // prenda entre la revisión del stock y la reserva.
        public Action AntesDeSeparar { get; set; }

        // ── Espías ────────────────────────────────────────────────────────────
        public int AltaSinReservaVeces { get; private set; }
        public BE.Pedido UltimoAltaSinReserva { get; private set; }
        public int ReemplazarSeleccionVeces { get; private set; }
        public List<BE.Prenda> UltimaSeleccionReemplazada { get; private set; }
        public int RegistrarFaltantesVeces { get; private set; }
        public List<BE.PedidoFaltante> UltimosFaltantes { get; private set; }
        public int UltimoIdEmpleadoControl { get; private set; }
        public int ConfirmarPrendasVeces { get; private set; }
        public int SepararPrendasVeces { get; private set; }
        public int FormalizarVeces { get; private set; }
        public int RegistrarDesistimientoVeces { get; private set; }
        public string UltimoMotivoDesistimiento { get; private set; }
        public BE.EtapaDesistimiento? UltimaEtapaDesistimiento { get; private set; }
        public int DespacharVeces { get; private set; }
        public int MarcarEntregadoVeces { get; private set; }
        public int RegistrarDevolucionVeces { get; private set; }
        public string UltimoActorDevolucion { get; private set; }
        public int CancelarVeces { get; private set; }
        public string UltimoMotivoCancelar { get; private set; }
        public int DesCancelarVeces { get; private set; }
        public int RestaurarOperacionAtomicaVeces { get; private set; }
        public IList<(string Campo, string ValorAnterior)> UltimoRestaurarOperacionCampos { get; private set; }
        public int RecalcularDVVeces { get; private set; }
        public int? UltimoIdOperacionRestaurar { get; private set; }

        // Historial que cada escritura recibió para asentar en su transacción (solo si la escritura
        // "se confirmó": no se cuenta si la escritura lanza o no toca nada).
        public int HistorialRegistradoVeces { get; private set; }
        public List<BE.PedidoHistorial> UltimoHistorial { get; private set; }
        private void Asentar(List<BE.PedidoHistorial> historial)
        {
            if (historial == null || historial.Count == 0) return;
            HistorialRegistradoVeces++;
            UltimoHistorial = historial;
        }

        public List<BE.Pedido> ObtenerTodos() => PedidosDevueltos;
        public List<BE.Pedido> ObtenerPendientes() => PedidosDevueltos.FindAll(p => p.Estado == BE.EstadoPedido.Pendiente);
        public List<BE.Pedido> ObtenerPorEstado(BE.EstadoPedido estado) => PedidosDevueltos.FindAll(p => p.Estado == estado);
        public Dictionary<int, DateTime> ObtenerFechaUltimoPedidoPorCliente() => FechaUltimoPedidoPorCliente;
        public List<BE.DesempenoVendedor> ObtenerEstadisticasPorEmpleado() => EstadisticasPorEmpleado;
        public DateTime? UltimoDesdeRotacion { get; private set; }
        public Dictionary<int, int> ObtenerCantidadPedidosPorPrenda(DateTime? desde = null)
        {
            UltimoDesdeRotacion = desde;
            return CantidadPedidosPorPrenda;
        }
        public List<BE.Prenda> ObtenerPrendasHistoricasPorCliente(int idCliente) => PrendasHistoricasPorCliente;
        public BE.Pedido ObtenerPorId(int idPedido) => PedidosDevueltos.Find(p => p.IdPedido == idPedido);
        public bool TienePedidoActivo(int idCliente) => PedidosDevueltos.Exists(p => p.IdCliente == idCliente && p.EsActivo());

        public int AltaSinReserva(BE.Pedido pedido, List<BE.PedidoHistorial> historial = null)
        {
            AltaSinReservaVeces++;
            UltimoAltaSinReserva = pedido;
            Asentar(historial);
            return AltaIdGenerado;
        }

        public void ReemplazarSeleccion(int idPedido, List<BE.Prenda> prendas, List<BE.PedidoHistorial> historial = null)
        {
            ReemplazarSeleccionVeces++;
            UltimaSeleccionReemplazada = prendas;
            Asentar(historial);
        }

        public void RegistrarFaltantes(int idPedido, int idEmpleadoControl, List<BE.PedidoFaltante> faltantes,
                                       List<BE.PedidoHistorial> historial = null)
        {
            RegistrarFaltantesVeces++;
            UltimosFaltantes = faltantes;
            UltimoIdEmpleadoControl = idEmpleadoControl;
            Asentar(historial);
        }

        public void ConfirmarPrendas(int idPedido, int idEmpleadoControl, List<BE.PedidoHistorial> historial = null)
        {
            ConfirmarPrendasVeces++;
            UltimoIdEmpleadoControl = idEmpleadoControl;
            Asentar(historial);
        }

        public void SepararPrendas(int idPedido, int idCliente, List<BE.PedidoHistorial> historial = null)
        {
            AntesDeSeparar?.Invoke();
            if (SepararPrendasLanza != null) throw SepararPrendasLanza;
            SepararPrendasVeces++;
            Asentar(historial);
        }

        public void Formalizar(int idPedido, List<BE.PedidoHistorial> historial = null)
        {
            FormalizarVeces++;
            Asentar(historial);
        }

        public List<BE.Prenda> UltimaSeleccionDesistida { get; private set; }

        public void RegistrarDesistimiento(int idPedido, string motivo, BE.EtapaDesistimiento etapa,
                                           List<BE.Prenda> seleccionAjustada = null,
                                           List<BE.PedidoHistorial> historial = null)
        {
            RegistrarDesistimientoVeces++;
            UltimoMotivoDesistimiento = motivo;
            UltimaEtapaDesistimiento = etapa;
            UltimaSeleccionDesistida = seleccionAjustada;
            Asentar(historial);
        }

        public List<BE.PedidoFaltante> ObtenerFaltantes(int idPedido) => FaltantesDevueltos;

        public void Despachar(int idPedido, List<BE.PedidoHistorial> historial = null)
        {
            DespacharVeces++;
            Asentar(historial);
        }

        public void MarcarEntregado(int idPedido, List<BE.PedidoHistorial> historial = null)
        {
            MarcarEntregadoVeces++;
            Asentar(historial);
        }

        public int RegistrarDevolucion(int idPedido, int idCliente, string actor,
                                       Func<int, List<BE.PedidoHistorial>> historial = null)
        {
            RegistrarDevolucionVeces++;
            UltimoActorDevolucion = actor;
            // Como el DAL real: el historial se arma con la cantidad devuelta, solo si volvió alguna.
            if (RegistrarDevolucionRespuesta > 0 && historial != null) Asentar(historial(RegistrarDevolucionRespuesta));
            return RegistrarDevolucionRespuesta;
        }

        public BE.EstadoPedido? UltimoEstadoEsperadoRestaurar { get; private set; }
        public void RestaurarOperacionAtomica(int idPedido, int idOperacion, BE.EstadoPedido estadoEsperado,
                                              IList<(string Campo, string ValorAnterior)> campos,
                                              List<BE.PedidoHistorial> historial = null)
        {
            RestaurarOperacionAtomicaVeces++;
            UltimoIdOperacionRestaurar = idOperacion;
            UltimoEstadoEsperadoRestaurar = estadoEsperado;
            UltimoRestaurarOperacionCampos = campos;
            Asentar(historial);
        }

        public BE.EstadoPedido? UltimoEstadoEsperadoCancelar { get; private set; }
        public void Cancelar(int idPedido, int idCliente, BE.EstadoPedido estadoEsperado, string motivo,
                             List<BE.PedidoHistorial> historial = null)
        {
            CancelarVeces++;
            UltimoMotivoCancelar = motivo;
            UltimoEstadoEsperadoCancelar = estadoEsperado;
            Asentar(historial);
        }

        public bool DesCancelar(int idPedido, int idCliente, List<BE.PedidoHistorial> historial = null)
        {
            DesCancelarVeces++;
            if (DesCancelarRespuesta) Asentar(historial);
            return DesCancelarRespuesta;
        }

        public List<BE.FilaDV> ObtenerFilasDV() => new List<BE.FilaDV>();
        public void RecalcularDV() => RecalcularDVVeces++;
    }
}
