using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 «Avisos al iniciar sesión» — Para quien tiene el Centro de decisiones (patente de Gerencia
    /// mnuSugerenciaPromocion): un resumen de lo que hay para decidir hoy, para que el análisis no espere a
    /// que alguien abra la pantalla:
    ///   · recomendaciones de prioridad Alta que se pueden decidir (visibles y sin una decisión final vigente);
    ///   · decisiones pospuestas cuya fecha de revisión es hoy o ya pasó (la última decisión de cada situación,
    ///     solo si el motor sigue recomendando esa situación a este usuario; se cuentan una vez, no también como altas);
    ///   · metas del mes en curso en riesgo (o ya perdidas).
    /// Si no hay nada, no se avisa. Cada usuario puede elegir "No mostrar hoy" (se guarda en ParametroSistema
    /// con la clave AvisoComercial.Oculto.&lt;IdUsuario&gt; = fecha). El cálculo es puro (<see cref="Resumir"/>).
    /// </summary>
    public class AvisosComerciales
    {
        public const string PrefijoOculto = "AvisoComercial.Oculto.";
        public const int MaximoDestacados = 4;

        private readonly DAL.Interfaces.IInteligenciaComercialDAL dal;
        private readonly Func<DateTime> hoy;
        private readonly Servicios.IRegistroBitacora bitacora = Servicios.FabricaBitacora.CrearSistema();

        public AvisosComerciales() : this(new DAL.InteligenciaComercial()) { }

        public AvisosComerciales(DAL.Interfaces.IInteligenciaComercialDAL dal, Func<DateTime> hoy = null)
        {
            this.dal = dal ?? throw new ArgumentNullException(nameof(dal));
            this.hoy = hoy ?? (() => DateTime.Today);
        }

        /// <summary>¿El usuario de la sesión debe recibir los avisos? (tiene el Centro de decisiones).</summary>
        public static bool Corresponde(Func<string, bool> tienePermiso) =>
            tienePermiso != null && tienePermiso(BE.Patentes.SugerenciaPromocion);

        /// <summary>Resumen de lo urgente (puro).</summary>
        public static BE.ResumenAvisosComerciales Resumir(IEnumerable<BE.Recomendacion> recomendaciones,
                                                          IEnumerable<BE.DecisionComercial> historial,
                                                          IEnumerable<BE.SeguimientoMeta> metas,
                                                          DateTime hoy, int diasSilencioDescarte, DateTime inicioAnalisis)
        {
            var r = new BE.ResumenAvisosComerciales();
            var recs = (recomendaciones ?? Enumerable.Empty<BE.Recomendacion>()).Where(x => x != null).ToList();
            var vigentes = new HashSet<string>(recs.Select(x => x.Clave ?? ""));

            // La última decisión de cada situación: una pospuesta que después se aceptó ya no cuenta. Solo las de
            // situaciones que el motor sigue recomendando HOY a este usuario (una de una regla retirada, o que el
            // usuario ya no puede ejecutar, no se avisa).
            var vencidas = (historial ?? Enumerable.Empty<BE.DecisionComercial>())
                .Where(d => d != null)
                .GroupBy(d => d.ClaveRecomendacion ?? "")
                .Select(g => g.OrderByDescending(d => d.Fecha).ThenByDescending(d => d.IdDecision).First())
                .Where(d => d.Estado == BE.EstadoDecision.Pospuesta && d.PospuestaHasta.HasValue && d.PospuestaHasta.Value.Date <= hoy.Date
                            && vigentes.Contains(d.ClaveRecomendacion ?? ""))
                .OrderBy(d => d.PospuestaHasta)
                .ToList();
            r.PospuestasVencidas = vencidas.Count;

            // Una recomendación alta que además es una pospuesta vencida se cuenta una sola vez (como pospuesta).
            var claveVencidas = new HashSet<string>(vencidas.Select(d => d.ClaveRecomendacion ?? ""));
            var altas = recs
                .Where(x => x.Prioridad == BE.PrioridadRecomendacion.Alta && !claveVencidas.Contains(x.Clave ?? "")
                            && DecisionesComerciales.DebeMostrarse(x, hoy, diasSilencioDescarte)
                            && DecisionesComerciales.PuedeDecidirse(x, hoy, diasSilencioDescarte, inicioAnalisis))
                .ToList();
            r.RecomendacionesAlta = altas.Count;

            var enRiesgo = (metas ?? Enumerable.Empty<BE.SeguimientoMeta>())
                .Where(m => m?.Meta != null && m.EsMesEnCurso && m.RequiereAccion).ToList();
            r.MetasEnRiesgo = enRiesgo.Count;

            foreach (var m in enRiesgo)
                r.Destacados.Add(new BE.TextoTraducible("aviso.inicio.destacado.meta", "Meta en riesgo: {0}", m.NombreIndicador));
            foreach (var x in altas)
                r.Destacados.Add(x.Titulo);
            foreach (var d in vencidas)
                r.Destacados.Add(new BE.TextoTraducible("aviso.inicio.destacado.pospuesta", "Pospuesta hasta el {0:d}: {1}", d.PospuestaHasta.Value, d.Titulo));
            r.Destacados = r.Destacados.Where(t => t != null).Take(MaximoDestacados).ToList();
            return r;
        }

        // ── "No mostrar hoy" ──────────────────────────────────────────────────────

        private static string Clave(int idUsuario) => idUsuario.ToString(CultureInfo.InvariantCulture);
        private string Hoy() => hoy().Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>¿El usuario pidió no ver los avisos hoy? (si no se puede leer, se avisa igual).</summary>
        public bool OcultoHoy(int idUsuario)
        {
            try
            {
                var valores = dal.ObtenerParametros(PrefijoOculto);
                return valores != null && valores.TryGetValue(Clave(idUsuario), out var v) && v == Hoy();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[BLL.AvisosComerciales] " + ex.Message);
                return false;
            }
        }

        /// <summary>Recuerda que el usuario no quiere ver los avisos por el resto del día.</summary>
        public void OcultarHoy(string modulo, int idUsuario)
        {
            PermisosAccion.Exigir(BE.Patentes.SugerenciaPromocion, BE.Patentes.SugerenciaPromocion);
            dal.GuardarParametros(PrefijoOculto, new Dictionary<string, string> { [Clave(idUsuario)] = Hoy() });
            try { bitacora.Registrar(modulo, $"Avisos del Centro de decisiones ocultos por hoy ({Hoy()})", BE.Criticidad.Baja); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.AvisosComerciales] " + ex.Message); }
        }
    }
}
