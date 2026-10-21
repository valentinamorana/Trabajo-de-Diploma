using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GUI.Inteligencia;

namespace GUI
{
    /// <summary>
    /// PN03 — Pestaña "Metas" del Centro de decisiones comerciales. El contenido es un control propio
    /// (Inteligencia.PanelMetasComerciales, hecho con el Diseñador); acá solo se lo aloja en una pestaña,
    /// se conecta con el análisis (al cambiar una meta se vuelve a analizar: las prioridades dependen de
    /// ella) y se suma al Informe ejecutivo.
    /// </summary>
    public partial class CentroDecisionesForm
    {
        private TabPage tabMetas;
        private PanelMetasComerciales panelMetas;

        private void AgregarPestanaMetas()
        {
            panelMetas = new PanelMetasComerciales { Dock = DockStyle.Fill, Name = "panelMetas" };
            tabMetas = new TabPage { Name = "tabMetas", Text = "Metas", BackColor = Tema.Papel, Padding = new Padding(6) };
            tabMetas.Controls.Add(panelMetas);
            tabs.TabPages.Add(tabMetas);
            panelMetas.Inicializar(bll, Modulo);
            panelMetas.MetasCambiaron += (s, e) => BtnAnalizar_Click(this, EventArgs.Empty);
            tabs.SelectedIndexChanged += (s, e) =>
            {
                if (tabs.SelectedTab == tabMetas) panelMetas.CargarSiHaceFalta();
            };
        }

        // Para el Informe ejecutivo: lo que muestra la pestaña o, si no se abrió, las metas del mes en curso.
        private List<BE.SeguimientoMeta> MetasParaInforme()
        {
            if (panelMetas != null && panelMetas.Cargado) return panelMetas.Seguimiento;
            try { return bll.SeguirMetas(DateTime.Today); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[CentroDecisiones] Metas para el informe: " + ex.Message);
                return new List<BE.SeguimientoMeta>();
            }
        }

        private void AgregarGraficoMetas(InformeEjecutivo.Datos datos)
        {
            byte[] png = panelMetas != null && panelMetas.Cargado ? panelMetas.GraficoPng() : null;
            if (png != null) datos.Graficos.Add((Tr("cdc.meta.grafico", "Metas: real contra objetivo"), png));
        }
    }
}
