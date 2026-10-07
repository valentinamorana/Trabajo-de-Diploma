using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Servicios.Multiidioma;
using Color = System.Drawing.Color;

namespace GUI.Exportacion
{
    /// <summary>
    /// Patrón FACTORY METHOD — rol "ConcreteProduct".
    ///
    /// Genera el archivo PDF directamente (iTextSharp), sin vista previa de impresión ni impresora
    /// virtual ("Microsoft Print to PDF" no está permitido en la Entrega 3). Pide dónde guardarlo,
    /// lo escribe y lo abre con el visor predeterminado. Sabe representar reportes TABULARES (grilla,
    /// ej. Bitácora) y de TEXTO (líneas monoespaciadas, ej. documentos de PN01–PN03), con el mismo
    /// encabezado y pie "Página N" en todos. Las fuentes de Windows se embeben (Identity-H), así que
    /// salen bien los acentos y el ruso.
    /// </summary>
    public class ExportadorPdf : Exportador
    {
        // Paleta vino del sistema.
        private static readonly BaseColor VinoOscuro = Convertir(Tema.RosaOscuro);
        private static readonly BaseColor VinoClaro  = Convertir(Tema.RosaPalido);
        private static readonly BaseColor VinoMedio  = Convertir(Tema.RosaTinta);
        private static readonly BaseColor Tinta      = Convertir(Tema.Tinta);
        private static readonly BaseColor Borde      = Convertir(Tema.Borde);

        public ExportadorPdf(string origen)
        {
            _formato = "PDF";
            _origen  = origen;
        }

        public override string Exportar(ReporteExportable reporte, IWin32Window propietario)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T(string k, string fb) => t.ContainsKey(k) ? t[k].Texto : fb;

            if (reporte.EsTabular && (reporte.Datos == null || reporte.Datos.Rows.Count == 0))
            {
                MessageBox.Show(propietario,
                    T("err.pdf.sinDatos", "No hay datos para exportar."),
                    T("lbl.exportarpdf",  "Exportar PDF"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            string ruta;
            using (var dlg = new SaveFileDialog
            {
                Title            = T("lbl.exportarpdf", "Exportar PDF"),
                Filter           = "PDF (*.pdf)|*.pdf",
                FileName         = NombreSeguro(reporte.NombreArchivo ?? reporte.Titulo ?? "Reporte") + ".pdf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                OverwritePrompt  = true
            })
            {
                if (dlg.ShowDialog(propietario) != DialogResult.OK) return null;
                ruta = dlg.FileName;
            }

            Generar(reporte, ruta, T);

            // Se abre con el visor de PDF del equipo (si no hay ninguno, el archivo igual quedó guardado).
            try { Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true }); }
            catch (Exception ex) { Trace.TraceWarning("[ExportadorPdf] No se pudo abrir el PDF: " + ex.Message); }
            return ruta;
        }

