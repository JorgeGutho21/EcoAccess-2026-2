using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Presentacion;
using Oracle.ManagedDataAccess.Client;

// Validación final de EcoAccess P3 contra Oracle Cloud (ver ValidacionFinal.csproj).
// Uso: ValidacionFinal.exe [--sin-estacion]
//   --sin-estacion  omite el depósito por la caneca demo (si la estación no está en marcha).
static class Program
{
    static readonly List<(string Grupo, string Prueba, string Estado, string Detalle)> Resultados = new();
    static string _cadena = "";
    static ServiciosAplicacion _s = null!;
    static readonly string Marca = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

    static async Task<int> Main(string[] args)
    {
        bool conEstacion = !args.Contains("--sin-estacion");
        string secretos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "UserSecrets", "ecoaccess-p3-framework", "secrets.json");
        if (!File.Exists(secretos)) { Console.Error.WriteLine("Este equipo no tiene configurada la conexión (User Secrets ecoaccess-p3-framework)."); return 2; }
        using (var doc = JsonDocument.Parse(File.ReadAllText(secretos)))
            _cadena = doc.RootElement.GetProperty("ConnectionStrings:OracleP3").GetString()!;
        _s = EcoAccess.P3.Presentacion.Program.ComponerServicios(_cadena, null);

        Console.WriteLine($"Validación final EcoAccess P3 · marca {Marca} · {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await Oracle_();
        Catalogo();
        // --solo-lectura: diagnóstico de un equipo nuevo (no crea cuentas ni depósitos).
        if (args.Contains("--solo-lectura")) return Informe();
        int? admin = await Ejecutar("Oracle", "Localizar un Supervisor activo (actor para crear la primera cuenta de prueba)", async () =>
        {
            object? id = await Escalar("SELECT MIN(u.id_usuario) FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario " +
                                       "JOIN rol r ON r.id_rol = ur.id_rol WHERE r.codigo = 3 AND u.activo = 1");
            if (id is null || id is DBNull) throw new InvalidOperationException("No hay ningún Supervisor activo.");
            return (Convert.ToInt32(id), "id " + Convert.ToInt32(id));
        });
        if (admin is null) return Informe();
        int i = Array.IndexOf(args, "--desactivar");
        if (i >= 0 && i + 1 < args.Length)
        {
            // Limpieza de cuentas de prueba de una corrida interrumpida: solo acepta cuentas validacion.final.*@ecoaccess.test.
            foreach (int id in args[i + 1].Split(',').Select(int.Parse))
            {
                string correo = Convert.ToString(await Escalar("SELECT correo_electronico FROM usuario WHERE id_usuario = " + id)) ?? "";
                if (!correo.StartsWith("validacion.final.") || !correo.EndsWith("@ecoaccess.test")) { Anotar("Limpieza", $"Cuenta {id}", "FALLIDA", "no es una cuenta de prueba: no se toca"); continue; }
                await Esperar("Limpieza", $"Desactivar la cuenta de prueba {id}", _s.Usuarios.DesactivarAsync(admin.Value, id), true);
            }
            return Informe();
        }

        var cuentas = await Perfiles(admin.Value);
        if (cuentas is not null)
        {
            if (conEstacion) await Deposito(cuentas.Value.ciudadano, cuentas.Value.operario);
            else Anotar("Depósito", "Depósito por la caneca demo", "NO EJECUTADA", "se pidió --sin-estacion");
            await Persistencia(cuentas.Value);
            await Limpieza(admin.Value, cuentas.Value);
        }
        await Robustez();
        return Informe();
    }

