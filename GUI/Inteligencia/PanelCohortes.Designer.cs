namespace GUI.Inteligencia
{
    partial class PanelCohortes
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                fuentePeor?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this.lblExplicacion = new System.Windows.Forms.Label();
            this.pnlResumen = new System.Windows.Forms.Panel();
            this.lblResumen = new System.Windows.Forms.Label();
            this.btnActuar = new System.Windows.Forms.Button();
            this.flpLeyenda = new System.Windows.Forms.FlowLayoutPanel();
            this.split = new System.Windows.Forms.SplitContainer();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart)).BeginInit();
            this.pnlResumen.SuspendLayout();
            this.flpLeyenda.SuspendLayout();
            this.split.SuspendLayout();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblExplicacion
            // 
            this.lblExplicacion.AutoSize = true;
            this.lblExplicacion.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblExplicacion.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblExplicacion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(80)))));
            this.lblExplicacion.Location = new System.Drawing.Point(4, 4);
            this.lblExplicacion.Name = "lblExplicacion";
            this.lblExplicacion.Padding = new System.Windows.Forms.Padding(2, 4, 2, 0);
            this.lblExplicacion.Size = new System.Drawing.Size(600, 19);
            this.lblExplicacion.TabIndex = 0;
            this.lblExplicacion.Text = "Cómo leerlo: cada fila agrupa a los clientes por el mes en que se suscribieron y cada columna muestra qué % de ellos sigue activo k meses después del alta.";
            // 
            // pnlResumen
            // 
            this.pnlResumen.Controls.Add(this.lblResumen);
            this.pnlResumen.Controls.Add(this.btnActuar);
            this.pnlResumen.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlResumen.Location = new System.Drawing.Point(4, 23);
            this.pnlResumen.Name = "pnlResumen";
            this.pnlResumen.Padding = new System.Windows.Forms.Padding(2, 4, 2, 4);
            this.pnlResumen.Size = new System.Drawing.Size(792, 54);
            this.pnlResumen.TabIndex = 1;
            // 
            // lblResumen
            // 
            this.lblResumen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblResumen.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblResumen.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(30)))), ((int)(((byte)(55)))));
            this.lblResumen.Location = new System.Drawing.Point(2, 4);
            this.lblResumen.Name = "lblResumen";
            this.lblResumen.Size = new System.Drawing.Size(558, 46);
            this.lblResumen.TabIndex = 0;
            this.lblResumen.Text = "Peor cohorte: —";
            this.lblResumen.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnActuar
            // 
            this.btnActuar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(100)))), ((int)(((byte)(135)))));
            this.btnActuar.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnActuar.Enabled = false;
            this.btnActuar.FlatAppearance.BorderSize = 0;
            this.btnActuar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnActuar.ForeColor = System.Drawing.Color.White;
            this.btnActuar.Location = new System.Drawing.Point(560, 4);
            this.btnActuar.Name = "btnActuar";
            this.btnActuar.Size = new System.Drawing.Size(230, 46);
            this.btnActuar.TabIndex = 1;
            this.btnActuar.Text = "Actuar sobre la peor cohorte";
            this.btnActuar.UseVisualStyleBackColor = false;
            this.btnActuar.Click += new System.EventHandler(this.BtnActuar_Click);
            // 
            // flpLeyenda
            // 
            this.flpLeyenda.AutoSize = true;
            this.flpLeyenda.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpLeyenda.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpLeyenda.Location = new System.Drawing.Point(4, 77);
            this.flpLeyenda.Name = "flpLeyenda";
            this.flpLeyenda.Size = new System.Drawing.Size(792, 0);
            this.flpLeyenda.TabIndex = 2;
            // 
            // split
            // 
            this.split.Panel1.Controls.Add(this.dgv);
            this.split.Panel2.Controls.Add(this.chart);
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.Location = new System.Drawing.Point(4, 77);
            this.split.Name = "split";
            this.split.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.split.Size = new System.Drawing.Size(792, 419);
            this.split.SplitterDistance = 230;
            this.split.TabIndex = 3;
            this.split.SizeChanged += new System.EventHandler(this.Split_SizeChanged);
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AllowUserToResizeRows = false;
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 0);
            this.dgv.MultiSelect = false;
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv.Size = new System.Drawing.Size(792, 230);
            this.dgv.TabIndex = 0;
            this.dgv.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Dgv_CellDoubleClick);
            this.dgv.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.Dgv_CellFormatting);
            this.dgv.CellToolTipTextNeeded += new System.Windows.Forms.DataGridViewCellToolTipTextNeededEventHandler(this.Dgv_CellToolTipTextNeeded);
            this.dgv.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.Dgv_DataBindingComplete);
            // 
            // chart
            // 
            chartArea1.Name = "principal";
            this.chart.ChartAreas.Add(chartArea1);
            legend1.Name = "leyenda";
            this.chart.Legends.Add(legend1);
            this.chart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chart.Location = new System.Drawing.Point(0, 0);
            this.chart.Name = "chart";
            this.chart.Size = new System.Drawing.Size(792, 185);
            this.chart.TabIndex = 0;
            this.chart.Text = "Retención promedio";
            // 
            // tip
            // 
            // 
            // PanelCohortes
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(251)))), ((int)(((byte)(247)))), ((int)(((byte)(248)))));
            this.Controls.Add(this.split);
            this.Controls.Add(this.flpLeyenda);
            this.Controls.Add(this.pnlResumen);
            this.Controls.Add(this.lblExplicacion);
            this.Name = "PanelCohortes";
            this.Padding = new System.Windows.Forms.Padding(4);
            this.Size = new System.Drawing.Size(800, 500);
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart)).EndInit();
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            this.split.ResumeLayout(false);
            this.flpLeyenda.ResumeLayout(false);
            this.flpLeyenda.PerformLayout();
            this.pnlResumen.ResumeLayout(false);
            this.pnlResumen.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblExplicacion;
        private System.Windows.Forms.Panel pnlResumen;
        private System.Windows.Forms.Label lblResumen;
        private System.Windows.Forms.Button btnActuar;
        private System.Windows.Forms.FlowLayoutPanel flpLeyenda;
        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart;
        private System.Windows.Forms.ToolTip tip;
    }
}
