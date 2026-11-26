// Voz: dictado (Web Speech API, es-CO) y respuestas habladas de ECO. No se graba ni se guarda audio.
// Chrome y Edge transcriben en sus servidores; Safari (iOS 14.5+) usa el dictado del sistema. Firefox no lo ofrece:
// en ese caso la interfaz ofrece la entrada escrita.

const SR = globalThis.SpeechRecognition || globalThis.webkitSpeechRecognition;

export const dictadoDisponible = () => Boolean(SR) && globalThis.isSecureContext !== false;

const MENSAJES_ERROR = {
  'not-allowed': 'No tengo permiso para usar el micrófono. Puedes permitirlo en el candado de la barra de direcciones o escribir tu residuo.',
  'service-not-allowed': 'El dictado no está habilitado en este navegador. Escribe tu residuo, por favor.',
  'audio-capture': 'No encontré un micrófono disponible. Escribe tu residuo, por favor.',
  'no-speech': 'No te escuché. Toca el micrófono e inténtalo de nuevo, o escribe tu residuo.',
  network: 'El dictado necesita conexión a internet. Escribe tu residuo, por favor.',
  aborted: '',
};

/**
 * Escucha una frase. Solo entrega el texto FINAL (onFinal); los parciales (onParcial) son solo para mostrarlos.
 * Nunca se debe actuar sobre un parcial.
 */
export function escuchar({ onParcial, onFinal, onError, onFin }) {
  if (!SR) { onError?.('No disponible', 'Tu navegador no ofrece dictado por voz. Escribe tu residuo, por favor.'); onFin?.(); return { detener() {} }; }
  const r = new SR();
  r.lang = 'es-CO';
  r.interimResults = true;
  r.continuous = false;
  r.maxAlternatives = 3;
  let entregado = false; let ultimoParcial = '';
  const limite = setTimeout(() => { try { r.stop(); } catch { /* ya detenido */ } }, 12000);
  r.onresult = (e) => {
    let parcial = '';
    for (let i = e.resultIndex; i < e.results.length; i++) {
      const res = e.results[i];
      if (res.isFinal && !entregado) {
        // Con ruido, elige la alternativa con mayor confianza que no esté vacía.
        const alternativas = Array.from(res).filter((a) => a.transcript.trim());
        alternativas.sort((a, b) => (b.confidence || 0) - (a.confidence || 0));
        entregado = true;
        onFinal?.(alternativas[0]?.transcript.trim() ?? '', alternativas.map((a) => a.transcript.trim()));
      } else if (!res.isFinal) parcial += res[0].transcript;
    }
    if (parcial && parcial !== ultimoParcial) { ultimoParcial = parcial; onParcial?.(parcial.trim()); }
  };
  r.onerror = (e) => { const m = MENSAJES_ERROR[e.error] ?? 'No pude usar el dictado. Escribe tu residuo, por favor.'; if (m) onError?.(e.error, m); };
  r.onend = () => { clearTimeout(limite); onFin?.(entregado); };
  try { r.start(); } catch (e) { clearTimeout(limite); onError?.('inicio', 'No pude iniciar el micrófono. Escribe tu residuo, por favor.'); onFin?.(false); }
  return { detener() { try { r.stop(); } catch { /* ya detenido */ } } };
}

// ── Respuestas habladas ──
let vozElegida = null;
function elegirVoz() {
  const voces = globalThis.speechSynthesis?.getVoices?.() ?? [];
  vozElegida = voces.find((v) => /es[-_]CO/i.test(v.lang))
    || voces.find((v) => /es[-_](419|MX|US)/i.test(v.lang))
    || voces.find((v) => /^es/i.test(v.lang)) || null;
}
if (globalThis.speechSynthesis) { elegirVoz(); speechSynthesis.addEventListener?.('voiceschanged', elegirVoz); }

export function hablar(texto, activo) {
  const s = globalThis.speechSynthesis;
  if (!activo || !s || !texto) return;
  s.cancel();
  const u = new SpeechSynthesisUtterance(texto);
  u.lang = vozElegida?.lang ?? 'es-CO';
  if (vozElegida) u.voice = vozElegida;
  u.rate = 1.02; u.pitch = 1.05;
  s.speak(u);
}
export const callar = () => globalThis.speechSynthesis?.cancel();
