// Inteligencia artificial. Fuentes: src/EcoAccess.P3.Bot/Vision/ClasificadorOllamaLocal.cs, web/ciudadano/js/motor.js y
// voz.js, y la evaluación real del 25/11/2026 en reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md §6.
import { cabecera, sello, codigo, escapar } from './comun.js';

export const titulo = 'Inteligencia artificial';
const MIGAS = [['Tecnología', '#/tecnologia']];

const PIPELINE = [
  ['Foto', 'La persona envía una foto al bot o la toma la aplicación de escritorio.'],
  ['Preprocesamiento', 'La imagen se reduce a 512 px por el lado mayor y se revisa su calidad; si es insuficiente, no se clasifica.'],
  ['Ollama con lfm3-vl', 'Un modelo de visión y lenguaje que corre en el mismo equipo (127.0.0.1), sin nube, con temperatura 0 para que responda igual ante la misma foto.'],
  ['JSON', 'El modelo debe responder un JSON que cumpla un esquema. Si no lo cumple, la respuesta se descarta.'],
  ['Reglas', 'C# valida el JSON: el material tiene que existir en el catálogo cerrado de EcoAccess. Lo que no esté en la lista no cuenta.'],
  ['Categoría', 'Las reglas, no el modelo, deciden la categoría y la caneca. Los residuos especiales (pilas, medicamentos, cortopunzantes…) nunca abren una caneca.'],
  ['Incertidumbre', 'Si hay duda, EcoAccess hace una segunda pregunta de verificación o pide a la persona que confirme. Ante la duda, no abre.'],
];

const METRICAS = [
  { n: '25 fotos reales de ajuste', ok: 25, mal: 0, total: 25, e: 'verificado', et: 'Sin valor de generalización', nota: 'Son las mismas fotos con las que se ajustó el sistema: acertarlas no demuestra que funcione con fotos nuevas.' },
  { n: '9 fotos negativas (no debe abrir)', ok: 8, mal: 1, total: 9, e: 'error', nota: 'Una falsa apertura: una bolsa plástica se leyó como servilleta y se abrió la caneca negra.' },
  { n: '10 escenas compuestas', ok: 6, mal: 4, total: 10, e: 'error', nota: 'Con varios residuos en la misma foto hubo falsas aperturas, por ejemplo verde en lugar de blanca y negra.' },
];

