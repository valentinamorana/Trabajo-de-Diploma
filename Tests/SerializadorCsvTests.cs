using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Servicios.Exportacion;

namespace Tests
{
    /// <summary>Pruebas del serializador CSV (RFC 4180) — núcleo de la exportación CSV.</summary>
    [TestClass]
    public class SerializadorCsvTests
    {
        private static string[] Lineas(string csv) =>
            csv.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

        private static DataTable Tabla(string[] columnas, params object[][] filas)
        {
            var dt = new DataTable();
            foreach (var c in columnas) dt.Columns.Add(c);
            foreach (var f in filas) dt.Rows.Add(f);
            return dt;
        }

        [TestMethod]
        public void Csv_EncabezadosYFilas_SeparadosPorPuntoYComa()
        {
            var dt  = Tabla(new[] { "c1", "c2" }, new object[] { "a", "b" }, new object[] { "c", "d" });
            var csv = SerializadorCsv.Generar(new[] { "Col 1", "Col 2" }, dt);

            var l = Lineas(csv);
            Assert.AreEqual(3, l.Length);          // encabezado + 2 filas
            Assert.AreEqual("Col 1;Col 2", l[0]);  // usa los encabezados pasados, no los de la tabla
            Assert.AreEqual("a;b", l[1]);
            Assert.AreEqual("c;d", l[2]);
        }

        [TestMethod]
        public void Csv_CampoConSeparador_SeEntrecomilla()
        {
            var dt  = Tabla(new[] { "c1" }, new object[] { "uno;dos" });
            var csv = SerializadorCsv.Generar(new[] { "H" }, dt);
            Assert.AreEqual("\"uno;dos\"", Lineas(csv)[1]);
        }

        [TestMethod]
        public void Csv_CampoConComillas_DuplicaComillasYEntrecomilla()
        {
            var dt  = Tabla(new[] { "c1" }, new object[] { "he dijo \"hola\"" });
            var csv = SerializadorCsv.Generar(new[] { "H" }, dt);
            Assert.AreEqual("\"he dijo \"\"hola\"\"\"", Lineas(csv)[1]);
        }

        [TestMethod]
        public void Csv_CampoConSaltoDeLinea_SeEntrecomilla()
        {
            var dt  = Tabla(new[] { "c1" }, new object[] { "linea1\nlinea2" });
            var csv = SerializadorCsv.Generar(new[] { "H" }, dt);
            Assert.IsTrue(csv.Contains("\"linea1\nlinea2\""));
        }

        [TestMethod]
        public void Csv_CampoSimple_NoSeEntrecomilla()
        {
            var dt  = Tabla(new[] { "c1" }, new object[] { "simple" });
            var csv = SerializadorCsv.Generar(new[] { "H" }, dt);
            Assert.AreEqual("simple", Lineas(csv)[1]);
        }

        [TestMethod]
        public void Csv_TablaVacia_SoloEncabezado()
        {
            var dt  = Tabla(new[] { "c1", "c2" });
            var csv = SerializadorCsv.Generar(new[] { "A", "B" }, dt);
            Assert.AreEqual(1, Lineas(csv).Length);
            Assert.AreEqual("A;B", Lineas(csv)[0]);
        }

        [TestMethod]
        public void Csv_CeldaNula_QuedaVacia()
        {
            var dt = new DataTable();
            dt.Columns.Add("c1");
            dt.Columns.Add("c2");
            dt.Rows.Add("x", System.DBNull.Value);
            var csv = SerializadorCsv.Generar(new[] { "A", "B" }, dt);
            Assert.AreEqual("x;", Lineas(csv)[1]);
        }

        // ── Mitigación de CSV/Formula Injection (CWE-1236) ───────────────────────

        [TestMethod]
        public void Csv_CampoEmpiezaConIgual_SeNeutralizaConApostrofe()
        {
            // Sin comillas internas a propósito: un campo con '"' se entrecomilla por RFC 4180
            // (ver Csv_CampoConComillas_*), lo que movería el apóstrofe agregado adentro de las
            // comillas — este test aísla solo la neutralización del prefijo de fórmula.
            var dt  = Tabla(new[] { "c1" }, new object[] { "=HYPERLINK(http://evil,click)" });
            var csv = SerializadorCsv.Generar(new[] { "H" }, dt);
            Assert.IsTrue(Lineas(csv)[1].StartsWith("'="), "Debe anteponer un apóstrofe al '=' inicial.");
        }

        [TestMethod]
        public void Csv_CampoEmpiezaConArroba_SeNeutralizaConApostrofe()
        {
            var dt  = Tabla(new[] { "c1" }, new object[] { "@SUM(A1:A2)" });
            var csv = SerializadorCsv.Generar(new[] { "H" }, dt);
            Assert.IsTrue(Lineas(csv)[1].StartsWith("'@"));
        }

        [TestMethod]
        public void Csv_CampoEmpiezaConMasOMenos_SeNeutralizaConApostrofe()
        {
            var dt  = Tabla(new[] { "c1", "c2" }, new object[] { "+1234", "-5678" });
            var csv = SerializadorCsv.Generar(new[] { "H1", "H2" }, dt);
            var celdas = Lineas(csv)[1].Split(';');
            Assert.IsTrue(celdas[0].StartsWith("'+"));
            Assert.IsTrue(celdas[1].StartsWith("'-"));
        }

        [TestMethod]
        public void Csv_MontoNegativoNormal_NoDeberiaConfundirseConFormula_PeroSeNeutralizaIgual()
        {
            // Nota: un número negativo legítimo ("-500") también empieza con '-' y se neutraliza
            // igual que una fórmula — es el mismo trade-off que aplican las librerías CSV modernas
            // (falso positivo aceptable frente al riesgo de ejecución de fórmula).
            var dt  = Tabla(new[] { "c1" }, new object[] { "-500" });
            var csv = SerializadorCsv.Generar(new[] { "H" }, dt);
            Assert.AreEqual("'-500", Lineas(csv)[1]);
        }

        [TestMethod]
        public void Csv_CampoNoEmpiezaConCaracterDeFormula_QuedaIntacto()
        {
            var dt  = Tabla(new[] { "c1" }, new object[] { "Remera roja" });
            var csv = SerializadorCsv.Generar(new[] { "H" }, dt);
            Assert.AreEqual("Remera roja", Lineas(csv)[1]);
        }
    }
}
