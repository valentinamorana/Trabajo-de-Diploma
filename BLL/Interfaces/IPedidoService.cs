using System;
using System.Collections.Generic;
using System.Data;

namespace BLL.Interfaces
{
    /// <summary>
    /// Gestión del ciclo de vida de Pedidos.
    ///
    /// Casos de uso definidos:
    ///   PN01 Armar pedido (diagrama de actividad): VerificarVigencia, RevisarPedidoActivo,
    ///     ComprobarCupo, EnviarAControlStock, AsentarDesistimiento, AjustarSeleccion,
    ///     FormalizarPedido, PrepararConfirmacion (Vendedor) y RevisarStock, InformarFaltantes,
    ///     ConfirmarPrendasDisponibles, SepararPrendas (Depósito)
    ///   Despachar()           — Deposito despacha el pedido
    ///   MarcarEntregado()     — Se confirma la entrega al cliente
    ///   RegistrarDevolucion() — El cliente devuelve las prendas al finalizar
    ///   Cancelar()            — Se cancela un pedido Pendiente
    ///   DesCancelar()         — Se revierte la cancelación
    ///   ObtenerHistorial()    — Devuelve el historial de cambios de un pedido
    ///   RestaurarOperacion()  — Restaura el pedido al estado previo a una operación
    /// </summary>
    public interface IPedidoService
    {
        // Devuelve todos los pedidos.
        List<BE.Pedido> ObtenerTodos();

        // Devuelve solo los pedidos en estado Pendiente.
        List<BE.Pedido> ObtenerPendientes();

        // Obtiene un pedido por ID con sus prendas asociadas.
        BE.Pedido ObtenerPorId(int id);

        // Pedidos en un estado dado.
        List<BE.Pedido> ObtenerPorEstado(BE.EstadoPedido estado);

        // ── PN01 — Armar pedido de prendas (una operación por actividad del diagrama) ──
        // "Verificar la vigencia de la suscripción": devuelve el cliente o lanza el motivo.
        BE.Cliente VerificarVigencia(int idCliente);

        // "Revisar existencia de un pedido activo": lanza el motivo si lo tiene.
        void RevisarPedidoActivo(BE.Cliente cliente);

        // Paso 1 del asistente: VerificarVigencia + RevisarPedidoActivo.
        BE.Cliente ValidarPuedeArmarPedido(int idCliente);

        // "Comprobar el cupo del plan": devuelve el plan o lanza el exceso.
        BE.PlanSuscripcion ComprobarCupo(BE.Cliente cliente, int cantidadPrendas);

        // "Enviar selección para control stock": crea el pedido EnControlStock. Devuelve el ID.
        int EnviarAControlStock(string modulo, int idCliente, List<BE.Prenda> prendas);

        // "Asentar desistimiento" por exceso de cupo, sin pedido previo. Devuelve el ID.
        int AsentarDesistimiento(string modulo, int idCliente, List<BE.Prenda> prendas, string motivo);

        // "Asentar desistimiento" de un pedido con faltantes informados.
        void AsentarDesistimiento(string modulo, BE.Pedido pedido, string motivo, BE.EtapaDesistimiento etapa,
                                  List<BE.Prenda> seleccionAjustada = null);

        // "Recibir selección ajustada por disponibilidad": vuelve a control de stock.
        void AjustarSeleccion(string modulo, BE.Pedido pedido, List<BE.Prenda> prendas);

        // Depósito — "Revisar stock de las prendas" (planilla de control de existencias).
        List<BE.LineaControlStock> RevisarStock(BE.Pedido pedido);

        // Depósito — "Informe de prendas faltantes" (faltantes y alternativas).
        List<BE.PedidoFaltante> InformarFaltantes(string modulo, BE.Pedido pedido);

        // Depósito — "Confirmar prendas disponibles".
        void ConfirmarPrendasDisponibles(string modulo, BE.Pedido pedido);

        // Depósito — "Separar prendas del pedido" (reserva las prendas).
        void SepararPrendas(string modulo, BE.Pedido pedido);

        // "Formalizar el pedido": desde acá no admite modificaciones.
        void FormalizarPedido(string modulo, BE.Pedido pedido);

        // "Preparar la confirmación": pedido formalizado completo para la constancia.
        BE.Pedido PrepararConfirmacion(string modulo, int idPedido);

        // Cola de Depósito (pedidos EnControlStock, FIFO).
        List<BE.Pedido> ObtenerColaControlStock();

        // Informe de faltantes y alternativas de un pedido.
        List<BE.PedidoFaltante> ObtenerInformeFaltantes(int idPedido);

        // Marca el pedido como Despachado. Solo válido desde estado Pendiente.
        void Despachar(string modulo, BE.Pedido pedido);

        // Marca el pedido como Entregado. Solo válido desde estado Despachado.
        void MarcarEntregado(string modulo, BE.Pedido pedido);

        // Registra la devolución de prendas por parte del cliente.
        // Libera las prendas a estado Disponible o EnLimpieza según configuración.
        void RegistrarDevolucion(string modulo, BE.Pedido pedido);

        // Cancela un pedido Pendiente con un motivo obligatorio.
        void Cancelar(string modulo, BE.Pedido pedido, string motivo);

        // Revierte la cancelación de un pedido si las prendas siguen disponibles.
        void DesCancelar(string modulo, BE.Pedido pedido);

        // Devuelve el historial de cambios de un pedido con filtros opcionales.
        System.Data.DataTable ObtenerHistorial(int idPedido, string accion = null,
                                               System.DateTime? desde = null,
                                               System.DateTime? hasta = null);

        // Restaura el pedido al estado previo a la operación indicada.
        void RestaurarOperacion(string modulo, int idPedido, int idOperacion);

        // Calcula el nivel de urgencia de un pedido según su estado y antigüedad.
        BE.NivelUrgencia CalcularNivelUrgencia(BE.Pedido pedido);
    }
}
