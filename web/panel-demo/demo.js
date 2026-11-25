// ECOACCESS P3 · Simulador del panel de canecas para la página pública (GitHub Pages).
// Reemplaza fetch("/api/...") por una estación simulada en el navegador: NO hay ESP32, NO hay backend y NO se mueve
// ninguna caneca física. El panel real (src/EcoAccess.P3.PanelCanecas) corre en http://localhost:8090 de la estación.
// Para capturas y pruebas: window.demoEstacion.desconectar() / .conectar() simulan quitar y poner el cable USB.
(function () {
  "use strict";
  const ahora = () => Date.now();
  const hora = (t) => new Date(t || ahora()).toLocaleTimeString("es-CO", { hour12: false });
  const NOM = { V: "Verde", B: "Blanca", N: "Negra" };
  const can = {
    V: { c: "V", nombre: "verde", pin: 23, cerrado: 0, abierto: 50, ms: 5000, estado: "REPOSO", hasta: 0, pendiente: false },
    B: { c: "B", nombre: "blanca", pin: 19, cerrado: 0, abierto: 70, ms: 5000, estado: "REPOSO", hasta: 0, pendiente: false },
    N: { c: "N", nombre: "negra", pin: 18, cerrado: 0, abierto: 40, ms: 5000, estado: "REPOSO", hasta: 0, pendiente: false },
  };
  const guardada = JSON.parse(JSON.stringify(can));
  const ultima = { V: "Sin operaciones", B: "Sin operaciones", N: "Sin operaciones" };
  let sesionHasta = 0, conectado = true, ultimaResp = ahora();
  const inicio = ahora();
  const historial = [];
  const anotar = (tipo, texto) => { historial.push({ hora: hora(), tipo, texto }); if (historial.length > 200) historial.shift(); };
  const politica = n => (n <= 1 ? 5000 : n === 2 ? 7000 : 10000);
  anotar("panel", "Demostración pública: estación simulada en el navegador (sin ESP32 ni movimiento físico).");

  function tick() {
    const t = ahora();
    if (conectado) ultimaResp = t;
    for (const k of "VBN") {
      const x = can[k];
      if (x.estado === "ESPERA" && t >= x.hasta) { x.estado = "CICLO"; x.hasta = t + x.msGrupo; anotar("esp32", `${NOM[k]}: PWM de apertura aplicado (simulado)`); }
      else if ((x.estado === "CICLO" || x.estado === "ABIERTA") && t >= x.hasta) { x.estado = "CERRANDO"; x.hasta = t + 1400; anotar("esp32", `${NOM[k]}: PWM de cierre aplicado (simulado)`); }
      else if (x.estado === "CERRANDO" && t >= x.hasta) { x.estado = "REPOSO"; x.hasta = 0; anotar("esp32", `${NOM[k]}: ciclo completado (simulado)`); }
    }
  }
  setInterval(tick, 200);

  // Pulso simulado del bot: se renueva cada 30 s, como el archivo bot-estado.json real.
  const pulso = () => Math.floor(((ahora() - inicio) / 1000) % 30) + 2;

  function estado() {
    tick();
    const sesion = ahora() < sesionHasta;
    const base = {
      ok: true, politica: { una: 5000, dos: 7000, tres: 10000, escalonMs: 250 }, anfitrion: true, ultimaOperacion: { ...ultima },
      bot: { activo: true, oracle: true, pendientes: 0, hace: pulso() }, pasarela: { activa: true, tunel: null, qrActivo: false },
    };
    if (!conectado) return { ...base, estado: "usb-desconectado", texto: "USB desconectado: no se detecta la placa (simulación).", conectado: false, usb: false, puerto: null,
      dispositivo: null, identificador: null, ultimaRespuesta: hora(ultimaResp), error: "Comunicación perdida: el cable USB se desconectó (simulación).", esp32: null,
      sesionSupervisada: false, sesionHasta: null, ocupada: false };
    return {
      ...base, estado: sesion ? "lista" : "conectado",
      texto: sesion ? "Simulación lista: puede mover las canecas virtuales." : "Simulación: pulsa ACTIVAR CONTROL para probar las canecas virtuales.",
      conectado: true, usb: true, puerto: "COM3", dispositivo: { Descripcion: "Silicon Labs CP210x USB to UART Bridge (simulado)", Vid: "10C4", Pid: "EA60" },
      identificador: "estacion_tres_canecas · ecoaccess-calibracion-v2 (simulado)", ultimaRespuesta: hora(ultimaResp), error: null,
      sesionSupervisada: sesion, sesionHasta: sesion ? new Date(sesionHasta).toLocaleTimeString("es-CO", { hour: "2-digit", minute: "2-digit", hour12: false }) : null,
      ocupada: Object.values(can).some(x => x.estado !== "REPOSO"),
      esp32: { ok: true, firmware: "ecoaccess-calibracion-v2", cola: "", sinGuardar: Object.values(can).some(x => x.pendiente),
        canecas: Object.values(can).map(x => ({ c: x.c, nombre: x.nombre, pin: x.pin, cerrado: x.cerrado, abierto: x.abierto, ms: x.ms, estado: x.estado,
          pwm: x.estado !== "REPOSO" && x.estado !== "ESPERA", pendiente: x.pendiente, restanteMs: x.estado === "REPOSO" ? 0 : Math.max(0, x.hasta - ahora()) })) },
    };
  }

  function diagnostico() {
    const e = estado(), c = [];
    const p = (id, nombre, est, valor, detalle, accion) => c.push({ id, nombre, estado: est, valor, detalle, accion: accion || null });
    if (!conectado) {
      p("usb", "Cable USB", "mal", "Desconectado", "No se detecta ningún dispositivo USB-serie.", "Conecta el cable USB de datos del ESP32 y pulsa «Buscar ESP32».");
      p("com", "Puerto COM", "mal", "No detectado", "Ningún puerto COM corresponde a una placa ESP32.", "Revisa el cable y el controlador USB-serie.");
      p("identidad", "Identidad ESP32", "inactivo", "Desconocida", "Sin dispositivo que identificar.");
      p("firmware", "Firmware", "inactivo", "No verificable", "Se verifica cuando el ESP32 responde.");
      p("serial", "Comunicación serial", "mal", "Sin respuesta", `Comunicación perdida. Última respuesta: ${e.ultimaRespuesta}.`, "Revisa el cable y pulsa «Buscar ESP32». La posición de las tapas es desconocida.");
      p("canales", "Canales GPIO", "inactivo", "Sin datos", "Esperados: verde GPIO 23, blanca GPIO 19, negra GPIO 18.");
      p("calibracion", "Calibraciones", "inactivo", "Sin datos", "Se leen del ESP32 cuando está conectado.");
    } else {
      p("usb", "Cable USB", "ok", "Conectado", "Silicon Labs CP210x (simulado) · VID 10C4 · PID EA60");
      p("com", "Puerto COM", "ok", "COM3", "Abierto por este panel (simulación).");
      p("identidad", "Identidad ESP32", "ok", "Placa reconocida", "Puente USB-serie habitual de ESP32 (VID 10C4).");
      p("firmware", "Firmware", "ok", "ecoaccess-calibracion-v2", "Calibración y aperturas simultáneas (orden GRUPO).");
      p("serial", "Comunicación serial", "ok", "Responde", `Última respuesta a ESTADO hace 0 s (${e.ultimaRespuesta}).`);
      p("canales", "Canales GPIO", "ok", "3 configurados", "Verde GPIO 23 · Blanca GPIO 19 · Negra GPIO 18. Un solo ESP32; sin medición de posición.");
      const pend = Object.values(can).filter(x => x.pendiente).map(x => NOM[x.c]);
      p("calibracion", "Calibraciones", pend.length ? "aviso" : "ok", pend.length ? `${pend.length} sin guardar` : "Guardadas",
        Object.values(can).map(x => `${NOM[x.c]} ${x.cerrado}°/${x.abierto}° ${x.ms / 1000} s`).join(" · "), pend.length ? `Guarda o restaura la calibración de: ${pend.join(", ")}.` : null);
    }
    p("bot", "Bot de Telegram", "ok", "Conectado", `Pulso hace ${e.bot.hace} s (simulado).`);
    p("oracle", "Oracle", "ok", "Conectado", `Confirmado por el bot hace ${e.bot.hace} s (simulado).`);
    p("pendientes", "Operaciones por subir a Oracle", "ok", "Ninguna", "No hay operaciones esperando registro.");
    p("pasarela", "Pasarela web y túnel", "aviso", "Sin túnel", "La pasarela responde, pero no tiene túnel HTTPS público (simulación).", "Reinicia el túnel desde la pasarela.");
    p("vision", "Reconocimiento visual", "aviso", "Sin modelo cargado", "Ollama responde; el modelo se cargará en la primera consulta (simulación).");
    const err = historial.filter(x => x.tipo === "error" || x.tipo === "rechazo");
    p("errores", "Errores recientes", err.length ? "aviso" : "ok", err.length ? `${err.length} en el historial` : "Ninguno",
      err.length ? err.slice(-3).reverse().map(x => `${x.hora} ${x.texto}`).join(" · ") : "Sin errores ni rechazos desde que inició la demostración.", err.length ? "Revisa el historial técnico." : null);
    const cuenta = t => c.filter(x => x.estado === t).length;
    return { ok: true, hora: hora(), resumen: { ok: cuenta("ok"), aviso: cuenta("aviso"), mal: cuenta("mal"), inactivo: cuenta("inactivo") }, componentes: c };
  }

  const ocupado = () => Object.values(can).some(x => x.estado !== "REPOSO");
  const r = (status, body) => new Promise(res => setTimeout(() => res(new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } })), 60));
  const marcar = (letras, texto) => { for (const k of letras) ultima[k] = `${texto} · ${hora()}`; };

  function grupo(letras, ms, esc) {
    if (ocupado()) return r(409, { ok: false, error: "Hay otra caneca en movimiento: espera a que termine o pulsa Detener." });
    const t = ahora();
    [...letras].forEach((k, i) => { const x = can[k]; x.estado = "ESPERA"; x.msGrupo = ms; x.hasta = t + i * esc; });
    return null;
  }
  const sinConexion = () => r(503, { ok: false, error: "USB desconectado: no se detecta la placa (simulación)." });

  window.demoEstacion = {
    desconectar() { conectado = false; sesionHasta = 0; anotar("error", "Comunicación perdida: el cable USB se desconectó (simulación). La posición física de las tapas es desconocida."); },
    conectar() { conectado = true; anotar("conexion", "ESP32 conectado en COM3 (simulación). No se reenvió ninguna orden anterior."); },
    // Escena fija para capturas: { V: ["CICLO", 4000], N: ["CERRANDO", 60000] } (estado y milisegundos restantes).
    fijar(escena) { for (const k in escena) { const [e, ms] = escena[k]; Object.assign(can[k], { estado: e, hasta: ahora() + (ms || 0), msGrupo: ms || 5000 }); } },
  };

  const fetchReal = window.fetch.bind(window);
  window.fetch = function (url, op) {
    const ruta = new URL(url, location.href).pathname.replace(/^.*\/api\//, "/api/");
    if (!ruta.startsWith("/api/")) return fetchReal(url, op);
    const m = (op && op.method) || "GET";
    const cuerpo = op && op.body ? JSON.parse(op.body) : {};
    const sesion = ahora() < sesionHasta;
    let x;
    if (m === "GET" && ruta === "/api/estacion/estado") return r(200, estado());
    if (m === "GET" && ruta === "/api/diagnostico") return r(200, diagnostico());
    if (m === "GET" && ruta === "/api/historial") return r(200, { ok: true, historial: historial.slice(-60).reverse() });
    if (ruta === "/api/estacion/buscar") return r(200, { ok: true, msg: conectado ? "Simulación: ESP32 encontrado en COM3." : "Simulación: buscando el ESP32…" });
    if (ruta === "/api/estacion/probar-comunicacion") return conectado ? r(200, { ok: true, msg: "Simulación: el ESP32 respondió en 41 ms (sin mover motores)." }) : sinConexion();
    if (ruta === "/api/sesion/terminar") { sesionHasta = 0; anotar("sesion", "Control desactivado."); return r(200, { ok: true }); }
    if (ruta === "/api/estacion/detener") {
      sesionHasta = 0;
      if (!conectado) return r(503, { ok: false, error: "Control desactivado. El ESP32 está desconectado y NO recibió la orden de detención: si hay una tapa abierta, corta la alimentación." });
      for (const k of "VBN") { if (can[k].estado === "ESPERA") { can[k].estado = "REPOSO"; can[k].hasta = 0; } else if (can[k].estado !== "REPOSO" && can[k].estado !== "CERRANDO") { can[k].estado = "CERRANDO"; can[k].hasta = ahora() + 1400; } }
      anotar("orden", "DETENER TODO (simulación)."); return r(200, { ok: true, msg: "Detenido" });
    }
    if (!conectado) return sinConexion();
    if (ruta === "/api/sesion/habilitar") { sesionHasta = ahora() + 2 * 3600e3; anotar("sesion", "Control activado (simulación)."); return r(200, { ok: true }); }
    if ((x = ruta.match(/^\/api\/canecas\/([VBN])\/detener$/))) { const c = can[x[1]]; if (c.estado === "ESPERA") c.estado = "REPOSO"; else if (c.estado !== "REPOSO") { c.estado = "CERRANDO"; c.hasta = ahora() + 1400; } marcar(x[1], "detenida"); return r(200, { ok: true }); }
    if ((x = ruta.match(/^\/api\/canecas\/([VBN])\/calibracion$/))) {
      const c = can[x[1]], a = +cuerpo.cerrado, b = +cuerpo.abierto, t = +cuerpo.ms;
      if (a < 0 || a > 180 || b < 0 || b > 180) return r(409, { ok: false, error: "Los ángulos deben estar entre 0° y 180°." });
      if (Math.abs(b - a) < 5) return r(409, { ok: false, error: "El recorrido debe ser de al menos 5°." });
      Object.assign(c, { cerrado: a, abierto: b, ms: t, pendiente: true });
      anotar("orden", `${NOM[x[1]]}: calibración aplicada (sin guardar) cerrada ${a}°, abierta ${b}°, ${t / 1000} s`);
      return r(200, { ok: true, msg: "Calibración aplicada (sin guardar)" });
    }
    if ((x = ruta.match(/^\/api\/(?:canecas\/([VBN])\/)?calibracion\/(guardar|descartar)$/))) {
      const ks = x[1] ? [x[1]] : ["V", "B", "N"];
      ks.forEach(k => { if (x[2] === "guardar") { guardada[k] = { ...can[k], pendiente: false }; can[k].pendiente = false; } else Object.assign(can[k], { cerrado: guardada[k].cerrado, abierto: guardada[k].abierto, ms: guardada[k].ms, pendiente: false }); });
      anotar("calibracion", `${ks.map(k => NOM[k]).join(", ")}: ${x[2] === "guardar" ? "calibración guardada y verificada (simulación)" : "se restauró la última calibración guardada"}`);
      return r(200, { ok: true, msg: x[2] === "guardar" ? "Calibración guardada (en esta pestaña)" : "Calibración restaurada" });
    }
    if (!sesion) return r(423, { ok: false, error: "Requiere sesión supervisada: pulsa ACTIVAR CONTROL." });
    if ((x = ruta.match(/^\/api\/canecas\/([VBN])\/(abrir|cerrar|probar|posicion)$/))) {
      const c = can[x[1]];
      if (x[2] === "cerrar") { if (c.estado !== "REPOSO") { c.estado = "CERRANDO"; c.hasta = ahora() + 1400; } marcar(x[1], "cierre solicitado"); return r(200, { ok: true }); }
      const e = grupo(x[1], c.ms, 0); if (e) return e;
      marcar(x[1], x[2] === "probar" ? "prueba técnica de ciclo" : "apertura solicitada");
      anotar("orden", `${NOM[x[1]]}: ${x[2] === "probar" ? "prueba de ciclo" : "apertura"} (simulación)`); return r(200, { ok: true });
    }
    if (ruta === "/api/estacion/abrir-grupo") {
      const letras = [...new Set(String(cuerpo.canecas || "").toUpperCase().replace(/[^VBN]/g, ""))].join("");
      const ms = politica(letras.length), sim = cuerpo.modalidad !== "secuencial";
      const e = grupo(letras, ms, sim ? 250 : ms + 1200); if (e) return e;
      marcar(letras, `web: abierta ${ms / 1000} s`);
      anotar("orden", `Apertura ${sim ? "simultánea" : "secuencial"} ${[...letras].map(k => NOM[k]).join(" + ")} · ${ms / 1000} s (simulación)`);
      return r(200, { ok: true, canecas: letras, duracionMs: ms, modalidad: sim ? "simultanea" : "secuencial" });
    }
    if (ruta === "/api/pruebas/secuencial") { const s = String(cuerpo.canecas || "VBN"); const e = grupo(s, can[s[0]].ms, can[s[0]].ms + 1200); return e || r(200, { ok: true }); }
    if (ruta === "/api/pruebas/c01-c07" || ruta === "/api/pruebas/combinaciones") {
      const combos = ["V", "B", "N", "VB", "VN", "BN", "VBN"]; let t = 0;
      combos.forEach(cb => { setTimeout(() => { if (ahora() < sesionHasta && conectado) { grupo(cb, politica(cb.length), 250); anotar("prueba", `${cb}: ${politica(cb.length) / 1000} s (simulación)`); } }, t); t += politica(cb.length) + 3000; });
      return r(200, { ok: true, msg: "Prueba C01–C07 simulada en curso." });
    }
    return r(404, { ok: false, error: "Ruta no disponible en la demostración." });
  };
})();
