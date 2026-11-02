using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    partial class FormPuntos
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
            tablaPuntos = new DataGridView();
            panelAcciones = new Panel();
            botonActualizar = new Button();
            botonActivar = new Button();
            botonDesactivar = new Button();
            grupoRegistro = new GroupBox();
            etiquetaNombre = new Label();
            textoNombre = new TextBox();
            etiquetaReferencia = new Label();
            textoReferencia = new TextBox();
            botonRegistrar = new Button();
            etiquetaEstado = new Label();
            ((System.ComponentModel.ISupportInitialize)tablaPuntos).BeginInit();
            panelAcciones.SuspendLayout();
            grupoRegistro.SuspendLayout();
            SuspendLayout();
            //
            // tablaPuntos
            //
            tablaPuntos.AllowUserToAddRows = false;
            tablaPuntos.AllowUserToDeleteRows = false;
            tablaPuntos.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tablaPuntos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tablaPuntos.Location = new Point(12, 12);
            tablaPuntos.MultiSelect = false;
            tablaPuntos.Name = "tablaPuntos";
            tablaPuntos.ReadOnly = true;
            tablaPuntos.RowHeadersVisible = false;
            tablaPuntos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablaPuntos.Size = new Size(620, 420);
            tablaPuntos.TabIndex = 0;
            //
            // panelAcciones
            //
            panelAcciones.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            panelAcciones.Controls.Add(botonActualizar);
            panelAcciones.Controls.Add(botonActivar);
            panelAcciones.Controls.Add(botonDesactivar);
            panelAcciones.Controls.Add(grupoRegistro);
            panelAcciones.Location = new Point(644, 12);
            panelAcciones.Name = "panelAcciones";
            panelAcciones.Size = new Size(330, 420);
            panelAcciones.TabIndex = 1;
            //
            // botonActualizar
            //
            botonActualizar.Location = new Point(4, 4);
            botonActualizar.Name = "botonActualizar";
            botonActualizar.Size = new Size(320, 32);
            botonActualizar.TabIndex = 0;
            botonActualizar.Text = "Actualizar lista";
            botonActualizar.UseVisualStyleBackColor = true;
            botonActualizar.Click += BotonActualizar_Click;
            //
            // botonActivar
            //
            botonActivar.Location = new Point(4, 42);
            botonActivar.Name = "botonActivar";
            botonActivar.Size = new Size(156, 32);
            botonActivar.TabIndex = 1;
            botonActivar.Text = "Activar";
            botonActivar.UseVisualStyleBackColor = true;
            botonActivar.Click += BotonActivar_Click;
            //
            // botonDesactivar
            //
            botonDesactivar.Location = new Point(168, 42);
            botonDesactivar.Name = "botonDesactivar";
            botonDesactivar.Size = new Size(156, 32);
            botonDesactivar.TabIndex = 2;
            botonDesactivar.Text = "Desactivar";
            botonDesactivar.UseVisualStyleBackColor = true;
            botonDesactivar.Click += BotonDesactivar_Click;
            //
            // grupoRegistro
            //
            grupoRegistro.Controls.Add(etiquetaNombre);
            grupoRegistro.Controls.Add(textoNombre);
            grupoRegistro.Controls.Add(etiquetaReferencia);
            grupoRegistro.Controls.Add(textoReferencia);
            grupoRegistro.Controls.Add(botonRegistrar);
            grupoRegistro.Location = new Point(4, 88);
            grupoRegistro.Name = "grupoRegistro";
            grupoRegistro.Size = new Size(320, 250);
            grupoRegistro.TabIndex = 3;
            grupoRegistro.TabStop = false;
            grupoRegistro.Text = "Registrar punto";
            //
            // etiquetaNombre
            //
            etiquetaNombre.AutoSize = true;
            etiquetaNombre.Location = new Point(10, 28);
            etiquetaNombre.Name = "etiquetaNombre";
            etiquetaNombre.Text = "Nombre (ej. Bloque A, primer piso)";
            //
            // textoNombre
            //
            textoNombre.Location = new Point(10, 50);
            textoNombre.Name = "textoNombre";
            textoNombre.Size = new Size(300, 27);
            textoNombre.TabIndex = 0;
            //
            // etiquetaReferencia
            //
            etiquetaReferencia.AutoSize = true;
            etiquetaReferencia.Location = new Point(10, 84);
            etiquetaReferencia.Name = "etiquetaReferencia";
            etiquetaReferencia.Text = "Referencia de ubicación (opcional)";
            //
            // textoReferencia
            //
            textoReferencia.Location = new Point(10, 106);
            textoReferencia.Multiline = true;
            textoReferencia.Name = "textoReferencia";
            textoReferencia.Size = new Size(300, 80);
            textoReferencia.TabIndex = 1;
            //
            // botonRegistrar
            //
            botonRegistrar.Location = new Point(10, 196);
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
            etiquetaEstado.Location = new Point(12, 442);
            etiquetaEstado.Name = "etiquetaEstado";
            etiquetaEstado.Size = new Size(962, 48);
            etiquetaEstado.Text = "";
            //
            // FormPuntos
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(986, 500);
            Controls.Add(tablaPuntos);
            Controls.Add(panelAcciones);
            Controls.Add(etiquetaEstado);
            MinimumSize = new Size(900, 480);
            Name = "FormPuntos";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Puntos de instalación";
            Load += FormPuntos_Load;
            ((System.ComponentModel.ISupportInitialize)tablaPuntos).EndInit();
            panelAcciones.ResumeLayout(false);
            grupoRegistro.ResumeLayout(false);
            grupoRegistro.PerformLayout();
            ResumeLayout(false);
        }

    #endregion

        private DataGridView tablaPuntos;
        private Panel panelAcciones;
        private Button botonActualizar;
        private Button botonActivar;
        private Button botonDesactivar;
        private GroupBox grupoRegistro;
        private Label etiquetaNombre;
        private TextBox textoNombre;
        private Label etiquetaReferencia;
        private TextBox textoReferencia;
        private Button botonRegistrar;
        private Label etiquetaEstado;
    }
}
