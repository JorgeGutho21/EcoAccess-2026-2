// Programación III y SOLID. Todos los fragmentos son copias breves de código real del repositorio (con «// …» donde
// se omiten líneas). Archivos revisados el 25/11/2026 sobre la rama integracion/ecoaccess-final.
import { cabecera, sello, codigo, pestanas } from './comun.js';
import { caja, relacion, lienzo, definiciones } from './uml.js';

export const titulo = 'Programación III';
const MIGAS = [['Tecnología', '#/tecnologia']];

function diagramaHerencia() {
  const control = caja(300, 8, 160, 'Control', { estereotipo: 'Windows Forms', tono: 'externo' });
  const base = caja(230, 84, 300, 'TarjetaDibujada', { estereotipo: 'abstract', tono: 'abstracta',
    miembros: ['# Superficie : Color', '+ MedirAlto(ancho) : int  {abstract}', '# PintarContenido(g)  {abstract}'] });
  const hijos = ['TarjetaCaneca', 'TarjetaResiduo', 'TarjetaIndicador', 'TarjetaDato'].map((n, i) =>
    caja(14 + i * 186, 236, 174, n, { miembros: ['override MedirAlto', 'override PintarContenido'] }));
  return lienzo(760, 320, 'Diagrama de clases: herencia de TarjetaDibujada',
    'TarjetaDibujada es abstracta y hereda de Control. TarjetaCaneca, TarjetaResiduo, TarjetaIndicador y TarjetaDato heredan de ella y sobrescriben MedirAlto y PintarContenido.',
    control.svg + base.svg + hijos.map((h) => h.svg).join('')
    + relacion(base.cx, base.arriba, control.cx, control.abajo, 'herencia')
    + hijos.map((h) => relacion(h.cx, h.arriba, base.cx, base.abajo, 'herencia')).join(''));
}

function diagramaInterfaz() {
  const i = caja(220, 8, 320, 'IClasificadorMaterial', { estereotipo: 'interface', tono: 'interfaz',
    miembros: ['+ Proveedor : string', '+ ClasificarAsync(imagen, catalogo, ct)'] });
  const impl = [['ClasificadorOllamaLocal', 'Bot/Vision'], ['ClasificadorGroq', 'Bot/Vision'], ['SelectorVision', 'Bot/Vision'], ['ClasificadorMaterialSimulado', 'Logica/Servicios']]
    .map(([n, p], k) => caja(10 + k * 188, 180, 178, n, { estereotipo: p }));
  return lienzo(760, 240, 'Diagrama de clases: implementaciones de IClasificadorMaterial',
    'Cuatro de las clases que implementan IClasificadorMaterial (en el repositorio hay seis): el clasificador local con Ollama, uno externo, un selector y un simulador.',
    i.svg + impl.map((c) => c.svg).join('') + impl.map((c) => relacion(c.cx, c.arriba, i.cx, i.abajo, 'realiza')).join(''));
}

