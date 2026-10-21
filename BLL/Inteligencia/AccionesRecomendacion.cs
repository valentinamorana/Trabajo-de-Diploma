using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 «Aplicar» — Qué exige y qué hace cada acción de una recomendación aceptada. Toda opción del motor
    /// ejecuta una operación que el sistema ya tiene; acá se decide (pura, probada) qué patente de edición
    /// pide además de la del Centro de decisiones y a qué estado lleva las prendas.
    /// </summary>
    public static class AccionesRecomendacion
    {
        /// <summary>Patente de edición que exige la operación (null = alcanza con la del Centro de decisiones).</summary>
        public static string PatenteRequerida(BE.AccionRecomendacion accion)
        {
            switch (accion)
            {
                case BE.AccionRecomendacion.SugerirPromocion:
                case BE.AccionRecomendacion.ExtenderPromocion:
                    return BE.Patentes.SugerenciaPromocion;
                case BE.AccionRecomendacion.SolicitarBajaPromocion:
                    return BE.Patentes.PromocionesVigentesEditar;
                case BE.AccionRecomendacion.DarDeBajaPrendas:
                case BE.AccionRecomendacion.EnviarAMantenimiento:
                    return BE.Patentes.StockEditar;
                default:
                    return null;   // exportar contactos, orden de compra: documentos del propio Centro
            }
        }

        /// <summary>¿El usuario puede ejecutar la opción? (se pregunta ANTES de registrar la aceptación).</summary>
        public static bool PuedeEjecutar(BE.OpcionRecomendacion opcion, Func<string, bool> tienePermiso)
        {
            if (opcion == null) return false;
            string patente = PatenteRequerida(opcion.Accion);
            return patente == null || (tienePermiso != null && tienePermiso(patente));
        }

        /// <summary>Estado al que la acción lleva las prendas (null si la acción no toca prendas).</summary>
        public static BE.EstadoPrenda? EstadoDestino(BE.AccionRecomendacion accion)
        {
            switch (accion)
            {
                case BE.AccionRecomendacion.DarDeBajaPrendas: return BE.EstadoPrenda.Baja;
                case BE.AccionRecomendacion.EnviarAMantenimiento: return BE.EstadoPrenda.EnLimpieza;
                default: return null;
            }
        }

        public static bool TocaPrendas(BE.AccionRecomendacion accion) => EstadoDestino(accion).HasValue;

        /// <summary>
        /// Lo que ve cada usuario: de cada recomendación quedan solo las opciones que PUEDE ejecutar; si la
        /// principal no es una de ellas, pasa a principal la de mayor impacto entre las que quedan, y una
        /// recomendación sin ninguna opción ejecutable no se muestra (tampoco cuenta en los avisos ni recibe la
        /// prioridad de las metas, que se aplican después de este filtro). Modifica las recomendaciones recibidas
        /// (son las del análisis de este usuario) y devuelve las que quedan, en el mismo orden.
        /// </summary>
        public static List<BE.Recomendacion> FiltrarPorPermiso(IEnumerable<BE.Recomendacion> recomendaciones, Func<string, bool> tienePermiso)
        {
            var resultado = new List<BE.Recomendacion>();
            foreach (var r in recomendaciones ?? Enumerable.Empty<BE.Recomendacion>())
            {
                if (r == null) continue;
                var ejecutables = r.Opciones().Where(o => PuedeEjecutar(o, tienePermiso)).ToList();
                if (ejecutables.Count == 0) continue;
                if (ejecutables.Count != r.Opciones().Count)
                {
                    var principal = ejecutables.Contains(r.Principal) ? r.Principal : ejecutables.OrderByDescending(o => o.ImpactoEstimado).First();
                    r.Principal = principal;
                    r.Alternativas = ejecutables.Where(o => !ReferenceEquals(o, principal)).ToList();
                }
                resultado.Add(r);
            }
            return resultado;
        }
    }

    /// <summary>Qué prendas de la recomendación se pueden llevar al estado pedido y cuáles no (con su estado actual).</summary>
    public sealed class PlanAccionPrendas
    {
        public BE.EstadoPrenda Destino { get; set; }
        public List<BE.Prenda> Aplicables { get; } = new List<BE.Prenda>();
        /// <summary>Prendas que hoy no admiten el cambio manual (en uso, ya de baja, pendientes de inspección…).</summary>
        public List<BE.Prenda> NoAplicables { get; } = new List<BE.Prenda>();
        /// <summary>IDs de la recomendación que ya no existen en el catálogo.</summary>
        public List<int> NoEncontradas { get; } = new List<int>();
    }

    /// <summary>Resultado de aplicar el cambio de estado: las que cambiaron y las que fallaron (con el motivo).</summary>
    public sealed class ResultadoAccionPrendas
    {
        public List<int> Aplicadas { get; } = new List<int>();
        public List<(BE.Prenda Prenda, Exception Error)> Fallidas { get; } = new List<(BE.Prenda, Exception)>();
        /// <summary>true si la decisión quedó Ejecutada con el detalle de lo aplicado.</summary>
        public bool Registrada { get; internal set; }
        /// <summary>Por qué no se pudo registrar la decisión (p. ej. err.bll.decision.concurrente); null si se registró.</summary>
        public Exception ErrorRegistro { get; internal set; }
        /// <summary>Lo que cortó el recorrido antes de terminar (no debería pasar: cada prenda se protege sola).</summary>
        public Exception Interrupcion { get; internal set; }

        /// <summary>Nota para la decisión: qué prendas no se cambiaron y por qué (en español, como la bitácora).</summary>
        public string NotaFallidas()
        {
            if (Fallidas.Count == 0) return null;
            string texto = "No se cambiaron: " + string.Join("; ", Fallidas.Select(f =>
                $"#{f.Prenda?.IdPrenda} ({(f.Error is BE.AppException ? f.Error.Message : "error inesperado")})"));
            return texto.Length <= 480 ? texto : texto.Substring(0, 479) + "…";
        }
    }

    /// <summary>
    /// Ejecuta sobre las prendas las acciones "dar de baja" y "mandar a revisión" con la operación existente
    /// del catálogo (BLL.Prenda.CambiarEstado): ella valida la transición (patrón State y las barreras de PN04),
    /// exige la patente de edición de stock, abre el mantenimiento al entrar en limpieza y deja la bitácora del
    /// sistema y la de negocio de cada prenda. Acá solo se arma el plan (qué se puede tocar hoy) y se recorre.
    /// </summary>
    public class AccionesPrendas
    {
        private readonly Interfaces.IPrendaService prendas;

        public AccionesPrendas() : this(new Prenda()) { }

        public AccionesPrendas(Interfaces.IPrendaService prendas)
        {
            this.prendas = prendas ?? throw new ArgumentNullException(nameof(prendas));
        }

        /// <summary>Relee las prendas del catálogo (no confía en el análisis) y separa las que admiten el cambio.</summary>
        public PlanAccionPrendas Planificar(BE.AccionRecomendacion accion, IEnumerable<int> ids)
        {
            var destino = AccionesRecomendacion.EstadoDestino(accion)
                ?? throw new BE.AppException("err.bll.accion.noprendas", "La acción elegida no cambia el estado de prendas.");
            var pedidas = (ids ?? Enumerable.Empty<int>()).Distinct().ToList();
            var catalogo = (prendas.ObtenerTodos() ?? new List<BE.Prenda>()).Where(p => pedidas.Contains(p.IdPrenda)).ToList();
            return Clasificar(destino, pedidas, catalogo, p => prendas.ObtenerTransicionesManuales(p));
        }

        /// <summary>Clasificación pura: aplicable si el cambio manual al destino está permitido para la prenda.</summary>
        public static PlanAccionPrendas Clasificar(BE.EstadoPrenda destino, IEnumerable<int> ids, IEnumerable<BE.Prenda> catalogo,
                                                   Func<BE.Prenda, List<BE.EstadoPrenda>> transiciones)
        {
            var plan = new PlanAccionPrendas { Destino = destino };
            var porId = (catalogo ?? Enumerable.Empty<BE.Prenda>()).GroupBy(p => p.IdPrenda).ToDictionary(g => g.Key, g => g.First());
            foreach (int id in (ids ?? Enumerable.Empty<int>()).Distinct())
            {
                if (!porId.TryGetValue(id, out var p)) { plan.NoEncontradas.Add(id); continue; }
                bool permitida = p.Estado != destino && (transiciones?.Invoke(p) ?? new List<BE.EstadoPrenda>()).Contains(destino);
                (permitida ? plan.Aplicables : plan.NoAplicables).Add(p);
            }
            return plan;
        }

        /// <summary>
        /// Aplica el cambio a las prendas del plan. Cada cambio se confirma solo (BLL.Prenda.CambiarEstado), así
        /// que una prenda que falla, por la causa que sea, no corta a las demás: queda en Fallidas con su motivo.
        /// </summary>
        public ResultadoAccionPrendas Aplicar(string modulo, PlanAccionPrendas plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            // Antes de tocar nada: sin la patente de stock no se intenta ninguna (y el error es claro).
            PermisosAccion.Exigir(BE.Patentes.StockEditar, BE.Patentes.Stock);
            var resultado = new ResultadoAccionPrendas();
            Recorrer(modulo, plan, resultado);
            return resultado;
        }

        private void Recorrer(string modulo, PlanAccionPrendas plan, ResultadoAccionPrendas resultado)
        {
            foreach (var p in plan.Aplicables)
            {
                try
                {
                    prendas.CambiarEstado(modulo, p, plan.Destino);
                    resultado.Aplicadas.Add(p.IdPrenda);
                }
                catch (Exception ex)
                {
                    if (!(ex is BE.AppException)) System.Diagnostics.Trace.TraceError("[BLL.AccionesPrendas] Prenda " + p.IdPrenda + ": " + ex);
                    resultado.Fallidas.Add((p, ex));
                }
            }
        }

        /// <summary>
        /// Aplica el plan y SIEMPRE deja registrado lo que realmente cambió: en un finally, si se cambió al menos
        /// una prenda, la decisión pasa a Ejecutada con esas prendas en su detalle (una sola transacción) y las que
        /// no se cambiaron quedan anotadas con su motivo. Si el registro falla (otra sesión cambió la decisión),
        /// lo cambiado no se pierde de vista: queda en el resultado (Aplicadas + ErrorRegistro) y en la bitácora
        /// de cada prenda. Un error que interrumpa el recorrido se informa en Interrupcion.
        /// </summary>
        public ResultadoAccionPrendas EjecutarDecision(string modulo, PlanAccionPrendas plan, int idDecision, DecisionesComerciales decisiones)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (decisiones == null) throw new ArgumentNullException(nameof(decisiones));
            PermisosAccion.Exigir(BE.Patentes.StockEditar, BE.Patentes.Stock);
            var resultado = new ResultadoAccionPrendas();
            try { Recorrer(modulo, plan, resultado); }
            catch (Exception ex) { resultado.Interrupcion = ex; }
            finally
            {
                if (resultado.Aplicadas.Count > 0)
                {
                    try
                    {
                        decisiones.MarcarEjecutadaConDetalle(modulo, idDecision, BE.DecisionComercial.DetallePrenda,
                            resultado.Aplicadas, resultado.NotaFallidas());
                        resultado.Registrada = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError($"[BLL.AccionesPrendas] Decisión #{idDecision}: prendas cambiadas {string.Join(", ", resultado.Aplicadas)} sin registrar: {ex.Message}");
                        resultado.ErrorRegistro = ex;
                    }
                }
            }
            return resultado;
        }
    }
}
