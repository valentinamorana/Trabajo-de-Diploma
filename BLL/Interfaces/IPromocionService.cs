using System;
using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// PN03 — Métricas, promociones y toma de decisiones (ciclo de vida de la Promocion).
    /// El mapeo de cada actividad del diagrama a su método está en el encabezado de BLL.Promocion.
    /// </summary>
    public interface IPromocionService
    {
        // ── Consultas (cada una cierra antes las promociones vencidas) ─────────
        List<BE.Promocion> ObtenerTodas();
        List<BE.Promocion> ObtenerParaVentas();
        List<BE.Promocion> ObtenerPendientesRevisionContable();
        List<BE.PromocionHistorial> ObtenerHistorial(int idPromocion);
        BE.DictamenContable ObtenerUltimoDictamen(int idPromocion);
        BE.SolicitudBajaPromocion ObtenerUltimaSolicitudBaja(int idPromocion);
        BE.PromocionHistorial ObtenerDescarte(int idPromocion);

        // ── Administración: crear (desde sugerencia / manual) → Validar ────────
        int CrearDesdeSugerencia(string modulo, int idSugerencia, string nombre, string descripcion,
                                  BE.TipoDescuento tipo, decimal valor, DateTime fechaInicio, DateTime fechaFin,
                                  decimal margenEstimado, string impactoEconomico);

        int CrearManual(string modulo, string nombre, string descripcion, BE.TipoDescuento tipo, decimal valor,
                         DateTime fechaInicio, DateTime fechaFin, int? idPlan, string categoriaPrenda,
                         decimal margenEstimado, string impactoEconomico);

        void ValidarPromocion(BE.Promocion promocion);

        // ── Contabilidad: Analizar margen e impacto → ¿Aprueba? ────────────────
        BE.AnalisisImpactoPromocion AnalizarMargenEImpacto(int idPromocion);
        bool PuedeDictaminar(BE.Promocion promocion);
        int AprobarContable(string modulo, BE.Promocion promocion, string observacion);
        int RechazarContable(string modulo, BE.Promocion promocion, string observacion);

        // ── Administración: ¿Reformular? ───────────────────────────────────────
        void Reformular(string modulo, BE.Promocion promocion);
        void DescartarPromocion(string modulo, BE.Promocion promocion, string motivo);

        // ── Vigencia (región interrumpible) ────────────────────────────────────
        int SolicitarBaja(string modulo, BE.Promocion promocion, string motivo);
        int AprobarBaja(string modulo, BE.Promocion promocion, string observacion);
        int RechazarBaja(string modulo, BE.Promocion promocion, string motivo);
        void Desactivar(string modulo, BE.Promocion promocion, string motivo);
        int CerrarVencidas();
    }
}
