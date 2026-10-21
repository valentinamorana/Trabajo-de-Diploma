using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL.Inteligencia.Reglas
{
    /// <summary>
    /// Regla «Renovaciones en juego» → campaña de renovación con los contactos de esos clientes.
    ///
    /// Cuenta los clientes con la suscripción vencida (hasta 30 días) o por vencer dentro de DiasPorVencer
    /// días (los pausados no: no pagan mientras dura la pausa).
    ///   dinero en juego = Σ precio mensual de sus planes
    ///   impacto         = dinero en juego × TasaRecuperoCobroPct / 100
    /// Al aceptarla se exportan los contactos (CSV) para la campaña, la decisión queda Ejecutada y guarda a
    /// quiénes se contactó: el Historial mide después el resultado real = Σ precio del plan de los
    /// contactados que renovaron o cambiaron de plan después de la decisión (MedicionCampanias).
    /// Prioridad Alta si ya hay suscripciones vencidas.
    /// </summary>
    public class ReglaRenovaciones : ReglaBase
    {
        public const string Codigo = "Renovaciones";
        public override string Tipo => Codigo;

        /// <summary>Una suscripción vencida hace más de estos días ya no se considera "en juego".</summary>
        public const int DiasMaximoVencida = 30;

        public override IEnumerable<BE.Recomendacion> Evaluar(BE.ContextoComercial ctx)
        {
            int diasPorVencer = (int)U(ctx, CatalogoUmbrales.DiasPorVencer);
            var enJuego = ctx.Vencimientos
                .Where(v => v.DiasRestantes <= diasPorVencer && v.DiasRestantes >= -DiasMaximoVencida)
                .OrderBy(v => v.DiasRestantes).ToList();
            if (enJuego.Count == 0) yield break;

            int vencidas = enJuego.Count(v => v.Vencida);
            decimal monto = enJuego.Sum(v => v.Precio);
            decimal recupero = U(ctx, CatalogoUmbrales.TasaRecuperoCobroPct);
            decimal impacto = Math.Round(monto * recupero / 100m, 2);

            var parametros = new BE.ParametrosAccion
            {
                Ids = enJuego.Select(v => v.IdCliente).ToList(),
                Contactos = enJuego
            };
            var rec = new BE.Recomendacion
            {
                TipoRegla = Codigo,
                Clave = Codigo,
                Prioridad = vencidas > 0 ? BE.PrioridadRecomendacion.Alta : BE.PrioridadRecomendacion.Media,
                Confianza = BE.NivelConfianza.Media,
                Titulo = T("rec.renov.titulo", "Gestionar la renovación de {0} cliente(s) ({1:C2} en juego)", enJuego.Count, monto),
                Diagnostico = T("rec.renov.diag",
                    "{0} suscripción(es) vencida(s) y {1} que vence(n) en los próximos {2} días. Representan {3:C2} de ingreso mensual.",
                    vencidas, enJuego.Count - vencidas, diasPorVencer, monto),
                Motivo = T("rec.renov.motivo",
                    "Una renovación gestionada antes del vencimiento se recupera mucho más que una ya perdida; se estima recuperar el {0:0} % del dinero en juego.",
                    recupero),
                Detalle = enJuego.Select(v => $"{v.Nombre} — {v.NombrePlan} — {v.FechaVencimiento:dd/MM/yyyy} ({v.DiasRestantes:+0;-0;0} d) — {v.Precio:C2}").ToList()
            };
            rec.Principal = Opcion(
                T("rec.renov.opcion", "Campaña de renovación: exportar los contactos de los {0} cliente(s) para llamarlos o escribirles antes de perderlos", enJuego.Count),
                impacto, T("rec.formula.renov", "{0:C2} en juego × {1:0} % de recupero = {2:C2}", monto, recupero, impacto),
                BE.AccionRecomendacion.ExportarContactos, parametros);
            yield return rec;
        }
    }
}
