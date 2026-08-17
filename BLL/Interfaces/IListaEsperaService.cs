using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// Lista de Espera de prendas (mejora opcional, no requerida por la cátedra —
    /// inspirada en el TP de un compañero). Un cliente se anota por una prenda EnUso;
    /// al liberarse, queda reservada para él durante una ventana de tiempo.
    /// </summary>
    public interface IListaEsperaService
    {
        // Anota un cliente en la lista de espera de una prenda EnUso.
        void Anotar(string modulo, int idPrenda, int idCliente, string actor);

        // Cancela una anotación Pendiente o Reservada.
        void Cancelar(string modulo, int idListaEspera, string actor);

        // Si hay alguien esperando esta prenda, reserva la fila más antigua (FIFO).
        // Llamado desde BLL.Prenda.CambiarEstado al liberarse una prenda.
        void NotificarSiCorresponde(int idPrenda, string actor);

        // True si la prenda está reservada por Lista de Espera para un cliente distinto
        // al indicado — bloquea la asignación en BLL.Pedido.
        bool EstaReservadaParaOtro(int idPrenda, int idClienteSolicitante);

        // Si esta prenda estaba reservada para este cliente, cierra el ciclo (Convertida).
        // Llamado desde BLL.Pedido tras crear el pedido.
        void CerrarSiReservada(string modulo, int idPrenda, int idCliente, string actor);

        // IDs de prenda reservados para otro cliente distinto al indicado (o todos, si es null).
        // Usado por BLL.Prenda.ObtenerDisponibles para ocultarlas a terceros.
        List<int> ObtenerIdsReservadosParaOtro(int? idClienteSolicitante);

        List<BE.ListaEspera> ObtenerActivas();
        List<BE.ListaEspera> ObtenerPorPrenda(int idPrenda);

        // Cantidad de prendas reservadas esperando retiro (para PanelAlertas).
        int ContarReservadasVigentes();
    }
}
