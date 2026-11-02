using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    partial class FormEstadisticas
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

    #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panelFiltros = new Panel();
            etiquetaDesde = new Label();
            etiquetaHasta = new Label();
            etiquetaCaneca = new Label();
            etiquetaMaterial = new Label();
            etiquetaUsuario = new Label();
            etiquetaVerificacion = new Label();
            etiquetaCriterio = new Label();
            etiquetaAgrupacion = new Label();
            fechaDesde = new DateTimePicker();
            fechaHasta = new DateTimePicker();
            comboCaneca = new ComboBox();
            comboMaterial = new ComboBox();
            comboVerificacion = new ComboBox();
            comboCriterio = new ComboBox();
            comboAgrupacion = new ComboBox();
            numeroUsuario = new NumericUpDown();
            botonConsultar = new Button();
            textoResumen = new TextBox();
            pestanas = new TabControl();
            pestanaAgrupado = new TabPage();
            pestanaFallos = new TabPage();
            pestanaDetalle = new TabPage();
            tablaAgrupada = new DataGridView();
            tablaFallos = new DataGridView();
            tablaDepositos = new DataGridView();
            panelPaginas = new Panel();
            botonAnterior = new Button();
            etiquetaPagina = new Label();
            botonSiguiente = new Button();
            etiquetaEstado = new Label();
            panelFiltros.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numeroUsuario).BeginInit();
            pestanas.SuspendLayout();
            pestanaAgrupado.SuspendLayout();
            pestanaFallos.SuspendLayout();
            pestanaDetalle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)tablaAgrupada).BeginInit();
            ((System.ComponentModel.ISupportInitialize)tablaFallos).BeginInit();
            ((System.ComponentModel.ISupportInitialize)tablaDepositos).BeginInit();
            panelPaginas.SuspendLayout();
            SuspendLayout();
            //
            // panelFiltros
            //
            panelFiltros.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelFiltros.Controls.Add(etiquetaDesde);
            panelFiltros.Controls.Add(etiquetaHasta);
            panelFiltros.Controls.Add(etiquetaCaneca);
            panelFiltros.Controls.Add(etiquetaMaterial);
            panelFiltros.Controls.Add(etiquetaUsuario);
            panelFiltros.Controls.Add(etiquetaVerificacion);
            panelFiltros.Controls.Add(etiquetaCriterio);
            panelFiltros.Controls.Add(etiquetaAgrupacion);
            panelFiltros.Controls.Add(fechaDesde);
            panelFiltros.Controls.Add(fechaHasta);
            panelFiltros.Controls.Add(comboCaneca);
            panelFiltros.Controls.Add(comboMaterial);
            panelFiltros.Controls.Add(comboVerificacion);
            panelFiltros.Controls.Add(comboCriterio);
            panelFiltros.Controls.Add(comboAgrupacion);
            panelFiltros.Controls.Add(numeroUsuario);
            panelFiltros.Controls.Add(botonConsultar);
            panelFiltros.Location = new Point(12, 12);
            panelFiltros.Name = "panelFiltros";
            panelFiltros.Size = new Size(1076, 132);
            panelFiltros.TabIndex = 0;
            //
            // etiquetaDesde
            //
            etiquetaDesde.AutoSize = true;
            etiquetaDesde.Location = new Point(10, 8);
            etiquetaDesde.Name = "etiquetaDesde";
            etiquetaDesde.Text = "Desde (día)";
            //
            // etiquetaHasta
            //
            etiquetaHasta.AutoSize = true;
            etiquetaHasta.Location = new Point(180, 8);
            etiquetaHasta.Name = "etiquetaHasta";
            etiquetaHasta.Text = "Hasta (día)";
            //
            // etiquetaCaneca
            //
            etiquetaCaneca.AutoSize = true;
            etiquetaCaneca.Location = new Point(350, 8);
            etiquetaCaneca.Name = "etiquetaCaneca";
            etiquetaCaneca.Text = "Caneca";
            //
            // etiquetaMaterial
            //
            etiquetaMaterial.AutoSize = true;
            etiquetaMaterial.Location = new Point(600, 8);
            etiquetaMaterial.Name = "etiquetaMaterial";
            etiquetaMaterial.Text = "Material";
            //
            // etiquetaUsuario
            //
            etiquetaUsuario.AutoSize = true;
            etiquetaUsuario.Location = new Point(850, 8);
            etiquetaUsuario.Name = "etiquetaUsuario";
            etiquetaUsuario.Text = "Id usuario (0 = todos)";
            //
            // etiquetaVerificacion
            //
            etiquetaVerificacion.AutoSize = true;
            etiquetaVerificacion.Location = new Point(10, 70);
            etiquetaVerificacion.Name = "etiquetaVerificacion";
            etiquetaVerificacion.Text = "Verificación";
            //
            // etiquetaCriterio
            //
            etiquetaCriterio.AutoSize = true;
            etiquetaCriterio.Location = new Point(220, 70);
            etiquetaCriterio.Name = "etiquetaCriterio";
            etiquetaCriterio.Text = "Criterio de material";
            //
            // etiquetaAgrupacion
            //
            etiquetaAgrupacion.AutoSize = true;
            etiquetaAgrupacion.Location = new Point(500, 70);
            etiquetaAgrupacion.Name = "etiquetaAgrupacion";
            etiquetaAgrupacion.Text = "Agrupar por";
            //
            // fechaDesde
            //
            fechaDesde.Format = DateTimePickerFormat.Short;
            fechaDesde.Location = new Point(10, 30);
            fechaDesde.Name = "fechaDesde";
            fechaDesde.Size = new Size(160, 27);
            fechaDesde.TabIndex = 0;
            //
            // fechaHasta
            //
            fechaHasta.Format = DateTimePickerFormat.Short;
            fechaHasta.Location = new Point(180, 30);
            fechaHasta.Name = "fechaHasta";
            fechaHasta.Size = new Size(160, 27);
            fechaHasta.TabIndex = 1;
            //
            // comboCaneca
            //
            comboCaneca.DropDownStyle = ComboBoxStyle.DropDownList;
            comboCaneca.Location = new Point(350, 30);
            comboCaneca.Name = "comboCaneca";
            comboCaneca.Size = new Size(240, 28);
            comboCaneca.TabIndex = 2;
            //
            // comboMaterial
            //
            comboMaterial.DropDownStyle = ComboBoxStyle.DropDownList;
            comboMaterial.Location = new Point(600, 30);
            comboMaterial.Name = "comboMaterial";
            comboMaterial.Size = new Size(240, 28);
            comboMaterial.TabIndex = 3;
            //
            // comboVerificacion
            //
            comboVerificacion.DropDownStyle = ComboBoxStyle.DropDownList;
            comboVerificacion.Location = new Point(10, 92);
            comboVerificacion.Name = "comboVerificacion";
            comboVerificacion.Size = new Size(200, 28);
            comboVerificacion.TabIndex = 5;
            //
            // comboCriterio
            //
            comboCriterio.DropDownStyle = ComboBoxStyle.DropDownList;
            comboCriterio.Location = new Point(220, 92);
            comboCriterio.Name = "comboCriterio";
            comboCriterio.Size = new Size(270, 28);
            comboCriterio.TabIndex = 6;
            //
            // comboAgrupacion
            //
            comboAgrupacion.DropDownStyle = ComboBoxStyle.DropDownList;
            comboAgrupacion.Location = new Point(500, 92);
            comboAgrupacion.Name = "comboAgrupacion";
            comboAgrupacion.Size = new Size(240, 28);
            comboAgrupacion.TabIndex = 7;
            //
            // numeroUsuario
            //
            numeroUsuario.Location = new Point(850, 30);
            numeroUsuario.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numeroUsuario.Name = "numeroUsuario";
            numeroUsuario.Size = new Size(160, 27);
            numeroUsuario.TabIndex = 4;
            //
            // botonConsultar
            //
            botonConsultar.Location = new Point(760, 90);
            botonConsultar.Name = "botonConsultar";
            botonConsultar.Size = new Size(250, 34);
            botonConsultar.TabIndex = 8;
            botonConsultar.Enabled = false;
            botonConsultar.Text = "Consultar";
            botonConsultar.UseVisualStyleBackColor = true;
            botonConsultar.Click += BotonConsultar_Click;
            //
            // textoResumen
            //
            textoResumen.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textoResumen.Location = new Point(12, 150);
            textoResumen.Multiline = true;
            textoResumen.Name = "textoResumen";
            textoResumen.ReadOnly = true;
            textoResumen.ScrollBars = ScrollBars.Vertical;
            textoResumen.Size = new Size(1076, 150);
            textoResumen.TabIndex = 1;
            //
            // pestanas
            //
            pestanas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pestanas.Controls.Add(pestanaAgrupado);
            pestanas.Controls.Add(pestanaFallos);
            pestanas.Controls.Add(pestanaDetalle);
            pestanas.Location = new Point(12, 308);
            pestanas.Name = "pestanas";
            pestanas.SelectedIndex = 0;
            pestanas.Size = new Size(1076, 370);
            pestanas.TabIndex = 2;
            //
            // pestanaAgrupado
            //
            pestanaAgrupado.Controls.Add(tablaAgrupada);
            pestanaAgrupado.Location = new Point(4, 29);
            pestanaAgrupado.Name = "pestanaAgrupado";
            pestanaAgrupado.Padding = new Padding(3);
            pestanaAgrupado.Size = new Size(1068, 337);
            pestanaAgrupado.TabIndex = 0;
            pestanaAgrupado.Text = "Depósitos agrupados";
            pestanaAgrupado.UseVisualStyleBackColor = true;
            //
            // pestanaFallos
            //
            pestanaFallos.Controls.Add(tablaFallos);
            pestanaFallos.Location = new Point(4, 29);
            pestanaFallos.Name = "pestanaFallos";
            pestanaFallos.Padding = new Padding(3);
            pestanaFallos.Size = new Size(1068, 337);
            pestanaFallos.TabIndex = 1;
            pestanaFallos.Text = "Incidencias por caneca";
            pestanaFallos.UseVisualStyleBackColor = true;
            //
            // pestanaDetalle
            //
            pestanaDetalle.Controls.Add(tablaDepositos);
            pestanaDetalle.Controls.Add(panelPaginas);
            pestanaDetalle.Location = new Point(4, 29);
            pestanaDetalle.Name = "pestanaDetalle";
            pestanaDetalle.Padding = new Padding(3);
            pestanaDetalle.Size = new Size(1068, 337);
            pestanaDetalle.TabIndex = 2;
            pestanaDetalle.Text = "Detalle de depósitos";
            pestanaDetalle.UseVisualStyleBackColor = true;
            //
            // tablaAgrupada
            //
            tablaAgrupada.AllowUserToAddRows = false;
            tablaAgrupada.AllowUserToDeleteRows = false;
            tablaAgrupada.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tablaAgrupada.Dock = DockStyle.Fill;
            tablaAgrupada.MultiSelect = false;
            tablaAgrupada.Name = "tablaAgrupada";
            tablaAgrupada.ReadOnly = true;
            tablaAgrupada.RowHeadersVisible = false;
            tablaAgrupada.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablaAgrupada.TabIndex = 0;
            //
            // tablaFallos
            //
            tablaFallos.AllowUserToAddRows = false;
            tablaFallos.AllowUserToDeleteRows = false;
            tablaFallos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tablaFallos.Dock = DockStyle.Fill;
            tablaFallos.MultiSelect = false;
            tablaFallos.Name = "tablaFallos";
            tablaFallos.ReadOnly = true;
            tablaFallos.RowHeadersVisible = false;
            tablaFallos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablaFallos.TabIndex = 0;
            //
            // tablaDepositos
            //
            tablaDepositos.AllowUserToAddRows = false;
            tablaDepositos.AllowUserToDeleteRows = false;
            tablaDepositos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tablaDepositos.Dock = DockStyle.Fill;
            tablaDepositos.MultiSelect = false;
            tablaDepositos.Name = "tablaDepositos";
            tablaDepositos.ReadOnly = true;
            tablaDepositos.RowHeadersVisible = false;
            tablaDepositos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablaDepositos.TabIndex = 0;
            //
            // panelPaginas
            //
            panelPaginas.Controls.Add(botonAnterior);
            panelPaginas.Controls.Add(etiquetaPagina);
            panelPaginas.Controls.Add(botonSiguiente);
            panelPaginas.Dock = DockStyle.Bottom;
            panelPaginas.Name = "panelPaginas";
            panelPaginas.Size = new Size(1062, 44);
            panelPaginas.TabIndex = 1;
            //
            // botonAnterior
            //
            botonAnterior.Enabled = false;
            botonAnterior.Location = new Point(4, 5);
            botonAnterior.Name = "botonAnterior";
            botonAnterior.Size = new Size(124, 34);
            botonAnterior.TabIndex = 0;
            botonAnterior.Text = "Anterior";
            botonAnterior.UseVisualStyleBackColor = true;
            botonAnterior.Click += BotonAnterior_Click;
            //
            // botonSiguiente
            //
            botonSiguiente.Enabled = false;
            botonSiguiente.Location = new Point(136, 5);
            botonSiguiente.Name = "botonSiguiente";
            botonSiguiente.Size = new Size(124, 34);
            botonSiguiente.TabIndex = 1;
            botonSiguiente.Text = "Siguiente";
            botonSiguiente.UseVisualStyleBackColor = true;
            botonSiguiente.Click += BotonSiguiente_Click;
            //
            // etiquetaPagina
            //
            etiquetaPagina.AutoSize = true;
            etiquetaPagina.Location = new Point(272, 12);
            etiquetaPagina.Name = "etiquetaPagina";
            etiquetaPagina.Text = "";
            //
            // etiquetaEstado
            //
            etiquetaEstado.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            etiquetaEstado.Location = new Point(12, 684);
            etiquetaEstado.Name = "etiquetaEstado";
            etiquetaEstado.Size = new Size(1076, 48);
            etiquetaEstado.Text = "";
            //
            // FormEstadisticas
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 740);
            Controls.Add(panelFiltros);
            Controls.Add(textoResumen);
            Controls.Add(pestanas);
            Controls.Add(etiquetaEstado);
            MinimumSize = new Size(1040, 640);
            Name = "FormEstadisticas";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Estadísticas de depósitos";
            Load += FormEstadisticas_Load;
            panelFiltros.ResumeLayout(false);
            panelFiltros.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numeroUsuario).EndInit();
            pestanas.ResumeLayout(false);
            pestanaAgrupado.ResumeLayout(false);
            pestanaFallos.ResumeLayout(false);
            pestanaDetalle.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)tablaAgrupada).EndInit();
            ((System.ComponentModel.ISupportInitialize)tablaFallos).EndInit();
            ((System.ComponentModel.ISupportInitialize)tablaDepositos).EndInit();
            panelPaginas.ResumeLayout(false);
            panelPaginas.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

    #endregion

        private Panel panelFiltros;
        private Label etiquetaDesde;
        private Label etiquetaHasta;
        private Label etiquetaCaneca;
        private Label etiquetaMaterial;
        private Label etiquetaUsuario;
        private Label etiquetaVerificacion;
        private Label etiquetaCriterio;
        private Label etiquetaAgrupacion;
        private DateTimePicker fechaDesde;
        private DateTimePicker fechaHasta;
        private ComboBox comboCaneca;
        private ComboBox comboMaterial;
        private ComboBox comboVerificacion;
        private ComboBox comboCriterio;
        private ComboBox comboAgrupacion;
        private NumericUpDown numeroUsuario;
        private Button botonConsultar;
        private TextBox textoResumen;
        private TabControl pestanas;
        private TabPage pestanaAgrupado;
        private TabPage pestanaFallos;
        private TabPage pestanaDetalle;
        private DataGridView tablaAgrupada;
        private DataGridView tablaFallos;
        private DataGridView tablaDepositos;
        private Panel panelPaginas;
        private Button botonAnterior;
        private Label etiquetaPagina;
        private Button botonSiguiente;
        private Label etiquetaEstado;
    }
}
