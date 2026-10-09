using System;
using System.Collections.Generic;
using System.Data;
using BLL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>
    /// Doble de prueba de IPedidoService (sin base de datos). Implementa todos los
    /// miembros del contrato con cuerpos mínimos y deja espías sobre
    /// Cancelar/DesCancelar/RegistrarDevolucion, que son los que ejercitan los
    /// comandos de BLL.Comandos (PdN3).
    /// </summary>
    public class FakePedidoService : IPedidoService
    {
        public int CancelarVeces { get; private set; }
        public int DesCancelarVeces { get; private set; }
        public int RegistrarDevolucionVeces { get; private set; }
        public string UltimoMotivoCancelacion { get; private set; }

        public void Cancelar(string modulo, BE.Pedido pedido, string motivo)
        {
            CancelarVeces++;
            UltimoMotivoCancelacion = motivo;
        }

        public void DesCancelar(string modulo, BE.Pedido pedido) => DesCancelarVeces++;

        public void RegistrarDevolucion(string modulo, BE.Pedido pedido) => RegistrarDevolucionVeces++;

        // Resto del contrato: no ejercitado por estos tests, cuerpos mínimos.
        public List<BE.Pedido> Pedidos { get; } = new List<BE.Pedido>();
        public List<BE.Pedido> ObtenerTodos() => Pedidos;
        public List<BE.Pedido> ObtenerPendientes() => new List<BE.Pedido>();
        public BE.Pedido ObtenerPorId(int id) => null;
        public List<BE.Pedido> ObtenerPorEstado(BE.EstadoPedido estado) => new List<BE.Pedido>();
        public BE.Cliente VerificarVigencia(int idCliente) => new BE.Cliente { IdCliente = idCliente };
        public void RevisarPedidoActivo(BE.Cliente cliente) { }
        public BE.Cliente ValidarPuedeArmarPedido(int idCliente) => new BE.Cliente { IdCliente = idCliente };
        public BE.PlanSuscripcion ComprobarCupo(BE.Cliente cliente, int cantidadPrendas) => null;
        public int EnviarAControlStock(string modulo, int idCliente, List<BE.Prenda> prendas) => 0;
        public int AsentarDesistimiento(string modulo, int idCliente, List<BE.Prenda> prendas, string motivo) => 0;
        public void AsentarDesistimiento(string modulo, BE.Pedido pedido, string motivo, BE.EtapaDesistimiento etapa,
                                         List<BE.Prenda> seleccionAjustada = null) { }
        public void AjustarSeleccion(string modulo, BE.Pedido pedido, List<BE.Prenda> prendas) { }
        public List<BE.LineaControlStock> RevisarStock(BE.Pedido pedido) => new List<BE.LineaControlStock>();
        public List<BE.PedidoFaltante> InformarFaltantes(string modulo, BE.Pedido pedido) => new List<BE.PedidoFaltante>();
        public void ConfirmarPrendasDisponibles(string modulo, BE.Pedido pedido) { }
        public void SepararPrendas(string modulo, BE.Pedido pedido) { }
        public void FormalizarPedido(string modulo, BE.Pedido pedido) { }
        public BE.Pedido PrepararConfirmacion(string modulo, int idPedido) => null;
        public List<BE.Pedido> ColaControlStock { get; set; } = new List<BE.Pedido>();
        public List<BE.Pedido> ObtenerColaControlStock() => ColaControlStock;
        public List<BE.PedidoFaltante> ObtenerInformeFaltantes(int idPedido) => new List<BE.PedidoFaltante>();
        public int DespacharVeces { get; private set; }
        public int EntregarVeces { get; private set; }
        public void Despachar(string modulo, BE.Pedido pedido) => DespacharVeces++;
        public void MarcarEntregado(string modulo, BE.Pedido pedido) => EntregarVeces++;
        public DataTable ObtenerHistorial(int idPedido, string accion = null, DateTime? desde = null, DateTime? hasta = null) => null;
        public void RestaurarOperacion(string modulo, int idPedido, int idOperacion) { }
        public BE.NivelUrgencia CalcularNivelUrgencia(BE.Pedido pedido) => BE.NivelUrgencia.NoAplica;
    }
}
