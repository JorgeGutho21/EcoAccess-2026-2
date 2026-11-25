"""Genera web/panel-demo/index.html (página pública de GitHub Pages) a partir del panel real.

Uso: python scripts/web/generar_panel_demo.py
Mismo HTML, CSS y JavaScript que src/EcoAccess.P3.PanelCanecas/wwwroot/index.html, con rutas relativas, un aviso
visible de DEMOSTRACIÓN y web/panel-demo/demo.js (estación simulada en el navegador, sin hardware ni backend).
También copia las capas de las canecas (web/ciudadano/assets/canecas/*.webp) a web/panel-demo/img/canecas/.
"""
import glob
import os
import shutil

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ORIGEN = os.path.join(RAIZ, "src", "EcoAccess.P3.PanelCanecas", "wwwroot", "index.html")
DESTINO = os.path.join(RAIZ, "web", "panel-demo", "index.html")
CAPAS = os.path.join(RAIZ, "web", "ciudadano", "assets", "canecas")
CAPAS_DESTINO = os.path.join(RAIZ, "web", "panel-demo", "img", "canecas")

AVISO = ('<div role="note" style="background:#3A2A0C;color:#F2C063;padding:8px 20px;font:600 14px Barlow,Segoe UI,system-ui,sans-serif;'
         'text-align:center;border-bottom:1px solid #6B4F1C">DEMOSTRACIÓN · estación simulada en tu navegador: '
         'no está conectada al ESP32 ni mueve canecas reales. El panel físico funciona solo en la estación anfitriona '
         '(http://localhost:8090).</div>')


def main():
    html = open(ORIGEN, encoding="utf-8").read()
    html = html.replace('href="/favicon.ico"', 'href="favicon.ico"').replace('"/img/', '"img/')
    html = html.replace("<title>", "<title>[Demostración] ", 1)
    html = html.replace("</head>", '<script src="demo.js"></script>\n</head>', 1)
    i = html.index("<body")
    j = html.index(">", i) + 1
    html = html[:j] + "\n" + AVISO + html[j:]
    open(DESTINO, "w", encoding="utf-8", newline="\n").write(html)
    print("Generado", os.path.relpath(DESTINO, RAIZ))

    os.makedirs(CAPAS_DESTINO, exist_ok=True)
    copiadas = 0
    for capa in sorted(glob.glob(os.path.join(CAPAS, "*.webp"))):
        shutil.copyfile(capa, os.path.join(CAPAS_DESTINO, os.path.basename(capa)))
        copiadas += 1
    print(f"Copiadas {copiadas} capas de canecas a", os.path.relpath(CAPAS_DESTINO, RAIZ))


if __name__ == "__main__":
    main()