export function render() {
  return `${cabecera({ titulo, migas: MIGAS, entrada: 'EcoAccess entiende texto, voz e imágenes. La IA <em>propone</em>; las reglas del proyecto <em>deciden</em>. Y sus errores medidos se publican.' })}

  <section class="bloque" aria-labelledby="i-entradas">
    <h2 id="i-entradas">Tres formas de entrada</h2>
    <div class="rejilla">
      <div class="dato"><b>Texto</b><span>Un motor determinista en JavaScript (<code>js/motor.js</code>) busca en un catálogo de residuos con alias colombianos, plurales y errores de escritura. Funciona en tu navegador, sin servidor y sin Ollama.</span>${sello('verificado', 'Pruebas del motor pasan')}</div>
      <div class="dato"><b>Voz</b><span>El dictado del navegador (español de Colombia) convierte la voz en texto y pasa al mismo motor. Solo se actúa con la frase final, nunca con una parcial.</span>${sello('pendiente', 'Micrófono real sin probar')}</div>
      <div class="dato"><b>Imagen</b><span>El bot y la aplicación de escritorio envían la foto al modelo lfm3-vl en Ollama, en el equipo de la estación.</span>${sello('error', 'Con limitaciones medidas')}</div>
    </div>
    <p><a href="#/">Prueba el reconocimiento escrito en el inicio</a>.</p>
  </section>

  <section class="bloque" aria-labelledby="i-pipeline">
    <h2 id="i-pipeline">De la foto a la categoría</h2>
    <ol class="recorrido" id="pipeline">${PIPELINE.map(([t, d]) => `<li><span><b>${escapar(t)}</b>${escapar(d)}</span></li>`).join('')}</ol>
    <div class="controles"><button class="btn-sec" type="button" id="pipelineBtn">Recorrer el proceso</button><span class="sr" aria-live="polite" id="pipelineSr"></span></div>
    ${codigo('src/EcoAccess.P3.Bot/Vision/ClasificadorOllamaLocal.cs', `
public const string Modelo = "lfm3-vl:latest";
public const string Servidor = "http://127.0.0.1:11434";
// …
/// <summary>Lado mayor de la imagen enviada al modelo: suficiente para distinguir el objeto y barato de codificar.</summary>
private const int LadoImagen = 512;
// …
ImagenResiduo preparada = PreparadorImagen.Preparar(imagen, LadoImagen);
if (CalidadImagen.Insuficiente(preparada.Contenido)) return null;`)}
  </section>

  <section class="bloque prosa" aria-labelledby="i-entrena">
    <h2 id="i-entrena">Guardar fotos no entrena el modelo</h2>
    <p>Cuando EcoAccess guarda una foto, lo hace como evidencia de lo que pasó, no para aprender. El modelo lfm3-vl no cambia con el uso: sus pesos son los mismos antes y después de cada foto. «Ajustar» en este proyecto significa cambiar las instrucciones, el catálogo y las reglas en C#, y volver a medir.</p>
  </section>

  <section class="bloque" aria-labelledby="i-metricas">
    <h2 id="i-metricas">Lo que se midió el 25/11/2026</h2>
    <p>Evaluación real con fotos del proyecto, el modelo a 512 px y sin hardware. Son limitaciones abiertas, no resueltas.</p>
    <div class="lamina">
      ${METRICAS.map((m) => `<div class="medidor">
        <div class="rotulo"><span>${escapar(m.n)}</span><b>${m.ok}/${m.total}</b></div>
        <div class="barra" role="img" aria-label="${m.ok} correctas de ${m.total}${m.mal ? `, ${m.mal} con error` : ''}"><i style="width:${(m.ok / m.total) * 100}%"></i>${m.mal ? `<i class="mal" style="width:${(m.mal / m.total) * 100}%"></i>` : ''}</div>
        <p>${sello(m.e, m.et)} ${escapar(m.nota)}</p>
      </div>`).join('')}
      <div class="medidor">
        <div class="rotulo"><span>Tiempo de respuesta (modelo ya cargado)</span><b>mediana 2,37 s · P95 5,5 s</b></div>
        <div class="barra" role="img" aria-label="Mediana 2,37 segundos y percentil 95 de 5,5 segundos sobre una escala de 0 a 6 segundos; la meta era de 1 a 2 segundos">
          <i style="width:${(2 / 6) * 100}%;background:var(--hoja)"></i><i class="mal" style="width:${(0.37 / 6) * 100}%"></i><i style="width:${(3.13 / 6) * 100}%;background:#E6B9B4"></i>
        </div>
        <p>${sello('error')} La meta era de 1 a 2 s. Los casos lentos (5 a 7 s) son los que hacen una segunda pregunta de verificación. Verde: meta; rojo: mediana por encima; rosado: hasta el P95.</p>
      </div>
    </div>
    <h3>Lo que falta para una evaluación honesta</h3>
    <ul>
      <li>Fotos nuevas, de otros objetos y otros días, para cada material (la cáscara de banano hoy tiene 0 fotos reales).</li>
      <li>Fotos negativas de bolsa plástica blanca arrugada y de papel blanco liso, que se confunden con la servilleta.</li>
      <li>Escenas reales con varios residuos, no compuestas.</li>
    </ul>
    <p>Se probó una verificación extra «¿es papel suave y no plástico?»: no evitó la falsa apertura y subió la latencia, así que se revirtió.</p>
    <p class="nota-fuente">Fuente: <code>reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md</code> §6 (<code>EcoAccess.P3.Bot.exe --evaluar-expo9</code>, lfm3-vl, 512 px).</p>
  </section>`;
}

export function montar(raiz) {
  const items = [...raiz.querySelectorAll('#pipeline li')];
  const btn = raiz.querySelector('#pipelineBtn');
  const sr = raiz.querySelector('#pipelineSr');
  let i = -1;
  btn.addEventListener('click', () => {
    i = (i + 1) % (items.length + 1);
    if (i === items.length) { items.forEach((li) => { li.className = ''; li.removeAttribute('aria-current'); }); btn.textContent = 'Recorrer el proceso'; sr.textContent = 'Recorrido reiniciado.'; i = -1; return; }
    items.forEach((li, k) => { li.className = k < i ? 'hecho' : k === i ? 'actual' : ''; if (k === i) li.setAttribute('aria-current', 'step'); else li.removeAttribute('aria-current'); });
    btn.textContent = i === items.length - 1 ? 'Reiniciar' : 'Siguiente paso';
    sr.textContent = `Paso ${i + 1} de ${items.length}: ${PIPELINE[i][0]}. ${PIPELINE[i][1]}`;
  });
}
