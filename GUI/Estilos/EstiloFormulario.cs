using System.Drawing;
using System.Windows.Forms;

namespace GUI.Estilos
{
    /// <summary>
    /// Paleta y helpers de estilo compartidos por los formularios de decisión/reporte
    /// (Renovación, Cobro, y los 6 de Analítica de Negocio: Abandono, Ventas por Vendedor,
    /// Rotación, Mantenimiento, Escasez, Recomendación de Prendas) para que sigan la misma
    /// identidad visual que ya usan ClienteForm/CambioEstadoDialog/DashboardControlStock/Menu
    /// (rosa de marca), en vez de quedar con los botones y grillas grises default de
    /// WinForms. Pura presentación (colores/fuentes) — ninguna decisión de negocio vive acá.
    /// </summary>
    public static class EstiloFormulario
    {
        public static readonly Color Rosa         = Color.FromArgb(210, 100, 135);
        public static readonly Color RosaOscuro   = Color.FromArgb(176, 62, 96);
        public static readonly Color RosaClara    = Color.FromArgb(245, 222, 230);
        public static readonly Color RosaMuyClara = Color.FromArgb(250, 244, 246);

        /// <summary>Botón de acción principal (Generar/Procesar/Confirmar) — mismo patrón
        /// que ClienteForm.btnGuardar y CambioEstadoDialog.btnConfirmar.</summary>
        public static void BotonPrimario(Button btn)
        {
            btn.UseVisualStyleBackColor = false;
            btn.BackColor = Rosa;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
        }

        /// <summary>Botón de acción secundaria (Exportar PDF/CSV, Reanudar) — contorno
        /// rosa sobre fondo blanco, para no competir con el botón primario del formulario.</summary>
        public static void BotonSecundario(Button btn)
        {
            btn.UseVisualStyleBackColor = false;
            btn.BackColor = Color.White;
            btn.ForeColor = RosaOscuro;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Rosa;
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
            dgv.GridColor = Color.FromArgb(230, 225, 228);

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