const SOLID = [
  {
    l: 'S', n: 'Responsabilidad única', e: 'verificado', et: 'Aplicado',
    def: 'Una clase debe tener una sola razón para cambiar.',
    dia: () => {
      const a = caja(20, 20, 230, 'ICanalDispositivo', { estereotipo: 'interface', tono: 'interfaz', miembros: ['+ Canal : string', '+ EnviarAperturaAsync(cmd, ct)'] });
      const b = caja(300, 20, 200, 'Material', { miembros: ['valida su código y nombre', 'Habilitar() / Deshabilitar()'] });
      const c = caja(540, 20, 200, 'RepositorioMateriales…', { estereotipo: 'Datos', miembros: ['solo SQL de MATERIAL'] });
      return lienzo(760, 120, 'Responsabilidades separadas', 'Canal entrega comandos; Material valida sus datos; el repositorio solo hace SQL.', a.svg + b.svg + c.svg);
    },
    ej: codigo('src/EcoAccess.P3.Logica/Contratos/ICanalDispositivo.cs', `
/// Canal hacia la caneca, independiente del medio (simulador, HTTP local, Serial/USB). Solo
/// entrega comandos: no tiene acceso a repositorios ni a SQL, y la IA no lo usa.
public interface ICanalDispositivo
{
    string Canal { get; }
    Task<bool> EnviarAperturaAsync(ComandoApertura comando, CancellationToken cancellationToken = default);
}`),
    ben: 'El canal hacia la caneca cambia solo si cambia el medio (USB, HTTP, simulador); las reglas de negocio y el SQL viven en otras clases.',
    mej: '<code>CoordinadorEstacionUnificado</code> (Logica/Vision35) serializa aperturas, guarda historial, controla la sesión física y además lee la variable de entorno de chats autorizados en su constructor. Separar la lectura de configuración y el historial lo dejaría con una sola razón para cambiar.',
  },
  {
    l: 'O', n: 'Abierto/cerrado', e: 'verificado', et: 'Aplicado',
    def: 'Abierto a la extensión, cerrado a la modificación: se agrega comportamiento nuevo sin editar el que ya funciona.',
    dia: diagramaInterfaz,
    ej: codigo('src/EcoAccess.P3.Logica/Contratos/IClasificadorMaterial.cs', `
/// Clasificador de fotos de residuos, independiente del proveedor (simulador, modelo local o API externa).
public interface IClasificadorMaterial
{
    string Proveedor { get; }
    Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken cancellationToken = default);
}`),
    ben: 'Pasar de un modelo a otro (por ejemplo, del simulador a Ollama con lfm3-vl) fue agregar una clase que implementa la interfaz, sin tocar quien la usa.',
    mej: 'Las reglas de combinación de canecas están en clases estáticas (<code>MotorCombinacionesCanecas</code>); si aparecieran estaciones con otras canecas, convendría expresarlas como una estrategia inyectable.',
  },
  {
    l: 'L', n: 'Sustitución de Liskov', e: 'pendiente', et: 'Parcial',
    def: 'Un subtipo debe poder usarse donde se espera su tipo base sin romper el programa.',
    dia: diagramaHerencia,
    ej: codigo('src/EcoAccess.P3.Presentacion/ComponentesExpo.cs', `
internal sealed class TarjetaCaneca : TarjetaDibujada
{
    // …
    internal override int MedirAlto(int ancho)
    // …
    protected override void PintarContenido(Graphics g)`),
    ben: 'El contenedor de tarjetas reparte columnas llamando a <code>MedirAlto</code> sin saber qué tarjeta concreta tiene: cualquier subclase sirve.',
    mej: '<code>IEjecutorAperturas</code> expone <code>EsSimulado</code> y el coordinador decide según esa bandera. Que el cliente pregunte «¿qué tipo eres?» es una señal de que los dos ejecutores no son del todo intercambiables. Separar el ejecutor físico en su propio contrato aclararía la sustitución.',
  },
  {
    l: 'I', n: 'Segregación de interfaces', e: 'pendiente', et: 'Parcial',
    def: 'Ningún cliente debe depender de métodos que no usa: mejor varias interfaces pequeñas que una grande.',
    dia: () => {
      const a = caja(20, 20, 220, 'IEjecutorAperturas', { estereotipo: 'interface', tono: 'interfaz', miembros: ['+ EsSimulado : bool', '+ AbrirAsync(cmd, ct)'] });
      const b = caja(280, 20, 220, 'ICanalDispositivo', { estereotipo: 'interface', tono: 'interfaz', miembros: ['+ Canal : string', '+ EnviarAperturaAsync(cmd, ct)'] });
      const c = caja(540, 20, 200, 'IRepositorioMateriales', { estereotipo: 'interface', tono: 'interfaz', miembros: ['ObtenerPorIdAsync', 'ExisteCodigoAsync', 'ListarAsync', 'ConsultarPaginaAsync', 'AgregarAsync', 'HabilitarAsync', '…'] });
      const d = caja(540, 230, 200, 'IRepositorioMaterialesLectura', { estereotipo: 'propuesta', tono: 'mejora', miembros: ['ListarAsync'] });
      return lienzo(760, 320, 'Interfaces pequeñas y una interfaz grande', 'IEjecutorAperturas e ICanalDispositivo tienen uno o dos miembros. IRepositorioMateriales reúne lectura y escritura; se propone separar la lectura.',
        a.svg + b.svg + c.svg + d.svg + relacion(d.cx, d.arriba, c.cx, c.abajo, 'usa', 'separar'));
    },
    ej: codigo('src/EcoAccess.P3.Logica/Contratos/IEjecutorAperturas.cs', `
public interface IEjecutorAperturas
{
    bool EsSimulado { get; }

    Task<ResultadoEjecucionApertura> AbrirAsync(ComandoApertura comando, CancellationToken cancellationToken = default);
}`) + '<p>La parte que falta se ve en la evaluación de visión: un catálogo de solo lectura tiene que implementar también los métodos de escritura, y lo resuelve lanzando excepciones.</p>' + codigo('src/EcoAccess.P3.Bot/Vision/EvaluacionVisionArchivos.cs', `
private sealed class CatalogoInmutable : IRepositorioMateriales
{
    // …
    public Task<int> AgregarAsync(Material m, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task HabilitarAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task DeshabilitarAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();`),
    ben: 'Los contratos de hardware son mínimos: el simulador y el ejecutor serie con HMAC solo implementan lo que necesitan.',
    mej: '<code>IRepositorioMateriales</code> mezcla consulta, paginación, alta y habilitación; por eso <code>CatalogoInmutable</code> lanza <code>NotSupportedException</code>. Propuesta: una interfaz de lectura y otra de administración.',
  },
  {
    l: 'D', n: 'Inversión de dependencias', e: 'verificado', et: 'Aplicado',
    def: 'Los módulos de alto nivel no dependen de los de bajo nivel; ambos dependen de abstracciones.',
    dia: () => {
      const i = caja(250, 14, 260, 'IRepositorioMateriales', { estereotipo: 'interface · Logica/Contratos', tono: 'interfaz' });
      const r = caja(250, 150, 260, 'RepositorioMaterialesOracle', { estereotipo: 'Datos/Repositorios' });
      const o = caja(570, 150, 170, 'Oracle', { estereotipo: 'ODP.NET', tono: 'externo' });
      const s = caja(20, 150, 180, 'Servicios de Logica', { tono: 'capa' });
      return lienzo(760, 210, 'Inversión de dependencias en el catálogo', 'Los servicios de Lógica usan la interfaz IRepositorioMateriales; la capa Datos la implementa con Oracle. Lógica no referencia a Datos.',
        i.svg + r.svg + o.svg + s.svg + relacion(r.cx, r.arriba, i.cx, i.abajo, 'realiza') + relacion(s.cx, s.arriba, i.x + 20, i.abajo, 'usa', 'usa')
        + relacion(r.x + r.w, r.y + 20, o.x, o.y + 20, 'usa'));
    },
    ej: codigo('src/EcoAccess.P3.Datos/Repositorios/RepositorioMaterialesOracle.cs', `
public sealed partial class RepositorioMaterialesOracle : IRepositorioMateriales
{
    private readonly FabricaConexionesOracle _fabrica;

    public RepositorioMaterialesOracle(FabricaConexionesOracle fabrica)
    {
        _fabrica = fabrica;
    }`),
    ben: 'En los proyectos, <code>EcoAccess.P3.Datos</code> referencia a <code>EcoAccess.P3.Logica</code> y no al revés: la lógica se prueba sin Oracle usando dobles de prueba.',
    mej: 'El coordinador lee <code>Environment.GetEnvironmentVariable</code> directamente. Recibir esa configuración por el constructor completaría la inversión y facilitaría las pruebas.',
  },
];

