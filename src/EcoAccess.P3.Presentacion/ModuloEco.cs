using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Estructura visual común de los módulos de gestión (skill eco-ui-supervisor), tomada de las pantallas de ciudadano:
    /// encabezado con icono, título en verde oscuro y descripción; tarjetas blancas con título; campos con la etiqueta en
    /// semibold arriba y una caja de 46 px con icono (como el inicio de sesión); botones semibold con icono. Reutiliza los
    /// controles del Designer (mismos objetos y eventos): solo cambia dónde están y cómo se ven. Las medidas van en unidades
    /// lógicas a 96 ppp; el formulario escala por DPI.
    /// </summary>
    internal static class ModuloEco
    {
        internal const int AltoCampo = 46;
        internal const int AltoBoton = 42;

        /// <summary>Limpia el formulario del Designer y devuelve la columna raíz con el encabezado ya puesto.</summary>
        internal static TableLayoutPanel Iniciar(Form formulario, IconChar icono, string titulo, string descripcion, Control? banner = null)
        {
            formulario.SuspendLayout();
            formulario.Controls.Clear();
            formulario.AutoScaleMode = AutoScaleMode.Dpi;
            formulario.AutoScaleDimensions = new SizeF(96, 96);
            formulario.Font = TemaEcoAccess.Cuerpo;
            formulario.BackColor = TemaEcoAccess.Fondo;
            formulario.ForeColor = TemaEcoAccess.Texto;
            var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(24, 18, 24, 18), BackColor = TemaEcoAccess.Fondo };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            // Encabezado normal o, si el módulo lo trae, un banner con la estética del dashboard (BannerModulo).
            Fila(raiz, banner ?? Encabezado(icono, titulo, descripcion));
            return raiz;
        }

        /// <summary>Agrega una fila a una columna: de alto automático o, con <paramref name="llenar"/>, la que ocupa el resto.</summary>
        internal static void Fila(TableLayoutPanel columna, Control control, bool llenar = false)
        {
            int fila = columna.RowCount++;
            columna.RowStyles.Add(llenar ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
            control.Dock = DockStyle.Fill;
            columna.Controls.Add(control, 0, fila);
        }

        /// <summary>Coloca la raíz y fija el tamaño mínimo a partir del cual el módulo se desplaza en lugar de apretarse.</summary>
        internal static void Terminar(Form formulario, TableLayoutPanel raiz, Size minimo)
        {
            formulario.MinimumSize = minimo;
            formulario.Controls.Add(raiz);
            formulario.ResumeLayout(true);
            TemaEcoAccess.Aplicar(formulario);
        }

        /// <summary>
        /// Como <see cref="Terminar"/>, pero el módulo se desplaza solo: la columna (de filas automáticas) va dentro de un
        /// <see cref="PanelDesplazable"/> que ocupa el formulario. Hace falta en módulos altos: Windows no deja que un formulario
        /// sea más alto que la pantalla (SystemInformation.MaxWindowTrackSize), así que un alto mínimo grande recortaba el final
        /// aunque la barra de la ventana principal siguiera. También encauza la rueda del ratón (<see cref="RuedaEco"/>).
        /// </summary>
        internal static PanelDesplazable TerminarDesplazable(Form formulario, TableLayoutPanel raiz, int anchoMinimo = 640)
        {
            raiz.Dock = DockStyle.Top;
            raiz.AutoSize = true;
            raiz.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            var desplazable = new PanelDesplazable { Dock = DockStyle.Fill, BackColor = TemaEcoAccess.Fondo, AccessibleName = formulario.Text };
            desplazable.Controls.Add(raiz);
            formulario.MinimumSize = new Size(anchoMinimo, 420);
            formulario.Controls.Add(desplazable);
            formulario.ResumeLayout(true);
            TemaEcoAccess.Aplicar(formulario);
            RuedaEco.Encauzar(formulario);
            return desplazable;
        }

        internal static Control Encabezado(IconChar icono, string titulo, string descripcion)
        {
            var fila = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 18), BackColor = TemaEcoAccess.Fondo };
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fila.Controls.Add(new InsigniaIcono(icono, TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave, circular: false)
            {
                Size = new Size(56, 56), Margin = new Padding(0, 0, 16, 0), Anchor = AnchorStyles.Left, AccessibleName = titulo,
            }, 0, 0);
            var textos = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = TemaEcoAccess.Fondo };
            textos.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.Titulo, ForeColor = TemaEcoAccess.VerdeOscuro, Margin = Padding.Empty, UseMnemonic = false });
            textos.Controls.Add(new Label { Text = descripcion, AutoSize = true, Font = TemaEcoAccess.Entrada, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 4, 0, 0), UseMnemonic = false });
            fila.Controls.Add(textos, 1, 0);
            // La descripción se parte en líneas al ancho que deja el icono (una tabla de tamaño automático no la limita sola).
            // El ancho disponible es el del contenedor: dentro de una columna de tamaño automático el propio encabezado se
            // encogería hasta su texto y lo partiría letra por letra.
            void Ajustar()
            {
                int ancho = Math.Max(fila.ClientSize.Width, fila.Parent is { } padre ? padre.ClientSize.Width - padre.Padding.Horizontal - fila.Margin.Horizontal : 0);
                int libre = Math.Max(TemaEcoAccess.Px(fila, 200), ancho - TemaEcoAccess.Px(fila, 80));
                foreach (Label etiqueta in textos.Controls.OfType<Label>())
                    if (etiqueta.MaximumSize.Width != libre) etiqueta.MaximumSize = new Size(libre, 0);
            }
            Control? padreActual = null;
            fila.ParentChanged += (_, _) =>
            {
                if (padreActual is not null) padreActual.SizeChanged -= AlCambiar;
                padreActual = fila.Parent;
                if (padreActual is not null) padreActual.SizeChanged += AlCambiar;
                Ajustar();
            };
            void AlCambiar(object? s, EventArgs e) => Ajustar();
            fila.SizeChanged += AlCambiar;
            return fila;
        }

        /// <summary>Tarjeta blanca con título (y descripción opcional); con <paramref name="llenar"/> ocupa todo el alto disponible.</summary>
        internal static SeccionEco Seccion(string titulo, string? descripcion = null, bool llenar = false, IconChar icono = IconChar.None) => new(titulo, descripcion, llenar, icono);

        /// <summary>Campo con la etiqueta en semibold arriba y la caja de 46 px con icono, como en el inicio de sesión y el registro.</summary>
        internal static TableLayoutPanel Campo(string etiqueta, IconChar icono, Control entrada, string? ayuda = null, int ancho = 0, int alto = AltoCampo) =>
            Campo(new Label { Text = etiqueta }, icono, entrada, ayuda, ancho, alto);

        internal static TableLayoutPanel Campo(Label etiqueta, IconChar icono, Control entrada, string? ayuda = null, int ancho = 0, int alto = AltoCampo)
        {
            var campo = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 14, 14), BackColor = TemaEcoAccess.Superficie };
            campo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            etiqueta.AutoSize = true;
            etiqueta.Dock = DockStyle.None;
            etiqueta.Anchor = AnchorStyles.Left;
            etiqueta.Font = TemaEcoAccess.Subtitulo;
            etiqueta.ForeColor = TemaEcoAccess.Texto;
            etiqueta.Margin = new Padding(0, 0, 0, 6);
            etiqueta.Padding = Padding.Empty;
            entrada.Font = TemaEcoAccess.Entrada;
            if (string.IsNullOrEmpty(entrada.AccessibleName)) entrada.AccessibleName = etiqueta.Text.Replace("&", "");
            if (entrada is ComboBox lista) EstiloControlesEco.Lista(lista);
            if (entrada is DateTimePicker fecha) EstiloControlesEco.Fecha(fecha);
            var marco = new MarcoEntrada(entrada, icono) { Height = alto, Margin = Padding.Empty, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            // En una fila de filtros el campo tiene ancho fijo (la tabla de tamaño automático no lo toma del marco).
            if (ancho > 0) { marco.Width = ancho; campo.MinimumSize = new Size(ancho, 0); }
            marco.Encerrar();
            EstiloControlesEco.MarcarEstilizado(entrada);
            campo.Controls.Add(etiqueta, 0, 0);
            campo.Controls.Add(marco, 0, 1);
            if (ayuda is not null)
                campo.Controls.Add(new Label { Text = ayuda, AutoSize = true, Font = TemaEcoAccess.Pequena, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 5, 0, 0), UseMnemonic = false }, 0, 2);
            return campo;
        }

        /// <summary>Botón con la tipografía de la app (semibold 11), alto 42 e icono opcional del color del texto.</summary>
        internal static T Boton<T>(T boton, IconChar icono = IconChar.None, bool ancho = false) where T : Button
        {
            boton.Font = TemaEcoAccess.Subtitulo;
            boton.AutoSize = true;
            boton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            boton.MinimumSize = new Size(0, AltoBoton);
            boton.Padding = new Padding(icono == IconChar.None ? 14 : 38, 0, 14, 0);
            boton.Margin = ancho ? new Padding(0, 0, 0, 10) : new Padding(0, 0, 10, 10);
            // A todo el ancho el botón no se ajusta a su texto: ocupa la celda con el alto de botón de la app.
            if (ancho)
            {
                boton.AutoSize = false;
                boton.Height = AltoBoton;
            }
            boton.Dock = ancho ? DockStyle.Fill : DockStyle.None;
            boton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            if (icono != IconChar.None) EstiloControlesEco.IconoBoton(boton, icono);
            return boton;
        }

        /// <summary>Icono para un campo según su etiqueta o nombre (cuando el módulo no indica uno).</summary>
        internal static IconChar IconoCampo(string pista)
        {
            string p = pista.ToLowerInvariant();
            if (p.Contains("correo")) return IconChar.Envelope;
            if (p.Contains("contraseña") || p.Contains("contrasena") || p.Contains("clave") || p.Contains("confirm")) return IconChar.Lock;
            if (p.Contains("código") || p.Contains("codigo")) return IconChar.Barcode;
            if (p.Contains("categor")) return IconChar.LayerGroup;
            if (p.Contains("descrip") || p.Contains("referencia") || p.Contains("comentario")) return IconChar.AlignLeft;
            if (p.Contains("rol")) return IconChar.UserTag;
            if (p.Contains("caneca")) return IconChar.Trash;
            if (p.Contains("material")) return IconChar.Leaf;
            if (p.Contains("punto") || p.Contains("ubicaci")) return IconChar.LocationDot;
            if (p.Contains("fecha") || p.Contains("desde") || p.Contains("hasta")) return IconChar.CalendarDays;
            if (p.Contains("ciudadano") || p.Contains("usuario") || p.Contains("nombre")) return IconChar.User;
            if (p.Contains("buscar")) return IconChar.MagnifyingGlass;
            return IconChar.PenToSquare;
        }

        /// <summary>Icono para un botón del Designer según su nombre o texto.</summary>
        internal static IconChar IconoBoton(Button boton)
        {
            string p = (boton.Name + " " + boton.Text).ToLowerInvariant();
            if (p.Contains("deshabilitar") || p.Contains("desactivar") || p.Contains("rechazar")) return IconChar.CircleXmark;
            if (p.Contains("habilitar") || p.Contains("activar") || p.Contains("verific")) return IconChar.CircleCheck;
            if (p.Contains("quitar")) return IconChar.UserMinus;
            if (p.Contains("asignar")) return IconChar.UserPlus;
            if (p.Contains("contraseña") || p.Contains("contrasena") || p.Contains("cambiar")) return IconChar.Key;
            if (p.Contains("registrar") || p.Contains("crear") || p.Contains("admitir")) return IconChar.Plus;
            if (p.Contains("actualizar")) return IconChar.ArrowsRotate;
            if (p.Contains("buscar") || p.Contains("consultar")) return IconChar.MagnifyingGlass;
            if (p.Contains("anterior")) return IconChar.ChevronLeft;
            if (p.Contains("siguiente")) return IconChar.ChevronRight;
            if (p.Contains("cancelar")) return IconChar.Xmark;
            if (p.Contains("exportar") || p.Contains("guardar")) return IconChar.Download;
            if (p.Contains("enviar")) return IconChar.PaperPlane;
            return IconChar.None;
        }

        /// <summary>Fila de botones que pasa a la línea siguiente si no caben.</summary>
        internal static FlowLayoutPanel Botones(params Control[] botones)
        {
            var fila = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Margin = Padding.Empty, BackColor = TemaEcoAccess.Superficie };
            fila.Controls.AddRange(botones);
            return fila;
        }

        /// <summary>Botones en dos columnas del mismo ancho (acciones de una tarjeta lateral).</summary>
        internal static TableLayoutPanel Rejilla(params Button[] botones)
        {
            var rejilla = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty, BackColor = TemaEcoAccess.Superficie };
            rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            rejilla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < botones.Length; i++)
            {
                Button boton = botones[i];
                boton.AutoSize = false;
                boton.Height = AltoBoton;
                boton.Dock = DockStyle.Fill;
                boton.Padding = new Padding(6, 0, 6, 0);
                boton.Margin = new Padding(i % 2 == 0 ? 0 : 5, 0, i % 2 == 0 ? 5 : 0, 10);
                if (i % 2 == 0) rejilla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                rejilla.Controls.Add(boton, i % 2, i / 2);
            }
            return rejilla;
        }

        /// <summary>Fila de campos (filtros) que se reparte en varias líneas en pantallas angostas.</summary>
        internal static FlowLayoutPanel FilaCampos(params Control[] campos)
        {
            var fila = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Margin = Padding.Empty, BackColor = TemaEcoAccess.Superficie };
            fila.Controls.AddRange(campos);
            return fila;
        }

        /// <summary>Un botón al lado de campos: baja lo que mide la etiqueta para quedar alineado con las cajas.</summary>
        internal static T JuntoACampos<T>(T boton) where T : Button
        {
            boton.Margin = new Padding(0, TemaEcoAccess.Subtitulo.Height + 6, 10, 14);
            boton.MinimumSize = new Size(boton.MinimumSize.Width, AltoCampo);
            return boton;
        }

        /// <summary>Contenido principal a la izquierda y columna lateral a la derecha; en pantallas angostas se apilan.</summary>
        internal static TableLayoutPanel DosColumnas(Control principal, Control lateral, int anchoLateral = 390, int anchoApilar = 900)
        {
            var cuerpo = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, BackColor = TemaEcoAccess.Fondo };
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, anchoLateral));
            cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            principal.Dock = DockStyle.Fill;
            principal.Margin = new Padding(0, 0, 16, 0);
            lateral.Dock = DockStyle.Fill;
            lateral.Margin = Padding.Empty;
            cuerpo.Controls.Add(principal, 0, 0);
            cuerpo.Controls.Add(lateral, 1, 0);
            bool? apilado = null;
            cuerpo.SizeChanged += (_, _) =>
            {
                bool apilar = cuerpo.ClientSize.Width < TemaEcoAccess.Px(cuerpo, anchoApilar);
                if (apilado == apilar || cuerpo.ClientSize.Width == 0) return;
                apilado = apilar;
                cuerpo.SuspendLayout();
                cuerpo.ColumnStyles[1].Width = apilar ? 0 : TemaEcoAccess.Px(cuerpo, anchoLateral);
                cuerpo.RowCount = apilar ? 2 : 1;
                cuerpo.RowStyles.Clear();
                cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, apilar ? 55 : 100));
                if (apilar) cuerpo.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
                principal.Margin = apilar ? new Padding(0, 0, 0, 14) : new Padding(0, 0, 16, 0);
                cuerpo.SetCellPosition(lateral, new TableLayoutPanelCellPosition(apilar ? 0 : 1, apilar ? 1 : 0));
                cuerpo.ResumeLayout(true);
            };
            return cuerpo;
        }

        /// <summary>
        /// Columna de tarjetas que se desplaza. Reutiliza el panel del Designer (<paramref name="contenedor"/>) para que el código
        /// que lo deshabilita durante una operación siga deshabilitando todas sus acciones.
        /// </summary>
        internal static Panel Columna(Panel contenedor, params Control[] tarjetas)
        {
            contenedor.Controls.Clear();
            contenedor.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            contenedor.AutoScroll = true;
            contenedor.BackColor = TemaEcoAccess.Fondo;
            contenedor.BorderStyle = BorderStyle.None;
            contenedor.Padding = Padding.Empty;
            var pila = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = TemaEcoAccess.Fondo, Margin = Padding.Empty };
            pila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (Control tarjeta in tarjetas) Fila(pila, tarjeta);
            contenedor.Controls.Add(pila);
            return contenedor;
        }

        /// <summary>Línea de estado o de ayuda bajo una sección: texto que se parte en líneas según el ancho.</summary>
        internal static Label Mensaje(Label etiqueta, bool pequeno = false)
        {
            etiqueta.AutoSize = true;
            etiqueta.Dock = DockStyle.Fill;
            etiqueta.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            etiqueta.Font = pequeno ? TemaEcoAccess.Pequena : TemaEcoAccess.Cuerpo;
            if (etiqueta.ForeColor == SystemColors.ControlText || etiqueta.ForeColor == Color.Empty) etiqueta.ForeColor = TemaEcoAccess.Suave;
            etiqueta.Margin = new Padding(0, 6, 0, 6);
            etiqueta.Padding = Padding.Empty;
            etiqueta.TextAlign = ContentAlignment.TopLeft;
            return etiqueta;
        }

        /// <summary>Las etiquetas automáticas se parten en líneas al ancho de su contenedor en lugar de salirse por la derecha.</summary>
        internal static void AjustarTextos(Control raiz)
        {
            void Ajustar(Control contenedor)
            {
                foreach (Control c in contenedor.Controls)
                {
                    if (c is Label { AutoSize: true } etiqueta && contenedor.ClientSize.Width > 0)
                        etiqueta.MaximumSize = new Size(Math.Max(TemaEcoAccess.Px(contenedor, 120), contenedor.ClientSize.Width - contenedor.Padding.Horizontal - etiqueta.Margin.Horizontal), 0);
                    else if (c is not (DataGridView or TabControl or MarcoEntrada)) Ajustar(c);
                }
            }
            raiz.SizeChanged += (_, _) => Ajustar(raiz);
            raiz.Layout += (_, e) => { if (e.AffectedControl == raiz) Ajustar(raiz); };
        }

        /// <summary>Página de pestaña con la tabla arriba y una fila de acciones abajo.</summary>
        internal static void PaginaConTabla(TabPage pagina, DataGridView tabla, params Control[] pie)
        {
            pagina.Controls.Clear();
            pagina.Padding = new Padding(12, 14, 12, 8);
            pagina.UseVisualStyleBackColor = false;
            pagina.BackColor = TemaEcoAccess.Superficie;
            var columna = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            columna.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Fila(columna, tabla, llenar: true);
            tabla.Margin = new Padding(0, 0, 0, 10);
            foreach (Control c in pie) Fila(columna, c);
            pagina.Controls.Add(columna);
        }
    }

    /// <summary>Tarjeta de un módulo de gestión: título en verde oscuro, descripción opcional y una columna de contenido.</summary>
    internal sealed class SeccionEco : TarjetaEco
    {
        internal SeccionEco(string titulo, string? descripcion, bool llenar, IconChar icono = IconChar.None)
        {
            Dock = llenar ? DockStyle.Fill : DockStyle.Top;
            Margin = new Padding(0, 0, 0, 14);
            Padding = new Padding(TemaEcoAccess.Px(this, 20 + Sombra), TemaEcoAccess.Px(this, 16 + Sombra), TemaEcoAccess.Px(this, 20 + Sombra), TemaEcoAccess.Px(this, 14 + Sombra));
            Cuerpo = new TableLayoutPanel { Dock = llenar ? DockStyle.Fill : DockStyle.Top, AutoSize = !llenar, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            Cuerpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Titulo = new Label { Text = titulo, AutoSize = true, Font = TemaEcoAccess.TituloSeccion, ForeColor = TemaEcoAccess.VerdeOscuro, Margin = new Padding(0, 0, 0, descripcion is null ? 12 : 2), UseMnemonic = false };
            if (icono != IconChar.None)
            {
                // Icono verde delante del título (Buscar cuentas, Cuentas, Gestión de la cuenta en el diseño de Usuarios).
                // El Label dibuja su Image dentro del relleno (taparía el texto): el icono se pinta aparte, en el hueco del relleno.
                Bitmap imagenTitulo = CacheIconos.Obtener(icono, TemaEcoAccess.Px(this, 24), TemaEcoAccess.Verde);
                Titulo.Padding = new Padding(imagenTitulo.Width + TemaEcoAccess.Px(this, 10), 0, 0, 0);
                Titulo.MinimumSize = new Size(0, imagenTitulo.Height + 4);
                Titulo.TextAlign = ContentAlignment.MiddleLeft;
                // Centrado con las letras (el texto de una etiqueta automática queda un poco por debajo del centro de la caja).
                Titulo.Paint += (_, e) => e.Graphics.DrawImage(imagenTitulo, 0, (Titulo.Height - imagenTitulo.Height) / 2 + TemaEcoAccess.Px(Titulo, 3));
            }
            ModuloEco.Fila(Cuerpo, Titulo);
            if (descripcion is not null)
                ModuloEco.Fila(Cuerpo, new Label { Text = descripcion, AutoSize = true, Font = TemaEcoAccess.Cuerpo, ForeColor = TemaEcoAccess.Suave, Margin = new Padding(0, 0, 0, 14), UseMnemonic = false });
            Controls.Add(Cuerpo);
            // Una tarjeta que no llena sigue el alto real de su contenido más el relleno (que incluye la sombra). El tamaño
            // automático del panel se calculaba antes de escalar y la sombra tapaba el último botón.
            if (!llenar) Cuerpo.SizeChanged += (_, _) =>
            {
                int alto = Cuerpo.Height + Padding.Vertical;
                if (Height != alto) Height = alto;
            };
        }

        internal TableLayoutPanel Cuerpo { get; }
        internal Label Titulo { get; }

        /// <summary>
        /// El relleno ya está en píxeles (Px). Al mover la tarjeta entre contenedores ya escalados, Windows Forms lo volvía a escalar
        /// en cada paso (26 → 98 px en la columna lateral): se conserva el mismo relleno.
        /// </summary>
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            Padding relleno = Padding;
            base.ScaleControl(factor, specified);
            Padding = relleno;
        }

        /// <summary>Agrega contenido a la tarjeta; con <paramref name="llenar"/> ocupa el alto restante (tablas, pestañas).</summary>
        internal SeccionEco Agregar(Control control, bool llenar = false)
        {
            ModuloEco.Fila(Cuerpo, control, llenar);
            if (control is TableLayoutPanel { AutoSize: true } campo && campo.Controls.OfType<MarcoEntrada>().Any()) campo.Margin = new Padding(0, 0, 0, 14);
            return this;
        }
    }
}
