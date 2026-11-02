using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    partial class FormMateriales
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
            tablaMateriales = new DataGridView();
            panelAcciones = new Panel();
            checkSoloHabilitados = new CheckBox();
            botonActualizar = new Button();
            botonHabilitar = new Button();
            botonDeshabilitar = new Button();
            grupoRegistro = new GroupBox();
            etiquetaCodigo = new Label();
            textoCodigo = new TextBox();
            etiquetaNombre = new Label();
            textoNombre = new TextBox();
            etiquetaCategoria = new Label();
            comboCategoria = new ComboBox();
            etiquetaDescripcion = new Label();
            textoDescripcion = new TextBox();
            botonRegistrar = new Button();
            etiquetaEstado = new Label();
            ((System.ComponentModel.ISupportInitialize)tablaMateriales).BeginInit();
            panelAcciones.SuspendLayout();
            grupoRegistro.SuspendLayout();
            SuspendLayout();
            //
            // tablaMateriales
            //
            tablaMateriales.AllowUserToAddRows = false;
            tablaMateriales.AllowUserToDeleteRows = false;
            tablaMateriales.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tablaMateriales.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tablaMateriales.Location = new Point(12, 12);
            tablaMateriales.MultiSelect = false;
            tablaMateriales.Name = "tablaMateriales";
            tablaMateriales.ReadOnly = true;
            tablaMateriales.RowHeadersVisible = false;
            tablaMateriales.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablaMateriales.Size = new Size(620, 470);
            tablaMateriales.TabIndex = 0;
            //
            // panelAcciones
            //
            panelAcciones.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            panelAcciones.Controls.Add(checkSoloHabilitados);
            panelAcciones.Controls.Add(botonActualizar);
            panelAcciones.Controls.Add(botonHabilitar);
            panelAcciones.Controls.Add(botonDeshabilitar);
            panelAcciones.Controls.Add(grupoRegistro);
            panelAcciones.Location = new Point(644, 12);
            panelAcciones.Name = "panelAcciones";
            panelAcciones.Size = new Size(330, 470);
            panelAcciones.TabIndex = 1;
            //
            // checkSoloHabilitados
            //
            checkSoloHabilitados.AutoSize = true;
            checkSoloHabilitados.Location = new Point(4, 4);
            checkSoloHabilitados.Name = "checkSoloHabilitados";
            checkSoloHabilitados.TabIndex = 0;
            checkSoloHabilitados.Text = "Solo habilitados";
            checkSoloHabilitados.UseVisualStyleBackColor = true;
            checkSoloHabilitados.CheckedChanged += CheckSoloHabilitados_CheckedChanged;
            //
            // botonActualizar
            //
            botonActualizar.Location = new Point(4, 36);
            botonActualizar.Name = "botonActualizar";
            botonActualizar.Size = new Size(320, 32);
            botonActualizar.TabIndex = 1;
            botonActualizar.Text = "Actualizar lista";
            botonActualizar.UseVisualStyleBackColor = true;
            botonActualizar.Click += BotonActualizar_Click;
            //
            // botonHabilitar
            //
            botonHabilitar.Location = new Point(4, 74);
            botonHabilitar.Name = "botonHabilitar";
            botonHabilitar.Size = new Size(156, 32);
            botonHabilitar.TabIndex = 2;
            botonHabilitar.Text = "Habilitar";
            botonHabilitar.UseVisualStyleBackColor = true;
            botonHabilitar.Click += BotonHabilitar_Click;
            //
            // botonDeshabilitar
            //
            botonDeshabilitar.Location = new Point(168, 74);
            botonDeshabilitar.Name = "botonDeshabilitar";
            botonDeshabilitar.Size = new Size(156, 32);
            botonDeshabilitar.TabIndex = 3;
            botonDeshabilitar.Text = "Deshabilitar";
            botonDeshabilitar.UseVisualStyleBackColor = true;
            botonDeshabilitar.Click += BotonDeshabilitar_Click;
            //
            // grupoRegistro
            //
            grupoRegistro.Controls.Add(etiquetaCodigo);
            grupoRegistro.Controls.Add(textoCodigo);
            grupoRegistro.Controls.Add(etiquetaNombre);
            grupoRegistro.Controls.Add(textoNombre);
            grupoRegistro.Controls.Add(etiquetaCategoria);
            grupoRegistro.Controls.Add(comboCategoria);
            grupoRegistro.Controls.Add(etiquetaDescripcion);
            grupoRegistro.Controls.Add(textoDescripcion);
            grupoRegistro.Controls.Add(botonRegistrar);
            grupoRegistro.Location = new Point(4, 120);
            grupoRegistro.Name = "grupoRegistro";
            grupoRegistro.Size = new Size(320, 340);
            grupoRegistro.TabIndex = 4;
            grupoRegistro.TabStop = false;
            grupoRegistro.Text = "Registrar material";
            //
            // etiquetaCodigo
            //
            etiquetaCodigo.AutoSize = true;
            etiquetaCodigo.Location = new Point(10, 28);
            etiquetaCodigo.Name = "etiquetaCodigo";
            etiquetaCodigo.Text = "Código (ej. PET)";
            //
            // textoCodigo
            //
            textoCodigo.CharacterCasing = CharacterCasing.Upper;
            textoCodigo.Location = new Point(10, 50);
            textoCodigo.Name = "textoCodigo";
            textoCodigo.Size = new Size(300, 27);
            textoCodigo.TabIndex = 0;
            //
            // etiquetaNombre
            //
            etiquetaNombre.AutoSize = true;
            etiquetaNombre.Location = new Point(10, 84);
            etiquetaNombre.Name = "etiquetaNombre";
            etiquetaNombre.Text = "Nombre";
            //
            // textoNombre
            //
            textoNombre.Location = new Point(10, 106);
            textoNombre.Name = "textoNombre";
            textoNombre.Size = new Size(300, 27);
            textoNombre.TabIndex = 1;
            //
            // etiquetaCategoria
            //
            etiquetaCategoria.AutoSize = true;
            etiquetaCategoria.Location = new Point(10, 140);
            etiquetaCategoria.Name = "etiquetaCategoria";
            etiquetaCategoria.Text = "Categoría";
            //
            // comboCategoria
            //
            comboCategoria.DropDownStyle = ComboBoxStyle.DropDownList;
            comboCategoria.Location = new Point(10, 162);
            comboCategoria.Name = "comboCategoria";
            comboCategoria.Size = new Size(300, 28);
            comboCategoria.TabIndex = 2;
            //
            // etiquetaDescripcion
            //
            etiquetaDescripcion.AutoSize = true;
            etiquetaDescripcion.Location = new Point(10, 198);
            etiquetaDescripcion.Name = "etiquetaDescripcion";
            etiquetaDescripcion.Text = "Descripción (opcional)";
            //
            // textoDescripcion
            //
            textoDescripcion.Location = new Point(10, 220);
            textoDescripcion.Multiline = true;
            textoDescripcion.Name = "textoDescripcion";
            textoDescripcion.Size = new Size(300, 66);
            textoDescripcion.TabIndex = 3;
            //
            // botonRegistrar
            //
            botonRegistrar.Location = new Point(10, 294);
            botonRegistrar.Name = "botonRegistrar";
            botonRegistrar.Size = new Size(300, 34);
            botonRegistrar.TabIndex = 4;
            botonRegistrar.Text = "Registrar";
            botonRegistrar.UseVisualStyleBackColor = true;
            botonRegistrar.Click += BotonRegistrar_Click;
            //
            // etiquetaEstado
            //
            etiquetaEstado.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            etiquetaEstado.Location = new Point(12, 492);
            etiquetaEstado.Name = "etiquetaEstado";
            etiquetaEstado.Size = new Size(962, 48);
            etiquetaEstado.Text = "";
            //
            // FormMateriales
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(986, 550);
            Controls.Add(tablaMateriales);
            Controls.Add(panelAcciones);
            Controls.Add(etiquetaEstado);
            MinimumSize = new Size(900, 520);
            Name = "FormMateriales";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Catálogo de materiales";
            Load += FormMateriales_Load;
            ((System.ComponentModel.ISupportInitialize)tablaMateriales).EndInit();
            panelAcciones.ResumeLayout(false);
            panelAcciones.PerformLayout();
            grupoRegistro.ResumeLayout(false);
            grupoRegistro.PerformLayout();
            ResumeLayout(false);
        }

    #endregion

        private DataGridView tablaMateriales;
        private Panel panelAcciones;
        private CheckBox checkSoloHabilitados;
        private Button botonActualizar;
        private Button botonHabilitar;
        private Button botonDeshabilitar;
        private GroupBox grupoRegistro;
        private Label etiquetaCodigo;
        private TextBox textoCodigo;
        private Label etiquetaNombre;
        private TextBox textoNombre;
        private Label etiquetaCategoria;
        private ComboBox comboCategoria;
        private Label etiquetaDescripcion;
        private TextBox textoDescripcion;
        private Button botonRegistrar;
        private Label etiquetaEstado;
    }
}