        // Escribe el PDF en 'ruta'. Público para poder generarlo sin diálogo (por ejemplo, en pruebas).
        public static void Generar(ReporteExportable reporte, string ruta, Func<string, string, string> T)
        {
            var tamanio = reporte.EsTabular ? PageSize.A4.Rotate() : PageSize.A4;   // tabular en horizontal
            using (var fs = new FileStream(ruta, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var doc = new Document(tamanio, 30, 30, 30, 40))
            {
                var writer = PdfWriter.GetInstance(doc, fs);
                writer.PageEvent = new PiePagina(string.Format(T("bit.pdf.pagina", "WardrobeFlow — Página {0}"), "{0}"));
                doc.AddTitle(reporte.Titulo ?? "");
                doc.AddCreator("WardrobeFlow");
                doc.Open();

                Encabezado(doc, reporte, T);
                if (reporte.EsTabular) Tabla(doc, reporte);
                else                   Texto(doc, reporte);

                doc.Close();
            }
        }

        // Barra de título + fecha de generación (+ cantidad de registros si es tabular).
        private static void Encabezado(Document doc, ReporteExportable reporte, Func<string, string, string> T)
        {
            var barra = new PdfPTable(1) { WidthPercentage = 100, SpacingAfter = 4 };
            barra.AddCell(new PdfPCell(new Phrase(reporte.Titulo ?? "", Fuente(Fuentes.TituloNegrita, 13, BaseColor.WHITE)))
            {
                BackgroundColor = VinoOscuro, Border = Rectangle.NO_BORDER,
                PaddingTop = 5, PaddingBottom = 7, PaddingLeft = 6
            });
            doc.Add(barra);

            string sub = $"{T("rpt.txt.generado", "Generado")}: {DateTime.Now:dd/MM/yyyy HH:mm}";
            if (reporte.EsTabular)
                sub += "   |   " + string.Format(T("msg.bit.registros", "{0} registro(s)"), reporte.Datos.Rows.Count);
            doc.Add(new Paragraph(sub, Fuente(Fuentes.Normal, 8, VinoMedio)) { SpacingAfter = 6 });
        }

        // Modo TABULAR: encabezados vino, filas alternadas, se repite el encabezado en cada página.
        private static void Tabla(Document doc, ReporteExportable reporte)
        {
            DataTable datos = reporte.Datos;
            int nCols = datos.Columns.Count;
            var tabla = new PdfPTable(nCols) { WidthPercentage = 100, HeaderRows = 1 };

            for (int c = 0; c < nCols; c++)
            {
                string nombre = reporte.Encabezados != null && c < reporte.Encabezados.Length
                    ? reporte.Encabezados[c] : datos.Columns[c].ColumnName;
                tabla.AddCell(new PdfPCell(new Phrase(nombre, Fuente(Fuentes.TituloNegrita, 8, BaseColor.WHITE)))
                {
                    BackgroundColor = VinoOscuro, BorderColor = VinoOscuro, Padding = 4
                });
            }

            bool alternar = false;
            foreach (DataRow fila in datos.Rows)
            {
                for (int c = 0; c < nCols; c++)
                    tabla.AddCell(new PdfPCell(new Phrase(fila[c]?.ToString() ?? "", Fuente(Fuentes.Normal, 7.5f, Tinta)))
                    {
                        BackgroundColor = alternar ? VinoClaro : BaseColor.WHITE,
                        BorderColor = Borde, BorderWidth = 0.5f, Padding = 3
                    });
                alternar = !alternar;
            }
            doc.Add(tabla);
        }

        // Modo TEXTO: líneas monoespaciadas (respeta la alineación de los documentos).
        private static void Texto(Document doc, ReporteExportable reporte)
        {
            var fuente = Fuente(Fuentes.Mono, 8.5f, Tinta);
            foreach (string linea in (reporte.TextoPlano ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
                doc.Add(new Paragraph(linea.Length == 0 ? " " : linea, fuente) { Leading = 11.5f });
        }

        // Pie "WardrobeFlow — Página N" con una línea, en todas las páginas.
        private sealed class PiePagina : PdfPageEventHelper
        {
            private readonly string _formato;
            public PiePagina(string formato) { _formato = formato; }

            public override void OnEndPage(PdfWriter writer, Document document)
            {
                var cb = writer.DirectContent;
                float y = document.BottomMargin - 14;
                cb.SetColorStroke(VinoOscuro);
                cb.SetLineWidth(1f);
                cb.MoveTo(document.LeftMargin, y + 10);
                cb.LineTo(document.PageSize.Width - document.RightMargin, y + 10);
                cb.Stroke();
                ColumnText.ShowTextAligned(cb, Element.ALIGN_LEFT,
                    new Phrase(_formato.Replace("{0}", writer.PageNumber.ToString()), Fuente(Fuentes.Normal, 8, VinoMedio)),
                    document.LeftMargin, y, 0);
            }
        }

        // ── Fuentes (embebidas, Unicode) ───────────────────────────────────────────
        private enum Fuentes { Normal, TituloNegrita, Mono }

        private static Font Fuente(Fuentes tipo, float tamanio, BaseColor color) =>
            new Font(Base(tipo), tamanio, Font.NORMAL, color);

        private static BaseFont _normal, _negrita, _mono;

        private static BaseFont Base(Fuentes tipo)
        {
            switch (tipo)
            {
                case Fuentes.TituloNegrita: return _negrita ?? (_negrita = Cargar("segoeuib.ttf", "arialbd.ttf"));
                case Fuentes.Mono:          return _mono    ?? (_mono    = Cargar("consola.ttf", "cour.ttf"));
                default:                    return _normal  ?? (_normal  = Cargar("segoeui.ttf", "arial.ttf"));
            }
        }

        // Fuente TrueType de Windows (con alternativa); si no hay ninguna, Helvetica (sin cirílico).
        private static BaseFont Cargar(params string[] archivos)
        {
            string carpeta = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            foreach (string a in archivos)
            {
                string ruta = Path.Combine(carpeta, a);
                if (File.Exists(ruta))
                    return BaseFont.CreateFont(ruta, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            }
            return BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
        }

        private static BaseColor Convertir(Color c) => new BaseColor(c.R, c.G, c.B);

        // Nombre de archivo sin caracteres inválidos.
        private static string NombreSeguro(string nombre)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) nombre = nombre.Replace(c, '_');
            return nombre.Trim();
        }
    }
}
