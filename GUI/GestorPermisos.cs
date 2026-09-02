using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// T04 — Gestión de Perfiles de Usuario (Patrón Composite) — UX "árbol + panel de acciones
    /// con modo Crear/Editar" (misma experiencia que la pantalla de Gestión de Roles de referencia):
    ///
    ///   • Izquierda: TreeView con TODA la estructura (Roles 👥 → sub-roles → Patentes 🔑).
    ///     Se selecciona el nodo sobre el que se quiere operar.
    ///   • Centro: panel de acciones con un TOGGLE de modo:
    ///       – Crear:  nombre + "Crear rol raíz" / "Crear sub-rol" (bajo el rol seleccionado).
    ///       – Editar: renombrar / eliminar el rol, asignar un permiso o rol por ComboBox,
    ///                 y quitar el ítem seleccionado de su rol padre.
    ///   • Derecha: árbol de PERMISOS EFECTIVOS (recursivo) del nodo seleccionado — lo que ese
    ///     rol concede tras resolver toda la jerarquía (visualización del Composite).
    ///
    /// La composición se persiste en [PermisoRelacion] de forma inmediata (cada acción guarda y
    /// re-aplica la seguridad en vivo). La validación anti-ciclos vive en la BLL.
    /// </summary>
    public partial class GestorPermisos : FormBase, IIdiomaObserver
    {
        protected override Label MensajeLabel => lblMensaje;

        private readonly BLL.Familia _familiaBLL = new BLL.Familia();

        // ── Estado ───────────────────────────────────────────────────────────────
        private List<BE.Componente> _raices = new List<BE.Componente>();
        private readonly Dictionary<int, BE.Componente> _todos = new Dictionary<int, BE.Componente>();
        private BE.Componente _seleccionado;
        private ExploradorCompositeForm _exploradorAbierto;

        // Envoltura para mostrar un componente en el ComboBox con su ícono.
        private class Item
        {
            public BE.Componente Comp;
            public override string ToString() => Etiqueta(Comp);
        }

        // ── Paleta de marca ────────────────────────────────────────────────────────
        private static readonly Color RosaPrimario = Color.FromArgb(210, 100, 135);  // #D26487
        private static readonly Color RosaOscuro   = Color.FromArgb(176, 62, 96);    // #B03E60
        private static readonly Color Peligro      = Color.FromArgb(200, 60, 60);    // #C83C3C
        private static readonly Color PanelClaro   = Color.FromArgb(245, 245, 250);  // #F5F5FA
        private static readonly Color Neutro       = Color.FromArgb(236, 236, 242);

        public GestorPermisos()
        {
            InitializeComponent();
            ActualizarControles();
        }

        // ── Ciclo de vida ────────────────────────────────────────────────────────
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GestorIdioma.SuscribirObservador(this);
            Traducir();
            CargarArbol();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            GestorIdioma.DesuscribirObservador(this);
            base.OnFormClosing(e);
        }

        public void UpdateLanguage(Idioma idioma) { Traducir(); }

        private string T(string key, string fallback)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            return t.ContainsKey(key) ? t[key].Texto : fallback;
        }

        private void Traducir()
        {
            this.Text            = T("frm.gestorpermisos",      "Gestor de Perfiles — Roles y Permisos (Composite)");
            lblTitulo.Text       = T("lbl.permisos.titulo",     "Perfiles y Permisos");
            lblSubtitulo.Text    = T("lbl.permisos.subtitulo",  "Gestión de roles (los permisos son un catálogo fijo)");
            lblEstructura.Text   = T("lbl.permisos.estructura", "Estructura del sistema (roles y permisos)");

            lblModo.Text         = T("lbl.permisos.modo",       "Modo:");
            rbCrear.Text         = T("rb.permisos.crear",       "Crear");
            rbEditar.Text        = T("rb.permisos.editar",      "Editar / Eliminar");
            lblNombreRol.Text    = T("lbl.permisos.nombrerol",  "Nombre del rol:");
            grpCrear.Text        = T("grp.permisos.crear",      "Crear rol");
            grpEditar.Text       = T("grp.permisos.editar",     "Editar rol");
            grpAsignar.Text      = T("grp.permisos.asignar",    "Asignar permiso o rol");

            btnCrearRaiz.Text    = T("btn.permisos.crearraiz",  "➕ Crear rol raíz");
            btnCrearSub.Text     = T("btn.permisos.crearsub",   "➕ Crear sub-rol");
            btnEditarNombre.Text = T("btn.permisos.editarnom",  "✏ Renombrar rol");
            btnEliminarRol.Text  = T("btn.permisos.eliminar",   "🗑 Eliminar rol");
            btnAsignar.Text      = T("btn.permisos.asignar",    "Asignar ↓");
            btnQuitar.Text       = T("btn.permisos.quitar",     "Quitar ítem seleccionado");
            btnActualizar.Text   = T("btn.permisos.actualizar", "↻ Actualizar");
            btnExplorador.Text   = T("btn.explorador",          "🌳 Ver vista completa del sistema");
            btnCerrar.Text       = T("btn.permisos.cerrar",     "Cerrar");

            if (tip != null)
                tip.SetToolTip(rbCrear, T("help.permisos.rol",
                    "Rol = perfil que se asigna a un usuario. Puede contener permisos (patentes) y otros roles (rol-en-rol)."));

            ActualizarControles();
        }

        private void PanelHeader_Paint(object sender, PaintEventArgs pe)
        {
            using (var br = new LinearGradientBrush(panelHeader.ClientRectangle,
                RosaPrimario, RosaOscuro, LinearGradientMode.Horizontal))
                pe.Graphics.FillRectangle(br, panelHeader.ClientRectangle);
        }

        // ── Carga / refresco del árbol ─────────────────────────────────────────────
        private void CargarArbol()
        {
            int idPrev = _seleccionado?.Id ?? 0;
            try
            {
                _raices = _familiaBLL.ObtenerArbol();

                _todos.Clear();
                foreach (var r in _raices) Aplanar(r, new HashSet<int>());

                tvEstructura.BeginUpdate();
                tvEstructura.Nodes.Clear();
                foreach (var raiz in _raices)
                    tvEstructura.Nodes.Add(CrearNodo(raiz, new HashSet<int>()));
                tvEstructura.ExpandAll();
                tvEstructura.EndUpdate();

                // Reseleccionar el nodo previo (si sigue existiendo) o limpiar.
                _seleccionado = idPrev != 0 && _todos.ContainsKey(idPrev) ? _todos[idPrev] : null;
                if (_seleccionado != null) SeleccionarNodoPorId(tvEstructura.Nodes, idPrev);
                ActualizarSeleccion();
            }
            catch (Exception ex)
            {
                MostrarError(string.Format(T("err.generico.cargar", "Error al cargar: {0}"), ex.Message));
            }
        }

        // Aplana el árbol a un diccionario id→componente (deduplicado) para la lista de asignables.
        private void Aplanar(BE.Componente nodo, HashSet<int> vis)
        {
            if (nodo.Id != 0 && !vis.Add(nodo.Id)) return;
            if (!_todos.ContainsKey(nodo.Id)) _todos[nodo.Id] = nodo;
            foreach (var h in nodo.Hijos) Aplanar(h, vis);
        }

        private TreeNode CrearNodo(BE.Componente comp, HashSet<int> vis)
        {
            var nodo = new TreeNode(Etiqueta(comp)) { Tag = comp };
            if (comp is BE.Rol)
            {
                nodo.NodeFont  = new Font("Segoe UI", 9f, FontStyle.Bold);
                nodo.ForeColor = Color.FromArgb(176, 62, 96);
            }
            if (comp.Id != 0 && vis.Add(comp.Id))
                foreach (var h in comp.Hijos)
                    nodo.Nodes.Add(CrearNodo(h, vis));
            return nodo;
        }

        // 👥 Rol · 📁 Familia · 🔑 Patente
        private static string Etiqueta(BE.Componente c)
            => (c is BE.Rol ? "👥 " : c is BE.Familia ? "📁 " : "🔑 ") + (c?.Nombre ?? "");

        private void Tv_AfterSelect(object sender, TreeViewEventArgs e)
        {
            _seleccionado = e.Node?.Tag as BE.Componente;
            ActualizarSeleccion();
        }

        private static bool EsRol(BE.Componente c) => c is BE.Rol;

        // ── Refresco central: efectivos + combo + estado de controles ──────────────
        private void ActualizarSeleccion()
        {
            cmbAsignables.DataSource = null;

            // ComboBox de asignables (solo aplica a un Rol): patentes + roles del sistema,
            // excluyendo el propio nodo, su subárbol (evita ciclos obvios) y sus hijos directos.
            if (_seleccionado != null && EsRol(_seleccionado))
                cmbAsignables.DataSource = CalcularAsignables((BE.Rol)_seleccionado);

            ActualizarControles();
        }

        // Patentes + roles asignables a 'rol' (excluye sí mismo, su subárbol y sus hijos directos).
        private List<Item> CalcularAsignables(BE.Rol rol)
        {
            var idsHijos = new HashSet<int>();
            foreach (var h in rol.Hijos) idsHijos.Add(h.Id);

            var subarbol = new HashSet<int>();
            RecolectarSubarbol(rol, subarbol, new HashSet<int>());

            var asignables = new List<Item>();
            foreach (var kv in _todos)
            {
                var c = kv.Value;
                if (c.Id == rol.Id)          continue;
                if (idsHijos.Contains(c.Id)) continue;
                if (subarbol.Contains(c.Id)) continue;
                asignables.Add(new Item { Comp = c });
            }
            asignables.Sort((a, b) => string.Compare(a.Comp.Nombre, b.Comp.Nombre, StringComparison.OrdinalIgnoreCase));
            return asignables;
        }

        // Habilita/oculta los controles según el MODO (Crear/Editar) y la selección. (Estilo referencia.)
        private void ActualizarControles()
        {
            bool crear = rbCrear.Checked;
            grpCrear.Visible  = crear;
            grpEditar.Visible = !crear;

            bool esRol = EsRol(_seleccionado);

            // Detalle de la selección.
            lblDetalle.Text = _seleccionado == null
                ? T("lbl.permisos.detalle.vacio", "Detalle: (sin selección)")
                : T("lbl.permisos.detalle", "Detalle: ") + Etiqueta(_seleccionado);

            if (crear)
            {
                txtNombreRol.Enabled = true;
                btnCrearRaiz.Enabled = true;
                // Sub-rol solo si hay un Rol seleccionado como padre.
                btnCrearSub.Enabled  = esRol;
            }
            else
            {
                btnEditarNombre.Enabled = esRol;
                btnEliminarRol.Enabled  = esRol;
                grpAsignar.Enabled      = esRol;
                txtNombreRol.Enabled    = esRol;
                if (esRol) txtNombreRol.Text = _seleccionado.Nombre;

                // Quitar: habilitado si el nodo seleccionado cuelga de un padre en el árbol.
                btnQuitar.Enabled = tvEstructura.SelectedNode?.Parent != null;
            }
        }

        private void RecolectarSubarbol(BE.Componente nodo, HashSet<int> acc, HashSet<int> vis)
        {
            if (nodo.Id != 0 && !vis.Add(nodo.Id)) return;
            foreach (var h in nodo.Hijos) { acc.Add(h.Id); RecolectarSubarbol(h, acc, vis); }
        }

        private void RbCrear_CheckedChanged(object sender, EventArgs e)
        {
            if (rbCrear.Checked) { txtNombreRol.Text = ""; ActualizarControles(); }
        }

        private void RbEditar_CheckedChanged(object sender, EventArgs e)
        {
            if (rbEditar.Checked) ActualizarControles();
        }

        private void BtnCrearRaiz_Click(object sender, EventArgs e) => CrearRolRaiz();
        private void BtnCrearSub_Click(object sender, EventArgs e) => CrearSubRol();
        private void BtnEditarNombre_Click(object sender, EventArgs e) => EditarNombre();
        private void BtnEliminarRol_Click(object sender, EventArgs e) => EliminarRolSel();
        private void BtnAsignar_Click(object sender, EventArgs e) => Asignar();
        private void BtnQuitar_Click(object sender, EventArgs e) => QuitarItem();
        private void BtnActualizar_Click(object sender, EventArgs e) => CargarArbol();
        private void BtnExplorador_Click(object sender, EventArgs e)
        {
            if (_exploradorAbierto != null && !_exploradorAbierto.IsDisposed)
            {
                _exploradorAbierto.BringToFront();
                return;
            }
            _exploradorAbierto = new ExploradorCompositeForm();
            _exploradorAbierto.FormClosed += (s, ev) => _exploradorAbierto = null;
            _exploradorAbierto.Show(this);
        }
        private void BtnCerrar_Click(object sender, EventArgs e) => this.Close();

        // ── Acciones: modo CREAR ───────────────────────────────────────────────────
        private void CrearRolRaiz()
        {
            string nombre = txtNombreRol.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            { MostrarError(T("perm.msg.nombrevacio", "Escribí un nombre para el rol.")); return; }
            try
            {
                _familiaBLL.CrearRol(nombre);
                GUI.Menu.RefrescarSeguridadAbierta();
                MostrarOk(string.Format(T("perm.ok.rolcreado", "Rol raíz '{0}' creado."), nombre));
                txtNombreRol.Text = "";
                CargarArbol();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void CrearSubRol()
        {
            if (!(_seleccionado is BE.Rol padre))
            { MostrarError(T("perm.msg.selpadrerol", "Seleccioná un Rol del árbol para crearle un sub-rol.")); return; }

            string nombre = txtNombreRol.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            { MostrarError(T("perm.msg.nombrevacio", "Escribí un nombre para el sub-rol.")); return; }
            try
            {
                int nuevoId = _familiaBLL.CrearRol(nombre);
                _familiaBLL.AgregarComponente(padre.Id, nuevoId);   // valida ciclos en la BLL
                GUI.Menu.RefrescarSeguridadAbierta();
                MostrarOk(string.Format(T("perm.ok.subcreado", "Sub-rol '{0}' creado dentro de '{1}'."), nombre, padre.Nombre));
                txtNombreRol.Text = "";
                CargarArbol();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // ── Acciones: modo EDITAR ──────────────────────────────────────────────────
        private void EditarNombre()
        {
            if (!(_seleccionado is BE.Rol rol))
            { MostrarError(T("perm.msg.selmodrol", "Seleccioná un ROL del árbol para renombrar (los permisos no se editan).")); return; }

            string nombre = txtNombreRol.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            { MostrarError(T("perm.msg.nombrevacio", "El nombre del rol no puede estar vacío.")); return; }
            try
            {
                _familiaBLL.RenombrarComponente(rol.Id, nombre, nombre);
                GUI.Menu.RefrescarSeguridadAbierta();
                MostrarOk(string.Format(T("perm.ok.modificado", "Rol actualizado a '{0}'."), nombre));
                CargarArbol();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void EliminarRolSel()
        {
            if (!(_seleccionado is BE.Rol rol))
            { MostrarError(T("perm.msg.selelirol", "Seleccioná un ROL del árbol para eliminar (los permisos no se eliminan).")); return; }

            if (MessageBox.Show(
                    string.Format(T("perm.conf.elirol", "¿Eliminar el rol '{0}'? No se permite si tiene usuarios asignados."), rol.Nombre),
                    T("perm.conf.titulo", "Confirmar"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                string nombre = rol.Nombre;
                _familiaBLL.EliminarRol(nombre);   // valida que no tenga usuarios asignados
                _seleccionado = null;
                GUI.Menu.RefrescarSeguridadAbierta();
                MostrarOk(string.Format(T("perm.ok.eliminado", "Rol '{0}' eliminado."), nombre));
                CargarArbol();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void Asignar()
        {
            if (!(_seleccionado is BE.Rol rol))
            { MostrarError(T("perm.msg.selpadre", "Seleccioná un Rol para asignarle un permiso o rol.")); return; }
            if (!(cmbAsignables.SelectedItem is Item it))
            { MostrarError(T("perm.msg.selasignable", "Elegí un permiso o rol de la lista para asignar.")); return; }
            try
            {
                _familiaBLL.AgregarComponente(rol.Id, it.Comp.Id);
                GUI.Menu.RefrescarSeguridadAbierta();
                MostrarOk(string.Format(T("perm.ok.agregado", "'{0}' asignado a '{1}'."), it.Comp.Nombre, rol.Nombre));
                CargarArbol();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        private void QuitarItem()
        {
            var node = tvEstructura.SelectedNode;
            if (node?.Parent == null)
            { MostrarError(T("perm.msg.selquitar", "Seleccioná un ítem que cuelgue de un rol para quitarlo.")); return; }

            var padre = node.Parent.Tag as BE.Componente;
            var hijo  = node.Tag as BE.Componente;
            if (padre == null || hijo == null) return;

            if (MessageBox.Show(
                    string.Format(T("perm.conf.quitar", "¿Quitar '{0}' del rol '{1}'?"), hijo.Nombre, padre.Nombre),
                    T("perm.conf.titulo", "Confirmar"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                _familiaBLL.QuitarComponente(padre.Id, hijo.Id);
                GUI.Menu.RefrescarSeguridadAbierta();
                MostrarOk(string.Format(T("perm.ok.quitado", "'{0}' quitado de '{1}'."), hijo.Nombre, padre.Nombre));
                CargarArbol();
            }
            catch (Exception ex) { MostrarError(ex); }
        }

        // ── Helpers ────────────────────────────────────────────────────────────────
        private void SeleccionarNodoPorId(TreeNodeCollection nodos, int id)
        {
            foreach (TreeNode n in nodos)
            {
                if (n.Tag is BE.Componente c && c.Id == id) { tvEstructura.SelectedNode = n; return; }
                SeleccionarNodoPorId(n.Nodes, id);
            }
        }
    }
}
