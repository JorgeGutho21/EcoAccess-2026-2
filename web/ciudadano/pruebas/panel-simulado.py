"""Panel de canecas SIMULADO para pruebas de integración web → pasarela → panel (no toca el puerto COM ni mueve motores).

Imita el contrato HTTP de EcoAccess.P3.PanelCanecas (localhost:8090). Uso:
    python web/ciudadano/pruebas/panel-simulado.py --puerto 8091 --firmware v1|v2
Registra cada orden recibida en GET /__ordenes (solo para las pruebas).
"""
import argparse
import json
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

ap = argparse.ArgumentParser()
ap.add_argument("--puerto", type=int, default=8091)
ap.add_argument("--firmware", default="v1")
args = ap.parse_args()

estado = {"sesion": False, "conectado": True, "ordenes": [], "ocupada_hasta": 0.0}
ms = {"V": 2000, "B": 2000, "N": 2000}


class Panel(BaseHTTPRequestHandler):
    def log_message(self, *a):
        pass

    def responder(self, codigo, cuerpo):
        b = json.dumps(cuerpo).encode()
        self.send_response(codigo)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(b)))
        self.end_headers()
        self.wfile.write(b)

    def do_GET(self):
        if self.path == "/__ordenes":
            return self.responder(200, estado["ordenes"])
        if self.path == "/api/estacion/estado":
            mov = time.time() < estado["ocupada_hasta"]
            d = {
                "ok": True, "conectado": estado["conectado"], "sesionSupervisada": estado["sesion"],
                "esp32": {"firmware": f"ecoaccess-calibracion-{args.firmware}", "canecas": [
                    {"c": c, "ms": ms[c], "estado": "CICLO" if mov else "REPOSO"} for c in "VBN"]},
            }
            if args.firmware == "v2":
                d["politica"] = {"una": 5000, "dos": 7000, "tres": 10000}
            return self.responder(200, d)
        self.responder(404, {"ok": False})

    def do_POST(self):
        largo = int(self.headers.get("Content-Length") or 0)
        cuerpo = json.loads(self.rfile.read(largo) or b"{}")
        if self.path == "/__conectado":
            estado["conectado"] = bool(cuerpo.get("valor"))
            return self.responder(200, {"ok": True})
        if self.path == "/api/sesion/habilitar":
            estado["sesion"] = True
            return self.responder(200, {"ok": True})
        if not estado["sesion"]:
            return self.responder(423, {"ok": False, "error": "Requiere sesión supervisada: habilítala desde la estación anfitriona."})
        if time.time() < estado["ocupada_hasta"]:
            return self.responder(409, {"ok": False, "error": "Hay otra caneca en movimiento: espera a que termine o pulsa Detener."})
        estado["ordenes"].append({"ruta": self.path, "cuerpo": cuerpo, "t": time.time()})
        estado["ocupada_hasta"] = time.time() + 1.0
        if self.path == "/api/estacion/abrir-grupo" and args.firmware != "v2":
            return self.responder(409, {"ok": False, "error": "orden desconocida"})
        return self.responder(200, {"ok": True})


ThreadingHTTPServer(("127.0.0.1", args.puerto), Panel).serve_forever()
