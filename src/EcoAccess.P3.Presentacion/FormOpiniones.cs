using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Valoración voluntaria y consulta agregada administrativa. Los permisos y límites viven en la lógica.</summary>
    public sealed class FormOpiniones : Form
    {
        private readonly CancellationTokenSource _cierre = new();
        private readonly string _sesionInvitada;

        public FormOpiniones(ServicioOpiniones servicio, int? actor, bool administracion = false, string? sesionInvitada = null)
        {
            _sesionInvitada = sesionInvitada ?? Guid.NewGuid().ToString("N");
            Text = administracion ? "Opiniones · administración" : "Tu experiencia con EcoAccess";
            BackColor = TemaEcoAccess.Fondo;
            ForeColor = TemaEcoAccess.Texto;
            Font = TemaEcoAccess.Cuerpo;
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(740, 570);
            MinimumSize = new Size(540, 460);
            var columna = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(24)
            };
            Controls.Add(columna);
            columna.Controls.Add(new Label { Text = Text, Font = TemaEcoAccess.TituloSeccion, AutoSize = true });
            var estado = new Label { AutoSize = true, MaximumSize = new Size(640, 0), ForeColor = TemaEcoAccess.Suave };
            if (administracion)
            {
                var tabla = new DataGridView
                {
                    Width = 650,
                    Height = 350,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    RowHeadersVisible = false
                };
                columna.Controls.Add(new Label { Text = "Últimos 30 días UTC · solo agregados, sin comentarios ni identificadores", AutoSize = true });
                columna.Controls.Add(tabla);
                columna.Controls.Add(estado);
                Shown += async (_, _) =>
                {
                    try
                    {
                        DateTime hasta = DateTime.UtcNow.Date.AddDays(1);
                        var resultado = await servicio.ConsultarTendenciasAsync(actor ?? 0, hasta.AddDays(-30), hasta, _cierre.Token);
                        if (IsDisposed) return;
                        if (resultado.Exitoso)
                        {
                            tabla.DataSource = resultado.Valor.ToList();
                            estado.Text = $"Período: {resultado.Valor.Sum(f => f.Cantidad):N0} valoraciones · {resultado.Valor.Sum(f => f.Invitados):N0} de invitados. La tabla muestra promedios diarios.";
                        }
                        else estado.Text = resultado.Mensaje;
                    }
                    catch (OperationCanceledException) when (_cierre.IsCancellationRequested) { }
                    catch (Exception) { if (!IsDisposed) estado.Text = "No pudimos consultar las valoraciones. Intenta de nuevo."; }
                };
                return;
            }
            columna.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(640, 0),
                Text =
                "Tu participación es opcional y no cambia tus ecopuntos. Se admite una valoración diaria por cuenta o sesión de invitado. No incluyas datos personales."
            });
            var estrellas = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, AccessibleName = "Calificación de 1 a 5 estrellas" };
            estrellas.Items.AddRange(Enumerable.Range(1, 5).Select(n => (object)$"{n} · {new string('★', n)}").ToArray());
            estrellas.SelectedIndex = 4;
            columna.Controls.Add(estrellas);
            var comentario = new TextBox { Width = 640, MaxLength = 500, AccessibleName = "Comentario opcional, máximo 500 caracteres", Visible = actor is not null };
            if (actor is not null) columna.Controls.Add(new Label { Text = "Comentario opcional", AutoSize = true });
            columna.Controls.Add(comentario);
            var consentimiento = new CheckBox
            {
                AutoSize = true,
                MaximumSize = new Size(640, 0),
                Text = actor is null
                ? "Acepto guardar mis estrellas con un identificador seudónimo de esta sesión, sin comentario."
                : "Acepto guardar esta valoración vinculada a mi cuenta para mejorar EcoAccess."
            };
            columna.Controls.Add(consentimiento);
            var enviar = new Button { Text = "Enviar opinión", AutoSize = true };
            TemaEcoAccess.EstilizarBoton(enviar);
            columna.Controls.Add(enviar);
            columna.Controls.Add(estado);
            enviar.Click += async (_, _) =>
            {
                enviar.Enabled = false;
                try
                {
                    var resultado = actor is { } ciudadano
                        ? await servicio.RegistrarCiudadanoAsync(ciudadano, estrellas.SelectedIndex + 1, comentario.Text, consentimiento.Checked, _cierre.Token)
                        : await servicio.RegistrarInvitadoAsync(_sesionInvitada, estrellas.SelectedIndex + 1, consentimiento.Checked, _cierre.Token);
                    if (IsDisposed) return;
                    estado.Text = resultado.Exitoso ? "Gracias. Recibimos tu valoración." : resultado.Mensaje;
                    enviar.Enabled = !resultado.Exitoso;
                }
                catch (OperationCanceledException) when (_cierre.IsCancellationRequested) { }
                catch (Exception) { if (!IsDisposed) { estado.Text = "No pudimos guardar tu opinión. Puedes reintentar sin duplicarla."; enviar.Enabled = true; } }
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed) { _cierre.Cancel(); _cierre.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
