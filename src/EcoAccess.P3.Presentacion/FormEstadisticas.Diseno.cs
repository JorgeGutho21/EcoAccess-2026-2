using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Estadísticas y reportes» (skill eco-ui-supervisor): encabezado, tarjeta de filtros con campos con
    /// icono, tarjeta de resumen y tarjeta de resultados con pestañas. Reutiliza los controles del Designer; los filtros siguen
    /// dentro de <c>panelFiltros</c>, que el formulario deshabilita mientras consulta.
    /// </summary>
    public partial class FormEstadisticas
    {
        private void ConstruirDiseno()
        {
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.ChartColumn, "Estadísticas y reportes",
                "Filtra los depósitos por fecha, caneca, material, persona y verificación; agrúpalos y exporta la página consultada.");

            var campos = ModuloEco.FilaCampos(
                ModuloEco.Campo(etiquetaDesde, IconChar.CalendarDays, fechaDesde, ancho: 200),
                ModuloEco.Campo(etiquetaHasta, IconChar.CalendarDays, fechaHasta, ancho: 200),
                ModuloEco.Campo(etiquetaCaneca, IconChar.Trash, comboCaneca, ancho: 250),
                ModuloEco.Campo(etiquetaMaterial, IconChar.Leaf, comboMaterial, ancho: 250),
                ModuloEco.Campo(etiquetaUsuario, IconChar.User, numeroUsuario, ancho: 210),
                ModuloEco.Campo(etiquetaVerificacion, IconChar.ClipboardCheck, comboVerificacion, ancho: 220),
                ModuloEco.Campo(etiquetaCriterio, IconChar.ListCheck, comboCriterio, ancho: 270),
                ModuloEco.Campo(etiquetaAgrupacion, IconChar.LayerGroup, comboAgrupacion, ancho: 250),
                ModuloEco.JuntoACampos(ModuloEco.Boton(botonConsultar, IconChar.MagnifyingGlass)));
            // panelFiltros (del Designer) se conserva como contenedor: sigue al alto de la fila de campos.
            panelFiltros.Controls.Clear();
            panelFiltros.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panelFiltros.BackColor = TemaEcoAccess.Superficie;
            panelFiltros.BorderStyle = BorderStyle.None;
            campos.Dock = DockStyle.Top;
            panelFiltros.Controls.Add(campos);
            campos.SizeChanged += (_, _) => panelFiltros.Height = campos.Height;
            var filtros = ModuloEco.Seccion("Filtros", "La fecha es la de Colombia. «Id usuario» en 0 incluye a todas las personas.");
            filtros.Agregar(panelFiltros);
            ModuloEco.Fila(raiz, filtros);

            // Resumen: texto de solo lectura dentro de su tarjeta (la tarjeta hace de marco).
            textoResumen.BorderStyle = BorderStyle.None;
            textoResumen.BackColor = TemaEcoAccess.Superficie;
            textoResumen.ForeColor = TemaEcoAccess.Texto;
            textoResumen.Font = TemaEcoAccess.Cuerpo;
            textoResumen.Height = 120;
            EstiloControlesEco.MarcarEstilizado(textoResumen);
            var resumen = ModuloEco.Seccion("Resumen de la consulta");
            resumen.Agregar(textoResumen);
            ModuloEco.Fila(raiz, resumen);

            Button? exportar = panelPaginas.Controls.OfType<Button>().FirstOrDefault(b => b != botonAnterior && b != botonSiguiente);
            panelPaginas.Controls.Clear();
            var paginas = ModuloEco.Botones(ModuloEco.Boton(botonAnterior, IconChar.ChevronLeft), ModuloEco.Boton(botonSiguiente, IconChar.ChevronRight), etiquetaPagina);
            etiquetaPagina.AutoSize = true;
            etiquetaPagina.Margin = new Padding(6, 12, 14, 0);
            etiquetaPagina.ForeColor = TemaEcoAccess.Suave;
            if (exportar is not null) paginas.Controls.Add(ModuloEco.Boton(exportar, IconChar.Download));
            ModuloEco.PaginaConTabla(pestanaAgrupado, tablaAgrupada);
            ModuloEco.PaginaConTabla(pestanaFallos, tablaFallos);
            ModuloEco.PaginaConTabla(pestanaDetalle, tablaDepositos, paginas);
            var resultados = ModuloEco.Seccion("Resultados", llenar: true);
            resultados.MinimumSize = new Size(0, 480);
            resultados.Agregar(pestanas, llenar: true);
            ModuloEco.Fila(raiz, resultados, llenar: true);
            ModuloEco.Fila(raiz, ModuloEco.Mensaje(etiquetaEstado));

            Disposed += (_, _) => panelPaginas.Dispose();
            ModuloEco.Terminar(this, raiz, new Size(640, 1180));
        }
    }
}
