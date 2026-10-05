using System;
using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Pedido (permite inyección y dobles de prueba).</summary>
    public interface IPedidoDAL
    {
        List<BE.Pedido> ObtenerTodos();
        List<BE.Pedido> ObtenerPendientes();
        List<BE.Pedido> ObtenerPorEstado(BE.EstadoPedido estado);
        Dictionary<int, DateTime> ObtenerFechaUltimoPedidoPorCliente();
        List<BE.DesempenoVendedor> ObtenerEstadisticasPorEmpleado();
        Dictionary<int, int> ObtenerCantidadPedidosPorPrenda();
        List<BE.Prenda> ObtenerPrendasHistoricasPorCliente(int idCliente);
        BE.Pedido ObtenerPorId(int idPedido);

        // ── PN01 — circuito de control de stock ─────────────────────────────
        // Inserta el pedido y sus líneas SIN reservar prendas (EnControlStock o Desistido).
        int AltaSinReserva(BE.Pedido pedido);
        // Reemplaza la selección de un pedido ConFaltantes y lo devuelve a EnControlStock.
        void ReemplazarSeleccion(int idPedido, List<BE.Prenda> prendas);
        // EnControlStock → ConFaltantes, con el informe de faltantes y alternativas.
        void RegistrarFaltantes(int idPedido, int idEmpleadoControl, List<BE.PedidoFaltante> faltantes);
        // Marca todas las líneas como confirmadas por Depósito (sigue EnControlStock).
        void ConfirmarPrendas(int idPedido, int idEmpleadoControl);
        // EnControlStock → Separado y prendas Disponible → EnUso, en una transacción.
        void SepararPrendas(int idPedido, int idCliente);
        // Separado → Pendiente (formalizado).
        void Formalizar(int idPedido);
        // ConFaltantes → Desistido.
        void RegistrarDesistimiento(int idPedido, string motivo, BE.EtapaDesistimiento etapa,
                                    List<BE.Prenda> seleccionAjustada = null);
        List<BE.PedidoFaltante> ObtenerFaltantes(int idPedido);

        void Despachar(int idPedido);
        void MarcarEntregado(int idPedido);
        int RegistrarDevolucion(int idPedido, int idCliente);
        void ReconciliarPrendasConEstado(int idPedido);
        void RestaurarOperacionAtomica(int idPedido, IList<(string Campo, string ValorAnterior)> campos);
        void Cancelar(int idPedido, string motivo);
        bool DesCancelar(int idPedido, int idCliente);
        List<BE.FilaDV> ObtenerFilasDV();
        void RecalcularDV();
    }
}
