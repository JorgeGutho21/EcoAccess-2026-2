using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Usuarios: consultar, registrar, activar y desactivar cuentas, asignar o quitar roles y dar la primera
    /// contraseña a operarios y administradores. Las reglas (correo único, no quedarse sin administrador activo,
    /// contraseña solo si la cuenta no tiene) las deciden <see cref="ServicioUsuarios"/> y
    /// <see cref="ServicioAutenticacion"/>; aquí solo se recogen los datos y se muestra la respuesta.
    /// </summary>
    public partial class FormUsuarios : Form
    {
        private readonly ServicioUsuarios _usuarios;
        private readonly ServicioAutenticacion _autenticacion;
        private readonly int _idActor;
        private bool _ocupado;
        private bool _escrituraActiva;
        private bool _cambioConfirmado;

        public FormUsuarios(ServicioUsuarios usuarios, ServicioAutenticacion autenticacion, int idActor)
        {
            _usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
            _autenticacion = autenticacion ?? throw new ArgumentNullException(nameof(autenticacion));
            _idActor = idActor;
            InitializeComponent();
            textoNombre.MaxLength = Usuario.LongitudMaximaNombre;
            textoCorreo.MaxLength = Usuario.LongitudMaximaCorreo;
            textoContrasena.MaxLength = CredencialAcceso.LongitudMaximaContrasena;
            textoConfirmacion.MaxLength = CredencialAcceso.LongitudMaximaContrasena;
            etiquetaContrasena.Text = $"Contraseña ({CredencialAcceso.LongitudMinimaContrasena} a {CredencialAcceso.LongitudMaximaContrasena} caracteres)";
            // Listas distintas: dos combos con la misma lista compartirían la selección.
            comboRol.DisplayMember = nameof(OpcionRol.Texto);
            comboRol.DataSource = OpcionRol.Todas();
            comboRolInicial.DisplayMember = nameof(OpcionRol.Texto);
            comboRolInicial.DataSource = OpcionRol.Todas();
            ConstruirDisenoUsuarios();
            FormClosing += (_, e) =>
            {
                if (_ocupado && _escrituraActiva) e.Cancel = true;
                else _consultaUsuarios?.Cancel();
            };
        }

        private async void FormUsuarios_Load(object? sender, EventArgs e) => await EjecutarAsync(CargarAsync, lectura: true);

        private async void BotonActualizar_Click(object? sender, EventArgs e) => await EjecutarAsync(CargarAsync, lectura: true);

        private async void BotonRegistrar_Click(object? sender, EventArgs e) => await EjecutarAsync(async () =>
        {
            _erroresUsuarios.Clear();
            if (Compat.IsNullOrWhiteSpace(textoNombre.Text))
            {
                ErrorCampoUsuarios(textoNombre, "Escribe el nombre completo.");
                return;
            }
            if (Compat.IsNullOrWhiteSpace(textoCorreo.Text))
            {
                ErrorCampoUsuarios(textoCorreo, "Escribe el correo electrónico.");
                return;
            }
            if (comboRolInicial.SelectedItem is not OpcionRol rol)
            {
                MostrarEstado("Error: seleccione el rol inicial.", esError: true);
                return;
            }

            Resultado<int> resultado = await _usuarios.RegistrarAsync(_idActor, textoNombre.Text, textoCorreo.Text, rol.Rol);
            if (Informar(resultado, "Usuario registrado."))
            {
                textoNombre.Clear();
                textoCorreo.Clear();
                await CargarAsync(idSeleccionar: resultado.Valor);
                if (_datosAutorizados)
                    MostrarEstado("Cuenta guardada en Oracle. Si queda fuera de esta página o filtro, búscala por su correo. El personal requiere establecer su contraseña inicial.", false);
            }
        });

        private async void BotonActivar_Click(object? sender, EventArgs e) =>
            await OperarSobreSeleccionAsync(id => _usuarios.ActivarAsync(_idActor, id), "Usuario activo.");

        private async void BotonDesactivar_Click(object? sender, EventArgs e) =>
            await OperarSobreSeleccionAsync(id => _usuarios.DesactivarAsync(_idActor, id), "Usuario inactivo.", "Desactivar la cuenta");

        private async void BotonAsignarRol_Click(object? sender, EventArgs e)
        {
            if (comboRol.SelectedItem is OpcionRol rol)
            {
                await OperarSobreSeleccionAsync(id => _usuarios.AsignarRolAsync(_idActor, id, rol.Rol), $"El usuario tiene el rol {rol.Texto}.", $"Asignar el rol {rol.Texto}");
            }
        }

        private async void BotonQuitarRol_Click(object? sender, EventArgs e)
        {
            if (comboRol.SelectedItem is OpcionRol rol)
            {
                await OperarSobreSeleccionAsync(id => _usuarios.QuitarRolAsync(_idActor, id, rol.Rol), $"El usuario no tiene el rol {rol.Texto}.", $"Quitar el rol {rol.Texto}");
            }
        }

        private async void BotonEstablecerContrasena_Click(object? sender, EventArgs e) => await EjecutarAsync(async () =>
        {
            // Los errores de selección o de confirmación no borran lo escrito; si se llama al servicio, las contraseñas
            // salen de los controles antes, para que no queden en ellos aunque falle la conexión.
            if (tablaUsuarios.CurrentRow?.DataBoundItem is not FilaUsuario fila)
            {
                MostrarEstado("Error: seleccione un usuario de la tabla.", esError: true);
                return;
            }

            if (Compat.IsNullOrEmpty(textoContrasena.Text))
            {
                ErrorCampoUsuarios(textoContrasena, "Escribe la contraseña inicial.");
                return;
            }
            if (textoContrasena.Text != textoConfirmacion.Text)
            {
                ErrorCampoUsuarios(textoConfirmacion, "La contraseña y su confirmación no coinciden.");
                return;
            }
            if (MessageBox.Show(this, $"Establecer contraseña inicial para:\n{fila.Nombre}\n{fila.Correo}\n\n¿Deseas continuar?",
                "Confirmar acceso inicial", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                MostrarEstado("Operación cancelada, no se guardó la contraseña.", false);
                return;
            }

            string contrasena = textoContrasena.Text;
            textoContrasena.Clear();
            textoConfirmacion.Clear();
            Informar(await _autenticacion.EstablecerContrasenaInicialAsync(_idActor, fila.Id, contrasena),
                $"Contraseña inicial guardada para {fila.Nombre}. Entréguesela por un medio seguro.");
        });

        private async Task OperarSobreSeleccionAsync(Func<int, Task<Resultado>> operacion, string mensajeExito, string? confirmar = null) => await EjecutarAsync(async () =>
        {
            if (tablaUsuarios.CurrentRow?.DataBoundItem is not FilaUsuario fila)
            {
                MostrarEstado("Error: seleccione un usuario de la tabla.", esError: true);
                return;
            }

            if (confirmar is not null && MessageBox.Show(this,
                $"{confirmar}:\n{fila.Nombre}\n{fila.Correo}\n\n¿Deseas continuar?", "Confirmar cambio de cuenta",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                MostrarEstado("Operación cancelada, no se guardaron cambios.", false);
                return;
            }
            if (Informar(await operacion(fila.Id), mensajeExito))
            {
                await CargarAsync();
                if (_datosAutorizados) MostrarEstado(mensajeExito, false);
            }
        });

        private async Task CargarAsync() => await CargarAsync(idSeleccionar: null);

        private async Task CargarAsync(int? idSeleccionar)
        {
            int? idSeleccionado = idSeleccionar ?? (tablaUsuarios.CurrentRow?.DataBoundItem as FilaUsuario)?.Id;
            LimpiarConsultaUsuarios();
            using var limiteRecarga = _consultaUsuarios is null ? new CancellationTokenSource(TimeSpan.FromSeconds(20)) : null;
            if (limiteRecarga is not null)
            {
                // La escritura ya terminó: esta fase es solo lectura y puede cancelarse o cerrarse.
                _escrituraActiva = false;
                _consultaUsuarios = limiteRecarga;
                _cancelar.Visible = _cancelar.Enabled = true;
            }
            Resultado<PaginaUsuarios> resultado;
            try
            {
                resultado = await _usuarios.ConsultarPaginaAsync(_idActor, CapturarFiltroUsuarios(), _consultaUsuarios!.Token);
            }
            finally
            {
                if (limiteRecarga is not null) _consultaUsuarios = null;
            }
            if (IsDisposed) return;
            if (!resultado.Exitoso)
            {
                Informar(resultado, "");
                if (_cambioConfirmado)
                    MostrarEstado("El cambio se guardó. La consulta posterior fue rechazada; no se muestran datos de cuentas. " + resultado.Mensaje, true);
                return;
            }

            IReadOnlyList<Usuario> usuarios = resultado.Valor.Filas;
            _datosAutorizados = true;
            _haySiguiente = resultado.Valor.HaySiguiente;
            tablaUsuarios.DataSource = usuarios.Select(FilaUsuario.Desde).ToList();
            Columna(nameof(FilaUsuario.Id)).Visible = false;
            Columna(nameof(FilaUsuario.Correo)).HeaderText = "Correo electrónico";
            Columna(nameof(FilaUsuario.Registro)).HeaderText = "Fecha de registro";
            foreach (DataGridViewColumn columna in tablaUsuarios.Columns)
            {
                columna.SortMode = DataGridViewColumnSortMode.NotSortable;
                columna.Width = TemaEcoAccess.Px(this, columna.Name is nameof(FilaUsuario.Correo) or nameof(FilaUsuario.Roles) ? 230 : 155);
            }
            SeleccionGrid.Restaurar(tablaUsuarios, (FilaUsuario f) => f.Id == idSeleccionado, nameof(FilaUsuario.Nombre));
            _paginaTexto.Text = $"Página {_paginaActual} · {usuarios.Count} cuentas · hasta 25 por página";
            MostrarEstado(usuarios.Count == 0 ? "No hay cuentas que coincidan con los filtros aplicados." : "Consulta autorizada en Oracle · orden por nombre e identificador.", false);
            ActualizarSeleccionUsuarios();
        }

        private DataGridViewColumn Columna(string nombre) =>
            tablaUsuarios.Columns[nombre] ?? throw new InvalidOperationException($"Falta la columna {nombre}.");

        // Igual que en FormMateriales: bloquea la ventana durante la operación y no muestra el texto del proveedor.
        private async Task EjecutarAsync(Func<Task> accion, bool lectura = false)
        {
            if (_ocupado)
            {
                return;
            }

            _ocupado = true;
            _escrituraActiva = !lectura;
            _cambioConfirmado = false;
            UseWaitCursor = true;
            panelAcciones.Enabled = false;
            tablaUsuarios.Enabled = false;
            _paginacion.Enabled = false;
            foreach (Control control in _filtros.Controls) control.Enabled = false;
            _erroresUsuarios.Clear();
            MostrarEstado(lectura ? "Consultando cuentas en Oracle…" : "Procesando la operación…", false);
            using var consulta = lectura ? new CancellationTokenSource(TimeSpan.FromSeconds(20)) : null;
            _consultaUsuarios = consulta;
            _cancelar.Visible = lectura;
            _cancelar.Enabled = lectura;
            try
            {
                await accion();
            }
            catch (OperationCanceledException) when (lectura || !_escrituraActiva)
            {
                if (!IsDisposed)
                {
                    LimpiarConsultaUsuarios();
                    MostrarEstado(_cambioConfirmado
                        ? "El cambio se guardó. La consulta posterior se canceló o agotó el tiempo; vuelve a buscar."
                        : "Consulta cancelada o tiempo de espera agotado. Puedes volver a buscar.", true);
                }
            }
            catch (Exception)
            {
                if (!IsDisposed)
                {
                    LimpiarConsultaUsuarios();
                    MostrarEstado(_cambioConfirmado ? "El cambio se guardó, pero no se pudo actualizar la tabla. Vuelve a buscar."
                        : lectura
                        ? "No se pudo consultar Oracle. Revisa la conexión y vuelve a buscar."
                        : "No se pudo confirmar el resultado de la operación. Consulta antes de repetirla para evitar duplicados.", true);
                }
            }
            finally
            {
                _ocupado = false;
                _escrituraActiva = false;
                _consultaUsuarios = null;
                if (!IsDisposed)
                {
                    panelAcciones.Enabled = _datosAutorizados;
                    tablaUsuarios.Enabled = true;
                    _paginacion.Enabled = true;
                    foreach (Control control in _filtros.Controls) control.Enabled = true;
                    _cancelar.Visible = false;
                    UseWaitCursor = false;
                    ActualizarSeleccionUsuarios();
                    _focoPendiente?.Focus();
                    _focoPendiente = null;
                }
            }
        }

        private bool Informar(Resultado resultado, string mensajeExito)
        {
            if (resultado.Exitoso && _escrituraActiva) _cambioConfirmado = true;
            if (resultado.Motivo is MotivoRechazo.SinPermiso or MotivoRechazo.ActorInactivo or MotivoRechazo.ActorNoEncontrado)
                LimpiarConsultaUsuarios();
            if (!resultado.Exitoso && resultado.Motivo is MotivoRechazo.DatosInvalidos or MotivoRechazo.Duplicado)
            {
                Control? campo = resultado.Motivo == MotivoRechazo.Duplicado || resultado.Mensaje?.StartsWith("correo", StringComparison.OrdinalIgnoreCase) == true ? textoCorreo
                    : resultado.Mensaje?.StartsWith("nombre", StringComparison.OrdinalIgnoreCase) == true ? textoNombre : null;
                if (campo is not null) { _erroresUsuarios.SetError(campo, resultado.Mensaje); _focoPendiente = campo; }
            }
            MostrarEstado(resultado.Exitoso ? mensajeExito : $"Error: {resultado.Mensaje ?? "no se pudo completar la operación."}", esError: !resultado.Exitoso);
            return resultado.Exitoso;
        }

        private void ErrorCampoUsuarios(Control campo, string mensaje)
        {
            _erroresUsuarios.SetError(campo, mensaje);
            _focoPendiente = campo;
            MostrarEstado(mensaje, true);
        }

        private void MostrarEstado(string mensaje, bool esError)
        {
            etiquetaEstado.ForeColor = esError ? TemaEcoAccess.Error : TemaEcoAccess.VerdeOscuro;
            etiquetaEstado.Text = mensaje;
        }

        private static string TextoRol(CodigoRol rol) => rol switch
        {
            CodigoRol.Ciudadano => "Ciudadano",
            CodigoRol.Operario => "Operario",
            CodigoRol.Administrador => AreasAcceso.NombreRolCompleto(CodigoRol.Administrador),
            _ => rol.ToString(),
        };

        private sealed record OpcionRol(CodigoRol Rol, string Texto)
        {
            public static List<OpcionRol> Todas() =>
                new[] { CodigoRol.Ciudadano, CodigoRol.Operario, CodigoRol.Administrador }.Select(r => new OpcionRol(r, TextoRol(r))).ToList();
        }

        private sealed record FilaUsuario(int Id, string Nombre, string Correo, string Roles, string Estado, string Consentimiento, string Registro)
        {
            public static FilaUsuario Desde(Usuario u) => new(
                u.Id,
                u.Nombre,
                u.CorreoElectronico,
                string.Join(", ", u.Roles.Order().Select(TextoRol)),
                u.Activo ? "Activo" : "Inactivo",
                u.TieneConsentimiento ? $"Sí ({u.VersionConsentimiento})" : "No",
                HoraColombia.DesdeUtc(u.FechaRegistroUtc).ToString("yyyy-MM-dd HH:mm") + " (COL)");
        }
    }
}
