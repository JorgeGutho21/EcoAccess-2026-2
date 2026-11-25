// Challenge: solo enlaza al juego publicado en /juego/ (web/juego, mantenido aparte). No copia el juego.
import { cabecera } from './comun.js';

export const titulo = 'EcoAccess Challenge';

export function render() {
  return `${cabecera({ titulo, entrada: 'El juego oficial de EcoAccess para ExpoSoftware 2026: preguntas sobre residuos y sobre cómo se construyó el proyecto.' })}
  <section class="reto" aria-labelledby="r-titulo">
    <div style="display:grid;gap:16px">
      <h2 id="r-titulo">Pon a prueba tus conocimientos</h2>
      <p>Regístrate con un alias, responde las preguntas y compara tu resultado en la clasificación. El tiempo y el puntaje los calcula el servidor.</p>
      <div class="acciones-reto">
        <a class="btn-principal" href="./juego/">Jugar el Challenge</a>
        <a class="btn-sec" href="./juego/clasificacion/">Ver la clasificación</a>
      </div>
    </div>
    <img src="assets/marca/eco-celebra-200.webp" alt="" width="200" height="200" loading="lazy">
  </section>
  <section class="bloque prosa" aria-labelledby="r-como">
    <h2 id="r-como">Cómo funciona</h2>
    <ul>
      <li>La ronda publicada tiene 7 preguntas: 5 sencillas y 2 avanzadas, sobre Oracle y C#.</li>
      <li>La respuesta correcta nunca llega a tu navegador antes de responder; la revisión se abre cuando termina la competencia.</li>
      <li>El juego no da ecopuntos ni abre canecas: usa sus propias tablas en Oracle.</li>
    </ul>
    <p class="nota-fuente">El Challenge se publica en esta misma página, bajo <code>/juego/</code>. Fuente: <code>reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md</code> §4.</p>
  </section>`;
}
