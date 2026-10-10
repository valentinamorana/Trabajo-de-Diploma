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

        public int AltaSinReserva(BE.Pedido pedido)
        {
            AltaSinReservaVeces++;
            UltimoAltaSinReserva = pedido;
            return AltaIdGenerado;
        }

        public void ReemplazarSeleccion(int idPedido, List<BE.Prenda> prendas)
        {
            ReemplazarSeleccionVeces++;
            UltimaSeleccionReemplazada = prendas;
        }

        public void RegistrarFaltantes(int idPedido, int idEmpleadoControl, List<BE.PedidoFaltante> faltantes)
        {
            RegistrarFaltantesVeces++;
            UltimosFaltantes = faltantes;
            UltimoIdEmpleadoControl = idEmpleadoControl;
        }

        public void ConfirmarPrendas(int idPedido, int idEmpleadoControl)
        {
            ConfirmarPrendasVeces++;
            UltimoIdEmpleadoControl = idEmpleadoControl;
        }

        public void SepararPrendas(int idPedido, int idCliente)
        {
            AntesDeSeparar?.Invoke();
            if (SepararPrendasLanza != null) throw SepararPrendasLanza;
            SepararPrendasVeces++;
        }

        public void Formalizar(int idPedido) => FormalizarVeces++;

        public List<BE.Prenda> UltimaSeleccionDesistida { get; private set; }

        public void RegistrarDesistimiento(int idPedido, string motivo, BE.EtapaDesistimiento etapa,
                                           List<BE.Prenda> seleccionAjustada = null)
        {
            RegistrarDesistimientoVeces++;
            UltimoMotivoDesistimiento = motivo;
            UltimaEtapaDesistimiento = etapa;
            UltimaSeleccionDesistida = seleccionAjustada;
        }

        public List<BE.PedidoFaltante> ObtenerFaltantes(int idPedido) => FaltantesDevueltos;

        public void Despachar(int idPedido) => DespacharVeces++;
        public void MarcarEntregado(int idPedido) => MarcarEntregadoVeces++;

        public int RegistrarDevolucion(int idPedido, int idCliente, string actor)
        {
            RegistrarDevolucionVeces++;
            UltimoActorDevolucion = actor;
            return RegistrarDevolucionRespuesta;
        }

        public BE.EstadoPedido? UltimoEstadoEsperadoRestaurar { get; private set; }
        public void RestaurarOperacionAtomica(int idPedido, BE.EstadoPedido estadoEsperado,
                                              IList<(string Campo, string ValorAnterior)> campos)
        {
            RestaurarOperacionAtomicaVeces++;
            UltimoEstadoEsperadoRestaurar = estadoEsperado;
            UltimoRestaurarOperacionCampos = campos;
        }

        public BE.EstadoPedido? UltimoEstadoEsperadoCancelar { get; private set; }
        public void Cancelar(int idPedido, int idCliente, BE.EstadoPedido estadoEsperado, string motivo)
        {
            CancelarVeces++;
            UltimoMotivoCancelar = motivo;
            UltimoEstadoEsperadoCancelar = estadoEsperado;
        }

        public bool DesCancelar(int idPedido, int idCliente)
        {
            DesCancelarVeces++;
            return DesCancelarRespuesta;
        }

        public List<BE.FilaDV> ObtenerFilasDV() => new List<BE.FilaDV>();
        public void RecalcularDV() => RecalcularDVVeces++;
        public int ActualizarDVVeces { get; private set; }
        public void ActualizarDV(int idPedido) => ActualizarDVVeces++;
    }
}
