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

        // Claims atómicos: lanzan "estado_cambiado" si el pedido ya no está en el estado esperado.
        void Despachar(int idPedido);
        void MarcarEntregado(int idPedido);
        int RegistrarDevolucion(int idPedido, int idCliente);
        // Revierte campos desde el historial solo si el pedido sigue en 'estadoEsperado'.
        void RestaurarOperacionAtomica(int idPedido, BE.EstadoPedido estadoEsperado,
                                       IList<(string Campo, string ValorAnterior)> campos);
        // Pendiente|Separado → Cancelado, liberando solo las prendas EnUso de ese cliente.
        void Cancelar(int idPedido, int idCliente, BE.EstadoPedido estadoEsperado, string motivo);
        bool DesCancelar(int idPedido, int idCliente);
        List<BE.FilaDV> ObtenerFilasDV();
        // T07: DVH de un pedido + DVV (tras cada escritura) / recálculo total (solo administrativo).
        void ActualizarDV(int idPedido);
        void RecalcularDV();
    }
}
