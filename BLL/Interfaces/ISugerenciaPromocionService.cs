using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// PN03 — CU-GE-01-Sugerir Promoción a la Administración. Actor: GerenteComercial (Gerencia).
    /// </summary>
    public interface ISugerenciaPromocionService
    {
        List<BE.SugerenciaPromocion> ObtenerPendientes();
        BE.SugerenciaPromocion ObtenerPorId(int idSugerencia);

        // Crea una sugerencia de promoción para un plan o una categoría de prenda (nunca ambos).
        int Crear(string modulo, int? idPlan, string categoriaPrenda, string motivo,
                  BE.TipoDescuento tipoSugerido, decimal beneficioEstimado);
    }
}
