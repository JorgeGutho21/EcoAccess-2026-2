// EcoAccess · Motor determinista de clasificación de residuos por texto o voz.
// Sin red, sin modelos: normaliza, quita tildes, singulariza, tolera errores ortográficos controlados, detecta varios
// materiales en una misma frase, residuos especiales (que nunca abren una caneca) y términos ambiguos (pide aclaración).

const VACIAS = new Set(['de', 'del', 'la', 'el', 'los', 'las', 'un', 'una', 'unos', 'unas', 'mi', 'mis', 'este', 'esta',
  'estos', 'estas', 'ese', 'esa', 'eso', 'lo', 'al', 'para', 'por', 'favor', 'que', 'es', 'son', 'muy', 'poco', 'poquito']);

// Frases de relleno al inicio de lo que dice la persona: «voy a botar…», «quiero desechar…», «tengo…».
const RELLENO = [
  /\b(hola|oye|eco|ecoaccess|buenas|buenos dias|buenas tardes|buenas noches|por favor|porfa|gracias)\b/g,
  /\b(yo\s+)?(voy|vamos|quiero|quisiera|necesito|debo|tengo que|me toca|puedo|como puedo|donde puedo|donde)\s+(a\s+)?(botar|tirar|desechar|echar|depositar|arrojar|reciclar|poner|dejar|meter|boto|tiro|echo|pongo)\b/g,
  /\b(donde\s+)?(se\s+)?(bota|botan|tira|tiran|echa|echan|pone|ponen|va|van|deposita|depositan)\b/g,
  /\b(tengo|traigo|llevo|cargo|hay|aqui tengo|aca tengo|me sobro|me quedo|es|son)\b/g,
  /\b(en que caneca|que caneca|cual caneca|en cual caneca|a que caneca)\b/g,
];

const SEPARADORES = /\s*(?:,|;|\by\b|\be\b|\btambien\b|\bmas\b|\badem[aá]s\b|\bjunto con\b|\bluego\b|\bdespues\b)\s*/g;
const SUCIO = /\b(sucio|sucia|sucios|sucias|grasoso|grasosa|engrasado|engrasada|contaminado|contaminada|con grasa|con comida|con restos|mojado|mojada|usado con comida)\b/;

export function normalizar(texto) {
  return String(texto ?? '')
    .toLowerCase()
    .normalize('NFD').replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9ñ\s,;]/g, ' ')
    .replace(/ñ/g, 'n')
    .replace(/\s+/g, ' ')
    .trim();
}

/**
 * Raíz ligera en español: singular, diminutivo largo y vocal final de género («plástico» = «plástica»).
 * Con «vocabulario» (palabras del catálogo) el singular se elige entre formas reales: «sobres» → «sobre», no «sobr».
 */
export function raiz(p, vocabulario) {
  let t = p;
  if (vocabulario) {
    // Prefiere siempre la forma singular real del catálogo, aunque el plural también aparezca en algún alias.
    if (t.endsWith('ces') && vocabulario.has(t.slice(0, -3) + 'z')) t = t.slice(0, -3) + 'z';
    else if (t.endsWith('es') && vocabulario.has(t.slice(0, -2))) t = t.slice(0, -2);
    else if (t.endsWith('s') && vocabulario.has(t.slice(0, -1))) t = t.slice(0, -1);
  }
  if (vocabulario && vocabulario.has(t)) { /* ya es una forma del catálogo */ }
  else if (t.length > 4 && t.endsWith('ces')) t = t.slice(0, -3) + 'z';
  else if (t.length > 4 && /[lnrdj]es$/.test(t)) t = t.slice(0, -2);
  else if (t.length > 3 && /[aeiou]s$/.test(t)) t = t.slice(0, -1);
  if (t.length >= 8 && /(it|ic)[ao]$/.test(t)) t = t.slice(0, -3);
  if (t.length >= 5 && /[ao]$/.test(t)) t = t.slice(0, -1);
  return t;
}

const palabras = (texto) => normalizar(texto).replace(/[,;]/g, ' ').split(' ').filter((w) => w && !VACIAS.has(w));
let VOCABULARIO = null;
const fichas = (texto) => palabras(texto).map((w) => raiz(w, VOCABULARIO));

/** Distancia de Damerau-Levenshtein (transposiciones incluidas), con corte temprano. */
export function distancia(a, b, max = 2) {
  if (Math.abs(a.length - b.length) > max) return max + 1;
  const d = Array.from({ length: a.length + 1 }, (_, i) => [i, ...Array(b.length).fill(0)]);
  for (let j = 1; j <= b.length; j++) d[0][j] = j;
  for (let i = 1; i <= a.length; i++) {
    let mejor = Infinity;
    for (let j = 1; j <= b.length; j++) {
      const c = a[i - 1] === b[j - 1] ? 0 : 1;
      d[i][j] = Math.min(d[i - 1][j] + 1, d[i][j - 1] + 1, d[i - 1][j - 1] + c);
      if (i > 1 && j > 1 && a[i - 1] === b[j - 2] && a[i - 2] === b[j - 1]) d[i][j] = Math.min(d[i][j], d[i - 2][j - 2] + 1);
      mejor = Math.min(mejor, d[i][j]);
    }
    if (mejor > max) return max + 1;
  }
  return d[a.length][b.length];
}

