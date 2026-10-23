using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GUI.Inteligencia;

namespace GUI
{
    /// <summary>
    /// PN03 — Pestaña "Metas" del Centro de decisiones comerciales. El contenido es un control propio
    /// (Inteligencia.PanelMetasComerciales, hecho con el Diseñador) ubicado en su pestaña con el Diseñador;
    /// se conecta con el análisis (al cambiar una meta se vuelve a analizar: las prioridades dependen de
    /// ella) y se suma al Informe ejecutivo.
    /// </summary>
    public partial class CentroDecisionesForm
    {
        private void AgregarPestanaMetas()
        {
            // La pestaña y el panel están en el Diseñador (última pestaña).
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

        // El gráfico va en la sección de metas del informe, a continuación de su tabla (no con los de Analizar).
        private void AgregarGraficoMetas(InformeEjecutivo.Datos datos)
        {
            datos.GraficoMetas = panelMetas != null && panelMetas.Cargado ? panelMetas.GraficoPng() : null;
        }
    }
}
