using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Prenda (permite inyección y dobles de prueba).</summary>
    public interface IPrendaDAL
    {
        List<BE.Prenda> ObtenerTodos();

        /// <summary>
        /// Prendas Disponible. Si se indica <paramref name="idClienteSolicitante"/>, excluye las
        /// que estén Reservadas por Lista de Espera para OTRO cliente (mejora opcional) — ese
        /// cliente sigue viéndolas. Sin cliente en contexto, se excluyen todas las reservas activas.
        /// </summary>
        List<BE.Prenda> ObtenerDisponibles(int? idClienteSolicitante = null);
        BE.Prenda ObtenerPorId(int idPrenda);

        /// <summary>Relee un lote de prendas por ID en una sola consulta (batch), en vez de
        /// N consultas individuales. Devuelve solo las que existen — no lanza por IDs faltantes.</summary>
        List<BE.Prenda> ObtenerPorIds(List<int> ids);

        List<BE.Prenda> ObtenerPorCliente(int idCliente);
        int Alta(BE.Prenda prenda);
        void Modificar(BE.Prenda prenda);
        /// <summary>Cambia el estado y, en la MISMA transacción, abre o cierra el mantenimiento de la
        /// prenda (así nunca queda una prenda Disponible con un mantenimiento abierto, ni al revés).</summary>
        void CambiarEstado(int idPrenda, BE.EstadoPrenda estadoAnterior, BE.EstadoPrenda nuevoEstado, int? idClienteActual = null,
                           MovimientoMantenimiento mantenimiento = MovimientoMantenimiento.Ninguno, string actor = null);
        List<BE.StockPorTalleCategoria> ObtenerConteoDisponiblesPorTalleCategoria();

        /// <summary>Nombres del catálogo de categorías activas (tabla Categoria), ordenados.</summary>
        List<string> ObtenerCategorias();
    }

    /// <summary>Qué le pasa al mantenimiento de la prenda junto con el cambio de estado.</summary>
    public enum MovimientoMantenimiento
    {
        Ninguno,
        Abrir,    // entra a limpieza en el depósito (mantenimiento manual)
        Cerrar    // sale de limpieza (reingreso o baja)
    }
}