const tolerancia = (w) => (w.length >= 8 ? 2 : w.length >= 5 ? 1 : 0);

function coincide(q, a, difuso) {
  if (q === a) return 0;
  if (!difuso) return -1;
  const tol = Math.min(tolerancia(a), tolerancia(q));
  if (tol === 0) return -1;
  const dd = distancia(q, a, tol);
  return dd <= tol ? dd : -1;
}

/** Busca la secuencia de fichas «patron» dentro de «tokens», contigua. Devuelve [{inicio, fin, errores}]. */
function buscar(tokens, patron, difuso) {
  const r = [];
  for (let i = 0; i + patron.length <= tokens.length; i++) {
    let errores = 0, ok = true;
    for (let k = 0; k < patron.length; k++) {
      const e = coincide(tokens[i + k], patron[k], difuso);
      if (e < 0) { ok = false; break; }
      errores += e;
    }
    if (ok) r.push({ inicio: i, fin: i + patron.length, errores });
  }
  return r;
}

export function crearMotor(catalogo) {
  VOCABULARIO = new Set();
  for (const r of catalogo.residuos) for (const a of [r.nombre, ...r.alias]) palabras(a).forEach((w) => VOCABULARIO.add(w));
  for (const g of catalogo.genericos) palabras(g.termino).forEach((w) => VOCABULARIO.add(w));
  for (const e of catalogo.especiales) for (const t of e.terminos) palabras(t).forEach((w) => VOCABULARIO.add(w));
  const porId = new Map(catalogo.residuos.map((r) => [r.id, r]));
  const patrones = [];
  for (const r of catalogo.residuos) {
    const vistos = new Set();
    for (const alias of [r.nombre, ...r.alias]) {
      const f = fichas(alias); const k = f.join(' ');
      if (f.length && !vistos.has(k)) { vistos.add(k); patrones.push({ tipo: 'residuo', ref: r, f }); }
    }
  }
  for (const g of catalogo.genericos) patrones.push({ tipo: 'generico', ref: g, f: fichas(g.termino) });
  const especiales = [];
  for (const e of catalogo.especiales) for (const t of e.terminos) especiales.push({ ref: e, f: fichas(t) });

  function analizarSegmento(seg) {
    const tokens = fichas(seg);
    if (!tokens.length) return [];
    // 1) Coincidencias del catálogo: exactas y difusas (la difusa pesa casi igual que la exacta de igual longitud).
    const candidatos = [];
    for (const p of patrones) {
      for (const m of buscar(tokens, p.f, false)) candidatos.push({ ...m, p, puntos: 1000 * p.f.length - (p.tipo === 'generico' ? 500 : 0) });
    }
    const exactos = candidatos.slice();
    for (const p of patrones) {
      for (const m of buscar(tokens, p.f, true)) {
        if (m.errores === 0) continue;
        candidatos.push({ ...m, p, puntos: 1000 * p.f.length - 150 * m.errores - (p.tipo === 'generico' ? 500 : 0) });
      }
    }
    // 2) Residuos especiales: prioridad sobre el catálogo, salvo que una frase EXACTA más larga del catálogo los contenga
    //    («caja de medicamento vacía» es cartón). Las coincidencias difusas de especiales no ganan a una palabra exacta
    //    del catálogo («patilla» no es «pastilla»).
    for (const difuso of [false, true]) {
      for (const p of especiales) {
        if (difuso && p.f.some((w) => w.length < 7)) continue;
        for (const m of buscar(tokens, p.f, difuso)) {
          const cubierto = exactos.some((c) => c.p.tipo === 'residuo' && (difuso
            ? c.inicio < m.fin && m.inicio < c.fin
            : c.inicio <= m.inicio && c.fin >= m.fin && c.p.f.length > p.f.length));
          if (!cubierto) return [{ tipo: 'especial', especial: p.ref, texto: seg }];
        }
      }
    }
    candidatos.sort((a, b) => b.puntos - a.puntos || a.inicio - b.inicio);
    const usados = new Array(tokens.length).fill(false);
    const elegidos = [];
    for (const c of candidatos) {
      let libre = true;
      for (let i = c.inicio; i < c.fin; i++) if (usados[i]) { libre = false; break; }
      if (!libre) continue;
      // Empate real entre dos residuos distintos de canecas diferentes con la misma puntuación → ambigüedad.
      const rival = candidatos.find((o) => o !== c && o.puntos === c.puntos && o.inicio === c.inicio && o.fin === c.fin
        && o.p.tipo === 'residuo' && c.p.tipo === 'residuo' && o.p.ref.caneca !== c.p.ref.caneca);
      for (let i = c.inicio; i < c.fin; i++) usados[i] = true;
      elegidos.push({ c, rival });
    }
    elegidos.sort((a, b) => a.c.inicio - b.c.inicio);
    // Un genérico suelto junto a un residuo concreto del mismo tramo es parte de su descripción («caja de cartón con grasa»).
    if (elegidos.some((e) => e.c.p.tipo === 'residuo')) for (let i = elegidos.length - 1; i >= 0; i--) if (elegidos[i].c.p.tipo === 'generico') elegidos.splice(i, 1);
    const sucio = SUCIO.test(normalizar(seg));
    return elegidos.map(({ c, rival }) => {
      const corregido = c.p.f.length && c.puntos < 1000 * c.p.f.length - (c.p.tipo === 'generico' ? 500 : 0);
      if (rival) {
        return { tipo: 'ambiguo', texto: seg, pregunta: '¿A cuál te refieres?',
          opciones: [c.p.ref, rival.p.ref].map((r) => ({ id: r.id, texto: r.nombre, residuo: r })) };
      }
      if (c.p.tipo === 'generico') {
        const g = c.p.ref;
        if (g.caneca) return { tipo: 'residuo', texto: seg, corregido, residuo: { id: 'G-' + g.termino, nombre: g.nombre, caneca: g.caneca, oracle: g.oracle, condicion: g.condicion, generico: true } };
        return { tipo: 'ambiguo', texto: seg, pregunta: g.pregunta,
          opciones: g.opciones.map((o) => ({ ...o, residuo: o.id ? porId.get(o.id) : null })) };
      }
      let r = c.p.ref;
      if (sucio && r.siSucio && porId.has(r.siSucio)) r = porId.get(r.siSucio);
      return { tipo: 'residuo', texto: seg, corregido, residuo: r, avisoLimpieza: sucio && r.caneca === 'blanca' };
    });
  }

  /**
   * Clasifica una frase. Resultado:
   *  { estado: 'vacio' | 'reconocido' | 'ambiguo' | 'especial' | 'desconocido' | 'mixto', items, canecas, sugerencias }
   *  - items: un elemento por material detectado.
   *  - canecas: canecas DISTINTAS a abrir (en orden V, B, N), solo si no hay especiales ni ambigüedades.
   */
  function clasificar(texto) {
    let base = normalizar(texto);
    if (!base) return { estado: 'vacio', items: [], canecas: [] };
    for (const re of RELLENO) base = base.replace(re, ' ');
    base = base.replace(/\s+/g, ' ').trim();
    const segmentos = base.split(SEPARADORES).map((s) => s.trim()).filter(Boolean);
    const items = []; const sinReconocer = [];
    for (const s of segmentos) {
      const r = analizarSegmento(s);
      if (r.length) items.push(...r); else if (fichas(s).length) sinReconocer.push(s);
    }
    // Un mismo residuo nombrado dos veces cuenta una vez.
    const unicos = []; const claves = new Set();
    for (const it of items) {
      const k = it.tipo === 'residuo' ? 'r:' + it.residuo.id : it.tipo === 'especial' ? 'e:' + it.especial.id : 'a:' + it.texto;
      if (!claves.has(k)) { claves.add(k); unicos.push(it); }
    }
    const reconocidos = unicos.filter((i) => i.tipo === 'residuo');
    const orden = ['verde', 'blanca', 'negra'];
    const canecas = orden.filter((c) => reconocidos.some((i) => i.residuo.caneca === c));
    let estado;
    if (!unicos.length) estado = 'desconocido';
    else if (unicos.some((i) => i.tipo === 'especial')) estado = 'especial';
    else if (unicos.some((i) => i.tipo === 'ambiguo')) estado = 'ambiguo';
    else if (sinReconocer.length) estado = 'mixto';
    else estado = 'reconocido';
    return { estado, items: unicos, canecas: estado === 'reconocido' ? canecas : [], sinReconocer,
      sugerencias: estado === 'desconocido' || estado === 'mixto' ? sugerir(sinReconocer.join(' ') || base, 4) : [] };
  }

  /** Autocompletado y «¿quisiste decir…?»: prefijos y similitud por fichas. */
  function sugerir(texto, limite = 6) {
    const q = fichas(texto);
    if (!q.length) return [];
    const puntos = new Map();
    for (const p of patrones) {
      if (p.tipo !== 'residuo') continue;
      let s = 0;
      for (const w of q) {
        let mejor = 0;
        for (const a of p.f) {
          if (a === w) mejor = Math.max(mejor, 3);
          else if (a.startsWith(w) && w.length >= 2) mejor = Math.max(mejor, 2);
          else if (w.length >= 4 && distancia(w, a, 2) <= tolerancia(a)) mejor = Math.max(mejor, 1.5);
        }
        s += mejor;
      }
      if (s > 0) {
        s = s / q.length - p.f.length * 0.05;
        const id = p.ref.id;
        if (!puntos.has(id) || puntos.get(id) < s) puntos.set(id, s);
      }
    }
    return [...puntos.entries()].sort((a, b) => b[1] - a[1]).slice(0, limite).map(([id]) => porId.get(id));
  }

  return { clasificar, sugerir, porId, catalogo };
}

/** Política de tiempos por cantidad de canecas distintas: 1 → 5 s, 2 → 7 s, 3 → 10 s. */
export const duracionPolitica = (n) => (n <= 1 ? 5000 : n === 2 ? 7000 : 10000);
