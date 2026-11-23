using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Presentacion;

// Prueba de la estación de tres canecas con el MISMO camino que el asistente de depósito de la aplicación:
// IniciarConCantidadAsync → AutorizarAperturaAsync → ConsultarOperacionAsync hasta el depósito y el cierre.
// Requiere la estación en marcha (scripts\estacion\EcoAccess-Estacion.ps1) y la conexión de Oracle de este equipo.
// Usa cuentas ficticias (por defecto 244, 1934, 2298 y 2299). Los depósitos quedan SIN verificar: no dan ecopuntos.
// Uso: PruebaEstacion.exe [id1,id2,id3,id4]
static class Program
{
    static readonly List<string> Fallos = new();
    static ServiciosAplicacion _s = null!;

    static async Task<int> Main(string[] args)
    {
        int[] usuarios = (args.Length > 0 ? args[0] : "244,1934,2298,2299").Split(',').Select(int.Parse).ToArray();
        if (usuarios.Length < 3) { Console.Error.WriteLine("Indica al menos tres usuarios de prueba."); return 2; }
        string secretos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "ecoaccess-p3-framework", "secrets.json");
        using (var doc = JsonDocument.Parse(File.ReadAllText(secretos)))
            _s = EcoAccess.P3.Presentacion.Program.ComponerServicios(doc.RootElement.GetProperty("ConnectionStrings:OracleP3").GetString()!, null);

        var botella = CatalogoPrototipo.Buscar("botella-agua")!;
        var banano = CatalogoPrototipo.Buscar("cascara-banano")!;
        var servilleta = CatalogoPrototipo.Buscar("servilletas-usadas")!;
        var periodico = CatalogoPrototipo.Buscar("periodico-usado")!;
        var lechuga = CatalogoPrototipo.Buscar("hojas-lechuga")!;

        Titulo("1. Cuatro usuarios, uno tras otro, en las tres canecas");
        await Depositar(usuarios[0], botella, "blanca");
        await Depositar(usuarios[1], banano, "verde");
        await Depositar(usuarios[2], servilleta, "negra");
        await Depositar(usuarios[3 % usuarios.Length], periodico, "blanca (residuo nuevo de la lámina)");

        Titulo("2. Tres usuarios a la vez, cada uno en una caneca distinta");
        await Task.WhenAll(Depositar(usuarios[0], banano, "verde"), Depositar(usuarios[1], servilleta, "negra"), Depositar(usuarios[2], botella, "blanca"));

        Titulo("3. Dos usuarios a la vez en la MISMA caneca: uno abre y el otro espera su turno (RN-16)");
        var a = Depositar(usuarios[0], botella, "blanca", esperarOcupada: true);
        await Task.Delay(1500);
        var b = Depositar(usuarios[1], periodico, "blanca", esperarOcupada: true);
        await Task.WhenAll(a, b);

        Titulo("4. Reintento con la misma clave: devuelve la misma orden, no abre dos veces");
        await Idempotencia(usuarios[2], lechuga);

        Titulo("5. Visitante (modo invitado) en la caneca verde");
        await Depositar(null, lechuga, "verde (invitado)");

