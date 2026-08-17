using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Cobro de suscripción (PdN6).</summary>
    public interface ICobroDAL
    {
        /// <summary>Inserta un nuevo intento de cobro. Devuelve el ID generado.</summary>
        int Alta(BE.Cobro cobro);

        List<BE.Cobro> ObtenerPorCliente(int idCliente);
        List<BE.Cobro> ObtenerTodos();
    }
}
