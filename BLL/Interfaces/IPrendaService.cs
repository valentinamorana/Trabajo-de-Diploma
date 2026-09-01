using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// Gestión del catálogo de Prendas.
    /// </summary>
    public interface IPrendaService
    {
        // Devuelve todas las prendas con cliente actual (JOIN).
        List<BE.Prenda> ObtenerTodos();

        // Devuelve solo las prendas disponibles para asignar a pedidos. Si se indica
        // idClienteSolicitante, incluye las prendas reservadas por Lista de Espera para
        // ESE cliente y excluye las reservadas para otros (mejora opcional).
        List<BE.Prenda> ObtenerDisponibles(int? idClienteSolicitante = null);

        // Devuelve las prendas actualmente asignadas a un cliente.
        List<BE.Prenda> ObtenerPorCliente(int idCliente);

        // Obtiene una prenda por ID.
        BE.Prenda ObtenerPorId(int idPrenda);

        // Da de alta una nueva prenda. Estado inicial siempre Disponible.
        void Alta(string modulo, BE.Prenda prenda);

        // Modifica los datos descriptivos de una prenda (no afecta estado ni cliente).
        void Modificar(string modulo, BE.Prenda prenda);

        // Cambia el estado de una prenda validando las transiciones permitidas por negocio.
        void CambiarEstado(string modulo, BE.Prenda prenda, BE.EstadoPrenda nuevoEstado, string actor = null);

        // CU01-CS-Verificar Disponibilidad (PN01): releyendo el estado real desde la base,
        // confirma si cada prenda de la selección sigue Disponible. Solo lectura.
        (bool Disponible, List<BE.Prenda> NoDisponibles) VerificarDisponibilidad(List<BE.Prenda> seleccion);

        // Devuelve el historial de registros de mantenimiento/limpieza de una prenda.
        System.Collections.Generic.List<BE.MantenimientoPrenda> ObtenerHistorialMantenimiento(int idPrenda);

        // Devuelve las prendas que actualmente están en mantenimiento (sin fecha de salida).
        System.Collections.Generic.List<BE.MantenimientoPrenda> ObtenerEnMantenimiento();

        // Devuelve el resumen de ocupación del stock (total, en uso, en limpieza, disponibles).
        BE.OcupacionStock ObtenerOcupacion();

        // PN04, CU-DEP-01 Inspeccionar Devolución: prendas EnLimpieza pendientes de resolución.
        List<BE.Prenda> ObtenerEnLimpieza();
    }
}
