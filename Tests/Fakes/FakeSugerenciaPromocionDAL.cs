using System;
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

        // false simula que otra sesión ya evaluó o descartó la sugerencia (el UPDATE condicionado no afectó filas).
        public bool MarcarEvaluadaResultado { get; set; } = true;
        public bool DescartarResultado { get; set; } = true;

        // ── Espías ────────────────────────────────────────────────────────────
        public int AltaVeces { get; private set; }
        public BE.SugerenciaPromocion UltimoAlta { get; private set; }
        public int MarcarEvaluadaVeces { get; private set; }
        public int UltimoIdEvaluado { get; private set; }
        public int ReabrirEvaluacionVeces { get; private set; }
        public int DescartarVeces { get; private set; }
        public string UltimoMotivoDescarte { get; private set; }

        public List<BE.SugerenciaPromocion> ObtenerPendientes() => Pendientes;
        public List<BE.SugerenciaPromocion> ObtenerTodas() => Pendientes;
        public BE.SugerenciaPromocion ObtenerPorId(int idSugerencia) => SugerenciaPorId;

        public int Alta(BE.SugerenciaPromocion sugerencia)
        {
            AltaVeces++;
            UltimoAlta = sugerencia;
            return AltaIdGenerado;
        }

        public bool MarcarEvaluada(int idSugerencia, DateTime fecha)
        {
            MarcarEvaluadaVeces++;
            UltimoIdEvaluado = idSugerencia;
            return MarcarEvaluadaResultado;
        }

        public void ReabrirEvaluacion(int idSugerencia) => ReabrirEvaluacionVeces++;

        public bool Descartar(int idSugerencia, string motivo, DateTime fecha)
        {
            DescartarVeces++;
            UltimoMotivoDescarte = motivo;
            return DescartarResultado;
        }
    }
}
