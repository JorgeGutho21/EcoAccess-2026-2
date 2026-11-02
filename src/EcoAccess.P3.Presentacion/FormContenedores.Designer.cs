using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    partial class FormContenedores
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
            tablaCanecas = new DataGridView();
            panelAcciones = new Panel();
            botonActualizar = new Button();
            botonVerQr = new Button();
            botonEnServicio = new Button();
            botonFueraDeServicio = new Button();
            botonDarDeBaja = new Button();
            grupoMateriales = new GroupBox();
            listaAdmitidos = new ListBox();
            botonQuitar = new Button();
            comboMaterial = new ComboBox();
            botonAdmitir = new Button();
            grupoRegistro = new GroupBox();
            etiquetaPunto = new Label();
            comboPunto = new ComboBox();
            etiquetaNombre = new Label();
            textoNombre = new TextBox();
            botonRegistrar = new Button();
            etiquetaEstado = new Label();
            ((System.ComponentModel.ISupportInitialize)tablaCanecas).BeginInit();
            panelAcciones.SuspendLayout();
            grupoMateriales.SuspendLayout();
            grupoRegistro.SuspendLayout();
            SuspendLayout();
            //
            // tablaCanecas
            //
            tablaCanecas.AllowUserToAddRows = false;
            tablaCanecas.AllowUserToDeleteRows = false;
            tablaCanecas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tablaCanecas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tablaCanecas.Location = new Point(12, 12);
            tablaCanecas.MultiSelect = false;
            tablaCanecas.Name = "tablaCanecas";
            tablaCanecas.ReadOnly = true;
            tablaCanecas.RowHeadersVisible = false;
            tablaCanecas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablaCanecas.Size = new Size(720, 560);
            tablaCanecas.TabIndex = 0;
            tablaCanecas.SelectionChanged += TablaCanecas_SelectionChanged;
            //
            // panelAcciones
            //
            panelAcciones.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            panelAcciones.Controls.Add(botonActualizar);
            panelAcciones.Controls.Add(botonVerQr);
            panelAcciones.Controls.Add(botonEnServicio);
            panelAcciones.Controls.Add(botonFueraDeServicio);
            panelAcciones.Controls.Add(botonDarDeBaja);
            panelAcciones.Controls.Add(grupoMateriales);
            panelAcciones.Controls.Add(grupoRegistro);
            panelAcciones.Location = new Point(744, 12);
            panelAcciones.Name = "panelAcciones";
            panelAcciones.Size = new Size(330, 560);
            panelAcciones.TabIndex = 1;
            //
            // botonActualizar
            //
            botonActualizar.Location = new Point(4, 4);
            botonActualizar.Name = "botonActualizar";
            botonActualizar.Size = new Size(156, 32);
            botonActualizar.TabIndex = 0;
            botonActualizar.Text = "Actualizar lista";
            botonActualizar.UseVisualStyleBackColor = true;
            botonActualizar.Click += BotonActualizar_Click;
            //
            // botonVerQr
            //
            botonVerQr.Location = new Point(168, 4);
            botonVerQr.Name = "botonVerQr";
            botonVerQr.Size = new Size(156, 32);
            botonVerQr.TabIndex = 1;
            botonVerQr.Text = "Ver QR";
            botonVerQr.UseVisualStyleBackColor = true;
            botonVerQr.Click += BotonVerQr_Click;
            //
            // botonEnServicio
            //
            botonEnServicio.Location = new Point(4, 42);
            botonEnServicio.Name = "botonEnServicio";
            botonEnServicio.Size = new Size(156, 32);
            botonEnServicio.TabIndex = 2;
            botonEnServicio.Text = "Poner en servicio";
            botonEnServicio.UseVisualStyleBackColor = true;
            botonEnServicio.Click += BotonEnServicio_Click;
            //
            // botonFueraDeServicio
            //
            botonFueraDeServicio.Location = new Point(168, 42);
            botonFueraDeServicio.Name = "botonFueraDeServicio";
            botonFueraDeServicio.Size = new Size(156, 32);
            botonFueraDeServicio.TabIndex = 3;
            botonFueraDeServicio.Text = "Fuera de servicio";
            botonFueraDeServicio.UseVisualStyleBackColor = true;
            botonFueraDeServicio.Click += BotonFueraDeServicio_Click;
            //
            // botonDarDeBaja
            //
            botonDarDeBaja.Location = new Point(4, 80);
            botonDarDeBaja.Name = "botonDarDeBaja";
            botonDarDeBaja.Size = new Size(320, 32);
            botonDarDeBaja.TabIndex = 4;
            botonDarDeBaja.Text = "Dar de baja (definitivo)";
            botonDarDeBaja.UseVisualStyleBackColor = true;
            botonDarDeBaja.Click += BotonDarDeBaja_Click;
            //
            // grupoMateriales
            //
            grupoMateriales.Controls.Add(listaAdmitidos);
            grupoMateriales.Controls.Add(botonQuitar);
            grupoMateriales.Controls.Add(comboMaterial);
            grupoMateriales.Controls.Add(botonAdmitir);
            grupoMateriales.Location = new Point(4, 122);
            grupoMateriales.Name = "grupoMateriales";
            grupoMateriales.Size = new Size(320, 232);
            grupoMateriales.TabIndex = 5;
            grupoMateriales.TabStop = false;
            grupoMateriales.Text = "Materiales admitidos";
            //
            // listaAdmitidos
            //
            listaAdmitidos.Location = new Point(10, 26);
            listaAdmitidos.Name = "listaAdmitidos";
            listaAdmitidos.Size = new Size(300, 84);
            listaAdmitidos.TabIndex = 0;
            //
            // botonQuitar
            //
            botonQuitar.Location = new Point(10, 116);
            botonQuitar.Name = "botonQuitar";
            botonQuitar.Size = new Size(300, 32);
            botonQuitar.TabIndex = 1;
            botonQuitar.Text = "Quitar material seleccionado";
            botonQuitar.UseVisualStyleBackColor = true;
            botonQuitar.Click += BotonQuitar_Click;
            //
            // comboMaterial
            //
            comboMaterial.DropDownStyle = ComboBoxStyle.DropDownList;
            comboMaterial.Location = new Point(10, 156);
            comboMaterial.Name = "comboMaterial";
            comboMaterial.Size = new Size(300, 28);
            comboMaterial.TabIndex = 2;
            //
            // botonAdmitir
            //
            botonAdmitir.Location = new Point(10, 190);
            botonAdmitir.Name = "botonAdmitir";
            botonAdmitir.Size = new Size(300, 32);
            botonAdmitir.TabIndex = 3;
            botonAdmitir.Text = "Admitir material";
            botonAdmitir.UseVisualStyleBackColor = true;
            botonAdmitir.Click += BotonAdmitir_Click;
            //
            // grupoRegistro
            //
            grupoRegistro.Controls.Add(etiquetaPunto);
            grupoRegistro.Controls.Add(comboPunto);
            grupoRegistro.Controls.Add(etiquetaNombre);
            grupoRegistro.Controls.Add(textoNombre);
            grupoRegistro.Controls.Add(botonRegistrar);
            grupoRegistro.Location = new Point(4, 362);
            grupoRegistro.Name = "grupoRegistro";
            grupoRegistro.Size = new Size(320, 190);
            grupoRegistro.TabIndex = 6;
            grupoRegistro.TabStop = false;
            grupoRegistro.Text = "Registrar caneca";
            //
            // etiquetaPunto
            //
            etiquetaPunto.AutoSize = true;
            etiquetaPunto.Location = new Point(10, 26);
            etiquetaPunto.Name = "etiquetaPunto";
            etiquetaPunto.Text = "Punto de instalación (activo)";
            //
            // comboPunto
            //
            comboPunto.DropDownStyle = ComboBoxStyle.DropDownList;
            comboPunto.Location = new Point(10, 48);
            comboPunto.Name = "comboPunto";
            comboPunto.Size = new Size(300, 28);
            comboPunto.TabIndex = 0;
            //
            // etiquetaNombre
            //
            etiquetaNombre.AutoSize = true;
            etiquetaNombre.Location = new Point(10, 82);
            etiquetaNombre.Name = "etiquetaNombre";
            etiquetaNombre.Text = "Nombre (ej. Caneca entrada norte)";
            //
            // textoNombre
            //
            textoNombre.Location = new Point(10, 104);
            textoNombre.Name = "textoNombre";
            textoNombre.Size = new Size(300, 27);
            textoNombre.TabIndex = 1;
            //
            // botonRegistrar
            //
            botonRegistrar.Location = new Point(10, 142);
            botonRegistrar.Name = "botonRegistrar";
            botonRegistrar.Size = new Size(300, 34);
            botonRegistrar.TabIndex = 2;
            botonRegistrar.Text = "Registrar";
            botonRegistrar.UseVisualStyleBackColor = true;
            botonRegistrar.Click += BotonRegistrar_Click;
            //
            // etiquetaEstado
            //
            etiquetaEstado.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            etiquetaEstado.Location = new Point(12, 582);
            etiquetaEstado.Name = "etiquetaEstado";
            etiquetaEstado.Size = new Size(1062, 48);
            etiquetaEstado.Text = "";
            //
            // FormContenedores
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1086, 640);
            Controls.Add(tablaCanecas);
            Controls.Add(panelAcciones);
            Controls.Add(etiquetaEstado);
            MinimumSize = new Size(1000, 660);
            Name = "FormContenedores";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Canecas";
            Load += FormContenedores_Load;
            ((System.ComponentModel.ISupportInitialize)tablaCanecas).EndInit();
            panelAcciones.ResumeLayout(false);
            grupoMateriales.ResumeLayout(false);
            grupoRegistro.ResumeLayout(false);
            grupoRegistro.PerformLayout();
            ResumeLayout(false);
        }

    #endregion

        private DataGridView tablaCanecas;
        private Panel panelAcciones;
        private Button botonActualizar;
        private Button botonVerQr;
        private Button botonEnServicio;
        private Button botonFueraDeServicio;
        private Button botonDarDeBaja;
        private GroupBox grupoMateriales;
        private ListBox listaAdmitidos;
        private Button botonQuitar;
        private ComboBox comboMaterial;
        private Button botonAdmitir;
        private GroupBox grupoRegistro;
        private Label etiquetaPunto;
        private ComboBox comboPunto;
        private Label etiquetaNombre;
        private TextBox textoNombre;
        private Button botonRegistrar;
        private Label etiquetaEstado;
    }
}
