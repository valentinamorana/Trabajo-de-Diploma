using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Alertas leídas por usuario (1.6.1). Las alertas del Centro de Alertas no se guardan: BLL.PanelAlertas
    /// las recalcula cada vez a partir del estado del sistema. Por eso «marcar como leída» guarda, por
    /// usuario, la FIRMA de la situación que vio (clave de la alerta + cantidad + parámetros) en
    /// ParametroSistema, con la clave «Alerta.Leida.&lt;IdUsuario&gt;.&lt;clave de la alerta&gt;».
    ///   · Mientras la situación no cambie, la alerta queda leída: no cuenta en el aviso «Alertas (N)» del
    ///     menú y no se lista (salvo que se pidan las leídas).
    ///   · Si cambia (otra cantidad, otro valor), la firma ya no coincide y vuelve a notificarse.
    ///   · Si la alerta desaparece, su marca se borra: si la situación vuelve más adelante, se notifica de
    ///     nuevo aunque tenga la misma cantidad.
    /// Es una preferencia de lectura de cada usuario (no un dato del negocio): no lleva dígitos verificadores,
    /// igual que el «No mostrar hoy» de los avisos comerciales. Sin tabla nueva.
    /// </summary>
    public class AlertasLeidas
    {
        public const string Prefijo = "Alerta.Leida.";

        private readonly DAL.Interfaces.IParametrosSistemaDAL dal;

        public AlertasLeidas() : this(new DAL.InteligenciaComercial()) { }

        public AlertasLeidas(DAL.Interfaces.IParametrosSistemaDAL dal)
        {
            this.dal = dal ?? throw new ArgumentNullException(nameof(dal));
        }

        private static string PrefijoDe(int idUsuario) => Prefijo + idUsuario.ToString(CultureInfo.InvariantCulture) + ".";

        /// <summary>Firma de la situación de una alerta (puro): cambia si cambia la cantidad o un parámetro.</summary>
        public static string Firma(BE.Alerta alerta)
        {
            if (alerta == null) return string.Empty;
            var partes = (alerta.Parametros ?? new object[0])
                .Select(p => Convert.ToString(p, CultureInfo.InvariantCulture) ?? string.Empty);
            string firma = alerta.Cantidad.ToString(CultureInfo.InvariantCulture) + "|" + string.Join(";", partes);
            return firma.Length <= 400 ? firma : firma.Substring(0, 400);   // ParametroSistema.Valor es NVARCHAR(400)
        }

        /// <summary>¿La alerta está leída con esta misma situación? (puro).</summary>
        public static bool EstaLeida(BE.Alerta alerta, IDictionary<string, string> marcas) =>
            alerta != null && !string.IsNullOrEmpty(alerta.ClaveI18n) && marcas != null
            && marcas.TryGetValue(alerta.ClaveI18n, out var firma) && firma == Firma(alerta);

        /// <summary>
        /// Marca en cada alerta si el usuario ya la leyó (BE.Alerta.Leida) y borra las marcas de las alertas
        /// que ya no están. Si no se pueden leer las marcas, todas quedan como no leídas (nunca se esconde una
        /// alerta por un error).
        /// </summary>
        public List<BE.Alerta> Aplicar(int idUsuario, IEnumerable<BE.Alerta> alertas)
        {
            var lista = (alertas ?? Enumerable.Empty<BE.Alerta>()).Where(a => a != null).ToList();
            Dictionary<string, string> marcas;
            try { marcas = dal.ObtenerParametros(PrefijoDe(idUsuario)) ?? new Dictionary<string, string>(); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[BLL.AlertasLeidas] " + ex.Message);
                foreach (var a in lista) a.Leida = false;
                return lista;
            }

            foreach (var a in lista) a.Leida = EstaLeida(a, marcas);

            var activas = new HashSet<string>(lista.Select(a => a.ClaveI18n ?? string.Empty), StringComparer.OrdinalIgnoreCase);
            var viejas = marcas.Where(m => !string.IsNullOrEmpty(m.Value) && !activas.Contains(m.Key))
                               .ToDictionary(m => m.Key, m => string.Empty);
            if (viejas.Count > 0)
            {
                try { dal.GuardarParametros(PrefijoDe(idUsuario), viejas); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.AlertasLeidas] " + ex.Message); }
            }
            return lista;
        }

        /// <summary>Marca como leídas (con su situación actual) las alertas dadas.</summary>
        public void MarcarLeidas(int idUsuario, IEnumerable<BE.Alerta> alertas)
        {
            var valores = (alertas ?? Enumerable.Empty<BE.Alerta>())
                .Where(a => a != null && !string.IsNullOrEmpty(a.ClaveI18n))
                .GroupBy(a => a.ClaveI18n, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => Firma(g.Last()), StringComparer.OrdinalIgnoreCase);
            if (valores.Count == 0) return;
            dal.GuardarParametros(PrefijoDe(idUsuario), valores);
            foreach (var a in alertas.Where(a => a != null && valores.ContainsKey(a.ClaveI18n ?? ""))) a.Leida = true;
        }

        /// <summary>Vuelve a marcar como no leída una alerta.</summary>
        public void MarcarNoLeida(int idUsuario, BE.Alerta alerta)
        {
            if (alerta == null || string.IsNullOrEmpty(alerta.ClaveI18n)) return;
            dal.GuardarParametros(PrefijoDe(idUsuario), new Dictionary<string, string> { [alerta.ClaveI18n] = string.Empty });
            alerta.Leida = false;
        }
    }
}
