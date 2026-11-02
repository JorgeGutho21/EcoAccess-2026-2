using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    partial class FormQrCaneca
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
            imagenQr = new PictureBox();
            etiquetaCaneca = new Label();
            textoEnlace = new TextBox();
            botonGuardar = new Button();
            botonImprimir = new Button();
            botonCerrar = new Button();
            etiquetaEstado = new Label();
            ((System.ComponentModel.ISupportInitialize)imagenQr).BeginInit();
            SuspendLayout();
            //
            // imagenQr
            //
            imagenQr.BackColor = Color.White;
            imagenQr.Location = new Point(12, 12);
            imagenQr.Name = "imagenQr";
            imagenQr.Size = new Size(456, 456);
            imagenQr.SizeMode = PictureBoxSizeMode.Zoom;
            imagenQr.TabStop = false;
            //
            // etiquetaCaneca
            //
            etiquetaCaneca.Location = new Point(12, 476);
            etiquetaCaneca.Name = "etiquetaCaneca";
            etiquetaCaneca.Size = new Size(456, 28);
            etiquetaCaneca.TextAlign = ContentAlignment.MiddleCenter;
            //
            // textoEnlace
            //
            textoEnlace.Location = new Point(12, 510);
            textoEnlace.Name = "textoEnlace";
            textoEnlace.ReadOnly = true;
            textoEnlace.Size = new Size(456, 27);
            textoEnlace.TabIndex = 0;
            //
            // botonGuardar
            //
            botonGuardar.Location = new Point(12, 548);
            botonGuardar.Name = "botonGuardar";
            botonGuardar.Size = new Size(148, 32);
            botonGuardar.TabIndex = 1;
            botonGuardar.Text = "Guardar PNG";
            botonGuardar.UseVisualStyleBackColor = true;
            botonGuardar.Click += BotonGuardar_Click;
            //
            // botonImprimir
            //
            botonImprimir.Location = new Point(166, 548);
            botonImprimir.Name = "botonImprimir";
            botonImprimir.Size = new Size(148, 32);
            botonImprimir.TabIndex = 2;
            botonImprimir.Text = "Imprimir";
            botonImprimir.UseVisualStyleBackColor = true;
            botonImprimir.Click += BotonImprimir_Click;
            //
            // botonCerrar
            //
            botonCerrar.DialogResult = DialogResult.Cancel;
            botonCerrar.Location = new Point(320, 548);
            botonCerrar.Name = "botonCerrar";
            botonCerrar.Size = new Size(148, 32);
            botonCerrar.TabIndex = 3;
            botonCerrar.Text = "Cerrar";
            botonCerrar.UseVisualStyleBackColor = true;
            //
            // etiquetaEstado
            //
            etiquetaEstado.Location = new Point(12, 586);
            etiquetaEstado.Name = "etiquetaEstado";
            etiquetaEstado.Size = new Size(456, 24);
            etiquetaEstado.Text = "";
            //
            // FormQrCaneca
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = botonCerrar;
            ClientSize = new Size(480, 620);
            Controls.Add(imagenQr);
            Controls.Add(etiquetaCaneca);
            Controls.Add(textoEnlace);
            Controls.Add(botonGuardar);
            Controls.Add(botonImprimir);
            Controls.Add(botonCerrar);
            Controls.Add(etiquetaEstado);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormQrCaneca";
            StartPosition = FormStartPosition.CenterParent;
            Text = "QR de la caneca";
            ((System.ComponentModel.ISupportInitialize)imagenQr).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

    #endregion

        private PictureBox imagenQr;
        private Label etiquetaCaneca;
        private TextBox textoEnlace;
        private Button botonGuardar;
        private Button botonImprimir;
        private Button botonCerrar;
        private Label etiquetaEstado;
    }
}
