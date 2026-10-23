namespace GUI
{
    partial class GuiaPrimerUsoForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitulo = new System.Windows.Forms.Label();
            this.flpCuerpo = new System.Windows.Forms.FlowLayoutPanel();
            this.lblIntro = new System.Windows.Forms.Label();
            this.tblPasos = new System.Windows.Forms.TableLayoutPanel();
            this.lblNumero1 = new System.Windows.Forms.Label();
            this.lblPaso1 = new System.Windows.Forms.Label();
            this.lblNumero2 = new System.Windows.Forms.Label();
            this.lblPaso2 = new System.Windows.Forms.Label();
            this.lblNumero3 = new System.Windows.Forms.Label();
            this.lblPaso3 = new System.Windows.Forms.Label();
            this.lblNumero4 = new System.Windows.Forms.Label();
            this.lblPaso4 = new System.Windows.Forms.Label();
            this.lblNumero5 = new System.Windows.Forms.Label();
            this.lblPaso5 = new System.Windows.Forms.Label();
            this.chkNoMostrar = new System.Windows.Forms.CheckBox();
            this.btnComenzar = new System.Windows.Forms.Button();
            this.flpCuerpo.SuspendLayout();
            this.tblPasos.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitulo
            // 
            this.lblTitulo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(176)))), ((int)(((byte)(62)))), ((int)(((byte)(96)))));
            this.lblTitulo.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitulo.ForeColor = System.Drawing.Color.White;
            this.lblTitulo.Location = new System.Drawing.Point(0, 0);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.lblTitulo.Size = new System.Drawing.Size(610, 46);
            this.lblTitulo.TabIndex = 1;
            this.lblTitulo.Text = "Bienvenido a WardrobeFlow";
            this.lblTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // flpCuerpo
            // 
            this.flpCuerpo.Controls.Add(this.lblIntro);
            this.flpCuerpo.Controls.Add(this.tblPasos);
            this.flpCuerpo.Controls.Add(this.chkNoMostrar);
            this.flpCuerpo.Controls.Add(this.btnComenzar);
            this.flpCuerpo.AutoSize = true;
            this.flpCuerpo.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpCuerpo.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpCuerpo.Location = new System.Drawing.Point(0, 46);
            this.flpCuerpo.Name = "flpCuerpo";
            this.flpCuerpo.Padding = new System.Windows.Forms.Padding(18, 14, 18, 0);
            this.flpCuerpo.Size = new System.Drawing.Size(610, 330);
            this.flpCuerpo.TabIndex = 0;
            this.flpCuerpo.WrapContents = false;
            // 
            // lblIntro
            // 
            this.lblIntro.AutoSize = true;
            this.lblIntro.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblIntro.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(26)))), ((int)(((byte)(32)))));
            this.lblIntro.Location = new System.Drawing.Point(18, 14);
            this.lblIntro.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.lblIntro.MaximumSize = new System.Drawing.Size(540, 0);
            this.lblIntro.Name = "lblIntro";
            this.lblIntro.Size = new System.Drawing.Size(400, 19);
            this.lblIntro.TabIndex = 0;
            this.lblIntro.Text = "Hola, usuario. Estos son los primeros pasos para su rol:";
            // 
            // tblPasos
            // 
            this.tblPasos.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tblPasos.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tblPasos.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblPasos.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblPasos.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblPasos.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblPasos.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tblPasos.Controls.Add(this.lblNumero1, 0, 0);
            this.tblPasos.Controls.Add(this.lblPaso1, 1, 0);
            this.tblPasos.Controls.Add(this.lblNumero2, 0, 1);
            this.tblPasos.Controls.Add(this.lblPaso2, 1, 1);
            this.tblPasos.Controls.Add(this.lblNumero3, 0, 2);
            this.tblPasos.Controls.Add(this.lblPaso3, 1, 2);
            this.tblPasos.Controls.Add(this.lblNumero4, 0, 3);
            this.tblPasos.Controls.Add(this.lblPaso4, 1, 3);
            this.tblPasos.Controls.Add(this.lblNumero5, 0, 4);
            this.tblPasos.Controls.Add(this.lblPaso5, 1, 4);
            this.tblPasos.AutoSize = true;
            this.tblPasos.ColumnCount = 2;
            this.tblPasos.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddRows;
            this.tblPasos.Location = new System.Drawing.Point(18, 43);
            this.tblPasos.Margin = new System.Windows.Forms.Padding(0);
            this.tblPasos.Name = "tblPasos";
            this.tblPasos.RowCount = 5;
            this.tblPasos.Size = new System.Drawing.Size(546, 200);
            this.tblPasos.TabIndex = 1;
            // 
            // lblNumero1
            // 
            this.lblNumero1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.lblNumero1.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNumero1.ForeColor = System.Drawing.Color.White;
            this.lblNumero1.Location = new System.Drawing.Point(0, 2);
            this.lblNumero1.Margin = new System.Windows.Forms.Padding(0, 2, 8, 10);
            this.lblNumero1.Name = "lblNumero1";
            this.lblNumero1.Size = new System.Drawing.Size(26, 26);
            this.lblNumero1.TabIndex = 0;
            this.lblNumero1.Text = "1";
            this.lblNumero1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPaso1
            // 
            this.lblPaso1.AutoSize = true;
            this.lblPaso1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(26)))), ((int)(((byte)(32)))));
            this.lblPaso1.Location = new System.Drawing.Point(36, 4);
            this.lblPaso1.Margin = new System.Windows.Forms.Padding(0, 4, 0, 10);
            this.lblPaso1.MaximumSize = new System.Drawing.Size(500, 0);
            this.lblPaso1.Name = "lblPaso1";
            this.lblPaso1.Size = new System.Drawing.Size(120, 15);
            this.lblPaso1.TabIndex = 1;
            this.lblPaso1.Text = "Qué hace el sistema para su rol.";
            // 
            // lblNumero2
            // 
            this.lblNumero2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.lblNumero2.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNumero2.ForeColor = System.Drawing.Color.White;
            this.lblNumero2.Location = new System.Drawing.Point(0, 2);
            this.lblNumero2.Margin = new System.Windows.Forms.Padding(0, 2, 8, 10);
            this.lblNumero2.Name = "lblNumero2";
            this.lblNumero2.Size = new System.Drawing.Size(26, 26);
            this.lblNumero2.TabIndex = 2;
            this.lblNumero2.Text = "2";
            this.lblNumero2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPaso2
            // 
            this.lblPaso2.AutoSize = true;
            this.lblPaso2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(26)))), ((int)(((byte)(32)))));
            this.lblPaso2.Location = new System.Drawing.Point(36, 4);
            this.lblPaso2.Margin = new System.Windows.Forms.Padding(0, 4, 0, 10);
            this.lblPaso2.MaximumSize = new System.Drawing.Size(500, 0);
            this.lblPaso2.Name = "lblPaso2";
            this.lblPaso2.Size = new System.Drawing.Size(120, 15);
            this.lblPaso2.TabIndex = 3;
            this.lblPaso2.Text = "Por dónde empezar.";
            // 
            // lblNumero3
            // 
            this.lblNumero3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.lblNumero3.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNumero3.ForeColor = System.Drawing.Color.White;
            this.lblNumero3.Location = new System.Drawing.Point(0, 2);
            this.lblNumero3.Margin = new System.Windows.Forms.Padding(0, 2, 8, 10);
            this.lblNumero3.Name = "lblNumero3";
            this.lblNumero3.Size = new System.Drawing.Size(26, 26);
            this.lblNumero3.TabIndex = 4;
            this.lblNumero3.Text = "3";
            this.lblNumero3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPaso3
            // 
            this.lblPaso3.AutoSize = true;
            this.lblPaso3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(26)))), ((int)(((byte)(32)))));
            this.lblPaso3.Location = new System.Drawing.Point(36, 4);
            this.lblPaso3.Margin = new System.Windows.Forms.Padding(0, 4, 0, 10);
            this.lblPaso3.MaximumSize = new System.Drawing.Size(500, 0);
            this.lblPaso3.Name = "lblPaso3";
            this.lblPaso3.Size = new System.Drawing.Size(120, 15);
            this.lblPaso3.TabIndex = 5;
            this.lblPaso3.Text = "Qué muestra su Panel de Control.";
            // 
            // lblNumero4
            // 
            this.lblNumero4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.lblNumero4.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNumero4.ForeColor = System.Drawing.Color.White;
            this.lblNumero4.Location = new System.Drawing.Point(0, 2);
            this.lblNumero4.Margin = new System.Windows.Forms.Padding(0, 2, 8, 10);
            this.lblNumero4.Name = "lblNumero4";
            this.lblNumero4.Size = new System.Drawing.Size(26, 26);
            this.lblNumero4.TabIndex = 6;
            this.lblNumero4.Text = "4";
            this.lblNumero4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPaso4
            // 
            this.lblPaso4.AutoSize = true;
            this.lblPaso4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(26)))), ((int)(((byte)(32)))));
            this.lblPaso4.Location = new System.Drawing.Point(36, 4);
            this.lblPaso4.Margin = new System.Windows.Forms.Padding(0, 4, 0, 10);
            this.lblPaso4.MaximumSize = new System.Drawing.Size(500, 0);
            this.lblPaso4.Name = "lblPaso4";
            this.lblPaso4.Size = new System.Drawing.Size(120, 15);
            this.lblPaso4.TabIndex = 7;
            this.lblPaso4.Text = "La barra inferior le indica dónde está y qué puede hacer en cada pantalla.";
            // 
            // lblNumero5
            // 
            this.lblNumero5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.lblNumero5.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNumero5.ForeColor = System.Drawing.Color.White;
            this.lblNumero5.Location = new System.Drawing.Point(0, 2);
            this.lblNumero5.Margin = new System.Windows.Forms.Padding(0, 2, 8, 10);
            this.lblNumero5.Name = "lblNumero5";
            this.lblNumero5.Size = new System.Drawing.Size(26, 26);
            this.lblNumero5.TabIndex = 8;
            this.lblNumero5.Text = "5";
            this.lblNumero5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPaso5
            // 
            this.lblPaso5.AutoSize = true;
            this.lblPaso5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(26)))), ((int)(((byte)(32)))));
            this.lblPaso5.Location = new System.Drawing.Point(36, 4);
            this.lblPaso5.Margin = new System.Windows.Forms.Padding(0, 4, 0, 10);
            this.lblPaso5.MaximumSize = new System.Drawing.Size(500, 0);
            this.lblPaso5.Name = "lblPaso5";
            this.lblPaso5.Size = new System.Drawing.Size(120, 15);
            this.lblPaso5.TabIndex = 9;
            this.lblPaso5.Text = "Presione F1 en cualquier pantalla para ver su ayuda.";
            // 
            // chkNoMostrar
            // 
            this.chkNoMostrar.AutoSize = true;
            this.chkNoMostrar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(80)))));
            this.chkNoMostrar.Location = new System.Drawing.Point(18, 255);
            this.chkNoMostrar.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.chkNoMostrar.Name = "chkNoMostrar";
            this.chkNoMostrar.Size = new System.Drawing.Size(250, 19);
            this.chkNoMostrar.TabIndex = 2;
            this.chkNoMostrar.Text = "No volver a mostrar al iniciar sesión";
            this.chkNoMostrar.UseVisualStyleBackColor = true;
            // 
            // btnComenzar
            // 
            this.btnComenzar.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnComenzar.AutoSize = true;
            this.btnComenzar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnComenzar.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnComenzar.FlatAppearance.BorderSize = 0;
            this.btnComenzar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnComenzar.ForeColor = System.Drawing.Color.White;
            this.btnComenzar.Location = new System.Drawing.Point(18, 282);
            this.btnComenzar.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.btnComenzar.MinimumSize = new System.Drawing.Size(120, 32);
            this.btnComenzar.Name = "btnComenzar";
            this.btnComenzar.Size = new System.Drawing.Size(120, 32);
            this.btnComenzar.TabIndex = 3;
            this.btnComenzar.Text = "Comenzar";
            this.btnComenzar.UseVisualStyleBackColor = false;
            // 
            // GuiaPrimerUsoForm
            // 
            this.AcceptButton = this.btnComenzar;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(247)))), ((int)(((byte)(248)))));
            this.CancelButton = this.btnComenzar;
            this.ClientSize = new System.Drawing.Size(610, 388);
            this.Controls.Add(this.flpCuerpo);
            this.Controls.Add(this.lblTitulo);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(610, 0);
            this.Name = "GuiaPrimerUsoForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Bienvenido a WardrobeFlow";
            this.tblPasos.ResumeLayout(false);
            this.tblPasos.PerformLayout();
            this.flpCuerpo.ResumeLayout(false);
            this.flpCuerpo.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.FlowLayoutPanel flpCuerpo;
        private System.Windows.Forms.Label lblIntro;
        private System.Windows.Forms.TableLayoutPanel tblPasos;
        private System.Windows.Forms.Label lblNumero1;
        private System.Windows.Forms.Label lblPaso1;
        private System.Windows.Forms.Label lblNumero2;
        private System.Windows.Forms.Label lblPaso2;
        private System.Windows.Forms.Label lblNumero3;
        private System.Windows.Forms.Label lblPaso3;
        private System.Windows.Forms.Label lblNumero4;
        private System.Windows.Forms.Label lblPaso4;
        private System.Windows.Forms.Label lblNumero5;
        private System.Windows.Forms.Label lblPaso5;
        private System.Windows.Forms.CheckBox chkNoMostrar;
        private System.Windows.Forms.Button btnComenzar;
    }
}
