using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    partial class FormInicioSesion
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
            etiquetaTitulo = new Label();
            panelCampos = new Panel();
            etiquetaCorreo = new Label();
            textoCorreo = new TextBox();
            etiquetaContrasena = new Label();
            textoContrasena = new TextBox();
            botonEntrar = new Button();
            enlaceConfiguracionInicial = new LinkLabel();
            etiquetaEstado = new Label();
            panelCampos.SuspendLayout();
            SuspendLayout();
            //
            // etiquetaTitulo
            //
            etiquetaTitulo.AutoSize = true;
            etiquetaTitulo.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            etiquetaTitulo.Location = new Point(24, 20);
            etiquetaTitulo.Name = "etiquetaTitulo";
            etiquetaTitulo.Text = "Iniciar sesión en EcoAccess P3";
            //
            // panelCampos
            //
            panelCampos.Controls.Add(etiquetaCorreo);
            panelCampos.Controls.Add(textoCorreo);
            panelCampos.Controls.Add(etiquetaContrasena);
            panelCampos.Controls.Add(textoContrasena);
            panelCampos.Controls.Add(botonEntrar);
            panelCampos.Controls.Add(enlaceConfiguracionInicial);
            panelCampos.Location = new Point(24, 70);
            panelCampos.Name = "panelCampos";
            panelCampos.Size = new Size(392, 200);
            //
            // etiquetaCorreo
            //
            etiquetaCorreo.AutoSize = true;
            etiquetaCorreo.Location = new Point(0, 4);
            etiquetaCorreo.Name = "etiquetaCorreo";
            etiquetaCorreo.Text = "Correo electrónico:";
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
            etiquetaContrasena.Text = "Contraseña:";
            //
            // textoContrasena
            //
            textoContrasena.Location = new Point(0, 90);
            textoContrasena.Name = "textoContrasena";
            textoContrasena.Size = new Size(392, 27);
            textoContrasena.TabIndex = 1;
            textoContrasena.UseSystemPasswordChar = true;
            //
            // botonEntrar
            //
            botonEntrar.Location = new Point(0, 132);
            botonEntrar.Name = "botonEntrar";
            botonEntrar.Size = new Size(392, 36);
            botonEntrar.TabIndex = 2;
            botonEntrar.Text = "Entrar";
            botonEntrar.UseVisualStyleBackColor = true;
            botonEntrar.Click += BotonEntrar_Click;
            //
            // enlaceConfiguracionInicial
            //
            enlaceConfiguracionInicial.AutoSize = true;
            enlaceConfiguracionInicial.Location = new Point(0, 176);
            enlaceConfiguracionInicial.Name = "enlaceConfiguracionInicial";
            enlaceConfiguracionInicial.TabIndex = 3;
            enlaceConfiguracionInicial.Text = "Configuración inicial: contraseña del primer administrador";
            enlaceConfiguracionInicial.Visible = false;
            enlaceConfiguracionInicial.LinkClicked += EnlaceConfiguracionInicial_LinkClicked;
            //
            // etiquetaEstado
            //
            etiquetaEstado.Location = new Point(24, 280);
            etiquetaEstado.Name = "etiquetaEstado";
            etiquetaEstado.Size = new Size(392, 48);
            //
            // FormInicioSesion
            //
            AcceptButton = botonEntrar;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(440, 340);
            Controls.Add(etiquetaTitulo);
            Controls.Add(panelCampos);
            Controls.Add(etiquetaEstado);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormInicioSesion";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "EcoAccess P3 — Inicio de sesión";
            Load += FormInicioSesion_Load;
            panelCampos.ResumeLayout(false);
            panelCampos.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

    #endregion

        private Label etiquetaTitulo;
        private Panel panelCampos;
        private Label etiquetaCorreo;
        private TextBox textoCorreo;
        private Label etiquetaContrasena;
        private TextBox textoContrasena;
        private Button botonEntrar;
        private LinkLabel enlaceConfiguracionInicial;
        private Label etiquetaEstado;
    }
}
