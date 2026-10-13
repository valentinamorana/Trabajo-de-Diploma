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
        // desde: solo los pedidos de esa fecha en adelante (PN03: métricas por período); null = toda la historia.
        Dictionary<int, int> ObtenerCantidadPedidosPorPrenda(DateTime? desde = null);
        List<BE.Prenda> ObtenerPrendasHistoricasPorCliente(int idCliente);
        BE.Pedido ObtenerPorId(int idPedido);
        // ¿El cliente tiene un pedido que todavía no terminó su ciclo (BE.Pedido.EsActivo)?
        bool TienePedidoActivo(int idCliente);

        // Todas las escrituras reciben 'historial': los campos del evento para PedidoHistorial (el
        // DAL completa IdPedido e IdOperacion). Se asientan en la MISMA transacción que el cambio,
        // junto con el recálculo del DV (T07): o queda todo, o no queda nada.

        // ── PN01 — circuito de control de stock ─────────────────────────────
        // Inserta el pedido y sus líneas SIN reservar prendas (EnControlStock o Desistido).
        int AltaSinReserva(BE.Pedido pedido, List<BE.PedidoHistorial> historial = null);
        // Reemplaza la selección de un pedido ConFaltantes y lo devuelve a EnControlStock.
        void ReemplazarSeleccion(int idPedido, List<BE.Prenda> prendas, List<BE.PedidoHistorial> historial = null);
        // EnControlStock → ConFaltantes, con el informe de faltantes y alternativas.
        void RegistrarFaltantes(int idPedido, int idEmpleadoControl, List<BE.PedidoFaltante> faltantes,
                                List<BE.PedidoHistorial> historial = null);
        // Marca todas las líneas como confirmadas por Depósito (sigue EnControlStock).
        void ConfirmarPrendas(int idPedido, int idEmpleadoControl, List<BE.PedidoHistorial> historial = null);
        // EnControlStock → Separado y prendas Disponible → EnUso, en una transacción.
        void SepararPrendas(int idPedido, int idCliente, List<BE.PedidoHistorial> historial = null);
        // Separado → Pendiente (formalizado).
        void Formalizar(int idPedido, List<BE.PedidoHistorial> historial = null);
        // ConFaltantes → Desistido.
        void RegistrarDesistimiento(int idPedido, string motivo, BE.EtapaDesistimiento etapa,
                                    List<BE.Prenda> seleccionAjustada = null, List<BE.PedidoHistorial> historial = null);
        List<BE.PedidoFaltante> ObtenerFaltantes(int idPedido);

        // Claims atómicos: lanzan "estado_cambiado" si el pedido ya no está en el estado esperado.
        void Despachar(int idPedido, List<BE.PedidoHistorial> historial = null);
        void MarcarEntregado(int idPedido, List<BE.PedidoHistorial> historial = null);
        // actor: quién registra la devolución (queda en MantenimientoPrenda.Actor). El historial
        // depende de cuántas prendas volvieron: se arma con esa cantidad (solo si volvió alguna).
        int RegistrarDevolucion(int idPedido, int idCliente, string actor,
                                Func<int, List<BE.PedidoHistorial>> historial = null);
        // Revierte campos desde el historial solo si el pedido sigue en 'estadoEsperado' e
        // 'idOperacion' sigue siendo la última operación del pedido.
        void RestaurarOperacionAtomica(int idPedido, int idOperacion, BE.EstadoPedido estadoEsperado,
                                       IList<(string Campo, string ValorAnterior)> campos,
                                       List<BE.PedidoHistorial> historial = null);
        // Pendiente|Separado → Cancelado, liberando solo las prendas EnUso de ese cliente.
        void Cancelar(int idPedido, int idCliente, BE.EstadoPedido estadoEsperado, string motivo,
                      List<BE.PedidoHistorial> historial = null);
        bool DesCancelar(int idPedido, int idCliente, List<BE.PedidoHistorial> historial = null);
        List<BE.FilaDV> ObtenerFilasDV();
        // T07: recálculo total del DV (solo administrativo). El DV de cada escritura se recalcula
        // dentro de su propia transacción.
        void RecalcularDV();
    }
}