        Console.WriteLine();
        Console.WriteLine(Fallos.Count == 0 ? "RESULTADO: todas las pruebas correctas." : $"RESULTADO: {Fallos.Count} con fallo:\n  " + string.Join("\n  ", Fallos));
        return Fallos.Count == 0 ? 0 : 1;
    }

    static void Titulo(string t) { Console.WriteLine(); Console.WriteLine("== " + t); }

    static async Task<DestinoPrototipo> Destino(ResiduoPrototipo r)
    {
        var d = await _s.CatalogoAmbiental.ResolverPrototipoAsync(r);
        if (!d.Exitoso) throw new InvalidOperationException($"{r.Nombre}: {d.Mensaje}");
        return d.Valor;
    }

    /// <summary>Un depósito completo. Con esperarOcupada, si la caneca está ocupada reintenta (como haría el ciudadano) hasta 60 s.</summary>
    static async Task Depositar(int? usuario, ResiduoPrototipo residuo, string etiqueta, bool esperarOcupada = false)
    {
        string quien = usuario is null ? "invitado" : $"usuario {usuario}";
        var reloj = Stopwatch.StartNew();
        try
        {
            DestinoPrototipo destino = await Destino(residuo);
            string clave = "prueba-" + Guid.NewGuid().ToString("N");
            int interaccion = 0, intentos = 0;
            Resultado<int> orden;
            while (true)
            {
                if (interaccion == 0)
                {
                    var inicio = usuario is int id
                        ? await _s.Interacciones.IniciarConCantidadAsync(id, destino.Caneca.Id, CanalInteraccion.Simulador, 1)
                        : await _s.Interacciones.IniciarVisitanteAsync(destino.Caneca.Id);
                    if (!inicio.Exitoso) { Fallar($"{quien} · {etiqueta}: no inició ({inicio.Mensaje})"); return; }
                    interaccion = inicio.Valor;
                }
                orden = usuario is int u
                    ? await _s.Interacciones.AutorizarAperturaAsync(u, interaccion, destino.Material.Id, clave)
                    : await _s.Interacciones.AutorizarAperturaVisitanteAsync(interaccion, destino.Material.Id, clave);
                if (orden.Exitoso) break;
                bool ocupada = orden.Mensaje?.Contains("RN-16") == true;
                if (esperarOcupada && ocupada && reloj.Elapsed < TimeSpan.FromSeconds(60))
                {
                    if (intentos++ == 0) Console.WriteLine($"   {quien} · {etiqueta}: caneca ocupada por otra persona (RN-16); espera su turno…");
                    await Task.Delay(2000);
                    continue;
                }
                Fallar($"{quien} · {etiqueta}: orden rechazada ({orden.Mensaje})");
                return;
            }

            DateTime limite = DateTime.UtcNow.AddSeconds(150);
            TimeSpan? abierta = null, deposito = null;
            while (DateTime.UtcNow < limite)
            {
                var consulta = usuario is int v
                    ? await _s.Interacciones.ConsultarOperacionAsync(v, interaccion, clave)
                    : await _s.Interacciones.ConsultarOperacionVisitanteAsync(interaccion, clave);
                if (consulta.Exitoso)
                {
                    var op = consulta.Valor;
                    if (abierta is null && op.Orden.Estado >= EstadoOrdenApertura.Abierta && op.Orden.Estado <= EstadoOrdenApertura.Cerrada) abierta = reloj.Elapsed;
                    if (deposito is null && op.Deposito is not null) deposito = reloj.Elapsed;
                    if (op.Orden.Estado is EstadoOrdenApertura.Vencida or EstadoOrdenApertura.Fallida)
                    { Fallar($"{quien} · {etiqueta}: la orden {op.Orden.Id} terminó en {op.Orden.Estado}"); return; }
                    if (op.Orden.Estado == EstadoOrdenApertura.Cerrada)
                    {
                        if (op.Deposito is null) { Fallar($"{quien} · {etiqueta}: cerró sin depósito"); return; }
                        bool sinVerificar = op.Deposito.IdVerificador is null;
                        bool materialOk = op.Deposito.IdMaterialDeclarado == destino.Material.Id;
                        string linea = $"   OK  {quien} · {etiqueta} · {residuo.Nombre} → caneca {destino.Caneca.Id}: orden {op.Orden.Id}, depósito {op.Deposito.Id}, " +
                            $"abierta a los {abierta?.TotalSeconds:N1} s, depósito a los {deposito?.TotalSeconds:N1} s, cerrada a los {reloj.Elapsed.TotalSeconds:N1} s" +
                            (sinVerificar ? ", sin verificar (sin ecopuntos)" : ", ¡VERIFICADO!");
                        if (!sinVerificar || !materialOk) Fallar(linea + (materialOk ? "" : " · material distinto"));
                        else Console.WriteLine(linea);
                        return;
                    }
                }
                await Task.Delay(1000);
            }
            Fallar($"{quien} · {etiqueta}: sin cierre en 150 s (¿estación detenida?)");
        }
        catch (Exception e) { Fallar($"{quien} · {etiqueta}: {e.GetType().Name}: {e.Message}"); }
    }

    static async Task Idempotencia(int usuario, ResiduoPrototipo residuo)
    {
        DestinoPrototipo destino = await Destino(residuo);
        var inicio = await _s.Interacciones.IniciarConCantidadAsync(usuario, destino.Caneca.Id, CanalInteraccion.Simulador, 1);
        if (!inicio.Exitoso) { Fallar("idempotencia: no inició " + inicio.Mensaje); return; }
        string clave = "prueba-" + Guid.NewGuid().ToString("N");
        var uno = await _s.Interacciones.AutorizarAperturaAsync(usuario, inicio.Valor, destino.Material.Id, clave);
        var dos = await _s.Interacciones.AutorizarAperturaAsync(usuario, inicio.Valor, destino.Material.Id, clave);
        if (uno.Exitoso && dos.Exitoso && uno.Valor == dos.Valor)
            Console.WriteLine($"   OK  usuario {usuario}: dos llamadas con la misma clave → la misma orden {uno.Valor}");
        else Fallar($"idempotencia: {uno.Mensaje ?? uno.Valor.ToString()} / {dos.Mensaje ?? dos.Valor.ToString()}");
        // Se espera a que termine para no dejar la caneca ocupada.
        for (int i = 0; i < 60; i++)
        {
            var c = await _s.Interacciones.ConsultarOperacionAsync(usuario, inicio.Valor, clave);
            if (c.Exitoso && c.Valor.Orden.Estado >= EstadoOrdenApertura.Cerrada) break;
            await Task.Delay(1000);
        }
    }

    static void Fallar(string texto) { lock (Fallos) Fallos.Add(texto); Console.WriteLine("   FALLO " + texto); }
}
