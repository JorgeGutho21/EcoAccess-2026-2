using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Logica.Vision35
{
    /// <summary>Máquina de estados obligatoria de la sesión de clasificación y apertura segura (Sección 9).</summary>
    public enum EstadoSesionDeposito
    {
        INACTIVO = 0,
        CAPTURANDO = 1,
        ANALIZANDO = 2,
        REQUIERE_CONFIRMACION = 3,
        CLASIFICADO = 4,
        PREPARADO_PARA_APERTURA = 5,
        ABRIENDO = 6,
        ESPERANDO_DEPOSITO = 7,
        CERRANDO = 8,
        FINALIZADO = 9,
        ERROR_SEGURO = 10,
    }

    /// <summary>Registro inmutable de una transición verificada de la máquina de estados.</summary>
    public sealed record TransicionEstado(
        DateTime FechaUtc,
        EstadoSesionDeposito Desde,
        EstadoSesionDeposito Hacia,
        string Motivo);

    /// <summary>Modalidad de apertura de canecas cuando se detectan 2 o 3 categorías (FASE 6).</summary>
    public enum ModoAperturaCanecas
    {
        /// <summary>Modo seguro predeterminado: abre y cierra una caneca a la vez guiando al ciudadano.</summary>
        SecuencialGuiada = 1,

        /// <summary>Modo opcional sujeto a validación supervisada del hardware y fuente de alimentación.</summary>
        SimultaneaSupervisada = 2,
    }

    /// <summary>Modo operativo de hardware (FASE 8): separación estricta entre simulación y hardware físico supervisado.</summary>
    public enum ModoOperacionHardware
    {
        Simulacion = 1,
        HardwareFisicoSupervisado = 2,
    }

    /// <summary>
    /// Resultado honesto por cada caneca del plan. Diferencia explícitamente comando enviado de apertura
    /// confirmada por el controlador (T029) y deja explícito si el hardware dispone o no de sensores físicos (FASE 8).
    /// </summary>
    public sealed record ResultadoCanecaSecuencial(
        CategoriaMaterial Caneca,
        int IdContenedor,
        string ClaveIdempotencia,
        bool ComandoEnviado,
        bool AperturaConfirmada,
        bool CierreConfirmado,
        bool EsSimulado,
        EstadoEjecucionApertura EstadoControlador,
        string Mensaje,
        bool TieneSensorFisico = false,
        bool ConfirmacionObservacionalRequerida = true);

    /// <summary>
    /// Máquina de estados determinista para el flujo de disposición. Rechaza cualquier transición inválida y mantiene
    /// trazabilidad completa para auditoría y pruebas automatizadas.
    /// </summary>
    public sealed class MaquinaEstadosDeposito
    {
        private static readonly Dictionary<EstadoSesionDeposito, HashSet<EstadoSesionDeposito>> TransicionesValidas = new()
        {
            [EstadoSesionDeposito.INACTIVO] = new()
            {
                EstadoSesionDeposito.CAPTURANDO,
                EstadoSesionDeposito.ERROR_SEGURO,
            },
            [EstadoSesionDeposito.CAPTURANDO] = new()
            {
                EstadoSesionDeposito.ANALIZANDO,
                EstadoSesionDeposito.INACTIVO,
                EstadoSesionDeposito.ERROR_SEGURO,
            },
            [EstadoSesionDeposito.ANALIZANDO] = new()
            {
                EstadoSesionDeposito.REQUIERE_CONFIRMACION,
                EstadoSesionDeposito.CLASIFICADO,
                EstadoSesionDeposito.FINALIZADO,
                EstadoSesionDeposito.INACTIVO,
                EstadoSesionDeposito.ERROR_SEGURO,
            },
            [EstadoSesionDeposito.REQUIERE_CONFIRMACION] = new()
            {
                EstadoSesionDeposito.CLASIFICADO,
                EstadoSesionDeposito.CAPTURANDO,
                EstadoSesionDeposito.FINALIZADO,
                EstadoSesionDeposito.INACTIVO,
                EstadoSesionDeposito.ERROR_SEGURO,
            },
            [EstadoSesionDeposito.CLASIFICADO] = new()
            {
                EstadoSesionDeposito.PREPARADO_PARA_APERTURA,
                EstadoSesionDeposito.CAPTURANDO,
                EstadoSesionDeposito.FINALIZADO,
                EstadoSesionDeposito.INACTIVO,
                EstadoSesionDeposito.ERROR_SEGURO,
            },
            [EstadoSesionDeposito.PREPARADO_PARA_APERTURA] = new()
            {
                EstadoSesionDeposito.ABRIENDO,
                EstadoSesionDeposito.FINALIZADO,
                EstadoSesionDeposito.INACTIVO,
                EstadoSesionDeposito.ERROR_SEGURO,
            },
            [EstadoSesionDeposito.ABRIENDO] = new()
            {
                EstadoSesionDeposito.ESPERANDO_DEPOSITO,
                EstadoSesionDeposito.CERRANDO,
                EstadoSesionDeposito.ERROR_SEGURO,
            },
            [EstadoSesionDeposito.ESPERANDO_DEPOSITO] = new()
            {
                EstadoSesionDeposito.CERRANDO,
                EstadoSesionDeposito.ERROR_SEGURO,
            },
            [EstadoSesionDeposito.CERRANDO] = new()
            {
                EstadoSesionDeposito.PREPARADO_PARA_APERTURA, // siguiente caneca en apertura secuencial guiada
                EstadoSesionDeposito.FINALIZADO,
                EstadoSesionDeposito.ERROR_SEGURO,
            },
            [EstadoSesionDeposito.FINALIZADO] = new()
            {
                EstadoSesionDeposito.INACTIVO,
                EstadoSesionDeposito.CAPTURANDO,
            },
            [EstadoSesionDeposito.ERROR_SEGURO] = new()
            {
                EstadoSesionDeposito.INACTIVO,
                EstadoSesionDeposito.CAPTURANDO,
            },
        };

        private readonly TimeProvider _reloj;
        private readonly List<TransicionEstado> _historial = new();

        public EstadoSesionDeposito EstadoActual { get; private set; } = EstadoSesionDeposito.INACTIVO;
        public IReadOnlyList<TransicionEstado> Historial => _historial;

        public MaquinaEstadosDeposito(TimeProvider? reloj = null)
        {
            _reloj = reloj ?? TimeProvider.System;
        }

        public bool PuedeTransicionarA(EstadoSesionDeposito destino) =>
            TransicionesValidas.TryGetValue(EstadoActual, out var permitidos) && permitidos.Contains(destino);

        public void Transicionar(EstadoSesionDeposito destino, string motivo)
        {
            if (!PuedeTransicionarA(destino))
            {
                throw new InvalidOperationException(
                    $"Transición de estado no permitida: {EstadoActual} → {destino} ({motivo}).");
            }

            var registro = new TransicionEstado(_reloj.GetUtcNow().UtcDateTime, EstadoActual, destino, motivo);
            EstadoActual = destino;
            _historial.Add(registro);
        }
    }

    /// <summary>
    /// Orquestador seguro de una sesión de clasificación multirresiduo y apertura secuencial guiada.
    /// Conecta la arquitectura completa:
    /// CAMARA → PROVEEDOR VISUAL → VALIDADOR JSON → MOTOR DE REGLAS C# → GESTOR DE SESIÓN → CONTROLADOR DE CANECAS → REGISTRO.
    /// </summary>
    public sealed class GestorSesionMultirresiduo
    {
        private static readonly TimeSpan VentanaAntiduplicado = TimeSpan.FromSeconds(30);

        private readonly IProveedorVisionMultirresiduo _proveedor;
        private readonly MotorReglasMultirresiduo _motor;
        private readonly IValidadorCalidadImagen? _validadorCalidad;
        private readonly IEjecutorAperturas? _ejecutor;
        private readonly TimeProvider _reloj;
        private readonly HashSet<string> _operacionesEjecutadas = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> _hashesRecientes = new(StringComparer.OrdinalIgnoreCase);
        private int _secuenciaOrden;

        public MaquinaEstadosDeposito Maquina { get; }
        public ResultadoAnalisisMultirresiduo? UltimoResultado { get; private set; }
        public IReadOnlyList<ResultadoCanecaSecuencial> UltimasAperturas { get; private set; } = Array.Empty<ResultadoCanecaSecuencial>();

        public GestorSesionMultirresiduo(
            IProveedorVisionMultirresiduo proveedor,
            MotorReglasMultirresiduo? motor = null,
            IEjecutorAperturas? ejecutor = null,
            IValidadorCalidadImagen? validadorCalidad = null,
            TimeProvider? reloj = null)
        {
            _proveedor = proveedor ?? throw new ArgumentNullException(nameof(proveedor));
            _motor = motor ?? new MotorReglasMultirresiduo();
            _ejecutor = ejecutor;
            _validadorCalidad = validadorCalidad;
            _reloj = reloj ?? TimeProvider.System;
            Maquina = new MaquinaEstadosDeposito(_reloj);
        }

        /// <summary>
        /// Ejecuta la captura, validación de imagen, observación visual y clasificación por reglas C#.
        /// Previene duplicación ante fotografías idénticas consecutivas del mismo depósito (T025).
        /// </summary>
        public async Task<ResultadoAnalisisMultirresiduo> AnalizarFotografiaAsync(byte[]? bytesImagen, CancellationToken ct = default)
        {
            if (Maquina.EstadoActual is EstadoSesionDeposito.FINALIZADO or EstadoSesionDeposito.ERROR_SEGURO)
                Maquina.Transicionar(EstadoSesionDeposito.INACTIVO, "Reinicio limpio para nueva captura");
            if (Maquina.EstadoActual != EstadoSesionDeposito.INACTIVO)
                throw new InvalidOperationException($"No se puede iniciar una captura en estado {Maquina.EstadoActual}.");

            Maquina.Transicionar(EstadoSesionDeposito.CAPTURANDO, "Inicio de recepción de imagen");

            if (!ValidadorJsonVision.ValidarFormatoImagen(bytesImagen, out _, out string? errorFormato))
            {
                Maquina.Transicionar(EstadoSesionDeposito.ERROR_SEGURO, errorFormato ?? "Imagen inválida");
                var errorRes = new ResultadoAnalisisMultirresiduo(
                    EstadoGlobalAnalisis.RequiereRepeticion,
                    Array.Empty<ObjetoClasificado35>(),
                    Array.Empty<CategoriaMaterial>(),
                    CombinacionCanecas.Ninguna,
                    0, false, false,
                    errorFormato ?? "Imagen corrupta o inválida. Por favor repite la toma.",
                    _proveedor.NombreProveedor, 0, errorFormato);
                UltimoResultado = errorRes;
                return errorRes;
            }

            string hash = Compat.ToHexString(Compat.Sha256(bytesImagen!));
            DateTime ahora = _reloj.GetUtcNow().UtcDateTime;

            // T025: dos fotografías consecutivas idénticas del mismo depósito no duplican registros ni aperturas.
            if (_hashesRecientes.TryGetValue(hash, out DateTime previo) && (ahora - previo) < VentanaAntiduplicado)
            {
                Maquina.Transicionar(EstadoSesionDeposito.INACTIVO, "Fotografía duplicada consecutiva bloqueada (T025)");
                var duplicado = new ResultadoAnalisisMultirresiduo(
                    EstadoGlobalAnalisis.SinResiduos,
                    Array.Empty<ObjetoClasificado35>(),
                    Array.Empty<CategoriaMaterial>(),
                    CombinacionCanecas.Ninguna,
                    0, false, false,
                    "Esta fotografía ya fue procesada recientemente para el mismo depósito. No se duplicarán registros ni aperturas.",
                    _proveedor.NombreProveedor, 0, "DUPLICADO_CONSECUTIVO", hash);
                UltimoResultado = duplicado;
                return duplicado;
            }

            if (_validadorCalidad is not null)
            {
                CalidadImagenVision calidadPrevia = _validadorCalidad.Evaluar(bytesImagen!, out string? detalleCalidad);
                if (calidadPrevia != CalidadImagenVision.Aceptable)
                {
                    Maquina.Transicionar(EstadoSesionDeposito.ERROR_SEGURO, detalleCalidad ?? "Calidad de imagen insuficiente");
                    var borrosa = new ResultadoAnalisisMultirresiduo(
                        EstadoGlobalAnalisis.RequiereRepeticion,
                        Array.Empty<ObjetoClasificado35>(),
                        Array.Empty<CategoriaMaterial>(),
                        CombinacionCanecas.Ninguna,
                        0, false, false,
                        detalleCalidad ?? "La fotografía está borrosa u oscura. Por favor repite la toma.",
                        _proveedor.NombreProveedor, 0, null, hash);
                    UltimoResultado = borrosa;
                    return borrosa;
                }
            }

            Maquina.Transicionar(EstadoSesionDeposito.ANALIZANDO, "Consultando servicio de visión desacoplado");

            RespuestaVisionEstructurada observacion;
            try
            {
                observacion = await _proveedor.ObservarAsync(bytesImagen!, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Maquina.Transicionar(EstadoSesionDeposito.INACTIVO, "Análisis cancelado por el usuario");
                var cancelado = new ResultadoAnalisisMultirresiduo(
                    EstadoGlobalAnalisis.Cancelado,
                    Array.Empty<ObjetoClasificado35>(),
                    Array.Empty<CategoriaMaterial>(),
                    CombinacionCanecas.Ninguna,
                    0, false, false,
                    "Análisis cancelado. No se abrió ninguna caneca.",
                    _proveedor.NombreProveedor, 0, "CANCELADO", hash);
                UltimoResultado = cancelado;
                return cancelado;
            }
            catch (Exception ex)
            {
                Maquina.Transicionar(EstadoSesionDeposito.ERROR_SEGURO, "Fallo del proveedor visual: " + ex.GetType().Name);
                var fallo = new ResultadoAnalisisMultirresiduo(
                    EstadoGlobalAnalisis.ModoSeguroError,
                    Array.Empty<ObjetoClasificado35>(),
                    Array.Empty<CategoriaMaterial>(),
                    CombinacionCanecas.Ninguna,
                    0, false, false,
                    "Servicio visual no disponible (modo seguro). No se abrirán canecas automáticamente; puedes usar selección manual.",
                    _proveedor.NombreProveedor, 0, ex.Message, hash);
                UltimoResultado = fallo;
                return fallo;
            }

            ResultadoAnalisisMultirresiduo resultado = _motor.Evaluar(observacion, hash);
            UltimoResultado = resultado;

            switch (resultado.EstadoGlobal)
            {
                case EstadoGlobalAnalisis.Clasificado:
                    _hashesRecientes[hash] = ahora;
                    Maquina.Transicionar(EstadoSesionDeposito.CLASIFICADO, $"Clasificado ({resultado.Combinacion})");
                    break;
                case EstadoGlobalAnalisis.RequiereConfirmacion:
                    Maquina.Transicionar(EstadoSesionDeposito.REQUIERE_CONFIRMACION, "Ambigüedad o condición pendiente de confirmación humana");
                    break;
                case EstadoGlobalAnalisis.SinResiduos:
                case EstadoGlobalAnalisis.NoIdentificado:
                    Maquina.Transicionar(EstadoSesionDeposito.FINALIZADO, $"Sin apertura ({resultado.EstadoGlobal})");
                    break;
                default:
                    Maquina.Transicionar(EstadoSesionDeposito.ERROR_SEGURO, resultado.ErrorTecnico ?? resultado.EstadoGlobal.ToString());
                    break;
            }

            return resultado;
        }

        /// <summary>Permite al ciudadano confirmar explícitamente las canecas cuando el estado es REQUIERE_CONFIRMACION.</summary>
        public ResultadoAnalisisMultirresiduo ConfirmarManualmente(IReadOnlyCollection<CategoriaMaterial> canecasConfirmadas, string motivo = "Confirmación manual del ciudadano")
        {
            if (Maquina.EstadoActual != EstadoSesionDeposito.REQUIERE_CONFIRMACION)
                throw new InvalidOperationException($"Solo se puede confirmar desde REQUIERE_CONFIRMACION (actual: {Maquina.EstadoActual}).");

            var unicas = canecasConfirmadas.Distinct().ToList();
            if (unicas.Count == 0)
            {
                Maquina.Transicionar(EstadoSesionDeposito.FINALIZADO, "Confirmación sin canecas");
                return UltimoResultado!;
            }

            var actualizado = (UltimoResultado ?? new ResultadoAnalisisMultirresiduo(
                EstadoGlobalAnalisis.Clasificado, Array.Empty<ObjetoClasificado35>(), unicas,
                MotorReglasMultirresiduo.CalcularCombinacion(unicas), unicas.Count, false, true,
                "Confirmado manualmente.", _proveedor.NombreProveedor, 0)) with
            {
                EstadoGlobal = EstadoGlobalAnalisis.Clasificado,
                CanecasRequeridas = unicas,
                Combinacion = MotorReglasMultirresiduo.CalcularCombinacion(unicas),
                RequiereConfirmacion = false,
                PuedeAbrirCanecas = true,
            };

            if (!string.IsNullOrWhiteSpace(actualizado.HashImagenSha256))
                _hashesRecientes[actualizado.HashImagenSha256!] = _reloj.GetUtcNow().UtcDateTime;

            UltimoResultado = actualizado;
            Maquina.Transicionar(EstadoSesionDeposito.CLASIFICADO, motivo);
            return actualizado;
        }

        /// <summary>T030: Cancela la operación de depósito en curso sin emitir ninguna nueva apertura.</summary>
        public void CancelarDeposito(string motivo = "Cancelado por el usuario")
        {
            if (Maquina.EstadoActual is EstadoSesionDeposito.CAPTURANDO or EstadoSesionDeposito.ANALIZANDO
                or EstadoSesionDeposito.REQUIERE_CONFIRMACION or EstadoSesionDeposito.CLASIFICADO
                or EstadoSesionDeposito.PREPARADO_PARA_APERTURA)
            {
                Maquina.Transicionar(EstadoSesionDeposito.INACTIVO, motivo);
            }
        }

        /// <summary>
        /// Ejecuta la apertura de 1, 2 o 3 canecas en modo seguro predeterminado (<see cref="ModoAperturaCanecas.SecuencialGuiada"/>)
        /// o en modo simultáneo supervisado opcional. Nunca abre si el estado no es CLASIFICADO, si la operación ya fue
        /// ejecutada (antiduplicado) o si el controlador está desconectado (T029). Cuando no existen sensores físicos,
        /// registra honestamente que se envió el comando y que la confirmación física requiere observación (FASE 8).
        /// </summary>
        public async Task<IReadOnlyList<ResultadoCanecaSecuencial>> EjecutarAperturaSecuencialAsync(
            string idOperacion,
            IReadOnlyList<CanecaEstacion> estacion,
            bool permitirSoloSimulador = true,
            CancellationToken ct = default,
            ModoAperturaCanecas modoApertura = ModoAperturaCanecas.SecuencialGuiada,
            bool hardwareConSensoresFisicos = false)
        {
            if (string.IsNullOrWhiteSpace(idOperacion))
                throw new ArgumentException("Se requiere un identificador de operación.", nameof(idOperacion));
            if (Maquina.EstadoActual != EstadoSesionDeposito.CLASIFICADO || UltimoResultado is null || !UltimoResultado.PuedeAbrirCanecas)
                throw new InvalidOperationException($"No se autoriza apertura en estado {Maquina.EstadoActual}.");

            if (!_operacionesEjecutadas.Add(idOperacion))
                return Array.Empty<ResultadoCanecaSecuencial>(); // Idempotencia: 0 aperturas duplicadas por reintento.

            if (_ejecutor is null)
            {
                Maquina.Transicionar(EstadoSesionDeposito.ERROR_SEGURO, "Controlador de canecas no configurado");
                return Array.Empty<ResultadoCanecaSecuencial>();
            }

            if (permitirSoloSimulador && !_ejecutor.EsSimulado)
            {
                Maquina.Transicionar(EstadoSesionDeposito.ERROR_SEGURO, "Bloqueo de seguridad: apertura física requiere modo supervisado explícito");
                throw new InvalidOperationException("Apertura física bloqueada sin autorización supervisada explícita.");
            }

            var resultados = new List<ResultadoCanecaSecuencial>();
            var canecas = UltimoResultado.CanecasRequeridas;

            if (canecas.Count == 0)
            {
                Maquina.Transicionar(EstadoSesionDeposito.FINALIZADO, "Sin canecas requeridas");
                UltimasAperturas = resultados;
                return resultados;
            }

            string etiquetaModo = modoApertura == ModoAperturaCanecas.SimultaneaSupervisada
                ? "Plan simultáneo supervisado"
                : "Plan secuencial guiado";
            Maquina.Transicionar(EstadoSesionDeposito.PREPARADO_PARA_APERTURA, $"{etiquetaModo} de {canecas.Count} caneca(s)");

            if (modoApertura == ModoAperturaCanecas.SimultaneaSupervisada && canecas.Count > 1)
            {
                // En modo simultáneo se despachan todas las canecas habilitadas en paralelo (solo bajo validación supervisada).
                Maquina.Transicionar(EstadoSesionDeposito.ABRIENDO, $"Enviando comandos simultáneos a {canecas.Count} canecas");
                var tareas = canecas.Select(async categoria =>
                {
                    CanecaEstacion? caneca = estacion.FirstOrDefault(c => c.Categoria == categoria && c.EnServicio);
                    string clave = $"{idOperacion}:{MotorReglasMultirresiduo.NombreCortoCaneca(categoria)}";
                    if (caneca is null)
                    {
                        return new ResultadoCanecaSecuencial(
                            categoria, 0, clave, false, false, false, _ejecutor.EsSimulado,
                            EstadoEjecucionApertura.Rechazada,
                            $"Caneca {MotorReglasMultirresiduo.NombreCortoCaneca(categoria)} fuera de servicio o inexistente.",
                            hardwareConSensoresFisicos, !hardwareConSensoresFisicos);
                    }
                    int idOrden = Interlocked.Increment(ref _secuenciaOrden);
                    var comando = new ComandoApertura(idOrden, caneca.IdContenedor, clave, _reloj.GetUtcNow().UtcDateTime.Add(ServicioInteracciones.VigenciaOrden));
                    ResultadoEjecucionApertura ejecucion;
                    try { ejecucion = await _ejecutor.AbrirAsync(comando, ct).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch (Exception) { ejecucion = new ResultadoEjecucionApertura(EstadoEjecucionApertura.SinRespuesta, Array.Empty<TipoEventoDispositivo>()); }
                    return ConstruirResultadoCaneca(categoria, caneca.IdContenedor, clave, ejecucion, _ejecutor.EsSimulado, hardwareConSensoresFisicos);
                }).ToArray();

                resultados.AddRange(await Task.WhenAll(tareas).ConfigureAwait(false));
                bool algunaOk = resultados.Any(r => r.AperturaConfirmada);
                if (algunaOk)
                {
                    Maquina.Transicionar(EstadoSesionDeposito.ESPERANDO_DEPOSITO, "Apertura simultánea en curso");
                    Maquina.Transicionar(EstadoSesionDeposito.CERRANDO, "Cierre de canecas simultáneas");
                }
                else
                {
                    Maquina.Transicionar(EstadoSesionDeposito.CERRANDO, "Sin confirmación de apertura en modo simultáneo");
                }

                Maquina.Transicionar(algunaOk ? EstadoSesionDeposito.FINALIZADO : EstadoSesionDeposito.ERROR_SEGURO,
                    algunaOk ? "Ciclo simultáneo supervisado completado" : "Fallo de hardware en ciclo simultáneo");
                UltimasAperturas = resultados;
                return resultados;
            }

            for (int i = 0; i < canecas.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                CategoriaMaterial categoria = canecas[i];
                CanecaEstacion? caneca = estacion.FirstOrDefault(c => c.Categoria == categoria && c.EnServicio);
                string clave = $"{idOperacion}:{MotorReglasMultirresiduo.NombreCortoCaneca(categoria)}";

                if (caneca is null)
                {
                    resultados.Add(new ResultadoCanecaSecuencial(
                        categoria, 0, clave, false, false, false, _ejecutor.EsSimulado,
                        EstadoEjecucionApertura.Rechazada,
                        $"Caneca {MotorReglasMultirresiduo.NombreCortoCaneca(categoria)} fuera de servicio o inexistente.",
                        hardwareConSensoresFisicos, !hardwareConSensoresFisicos));
                    continue;
                }

                if (Maquina.EstadoActual == EstadoSesionDeposito.CERRANDO)
                    Maquina.Transicionar(EstadoSesionDeposito.PREPARADO_PARA_APERTURA, $"Siguiente caneca: {MotorReglasMultirresiduo.NombreCortoCaneca(categoria)}");

                Maquina.Transicionar(EstadoSesionDeposito.ABRIENDO, $"Enviando comando a caneca {caneca.Nombre}");
                int idOrden = Interlocked.Increment(ref _secuenciaOrden);
                var comando = new ComandoApertura(idOrden, caneca.IdContenedor, clave, _reloj.GetUtcNow().UtcDateTime.Add(ServicioInteracciones.VigenciaOrden));

                ResultadoEjecucionApertura ejecucion;
                try
                {
                    ejecucion = await _ejecutor.AbrirAsync(comando, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    Maquina.Transicionar(EstadoSesionDeposito.ERROR_SEGURO, "Cancelado durante apertura");
                    throw;
                }
                catch (Exception)
                {
                    ejecucion = new ResultadoEjecucionApertura(EstadoEjecucionApertura.SinRespuesta, Array.Empty<TipoEventoDispositivo>());
                }

                ResultadoCanecaSecuencial resCaneca = ConstruirResultadoCaneca(categoria, caneca.IdContenedor, clave, ejecucion, _ejecutor.EsSimulado, hardwareConSensoresFisicos);

                if (resCaneca.AperturaConfirmada)
                {
                    Maquina.Transicionar(EstadoSesionDeposito.ESPERANDO_DEPOSITO, "Apertura reportada por controlador");
                    Maquina.Transicionar(EstadoSesionDeposito.CERRANDO, resCaneca.CierreConfirmado ? "Cierre reportado" : "Finalizando ventana de cierre");
                }
                else
                {
                    // T029: hardware desconectado o sin respuesta -> NO simular apertura exitosa.
                    Maquina.Transicionar(EstadoSesionDeposito.CERRANDO, $"Sin confirmación de apertura ({ejecucion.Estado})");
                }

                resultados.Add(resCaneca);
            }

            if (Maquina.EstadoActual is EstadoSesionDeposito.CERRANDO or EstadoSesionDeposito.PREPARADO_PARA_APERTURA)
            {
                bool algunaDesconectada = resultados.Any(r => r.EstadoControlador is EstadoEjecucionApertura.Desconectada or EstadoEjecucionApertura.SinRespuesta or EstadoEjecucionApertura.FalloApertura);
                Maquina.Transicionar(
                    algunaDesconectada && resultados.All(r => !r.AperturaConfirmada) ? EstadoSesionDeposito.ERROR_SEGURO : EstadoSesionDeposito.FINALIZADO,
                    algunaDesconectada ? "Ciclo terminado con incidencia de hardware" : "Ciclo de apertura secuencial completado");
            }

            UltimasAperturas = resultados;
            return resultados;
        }

        private static ResultadoCanecaSecuencial ConstruirResultadoCaneca(
            CategoriaMaterial categoria,
            int idContenedor,
            string clave,
            ResultadoEjecucionApertura ejecucion,
            bool esSimulado,
            bool conSensoresFisicos)
        {
            bool aperturaConfirmada = ejecucion.Estado is EstadoEjecucionApertura.Completada or EstadoEjecucionApertura.FalloCierre
                || ejecucion.Eventos.Contains(TipoEventoDispositivo.Apertura);
            bool cierreConfirmado = ejecucion.Estado == EstadoEjecucionApertura.Completada
                || ejecucion.Eventos.Contains(TipoEventoDispositivo.Cierre)
                || ejecucion.Eventos.Contains(TipoEventoDispositivo.CierreSinSensor);

            string msg = ejecucion.Estado switch
            {
                EstadoEjecucionApertura.Completada when esSimulado => "Apertura y cierre ejecutados en controlador SIMULADO (MODO 1 — sin movimiento físico).",
                EstadoEjecucionApertura.Completada when conSensoresFisicos => "Apertura y cierre confirmados mediante sensores físicos (MODO 2).",
                EstadoEjecucionApertura.Completada => "Comando enviado y acuse recibido del ESP32 sin sensores de posición: confirmación física requiere observación directa.",
                EstadoEjecucionApertura.Desconectada => "Hardware desconectado: comando no confirmado (NO se registró apertura exitosa).",
                EstadoEjecucionApertura.SinRespuesta => "Controlador sin respuesta (timeout): operación incierta.",
                EstadoEjecucionApertura.Duplicada => "Comando duplicado bloqueado por idempotencia.",
                EstadoEjecucionApertura.FalloApertura => "El controlador reportó fallo de apertura.",
                EstadoEjecucionApertura.FalloCierre => "Apertura reportada pero el controlador informó fallo al cerrar (revisión física requerida).",
                _ => "Orden rechazada por el controlador.",
            };

            return new ResultadoCanecaSecuencial(
                categoria, idContenedor, clave, ComandoEnviado: true,
                AperturaConfirmada: aperturaConfirmada, CierreConfirmado: cierreConfirmado,
                EsSimulado: esSimulado, EstadoControlador: ejecucion.Estado, Mensaje: msg,
                TieneSensorFisico: conSensoresFisicos,
                ConfirmacionObservacionalRequerida: esSimulado || !conSensoresFisicos);
        }
    }
}
