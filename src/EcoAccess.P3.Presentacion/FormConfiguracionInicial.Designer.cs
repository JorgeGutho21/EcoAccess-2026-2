using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    partial class FormConfiguracionInicial
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
            etiquetaExplicacion = new Label();
            panelCampos = new Panel();
            etiquetaCorreo = new Label();
            textoCorreo = new TextBox();
            etiquetaContrasena = new Label();
            textoContrasena = new TextBox();
            etiquetaConfirmacion = new Label();
            textoConfirmacion = new TextBox();
            botonAceptar = new Button();
            botonCancelar = new Button();
            etiquetaEstado = new Label();
            panelCampos.SuspendLayout();
            SuspendLayout();
            //
            // etiquetaExplicacion
            //
            etiquetaExplicacion.Location = new Point(24, 16);
            etiquetaExplicacion.Name = "etiquetaExplicacion";
            etiquetaExplicacion.Size = new Size(392, 60);
            //
            // panelCampos
            //
            panelCampos.Controls.Add(etiquetaCorreo);
            panelCampos.Controls.Add(textoCorreo);
            panelCampos.Controls.Add(etiquetaContrasena);
            panelCampos.Controls.Add(textoContrasena);
            panelCampos.Controls.Add(etiquetaConfirmacion);
            panelCampos.Controls.Add(textoConfirmacion);
            panelCampos.Controls.Add(botonAceptar);
            panelCampos.Controls.Add(botonCancelar);
            panelCampos.Location = new Point(24, 84);
            panelCampos.Name = "panelCampos";
            panelCampos.Size = new Size(392, 234);
            //
            // etiquetaCorreo
            //
            etiquetaCorreo.AutoSize = true;
            etiquetaCorreo.Location = new Point(0, 4);
            etiquetaCorreo.Name = "etiquetaCorreo";
            etiquetaCorreo.Text = "Correo del administrador:";
            //
            // textoCorreo
            //
            textoCorreo.Location = new Point(0, 28);
            textoCorreo.Name = "textoCorreo";
            textoCorreo.Size = new Size(392, 27);
            textoCorreo.TabIndex = 0;
            //
            // etiquetaContrasena
            //
            etiquetaContrasena.AutoSize = true;
            etiquetaContrasena.Location = new Point(0, 66);
            etiquetaContrasena.Name = "etiquetaContrasena";
            etiquetaContrasena.Text = "Contraseña nueva:";
            //
            // textoContrasena
            //
            textoContrasena.Location = new Point(0, 90);
            textoContrasena.Name = "textoContrasena";
            textoContrasena.Size = new Size(392, 27);
            textoContrasena.TabIndex = 1;
            textoContrasena.UseSystemPasswordChar = true;
            //
            // etiquetaConfirmacion
            //
            etiquetaConfirmacion.AutoSize = true;
            etiquetaConfirmacion.Location = new Point(0, 128);
            etiquetaConfirmacion.Name = "etiquetaConfirmacion";
            etiquetaConfirmacion.Text = "Repita la contraseña:";
            //
            // textoConfirmacion
            //
            textoConfirmacion.Location = new Point(0, 152);
            textoConfirmacion.Name = "textoConfirmacion";
            textoConfirmacion.Size = new Size(392, 27);
            textoConfirmacion.TabIndex = 2;
            textoConfirmacion.UseSystemPasswordChar = true;
            //
            // botonAceptar
            //
            botonAceptar.Location = new Point(0, 194);
            botonAceptar.Name = "botonAceptar";
            botonAceptar.Size = new Size(190, 36);
            botonAceptar.TabIndex = 3;
            botonAceptar.Text = "Guardar";
            botonAceptar.UseVisualStyleBackColor = true;
            botonAceptar.Click += BotonAceptar_Click;
            //
            // botonCancelar
            //
            botonCancelar.DialogResult = DialogResult.Cancel;
            botonCancelar.Location = new Point(202, 194);
            botonCancelar.Name = "botonCancelar";
            botonCancelar.Size = new Size(190, 36);
            botonCancelar.TabIndex = 4;
            botonCancelar.Text = "Cancelar";
            botonCancelar.UseVisualStyleBackColor = true;
            //
            // etiquetaEstado
            //
            etiquetaEstado.Location = new Point(24, 326);
            etiquetaEstado.Name = "etiquetaEstado";
            etiquetaEstado.Size = new Size(392, 48);
            //
            // FormConfiguracionInicial
            //
            AcceptButton = botonAceptar;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = botonCancelar;
            ClientSize = new Size(440, 382);
            Controls.Add(etiquetaExplicacion);
            Controls.Add(panelCampos);
            Controls.Add(etiquetaEstado);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormConfiguracionInicial";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Configuración inicial";
            panelCampos.ResumeLayout(false);
            panelCampos.PerformLayout();
            ResumeLayout(false);
        }

    #endregion

        private Label etiquetaExplicacion;
        private Panel panelCampos;
        private Label etiquetaCorreo;
        private TextBox textoCorreo;
        private Label etiquetaContrasena;
        private TextBox textoContrasena;
        private Label etiquetaConfirmacion;
        private TextBox textoConfirmacion;
        private Button botonAceptar;
        private Button botonCancelar;
        private Label etiquetaEstado;
    }
}
