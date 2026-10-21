using System;
using System.Windows.Forms;
using Servicios;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Números escritos por el usuario (importes, umbrales, metas, simulador): los lee
    /// <see cref="LectorNumeros"/> (coma decimal; punto solo para miles en grupos de tres) y, si el
    /// texto es ambiguo, el mensaje traducido que explica cómo escribirlo. Nunca se lee "21600.50"
    /// como 2.160.050 ni "2.5" como 25.
    /// </summary>
    public static class EntradaNumerica
    {
        /// <summary>
        /// Lee el texto. Si no es un número sin ambigüedad, devuelve false y en <paramref name="mensaje"/>
        /// el motivo traducido (vacío o mal escrito: <paramref name="mensajeVacio"/>, el mensaje propio de la pantalla).
        /// </summary>
        public static bool TryLeer(string texto, string mensajeVacio, out decimal valor, out string mensaje)
        {
            if (LectorNumeros.TryLeer(texto, out valor, out var error)) { mensaje = null; return true; }
            mensaje = Mensaje(error, mensajeVacio);
            return false;
        }

        /// <summary>Mensaje traducido de un error de lectura.</summary>
        public static string Mensaje(ErrorNumero error, string mensajeVacio = null)
        {
            switch (error)
            {
                case ErrorNumero.UsarComaDecimal:
                    return Tr("num.invalido.coma", "Use la coma como separador decimal (por ejemplo, 21600,50): el punto solo separa los miles.");
                case ErrorNumero.Formato:
                    return Tr("num.invalido.formato", "Número no válido: use la coma para los decimales y el punto solo para separar los miles en grupos de tres (por ejemplo, 21.600,50).");
                default:
                    return mensajeVacio ?? Tr("cdc.umbrales.numero", "Ingrese un número.");
            }
        }

        private static string Tr(string clave, string fallback) =>
            Traductor.Resolver(clave, fallback, null, GestorIdioma.IdiomaActual);
    }

    /// <summary>
    /// NumericUpDown que lee lo escrito con <see cref="LectorNumeros"/> en vez de decimal.Parse con la
    /// cultura del hilo (que en es-AR toma "2.5" como 25). Un texto ambiguo NO se convierte: el control
    /// vuelve al valor anterior y avisa con <see cref="EntradaInvalida"/> (el mensaje ya traducido), para
    /// que la pantalla lo muestre en su barra de mensajes.
    /// </summary>
    public class NumericUpDownLocal : NumericUpDown
    {
        /// <summary>Lo escrito no es un número sin ambigüedad: el argumento es el mensaje traducido.</summary>
        public event EventHandler<string> EntradaInvalida;

        protected override void ValidateEditText()
        {
            string texto = Text;
            if (!UserEdit || Hexadecimal || string.IsNullOrWhiteSpace(texto) || texto.Trim() == "-")
            {
                base.ValidateEditText();
                return;
            }
            // UserEdit en false ANTES de tocar Value: si no, un ValueChanged que lea Value volvería a entrar acá.
            UserEdit = false;
            if (LectorNumeros.TryLeer(texto, out var valor, out var error))
            {
                decimal acotado = Math.Min(Maximum, Math.Max(Minimum, Math.Round(valor, DecimalPlaces, MidpointRounding.AwayFromZero)));
                if (acotado != Value) Value = acotado;
                UpdateEditText();
                return;
            }
            UpdateEditText();                                   // vuelve a mostrar el valor anterior
            EntradaInvalida?.Invoke(this, EntradaNumerica.Mensaje(error));
        }
    }
}