    // ---------------------------------------------------------------- Oracle
    static async Task Oracle_()
    {
        var b = new OracleConnectionStringBuilder(_cadena);
        await Ejecutar("Oracle", "Conexión con Oracle Cloud (ODP.NET administrado, wallet mTLS)", async () =>
        {
            using var c = new OracleConnection(_cadena);
            var reloj = Stopwatch.StartNew();
            await c.OpenAsync();
            string wallet = string.IsNullOrEmpty(c.TnsAdmin) && !_cadena.ToUpperInvariant().Contains("WALLET") ? "sin wallet declarado" : "con wallet";
            return (0, $"usuario {b.UserID}, servicio {b.DataSource}, {wallet}, versión {c.ServerVersion}, {reloj.ElapsedMilliseconds} ms");
        });
        await Ejecutar("Oracle", "Lectura: conteos de tablas principales", async () =>
        {
            var partes = new List<string>();
            foreach (var t in new[] { "usuario", "material", "contenedor", "deposito", "orden_apertura", "movimiento_ecopuntos" })
                partes.Add($"{t} {await Escalar("SELECT COUNT(*) FROM " + t)}");
            partes.Add($"depósitos verificados {await Escalar("SELECT COUNT(*) FROM deposito WHERE id_verificador IS NOT NULL")}");
            partes.Add($"reglas de ecopuntos {await Escalar("SELECT COUNT(*) FROM regla_ecopuntos")}");
            return (0, string.Join(", ", partes));
        });
        await Ejecutar("Oracle", "Integridad referencial: claves foráneas habilitadas y validadas", async () =>
        {
            object? total = await Escalar("SELECT COUNT(*) FROM user_constraints WHERE constraint_type = 'R'");
            object? malas = await Escalar("SELECT COUNT(*) FROM user_constraints WHERE constraint_type = 'R' AND (status <> 'ENABLED' OR validated <> 'VALIDATED')");
            if (Convert.ToInt32(malas) != 0) throw new InvalidOperationException($"{malas} claves foráneas deshabilitadas o sin validar");
            return (0, $"{total} claves foráneas, todas ENABLED y VALIDATED");
        });
        await Ejecutar("Oracle", "Objetos inválidos del esquema", async () =>
        {
            object? n = await Escalar("SELECT COUNT(*) FROM user_objects WHERE status <> 'VALID'");
            if (Convert.ToInt32(n) != 0) throw new InvalidOperationException($"{n} objetos inválidos");
            return (0, "0 objetos inválidos");
        });
        await Ejecutar("Oracle", "Roles ECOACCESS_ROL_CONSULTA y ECOACCESS_ROL_EDICION (permisos concedidos por el esquema)", async () =>
        {
            var partes = new List<string>();
            using var c = await Abrir();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT grantee, privilege, COUNT(*) FROM user_tab_privs_made WHERE grantee IN ('ECOACCESS_ROL_CONSULTA','ECOACCESS_ROL_EDICION') GROUP BY grantee, privilege ORDER BY 1, 2";
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) partes.Add($"{r.GetString(0)}:{r.GetString(1)}={r.GetDecimal(2)}");
            if (!partes.Any(p => p.StartsWith("ECOACCESS_ROL_CONSULTA:SELECT"))) throw new InvalidOperationException("CONSULTA sin SELECT");
            if (partes.Any(p => p.Contains(":DELETE"))) throw new InvalidOperationException("algún rol tiene DELETE");
            object? sensibles = await Escalar("SELECT COUNT(*) FROM user_tab_privs_made WHERE grantee LIKE 'ECOACCESS_ROL_%' AND table_name IN ('CREDENCIAL_ACCESO','TOKEN_VINCULACION','VINCULACION_PENDIENTE','UPDATE_TELEGRAM')");
            if (Convert.ToInt32(sensibles) != 0) throw new InvalidOperationException("los roles tienen acceso a tablas sensibles");
            return (0, string.Join(", ", partes) + "; sin DELETE y sin acceso a tablas sensibles");
        });
    }

    // ---------------------------------------------------------------- Catálogo
    static void Catalogo()
    {
        var casos = new (string Texto, string Id, CategoriaMaterial Caneca)[]
        {
            ("Cáscara de aguacate", "cascara-aguacate", CategoriaMaterial.OrganicoAprovechable),
            ("CASCARA DE AGUACATE", "cascara-aguacate", CategoriaMaterial.OrganicoAprovechable),
            ("Botella plástica", "botella-agua", CategoriaMaterial.Aprovechable),
            ("botella plastica", "botella-agua", CategoriaMaterial.Aprovechable),
            ("Cartón limpio", "carton-limpio", CategoriaMaterial.Aprovechable),
            ("Cartón contaminado con grasa", "papel-contaminado", CategoriaMaterial.NoAprovechable),
            ("Colilla de cigarrillo", "colilla", CategoriaMaterial.NoAprovechable),
            ("Condón usado correctamente envuelto", "condon-usado", CategoriaMaterial.NoAprovechable),
            ("Residuos orgánicos de alimentos", "", CategoriaMaterial.OrganicoAprovechable),
        };
        foreach (var (texto, id, caneca) in casos)
        {
            var r = CatalogoPrototipo.Consultar(texto);
            var primero = r.Residuos.FirstOrDefault();
            string detalle = primero is null ? $"sin resultado ({r.Mensaje})"
                : $"«{primero.Nombre}» → caneca {CatalogoPrototipo.NombreCaneca(primero.Caneca)}" + (r.Residuos.Count > 1 ? $" (+{r.Residuos.Count - 1} coincidencias)" : "");
            bool ok = primero is not null && primero.Caneca == caneca && (id == "" || r.Residuos.Any(x => x.Id == id));
            Anotar("Catálogo", $"Búsqueda «{texto}»", ok ? "CORRECTA" : "FALLIDA", detalle);
        }
        var nada = CatalogoPrototipo.Consultar("objeto inexistente xyz");
        Anotar("Catálogo", "Búsqueda de un residuo inexistente", nada.Residuos.Count == 0 ? "CORRECTA" : "FALLIDA", nada.Mensaje);
        // Diseño: con la caja de búsqueda vacía se muestra el catálogo completo.
        var vacia = CatalogoPrototipo.Consultar("   ");
        Anotar("Catálogo", "Búsqueda vacía muestra el catálogo completo", vacia.Residuos.Count == CatalogoPrototipo.Todos.Count ? "CORRECTA" : "FALLIDA", vacia.Mensaje);
        var sinFoto = CatalogoPrototipo.Todos.Where(x => !CatalogoPrototipo.TieneImagen(x)).Select(x => x.Id).ToList();
        int rotas = CatalogoPrototipo.Todos.Where(CatalogoPrototipo.TieneImagen).Count(x => CatalogoFotos.DeCaneca(x.Caneca).Count == 0);
        Anotar("Catálogo", "Residuos, imágenes y faltantes", rotas == 0 ? "CORRECTA" : "FALLIDA",
            $"{CatalogoPrototipo.Todos.Count} residuos; visibles con imagen: blanca {CatalogoPrototipo.VisiblesDe(CategoriaMaterial.Aprovechable).Count}, " +
            $"verde {CatalogoPrototipo.VisiblesDe(CategoriaMaterial.OrganicoAprovechable).Count}, negra {CatalogoPrototipo.VisiblesDe(CategoriaMaterial.NoAprovechable).Count}; " +
            $"sin imagen (se muestran solo como texto en la búsqueda): {string.Join(", ", sinFoto)}");
    }

    // ---------------------------------------------------------------- Perfiles
    static async Task<(int ciudadano, int operario, int supervisor, string clave)?> Perfiles(int admin)
    {
        string clave = "Vf9#" + Guid.NewGuid().ToString("N").Substring(0, 12) + "kQ!"; // 18 caracteres: el máximo es 20
        string correo(string p) => $"validacion.final.{p}.{Marca}@ecoaccess.test";

        int? sup = await Ejecutar("Perfiles", "Crear Supervisor de prueba (actor: Supervisor real, nivel de servicio)", async () =>
        {
            var id = await _s.Usuarios.RegistrarAsync(admin, "Validacion Final Supervisor", correo("supervisor"), CodigoRol.Administrador);
            Exigir(id, "registro");
            Exigir(await _s.Autenticacion.EstablecerContrasenaInicialAsync(admin, id.Valor, clave), "contraseña inicial");
            return (id.Valor, "id " + id.Valor);
        });
        if (sup is null) return null;
        SesionActiva? sesionSup = await Ejecutar("Perfiles", "Inicio de sesión real del Supervisor de prueba", async () =>
        {
            var s = Exigir(await _s.Autenticacion.IniciarSesionAsync(correo("supervisor"), clave), "inicio");
            if (!s.TieneRol(CodigoRol.Administrador)) throw new InvalidOperationException("sin rol Supervisor");
            return (s, "roles: " + string.Join(", ", s.Roles));
        });
        if (sesionSup is null) return null;

        int? ope = await Ejecutar("Perfiles", "El Supervisor de prueba crea un Operario (gestión de usuarios)", async () =>
        {
            var id = Exigir(await _s.Usuarios.RegistrarAsync(sesionSup.IdUsuario, "Validacion Final Operario", correo("operario"), CodigoRol.Operario), "registro");
            Exigir(await _s.Autenticacion.EstablecerContrasenaInicialAsync(sesionSup.IdUsuario, id, clave), "contraseña");
            return (id, "id " + id);
        });
        int? ciu = await Ejecutar("Perfiles", "Registro público de un Ciudadano (con consentimiento)", async () =>
        {
            var id = Exigir(await _s.RegistroCiudadanos.RegistrarAsync("Validacion Final Ciudadano", correo("ciudadano"), clave, clave, true), "registro");
            return (id, "id " + id);
        });
        if (ope is null || ciu is null) return null;

        await Esperar("Perfiles", "Registro duplicado con el mismo correo es rechazado",
            _s.RegistroCiudadanos.RegistrarAsync("Validacion Final Ciudadano", correo("ciudadano").ToUpperInvariant(), clave, clave, true), false);
        await Esperar("Perfiles", "Registro sin aceptar el tratamiento de datos es rechazado",
            _s.RegistroCiudadanos.RegistrarAsync("Sin Consentimiento", correo("sinconsentimiento"), clave, clave, false), false);
        await Esperar("Perfiles", "Registro con confirmación distinta es rechazado",
            _s.RegistroCiudadanos.RegistrarAsync("Confirmacion Distinta", correo("confirmacion"), clave, clave + "x", true), false);
        await Esperar("Perfiles", "Inicio de sesión con contraseña incorrecta es rechazado", _s.Autenticacion.IniciarSesionPublicaAsync(correo("ciudadano"), clave + "x"), false);
        await Esperar("Perfiles", "Inicio de sesión con campos vacíos es rechazado", _s.Autenticacion.IniciarSesionPublicaAsync("", ""), false);
        await Esperar("Perfiles", "Inicio de sesión real del Ciudadano", _s.Autenticacion.IniciarSesionPublicaAsync(correo("ciudadano"), clave), true);
        await Esperar("Perfiles", "Inicio de sesión real del Operario", _s.Autenticacion.IniciarSesionAsync(correo("operario"), clave), true);

        // Permisos: cada perfil solo puede lo suyo (lo comprueba cada servicio en Oracle).
        await Esperar("Permisos", "Ciudadano NO puede listar usuarios", _s.Usuarios.ListarAsync(ciu.Value), false);
        await Esperar("Permisos", "Ciudadano NO puede abrir el panel administrativo", _s.PanelAdministrativo.ConsultarAsync(ciu.Value), false);
        await Esperar("Permisos", "Ciudadano NO puede ver depósitos sin verificar", _s.Interacciones.ListarDepositosSinVerificarAsync(ciu.Value), false);
        await Esperar("Permisos", "Ciudadano NO puede crear usuarios", _s.Usuarios.RegistrarAsync(ciu.Value, "Intruso", correo("intruso1"), CodigoRol.Administrador), false);
        await Esperar("Permisos", "Ciudadano SÍ consulta sus ecopuntos", _s.Ecopuntos.ConsultarMiosAsync(ciu.Value), true);
        await Esperar("Permisos", "Operario SÍ ve depósitos sin verificar", _s.Interacciones.ListarDepositosSinVerificarAsync(ope.Value), true);
        await Esperar("Permisos", "Operario NO puede crear usuarios", _s.Usuarios.RegistrarAsync(ope.Value, "Intruso", correo("intruso2"), CodigoRol.Administrador), false);
        await Esperar("Permisos", "Operario NO puede asignarse el rol Supervisor", _s.Usuarios.AsignarRolAsync(ope.Value, ope.Value, CodigoRol.Administrador), false);
        await Esperar("Permisos", "Supervisor SÍ lista usuarios", _s.Usuarios.ListarAsync(sesionSup.IdUsuario), true);
        await Esperar("Permisos", "Supervisor SÍ abre el panel administrativo", _s.PanelAdministrativo.ConsultarAsync(sesionSup.IdUsuario), true);
        await Esperar("Permisos", "Supervisor SÍ consulta el diagnóstico de Oracle", _s.DiagnosticoOracle.ConsultarAsync(sesionSup.IdUsuario), true);
        return (ciu.Value, ope.Value, sesionSup.IdUsuario, clave);
    }

    // ---------------------------------------------------------------- Depósito
    static async Task Deposito(int ciudadano, int operario)
    {
        var residuo = CatalogoPrototipo.Buscar("botella-agua")!;
        long saldoAntes = 0;
        int? deposito = await Ejecutar("Depósito", "Ciudadano deposita una botella por la caneca blanca demo (flujo del asistente)", async () =>
        {
            saldoAntes = Exigir(await _s.Ecopuntos.ConsultarMiosAsync(ciudadano), "saldo").Saldo;
            var destino = Exigir(await _s.CatalogoAmbiental.ResolverPrototipoAsync(residuo), "destino");
            int interaccion = Exigir(await _s.Interacciones.IniciarConCantidadAsync(ciudadano, destino.Caneca.Id, CanalInteraccion.Simulador, 1), "inicio");
            string clave = "validacion-" + Guid.NewGuid().ToString("N");
            var reloj = Stopwatch.StartNew();
            Resultado<int> orden;
            while (!(orden = await _s.Interacciones.AutorizarAperturaAsync(ciudadano, interaccion, destino.Material.Id, clave)).Exitoso)
            {
                if (orden.Mensaje?.Contains("RN-16") != true || reloj.Elapsed > TimeSpan.FromSeconds(90)) Exigir(orden, "orden");
                await Task.Delay(2000);
            }
            int ordenRepetida = Exigir(await _s.Interacciones.AutorizarAperturaAsync(ciudadano, interaccion, destino.Material.Id, clave), "reintento");
            if (ordenRepetida != orden.Valor) throw new InvalidOperationException("el reintento creó otra orden");
            while (reloj.Elapsed < TimeSpan.FromSeconds(150))
            {
                var op = await _s.Interacciones.ConsultarOperacionAsync(ciudadano, interaccion, clave);
                if (op.Exitoso && op.Valor.Orden.Estado is EstadoOrdenApertura.Vencida or EstadoOrdenApertura.Fallida)
                    throw new InvalidOperationException("orden " + op.Valor.Orden.Estado);
                if (op.Exitoso && op.Valor.Orden.Estado == EstadoOrdenApertura.Cerrada)
                {
                    var d = op.Valor.Deposito ?? throw new InvalidOperationException("cerró sin depósito");
                    if (d.IdVerificador is not null) throw new InvalidOperationException("quedó verificado sin un operario");
                    return (d.Id, $"orden {orden.Valor} (reintento = misma orden), depósito {d.Id} sin verificar, cerrada a los {reloj.Elapsed.TotalSeconds:N1} s");
                }
                await Task.Delay(1000);
            }
            throw new TimeoutException("la caneca no cerró en 150 s (¿estación demo detenida?)");
        });
        if (deposito is null) return;
        await Ejecutar("Depósito", "El historial del ciudadano muestra el depósito", async () =>
        {
            var lista = Exigir(await _s.Interacciones.ListarMisDepositosAsync(ciudadano), "historial");
            if (!lista.Any(x => x.Id == deposito)) throw new InvalidOperationException("no aparece");
            return (0, $"{lista.Count} depósito(s) en el historial");
        });
        await Ejecutar("Depósito", "Sin verificación no se acreditan ecopuntos", async () =>
        {
            long ahora = Exigir(await _s.Ecopuntos.ConsultarMiosAsync(ciudadano), "saldo").Saldo;
            if (ahora != saldoAntes) throw new InvalidOperationException($"saldo cambió de {saldoAntes} a {ahora}");
            return (0, $"saldo {ahora}");
        });
        await Esperar("Depósito", "Ciudadano NO puede verificar su propio depósito",
            _s.Interacciones.VerificarDepositoAsync(ciudadano, deposito.Value, CatalogoPrototipo.Buscar("botella-agua") is { } ? (await _s.CatalogoAmbiental.ResolverPrototipoAsync(residuo)).Valor.Material.Id : 0), false);
        await Ejecutar("Depósito", "El Operario verifica el depósito y se consulta el saldo", async () =>
        {
            var destino = Exigir(await _s.CatalogoAmbiental.ResolverPrototipoAsync(residuo), "destino");
            // Igual que FormInteraccionesDepositos: el operario confirma el material y cuenta las unidades.
            Exigir(await _s.Interacciones.VerificarDepositoAsync(operario, deposito.Value, destino.Material.Id, default, cantidadVerificada: 1), "verificación");
            var lista = Exigir(await _s.Interacciones.ListarMisDepositosAsync(ciudadano), "historial");
            var d = lista.First(x => x.Id == deposito);
            long saldo = Exigir(await _s.Ecopuntos.ConsultarMiosAsync(ciudadano), "saldo").Saldo;
            return (0, $"verificado por {d.IdVerificador}; saldo del ciudadano {saldoAntes} → {saldo}");
        });
        await Esperar("Depósito", "Verificar dos veces el mismo depósito es rechazado",
            _s.Interacciones.VerificarDepositoAsync(operario, deposito.Value, (await _s.CatalogoAmbiental.ResolverPrototipoAsync(residuo)).Valor.Material.Id), false);

        // Ecopuntos: en EcoAccess la acreditación es un paso aparte («Acreditar depósito» en Ecopuntos), con la tarifa vigente.
        int material = (await _s.CatalogoAmbiental.ResolverPrototipoAsync(residuo)).Valor.Material.Id;
        var reglas = await _s.Ecopuntos.ListarReglasAsync(operario);
        var regla = reglas.Exitoso ? reglas.Valor.Where(r => r.IdMaterial == material && r.EstaVigente(DateTime.UtcNow)).OrderByDescending(r => r.DesdeUtc).FirstOrDefault() : null;
        if (regla is null) { Anotar("Ecopuntos", "Acreditar el depósito verificado", "NO EJECUTADA", $"no hay tarifa vigente para el material {material}"); return; }
        string claveCredito = "validacion-credito-" + Marca;
        await Ejecutar("Ecopuntos", "El Operario acredita el depósito verificado con la tarifa vigente", async () =>
        {
            Exigir(await _s.Ecopuntos.AcreditarAsync(operario, deposito.Value, regla.Id, claveCredito), "acreditación");
            long saldo = Exigir(await _s.Ecopuntos.ConsultarMiosAsync(ciudadano), "saldo").Saldo;
            if (saldo <= saldoAntes) throw new InvalidOperationException($"el saldo no subió ({saldo})");
            return (0, $"regla {regla.Id} ({regla.Puntos} puntos); saldo {saldoAntes} → {saldo}");
        });
        await Ejecutar("Ecopuntos", "Repetir la acreditación con la misma clave no suma dos veces", async () =>
        {
            long antes = Exigir(await _s.Ecopuntos.ConsultarMiosAsync(ciudadano), "saldo").Saldo;
            var otra = await _s.Ecopuntos.AcreditarAsync(operario, deposito.Value, regla.Id, claveCredito);
            long despues = Exigir(await _s.Ecopuntos.ConsultarMiosAsync(ciudadano), "saldo").Saldo;
            if (despues != antes) throw new InvalidOperationException($"saldo {antes} → {despues}");
            return (0, $"saldo se mantiene en {despues} ({(otra.Exitoso ? "respuesta idempotente" : "rechazado: " + otra.Mensaje)})");
        });
        await Esperar("Ecopuntos", "Un Ciudadano NO puede acreditarse puntos",
            _s.Ecopuntos.AcreditarAsync(ciudadano, deposito.Value, regla.Id, claveCredito + "-c"), false);
    }

    // ---------------------------------------------------------------- Persistencia y transacción
    static async Task Persistencia((int ciudadano, int operario, int supervisor, string clave) c)
    {
        await Ejecutar("Oracle", "Transacción: un cambio con ROLLBACK no persiste", async () =>
        {
            string original;
            using (var con = await Abrir())
            {
                original = Convert.ToString(await EscalarEn(con, "SELECT nombre FROM usuario WHERE id_usuario = " + c.ciudadano))!;
                using var tx = con.BeginTransaction();
                using var cmd = con.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "UPDATE usuario SET nombre = 'Cambio Revertido' WHERE id_usuario = :id";
                cmd.Parameters.Add("id", c.ciudadano);
                if (await cmd.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("no actualizó");
                tx.Rollback();
            }
            OracleConnection.ClearAllPools();
            string despues = Convert.ToString(await Escalar("SELECT nombre FROM usuario WHERE id_usuario = " + c.ciudadano))!;
            if (despues != original) throw new InvalidOperationException($"quedó «{despues}»");
            return (0, $"nombre sigue siendo «{original}»");
        });
        await Ejecutar("Oracle", "Persistencia: cerrar todas las conexiones, volver a componer los servicios e iniciar sesión", async () =>
        {
            OracleConnection.ClearAllPools();
            _s = EcoAccess.P3.Presentacion.Program.ComponerServicios(_cadena, null);
            var s = Exigir(await _s.Autenticacion.IniciarSesionPublicaAsync($"validacion.final.ciudadano.{Marca}@ecoaccess.test", c.clave), "inicio");
            var lista = Exigir(await _s.Interacciones.ListarMisDepositosAsync(s.IdUsuario), "historial");
            return (0, $"ciudadano {s.IdUsuario} inicia sesión de nuevo; {lista.Count} depósito(s) persistidos");
        });
    }

    static async Task Limpieza(int admin, (int ciudadano, int operario, int supervisor, string clave) c)
    {
        await Ejecutar("Limpieza", "Desactivar las tres cuentas de prueba", async () =>
        {
            Exigir(await _s.Usuarios.DesactivarAsync(c.supervisor, c.ciudadano), "ciudadano");
            Exigir(await _s.Usuarios.DesactivarAsync(c.supervisor, c.operario), "operario");
            Exigir(await _s.Usuarios.DesactivarAsync(admin, c.supervisor), "supervisor");
            return (0, $"ids {c.ciudadano}, {c.operario}, {c.supervisor} inactivos (se conservan por la trazabilidad de sus depósitos)");
        });
        await Esperar("Limpieza", "Una cuenta desactivada ya no puede iniciar sesión",
            _s.Autenticacion.IniciarSesionPublicaAsync($"validacion.final.ciudadano.{Marca}@ecoaccess.test", c.clave), false);
    }

    // ---------------------------------------------------------------- Robustez
    static async Task Robustez()
    {
        await Ejecutar("Robustez", "Sin conexión: Oracle inalcanzable da un error controlado", async () =>
        {
            var rota = new OracleConnectionStringBuilder(_cadena) { DataSource = "127.0.0.1:1/NOEXISTE", ConnectionTimeout = 5, Pooling = false };
            var s = EcoAccess.P3.Presentacion.Program.ComponerServicios(rota.ConnectionString, null);
            var reloj = Stopwatch.StartNew();
            try
            {
                var r = await s.Autenticacion.IniciarSesionPublicaAsync("alguien@ecoaccess.test", "Clave#12345678");
                if (r.Exitoso) throw new InvalidOperationException("¡inició sesión sin base de datos!");
                return (0, $"rechazo controlado en {reloj.Elapsed.TotalSeconds:N1} s: {r.Mensaje}");
            }
            catch (Exception e) when (e is not InvalidOperationException)
            {
                return (0, $"excepción {e.GetType().Name} en {reloj.Elapsed.TotalSeconds:N1} s (la GUI la muestra como error de conexión)");
            }
        });
        await Esperar("Robustez", "Invitado NO puede consultar ecopuntos", _s.Ecopuntos.ConsultarMiosAsync(0), false);
    }

    // ---------------------------------------------------------------- utilidades
    static async Task<OracleConnection> Abrir() { var c = new OracleConnection(_cadena); await c.OpenAsync(); return c; }
    static async Task<object?> Escalar(string sql) { using var c = await Abrir(); return await EscalarEn(c, sql); }
    static async Task<object?> EscalarEn(OracleConnection c, string sql) { using var cmd = c.CreateCommand(); cmd.CommandText = sql; return await cmd.ExecuteScalarAsync(); }

    static T Exigir<T>(Resultado<T> r, string paso) => r.Exitoso ? r.Valor : throw new InvalidOperationException($"{paso}: {r.Mensaje}");
    static void Exigir(Resultado r, string paso) { if (!r.Exitoso) throw new InvalidOperationException($"{paso}: {r.Mensaje}"); }

    static async Task Esperar(string grupo, string prueba, Task<Resultado> tarea, bool exito)
    {
        try
        {
            var r = await tarea;
            Anotar(grupo, prueba, r.Exitoso == exito ? "CORRECTA" : "FALLIDA", r.Exitoso ? "aceptado" : "rechazado: " + r.Mensaje);
        }
        catch (Exception e) { Anotar(grupo, prueba, "FALLIDA", $"{e.GetType().Name}: {e.Message}"); }
    }
    static Task Esperar<T>(string grupo, string prueba, Task<Resultado<T>> tarea, bool exito) =>
        Esperar(grupo, prueba, tarea.ContinueWith(t => (Resultado)t.Result), exito);

    static async Task<T?> Ejecutar<T>(string grupo, string prueba, Func<Task<(T, string)>> accion)
    {
        try { var (valor, detalle) = await accion(); Anotar(grupo, prueba, "CORRECTA", detalle); return valor; }
        catch (Exception e) { Anotar(grupo, prueba, "FALLIDA", $"{e.GetType().Name}: {e.Message}"); return default; }
    }
    static async Task<int?> Ejecutar(string grupo, string prueba, Func<Task<(int, string)>> accion)
    {
        try { var (valor, detalle) = await accion(); Anotar(grupo, prueba, "CORRECTA", detalle); return valor; }
        catch (Exception e) { Anotar(grupo, prueba, "FALLIDA", $"{e.GetType().Name}: {e.Message}"); return null; }
    }

    static void Anotar(string grupo, string prueba, string estado, string detalle)
    {
        Resultados.Add((grupo, prueba, estado, detalle));
        Console.WriteLine($"[{estado}] {grupo} · {prueba}: {detalle}");
    }

    static int Informe()
    {
        Console.WriteLine();
        foreach (var g in Resultados.GroupBy(r => r.Estado)) Console.WriteLine($"{g.Key}: {g.Count()}");
        int fallidas = Resultados.Count(r => r.Estado == "FALLIDA");
        Console.WriteLine(fallidas == 0 ? "RESULTADO: todas las comprobaciones ejecutadas fueron correctas." : $"RESULTADO: {fallidas} comprobación(es) fallida(s).");
        return fallidas == 0 ? 0 : 1;
    }
}
