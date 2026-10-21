using System.Linq;

namespace BE
{
    /// <summary>
    /// PN03 — Texto que arma la BLL (diagnóstico, motivo, interpretación de un KPI) y que la GUI
    /// muestra en el idioma activo: clave de traducción + texto en español (fallback) + argumentos
    /// para string.Format. Mismo criterio que ClienteEnRiesgo.Clave/Motivo/Args, pero en un objeto
    /// para poder anidarlo (un argumento puede ser otro TextoTraducible, por ejemplo el nombre de
    /// un KPI dentro de su interpretación).
    ///
    /// <see cref="Espanol"/> es lo que se guarda en la base (DecisionComercial) y en la bitácora:
    /// los registros quedan en el idioma del sistema, igual que el resto de la bitácora.
    /// </summary>
    public class TextoTraducible
    {
        public string Clave { get; set; }
        public string Fallback { get; set; }
        public object[] Args { get; set; }

        public TextoTraducible() { }

        public TextoTraducible(string clave, string fallback, params object[] args)
        {
            Clave = clave;
            Fallback = fallback;
            Args = args;
        }

        /// <summary>Texto en español con los argumentos ya aplicados (los anidados también en español).</summary>
        public string Espanol => Formatear(Fallback, Args?.Select(a => a is TextoTraducible t ? (object)t.Espanol : a).ToArray());

        public override string ToString() => Espanol;

        /// <summary>string.Format tolerante: sin argumentos devuelve el texto tal cual; si el formato no
        /// coincide con los argumentos, no rompe (devuelve el texto sin formatear).</summary>
        public static string Formatear(string texto, object[] args)
        {
            if (texto == null) return string.Empty;
            if (args == null || args.Length == 0) return texto;
            try { return string.Format(texto, args); }
            catch (System.FormatException) { return texto; }
        }
    }
}
