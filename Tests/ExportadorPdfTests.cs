using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using GUI.Exportacion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// GUI.Exportacion.ExportadorPdf (1.6.1): PDF reales con iTextSharp, sin diálogo.
    ///   • Sin la NullReferenceException de iTextSharp 5.5.13.3 (Version.GetInstance buscaba en cada PDF la
    ///     licencia comercial y lanzaba/atrapaba una NRE: con el depurador se veía en «using (var doc …)»).
    ///   • Si la generación falla a mitad, no queda un PDF a medias ni el archivo abierto, se conserva el PDF
    ///     anterior con ese nombre y llega la excepción original.
    ///   • Si el destino está abierto en otro programa: AppException err.pdf.enuso (mensaje estándar).
    /// </summary>
    [TestClass]
    public class ExportadorPdfTests
    {
        private string _carpeta;

        [TestInitialize]
        public void Crear() => Directory.CreateDirectory(_carpeta = Path.Combine(Path.GetTempPath(), "wf_pdf_" + Guid.NewGuid().ToString("N")));

        [TestCleanup]
        public void Borrar() { try { Directory.Delete(_carpeta, true); } catch (IOException) { } }

        private static string T(string clave, string fallback) => fallback;

        private static byte[] Png()
        {
            using (var bmp = new Bitmap(120, 60))
            using (var g = Graphics.FromImage(bmp))
            using (var ms = new MemoryStream())
            {
                g.Clear(Color.MistyRose);
                g.DrawLine(Pens.DarkRed, 0, 59, 119, 0);
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        // Igual que el Informe ejecutivo: portada, tabla con nulos y textos largos, gráfico, sección vacía en página nueva.
        private static ReporteExportable InformeCompuesto(byte[] imagen)
        {
            var t = new DataTable();
            t.Columns.Add("A"); t.Columns.Add("B");
            t.Rows.Add("Básico", null);
            t.Rows.Add(new string('x', 3000), "Удержание клиентов");
            return new ReporteExportable
            {
                Titulo = "Informe ejecutivo: prueba",
                NombreArchivo = "Informe",
                Secciones = new System.Collections.Generic.List<SeccionReporte>
                {
                    new SeccionReporte { EsPortada = true, Titulo = "Informe", Parrafos = { "Período", null, "" } },
                    new SeccionReporte { Titulo = "Indicadores", PaginaNueva = true, Tabla = t, Encabezados = new[] { "Indicador" } },
                    new SeccionReporte { Titulo = "Gráfico", Imagen = imagen },
                    new SeccionReporte { Titulo = null, PaginaNueva = true },
                    new SeccionReporte { Titulo = "Glosario", Parrafos = { "Parámetros del análisis" } }
                }
            };
        }

        private static ReporteExportable Tabular()
        {
            var t = new DataTable();
            t.Columns.Add("Id"); t.Columns.Add("Texto");
            t.Rows.Add("1", "uno");
            return new ReporteExportable { Titulo = "Tabla", Encabezados = new[] { "Id", "Texto" }, Datos = t };
        }

        private static void AssertPdfCompleto(string ruta)
        {
            var bytes = File.ReadAllBytes(ruta);
            Assert.IsTrue(bytes.Length > 1000, "El PDF quedó vacío.");
            Assert.AreEqual("%PDF", Encoding.ASCII.GetString(bytes, 0, 4), "No empieza como un PDF.");
            string final = Encoding.ASCII.GetString(bytes, Math.Max(0, bytes.Length - 32), Math.Min(32, bytes.Length));
            Assert.IsTrue(final.Contains("%%EOF"), "El PDF no está cerrado (falta %%EOF).");
        }

        private void AssertSinTemporales() =>
            Assert.AreEqual(0, Directory.GetFiles(_carpeta, "*.tmp").Length, "Quedó un temporal en la carpeta.");

        [TestMethod]
        public void Generar_InformeCompuesto_TabularYTexto_NoLanzaLaNullReferenceDeITextSharp()
        {
            int nre = 0;
            EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> contar = (s, e) =>
            {
                if (e.Exception is NullReferenceException && e.Exception.TargetSite?.DeclaringType?.FullName == "iTextSharp.text.Version") nre++;
            };
            AppDomain.CurrentDomain.FirstChanceException += contar;
            try
            {
                ExportadorPdf.Generar(InformeCompuesto(Png()), Path.Combine(_carpeta, "informe.pdf"), T, "Prueba");
                ExportadorPdf.Generar(Tabular(), Path.Combine(_carpeta, "tabla.pdf"), T, "Prueba");
                ExportadorPdf.Generar(new ReporteExportable { Titulo = "Texto", TextoPlano = "Línea 1\r\n\r\nЛиния 3" },
                                      Path.Combine(_carpeta, "texto.pdf"), T, "Prueba");
            }
            finally { AppDomain.CurrentDomain.FirstChanceException -= contar; }

            Assert.AreEqual(0, nre, "iTextSharp volvió a lanzar NullReferenceException en Version.GetInstance (la versión no quedó guardada).");
            Assert.IsTrue(ExportadorPdf.FijarVersionITextSharp(), "Esta versión de iTextSharp ya no tiene los miembros que se fijan.");
            foreach (var f in new[] { "informe.pdf", "tabla.pdf", "texto.pdf" }) AssertPdfCompleto(Path.Combine(_carpeta, f));
            AssertSinTemporales();
        }

        [TestMethod]
        public void Generar_ElProducerSigueSiendoLaVersionAGPLDeITextSharp()
        {
            string ruta = Path.Combine(_carpeta, "producer.pdf");
            ExportadorPdf.Generar(Tabular(), ruta, T, "Prueba");
            // Por reflexión: el proyecto de pruebas no referencia iTextSharp directamente.
            var tipo = typeof(ExportadorPdf).Assembly.GetReferencedAssemblies()
                .Where(a => a.Name.Equals("itextsharp", StringComparison.OrdinalIgnoreCase))
                .Select(a => System.Reflection.Assembly.Load(a).GetType("iTextSharp.text.Version")).Single();
            string agpl = (string)tipo.GetField("AGPL").GetValue(null);
            object version = tipo.GetMethod("GetInstance").Invoke(null, null);
            string texto = (string)tipo.GetProperty("GetVersion").GetValue(version);
            Assert.IsTrue(texto.StartsWith("iTextSharp") && texto.Contains(agpl),
                "La versión guardada debe ser la misma que arma iTextSharp sin licencia comercial: " + texto);
        }

        [TestMethod]
        public void Generar_SiFallaAMitad_PropagaLaExcepcionOriginal_YConservaElPdfAnterior_SinTemporalNiArchivoAbierto()
        {
            string ruta = Path.Combine(_carpeta, "informe.pdf");
            File.WriteAllText(ruta, "PDF anterior");
            Exception error = null;
            try { ExportadorPdf.Generar(InformeCompuesto(new byte[] { 1, 2, 3, 4, 5 }), ruta, T, "Prueba"); }
            catch (Exception ex) { error = ex; }

            Assert.IsNotNull(error, "Una imagen inválida debía hacer fallar la generación.");
            Assert.IsNotInstanceOfType(error, typeof(NullReferenceException), "Cerrar el documento roto tapó la excepción original: " + error);
            Assert.IsNotInstanceOfType(error, typeof(ObjectDisposedException), "Cerrar el documento roto tapó la excepción original: " + error);
            Assert.AreEqual("PDF anterior", File.ReadAllText(ruta), "El PDF anterior con ese nombre no debía perderse.");
            AssertSinTemporales();
            File.Delete(ruta);   // sin handles abiertos: se puede borrar
        }

        [TestMethod]
        public void Generar_DestinoAbiertoEnOtroPrograma_LanzaAppExceptionEnUso_YNoDejaTemporal()
        {
            string ruta = Path.Combine(_carpeta, "abierto.pdf");
            using (new FileStream(ruta, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                try { ExportadorPdf.Generar(Tabular(), ruta, T, "Prueba"); Assert.Fail("Debía avisar que el archivo está en uso."); }
                catch (BE.AppException ex)
                {
                    Assert.AreEqual("err.pdf.enuso", ex.Clave);
                    Assert.IsTrue(ex.Message.Contains(ruta), "El mensaje nombra el archivo.");
                }
            }
            AssertSinTemporales();
        }

        [TestMethod]
        public void Generar_ReemplazaUnPdfAnteriorConElMismoNombre()
        {
            string ruta = Path.Combine(_carpeta, "informe.pdf");
            File.WriteAllText(ruta, "viejo");
            ExportadorPdf.Generar(InformeCompuesto(Png()), ruta, T, "Prueba");
            AssertPdfCompleto(ruta);
            AssertSinTemporales();
        }

        [TestMethod]
        public void MensajeEnUso_EnLosCuatroIdiomas()
        {
            foreach (var idioma in new[] { "ES", "EN", "RU", "PT" })
            {
                var t = Servicios.Multiidioma.Traductor.ObtenerTraduccionesHardcode(new Servicios.Multiidioma.Idioma { Id = idioma });
                Assert.IsTrue(t.ContainsKey("err.pdf.enuso") && t["err.pdf.enuso"].Texto.Contains("{0}"), "Falta err.pdf.enuso en " + idioma);
            }
        }
    }
}
