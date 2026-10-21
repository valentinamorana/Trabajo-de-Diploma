using System;
using System.Collections.Generic;
using System.Linq;
using BLL.Inteligencia.Reglas;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 «Recomendar / decidir» — Motor de recomendaciones: contexto del patrón STRATEGY. Recorre las
    /// reglas registradas (cada una una IReglaRecomendacion independiente), junta lo que devuelven y lo
    /// ordena por prioridad y, dentro de la misma prioridad, por impacto estimado.
    ///
    /// Robustez: si una regla falla (dato inesperado), no se lleva puestas a las demás: su error queda en
    /// <see cref="ResultadoMotor.ReglasConError"/> y la pantalla lo avisa. Las reglas son puras: el motor no
    /// toca la base (el contexto lo arma BLL.Inteligencia.InteligenciaComercial).
    ///
    /// Reglas por defecto (README, sección PN03): las 7 terminan en una acción ejecutable al aceptarlas.
    ///   ChurnPlan · Renovaciones · Escasez · BajaRotacion · Mantenimiento · RendimientoPromocion · UpgradePlan
    /// (Vendedores y OcupacionStock se retiraron: solo registraban la decisión, sin operación que ejecutar.)
    /// </summary>
    public class MotorRecomendaciones
    {
        private readonly List<IReglaRecomendacion> reglas;

        public MotorRecomendaciones() : this(ReglasPorDefecto()) { }

        public MotorRecomendaciones(IEnumerable<IReglaRecomendacion> reglas)
        {
            this.reglas = (reglas ?? throw new ArgumentNullException(nameof(reglas))).Where(r => r != null).ToList();
        }

        public static List<IReglaRecomendacion> ReglasPorDefecto() => new List<IReglaRecomendacion>
        {
            new ReglaChurnPlan(),
            new ReglaRenovaciones(),
            new ReglaEscasez(),
            new ReglaBajaRotacion(),
            new ReglaMantenimiento(),
            new ReglaRendimientoPromocion(),
            new ReglaUpgradePlan()
        };

        public IReadOnlyList<IReglaRecomendacion> Reglas => reglas;

        public ResultadoMotor Evaluar(BE.ContextoComercial ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            var resultado = new ResultadoMotor();
            foreach (var regla in reglas)
            {
                try
                {
                    // ToList: la evaluación perezosa (yield) se ejecuta acá, dentro del try.
                    var recs = (regla.Evaluar(ctx) ?? Enumerable.Empty<BE.Recomendacion>()).Where(r => r != null && r.Principal != null).ToList();
                    foreach (var r in recs)
                        if (string.IsNullOrEmpty(r.TipoRegla)) r.TipoRegla = regla.Tipo;
                    resultado.Recomendaciones.AddRange(recs);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError($"[BLL.MotorRecomendaciones] Regla {regla.Tipo}: {ex.Message}");
                    resultado.ReglasConError.Add(regla.Tipo);
                }
            }
            resultado.Recomendaciones = Ordenar(resultado.Recomendaciones);
            return resultado;
        }

        /// <summary>Prioridad (Alta primero) y, a igual prioridad, mayor impacto primero.</summary>
        public static List<BE.Recomendacion> Ordenar(IEnumerable<BE.Recomendacion> recomendaciones) =>
            recomendaciones.OrderBy(r => r.Prioridad).ThenByDescending(r => r.ImpactoEstimado).ThenBy(r => r.Clave).ToList();
    }

    public class ResultadoMotor
    {
        public List<BE.Recomendacion> Recomendaciones { get; set; } = new List<BE.Recomendacion>();
        public List<string> ReglasConError { get; } = new List<string>();
        /// <summary>Seguimiento de las metas del mes en curso con el que se ajustaron las prioridades (vacío sin metas).</summary>
        public List<BE.SeguimientoMeta> Metas { get; set; } = new List<BE.SeguimientoMeta>();
    }
}
