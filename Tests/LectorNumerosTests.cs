using Microsoft.VisualStudio.TestTools.UnitTesting;
using Servicios;

namespace Tests
{
    /// <summary>
    /// Números escritos por el usuario (Servicios.LectorNumeros): coma decimal y punto solo como separador
    /// de miles en grupos de tres. decimal.TryParse con es-AR leía "21600.50" como 2.160.050 y "2.5" como 25.
    /// </summary>
    [TestClass]
    public class LectorNumerosTests
    {
        private static void Acepta(string texto, decimal esperado)
        {
            Assert.IsTrue(LectorNumeros.TryLeer(texto, out var v, out var error), $"«{texto}» debería ser válido ({error}).");
            Assert.AreEqual(esperado, v, $"«{texto}»");
            Assert.AreEqual(ErrorNumero.Ninguno, error);
            Assert.AreEqual(esperado, LectorNumeros.Leer(texto));
        }

        private static void Rechaza(string texto, ErrorNumero motivo)
        {
            Assert.IsFalse(LectorNumeros.TryLeer(texto, out var v, out var error), $"«{texto}» no debería leerse (leyó {v}).");
            Assert.AreEqual(motivo, error, $"«{texto}»");
            Assert.AreEqual(0m, v);
            Assert.IsNull(LectorNumeros.Leer(texto));
        }

        [TestMethod]
        public void ComaDecimal_SinMiles() => Acepta("21600,50", 21600.50m);

        [TestMethod]
        public void PuntoDeMilesEnGruposDeTres_ConComaDecimal() => Acepta("21.600,50", 21600.50m);

        [TestMethod]
        public void Entero() => Acepta("21600", 21600m);

        [TestMethod]
        public void PuntoSeguidoDeDosDigitos_SinComa_SeRechazaPidiendoLaComa() => Rechaza("21600.50", ErrorNumero.UsarComaDecimal);

        [TestMethod]
        public void PuntoSeguidoDeUnDigito_SeRechazaPidiendoLaComa() => Rechaza("2.5", ErrorNumero.UsarComaDecimal);

        [TestMethod]
        public void ComaDecimalCorta() => Acepta("2,5", 2.5m);

        [TestMethod]
        public void VariosGruposDeMiles() => Acepta("1.234.567,89", 1234567.89m);

        [TestMethod]
        public void NegativoConMiles() => Acepta("-1.000", -1000m);

        [TestMethod]
        public void VacioOSoloEspacios_SeRechazan()
        {
            Rechaza("", ErrorNumero.Vacio);
            Rechaza("   ", ErrorNumero.Vacio);
            Rechaza("\t", ErrorNumero.Vacio);
            Rechaza(null, ErrorNumero.Vacio);
        }

        [TestMethod]
        public void EspaciosAlrededor_SeIgnoran() => Acepta("  21.600,50 ", 21600.50m);

        [TestMethod]
        public void PuntosMalAgrupados_SonAmbiguos_SeRechazan()
        {
            Rechaza("1.2345", ErrorNumero.Formato);        // grupo de 4
            Rechaza("1234.567", ErrorNumero.Formato);      // primer grupo de 4: ¿miles o decimales?
            Rechaza("21600.505", ErrorNumero.Formato);
            Rechaza("1.5,25", ErrorNumero.Formato);        // punto que no separa miles, con coma
            Rechaza("1.000.5", ErrorNumero.Formato);
            Rechaza(".5", ErrorNumero.UsarComaDecimal);
        }

        [TestMethod]
        public void OtrosTextosInvalidos_SeRechazan()
        {
            Rechaza("abc", ErrorNumero.Formato);
            Rechaza("1,2,3", ErrorNumero.Formato);
            Rechaza("-", ErrorNumero.Formato);
            Rechaza(",", ErrorNumero.Formato);
            Rechaza("1 000", ErrorNumero.Formato);
            Rechaza("$ 100", ErrorNumero.Formato);
            Rechaza("--5", ErrorNumero.Formato);
            Rechaza("99999999999999999999999999999999", ErrorNumero.Formato);   // fuera del rango de decimal
        }

        [TestMethod]
        public void CasosLimiteValidos()
        {
            Acepta(",5", 0.5m);
            Acepta("-2,5", -2.5m);
            Acepta("0", 0m);
            Acepta("999.999", 999999m);
        }

        [TestMethod]
        public void NoDependeDeLaCulturaDelHilo()
        {
            var anterior = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                Acepta("21.600,50", 21600.50m);
                Rechaza("21600.50", ErrorNumero.UsarComaDecimal);
            }
            finally { System.Threading.Thread.CurrentThread.CurrentCulture = anterior; }
        }
    }
}
