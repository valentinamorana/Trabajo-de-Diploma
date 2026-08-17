using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// T04 — Explorador del Patrón Composite (demostración académica, solo lectura).
    ///
    /// Muestra la estructura organizacional completa de WardrobeFlow como árbol Composite:
    ///
    ///   📁 WardrobeFlow
    ///     📁 Administración
    ///       📁 Administrador
    ///         🔑 Gestionar Usuarios
    ///         🔑 Ver Auditoría
    ///         ...
    ///       📁 Auditor / Supervisor
    ///     📁 Comercial
    ///       📁 Gerente Comercial / Vendedor
    ///     📁 Inventario y Logística
    ///       📁 ...
    ///
    /// El árbol se obtiene de BLL.Familia.ObtenerArbol() (árbol real desde BD vía
    /// PermisoRelacion) usando BE.Rol/BE.Familia (nodos compuestos) y BE.Patente (hojas).
    ///
    /// Implementa IIdiomaObserver: todos los controles se traducen al cambiar de idioma.
    /// NO modifica permisos ni afecta la autorización.
    /// </summary>
    public partial class ExploradorCompositeForm : Form, IIdiomaObserver
    {
        private readonly BLL.Familia _familiaBLL = new BLL.Familia();

        public ExploradorCompositeForm()
        {
            InitializeComponent();
        }

        // Helper de traducción — obtiene texto con fallback.
        private string T(string key, string fallback)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            return t.ContainsKey(key) ? t[key].Texto : fallback;
        }

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                string ico = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (System.IO.File.Exists(ico)) this.Icon = new Icon(ico);
            }
            catch { }
            GestorIdioma.SuscribirObservador(this);
            AplicarIdioma();
            CargarArbol();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        // ── IIdiomaObserver ───────────────────────────────────────────────────

        public void UpdateLanguage(Idioma idioma)
        {
            AplicarIdioma();
            CargarArbol(); // reconstruye el árbol con nombres de patentes/familias en el nuevo idioma
        }

        // Actualiza TODOS los controles de texto del formulario con las traducciones activas.
        private void AplicarIdioma()
        {
            this.Text            = T("frm.explorador",             "Vista completa del sistema");
            lblTitulo.Text       = T("lbl.explorador.titulo",      "Vista completa del sistema");
            lblDescripcion.Text  = T("lbl.explorador.descripcion", "Estructura organizacional de WardrobeFlow — Solo lectura");
            lblLeyenda.Text      = T("lbl.explorador.leyenda",     "📁 Familia (nodo compuesto — Área o Rol)    🔑 Patente (hoja — permiso atómico)");
            btnCerrar.Text       = T("btn.explorador.cerrar",      "Cerrar");
            btnColapsar.Text     = T("btn.explorador.colapsar",    "⊟ Colapsar todo");
            btnExpandir.Text     = T("btn.explorador.expandir",    "⊞ Expandir todo");
            btnActualizar.Text   = T("btn.permisos.actualizar",    "↻ Actualizar");
        }

        // ── Construcción del árbol ────────────────────────────────────────────

        private void CargarArbol()
        {
            // Obtener traducciones una sola vez para toda la construcción del árbol
            var traducciones = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);

            treeView.BeginUpdate();
            treeView.Nodes.Clear();

            try
            {
                // Árbol REAL desde BD (vía PermisoRelacion): roles, familias y patentes.
                // Los nodos raíz son los que no tienen padre (roles y familias huérfanas).
                var raices = _familiaBLL.ObtenerArbol();
                var empresa = new TreeNode("📁 WardrobeFlow")
                {
                    NodeFont  = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(176, 62, 96)
                };
                foreach (BE.Componente raiz in raices)
                    empresa.Nodes.Add(CrearNodoRecursivo(raiz, traducciones));
                treeView.Nodes.Add(empresa);

                treeView.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(T("err.explorador.cargar", "Error al cargar árbol Composite:\n{0}"), ex.Message),
                    T("diag.err.titulo", "Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            treeView.EndUpdate();
        }

        /// <summary>
        /// Construye un TreeNode recursivamente a partir de un BE.Componente.
        /// Familias → nodo azul en negrita con prefijo 📁
        /// Patentes  → nodo verde con prefijo 🔑
        ///
        /// Idéntico al método CrearNodoRecursivo de Stach/GUI/PermisosForm.cs.
        /// Las traducciones se pasan como parámetro para no llamar a ObtenerTraducciones en cada nodo.
        /// </summary>
        private TreeNode CrearNodoRecursivo(BE.Componente componente,
                                             IDictionary<string, Traduccion> t)
        {
            bool esFamilia = componente is BE.Familia;
            string prefijo = esFamilia ? "📁 " : "🔑 ";
            string nombre  = componente.Nombre;

            if (esFamilia)
            {
                // Intentar traducir como grupo de permisos
                string claveGrp = "perm.grp." + nombre.ToLowerInvariant()
                                      .Replace(" ", "").Replace("(", "").Replace(")", "");
                if (t.ContainsKey(claveGrp))
                    nombre = t[claveGrp].Texto;
                else
                {
                    // Intentar traducir como rol
                    string claveRol = "perm.rol." + componente.Nombre.ToLowerInvariant()
                                          .Replace(" ", "").Replace("(", "").Replace(")", "");
                    if (t.ContainsKey(claveRol)) nombre = t[claveRol].Texto;
                }
            }
            else
            {
                string clavePat = "perm.pat." + nombre.ToLowerInvariant()
                                      .Replace(" ", "").Replace("(", "").Replace(")", "");
                if (t.ContainsKey(clavePat)) nombre = t[clavePat].Texto;
            }

            var nodo = new TreeNode(prefijo + nombre)
            {
                Tag       = componente,
                NodeFont  = esFamilia
                    ? new Font("Segoe UI", 9f, FontStyle.Bold)
                    : new Font("Segoe UI", 9f),
                ForeColor = esFamilia
                    ? Color.FromArgb(176, 62, 96)
                    : Color.FromArgb(30, 110, 50)
            };

            // Recursión sobre los hijos — profundidad arbitraria
            if (esFamilia)
            {
                foreach (BE.Componente hijo in componente.Hijos)
                    nodo.Nodes.Add(CrearNodoRecursivo(hijo, t));
            }

            return nodo;
        }

        // ── Eventos ───────────────────────────────────────────────────────────

        private void BtnCerrar_Click(object sender, EventArgs e) => this.Close();
        private void BtnColapsar_Click(object sender, EventArgs e) => treeView.CollapseAll();
        private void BtnExpandir_Click(object sender, EventArgs e) => treeView.ExpandAll();
        private void BtnActualizar_Click(object sender, EventArgs e) => CargarArbol();
    }
}
