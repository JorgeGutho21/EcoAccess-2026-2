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
            this.etiquetaExplicacion = new System.Windows.Forms.Label();
            this.panelCampos = new System.Windows.Forms.Panel();
            this.etiquetaCorreo = new System.Windows.Forms.Label();
            this.textoCorreo = new System.Windows.Forms.TextBox();
            this.etiquetaContrasena = new System.Windows.Forms.Label();
            this.textoContrasena = new System.Windows.Forms.TextBox();
            this.etiquetaConfirmacion = new System.Windows.Forms.Label();
            this.textoConfirmacion = new System.Windows.Forms.TextBox();
            this.botonAceptar = new System.Windows.Forms.Button();
            this.botonCancelar = new System.Windows.Forms.Button();
            this.etiquetaEstado = new System.Windows.Forms.Label();
            this.panelCampos.SuspendLayout();
            this.SuspendLayout();
            // 
            // etiquetaExplicacion
            // 
            this.etiquetaExplicacion.Location = new System.Drawing.Point(24, 13);
            this.etiquetaExplicacion.Name = "etiquetaExplicacion";
            this.etiquetaExplicacion.Size = new System.Drawing.Size(392, 48);
            this.etiquetaExplicacion.TabIndex = 0;
            // 
            // panelCampos
            // 
            this.panelCampos.Controls.Add(this.etiquetaCorreo);
            this.panelCampos.Controls.Add(this.textoCorreo);
            this.panelCampos.Controls.Add(this.etiquetaContrasena);
            this.panelCampos.Controls.Add(this.textoContrasena);
            this.panelCampos.Controls.Add(this.etiquetaConfirmacion);
            this.panelCampos.Controls.Add(this.textoConfirmacion);
            this.panelCampos.Controls.Add(this.botonAceptar);
            this.panelCampos.Controls.Add(this.botonCancelar);
            this.panelCampos.Location = new System.Drawing.Point(24, 67);
            this.panelCampos.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panelCampos.Name = "panelCampos";
            this.panelCampos.Size = new System.Drawing.Size(392, 187);
            this.panelCampos.TabIndex = 1;
            // 
            // etiquetaCorreo
            // 
            this.etiquetaCorreo.AutoSize = true;
            this.etiquetaCorreo.Location = new System.Drawing.Point(0, 3);
            this.etiquetaCorreo.Name = "etiquetaCorreo";
            this.etiquetaCorreo.Size = new System.Drawing.Size(158, 16);
            this.etiquetaCorreo.TabIndex = 0;
            this.etiquetaCorreo.Text = "Correo del administrador:";
            // 
            // textoCorreo
            // 
            this.textoCorreo.Location = new System.Drawing.Point(0, 22);
            this.textoCorreo.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textoCorreo.Name = "textoCorreo";
            this.textoCorreo.Size = new System.Drawing.Size(392, 22);
            this.textoCorreo.TabIndex = 0;
            // 
            // etiquetaContrasena
            // 
            this.etiquetaContrasena.AutoSize = true;
            this.etiquetaContrasena.Location = new System.Drawing.Point(0, 53);
            this.etiquetaContrasena.Name = "etiquetaContrasena";
            this.etiquetaContrasena.Size = new System.Drawing.Size(119, 16);
            this.etiquetaContrasena.TabIndex = 1;
            this.etiquetaContrasena.Text = "Contraseña nueva:";
            // 
            // textoContrasena
            // 
            this.textoContrasena.Location = new System.Drawing.Point(0, 72);
            this.textoContrasena.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textoContrasena.Name = "textoContrasena";
            this.textoContrasena.Size = new System.Drawing.Size(392, 22);
            this.textoContrasena.TabIndex = 1;
            this.textoContrasena.UseSystemPasswordChar = true;
            // 
            // etiquetaConfirmacion
            // 
            this.etiquetaConfirmacion.AutoSize = true;
            this.etiquetaConfirmacion.Location = new System.Drawing.Point(0, 102);
            this.etiquetaConfirmacion.Name = "etiquetaConfirmacion";
            this.etiquetaConfirmacion.Size = new System.Drawing.Size(134, 16);
            this.etiquetaConfirmacion.TabIndex = 2;
            this.etiquetaConfirmacion.Text = "Repita la contraseña:";
            // 
            // textoConfirmacion
            // 
            this.textoConfirmacion.Location = new System.Drawing.Point(0, 122);
            this.textoConfirmacion.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textoConfirmacion.Name = "textoConfirmacion";
            this.textoConfirmacion.Size = new System.Drawing.Size(392, 22);
            this.textoConfirmacion.TabIndex = 2;
            this.textoConfirmacion.UseSystemPasswordChar = true;
            // 
            // botonAceptar
            // 
            this.botonAceptar.Location = new System.Drawing.Point(0, 155);
            this.botonAceptar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.botonAceptar.Name = "botonAceptar";
            this.botonAceptar.Size = new System.Drawing.Size(190, 29);
            this.botonAceptar.TabIndex = 3;
            this.botonAceptar.Text = "Guardar";
            this.botonAceptar.UseVisualStyleBackColor = true;
            this.botonAceptar.Click += new System.EventHandler(this.botonAceptar_Click_1);
            // 
            // botonCancelar
            // 
            this.botonCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.botonCancelar.Location = new System.Drawing.Point(202, 155);
            this.botonCancelar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.botonCancelar.Name = "botonCancelar";
            this.botonCancelar.Size = new System.Drawing.Size(190, 29);
            this.botonCancelar.TabIndex = 4;
            this.botonCancelar.Text = "Cancelar";
            this.botonCancelar.UseVisualStyleBackColor = true;
            // 
            // etiquetaEstado
            // 
            this.etiquetaEstado.Location = new System.Drawing.Point(24, 261);
            this.etiquetaEstado.Name = "etiquetaEstado";
            this.etiquetaEstado.Size = new System.Drawing.Size(392, 38);
            this.etiquetaEstado.TabIndex = 2;
            // 
            // FormConfiguracionInicial
            // 
            this.AcceptButton = this.botonAceptar;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.botonCancelar;
            this.ClientSize = new System.Drawing.Size(663, 513);
            this.Controls.Add(this.etiquetaExplicacion);
            this.Controls.Add(this.panelCampos);
            this.Controls.Add(this.etiquetaEstado);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormConfiguracionInicial";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configuración inicial";
            this.panelCampos.ResumeLayout(false);
            this.panelCampos.PerformLayout();
            this.ResumeLayout(false);

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
