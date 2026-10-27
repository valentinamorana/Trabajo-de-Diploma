using System.Drawing;
using System.Windows.Forms;

namespace GUI.Estilos
{
    /// <summary>
    /// Paleta y helpers de estilo compartidos por los formularios de decisión/reporte
    /// (Renovación, Cobro, y los 6 reportes de decisión: Abandono, Ventas por Vendedor,
    /// Rotación, Mantenimiento, Escasez, Recomendación de Prendas) para que sigan la misma
    /// identidad visual que ya usan ClienteForm/CambioEstadoDialog/DashboardDeposito/Menu
    /// (rosa de marca), en vez de quedar con los botones y grillas grises default de
    /// WinForms. Pura presentación (colores/fuentes) — ninguna decisión de negocio vive acá.
    /// </summary>
    public static class EstiloFormulario
    {
        // Alias de los tokens de GUI/Tema.cs (única fuente de la paleta): antes RosaOscuro y
        // las variantes claras se redefinían acá como literales.
        public static readonly Color Rosa         = Tema.RosaPrimario;
        public static readonly Color RosaOscuro   = Tema.RosaOscuro;
        public static readonly Color RosaClara    = Tema.RosaClara;
        public static readonly Color RosaMuyClara = Tema.RosaMuyClara;

        /// <summary>Botón de acción principal (Generar/Procesar/Confirmar) — mismo patrón
        /// que ClienteForm.btnGuardar y CambioEstadoDialog.btnConfirmar.</summary>
        public static void BotonPrimario(Button btn)
        {
            btn.UseVisualStyleBackColor = false;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.EnabledChanged -= PintarPrimario;
            btn.EnabledChanged += PintarPrimario;
            PintarPrimario(btn, System.EventArgs.Empty);
        }

        /// <summary>Botón de acción secundaria (Exportar PDF/CSV, Reanudar) — contorno
        /// rosa sobre fondo blanco, para no competir con el botón primario del formulario.</summary>
        public static void BotonSecundario(Button btn)
        {
            btn.UseVisualStyleBackColor = false;
            btn.BackColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.EnabledChanged -= PintarSecundario;
            btn.EnabledChanged += PintarSecundario;
            PintarSecundario(btn, System.EventArgs.Empty);
        }

        // Un botón Flat deshabilitado conserva su BackColor y WinForms solo agrisa el texto: el botón
        // rosa seguía pareciendo activo y "no hacía nada" al pulsarlo (p. ej. «Actuar sobre la peor
        // cohorte» sin peor cohorte). Deshabilitado se pinta gris para que se note que no está disponible.
        private static void PintarPrimario(object sender, System.EventArgs e)
        {
            var btn = (Button)sender;
            btn.BackColor = btn.Enabled ? Rosa : Tema.Borde;
            btn.ForeColor = btn.Enabled ? Color.White : Tema.TextoMuted;
        }

        private static void PintarSecundario(object sender, System.EventArgs e)
        {
            var btn = (Button)sender;
            btn.ForeColor = btn.Enabled ? RosaOscuro : Tema.TextoMuted;
            btn.FlatAppearance.BorderColor = btn.Enabled ? Rosa : Tema.Borde;
        }

        /// <summary>Estilo de grilla de reportes: encabezado con tinte rosa, filas
        /// alternadas y selección en el color de marca — reemplaza el DataGridView
        /// 100% default que traían estos 6 formularios.</summary>
        public static void Grilla(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.BorderStyle = BorderStyle.None;
            dgv.RowHeadersVisible = false;
            dgv.BackgroundColor = Color.White;
            dgv.GridColor = Tema.Borde;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = RosaClara;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = RosaOscuro;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = RosaClara;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = RosaOscuro;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 32;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = RosaMuyClara;
            dgv.DefaultCellStyle.SelectionBackColor = Rosa;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        }
    }
}
