using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Lista de Espera de prendas (mejora opcional).</summary>
    public interface IListaEsperaDAL
    {
        /// <summary>Inserta una nueva anotación en estado Pendiente. Devuelve el ID generado.</summary>
        int Alta(BE.ListaEspera fila);

        BE.ListaEspera ObtenerPorId(int id);

        /// <summary>
        /// Fila Pendiente más antigua (FIFO) para una prenda, o null si no hay nadie
        /// esperándola. Usada al liberarse la prenda (BLL.Prenda.CambiarEstado).
        /// </summary>
        BE.ListaEspera ObtenerPendienteMasAntigua(int idPrenda);

        /// <summary>
        /// Fila Reservada vigente (no expirada) de una prenda para un cliente distinto al
        /// indicado — si existe, la prenda está retenida para otro y no se puede asignar.
        /// </summary>
        BE.ListaEspera ObtenerReservaVigenteDeOtro(int idPrenda, int idClienteSolicitante);

        /// <summary>Fila Reservada vigente de esta prenda para este cliente (para cerrarla al crear el pedido).</summary>
        BE.ListaEspera ObtenerReservaVigenteDeCliente(int idPrenda, int idCliente);

        List<BE.ListaEspera> ObtenerActivas();
        List<BE.ListaEspera> ObtenerPorPrenda(int idPrenda);

        /// <summary>Cantidad de filas Reservadas con la ventana todavía vigente (para PanelAlertas).</summary>
        int ContarReservadasVigentes();

        void CambiarEstado(int idListaEspera, BE.EstadoListaEspera nuevoEstado,
                            System.DateTime? fechaLimiteReserva, string actor);
    }
}
