using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Disposición Eco de «Depósitos e interacciones» (skill eco-ui-supervisor): encabezado, tarjeta de consulta con los filtros
    /// como campos con icono, y tarjeta de resultados con las pestañas. Reutiliza los controles del Designer; los filtros siguen
    /// dentro de <c>panelFiltros</c> y el botón de cancelar queda fuera, porque se usa mientras los filtros están deshabilitados.
    /// </summary>
    public partial class FormInteraccionesDepositos
    {
        private void ConstruirDiseno()
        {
            TableLayoutPanel raiz = ModuloEco.Iniciar(this, IconChar.BoxOpen, "Depósitos e interacciones",
                "Consulta los depósitos por fecha y caneca, registra la verificación del material observado y revisa las solicitudes y eventos de cada caneca.");

            panelFiltros.Controls.Clear();
            panelFiltros.AutoSize = true;
            panelFiltros.WrapContents = true;
            panelFiltros.BackColor = TemaEcoAccess.Superficie;
            panelFiltros.Padding = Padding.Empty;
            panelFiltros.Controls.AddRange(new Control[]
            {
                ModuloEco.Campo(etiquetaDesde, IconChar.CalendarDays, fechaDesde, ancho: 200),
                ModuloEco.Campo(etiquetaHasta, IconChar.CalendarDays, fechaHasta, ancho: 200),
                ModuloEco.Campo(etiquetaFiltroCaneca, IconChar.Trash, comboCaneca, ancho: 260),
                ModuloEco.Campo(etiquetaFiltroVerificacion, IconChar.ClipboardCheck, comboVerificacion, ancho: 220),
                ModuloEco.JuntoACampos(ModuloEco.Boton(botonConsultar, IconChar.MagnifyingGlass)),
            });
            var consulta = ModuloEco.Seccion("Consulta", "La fecha es la de Colombia. La consulta solo lee; verificar un depósito es la única acción que escribe.");
            consulta.Agregar(panelFiltros);
            consulta.Agregar(ModuloEco.Mensaje(etiquetaConsulta));
            consulta.Agregar(ModuloEco.Mensaje(etiquetaCaneca, pequeno: true));
            consulta.Agregar(ModuloEco.Mensaje(etiquetaUltimaConsulta, pequeno: true));
            consulta.Agregar(ModuloEco.Botones(ModuloEco.Boton(botonCancelar, IconChar.Xmark)));
            ModuloEco.Fila(raiz, consulta);

            // Pestaña de depósitos: tabla, paginación y, debajo, la verificación del material observado.
            var paginacion = ModuloEco.Botones(ModuloEco.Boton(botonAnterior, IconChar.ChevronLeft), ModuloEco.Boton(botonSiguiente, IconChar.ChevronRight), etiquetaPagina);
            etiquetaPagina.Margin = new Padding(6, 12, 0, 0);
            etiquetaPagina.ForeColor = TemaEcoAccess.Suave;
            panelDepositos.Controls.Clear();
            panelDepositos.AutoSize = true;
            panelDepositos.WrapContents = true;
            panelDepositos.BackColor = TemaEcoAccess.Superficie;
            _conteo.Font = TemaEcoAccess.Subtitulo;
            _conteo.Margin = new Padding(0, TemaEcoAccess.Subtitulo.Height + 18, 14, 14);
            panelDepositos.Controls.AddRange(new Control[]
            {
                ModuloEco.Campo(etiquetaMaterial, IconChar.Leaf, comboMaterial, ancho: 260),
                _conteo,
                ModuloEco.Campo("Unidades contadas", IconChar.Hashtag, _cantidadVerificada, ancho: 170),
                ModuloEco.JuntoACampos(ModuloEco.Boton(botonVerificar, IconChar.CircleCheck)),
            });
            ModuloEco.PaginaConTabla(paginaDepositos, tablaDepositos, paginacion, panelDepositos);
            ModuloEco.PaginaConTabla(paginaInteracciones, tablaInteracciones, ModuloEco.Mensaje(etiquetaInteracciones));
            ModuloEco.PaginaConTabla(paginaIncidencias, tablaIncidencias, ModuloEco.Mensaje(etiquetaIncidencias));
            var resultados = ModuloEco.Seccion("Resultados", llenar: true);
            resultados.MinimumSize = new Size(0, 460);
            resultados.Agregar(pestanas, llenar: true);
            ModuloEco.Fila(raiz, resultados, llenar: true);
            ModuloEco.Fila(raiz, ModuloEco.Mensaje(etiquetaEstado));

            Disposed += (_, _) => { panelSuperior.Dispose(); panelInferior.Dispose(); };
            ModuloEco.Terminar(this, raiz, new Size(640, 960));
        }
    }
}
