using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    partial class FormPrincipal
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
            etiquetaSesion = new Label();
            botonMateriales = new Button();
            botonPuntos = new Button();
            botonCanecas = new Button();
            botonUsuarios = new Button();
            botonEstadisticas = new Button();
            botonCambiarContrasena = new Button();
            botonCerrarSesion = new Button();
            SuspendLayout();
            //
            // etiquetaSesion
            //
            etiquetaSesion.AutoSize = true;
            etiquetaSesion.Location = new Point(24, 26);
            etiquetaSesion.Name = "etiquetaSesion";
            //
            // botonMateriales
            //
            botonMateriales.Location = new Point(24, 70);
            botonMateriales.Name = "botonMateriales";
            botonMateriales.Size = new Size(246, 36);
            botonMateriales.TabIndex = 0;
            botonMateriales.Text = "Catálogo de materiales";
            botonMateriales.UseVisualStyleBackColor = true;
            botonMateriales.Click += BotonMateriales_Click;
            //
            // botonPuntos
            //
            botonPuntos.Location = new Point(24, 114);
            botonPuntos.Name = "botonPuntos";
            botonPuntos.Size = new Size(246, 36);
            botonPuntos.TabIndex = 3;
            botonPuntos.Text = "Puntos de instalación";
            botonPuntos.UseVisualStyleBackColor = true;
            botonPuntos.Click += BotonPuntos_Click;
            //
            // botonCanecas
            //
            botonCanecas.Location = new Point(24, 158);
            botonCanecas.Name = "botonCanecas";
            botonCanecas.Size = new Size(246, 36);
            botonCanecas.TabIndex = 4;
            botonCanecas.Text = "Canecas";
            botonCanecas.UseVisualStyleBackColor = true;
            botonCanecas.Click += BotonCanecas_Click;
            //
            // botonUsuarios
            //
            botonUsuarios.Location = new Point(24, 202);
            botonUsuarios.Name = "botonUsuarios";
            botonUsuarios.Size = new Size(246, 36);
            botonUsuarios.TabIndex = 5;
            botonUsuarios.Text = "Usuarios y roles";
            botonUsuarios.UseVisualStyleBackColor = true;
            botonUsuarios.Click += BotonUsuarios_Click;
            //
            // botonEstadisticas
            //
            botonEstadisticas.Location = new Point(24, 246);
            botonEstadisticas.Name = "botonEstadisticas";
            botonEstadisticas.Size = new Size(246, 36);
            botonEstadisticas.TabIndex = 6;
            botonEstadisticas.Text = "Estadísticas";
            botonEstadisticas.UseVisualStyleBackColor = true;
            botonEstadisticas.Click += BotonEstadisticas_Click;
            //
            // botonCambiarContrasena
            //
            botonCambiarContrasena.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            botonCambiarContrasena.Location = new Point(620, 18);
            botonCambiarContrasena.Name = "botonCambiarContrasena";
            botonCambiarContrasena.Size = new Size(200, 36);
            botonCambiarContrasena.TabIndex = 1;
            botonCambiarContrasena.Text = "Cambiar contraseña";
            botonCambiarContrasena.UseVisualStyleBackColor = true;
            botonCambiarContrasena.Click += BotonCambiarContrasena_Click;
            //
            // botonCerrarSesion
            //
            botonCerrarSesion.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            botonCerrarSesion.Location = new Point(832, 18);
            botonCerrarSesion.Name = "botonCerrarSesion";
            botonCerrarSesion.Size = new Size(168, 36);
            botonCerrarSesion.TabIndex = 2;
            botonCerrarSesion.Text = "Cerrar sesión";
            botonCerrarSesion.UseVisualStyleBackColor = true;
            botonCerrarSesion.Click += BotonCerrarSesion_Click;
            //
            // FormPrincipal
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1024, 640);
            Controls.Add(etiquetaSesion);
            Controls.Add(botonMateriales);
            Controls.Add(botonPuntos);
            Controls.Add(botonCanecas);
            Controls.Add(botonUsuarios);
            Controls.Add(botonEstadisticas);
            Controls.Add(botonCambiarContrasena);
            Controls.Add(botonCerrarSesion);
            Name = "FormPrincipal";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "EcoAccess P3";
            ResumeLayout(false);
            PerformLayout();
        }

    #endregion

        private Label etiquetaSesion;
        private Button botonMateriales;
        private Button botonPuntos;
        private Button botonCanecas;
        private Button botonUsuarios;
        private Button botonEstadisticas;
        private Button botonCambiarContrasena;
        private Button botonCerrarSesion;
    }
}
