using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Servicios.Multiidioma;
using Docs = GUI.Exportacion.DocumentosPromocion;

namespace GUI
{
    /// <summary>
    /// Capa de Presentación — PN03, carril Contabilidad del diagrama de actividad:
    ///   Analizar margen e impacto (beneficio estimado, margen proyectado = beneficio − costo del
    ///   descuento sobre los clientes activos del plan, y promociones Vigentes del mismo plan
    ///   superpuestas en fechas) → ¿Aprueba? Sí: Vigente; No: RechazadaContabilidad.
    ///   Cada decisión genera el «Dictamen contable», que se puede imprimir.
    /// Quien creó la promoción no puede dictaminarla: la guarda es BLL.Promocion.PuedeDictaminar.
    /// </summary>
    public partial class PromocionesContabilidadForm : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Interfaces.IPromocionService promocionBLL = new BLL.Promocion();
        private readonly MenuDocumentosPromocion menuDocumentos;

        private List<BE.Promocion> _promociones = new List<BE.Promocion>();

        private Idioma _idioma = GestorIdioma.IdiomaActual;

        public PromocionesContabilidadForm()
        {
            InitializeComponent();
            // Mismo estilo de grilla que el resto de las pantallas.
            GUI.Estilos.EstiloFormulario.Grilla(dgvPromociones);
            // Paleta centralizada (GUI/Tema.cs).
            btnAprobar.BackColor = Tema.Exito;
            btnRechazar.BackColor = Tema.Error;
            btnImprimir.BackColor = Tema.RosaOscuro;
            txtAnalisis.BackColor = Tema.Papel;
            menuDocumentos = new MenuDocumentosPromocion(promocionBLL, ObtenerSeleccionada, this,
                ex => MostrarError(ex), (k, f) => Tr(k, f));
        }

