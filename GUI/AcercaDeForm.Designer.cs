namespace GUI
{
    partial class AcercaDeForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblProducto         = new System.Windows.Forms.Label();
            this.flpCuerpo           = new System.Windows.Forms.FlowLayoutPanel();
            this.lblDescripcion      = new System.Windows.Forms.Label();
            this.tblDatos            = new System.Windows.Forms.TableLayoutPanel();
            this.lblVersion          = new System.Windows.Forms.Label();
            this.lblVersionValor     = new System.Windows.Forms.Label();
            this.lblCompilacion      = new System.Windows.Forms.Label();
            this.lblCompilacionValor = new System.Windows.Forms.Label();
            this.lblServidor         = new System.Windows.Forms.Label();
            this.lblServidorValor    = new System.Windows.Forms.Label();
            this.lblBaseDatos        = new System.Windows.Forms.Label();
            this.lblBaseDatosValor   = new System.Windows.Forms.Label();
            this.lblAutora           = new System.Windows.Forms.Label();
            this.lblAutoraValor      = new System.Windows.Forms.Label();
            this.lblTrabajo          = new System.Windows.Forms.Label();
            this.btnCerrar           = new System.Windows.Forms.Button();
            this.flpCuerpo.SuspendLayout();
            this.tblDatos.SuspendLayout();
            this.SuspendLayout();

            // lblProducto — encabezado (mismo estilo que la guía de inicio)
            this.lblProducto.BackColor = Tema.RosaOscuro;
            this.lblProducto.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblProducto.Font = new System.Drawing.Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold);
            this.lblProducto.ForeColor = System.Drawing.Color.White;
            this.lblProducto.Height = 46;
            this.lblProducto.Name = "lblProducto";
            this.lblProducto.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.lblProducto.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblProducto.Text = "WardrobeFlow";

            // flpCuerpo
            this.flpCuerpo.AutoSize = true;
            this.flpCuerpo.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpCuerpo.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpCuerpo.Name = "flpCuerpo";
            this.flpCuerpo.Padding = new System.Windows.Forms.Padding(18, 14, 18, 0);
            this.flpCuerpo.WrapContents = false;
            this.flpCuerpo.Controls.Add(this.lblDescripcion);
            this.flpCuerpo.Controls.Add(this.tblDatos);
            this.flpCuerpo.Controls.Add(this.lblTrabajo);
            this.flpCuerpo.Controls.Add(this.btnCerrar);

            // lblDescripcion
            this.lblDescripcion.AutoSize = true;
            this.lblDescripcion.Font = Tema.FuenteSubtitulo;
            this.lblDescripcion.ForeColor = Tema.Tinta;
            this.lblDescripcion.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.lblDescripcion.MaximumSize = new System.Drawing.Size(420, 0);
            this.lblDescripcion.Name = "lblDescripcion";
            this.lblDescripcion.Text = "Gestión de suscripciones de alquiler de indumentaria.";

            // tblDatos — etiqueta | valor
            this.tblDatos.AutoSize = true;
            this.tblDatos.ColumnCount = 2;
            this.tblDatos.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblDatos.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblDatos.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddRows;
            this.tblDatos.Margin = new System.Windows.Forms.Padding(0);
            this.tblDatos.Name = "tblDatos";
            this.tblDatos.RowCount = 5;
            this.tblDatos.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblDatos.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblDatos.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblDatos.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblDatos.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblDatos.Controls.Add(this.lblVersion, 0, 0);
            this.tblDatos.Controls.Add(this.lblVersionValor, 1, 0);
            this.tblDatos.Controls.Add(this.lblCompilacion, 0, 1);
            this.tblDatos.Controls.Add(this.lblCompilacionValor, 1, 1);
            this.tblDatos.Controls.Add(this.lblServidor, 0, 2);
            this.tblDatos.Controls.Add(this.lblServidorValor, 1, 2);
            this.tblDatos.Controls.Add(this.lblBaseDatos, 0, 3);
            this.tblDatos.Controls.Add(this.lblBaseDatosValor, 1, 3);
            this.tblDatos.Controls.Add(this.lblAutora, 0, 4);
            this.tblDatos.Controls.Add(this.lblAutoraValor, 1, 4);

            // Etiquetas (columna izquierda)
            this.lblVersion.AutoSize = true;
            this.lblVersion.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            this.lblVersion.ForeColor = Tema.TextoSecundario;
            this.lblVersion.Margin = new System.Windows.Forms.Padding(0, 0, 16, 8);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Text = "Versión";

            this.lblCompilacion.AutoSize = true;
            this.lblCompilacion.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            this.lblCompilacion.ForeColor = Tema.TextoSecundario;
            this.lblCompilacion.Margin = new System.Windows.Forms.Padding(0, 0, 16, 8);
            this.lblCompilacion.Name = "lblCompilacion";
            this.lblCompilacion.Text = "Compilación";

            this.lblServidor.AutoSize = true;
            this.lblServidor.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            this.lblServidor.ForeColor = Tema.TextoSecundario;
            this.lblServidor.Margin = new System.Windows.Forms.Padding(0, 0, 16, 8);
            this.lblServidor.Name = "lblServidor";
            this.lblServidor.Text = "Servidor";

            this.lblBaseDatos.AutoSize = true;
            this.lblBaseDatos.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            this.lblBaseDatos.ForeColor = Tema.TextoSecundario;
            this.lblBaseDatos.Margin = new System.Windows.Forms.Padding(0, 0, 16, 8);
            this.lblBaseDatos.Name = "lblBaseDatos";
            this.lblBaseDatos.Text = "Base de datos";

            this.lblAutora.AutoSize = true;
            this.lblAutora.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            this.lblAutora.ForeColor = Tema.TextoSecundario;
            this.lblAutora.Margin = new System.Windows.Forms.Padding(0, 0, 16, 8);
            this.lblAutora.Name = "lblAutora";
            this.lblAutora.Text = "Autora";

            // Valores (columna derecha): se completan en tiempo de ejecución (AcercaDeForm.Traducir)
            this.lblVersionValor.AutoSize = true;
            this.lblVersionValor.Font = Tema.FuenteNormal;
            this.lblVersionValor.ForeColor = Tema.Tinta;
            this.lblVersionValor.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblVersionValor.MaximumSize = new System.Drawing.Size(300, 0);
            this.lblVersionValor.Name = "lblVersionValor";
            this.lblVersionValor.Text = "—";

            this.lblCompilacionValor.AutoSize = true;
            this.lblCompilacionValor.Font = Tema.FuenteNormal;
            this.lblCompilacionValor.ForeColor = Tema.Tinta;
            this.lblCompilacionValor.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblCompilacionValor.MaximumSize = new System.Drawing.Size(300, 0);
            this.lblCompilacionValor.Name = "lblCompilacionValor";
            this.lblCompilacionValor.Text = "—";

            this.lblServidorValor.AutoSize = true;
            this.lblServidorValor.Font = Tema.FuenteNormal;
            this.lblServidorValor.ForeColor = Tema.Tinta;
            this.lblServidorValor.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblServidorValor.MaximumSize = new System.Drawing.Size(300, 0);
            this.lblServidorValor.Name = "lblServidorValor";
            this.lblServidorValor.Text = "—";

            this.lblBaseDatosValor.AutoSize = true;
            this.lblBaseDatosValor.Font = Tema.FuenteNormal;
            this.lblBaseDatosValor.ForeColor = Tema.Tinta;
            this.lblBaseDatosValor.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblBaseDatosValor.MaximumSize = new System.Drawing.Size(300, 0);
            this.lblBaseDatosValor.Name = "lblBaseDatosValor";
            this.lblBaseDatosValor.Text = "—";

            this.lblAutoraValor.AutoSize = true;
            this.lblAutoraValor.Font = Tema.FuenteNormal;
            this.lblAutoraValor.ForeColor = Tema.Tinta;
            this.lblAutoraValor.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblAutoraValor.MaximumSize = new System.Drawing.Size(300, 0);
            this.lblAutoraValor.Name = "lblAutoraValor";
            this.lblAutoraValor.Text = "Valentina Morana";

            // lblTrabajo
            this.lblTrabajo.AutoSize = true;
            this.lblTrabajo.Font = Tema.FuenteNormal;
            this.lblTrabajo.ForeColor = Tema.TextoSecundario;
            this.lblTrabajo.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.lblTrabajo.MaximumSize = new System.Drawing.Size(420, 0);
            this.lblTrabajo.Name = "lblTrabajo";
            this.lblTrabajo.Text = "Trabajo de Diploma – Ingeniería en Sistemas – UAI – 2026";

            // btnCerrar
            this.btnCerrar.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnCerrar.AutoSize = true;
            this.btnCerrar.BackColor = Tema.RosaPrimario;
            this.btnCerrar.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCerrar.FlatAppearance.BorderSize = 0;
            this.btnCerrar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCerrar.ForeColor = System.Drawing.Color.White;
            this.btnCerrar.Margin = new System.Windows.Forms.Padding(0, 16, 0, 0);
            this.btnCerrar.MinimumSize = new System.Drawing.Size(120, 32);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.TabIndex = 0;
            this.btnCerrar.Text = "Cerrar";
            this.btnCerrar.UseVisualStyleBackColor = false;

            // AcercaDeForm
            this.AcceptButton = this.btnCerrar;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = Tema.Papel;
            this.CancelButton = this.btnCerrar;
            this.Controls.Add(this.flpCuerpo);
            this.Controls.Add(this.lblProducto);
            this.Font = Tema.FuenteNormal;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(480, 0);
            this.Name = "AcercaDeForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Acerca de WardrobeFlow";

            this.tblDatos.ResumeLayout(false);
            this.tblDatos.PerformLayout();
            this.flpCuerpo.ResumeLayout(false);
            this.flpCuerpo.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label            lblProducto;
        private System.Windows.Forms.FlowLayoutPanel  flpCuerpo;
        private System.Windows.Forms.Label            lblDescripcion;
        private System.Windows.Forms.TableLayoutPanel tblDatos;
        private System.Windows.Forms.Label            lblVersion;
        private System.Windows.Forms.Label            lblVersionValor;
        private System.Windows.Forms.Label            lblCompilacion;
        private System.Windows.Forms.Label            lblCompilacionValor;
        private System.Windows.Forms.Label            lblServidor;
        private System.Windows.Forms.Label            lblServidorValor;
        private System.Windows.Forms.Label            lblBaseDatos;
        private System.Windows.Forms.Label            lblBaseDatosValor;
        private System.Windows.Forms.Label            lblAutora;
        private System.Windows.Forms.Label            lblAutoraValor;
        private System.Windows.Forms.Label            lblTrabajo;
        private System.Windows.Forms.Button           btnCerrar;
    }
}
