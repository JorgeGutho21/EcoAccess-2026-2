// ECOACCESS P3 · Simulador del panel de canecas para la página pública (GitHub Pages).
// Reemplaza fetch("/api/...") por una estación simulada en el navegador: NO hay ESP32, NO hay backend y NO se mueve
// ninguna caneca física. El panel real (src/EcoAccess.P3.PanelCanecas) corre en http://localhost:8090 de la estación.
(function () {
  "use strict";
  const ahora = () => Date.now();
  const hora = () => new Date().toLocaleTimeString("es-CO", { hour12: false });
  const NOM = { V: "Verde", B: "Blanca", N: "Negra" };
  const can = {
    V: { c: "V", nombre: "verde", pin: 23, cerrado: 0, abierto: 50, ms: 5000, estado: "REPOSO", hasta: 0, pendiente: false },
    B: { c: "B", nombre: "blanca", pin: 19, cerrado: 0, abierto: 70, ms: 5000, estado: "REPOSO", hasta: 0, pendiente: false },
    N: { c: "N", nombre: "negra", pin: 18, cerrado: 0, abierto: 40, ms: 5000, estado: "REPOSO", hasta: 0, pendiente: false },
  };
  const guardada = JSON.parse(JSON.stringify(can));
  let sesionHasta = 0;
  const historial = [];
  const anotar = (tipo, texto) => { historial.push({ hora: hora(), tipo, texto }); if (historial.length > 200) historial.shift(); };
  const politica = n => (n <= 1 ? 5000 : n === 2 ? 7000 : 10000);
  anotar("panel", "Demostración pública: estación simulada en el navegador (sin ESP32 ni movimiento físico).");

  function tick() {
    const t = ahora();
    for (const k of "VBN") {
      const x = can[k];
      if (x.estado === "ESPERA" && t >= x.hasta) { x.estado = "CICLO"; x.hasta = t + x.msGrupo; anotar("esp32", `${NOM[k]}: PWM de apertura aplicado (simulado)`); }
      else if ((x.estado === "CICLO" || x.estado === "ABIERTA") && t >= x.hasta) { x.estado = "CERRANDO"; x.hasta = t + 1000; anotar("esp32", `${NOM[k]}: PWM de cierre aplicado (simulado)`); }
      else if (x.estado === "CERRANDO" && t >= x.hasta) { x.estado = "REPOSO"; x.hasta = 0; anotar("esp32", `${NOM[k]}: ciclo completado (simulado)`); }
    }
  }
  setInterval(tick, 200);

  function estado() {
    tick();
    const sesion = ahora() < sesionHasta;
    return {
      ok: true, estado: sesion ? "lista" : "conectado",
      texto: sesion ? "Simulación lista: puede mover las canecas virtuales." : "Simulación: pulsa ACTIVAR CONTROL para probar las canecas virtuales.",
      conectado: true, usb: true, puerto: "SIMULADO", dispositivo: { Descripcion: "Estación simulada en el navegador", Vid: "-", Pid: "-" },
      identificador: "estacion_tres_canecas · simulacion-web", politica: { una: 5000, dos: 7000, tres: 10000, escalonMs: 250 },
      ultimaRespuesta: hora(), error: null, sesionSupervisada: sesion, sesionHasta: sesion ? new Date(sesionHasta).toLocaleTimeString("es-CO", { hour: "2-digit", minute: "2-digit" }) : null,
      anfitrion: true, ocupada: false, bot: { activo: false, oracle: null, pendientes: 0 }, ultimaOperacion: { V: "simulación", B: "simulación", N: "simulación" },
      esp32: { ok: true, firmware: "simulacion-web", cola: "", sinGuardar: Object.values(can).some(x => x.pendiente),
        canecas: Object.values(can).map(x => ({ c: x.c, nombre: x.nombre, pin: x.pin, cerrado: x.cerrado, abierto: x.abierto, ms: x.ms, estado: x.estado,
          pwm: x.estado !== "REPOSO", pendiente: x.pendiente, restanteMs: x.estado === "REPOSO" ? 0 : Math.max(0, x.hasta - ahora()) })) },
    };
  }
  const ocupado = () => Object.values(can).some(x => x.estado !== "REPOSO");
  const r = (status, body) => Promise.resolve(new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } }));

  function grupo(letras, ms, esc) {
    if (ocupado()) return r(409, { ok: false, error: "Hay otra caneca en movimiento: espera a que termine o pulsa Detener." });
    const t = ahora();
    [...letras].forEach((k, i) => { const x = can[k]; x.estado = "ESPERA"; x.msGrupo = ms; x.hasta = t + i * esc; });
    return null;
  }

  const fetchReal = window.fetch.bind(window);
  window.fetch = function (url, op) {
    const ruta = new URL(url, location.href).pathname.replace(/^.*\/api\//, "/api/");
    if (!ruta.startsWith("/api/")) return fetchReal(url, op);
    const m = (op && op.method) || "GET";
    const cuerpo = op && op.body ? JSON.parse(op.body) : {};
    const sesion = ahora() < sesionHasta;
    let x;
    if (m === "GET" && ruta === "/api/estacion/estado") return r(200, estado());
    if (m === "GET" && ruta === "/api/historial") return r(200, { ok: true, historial: historial.slice(-60).reverse() });
    if (ruta === "/api/estacion/buscar" || ruta === "/api/estacion/probar-comunicacion") return r(200, { ok: true, msg: "Simulación: comunicación correcta (sin hardware)." });
    if (ruta === "/api/sesion/habilitar") { sesionHasta = ahora() + 2 * 3600e3; anotar("sesion", "Control activado (simulación)."); return r(200, { ok: true }); }
    if (ruta === "/api/sesion/terminar") { sesionHasta = 0; anotar("sesion", "Control desactivado."); return r(200, { ok: true }); }
    if (ruta === "/api/estacion/detener") { sesionHasta = 0; for (const k of "VBN") if (can[k].estado !== "REPOSO" && can[k].estado !== "CERRANDO") { can[k].estado = "CERRANDO"; can[k].hasta = ahora() + 1000; } anotar("orden", "DETENER TODO (simulación)."); return r(200, { ok: true, msg: "Detenido" }); }
    if ((x = ruta.match(/^\/api\/canecas\/([VBN])\/detener$/))) { const c = can[x[1]]; if (c.estado !== "REPOSO") { c.estado = "CERRANDO"; c.hasta = ahora() + 1000; } return r(200, { ok: true }); }
    if ((x = ruta.match(/^\/api\/canecas\/([VBN])\/calibracion$/))) {
      const c = can[x[1]], a = +cuerpo.cerrado, b = +cuerpo.abierto, t = +cuerpo.ms;
      if (a < 0 || a > 180 || b < 0 || b > 180) return r(409, { ok: false, error: "Los ángulos deben estar entre 0° y 180°." });
      if (Math.abs(b - a) < 5) return r(409, { ok: false, error: "El recorrido debe ser de al menos 5°." });
      Object.assign(c, { cerrado: a, abierto: b, ms: t, pendiente: true }); return r(200, { ok: true, msg: "Calibración aplicada (sin guardar)" });
    }
    if ((x = ruta.match(/^\/api\/(?:canecas\/([VBN])\/)?calibracion\/(guardar|descartar)$/))) {
      const ks = x[1] ? [x[1]] : ["V", "B", "N"];
      ks.forEach(k => { if (x[2] === "guardar") { guardada[k] = { ...can[k], pendiente: false }; can[k].pendiente = false; } else Object.assign(can[k], { ...guardada[k], estado: can[k].estado, hasta: can[k].hasta }); });
      return r(200, { ok: true, msg: x[2] === "guardar" ? "Calibración guardada (en esta pestaña)" : "Calibración restaurada" });
    }
    if (!sesion) return r(423, { ok: false, error: "Requiere sesión supervisada: pulsa ACTIVAR CONTROL." });
    if ((x = ruta.match(/^\/api\/canecas\/([VBN])\/(abrir|cerrar|probar|posicion)$/))) {
      const c = can[x[1]];
      if (x[2] === "cerrar") { if (c.estado !== "REPOSO") { c.estado = "CERRANDO"; c.hasta = ahora() + 1000; } return r(200, { ok: true }); }
      const e = grupo(x[1], c.ms, 0); if (e) return e; anotar("orden", `${NOM[x[1]]}: apertura (simulación)`); return r(200, { ok: true });
    }
    if (ruta === "/api/estacion/abrir-grupo") {
      const letras = [...new Set(String(cuerpo.canecas || "").toUpperCase().replace(/[^VBN]/g, ""))].join("");
      const ms = politica(letras.length), sim = cuerpo.modalidad !== "secuencial";
      const e = grupo(letras, ms, sim ? 250 : ms + 1200); if (e) return e;
      anotar("orden", `Apertura ${sim ? "simultánea" : "secuencial"} ${[...letras].map(k => NOM[k]).join(" + ")} · ${ms / 1000} s (simulación)`);
      return r(200, { ok: true, canecas: letras, duracionMs: ms, modalidad: sim ? "simultanea" : "secuencial" });
    }
    if (ruta === "/api/pruebas/secuencial") { const s = String(cuerpo.canecas || "VBN"); const e = grupo(s[0], can[s[0]].ms, 0); return e || r(200, { ok: true }); }
    if (ruta === "/api/pruebas/c01-c07" || ruta === "/api/pruebas/combinaciones") {
      const combos = ["V", "B", "N", "VB", "VN", "BN", "VBN"]; let t = 0;
      combos.forEach(cb => { setTimeout(() => { grupo(cb, politica(cb.length), 250); anotar("prueba", `${cb}: ${politica(cb.length) / 1000} s (simulación)`); }, t); t += politica(cb.length) + 3000; });
      return r(200, { ok: true, msg: "Prueba C01–C07 simulada en curso." });
    }
    return r(404, { ok: false, error: "Ruta no disponible en la demostración." });
  };
})();
