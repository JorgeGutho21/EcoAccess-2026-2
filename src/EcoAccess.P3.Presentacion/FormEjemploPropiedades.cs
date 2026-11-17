using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// =========================================================================================
    /// GUÍA PRÁCTICA: INSERCIÓN DE CÓDIGO Y MODIFICACIÓN DE PROPIEDADES EN WINFORMS (EcoAccess)
    /// =========================================================================================
    /// 
    /// Este formulario es un laboratorio creado 100% por código para que aprendas:
    ///  1. Nombres de variables y su declaración.
    ///  2. Modificar colores (BackColor, ForeColor, RGB, colores de TemaEcoAccess).
    ///  3. Cambiar tamaños (Width, Height, Size, AutoSize).
    ///  4. Fuentes y tipografía (Font, estilos negrita, tamaños en puntos).
    ///  5. Posicionamiento y márgenes (Location, Dock, Margin, Padding).
    ///  6. Estados (Enabled, Visible, ReadOnly, MaxLength, Multiline).
    ///  7. Eventos interactivos (Click, TextChanged, CheckedChanged).
    /// 
    /// INSTRUCCIÓN:
    /// Busca las líneas comentadas con [PRUEBA / DESCOMENTA] y actívalas quitando '//'
    /// para ver cómo cambia cada control inmediatamente al compilar.
    /// </summary>
    public class FormEjemploPropiedades : Form
    {
        // ======================================================================================
        // 1. DECLARACIÓN DE VARIABLES (LOS ELEMENTOS DEL FORMULARIO)
        // ======================================================================================
        // Cada elemento visual en pantalla se guarda en una "variable".
        // La estructura estándar es: private readonly TipoControl _nombreVariable;

        // --- Etiquetas (Labels) ---
        private readonly Label _lblTitulo = new();
        private readonly Label _lblSubtitulo = new();
        private readonly Label _lblInfo = new();

        // --- Cajas de Texto (TextBox / CajaTexto) ---
        private readonly TextBox _txtSimple = new();
        private readonly CajaTexto _txtConAyuda = new();
        private readonly TextBox _txtMultilinea = new();
        private readonly TextBox _txtContrasena = new();

        // --- Botones (Button estándar y KryptonButton) ---
        private readonly Button _btnEstandar = new();
        private readonly KryptonButton _btnKrypton = new();
        private readonly Button _btnRestablecer = new();

        // --- Otros Controles Comunes ---
        private readonly CheckBox _chkOpcion = new();
        private readonly ComboBox _cmbColores = new();
        private readonly Panel _panelCajaDecorativa = new();

        public FormEjemploPropiedades()
        {
            // ==================================================================================
            // 2. PROPIEDADES DEL FORMULARIO PRINCIPAL (this)
            // ==================================================================================
            // 'this' representa la ventana completa.

            // --- Título de la ventana (Barra superior) ---
            this.Text = "Laboratorio de Aprendizaje: Propiedades en Código";

            // --- Tamaño de la ventana (Ancho, Alto en píxeles) ---
            this.ClientSize = new Size(820, 720);
            // [PRUEBA / DESCOMENTA] Cambiar el tamaño de la ventana:
            // this.ClientSize = new Size(600, 500);
            // this.ClientSize = new Size(950, 800);

            // --- Posición al abrir la ventana ---
            this.StartPosition = FormStartPosition.CenterScreen; // Centrado en la pantalla
            // this.StartPosition = FormStartPosition.WindowsDefaultLocation; // Donde Windows decida

            // --- Colores de fondo y texto del Formulario ---
            // Opción 1: Usando la paleta oficial de tu proyecto (TemaEcoAccess):
            this.BackColor = TemaEcoAccess.Fondo;
            this.ForeColor = TemaEcoAccess.Texto;
            // Opción 2: Usando colores predeterminados de C#:
            // this.BackColor = Color.White;
            // Opción 3: Usando código de color RGB (Rojo, Verde, Azul entre 0 y 255):
            // this.BackColor = Color.FromArgb(240, 244, 248);

            // --- Estilo del borde de la ventana ---
            this.FormBorderStyle = FormBorderStyle.Sizable; // Permite estirar los bordes con el mouse
            // this.FormBorderStyle = FormBorderStyle.FixedDialog; // Tamaño fijo (no redimensionable)
            // this.FormBorderStyle = FormBorderStyle.None; // Sin barra superior ni bordes

            // --- Botones de la barra de título ---
            this.MaximizeBox = true; // Botón maximizar
            this.MinimizeBox = true; // Botón minimizar

            // --- AutoScroll (Barras de desplazamiento si los controles no caben) ---
            this.AutoScroll = true;


            // ==================================================================================
            // 3. CONTENEDOR (Panel con Scroll vertical para organizar los elementos)
            // ==================================================================================
            var panelContenedor = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,                // Ocupa todo el interior de la ventana
                FlowDirection = FlowDirection.TopDown, // Apila los elementos de arriba hacia abajo
                WrapContents = false,                 // No pasa a una segunda columna lateral
                AutoScroll = true,                    // Activa scroll vertical si se desborda
                Padding = new Padding(24),            // Espacio interior en los bordes
                BackColor = Color.Transparent
            };
            this.Controls.Add(panelContenedor);


            // ==================================================================================
            // 4. ETIQUETAS (Label) - Textos informativos
            // ==================================================================================
            _lblTitulo = new Label();

            // --- Propiedad: Text (el contenido visible) ---
            _lblTitulo.Text = "Taller Práctico: Propiedades por Código";

            // --- Propiedad: Font (Familia de fuente, Tamaño en pt, Estilo) ---
            _lblTitulo.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            // [PRUEBA / DESCOMENTA] Otras fuentes y estilos:
            // _lblTitulo.Font = new Font("Arial", 18, FontStyle.Italic);
            // _lblTitulo.Font = new Font("Consolas", 15, FontStyle.Underline);

            // --- Propiedad: ForeColor (Color del texto) ---
            _lblTitulo.ForeColor = TemaEcoAccess.VerdeOscuro;
            // [PRUEBA / DESCOMENTA] Probar otros colores:
            // _lblTitulo.ForeColor = Color.Crimson;
            // _lblTitulo.ForeColor = Color.FromArgb(14, 116, 144);

            // --- Propiedad: AutoSize (se adapta automáticamente al largo del texto) ---
            _lblTitulo.AutoSize = true;

            // --- Propiedad: Margin (Espacio exterior: Izquierda, Arriba, Derecha, Abajo) ---
            _lblTitulo.Margin = new Padding(0, 0, 0, 8);

            panelContenedor.Controls.Add(_lblTitulo);


            // --- Subtítulo descriptivo ---
            _lblSubtitulo = new Label
            {
                Text = "Descomenta o cambia las propiedades de este archivo y recompila para ver el cambio visual.",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TemaEcoAccess.Suave,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 18)
            };
            panelContenedor.Controls.Add(_lblSubtitulo);


            // ==================================================================================
            // 5. CAJAS DE TEXTO (TextBox) - Entrada de datos
            // ==================================================================================
            panelContenedor.Controls.Add(CrearEncabezado("1. Cajas de Texto (TextBox)"));

            // --- A) Caja de Texto Básica ---
            _txtSimple = new TextBox();

            // --- Propiedad: Text (Texto inicial) ---
            _txtSimple.Text = "Texto inicial para editar";

            // --- Propiedad: Size (Ancho, Alto en píxeles) ---
            _txtSimple.Size = new Size(360, 28);
            // [PRUEBA / DESCOMENTA] Modificar ancho específico:
            // _txtSimple.Width = 480;

            // --- Propiedad: Font (Letra de lo que escribe el usuario) ---
            _txtSimple.Font = new Font("Segoe UI", 11, FontStyle.Regular);

            // --- Propiedad: BackColor y ForeColor (Fondo de la caja y color de letra) ---
            _txtSimple.BackColor = Color.White;
            _txtSimple.ForeColor = Color.FromArgb(30, 41, 59);
            // [PRUEBA / DESCOMENTA] Fondo amarillo pastel o gris suave:
            // _txtSimple.BackColor = Color.FromArgb(254, 249, 195);
            // _txtSimple.ForeColor = Color.DarkBlue;

            // --- Propiedad: MaxLength (Máxima cantidad de caracteres permitidos) ---
            _txtSimple.MaxLength = 40; // No deja escribir más de 40 caracteres

            // --- Propiedad: TextAlign (Alineación del texto) ---
            _txtSimple.TextAlign = HorizontalAlignment.Left;
            // [PRUEBA / DESCOMENTA] Centrar o alinear a la derecha:
            // _txtSimple.TextAlign = HorizontalAlignment.Center;
            // _txtSimple.TextAlign = HorizontalAlignment.Right;

            _txtSimple.Margin = new Padding(0, 4, 0, 12);
            panelContenedor.Controls.Add(_txtSimple);


            // --- B) Caja con Texto de Ayuda / Placeholder (Usa CajaTexto de tu proyecto) ---
            _txtConAyuda = new CajaTexto
            {
                PlaceholderText = "Escribe aquí tu correo o dato...", // Texto transparente de guía
                Size = new Size(360, 28),
                Font = new Font("Segoe UI", 10.5f),
                Margin = new Padding(0, 0, 0, 12)
            };
            panelContenedor.Controls.Add(_txtConAyuda);


            // --- C) Caja de Texto Multilínea (para párrafos o notas largas) ---
            _txtMultilinea = new TextBox();

            // --- Propiedad: Multiline (Permite escribir múltiples renglones) ---
            _txtMultilinea.Multiline = true;

            // --- Propiedad: ScrollBars (Barras de scroll interna) ---
            _txtMultilinea.ScrollBars = ScrollBars.Vertical;

            _txtMultilinea.Size = new Size(460, 75);
            _txtMultilinea.Text = "Línea 1: Esta caja permite múltiples líneas ('Multiline = true').\r\nLínea 2: Puedes escribir notas largas aquí.";
            _txtMultilinea.Font = new Font("Segoe UI", 9.5f);
            _txtMultilinea.Margin = new Padding(0, 0, 0, 12);
            // [PRUEBA / DESCOMENTA] Solo lectura (el usuario solo puede leer, no modificar):
            // _txtMultilinea.ReadOnly = true;

            panelContenedor.Controls.Add(_txtMultilinea);


            // --- D) Caja de Texto para Contraseña (Oculta los caracteres) ---
            _txtContrasena = new TextBox
            {
                Size = new Size(260, 28),
                Font = new Font("Segoe UI", 10.5f),
                Margin = new Padding(0, 0, 0, 20)
            };
            // --- Propiedad: UseSystemPasswordChar (Usa el punto nativo de Windows) ---
            _txtContrasena.UseSystemPasswordChar = true;
            // [PRUEBA / DESCOMENTA] Cambiar el carácter oculto a un asterisco (*):
            // _txtContrasena.UseSystemPasswordChar = false;
            // _txtContrasena.PasswordChar = '*';

            panelContenedor.Controls.Add(_txtContrasena);


            // ==================================================================================
            // 6. BOTONES (Button) - Acciones y Estilos
            // ==================================================================================
            panelContenedor.Controls.Add(CrearEncabezado("2. Botones y Eventos de Clic (Button)"));

            // Panel para colocar los botones uno al lado del otro
            var filaBotones = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 4, 0, 16)
            };

            // --- A) Botón Estándar de Windows Forms ---
            _btnEstandar = new Button();

            // --- Propiedad: Text ---
            _btnEstandar.Text = "Botón Estándar";

            // --- Propiedad: Size (Ancho, Alto) ---
            _btnEstandar.Size = new Size(160, 40);

            // --- Propiedades: BackColor y ForeColor ---
            _btnEstandar.BackColor = Color.FromArgb(22, 138, 85); // Verde marca
            _btnEstandar.ForeColor = Color.White;
            // [PRUEBA / DESCOMENTA] Probar otros colores:
            // _btnEstandar.BackColor = Color.DodgerBlue;
            // _btnEstandar.BackColor = Color.FromArgb(180, 35, 24); // Rojo error

            // --- Propiedad: Font ---
            _btnEstandar.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            // --- Propiedad: FlatStyle (Diseño plano moderno sin efecto 3D viejo) ---
            _btnEstandar.FlatStyle = FlatStyle.Flat;
            _btnEstandar.FlatAppearance.BorderSize = 0; // Sin borde visible
            // [PRUEBA / DESCOMENTA] Con borde definido:
            // _btnEstandar.FlatAppearance.BorderSize = 2;
            // _btnEstandar.FlatAppearance.BorderColor = Color.DarkGreen;

            // --- Propiedad: Cursor (Icono del ratón sobre el botón) ---
            _btnEstandar.Cursor = Cursors.Hand; // Mano de clic

            _btnEstandar.Margin = new Padding(0, 0, 12, 0);

            // --- EVENTO CLICK: ¿Qué acción realiza al pulsarlo? ---
            _btnEstandar.Click += (sender, e) =>
            {
                string textoIngresado = _txtSimple.Text;
                _lblInfo.Text = $"¡Clic en Botón Estándar! El texto de la caja es: '{textoIngresado}'";
                _lblInfo.ForeColor = Color.DarkGreen;
                _btnEstandar.BackColor = Color.SeaGreen;
            };
            filaBotones.Controls.Add(_btnEstandar);


            // --- B) Botón con Krypton (Diseño propio de EcoAccess) ---
            _btnKrypton = new KryptonButton
            {
                Text = "Botón Krypton",
                Size = new Size(160, 40),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 12, 0)
            };
            // Aplica el estilo oficial de tu proyecto:
            TemaEcoAccess.EstilizarAccion(_btnKrypton, principal: true);
            // [PRUEBA / DESCOMENTA] Estilo secundario (borde y fondo claro):
            // TemaEcoAccess.EstilizarAccion(_btnKrypton, principal: false);

            _btnKrypton.Click += (sender, e) =>
            {
                _lblInfo.Text = "¡Clic en KryptonButton! Usa los estilos compartidos de TemaEcoAccess.";
                _lblInfo.ForeColor = TemaEcoAccess.Info;
            };
            filaBotones.Controls.Add(_btnKrypton);


            // --- C) Botón Secundario (Limpiar / Restablecer) ---
            _btnRestablecer = new Button
            {
                Text = "Restablecer Campos",
                Size = new Size(160, 40),
                BackColor = Color.WhiteSmoke,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnRestablecer.FlatAppearance.BorderColor = Color.LightGray;

            _btnRestablecer.Click += (sender, e) =>
            {
                _txtSimple.Clear();
                _txtConAyuda.Clear();
                _txtMultilinea.Text = "Línea 1: Esta caja permite múltiples líneas.\r\nLínea 2: Texto restablecido.";
                _txtContrasena.Clear();
                _lblInfo.Text = "Campos restablecidos a sus valores iniciales.";
                _lblInfo.ForeColor = TemaEcoAccess.Suave;
                _btnEstandar.BackColor = Color.FromArgb(22, 138, 85);
            };
            filaBotones.Controls.Add(_btnRestablecer);

            panelContenedor.Controls.Add(filaBotones);


            // ==================================================================================
            // 7. ETIQUETA INFORMATIVA DINÁMICA (Muestra resultados en tiempo real)
            // ==================================================================================
            _lblInfo = new Label
            {
                Text = "Estado: interactúa con los controles arriba para ver los cambios aquí.",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = TemaEcoAccess.Suave,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 18)
            };
            panelContenedor.Controls.Add(_lblInfo);


            // ==================================================================================
            // 8. OTROS ELEMENTOS ÚTILES (CheckBox, ComboBox y Panel)
            // ==================================================================================
            panelContenedor.Controls.Add(CrearEncabezado("3. Casilla (CheckBox), Desplegable (ComboBox) y Panel"));

            // --- CheckBox (Casilla de verificación) ---
            _chkOpcion = new CheckBox
            {
                Text = "Habilitar / Deshabilitar la caja de texto simple",
                Checked = true,                     // Comienza marcada
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 0, 12)
            };
            // Evento: Se activa cuando marcas o desmarcas la casilla
            _chkOpcion.CheckedChanged += (sender, e) =>
            {
                // Propiedad: Enabled (activa o desactiva la interacción con un control)
                _txtSimple.Enabled = _chkOpcion.Checked;
                _lblInfo.Text = _chkOpcion.Checked ? "Caja de texto: HABILITADA." : "Caja de texto: DESHABILITADA.";
            };
            panelContenedor.Controls.Add(_chkOpcion);


            // --- ComboBox (Menú desplegable) ---
            _cmbColores = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, // Solo permite seleccionar, no escribir texto libre
                Font = new Font("Segoe UI", 10),
                Width = 260,
                Margin = new Padding(0, 0, 0, 14)
            };
            // Agregar opciones al desplegable:
            _cmbColores.Items.Add("Verde Marca (EcoAccess)");
            _cmbColores.Items.Add("Azul Océano");
            _cmbColores.Items.Add("Rojo Alerta");
            _cmbColores.Items.Add("Pizarra Oscuro");
            _cmbColores.SelectedIndex = 0; // Selecciona la primera opción por defecto

            // Evento: Al cambiar la selección del menú
            _cmbColores.SelectedIndexChanged += (sender, e) =>
            {
                // Cambiar el color de fondo del panel decorativo según la opción:
                _panelCajaDecorativa.BackColor = _cmbColores.SelectedIndex switch
                {
                    0 => TemaEcoAccess.Verde,
                    1 => Color.RoyalBlue,
                    2 => Color.Crimson,
                    3 => Color.DarkSlateGray,
                    _ => Color.Gray
                };
            };
            panelContenedor.Controls.Add(_cmbColores);


            // --- Panel (Contenedor / Caja decorativa) ---
            _panelCajaDecorativa = new Panel
            {
                Size = new Size(320, 55),
                BackColor = TemaEcoAccess.Verde,
                Margin = new Padding(0, 0, 0, 20)
            };
            var lblDentroPanel = new Label
            {
                Text = "Caja interactiva: cambia de color con el ComboBox",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Dock = DockStyle.Fill,                   // Ocupa todo el interior del panel
                TextAlign = ContentAlignment.MiddleCenter // Centra el texto vertical y horizontalmente
            };
            _panelCajaDecorativa.Controls.Add(lblDentroPanel);
            panelContenedor.Controls.Add(_panelCajaDecorativa);


            // ==================================================================================
            // 9. EVENTO AL ESCRIBIR EN TIEMPO REAL (TextChanged)
            // ==================================================================================
            _txtSimple.TextChanged += (sender, e) =>
            {
                // Este código se ejecuta en cada pulsación de tecla
                _lblInfo.Text = $"Escribiendo en caja simple... Longitud: {_txtSimple.Text.Length} caracteres.";
                _lblInfo.ForeColor = Color.Black;
            };
        }

        /// <summary>
        /// Función auxiliar para crear títulos de sección limpios y uniformes.
        /// </summary>
        private static Label CrearEncabezado(string texto)
        {
            return new Label
            {
                Text = texto,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TemaEcoAccess.VerdeOscuro,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 8)
            };
        }
    }
}
