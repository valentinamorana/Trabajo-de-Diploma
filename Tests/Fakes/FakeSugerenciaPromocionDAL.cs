using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de ISugerenciaPromocionDAL (sin base de datos). Configurable sobre
    /// los valores de retorno que BLL.SugerenciaPromocion y BLL.Promocion necesitan para ejercitar
    /// sus distintas ramas; espía sobre las escrituras.</summary>
    public class FakeSugerenciaPromocionDAL : ISugerenciaPromocionDAL
    {
        // ── Configuración ─────────────────────────────────────────────────────
        public List<BE.SugerenciaPromocion> Pendientes { get; set; } = new List<BE.SugerenciaPromocion>();
        public BE.SugerenciaPromocion SugerenciaPorId { get; set; }
        public int AltaIdGenerado { get; set; }

        // ── Espías ────────────────────────────────────────────────────────────
        public int AltaVeces { get; private set; }
        public BE.SugerenciaPromocion UltimoAlta { get; private set; }
        public int MarcarEvaluadaVeces { get; private set; }
        public int UltimoIdEvaluado { get; private set; }

        public List<BE.SugerenciaPromocion> ObtenerPendientes() => Pendientes;

        public BE.SugerenciaPromocion ObtenerPorId(int idSugerencia) => SugerenciaPorId;

        public int Alta(BE.SugerenciaPromocion sugerencia)
        {
            AltaVeces++;
            UltimoAlta = sugerencia;
            return AltaIdGenerado;
        }

        // false simula que otra sesión ya evaluó la sugerencia (el UPDATE condicionado no afectó filas).
        public bool MarcarEvaluadaResultado { get; set; } = true;
        public int ReabrirEvaluacionVeces { get; private set; }
        public void ReabrirEvaluacion(int idSugerencia) => ReabrirEvaluacionVeces++;

        public bool MarcarEvaluada(int idSugerencia)
        {
            MarcarEvaluadaVeces++;
            UltimoIdEvaluado = idSugerencia;
            return MarcarEvaluadaResultado;
        }
    }
}