export function render() {
  return `${definiciones}${cabecera({ titulo, migas: MIGAS, entrada: 'Los conceptos del curso, mostrados en el código real de EcoAccess. Cada fragmento es breve y cita su archivo; donde el código no demuestra un principio, se dice.' })}

  <section class="bloque" aria-labelledby="p-clases">
    <h2 id="p-clases">Clases, objetos y encapsulamiento</h2>
    <p>Una clase describe un concepto del dominio; cada material del catálogo es un objeto de la clase <code>Material</code>. Sus propiedades se leen desde fuera, pero <code>Habilitado</code> solo cambia con los métodos de la propia clase: eso es encapsulamiento.</p>
    ${codigo('src/EcoAccess.P3.Entidades/Material.cs', `
public class Material
{
    public string Codigo { get; }
    public string Nombre { get; }
    public CategoriaMaterial Categoria { get; }
    // …
    public bool Habilitado { get; private set; }
    // …
    // Deshabilitar retira el material de nuevas clasificaciones sin borrar los depósitos que ya lo usaron.
    public void Habilitar() => Habilitado = true;

    public void Deshabilitar() => Habilitado = false;`)}
  </section>

  <section class="bloque" aria-labelledby="p-validacion">
    <h2 id="p-validacion">Validaciones y excepciones</h2>
    <p>El constructor no deja crear un objeto inválido: valida y, si algo falla, lanza una excepción con un mensaje claro.</p>
    ${codigo('src/EcoAccess.P3.Entidades/Material.cs', `
Codigo = NormalizarCodigo(codigo, nameof(codigo));
Nombre = Validacion.TextoRequerido(nombre, nameof(nombre), LongitudMaximaNombre, LongitudMinimaNombre);
Categoria = Validacion.EnumDefinido(categoria, nameof(categoria));
// …
throw new ArgumentException(
    "El código solo admite letras sin tilde, números y guion bajo, y debe empezar por una letra.", nombreParametro);`)}
    <p>También hay excepciones propias del dominio, como <code>ConflictoUnicidadException</code>, <code>CambioConcurrenteException</code> y <code>UltimoAdministradorException</code> en <code>Logica/Contratos</code>: permiten que la lógica responda «duplicado» sin conocer los códigos de error de Oracle.</p>
    ${codigo('src/EcoAccess.P3.Logica/Contratos/ConflictoUnicidadException.cs', `
public sealed class ConflictoUnicidadException : Exception
{
    public ConflictoUnicidadException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
}`)}
    <p>${sello('pendiente', 'Mejora')} En <code>CoordinadorEstacionUnificado.SolicitarAperturaAsync</code> un <code>catch</code> sin tipo convierte cualquier error del ejecutor en «sin respuesta». Es seguro (no reintenta una apertura incierta), pero ocultar la causa dificulta el diagnóstico: registrar la excepción ayudaría.</p>
  </section>

  <section class="bloque" aria-labelledby="p-abstraccion">
    <h2 id="p-abstraccion">Abstracción, herencia y polimorfismo</h2>
    <p>Sí existen en el código. <code>TarjetaDibujada</code> es una clase abstracta: define qué debe saber hacer toda tarjeta (medir su alto y pintarse) sin decir cómo. Cada tarjeta concreta hereda de ella y sobrescribe esos métodos; quien las dibuja las trata a todas por igual. Eso es polimorfismo.</p>
    ${codigo('src/EcoAccess.P3.Presentacion/ComponentesExplorar.cs', `
internal abstract class TarjetaDibujada : Control
{
    // …
    /// <summary>Alto que necesita la tarjeta para mostrar todo su contenido con ese ancho.</summary>
    internal abstract int MedirAlto(int ancho);
    // …
    protected abstract void PintarContenido(Graphics g);`)}
    <div class="lamina">${diagramaHerencia()}<p class="lamina-pie">Triángulo hueco con línea continua: herencia. Nombre en cursiva: clase abstracta.</p></div>
  </section>

  <section class="bloque" aria-labelledby="p-interfaces">
    <h2 id="p-interfaces">Interfaces</h2>
    <p>Una interfaz es un contrato. Solo <code>Logica/Contratos</code> define 31 (por ejemplo, <code>IRepositorioMateriales</code> o <code>IEjecutorAperturas</code>), y el bot agrega otras como <code>IClienteTelegram</code>. Así el reconocimiento por foto puede venir de Ollama, de otro proveedor o de un simulador para pruebas.</p>
    <div class="lamina">${diagramaInterfaz()}<p class="lamina-pie">Borde punteado: interfaz. Línea punteada con triángulo hueco: «implementa».</p></div>
  </section>

  <section class="bloque" aria-labelledby="p-colecciones">
    <h2 id="p-colecciones">Colecciones y estructuras de datos</h2>
    <p>El coordinador de la estación elige cada colección por lo que necesita garantizar:</p>
    ${codigo('src/EcoAccess.P3.Logica/Vision35/CoordinadorEstacionUnificado.cs', `
private readonly SemaphoreSlim _colaEjecucion = new(1, 1);
private readonly HashSet<string> _clavesEjecutadas = new(StringComparer.Ordinal);
private readonly HashSet<long> _chatsAutorizados = new();
private readonly List<RegistroOperacionEstacion> _historial = new();
// …
private readonly IReadOnlyList<CanecaEstacion> _estacion;`)}
    <ul>
      <li><strong><code>HashSet&lt;string&gt;</code></strong>: <code>Add</code> devuelve <code>false</code> si la clave ya estaba. Así una solicitud repetida no abre la caneca dos veces (idempotencia), en tiempo constante.</li>
      <li><strong><code>List&lt;T&gt;</code></strong>: historial en orden de llegada.</li>
      <li><strong><code>IReadOnlyList&lt;T&gt;</code></strong>: la configuración de la estación se consulta pero no se modifica desde fuera.</li>
      <li><strong><code>SemaphoreSlim(1, 1)</code></strong>: una cola de un solo turno; nunca dos operaciones compiten por el puerto USB del ESP32.</li>
      <li><strong><code>record</code> y <code>enum</code></strong>: <code>RegistroOperacionEstacion</code> es un registro inmutable y <code>CategoriaMaterial</code> limita las categorías a cuatro valores.</li>
    </ul>
  </section>

  <section class="bloque" aria-labelledby="p-async">
    <h2 id="p-async">Programación asíncrona</h2>
    <p>Hablar con Oracle, con Ollama o con el ESP32 tarda. Con <code>async</code>/<code>await</code> el programa espera sin bloquear la interfaz, y el <code>CancellationToken</code> permite cancelar.</p>
    ${codigo('src/EcoAccess.P3.Logica/Vision35/CoordinadorEstacionUnificado.cs', `
await _colaEjecucion.WaitAsync(ct).ConfigureAwait(false);
try
{
    if (!_clavesEjecutadas.Add(claveIdempotencia))
    {
        // … «Solicitud duplicada bloqueada por idempotencia (no se repitió la apertura).»
        return dup;
    }`)}
  </section>

  <section class="bloque" aria-labelledby="p-solid">
    <h2 id="p-solid">SOLID en EcoAccess</h2>
    <p>Cinco principios de diseño, revisados contra el código real el 25/11/2026. Cada uno lleva un sello: <strong>aplicado</strong> (hay un ejemplo en el código que lo cumple), <strong>parcial</strong> (se cumple en una parte y se incumple en otra) y una <strong>oportunidad</strong> concreta de mejora. Tres están aplicados y dos son parciales.</p>
    <div id="solid">
      <div class="pestanas" role="tablist" aria-label="Principios SOLID">
        ${SOLID.map((s, i) => `<button role="tab" id="tab-solid-${i}" aria-controls="panel-solid-${i}" aria-selected="${i === 0}" type="button"><span class="letra" aria-hidden="true">${s.l}</span>${s.n}</button>`).join('')}
      </div>
      ${SOLID.map((s, i) => `<div class="lamina" role="tabpanel" id="panel-solid-${i}" aria-labelledby="tab-solid-${i}" tabindex="0" ${i ? 'hidden' : ''}>
        <h3>${s.n} ${sello(s.e, s.et)}</h3>
        <p><strong>Qué es:</strong> ${s.def}</p>
        ${s.dia()}
        <h4>Ejemplo real</h4>
        ${s.ej}
        <p><strong>Beneficio:</strong> ${s.ben}</p>
        <p>${sello('propuesta', 'Oportunidad')} ${s.mej}</p>
      </div>`).join('')}
    </div>
  </section>`;
}

export function montar(raiz) { pestanas(raiz.querySelector('#solid')); }
