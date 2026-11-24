using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using FontAwesome.Sharp;
using Krypton.Toolkit;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Registro manual» (skills eco-ui-operario y eco-ui-supervisor): el mismo <see cref="BannerModulo"/>,
    /// la caneca elegida con tarjetas e imagen como en la pantalla del ciudadano y del invitado, y los pasos en secciones.
    /// La lista desplegable de canecas del formulario sigue siendo la fuente de la elección (la lógica la lee); las tarjetas
    /// solo la cambian y la reflejan.
    /// </summary>
    public sealed partial class FormRegistroManual
    {
        private readonly VistaCanecaElegida _vistaCaneca = new() { Margin = new Padding(0, 2, 0, 4) };
        private readonly Label _sinCanecas = new() { UseMnemonic = false, Text = "Cargando las canecas registradas…" };
        private bool _seleccionInicialQuitada;

        private void ConstruirDiseno()
        {
            var banner = new BannerModulo("Registro manual de depósitos",
                "Registra el depósito de un ciudadano que no usa el bot: elige la caneca, busca el residuo y al ciudadano, solicita la apertura y confirma lo recibido.",
                IconChar.PenToSquare) { Estado = "Elige la caneca, busca el residuo y selecciona al ciudadano." };
            _estado.TextChanged += (_, _) => { if (_estado.Text.Length > 0) banner.Estado = _estado.Text; };
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.PenToSquare, "Registro manual de depósitos",
                "Registra el depósito de un ciudadano que no usa el bot.", banner);

            // 1. Caneca: arriba el selector con las canecas en servicio que registró el Supervisor (la misma lista que lee la
            // lógica) y debajo una sola caneca, la elegida, con la imagen de su tipo como en la pantalla del ciudadano.
            var paso1 = ModuloEco.Seccion("1. Verifica la caneca",
                "Elige en la lista la caneca donde el ciudadano va a depositar; debajo verás cuál es y qué residuos recibe.", icono: IconChar.TrashCan);
            paso1.Agregar(ModuloEco.FilaCampos(ModuloEco.Campo("Caneca", IconChar.TrashCan, _canecas, ancho: 460)));
            paso1.Agregar(_vistaCaneca);
            paso1.Agregar(new AvisoEco(_sinCanecas, null, IconChar.CircleInfo) { Margin = new Padding(0, 6, 0, 2) });
            // Al enlazar sus opciones la lista elige la primera: aquí nadie debe quedar elegido sin escogerlo.
            _canecas.SelectedIndexChanged += (_, _) => MostrarCanecaElegida();
            ModuloEco.Fila(raiz, paso1);

            // 2. Residuo y 3. Ciudadano, lado a lado (se apilan en pantallas angostas).
            TemaEcoAccess.EstilizarAccion(_botonBuscar, true);
            Tamano(_botonBuscar, IconChar.MagnifyingGlass, Color.White);
            var paso2 = ModuloEco.Seccion("2. Busca el residuo", "Solo aparecen residuos que recibe la caneca elegida.", icono: IconChar.MagnifyingGlass);
            paso2.Agregar(FilaConBoton(ModuloEco.Campo("Residuo", IconChar.Leaf, _buscarResiduo), _botonBuscar));
            paso2.Agregar(ModuloEco.Mensaje(_estadoBusqueda, pequeno: true));
            paso2.Agregar(Lista(_residuos, 330));
            paso2.Agregar(ModuloEco.Mensaje(_orientacion, pequeno: true));

            var paso3 = ModuloEco.Seccion("3. Ciudadano", "Busca por nombre o correo; si no tiene cuenta, regístralo.", icono: IconChar.User);
            paso3.Agregar(ModuloEco.Campo("Nombre o correo", IconChar.MagnifyingGlass, _buscarCiudadano));
            _ciudadanos.DrawMode = DrawMode.OwnerDrawFixed;
            _ciudadanos.ItemHeight = TemaEcoAccess.Px(this, 54);
            _ciudadanos.IntegralHeight = false;
            _ciudadanos.DrawItem += DibujarCiudadano;
            paso3.Agregar(Lista(_ciudadanos, 330));
            TemaEcoAccess.EstilizarAccion(_registrar, false);
            Tamano(_registrar, IconChar.UserPlus, TemaEcoAccess.VerdeOscuro);
            paso3.Agregar(ModuloEco.Botones(_registrar));
            ModuloEco.Fila(raiz, LadoALado(paso2, paso3));

            // 4. Confirmación: resumen, solicitud de apertura, progreso y lo recibido.
            var paso4 = ModuloEco.Seccion("4. Confirma y registra", "La caneca solo se abre si admite el residuo; la recepción queda pendiente de verificación hasta confirmarla.", icono: IconChar.CircleCheck);
            paso4.Agregar(new AvisoEco(_resumen, "Resumen de la solicitud", IconChar.ClipboardList) { Margin = new Padding(0, 0, 0, 12) });
            TemaEcoAccess.EstilizarAccion(_abrir, true);
            Tamano(_abrir, IconChar.DoorOpen, Color.White);
            paso4.Agregar(ModuloEco.Botones(_abrir));
            paso4.Agregar(ModuloEco.Mensaje(_estado));
            TemaEcoAccess.EstilizarAccion(_confirmar, true);
            Tamano(_confirmar, IconChar.ClipboardCheck, Color.White);
            _confirmar.Margin = new Padding(0, TemaEcoAccess.Subtitulo.Height + 6, 10, 14);
            paso4.Agregar(ModuloEco.FilaCampos(ModuloEco.Campo("Cantidad recibida", IconChar.Hashtag, _cantidad, ancho: 200), _confirmar));
            ModuloEco.Fila(raiz, paso4);

            ModuloEco.TerminarDesplazable(this, raiz, 760);
        }

        /// <summary>Botón Krypton de tamaño fijo con icono (el ajuste automático de Krypton estiraba los anchos).</summary>
        private void Tamano(KryptonButton boton, IconChar icono, Color colorIcono)
        {
            boton.AutoSize = false;
            boton.MinimumSize = Size.Empty;
            boton.Values.Image = CacheIconos.Obtener(icono, TemaEcoAccess.Px(this, 16), colorIcono);
            boton.Size = new Size(TextRenderer.MeasureText(boton.Text, TemaEcoAccess.Subtitulo).Width + TemaEcoAccess.Px(this, 76), TemaEcoAccess.Px(this, ModuloEco.AltoCampo));
            boton.Margin = new Padding(0, 0, 10, 8);
        }

        /// <summary>Campo que llena el ancho y el botón al lado, alineado con la caja.</summary>
        private static Control FilaConBoton(TableLayoutPanel campo, Control boton)
        {
            var fila = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = TemaEcoAccess.Superficie, Margin = Padding.Empty };
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            campo.Dock = DockStyle.Fill;
            boton.Margin = new Padding(0, TemaEcoAccess.Subtitulo.Height + 6, 0, 14);
            fila.Controls.Add(campo, 0, 0);
            fila.Controls.Add(boton, 1, 0);
            return fila;
        }

        /// <summary>Lista dentro de un marco blanco redondeado de alto fijo.</summary>
        private Control Lista(ListBox lista, int alto)
        {
            lista.BorderStyle = BorderStyle.None;
            lista.Dock = DockStyle.Fill;
            lista.BackColor = TemaEcoAccess.Superficie;
            var marco = new Panel { Height = TemaEcoAccess.Px(this, alto), BackColor = TemaEcoAccess.Superficie, Padding = new Padding(1), Margin = new Padding(0, 0, 0, 8) };
            marco.Paint += (_, e) =>
            {
                using var borde = new Pen(TemaEcoAccess.Borde);
                e.Graphics.DrawRectangle(borde, 0, 0, marco.Width - 1, marco.Height - 1);
            };
            marco.Controls.Add(lista);
            return marco;
        }

        /// <summary>Dos secciones en columnas iguales; en pantallas angostas una debajo de la otra.</summary>
        private static Control LadoALado(Control izquierda, Control derecha)
        {
            var fila = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = TemaEcoAccess.Fondo, Margin = Padding.Empty };
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            izquierda.Dock = derecha.Dock = DockStyle.Fill;
            izquierda.Margin = new Padding(0, 0, 8, 14);
            derecha.Margin = new Padding(8, 0, 0, 14);
            fila.Controls.Add(izquierda, 0, 0);
            fila.Controls.Add(derecha, 1, 0);
            bool? apilado = null;
            fila.SizeChanged += (_, _) =>
            {
                bool apilar = fila.Width > 0 && fila.Width < TemaEcoAccess.Px(fila, 900);
                if (apilado == apilar) return;
                apilado = apilar;
                fila.SuspendLayout();
                fila.ColumnStyles[1].Width = apilar ? 0 : 50;
                fila.ColumnStyles[0].Width = apilar ? 100 : 50;
                fila.SetCellPosition(derecha, new TableLayoutPanelCellPosition(apilar ? 0 : 1, apilar ? 1 : 0));
                izquierda.Margin = apilar ? new Padding(0, 0, 0, 14) : new Padding(0, 0, 8, 14);
                derecha.Margin = apilar ? new Padding(0, 0, 0, 14) : new Padding(8, 0, 0, 14);
                fila.ResumeLayout(true);
            };
            return fila;
        }


        /// <summary>
        /// Tipo de la caneca según los materiales que admite (la categoría más frecuente, sin contar peligrosos); si no
        /// se puede saber, según su nombre (verde, blanca o negra).
        /// </summary>
        private CategoriaMaterial TipoDe(Contenedor? caneca)
        {
            if (caneca is not null)
            {
                var categorias = caneca.MaterialesAdmitidos.Where(_catalogo.ContainsKey).Select(id => _catalogo[id].Categoria)
                    .Where(c => c != CategoriaMaterial.Peligroso).GroupBy(c => c).OrderByDescending(g => g.Count()).ToList();
                if (categorias.Count > 0 && (categorias.Count == 1 || categorias[0].Count() > categorias[1].Count())) return categorias[0].Key;
                string nombre = caneca.Nombre.ToLowerInvariant();
                if (nombre.Contains("verde")) return CategoriaMaterial.OrganicoAprovechable;
                if (nombre.Contains("blanca")) return CategoriaMaterial.Aprovechable;
                if (categorias.Count > 0) return categorias[0].Key;
            }
            return CategoriaMaterial.NoAprovechable;
        }

        /// <summary>Ciudadano con sus iniciales en un círculo verde suave, el nombre y el correo debajo.</summary>
        private void DibujarCiudadano(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || _ciudadanos.Items[e.Index] is not Opcion persona) return;
            Graphics g = e.Graphics;
            bool elegido = (e.State & DrawItemState.Selected) != 0;
            using (var fondo = new SolidBrush(elegido ? TemaEcoAccess.VerdeSuave : TemaEcoAccess.Superficie)) g.FillRectangle(fondo, e.Bounds);
            using (var linea = new Pen(TemaEcoAccess.Borde)) g.DrawLine(linea, e.Bounds.Left + TemaEcoAccess.Px(this, 10), e.Bounds.Bottom - 1, e.Bounds.Right - TemaEcoAccess.Px(this, 10), e.Bounds.Bottom - 1);
            string[] partes = persona.Texto.Split(new[] { " · " }, 2, StringSplitOptions.None);
            string nombre = partes[0], correo = partes.Length > 1 ? partes[1] : "";
            int lado = TemaEcoAccess.Px(this, 34), x = e.Bounds.Left + TemaEcoAccess.Px(this, 10), y = e.Bounds.Top + (e.Bounds.Height - lado) / 2;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var circulo = new SolidBrush(elegido ? TemaEcoAccess.Superficie : TemaEcoAccess.VerdeSuave)) g.FillEllipse(circulo, x, y, lado, lado);
            string iniciales = string.Concat(nombre.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
            TextRenderer.DrawText(g, iniciales, DibujoDashboard.Semibold(this, 11.5f), new Rectangle(x, y, lado, lado), TemaEcoAccess.VerdeOscuro,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            int xt = x + lado + TemaEcoAccess.Px(this, 12), ancho = e.Bounds.Right - xt - TemaEcoAccess.Px(this, 8), mitad = e.Bounds.Height / 2;
            const TextFormatFlags Linea = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, nombre, DibujoDashboard.Semibold(this, 13.5f), new Rectangle(xt, e.Bounds.Top + TemaEcoAccess.Px(this, 6), ancho, mitad), TemaEcoAccess.Texto, Linea | TextFormatFlags.Bottom);
            TextRenderer.DrawText(g, correo, DibujoDashboard.Fuente(this, 12f), new Rectangle(xt, e.Bounds.Top + mitad, ancho, mitad - TemaEcoAccess.Px(this, 4)), TemaEcoAccess.Suave, Linea | TextFormatFlags.Top);
            if (elegido)
            {
                Bitmap check = CacheIconos.Obtener(IconChar.CircleCheck, TemaEcoAccess.Px(this, 18), TemaEcoAccess.Verde);
                g.DrawImage(check, e.Bounds.Right - check.Width - TemaEcoAccess.Px(this, 12), e.Bounds.Top + (e.Bounds.Height - check.Height) / 2);
            }
        }

        /// <summary>
        /// Muestra la caneca elegida en el selector (una sola) con la imagen de su tipo y los residuos que recibe. La primera
        /// vez que se cargan las opciones quita la selección automática: nadie queda elegido sin escogerlo.
        /// </summary>
        private void MostrarCanecaElegida()
        {
            int total = _canecas.Items.Count;
            if (!_seleccionInicialQuitada && total > 0 && IsHandleCreated)
            {
                _seleccionInicialQuitada = true;
                BeginInvoke(new Action(() => { if (!IsDisposed && _canecas.SelectedIndex >= 0) _canecas.SelectedIndex = -1; }));
            }
            _sinCanecas.Text = total == 0
                ? "No hay canecas en servicio. El Supervisor las registra y las pone en servicio en «Canecas y QR»."
                : $"{total} {(total == 1 ? "caneca en servicio" : "canecas en servicio")} en la lista. El residuo debe ir en una caneca que lo admita: el paso 2 ya muestra solo esos.";
            if (_canecas.SelectedItem is not Opcion opcion || !_datosCanecas.TryGetValue(opcion.Id, out Contenedor? caneca))
            {
                _vistaCaneca.Vaciar(total == 0 ? "No hay canecas en servicio para elegir." : "Elige una caneca en la lista de arriba para ver cuál es y qué residuos recibe.");
                return;
            }
            var residuos = caneca.MaterialesAdmitidos.Where(_catalogo.ContainsKey).Select(id => _catalogo[id])
                .Where(m => m.Categoria != CategoriaMaterial.Peligroso).OrderBy(m => m.Nombre, StringComparer.CurrentCulture).Select(m => m.Nombre).ToList();
            _vistaCaneca.Mostrar(opcion.Texto, TipoDe(caneca), residuos);
        }
    }

    /// <summary>
    /// La caneca elegida para el registro manual: la imagen de su tipo (verde, blanca o negra, la misma de la pantalla del
    /// ciudadano y del invitado), su nombre, el tipo en pastilla y algunos de los residuos que recibe. Sin caneca elegida
    /// muestra las tres canecas atenuadas y una indicación.
    /// </summary>
    internal sealed class VistaCanecaElegida : Control
    {
        private string? _nombre;
        private CategoriaMaterial _tipo;
        private IReadOnlyList<string> _residuos = Array.Empty<string>();
        private string _indicacion = "Cargando las canecas registradas…";

        internal VistaCanecaElegida()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = TemaEcoAccess.Superficie;
            Height = TemaEcoAccess.Px(this, 300);
            AccessibleRole = AccessibleRole.Graphic;
            AccessibleName = "Caneca elegida";
        }

        internal void Mostrar(string nombre, CategoriaMaterial tipo, IReadOnlyList<string> residuos)
        {
            _nombre = nombre; _tipo = tipo; _residuos = residuos;
            AccessibleDescription = $"{nombre}: caneca {NombreTipo(tipo)} ({Descripcion(tipo)}), recibe {residuos.Count} residuos.";
            Invalidate();
        }

        internal void Vaciar(string indicacion)
        {
            _nombre = null; _indicacion = indicacion;
            AccessibleDescription = indicacion;
            Invalidate();
        }

        internal static string NombreTipo(CategoriaMaterial tipo) => tipo switch
        {
            CategoriaMaterial.OrganicoAprovechable => "verde",
            CategoriaMaterial.Aprovechable => "blanca",
            _ => "negra",
        };

        private static string Descripcion(CategoriaMaterial tipo) => tipo switch
        {
            CategoriaMaterial.OrganicoAprovechable => "orgánicos aprovechables",
            CategoriaMaterial.Aprovechable => "aprovechables / reciclables",
            _ => "no aprovechables",
        };

        private int P(float n) => TemaEcoAccess.Px(this, (int)n);

        private static void Dibujar(Graphics g, Image imagen, Rectangle zona, float opacidad = 1f)
        {
            float escala = Math.Min((float)zona.Width / imagen.Width, (float)zona.Height / imagen.Height);
            int w = (int)(imagen.Width * escala), h = (int)(imagen.Height * escala);
            var destino = new Rectangle(zona.Left + (zona.Width - w) / 2, zona.Top + (zona.Height - h) / 2, w, h);
            if (opacidad >= 1f) { g.DrawImage(imagen, destino); return; }
            using var atributos = new System.Drawing.Imaging.ImageAttributes();
            atributos.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = opacidad });
            g.DrawImage(imagen, destino, 0, 0, imagen.Width, imagen.Height, GraphicsUnit.Pixel, atributos);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var area = new RectangleF(1, 1, Width - 3, Height - 3);
            using (var ruta = RecursosEco.Redondeado(area, P(16)))
            {
                using (var relleno = new SolidBrush(_nombre is null ? TemaEcoAccess.Fondo : TemaEcoAccess.VerdeSuave)) g.FillPath(relleno, ruta);
                using var borde = new Pen(_nombre is null ? TemaEcoAccess.Borde : TemaEcoAccess.VerdeHoja);
                g.DrawPath(borde, ruta);
            }
            const TextFormatFlags Linea = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;

            if (_nombre is null)
            {
                // Sin caneca elegida: las tres canecas (verde, blanca y negra) atenuadas, una junto a otra, y la indicación.
                var zona = new Rectangle(P(24), P(18), Math.Min(Width - P(48), P(420)), Height - P(36));
                int anchoUna = (zona.Width - 2 * P(12)) / 3;
                CategoriaMaterial[] tipos = { CategoriaMaterial.OrganicoAprovechable, CategoriaMaterial.Aprovechable, CategoriaMaterial.NoAprovechable };
                for (int k = 0; k < tipos.Length; k++)
                    if (RecursosEco.Imagen($"registro/caneca_{NombreTipo(tipos[k])}.png") is { } una) // eco-estetica: permitido imágenes de las canecas del registro manual (pedido del usuario 23/11/2026)
                        Dibujar(g, una, new Rectangle(zona.Left + k * (anchoUna + P(12)), zona.Top, anchoUna, zona.Height), 0.35f);
                var texto = Rectangle.FromLTRB(zona.Right + P(24), P(18), Width - P(24), Height - P(18));
                TextRenderer.DrawText(g, _indicacion, DibujoDashboard.Semibold(this, 16f), texto, TemaEcoAccess.Suave,
                    TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                return;
            }

            // La caneca elegida: su imagen a la izquierda y la información a la derecha.
            var zonaImagen = new Rectangle(P(28), P(14), P(210), Height - P(28));
            if (RecursosEco.Imagen($"registro/caneca_{NombreTipo(_tipo)}.png") is { } imagen) Dibujar(g, imagen, zonaImagen); // eco-estetica: permitido imagen de la caneca del registro manual (pedido del usuario 23/11/2026)
            else DibujoCaneca.Pintar(g, new RectangleF(zonaImagen.Left + P(40), zonaImagen.Top + P(20), zonaImagen.Width - P(80), zonaImagen.Height - P(40)), _tipo);

            int x = zonaImagen.Right + P(28), ancho = Width - x - P(28), y = P(30);
            TextRenderer.DrawText(g, "Caneca elegida", DibujoDashboard.Semibold(this, 12.5f), new Rectangle(x, y, ancho, P(20)), TemaEcoAccess.Suave, Linea);
            y += P(24);
            Font titulo = DibujoDashboard.Semibold(this, 24f);
            TextRenderer.DrawText(g, _nombre, titulo, new Rectangle(x, y, ancho, titulo.Height + P(4)), TemaEcoAccess.VerdeOscuro, Linea);
            y += titulo.Height + P(10);

            // Pastilla del tipo con su icono y color.
            var (tono, fondo, icono, _) = EstiloCategoria.Para(_tipo);
            DisenoGestion.Pastilla(g, new Rectangle(x - P(8), y, ancho, P(34)), $"Caneca {NombreTipo(_tipo)} · {Descripcion(_tipo)}", tono, fondo, TemaEcoAccess.Borde, icono, tono, this);
            y += P(46);

            Bitmap check = CacheIconos.Obtener(IconChar.CircleCheck, P(16), TemaEcoAccess.Verde);
            g.DrawImage(check, x, y + P(2));
            string recibe = _residuos.Count == 0 ? "No tiene residuos del catálogo habilitados." : $"Recibe {_residuos.Count} {(_residuos.Count == 1 ? "residuo" : "residuos")} del catálogo:";
            TextRenderer.DrawText(g, recibe, DibujoDashboard.Semibold(this, 13.5f), new Rectangle(x + check.Width + P(8), y, ancho - check.Width - P(8), P(22)), TemaEcoAccess.Texto, Linea);
            y += P(28);

            // Algunos residuos como fichas pequeñas, en las líneas que quepan.
            Font fuenteFicha = DibujoDashboard.Fuente(this, 12f);
            int xf = x, altoFicha = fuenteFicha.Height + P(8), mostrados = 0;
            foreach (string residuo in _residuos)
            {
                int anchoFicha = TextRenderer.MeasureText(g, residuo, fuenteFicha, Size.Empty, TextFormatFlags.NoPadding).Width + P(20);
                if (xf > x && xf + anchoFicha > x + ancho) { xf = x; y += altoFicha + P(6); }
                if (y + altoFicha > Height - P(30)) break;
                using (var ruta = RecursosEco.Redondeado(new RectangleF(xf, y, Math.Min(anchoFicha, ancho), altoFicha), altoFicha / 2f))
                using (var blanco = new SolidBrush(TemaEcoAccess.Superficie))
                using (var borde = new Pen(TemaEcoAccess.VerdeHoja))
                {
                    g.FillPath(blanco, ruta);
                    g.DrawPath(borde, ruta);
                }
                TextRenderer.DrawText(g, residuo, fuenteFicha, new Rectangle(xf + P(10), y, Math.Min(anchoFicha, ancho) - P(20), altoFicha), TemaEcoAccess.Texto, Linea | TextFormatFlags.VerticalCenter);
                xf += anchoFicha + P(6);
                mostrados++;
            }
            if (mostrados < _residuos.Count && mostrados > 0)
                TextRenderer.DrawText(g, $"y {_residuos.Count - mostrados} más", fuenteFicha, new Rectangle(x, Height - P(14) - fuenteFicha.Height, ancho, fuenteFicha.Height), TemaEcoAccess.Suave, Linea);
        }
    }
}

