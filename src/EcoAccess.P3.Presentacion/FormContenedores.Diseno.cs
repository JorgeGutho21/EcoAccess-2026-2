using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Canecas y QR» con el diseño aprobado de Usuarios y Materiales (skill eco-ui-supervisor): banner con la
    /// estética del dashboard, tabla pulida (caneca con el color de su tipo, estado en pastilla, QR con icono) y una tarjeta
    /// lateral con píldoras para elegir la página (caneca, materiales, registro). Reutiliza los controles del Designer, así los
    /// eventos y la lógica no cambian; las acciones siguen dentro de <c>panelAcciones</c>, que el formulario deshabilita mientras
    /// trabaja.
    /// </summary>
    public partial class FormContenedores
    {
        private readonly TabControl _paginasCaneca = new PaginasSinPestanas { Dock = DockStyle.Fill, AccessibleName = "Gestión de la caneca" };
        private readonly Label _fichaCaneca = DisenoFormularios.Texto("Selecciona una caneca de la tabla.");

        private void ConstruirDiseno()
        {
            var banner = new BannerModulo("Canecas y códigos QR",
                "Registra las canecas de cada punto de instalación, controla si están en servicio, qué materiales admiten y su código QR.",
                IconChar.Qrcode) { Estado = "Sin canecas consultadas todavía." };
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.Trash, "Canecas y códigos QR",
                "Registra las canecas de cada punto de instalación, controla si están en servicio y qué materiales admiten.", banner);

            var lista = ModuloEco.Seccion("Canecas registradas", "Selecciona una caneca de la tabla para ver su QR, cambiar su estado o sus materiales.",
                llenar: true, icono: IconChar.TrashCan);
            lista.Agregar(tablaCanecas, llenar: true);
            lista.Agregar(ModuloEco.Mensaje(etiquetaEstado));

            // Página «Caneca»: ficha de la fila elegida, acciones frecuentes en botones suaves y la baja como acción de peligro.
            TableLayoutPanel caneca = CrearPaginaCaneca("Caneca");
            DisenoFormularios.Agregar(caneca, DisenoFormularios.Texto("Caneca seleccionada", true));
            DisenoFormularios.Agregar(caneca, DisenoGestion.Ficha(_fichaCaneca, IconChar.TrashCan, "Caneca"));
            DisenoFormularios.Agregar(caneca, DisenoGestion.BotonSuave(botonVerQr, IconChar.Qrcode));
            DisenoFormularios.Agregar(caneca, ModuloEco.Rejilla(
                DisenoGestion.BotonSuave(botonEnServicio, IconChar.CirclePlay),
                DisenoGestion.BotonSuave(botonFueraDeServicio, IconChar.CirclePause)));
            DisenoFormularios.Agregar(caneca, DisenoGestion.BotonSuave(botonActualizar, IconChar.ArrowsRotate));
            DisenoFormularios.Agregar(caneca, new AvisoEco(DisenoFormularios.Texto("Dar de baja es definitivo. Para pausar una caneca un tiempo, usa «Fuera de servicio»."),
                icono: IconChar.TriangleExclamation));
            DisenoFormularios.Agregar(caneca, ModuloEco.Boton(botonDarDeBaja, IconChar.TrashCan, ancho: true));

            // Página «Materiales»: lo que admite la caneca elegida (el título lo sigue escribiendo el formulario en grupoMateriales).
            TableLayoutPanel materiales = CrearPaginaCaneca("Materiales");
            Label tituloMateriales = DisenoFormularios.Texto(grupoMateriales.Text, true);
            grupoMateriales.TextChanged += (_, _) => tituloMateriales.Text = grupoMateriales.Text;
            DisenoFormularios.Agregar(materiales, tituloMateriales);
            DisenoFormularios.Agregar(materiales, new AvisoEco(DisenoFormularios.Texto("Lo que la caneca seleccionada recibe. Una caneca en servicio necesita al menos un material habilitado.")));
            listaAdmitidos.Height = 132;
            DisenoFormularios.Agregar(materiales, listaAdmitidos);
            listaAdmitidos.Margin = new Padding(3, 3, 3, 14); // el borde redondeado se dibuja 2 px por fuera de la lista
            DisenoFormularios.Agregar(materiales, DisenoGestion.BotonSuave(botonQuitar, IconChar.Xmark));
            DisenoFormularios.Agregar(materiales, ModuloEco.Campo("Material para admitir", IconChar.Leaf, comboMaterial));
            DisenoFormularios.Agregar(materiales, ModuloEco.Boton(botonAdmitir, IconChar.Plus, ancho: true));

            // Página «Registrar»: alta de una caneca en un punto activo.
            TableLayoutPanel registro = CrearPaginaCaneca("Registrar");
            DisenoFormularios.Agregar(registro, new AvisoEco(DisenoFormularios.Texto("Crea una caneca en un punto de instalación activo."),
                grupoRegistro.Text));
            DisenoFormularios.Agregar(registro, ModuloEco.Campo(etiquetaPunto, IconChar.LocationDot, comboPunto));
            DisenoFormularios.Agregar(registro, ModuloEco.Campo(etiquetaNombre, IconChar.Tag, textoNombre));
            DisenoGestion.TextoGuia(textoNombre, "Ej. EcoAccess · Caneca verde");
            DisenoFormularios.Agregar(registro, ModuloEco.Boton(botonRegistrar, IconChar.Plus, ancho: true));

            var gestion = ModuloEco.Seccion("Gestión de la caneca", llenar: true, icono: IconChar.Gear);
            gestion.Agregar(new SelectorPestanas(_paginasCaneca, IconChar.TrashCan, IconChar.Leaf, IconChar.Plus));
            gestion.Agregar(_paginasCaneca, llenar: true);
            panelAcciones.Controls.Clear();
            panelAcciones.AutoScroll = false;
            panelAcciones.BorderStyle = BorderStyle.None;
            panelAcciones.Padding = Padding.Empty;
            panelAcciones.BackColor = TemaEcoAccess.Fondo;
            gestion.Dock = DockStyle.Fill;
            gestion.Margin = Padding.Empty;
            panelAcciones.Controls.Add(gestion);
            ModuloEco.Fila(raiz, ModuloEco.DosColumnas(lista, panelAcciones, 470), llenar: true);

            PulirTablaCanecas(banner);
            Disposed += (_, _) => { grupoMateriales.Dispose(); grupoRegistro.Dispose(); };
            ModuloEco.Terminar(this, raiz, new Size(640, 600));
        }

        private TableLayoutPanel CrearPaginaCaneca(string titulo)
        {
            var pagina = new TabPage(titulo) { BackColor = Color.White, AutoScroll = true, Padding = Padding.Empty };
            var columna = DisenoFormularios.Columna();
            columna.Padding = new Padding(0, 4, 6, 4);
            int anchoAplicado = -1;
            columna.SizeChanged += (_, _) =>
            {
                int ancho = Math.Max(100, columna.ClientSize.Width - columna.Padding.Horizontal);
                if (ancho == anchoAplicado) return;
                anchoAplicado = ancho;
                foreach (Label label in columna.Controls.OfType<Label>())
                    label.MaximumSize = new Size(ancho, 0);
                listaAdmitidos.Width = ancho;
                // Las filas de alto automático se midieron con el texto partido en el ancho inicial (estrecho): se vuelven a medir.
                if (columna.IsHandleCreated) columna.BeginInvoke(new Action(() => { if (!columna.IsDisposed) columna.PerformLayout(); }));
            };
            pagina.Controls.Add(columna);
            _paginasCaneca.TabPages.Add(pagina);
            return columna;
        }

        /// <summary>
        /// Tabla con el aspecto del dashboard (DisenoGestion.PulirTabla): la caneca con un icono del color de su tipo, el estado en
        /// pastilla (verde en servicio, ámbar fuera de servicio, gris dada de baja) y el QR con su icono. La ficha lateral y la
        /// línea del banner leen la fila elegida y el total de la tabla; no consultan nada.
        /// </summary>
        private void PulirTablaCanecas(BannerModulo banner)
        {
            DisenoGestion.PulirTabla(tablaCanecas, columna => columna switch
            {
                "Nombre" => 290, "Punto" => 200, "Estado" => 160, "Materiales" => 260, "IdentificadorQr" => 220, "Alta" => 150, _ => 0,
            }, "Aún no hay canecas para mostrar. Registra la primera en la pestaña «Registrar».", IconChar.TrashCan);
            tablaCanecas.CellPainting += PintarCeldaCaneca;
            tablaCanecas.SelectionChanged += (_, _) => MostrarFichaCaneca();
            tablaCanecas.CurrentCellChanged += (_, _) => MostrarFichaCaneca();
            tablaCanecas.DataBindingComplete += (_, _) =>
            {
                var filas = tablaCanecas.Rows.Cast<DataGridViewRow>().Select(r => r.DataBoundItem).OfType<FilaCaneca>().ToList();
                int enServicio = filas.Count(f => f.Estado == "En servicio");
                banner.Estado = filas.Count == 0 ? "No hay canecas registradas." : $"{filas.Count} canecas registradas · {enServicio} en servicio";
                MostrarFichaCaneca();
            };
        }

        private void MostrarFichaCaneca()
        {
            _fichaCaneca.Text = tablaCanecas.CurrentRow?.DataBoundItem is FilaCaneca fila
                ? $"{fila.Nombre}\n{fila.Punto}\n{fila.Estado} · QR {fila.IdentificadorQr}"
                : "Selecciona una caneca de la tabla.";
        }

        /// <summary>Color de la caneca según su nombre (verde, negra o blanca, como las de la Resolución 2184).</summary>
        private static (Color Tono, Color Fondo) ColorCaneca(string nombre)
        {
            string n = nombre.ToLowerInvariant();
            if (n.Contains("verde")) return (TemaEcoAccess.Verde, TemaEcoAccess.VerdeSuave);
            if (n.Contains("negra")) return (TemaEcoAccess.Texto, TemaEcoAccess.Borde);
            if (n.Contains("blanca")) return (TemaEcoAccess.Suave, TemaEcoAccess.Fondo);
            return (TemaEcoAccess.VerdeOscuro, TemaEcoAccess.VerdeSuave);
        }

        private void PintarCeldaCaneca(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics is null || e.Value is not string valor) return;
            string columna = tablaCanecas.Columns[e.ColumnIndex].Name;
            if (columna is not ("Nombre" or "Estado" or "IdentificadorQr")) return;
            Graphics g = e.Graphics;
            bool elegida = (e.State & DataGridViewElementStates.Selected) != 0;
            e.PaintBackground(e.CellBounds, elegida);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int px(int n) => TemaEcoAccess.Px(tablaCanecas, n);
            Color textoFila = elegida ? e.CellStyle!.SelectionForeColor : TemaEcoAccess.Texto;
            const TextFormatFlags Linea = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            if (columna == "Nombre")
            {
                var (tono, fondo) = ColorCaneca(valor);
                int lado = px(30);
                var cuadro = new Rectangle(e.CellBounds.Left + px(8), e.CellBounds.Top + (e.CellBounds.Height - lado) / 2, lado, lado);
                DibujoEco.Icono(g, IconChar.TrashCan, tono, fondo, cuadro, px(8));
                TextRenderer.DrawText(g, valor, e.CellStyle?.Font ?? tablaCanecas.Font,
                    Rectangle.FromLTRB(cuadro.Right + px(10), e.CellBounds.Top, e.CellBounds.Right - px(6), e.CellBounds.Bottom), textoFila, Linea);
            }
            else if (columna == "IdentificadorQr")
            {
                Bitmap qr = CacheIconos.Obtener(IconChar.Qrcode, px(15), TemaEcoAccess.VerdeOscuro);
                int x = e.CellBounds.Left + px(10);
                g.DrawImage(qr, x, e.CellBounds.Top + (e.CellBounds.Height - qr.Height) / 2);
                TextRenderer.DrawText(g, valor, e.CellStyle?.Font ?? tablaCanecas.Font,
                    Rectangle.FromLTRB(x + qr.Width + px(8), e.CellBounds.Top, e.CellBounds.Right - px(6), e.CellBounds.Bottom), textoFila, Linea);
            }
            else if (valor == "Fuera de servicio")
                DisenoGestion.Pastilla(g, e.CellBounds, valor, TemaEcoAccess.Advertencia, TemaEcoAccess.AdvertenciaSuave, TemaEcoAccess.Borde,
                    IconChar.None, TemaEcoAccess.Advertencia, tablaCanecas);
            else
                DisenoGestion.PastillaEstado(g, e.CellBounds, valor, valor == "En servicio", tablaCanecas);
            e.Handled = true;
        }
    }
}
