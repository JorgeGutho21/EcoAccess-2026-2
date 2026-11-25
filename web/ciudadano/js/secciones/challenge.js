// Challenge: solo enlaza al juego publicado en /juego/ (web/juego, mantenido aparte). No copia el juego.
import { cabecera, sello } from './comun.js';

export const titulo = 'EcoAccess Challenge';

export function render() {
  return `${cabecera({ titulo, entrada: 'El juego oficial de EcoAccess para ExpoSoftware 2026: preguntas sobre residuos y sobre cómo se construyó el proyecto.' })}
  <section class="reto" aria-labelledby="r-titulo">
    <div class="reto-texto">
      <h2 id="r-titulo">¿Cuánto sabes de residuos y de EcoAccess?</h2>
      <p>Regístrate con un alias, responde las preguntas y compara tu resultado en la clasificación. El tiempo y el puntaje los calcula el servidor.</p>
      <div class="acciones-reto">
        <a class="btn-principal" id="jugarChallenge" href="./juego/">Pon a prueba tus conocimientos</a>
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
      <li>Hay una ronda de 10 preguntas preparada (migraciones V034 y V035), probada en un esquema aislado y sin aplicar en producción para no alterar la ronda oficial. ${sello('pendiente', 'No publicada')}</li>
      <li>Con jugadores que llegan escalonados terminaron 100 de 100 partidas; con 100 que empiezan en el mismo segundo, 99 de 100. ${sello('verificado', '100/100 escalonados')} ${sello('error', '99/100 simultáneos')} <a href="#/resultados">Ver resultados</a>.</li>
    </ul>
    <p class="nota-fuente">El Challenge se publica en esta misma página, bajo <code>/juego/</code>. Fuente: <code>reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md</code> §4, §5 y §14.2.</p>
  </section>`;
}
