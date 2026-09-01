using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>Contrato del acceso a datos de Promocion (PN03, permite dobles de prueba).</summary>
    public interface IPromocionDAL
    {
        List<BE.Promocion> ObtenerTodas();
        List<BE.Promocion> ObtenerVigentes();
        List<BE.Promocion> ObtenerPendientesRevisionContable();
        BE.Promocion ObtenerPorId(int idPromocion);
        int Alta(BE.Promocion promocion);
        void Modificar(BE.Promocion promocion);
        void CambiarEstado(int idPromocion, BE.EstadoPromocion nuevoEstado, string observacionOMotivo);

        // PN03, CU-VEND-04-Sugerir Baja: pasa a BajaSolicitada y registra el motivo (columna
        // propia, distinta de Observacion que usa Contabilidad).
        void SolicitarBaja(int idPromocion, string motivo);
    }
}
