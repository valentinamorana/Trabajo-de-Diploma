using System;
using System.Linq;
using System.Windows.Forms;
using GUI.Inteligencia;
using BLLI = BLL.Inteligencia;

namespace GUI
{
    /// <summary>
    /// Centro de decisiones comerciales — pestaña «1b. Cohortes» (retención por mes de alta). La pestaña
    /// es un control propio (<see cref="PanelCohortes"/>) que se agrega por código; el cálculo es de
    /// BLL.Inteligencia.AnalisisCohortes y corre junto con el análisis, en segundo plano.
    ///
    /// Movimiento: «Actuar sobre la peor cohorte» lleva a la pestaña Recomendaciones y selecciona la
    /// recomendación de bajas del plan que más explica esa cohorte ("ChurnPlan:&lt;IdPlan&gt;"); si el motor
    /// no la propuso, explica por qué y ofrece abrir el Simulador con ese plan.
    /// </summary>
    public partial class CentroDecisionesForm
    {
        private readonly BLLI.AnalisisCohortes cohortesBLL = new BLLI.AnalisisCohortes();
        private TabPage tabCohortes;
        private PanelCohortes panelCohortes;
        private BE.MatrizCohortes _cohortes;

        private void InicializarCohortes()
        {
            panelCohortes = new PanelCohortes { Name = "panelCohortes" };
            panelCohortes.ActuarSobrePeorCohorte += PanelCohortes_ActuarSobrePeorCohorte;
            tabCohortes = new TabPage { Name = "tabCohortes", Padding = new Padding(4), BackColor = Tema.Papel };
            tabCohortes.Controls.Add(panelCohortes);
            // Justo después de «1. Analizar»: es parte del análisis.
            tabs.TabPages.Insert(Math.Min(1, tabs.TabPages.Count), tabCohortes);
        }

        private void TraducirCohortes()
        {
            tabCohortes.Text = Tr("cdc.cohorte.tab", "1b. Cohortes");
            panelCohortes.Traducir();
        }

        /// <summary>Se llama en segundo plano junto con el análisis: si falla, la pestaña muestra el aviso y el resto sigue.</summary>
        private BE.MatrizCohortes LeerCohortes(DateTime hasta, int? idPlan)
        {
            try { return cohortesBLL.Analizar(hasta, idPlan); }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("[CentroDecisiones] Cohortes: " + ex.Message);
                return new BE.MatrizCohortes
                {
                    Corte = hasta.Date,
                    Resumen = new BE.TextoTraducible("cdc.cohorte.error", "No se pudo calcular el análisis de cohortes: vuelva a analizar.")
                };
            }
        }

        private void MostrarCohortes() => panelCohortes.Mostrar(_cohortes);

        private void AgregarCohortesAlInforme(InformeEjecutivo.Datos datos)
        {
            datos.Cohortes = _cohortes;
            // Sin meses completos no hay curva: el informe lleva la tabla y el motivo, sin imagen.
            if (_cohortes != null && !_cohortes.SinDatos && !_cohortes.SinMesesCompletos)
                datos.GraficoCohortes = panelCohortes.GraficoPng();
        }

        private void PanelCohortes_ActuarSobrePeorCohorte(object sender, BE.PeorCohorte peor)
        {
            if (peor == null || _panel == null) return;

            // 1) La recomendación de bajas del plan, si el motor la propuso (aunque esté oculta).
            var rec = BLLI.AnalisisCohortes.RecomendacionDeLaPeor(peor, _recs);
            if (rec != null)
            {
                tabs.SelectedTab = tabRecomendaciones;
                if (!_recsVisibles.Contains(rec)) chkMostrarOcultas.Checked = true;   // pospuesta o descartada: vuelve a armar la lista
                SeleccionarFuente(dgvRecomendaciones, _recsVisibles.IndexOf(rec));
                MostrarDetalle(RecomendacionSeleccionada());
                MostrarOk(Tr("cdc.cohorte.msg.recomendacion",
                    "Recomendación de bajas del plan {0}: es el plan que más explica las bajas de la cohorte de {1:yyyy-MM}.",
                    new object[] { peor.NombrePlan, peor.Mes }));
                return;
            }

            // 2) No hay recomendación: por qué, y el Simulador precargado con ese plan.
            string motivo = TextosComerciales.R(BLLI.AnalisisCohortes.ExplicarSinRecomendacion(peor, _panel.Contexto, _reglasConError));
            var escenario = peor.IdPlan.HasValue ? BLLI.InteligenciaComercial.EscenarioBase(_panel, peor.IdPlan.Value) : null;
            var item = escenario == null ? null
                : cmbSimPlan.Items.Cast<Item<int?>>().FirstOrDefault(x => x.Valor == escenario.IdPlan);
            if (item == null)
            {
                MessageBox.Show(this, motivo, Tr("cdc.cohorte.btn.actuar", "Actuar sobre la peor cohorte"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ConfirmarSiNo(motivo + Environment.NewLine + Environment.NewLine +
                    Tr("cdc.cohorte.msg.simular", "¿Desea abrir el Simulador con el plan {0} para evaluar una promoción de retención?",
                       new object[] { escenario.NombrePlan }),
                    Tr("cdc.cohorte.btn.actuar", "Actuar sobre la peor cohorte")))
                return;
            if (ReferenceEquals(cmbSimPlan.SelectedItem, item))
                CmbSimPlan_SelectedIndexChanged(cmbSimPlan, EventArgs.Empty);   // vuelve al escenario base del plan
            else
                cmbSimPlan.SelectedItem = item;                                // carga el escenario base del plan
            tabs.SelectedTab = tabSimulador;
            MostrarOk(Tr("cdc.cohorte.msg.simulador",
                "Simulador cargado con el plan {0} (peor cohorte: {1:yyyy-MM}): ajuste los supuestos para evaluar la retención.",
                new object[] { escenario.NombrePlan, peor.Mes }));
        }
    }
}
