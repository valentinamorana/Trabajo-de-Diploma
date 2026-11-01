using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// PN03: «Constancia de la decisión» (1.6.1), solo lectura: lo que Gerencia vio al decidir, en secciones
    /// (recomendación, opción elegida, alternativas, indicadores, parámetros del análisis, supuestos y
    /// advertencias), nunca el XML crudo. Desde la pestaña «3. Historial de decisiones» del Centro:
    ///   · «Ver constancia» → la registrada en la base, con «Exportar XML/JSON…» (el formato lo decide la
    ///     extensión, como en Versiones y Errores).
    ///   · «Abrir constancia desde archivo…» → la de un archivo, con el resultado de compararla campo por
    ///     campo con la registrada (coincide / fue modificada y qué campos / sin registro).
    /// Toda la lógica (armar, validar, comparar, exportar) es de BLL.Inteligencia.ConstanciasDecision.
    /// Los valores se muestran como se guardaron (en español, como la bitácora); los rótulos se traducen.
    /// </summary>
    public partial class ConstanciaDecisionForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private const string Modulo = "Centro de decisiones comerciales";
        private readonly BE.ConstanciaDecision _constancia;
        private readonly BE.ConstanciaVerificada _verificada;

        /// <param name="verificada">null = la constancia registrada en la base; si no, la abierta desde un archivo.</param>
        public ConstanciaDecisionForm(BE.ConstanciaDecision constancia, BE.ConstanciaVerificada verificada)
        {
            InitializeComponent();
            _constancia = constancia ?? throw new ArgumentNullException(nameof(constancia));
            _verificada = verificada;
            Estilos.EstiloFormulario.BotonPrimario(btnExportar);
            Estilos.EstiloFormulario.BotonSecundario(btnCerrar);
            btnExportar.Visible = verificada == null;   // la de un archivo ya es un archivo
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir(GestorIdioma.IdiomaActual);
        }

        public void UpdateLanguage(Idioma idioma) => Traducir(idioma);

        private string Accion(string accion)
        {
            switch (accion)
            {
                case nameof(BE.EstadoDecision.Aceptada): return Tr("cdc.constancia.aceptada", "ACEPTADA");
                case nameof(BE.EstadoDecision.Descartada): return Tr("cdc.constancia.descartada", "DESCARTADA");
                case nameof(BE.EstadoDecision.Pospuesta): return Tr("cdc.constancia.pospuesta", "POSPUESTA");
                default: return (accion ?? "").ToUpperInvariant();
            }
        }

        private void Traducir(Idioma idioma)
        {
            var c = _constancia;
            Text = Tr("cdc.constancia.titulo", "Constancia de la decisión") + " #" + c.IdDecision;
            lblEncabezado.Text = Tr("cdc.constancia.encabezado", "{0} · Decisión #{1} · Decidió {2} (usuario {3}) el {4:dd/MM/yyyy} a las {4:HH:mm}",
                new object[] { Accion(c.Accion), c.IdDecision, c.NombreUsuario ?? "-", c.Username ?? "-", c.Fecha });
            btnExportar.Text = Tr("cdc.constancia.exportar", "Exportar XML/JSON…");
            btnCerrar.Text = Tr("btn.cerrar", "Cerrar");
            tip.SetToolTip(btnExportar, Tr("cdc.tip.exportarconstancia", "Guarda esta constancia en un archivo .xml o .json (el formato lo decide la extensión) para llevarla a otra PC o adjuntarla."));
            MostrarVerificacion();
            Dibujar();
        }

        private void MostrarVerificacion()
        {
            if (_verificada == null) { lblVerificacion.Visible = false; return; }
            lblVerificacion.Visible = true;
            switch (_verificada.Estado)
            {
                case BE.EstadoVerificacionConstancia.Coincide:
                    lblVerificacion.ForeColor = Tema.Exito;
                    lblVerificacion.Text = "✔ " + Tr("cdc.constancia.coincide", "Coincide con la constancia registrada en el sistema.");
                    break;
                case BE.EstadoVerificacionConstancia.Modificada:
                    lblVerificacion.ForeColor = Tema.Error;
                    lblVerificacion.Text = "✖ " + Tr("cdc.constancia.modificada", "El archivo NO coincide con la constancia registrada: fue modificado ({0} campo(s) distinto(s), ver abajo).",
                        new object[] { _verificada.Diferencias.Count });
                    break;
                case BE.EstadoVerificacionConstancia.SinConstanciaEnSistema:
                    lblVerificacion.ForeColor = Tema.Alerta;
                    lblVerificacion.Text = "? " + Tr("cdc.constancia.sinconstanciaensistema", "La decisión existe en este sistema, pero no tiene constancia registrada: no se puede comparar.");
                    break;
                default:
                    lblVerificacion.ForeColor = Tema.Alerta;
                    lblVerificacion.Text = "? " + Tr("cdc.constancia.sinregistro", "Sin registro en este sistema: la decisión #{0} no existe en esta base.",
                        new object[] { _constancia.IdDecision });
                    break;
            }
        }

        // ── Secciones ──

        private void Titulo(string texto)
        {
            txtContenido.SelectionFont = new Font(txtContenido.Font.FontFamily, 10.5f, FontStyle.Bold);
            txtContenido.SelectionColor = Tema.RosaOscuro;
            txtContenido.AppendText((txtContenido.TextLength > 0 ? "\n" : "") + texto + "\n");
        }

        private void Linea(string rotulo, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return;
            txtContenido.SelectionFont = new Font(txtContenido.Font, FontStyle.Bold);
            txtContenido.SelectionColor = Tema.Tinta;
            txtContenido.AppendText("  " + rotulo + ": ");
            txtContenido.SelectionFont = txtContenido.Font;
            txtContenido.AppendText(valor + "\n");
        }

        private void Item(string texto)
        {
            txtContenido.SelectionFont = txtContenido.Font;
            txtContenido.SelectionColor = Tema.Tinta;
            txtContenido.AppendText("  • " + texto + "\n");
        }

        private static string Num(decimal? v) => v.HasValue ? v.Value.ToString("#,0.##", CultureInfo.CurrentCulture) : "-";

        private void Dibujar()
        {
            var c = _constancia;
            txtContenido.Clear();

            if (_verificada != null && _verificada.Diferencias.Count > 0)
            {
                Titulo(Tr("cdc.constancia.diferencias", "Campos que difieren (archivo ≠ registrado)"));
                foreach (var d in _verificada.Diferencias) Item(d);
            }

            Titulo(Tr("cdc.constancia.sec.decision", "Decisión"));
            Linea(Tr("cdc.constancia.accion", "Acción"), Accion(c.Accion));
            Linea(Tr("cdc.col.motivo", "Motivo"), c.Motivo);
            if (c.PospuestaHasta.HasValue) Linea(Tr("cdc.constancia.pospuestahasta", "Pospuesta hasta"), c.PospuestaHasta.Value.ToString("d"));

            var r = c.Recomendacion ?? new BE.ConstanciaRecomendacion();
            Titulo(Tr("cdc.constancia.sec.recomendacion", "Recomendación"));
            Linea(Tr("cdc.col.recomendacion", "Recomendación"), r.Titulo);
            Linea(Tr("cdc.col.regla", "Regla"), r.Regla);
            Linea(Tr("cdc.col.prioridad", "Prioridad"), r.Prioridad);
            Linea(Tr("cdc.col.confianza", "Confianza"), r.Confianza);
            Linea(Tr("cdc.constancia.diagnostico", "Diagnóstico"), r.Diagnostico);
            Linea(Tr("cdc.col.motivo", "Motivo"), r.Motivo);

            var o = c.OpcionElegida ?? new BE.ConstanciaOpcion();
            Titulo(Tr("cdc.constancia.sec.opcion", "Opción elegida"));
            Linea(Tr("cdc.col.propuesta", "Acción propuesta"), o.Descripcion);
            Linea(Tr("cdc.col.impacto", "Impacto estimado"), o.ImpactoEstimado.ToString("C0"));
            Linea(Tr("cdc.col.formula", "Fórmula y fuente"), o.Formula);
            if (c.Alternativas.Count > 0)
            {
                Titulo(Tr("cdc.col.alternativas", "Alternativas"));
                foreach (var a in c.Alternativas) Item($"{a.Descripcion} ({a.ImpactoEstimado:C0})");
            }

            var p = c.Periodo ?? new BE.ConstanciaPeriodo();
            Titulo(Tr("cdc.constancia.sec.periodo", "Período y plan analizados"));
            if (p.Desde != default(DateTime)) Linea(Tr("cdc.constancia.periodo", "Período"), $"{p.Desde:d} – {p.Hasta:d}");
            Linea(Tr("cdc.plan", "Plan:").TrimEnd(':'), p.Plan);

            if (c.Indicadores.Count > 0)
            {
                Titulo(Tr("cdc.constancia.sec.indicadores", "Indicadores al decidir"));
                foreach (var k in c.Indicadores)
                    Item($"{k.Nombre}: {Num(k.Valor)} ({Tr("cdc.col.anterior", "Anterior")}: {Num(k.Anterior)}; {Tr("cdc.col.variacion", "Variación")}: {Num(k.Variacion)})");
            }
            if (c.ParametrosDelAnalisis.Count > 0)
            {
                Titulo(Tr("cdc.constancia.sec.parametros", "Parámetros del análisis"));
                foreach (var q in c.ParametrosDelAnalisis) Item($"{q.Nombre}: {Num(q.Valor)}");
            }
            if (c.Supuestos.Count > 0)
            {
                Titulo(Tr("cdc.constancia.sec.supuestos", "Supuestos"));
                foreach (var s in c.Supuestos) Item(s);
            }
            Titulo(Tr("cdc.constancia.sec.advertencias", "Advertencias"));
            if (c.Advertencias.Count == 0) Item(Tr("cdc.constancia.sinadvertencias", "Ninguna."));
            foreach (var a in c.Advertencias) Item(a);
            txtContenido.SelectionStart = 0;
            txtContenido.ScrollToCaret();
        }

        private void BtnExportar_Click(object sender, EventArgs e)
        {
            string ruta;
            using (var dlg = new SaveFileDialog
            {
                // XML o JSON: el formato lo decide la extensión elegida (BLL → FabricaSerializadores).
                Filter = Tr("dlg.filtro.serial.guardar", "Archivos XML (*.xml)|*.xml|Archivos JSON (*.json)|*.json"),
                DefaultExt = "xml",
                AddExtension = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileName = BLL.Inteligencia.ConstanciasDecision.NombreArchivoSugerido(_constancia, DateTime.Now)
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                ruta = dlg.FileName;
            }
            try
            {
                new BLL.Inteligencia.ConstanciasDecision().Exportar(Modulo, _constancia.IdDecision, ruta);
                MostrarOk(Tr("cdc.constancia.exportada", "Constancia exportada: {0}", new object[] { ruta }));
            }
            catch (Exception ex) { MostrarError(ex); }
        }
    }
}
