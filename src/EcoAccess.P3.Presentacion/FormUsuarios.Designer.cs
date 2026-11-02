using System.Drawing;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    partial class FormUsuarios
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
            tablaUsuarios = new DataGridView();
            panelAcciones = new Panel();
            botonActualizar = new Button();
            botonActivar = new Button();
            botonDesactivar = new Button();
            grupoRoles = new GroupBox();
            comboRol = new ComboBox();
            botonAsignarRol = new Button();
            botonQuitarRol = new Button();
            grupoContrasena = new GroupBox();
            etiquetaContrasena = new Label();
            textoContrasena = new TextBox();
            etiquetaConfirmacion = new Label();
            textoConfirmacion = new TextBox();
            botonEstablecerContrasena = new Button();
            grupoRegistro = new GroupBox();
            etiquetaNombre = new Label();
            textoNombre = new TextBox();
            etiquetaCorreo = new Label();
            textoCorreo = new TextBox();
            etiquetaRolInicial = new Label();
            comboRolInicial = new ComboBox();
            botonRegistrar = new Button();
            etiquetaEstado = new Label();
            ((System.ComponentModel.ISupportInitialize)tablaUsuarios).BeginInit();
            panelAcciones.SuspendLayout();
            grupoRoles.SuspendLayout();
            grupoContrasena.SuspendLayout();
            grupoRegistro.SuspendLayout();
            SuspendLayout();
            //
            // tablaUsuarios
            //
            tablaUsuarios.AllowUserToAddRows = false;
            tablaUsuarios.AllowUserToDeleteRows = false;
            tablaUsuarios.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tablaUsuarios.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tablaUsuarios.Location = new Point(12, 12);
            tablaUsuarios.MultiSelect = false;
            tablaUsuarios.Name = "tablaUsuarios";
            tablaUsuarios.ReadOnly = true;
            tablaUsuarios.RowHeadersVisible = false;
            tablaUsuarios.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablaUsuarios.Size = new Size(640, 640);
            tablaUsuarios.TabIndex = 0;
            //
            // panelAcciones
            //
            panelAcciones.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            // Con la ventana baja, el panel se desplaza en vertical en lugar de recortar los grupos.
            panelAcciones.AutoScroll = true;
            panelAcciones.Controls.Add(botonActualizar);
            panelAcciones.Controls.Add(botonActivar);
            panelAcciones.Controls.Add(botonDesactivar);
            panelAcciones.Controls.Add(grupoRoles);
            panelAcciones.Controls.Add(grupoContrasena);
            panelAcciones.Controls.Add(grupoRegistro);
            panelAcciones.Location = new Point(664, 12);
            panelAcciones.Name = "panelAcciones";
            panelAcciones.Size = new Size(350, 640);
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
            // grupoRoles
            //
            grupoRoles.Controls.Add(comboRol);
            grupoRoles.Controls.Add(botonAsignarRol);
            grupoRoles.Controls.Add(botonQuitarRol);
            grupoRoles.Location = new Point(4, 84);
            grupoRoles.Name = "grupoRoles";
            grupoRoles.Size = new Size(320, 108);
            grupoRoles.TabIndex = 3;
            grupoRoles.TabStop = false;
            grupoRoles.Text = "Roles del usuario seleccionado";
            //
            // comboRol
            //
            comboRol.DropDownStyle = ComboBoxStyle.DropDownList;
            comboRol.Location = new Point(10, 28);
            comboRol.Name = "comboRol";
            comboRol.Size = new Size(300, 28);
            comboRol.TabIndex = 0;
            //
            // botonAsignarRol
            //
            botonAsignarRol.Location = new Point(10, 64);
            botonAsignarRol.Name = "botonAsignarRol";
            botonAsignarRol.Size = new Size(146, 32);
            botonAsignarRol.TabIndex = 1;
            botonAsignarRol.Text = "Asignar rol";
            botonAsignarRol.UseVisualStyleBackColor = true;
            botonAsignarRol.Click += BotonAsignarRol_Click;
            //
            // botonQuitarRol
            //
            botonQuitarRol.Location = new Point(164, 64);
            botonQuitarRol.Name = "botonQuitarRol";
            botonQuitarRol.Size = new Size(146, 32);
            botonQuitarRol.TabIndex = 2;
            botonQuitarRol.Text = "Quitar rol";
            botonQuitarRol.UseVisualStyleBackColor = true;
            botonQuitarRol.Click += BotonQuitarRol_Click;
            //
            // grupoContrasena
            //
            grupoContrasena.Controls.Add(etiquetaContrasena);
            grupoContrasena.Controls.Add(textoContrasena);
            grupoContrasena.Controls.Add(etiquetaConfirmacion);
            grupoContrasena.Controls.Add(textoConfirmacion);
            grupoContrasena.Controls.Add(botonEstablecerContrasena);
            grupoContrasena.Location = new Point(4, 200);
            grupoContrasena.Name = "grupoContrasena";
            grupoContrasena.Size = new Size(320, 186);
            grupoContrasena.TabIndex = 4;
            grupoContrasena.TabStop = false;
            grupoContrasena.Text = "Contraseña inicial (operario o administrador)";
            //
            // etiquetaContrasena
            //
            etiquetaContrasena.AutoSize = true;
            etiquetaContrasena.Location = new Point(10, 28);
            etiquetaContrasena.Name = "etiquetaContrasena";
            etiquetaContrasena.Text = "Contraseña";
            //
            // textoContrasena
            //
            textoContrasena.Location = new Point(10, 50);
            textoContrasena.Name = "textoContrasena";
            textoContrasena.Size = new Size(300, 27);
            textoContrasena.TabIndex = 0;
            textoContrasena.UseSystemPasswordChar = true;
            //
            // etiquetaConfirmacion
            //
            etiquetaConfirmacion.AutoSize = true;
            etiquetaConfirmacion.Location = new Point(10, 84);
            etiquetaConfirmacion.Name = "etiquetaConfirmacion";
            etiquetaConfirmacion.Text = "Confirmación";
            //
            // textoConfirmacion
            //
            textoConfirmacion.Location = new Point(10, 106);
            textoConfirmacion.Name = "textoConfirmacion";
            textoConfirmacion.Size = new Size(300, 27);
            textoConfirmacion.TabIndex = 1;
            textoConfirmacion.UseSystemPasswordChar = true;
            //
            // botonEstablecerContrasena
            //
            botonEstablecerContrasena.Location = new Point(10, 142);
            botonEstablecerContrasena.Name = "botonEstablecerContrasena";
            botonEstablecerContrasena.Size = new Size(300, 34);
            botonEstablecerContrasena.TabIndex = 2;
            botonEstablecerContrasena.Text = "Establecer contraseña inicial";
            botonEstablecerContrasena.UseVisualStyleBackColor = true;
            botonEstablecerContrasena.Click += BotonEstablecerContrasena_Click;
            //
            // grupoRegistro
            //
            grupoRegistro.Controls.Add(etiquetaNombre);
            grupoRegistro.Controls.Add(textoNombre);
            grupoRegistro.Controls.Add(etiquetaCorreo);
            grupoRegistro.Controls.Add(textoCorreo);
            grupoRegistro.Controls.Add(etiquetaRolInicial);
            grupoRegistro.Controls.Add(comboRolInicial);
            grupoRegistro.Controls.Add(botonRegistrar);
            grupoRegistro.Location = new Point(4, 394);
            grupoRegistro.Name = "grupoRegistro";
            grupoRegistro.Size = new Size(320, 246);
            grupoRegistro.TabIndex = 5;
            grupoRegistro.TabStop = false;
            grupoRegistro.Text = "Registrar usuario";
            //
            // etiquetaNombre
            //
            etiquetaNombre.AutoSize = true;
            etiquetaNombre.Location = new Point(10, 28);
            etiquetaNombre.Name = "etiquetaNombre";
            etiquetaNombre.Text = "Nombre completo";
            //
            // textoNombre
            //
            textoNombre.Location = new Point(10, 50);
            textoNombre.Name = "textoNombre";
            textoNombre.Size = new Size(300, 27);
            textoNombre.TabIndex = 0;
            //
            // etiquetaCorreo
            //
            etiquetaCorreo.AutoSize = true;
            etiquetaCorreo.Location = new Point(10, 84);
            etiquetaCorreo.Name = "etiquetaCorreo";
            etiquetaCorreo.Text = "Correo electrónico";
            //
            // textoCorreo
            //
            textoCorreo.Location = new Point(10, 106);
            textoCorreo.Name = "textoCorreo";
            textoCorreo.Size = new Size(300, 27);
            textoCorreo.TabIndex = 1;
            //
            // etiquetaRolInicial
            //
            etiquetaRolInicial.AutoSize = true;
            etiquetaRolInicial.Location = new Point(10, 140);
            etiquetaRolInicial.Name = "etiquetaRolInicial";
            etiquetaRolInicial.Text = "Rol inicial";
            //
            // comboRolInicial
            //
            comboRolInicial.DropDownStyle = ComboBoxStyle.DropDownList;
            comboRolInicial.Location = new Point(10, 162);
            comboRolInicial.Name = "comboRolInicial";
            comboRolInicial.Size = new Size(300, 28);
            comboRolInicial.TabIndex = 2;
            //
            // botonRegistrar
            //
            botonRegistrar.Location = new Point(10, 200);
            botonRegistrar.Name = "botonRegistrar";
            botonRegistrar.Size = new Size(300, 34);
            botonRegistrar.TabIndex = 3;
            botonRegistrar.Text = "Registrar";
            botonRegistrar.UseVisualStyleBackColor = true;
            botonRegistrar.Click += BotonRegistrar_Click;
            //
            // etiquetaEstado
            //
            etiquetaEstado.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            etiquetaEstado.Location = new Point(12, 662);
            etiquetaEstado.Name = "etiquetaEstado";
            etiquetaEstado.Size = new Size(1002, 48);
            etiquetaEstado.Text = "";
            //
            // FormUsuarios
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1026, 720);
            Controls.Add(tablaUsuarios);
            Controls.Add(panelAcciones);
            Controls.Add(etiquetaEstado);
            MinimumSize = new Size(940, 480);
            Name = "FormUsuarios";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Usuarios y roles";
            Load += FormUsuarios_Load;
            ((System.ComponentModel.ISupportInitialize)tablaUsuarios).EndInit();
            panelAcciones.ResumeLayout(false);
            grupoRoles.ResumeLayout(false);
            grupoRoles.PerformLayout();
            grupoContrasena.ResumeLayout(false);
            grupoContrasena.PerformLayout();
            grupoRegistro.ResumeLayout(false);
            grupoRegistro.PerformLayout();
            ResumeLayout(false);
        }

    #endregion

        private DataGridView tablaUsuarios;
        private Panel panelAcciones;
        private Button botonActualizar;
        private Button botonActivar;
        private Button botonDesactivar;
        private GroupBox grupoRoles;
        private ComboBox comboRol;
        private Button botonAsignarRol;
        private Button botonQuitarRol;
        private GroupBox grupoContrasena;
        private Label etiquetaContrasena;
        private TextBox textoContrasena;
        private Label etiquetaConfirmacion;
        private TextBox textoConfirmacion;
        private Button botonEstablecerContrasena;
        private GroupBox grupoRegistro;
        private Label etiquetaNombre;
        private TextBox textoNombre;
        private Label etiquetaCorreo;
        private TextBox textoCorreo;
        private Label etiquetaRolInicial;
        private ComboBox comboRolInicial;
        private Button botonRegistrar;
        private Label etiquetaEstado;
    }
}