        // ── Observer de idioma ────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Traducir(GestorIdioma.IdiomaActual);
        }

        public void UpdateLanguage(Idioma idioma)
        {
            Traducir(idioma);
            CargarPromociones();
        }

        private void Traducir(Idioma idioma)
        {
            _idioma = idioma;
            var t = Traductor.ObtenerTraducciones(idioma);
            if (this.Tag != null && t.ContainsKey(this.Tag.ToString()))
                this.Text = t[this.Tag.ToString()].Texto;
            foreach (Control c in panelTop.Controls)
                if (c.Tag != null && t.ContainsKey(c.Tag.ToString()))
                    c.Text = t[c.Tag.ToString()].Texto;
            tip.SetToolTip(btnRefrescar, Tr("tip.actualizar", "Actualizar"));
            btnRefrescar.Text = Tr("tip.actualizar", "Actualizar");
            menuDocumentos.Traducir();
            TraducirHeadersGrilla();
        }

        private void TraducirHeadersGrilla()
        {
            void RH(string col, string clave, string fb)
            {
                if (dgvPromociones.Columns.Contains(col)) dgvPromociones.Columns[col].HeaderText = Tr(clave, fb);
            }
            RH("ID", "col.promo.id", "ID");
            RH("Nombre", "col.promo.nombre", "Nombre");
            RH("Aplica a", "col.promo.aplicaa", "Aplica a");
            RH("Tipo", "col.promo.tipo", "Tipo");
            RH("Valor", "col.promo.valor", "Valor");
            RH("Vigencia", "col.promo.vigencia", "Vigencia");
            RH("Margen Est.", "col.promo.margenest", "Margen Est.");
            RH("Impacto Económico", "col.promo.impactoeconomico", "Impacto Económico");
            RH("Creada por", "col.promo.creadapor", "Creada por");
        }

        private void PromocionesContabilidadForm_Load(object sender, EventArgs e) => CargarPromociones();

        private void BtnRefrescar_Click(object sender, EventArgs e) => CargarPromociones();

        private void CargarPromociones()
        {
            try
            {
                _promociones = promocionBLL.ObtenerPendientesRevisionContable();

                var tabla = new DataTable();
                tabla.Columns.Add("ID", typeof(int));
                tabla.Columns.Add("Nombre", typeof(string));
                tabla.Columns.Add("Aplica a", typeof(string));
                tabla.Columns.Add("Tipo", typeof(string));
                tabla.Columns.Add("Valor", typeof(decimal));
                tabla.Columns.Add("Vigencia", typeof(string));
                tabla.Columns.Add("Margen Est.", typeof(decimal));
                tabla.Columns.Add("Impacto Económico", typeof(string));
                tabla.Columns.Add("Creada por", typeof(string));

                foreach (var p in _promociones)
                    tabla.Rows.Add(p.IdPromocion, p.Nombre, Docs.AplicaA(p.IdPlan, p.NombrePlan, p.CategoriaPrenda),
                        Docs.Tipo(p.TipoDescuento), p.Valor, $"{p.FechaInicio:d} - {p.FechaFin:d}",
                        p.MargenEstimado, p.ImpactoEconomico ?? "—", p.NombreUsuarioAlta ?? "—");

                dgvPromociones.DataSource = tabla;
                if (dgvPromociones.Columns.Contains("ID")) dgvPromociones.Columns["ID"].Width = 44;
                TraducirHeadersGrilla();

                lblConteo.Text = Tr("promo.conteo.revisioncontable", "{0} promoción(es) pendiente(s) de revisión contable.",
                    new object[] { _promociones.Count });
                dgvPromociones.ClearSelection();
                MostrarAnalisis(null);
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private BE.Promocion ObtenerSeleccionada()
        {
            if (dgvPromociones.SelectedRows.Count == 0) return null;
            int id = Convert.ToInt32(dgvPromociones.SelectedRows[0].Cells["ID"].Value);
            return _promociones.Find(p => p.IdPromocion == id);
        }

        // "Analizar margen e impacto" de la promoción seleccionada.
        private void DgvPromociones_SelectionChanged(object sender, EventArgs e)
        {
            var p = ObtenerSeleccionada();
            if (p == null) { MostrarAnalisis(null); return; }
            try { MostrarAnalisis(promocionBLL.AnalizarMargenEImpacto(p.IdPromocion)); }
            catch (Exception ex) { MostrarAnalisis(null); MostrarError(ex); }
        }

        private void MostrarAnalisis(BE.AnalisisImpactoPromocion a)
        {
            btnImprimir.Enabled = a != null;
            btnAprobar.Enabled = btnRechazar.Enabled = a != null && a.UsuarioPuedeDictaminar;
            if (a == null)
            {
                txtAnalisis.Text = Tr("promo.analisis.vacio", "Seleccioná una promoción para analizar su margen e impacto.");
                return;
            }

            var p = a.Promocion;
            var sb = new StringBuilder();
            sb.AppendLine(Tr("promo.analisis.titulo", "Análisis de margen e impacto — Promoción #{0} '{1}'", new object[] { p.IdPromocion, p.Nombre }));
            sb.AppendLine(Tr("promo.analisis.margen", "Margen estimado: {0:C2} — Impacto: {1}", new object[] { p.MargenEstimado, p.ImpactoEconomico ?? "—" }));
            sb.AppendLine(a.BeneficioEstimadoSugerencia.HasValue
                ? Tr("promo.analisis.beneficio", "Beneficio estimado por Gerencia en la sugerencia #{0}: {1:C2} ({2}).",
                     new object[] { p.IdSugerenciaOrigen, a.BeneficioEstimadoSugerencia.Value,
                                    a.OrigenSugerencia.HasValue ? Docs.Origen(a.OrigenSugerencia.Value) : "—" })
                : Tr("promo.analisis.sinsugerencia", "Alta manual: no hay beneficio estimado de Gerencia."));
            AgregarMargenProyectado(sb, a);
            if (a.TieneSuperposicion())
            {
                sb.AppendLine(Tr("promo.analisis.superpuestas", "ADVERTENCIA: se superpone en fechas con otras promociones vigentes del mismo plan:"));
                foreach (var o in a.Superpuestas)
                    sb.AppendLine($"   • #{o.IdPromocion} {o.Nombre} ({o.FechaInicio:d} - {o.FechaFin:d}, {Docs.Estado(o.Estado)})");
            }
            else
                sb.AppendLine(Tr("promo.analisis.sinsuperposicion", "No se superpone con otras promociones vigentes del mismo plan."));
            if (!a.UsuarioPuedeDictaminar)
                sb.AppendLine(Tr("promo.analisis.creador", "Creaste esta promoción: la tiene que dictaminar otro usuario de Contabilidad."));
            txtAnalisis.Text = sb.ToString();
        }

        // Margen proyectado (mensual) que calcula BLL.Promocion.AnalizarMargenEImpacto, paso a paso:
        // descuento por cliente → costo del descuento → beneficio estimado → margen → ¿conviene?
        private const string Sangria = "   ";

        private void AgregarMargenProyectado(StringBuilder sb, BE.AnalisisImpactoPromocion a)
        {
            var p = a.Promocion;
            sb.AppendLine();
            sb.AppendLine(Tr("promo.analisis.calc.titulo", "Margen proyectado (por mes):"));
            if (a.EsInformativa)
            {
                sb.Append(Sangria).AppendLine(Tr("promo.analisis.calc.informativa",
                    "Promoción por categoría: es informativa (no descuenta en el cobro); no tiene costo proyectado ni resultado."));
                sb.AppendLine();
                return;
            }
            if (!a.MargenCalculable)
            {
                sb.Append(Sangria).AppendLine(Tr("promo.analisis.calc.sinplan", "No se puede calcular el margen: el plan de la promoción ya no existe."));
                sb.AppendLine();
                return;
            }
            sb.Append(Sangria).AppendLine(Tr("promo.analisis.calc.descuento", "Descuento por cliente: {0:C2} ({1} {2} sobre el precio del plan, {3:C2})",
                new object[] { a.DescuentoPorCliente, Docs.Tipo(p.TipoDescuento), Docs.Valor(p), a.PrecioPlan.Value }));
            sb.Append(Sangria).AppendLine(Tr("promo.analisis.calc.costo", "Costo del descuento: {0:C2} × {1} cliente(s) activo(s) del plan = {2:C2}",
                new object[] { a.DescuentoPorCliente, a.ClientesActivosPlan, a.CostoDescuentoProyectado }));
            sb.Append(Sangria).AppendLine(a.BeneficioDeSugerencia
                ? Tr("promo.analisis.calc.beneficiosug", "Beneficio estimado (sugerencia de Gerencia): {0:C2}", new object[] { a.BeneficioEstimado })
                : Tr("promo.analisis.calc.beneficioadm", "Beneficio estimado (margen cargado por Administración): {0:C2}", new object[] { a.BeneficioEstimado }));
            sb.Append(Sangria).AppendLine(Tr("promo.analisis.calc.margen", "Margen proyectado: {0:C2} − {1:C2} = {2:C2}",
                new object[] { a.BeneficioEstimado, a.CostoDescuentoProyectado, a.MargenProyectado }));
            sb.Append(Sangria).AppendLine(a.Conviene == true
                ? Tr("promo.analisis.calc.conviene", "Resultado: CONVIENE (el beneficio estimado cubre el costo del descuento).")
                : Tr("promo.analisis.calc.noconviene", "Resultado: NO CONVIENE (el descuento cuesta igual o más de lo que se espera ganar)."));
            sb.Append(Sangria).AppendLine(Tr("promo.analisis.calc.nota", "Es una recomendación: la decisión es de Contabilidad."));
            sb.AppendLine();
        }

        // ¿Aprueba? Sí → Vigente + «Dictamen contable».
        private void BtnAprobar_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerSeleccionada();
            if (promocion == null) return;
            if (!FormBase.MostrarConfirmacionSiNo(this,
                    Tr("conf.promo.aprobarcontable.msg", "¿Aprobar y activar la promoción '{0}'?", new object[] { promocion.Nombre }),
                    Tr("conf.promo.aprobarcontable.titulo", "Confirmar Aprobación"), porDefectoNo: false))
                return;
            Dictaminar(promocion, () => promocionBLL.AprobarContable(this.Text, promocion, txtObservacion.Text),
                Tr("msg.promo.aprobada", "Promoción '{0}' aprobada y activada.", new object[] { promocion.Nombre }));
        }

        // ¿Aprueba? No → RechazadaContabilidad + «Dictamen contable» (vuelve a Administración).
        private void BtnRechazar_Click(object sender, EventArgs e)
        {
            var promocion = ObtenerSeleccionada();
            if (promocion == null) return;
            if (!FormBase.MostrarConfirmacionSiNo(this,
                    Tr("conf.promo.rechazarcontable.msg", "¿Rechazar la promoción '{0}'? Vuelve a Administración para reformularla.", new object[] { promocion.Nombre }),
                    Tr("conf.promo.rechazarcontable.titulo", "Confirmar Rechazo"), porDefectoNo: false))
                return;
            Dictaminar(promocion, () => promocionBLL.RechazarContable(this.Text, promocion, txtObservacion.Text),
                Tr("msg.promo.rechazada", "Promoción '{0}' rechazada.", new object[] { promocion.Nombre }));
        }

        private void Dictaminar(BE.Promocion promocion, Func<int> dictaminar, string mensajeOk)
        {
            try
            {
                dictaminar();
                MostrarOk(mensajeOk);
                txtObservacion.Clear();
                CargarPromociones();
            }
            catch (Exception ex) { MostrarError(ex); }
            // El dictamen contable está en el menú "Imprimir".
        }

        private void BtnImprimir_Click(object sender, EventArgs e) => menuDocumentos.Mostrar(btnImprimir);
    }
}
