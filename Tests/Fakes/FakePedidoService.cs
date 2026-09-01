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
        public List<BE.Pedido> ObtenerTodos() => new List<BE.Pedido>();
        public List<BE.Pedido> ObtenerPendientes() => new List<BE.Pedido>();
        public BE.Pedido ObtenerPorId(int id) => null;
        public int CrearPedido(string modulo, int idCliente, List<BE.Prenda> prendas) => 0;
        public BE.PlanSuscripcion ValidarCupoDisponible(BE.Cliente cliente, int cantidadPrendas) => null;
        public int ReservarPrendas(List<BE.Prenda> prendas, int idCliente) => 0;
        public void Despachar(string modulo, BE.Pedido pedido) { }
        public void MarcarEntregado(string modulo, BE.Pedido pedido) { }
        public DataTable ObtenerHistorial(int idPedido, string accion = null, DateTime? desde = null, DateTime? hasta = null) => null;
        public void RestaurarOperacion(string modulo, int idPedido, int idOperacion) { }
        public BE.NivelUrgencia CalcularNivelUrgencia(BE.Pedido pedido) => BE.NivelUrgencia.NoAplica;
    }
}
