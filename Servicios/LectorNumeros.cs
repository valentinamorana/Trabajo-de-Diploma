using System.Globalization;
using System.Text.RegularExpressions;

namespace Servicios
{
    /// <summary>Por qué un texto no se pudo leer como número (ver <see cref="LectorNumeros"/>).</summary>
    public enum ErrorNumero
    {
        Ninguno = 0,
        /// <summary>Vacío o solo espacios.</summary>
        Vacio = 1,
        /// <summary>Un punto seguido de 1 o 2 dígitos y sin coma ("21600.50", "2.5"): el usuario quiso decimales.</summary>
        UsarComaDecimal = 2,
        /// <summary>Cualquier otro texto que no es un número sin ambigüedad (puntos mal agrupados, letras, dos comas…).</summary>
        Formato = 3
    }

    /// <summary>
    /// Lee números escritos por el usuario con el formato del negocio (es-AR): la coma es el separador
    /// decimal y el punto, el de miles. decimal.TryParse con la cultura es-AR no valida la posición de
    /// los puntos ("21600.50" daba 2.160.050 y "2.5" daba 25), así que acá:
    ///   · la coma (una sola) separa los decimales: "21600,50", "2,5", ",5";
    ///   · el punto se acepta SOLO como separador de miles en grupos de tres: "21.600,50", "1.234.567,89";
    ///   · un punto seguido de 1 o 2 dígitos y sin coma ("21600.50", "2.5") es <see cref="ErrorNumero.UsarComaDecimal"/>;
    ///   · cualquier otro uso del punto ("1.2345", "1234.567") es <see cref="ErrorNumero.Formato"/>.
    /// Nunca adivina: un texto ambiguo se rechaza y la interfaz explica cómo escribirlo. Puro (sin la cultura del hilo).
    /// </summary>
    public static class LectorNumeros
    {
        private static readonly Regex Numero = new Regex(@"^(?<signo>-)?(?<entero>\d*(?:\.\d+)*)(?:,(?<dec>\d+))?$", RegexOptions.CultureInvariant);
        private static readonly Regex EnteroConMiles = new Regex(@"^\d{1,3}(?:\.\d{3})+$", RegexOptions.CultureInvariant);
        private static readonly Regex PuntoDecimal = new Regex(@"^-?\d*\.\d{1,2}$", RegexOptions.CultureInvariant);

        /// <summary>Lee <paramref name="texto"/>; si no es un número sin ambigüedad devuelve false y el motivo.</summary>
        public static bool TryLeer(string texto, out decimal valor, out ErrorNumero error)
        {
            valor = 0m;
            string t = (texto ?? string.Empty).Trim();
            if (t.Length == 0) { error = ErrorNumero.Vacio; return false; }

            var m = Numero.Match(t);
            string entero = m.Success ? m.Groups["entero"].Value : null;
            bool conDecimales = m.Success && m.Groups["dec"].Success;
            if (!m.Success || (entero.Length == 0 && !conDecimales))
            {
                error = PuntoDecimal.IsMatch(t) ? ErrorNumero.UsarComaDecimal : ErrorNumero.Formato;
                return false;
            }
            if (entero.Contains("."))
            {
                if (!EnteroConMiles.IsMatch(entero))
                {
                    error = !conDecimales && PuntoDecimal.IsMatch(t) ? ErrorNumero.UsarComaDecimal : ErrorNumero.Formato;
                    return false;
                }
                entero = entero.Replace(".", string.Empty);
            }

            string canonico = m.Groups["signo"].Value + (entero.Length == 0 ? "0" : entero)
                              + (conDecimales ? "." + m.Groups["dec"].Value : string.Empty);
            if (!decimal.TryParse(canonico, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                                  CultureInfo.InvariantCulture, out valor))
            {
                valor = 0m;
                error = ErrorNumero.Formato;   // fuera del rango de decimal
                return false;
            }
            error = ErrorNumero.Ninguno;
            return true;
        }

        /// <summary>Atajo: el número, o null si el texto no es un número sin ambigüedad.</summary>
        public static decimal? Leer(string texto) => TryLeer(texto, out var v, out _) ? v : (decimal?)null;
    }
}
