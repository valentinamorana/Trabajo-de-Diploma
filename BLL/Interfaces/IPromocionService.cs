using System;
using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// PN03 — Métricas, promociones y toma de decisiones.
    ///
    /// Casos de uso definidos:
    ///   CrearDesdeSugerencia() / CrearManual() / Modificar() / Desactivar() — Administración
    ///   AprobarContable() / RechazarContable()                             — Contabilidad
    ///   SugerirBaja()                                                      — Vendedor
    ///   AprobarBaja() / RechazarBaja()                                     — Administración
    /// </summary>
    public interface IPromocionService
    {
        List<BE.Promocion> ObtenerTodas();
        List<BE.Promocion> ObtenerVigentes();
        List<BE.Promocion> ObtenerPendientesRevisionContable();
        BE.Promocion ObtenerPorId(int idPromocion);

        int CrearDesdeSugerencia(string modulo, int idSugerencia, string nombre, string descripcion,
                                  BE.TipoDescuento tipo, decimal valor, DateTime fechaInicio, DateTime fechaFin,
                                  decimal margenEstimado, string impactoEconomico);

        int CrearManual(string modulo, string nombre, string descripcion, BE.TipoDescuento tipo, decimal valor,
                         DateTime fechaInicio, DateTime fechaFin, int? idPlan, string categoriaPrenda,
                         decimal margenEstimado, string impactoEconomico);

        void Modificar(string modulo, BE.Promocion promocion);
        void Desactivar(string modulo, BE.Promocion promocion);

        void AprobarContable(string modulo, BE.Promocion promocion, string observacion);
        void RechazarContable(string modulo, BE.Promocion promocion, string observacion);

        void SugerirBaja(string modulo, BE.Promocion promocion, string motivo);
        void AprobarBaja(string modulo, BE.Promocion promocion);
        void RechazarBaja(string modulo, BE.Promocion promocion, string motivo);
    }
}
