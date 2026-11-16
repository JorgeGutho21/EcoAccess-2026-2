using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Datos.Simulacion
{
    /// <summary>Lo que hizo una ejecución del generador. Repetirla con el mismo lote da todo en «existentes».</summary>
    public sealed record ResultadoLoteSimulacion(string IdLote, int CiudadanosNuevos, int RegistrosNuevos, int Existentes,
        int DeclaradosSinSensor, int DetectadosPorSensor, int Verificados, IReadOnlyList<string> Rechazos);

    /// <summary>
    /// Fase 8: crea un lote de SIMULACIÓN con 50 ciudadanos ficticios y 100 depósitos sintéticos usando los mismos
    /// servicios que el bot (control de acceso, ventana, idempotencia, estimación de peso), con un reloj propio para
    /// repartirlos en los últimos 30 días.
    /// </summary>
    /// <remarks>
    /// Cada fila queda en REGISTRO_SIMULACION (V025) con una clave estable, así que repetir la ejecución no duplica nada.
    /// Todo se crea con canal Simulador (3) en una estación «SIMULACIÓN», que las vistas de informes ya excluyen por
    /// defecto. Los ciudadanos usan el dominio reservado <c>.invalid</c> y no tienen contraseña ni Telegram: no pueden
    /// iniciar sesión. Un depósito sintético no es un depósito físico y su peso es siempre estimado.
    /// </remarks>
    public sealed class GeneradorLoteSimulacion
    {
        public const int Ciudadanos = 50;
        public const int Registros = 100;
        public const string DominioCorreo = "simulacion.ecoaccess.invalid";

        private static readonly (string Codigo, CategoriaMaterial Categoria, int Peso)[] Mezcla =
        {
            ("PET", CategoriaMaterial.Aprovechable, 22), ("LATA_ALUMINIO", CategoriaMaterial.Aprovechable, 14),
            ("CARTON", CategoriaMaterial.Aprovechable, 12), ("PAPEL", CategoriaMaterial.Aprovechable, 10),
            ("VIDRIO", CategoriaMaterial.Aprovechable, 6), ("MULTICAPA", CategoriaMaterial.Aprovechable, 6),
            ("METAL", CategoriaMaterial.Aprovechable, 4), ("RESTOS_ALIMENTOS", CategoriaMaterial.OrganicoAprovechable, 12),
            ("SERVILLETA_USADA", CategoriaMaterial.NoAprovechable, 8), ("PLASTICO_FLEXIBLE", CategoriaMaterial.NoAprovechable, 6),
        };

        private sealed class RelojSimulacion : TimeProvider
        {
            public DateTimeOffset Ahora { get; set; }
            public override DateTimeOffset GetUtcNow() => Ahora;
        }

        private readonly FabricaConexionesOracle _fabrica;
        private readonly RepositorioLoteSimulacionOracle _lote;
        private readonly Action<string> _registro;

        public GeneradorLoteSimulacion(FabricaConexionesOracle fabrica, Action<string>? registro = null)
        {
            _fabrica = fabrica ?? throw new ArgumentNullException(nameof(fabrica));
            _lote = new RepositorioLoteSimulacionOracle(fabrica);
            _registro = registro ?? (_ => { });
        }

        public async Task<ResultadoLoteSimulacion> GenerarAsync(string idLote, DateTime finUtc, int semilla, CancellationToken ct = default)
        {
            if (!RepositorioLoteSimulacionOracle.LoteValido(idLote)) throw new ArgumentException("El lote debe tener la forma SIM-XXXX.", nameof(idLote));
            if (finUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("La fecha final debe ser UTC.", nameof(finUtc));
            if (!await _lote.TablaDisponibleAsync(ct)) throw new InvalidOperationException("La base no tiene V025 (REGISTRO_SIMULACION).");

            var reloj = new RelojSimulacion { Ahora = new DateTimeOffset(finUtc.AddDays(-31)) };
            var usuarios = new RepositorioUsuariosOracle(_fabrica);
            var materiales = new RepositorioMaterialesOracle(_fabrica);
            var contenedores = new RepositorioContenedoresOracle(_fabrica);
            var puntos = new RepositorioPuntosInstalacionOracle(_fabrica);
            var interacciones = new RepositorioInteraccionesOracle(_fabrica, reloj);
            var servicio = new ServicioInteracciones(usuarios, interacciones, contenedores, materiales, reloj);
            var cuentas = new ServicioUsuarios(usuarios, reloj);
            string sufijo = idLote.Substring(4).Replace("-", "");
            await _lote.AsegurarLoteAsync(idLote, $"{Ciudadanos} ciudadanos ficticios y {Registros} depósitos sintéticos (SIMULACION)", semilla, reloj.Ahora.UtcDateTime, ct);

            IReadOnlyList<Material> catalogo = await materiales.ListarAsync(true, ct);
            var porCodigo = Mezcla.Select(m => catalogo.FirstOrDefault(c => c.Codigo == m.Codigo && c.Categoria == m.Categoria))
                .Where(m => m is not null).ToDictionary(m => m!.Codigo, m => m!, StringComparer.Ordinal);
            var mezcla = Mezcla.Where(m => porCodigo.ContainsKey(m.Codigo)).ToArray();
            if (mezcla.Length == 0) throw new InvalidOperationException("El catálogo no tiene ninguno de los materiales del lote.");

            // Estación y canecas del lote: el nombre contiene «SIMULACIÓN» y el QR es interno (no se imprime).
            int punto = await AsegurarAsync(idLote, "PUNTO_INSTALACION", "estacion", () =>
                puntos.AgregarAsync(new PuntoInstalacion($"EcoAccess · Estación de SIMULACIÓN {idLote}", "Datos sintéticos; no es una ubicación física"), ct), ct);
            var canecas = new Dictionary<CategoriaMaterial, int>();
            foreach (var (categoria, color, ubicacion) in new[] { (CategoriaMaterial.Aprovechable, "Blanca", "izquierda"),
                (CategoriaMaterial.OrganicoAprovechable, "Verde", "centro"), (CategoriaMaterial.NoAprovechable, "Negra", "derecha") })
            {
                int id = await AsegurarAsync(idLote, "CONTENEDOR", "caneca-" + color.ToLowerInvariant(), async () =>
                {
                    int nuevo = await contenedores.AgregarAsync(new Contenedor(punto, $"SIMULACIÓN · Caneca {color.ToLowerInvariant()}", $"SimLote{sufijo}{color}", reloj.Ahora.UtcDateTime), ct);
                    foreach (var m in porCodigo.Values.Where(m => m.Categoria == categoria)) await contenedores.AdmitirMaterialAsync(nuevo, m.Id, ct);
                    await contenedores.CambiarEstadoAsync(nuevo, EstadoContenedor.EnServicio, ct);
                    await _lote.ConfigurarCanecaAsync(nuevo, (int)categoria, $"sim-{sufijo.ToLowerInvariant()}-{color.ToLowerInvariant()}", ubicacion, reloj.Ahora.UtcDateTime, ct);
                    return nuevo;
                }, ct);
                canecas[categoria] = id;
            }

            int operario = await AsegurarAsync(idLote, "USUARIO", "operario", () =>
                usuarios.AgregarAsync(new Usuario($"Operario simulado {idLote}", $"operario.{sufijo.ToLowerInvariant()}@{DominioCorreo}", CodigoRol.Operario, reloj.Ahora.UtcDateTime), ct), ct);

            int ciudadanosNuevos = 0;
            var ciudadanos = new int[Ciudadanos];
            for (int n = 1; n <= Ciudadanos; n++)
            {
                string clave = $"ciudadano-{n:00}";
                int? previo = await _lote.BuscarAsync(idLote, clave, ct);
                if (previo is { } existente) { ciudadanos[n - 1] = existente; continue; }
                int id = await usuarios.AgregarAsync(new Usuario($"Ciudadano simulado {n:00}", $"ciudadano{n:00}.{sufijo.ToLowerInvariant()}@{DominioCorreo}", CodigoRol.Ciudadano, reloj.Ahora.UtcDateTime), ct);
                var consentimiento = await cuentas.RegistrarConsentimientoAsync(id, "simulacion-v1", ct);
                if (!consentimiento.Exitoso) throw new InvalidOperationException("No se pudo registrar el consentimiento ficticio: " + consentimiento.Mensaje);
                await _lote.RegistrarAsync(idLote, "USUARIO", id, clave, ct);
                ciudadanos[n - 1] = id;
                ciudadanosNuevos++;
            }

            var azar = new Random(semilla);
            int nuevos = 0, existentes = 0, sinSensor = 0, conSensor = 0, verificados = 0;
            var rechazos = new List<string>();
            var simuladas = canecas.ToDictionary(c => c.Value, c => new CanecaSimulada(c.Value, servicio.RegistrarEventoAsync, reloj));
            for (int k = 1; k <= Registros; k++)
            {
                // Las decisiones aleatorias se toman siempre, aunque el registro ya exista: así la secuencia es la misma al repetir.
                var elegido = Elegir(mezcla, azar);
                int ciudadano = ciudadanos[(k - 1) % Ciudadanos];
                int unidades = 1 + azar.Next(4);
                double dia = azar.NextDouble() * 30;
                bool tieneSensor = azar.Next(10) < 3;
                bool verificar = azar.Next(10) < 6;
                string clave = $"registro-{k:000}";
                if (await _lote.BuscarAsync(idLote, clave, ct) is not null) { existentes++; continue; }

                Material material = porCodigo[elegido.Codigo];
                int caneca = canecas[elegido.Categoria];
                reloj.Ahora = new DateTimeOffset(finUtc.AddDays(-30 + dia));
                var inicio = await servicio.IniciarConCantidadAsync(ciudadano, caneca, CanalInteraccion.Simulador, unidades, ct);
                if (!inicio.Exitoso) { rechazos.Add($"{clave}: {inicio.Mensaje}"); continue; }
                var orden = await servicio.AutorizarAperturaAsync(ciudadano, inicio.Valor, material.Id, $"{idLote}-{clave}", ct);
                if (!orden.Exitoso) { rechazos.Add($"{clave}: {orden.Mensaje}"); continue; }
                var tapa = simuladas[caneca];
                tapa.Escenario = tieneSensor ? EscenarioCaneca.Normal : EscenarioCaneca.SinSensor;
                var despacho = new ServicioDespachoOrdenes(interacciones, servicio, tapa, reloj);
                if (await despacho.DespacharAsync(orden.Valor, ct) != ResultadoDespacho.Enviada) { rechazos.Add($"{clave}: no se despachó"); continue; }
                var eventos = await tapa.EjecutarAsync(orden.Valor, ct);
                if (eventos.Any(e => !e.Exitoso)) { rechazos.Add($"{clave}: evento rechazado"); continue; }
                Deposito? deposito = await interacciones.ObtenerDepositoPorOrdenAsync(orden.Valor, ct);
                if (deposito is null) { rechazos.Add($"{clave}: sin depósito"); continue; }
                await _lote.RegistrarAsync(idLote, "INTERACCION", inicio.Valor, "interaccion-" + k.ToString("000"), ct);
                await _lote.RegistrarAsync(idLote, "DEPOSITO", deposito.Id, clave, ct);
                if (tieneSensor) conSensor++; else sinSensor++;
                if (verificar)
                {
                    reloj.Ahora = reloj.Ahora.AddHours(2);
                    var verificacion = await servicio.VerificarDepositoAsync(operario, deposito.Id, material.Id, ct, unidades);
                    if (verificacion.Exitoso) verificados++; else rechazos.Add($"{clave}: verificación {verificacion.Mensaje}");
                }
                nuevos++;
                if (k % 10 == 0) _registro($"Lote {idLote}: {k}/{Registros}");
            }

            await _lote.CompletarAsync(idLote, ct);
            return new ResultadoLoteSimulacion(idLote, ciudadanosNuevos, nuevos, existentes, sinSensor, conSensor, verificados, rechazos);
        }

        private async Task<int> AsegurarAsync(string idLote, string tabla, string clave, Func<Task<int>> crear, CancellationToken ct)
        {
            if (await _lote.BuscarAsync(idLote, clave, ct) is { } existente) return existente;
            int id = await crear();
            await _lote.RegistrarAsync(idLote, tabla, id, clave, ct);
            return id;
        }

        private static (string Codigo, CategoriaMaterial Categoria, int Peso) Elegir((string Codigo, CategoriaMaterial Categoria, int Peso)[] mezcla, Random azar)
        {
            int total = mezcla.Sum(m => m.Peso), tiro = azar.Next(total);
            foreach (var m in mezcla) { if (tiro < m.Peso) return m; tiro -= m.Peso; }
            return mezcla[mezcla.Length - 1];
        }
    }
}
