"""Genera assets/identidad/EcoAccess_WEB.ico: icono del acceso directo «ECOACCESS WEB» (página pública).

Círculo verde institucional con un globo (Internet) y una hoja (sostenibilidad). Varias resoluciones (16-256 px).
Uso (desde la raíz del repositorio): python scripts/instalacion/generar_icono_web.py
"""
import os
from PIL import Image, ImageChops, ImageDraw

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DESTINO = os.path.join(RAIZ, "assets", "identidad", "EcoAccess_WEB.ico")
VERDE_OSC, VERDE, HOJA, BLANCO = (16, 92, 59), (22, 138, 85), (183, 228, 203), (255, 255, 255)


def dibujar(lado=1024):
    im = Image.new("RGBA", (lado, lado), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    m = lado // 32
    d.ellipse([m, m, lado - m, lado - m], fill=VERDE_OSC)
    # Globo: contorno, ecuador, paralelos y meridianos.
    c, r = lado // 2, int(lado * 0.33)
    w = max(3, lado // 40)
    caja = [c - r, c - r, c + r, c + r]
    d.ellipse(caja, outline=BLANCO, width=w)
    d.line([c - r, c, c + r, c], fill=BLANCO, width=w)
    for f in (0.55, -0.55):
        y = c + int(r * f)
        dx = int(r * (1 - f * f) ** 0.5)
        d.line([c - dx, y, c + dx, y], fill=BLANCO, width=w)
    d.ellipse([c - r // 2, c - r, c + r // 2, c + r], outline=BLANCO, width=w)
    d.line([c, c - r, c, c + r], fill=BLANCO, width=w)
    # Hoja en la esquina inferior derecha (sostenibilidad): intersección de dos círculos (forma de lente).
    t = int(lado * 0.34)
    x0, y0 = lado - m - t - lado // 40, lado - m - t - lado // 40
    fondo = ImageDraw.Draw(im)
    fondo.ellipse([x0 - t // 6, y0 - t // 6, x0 + t + t // 6, y0 + t + t // 6], fill=VERDE_OSC)
    a = Image.new("L", (lado, lado), 0); ImageDraw.Draw(a).ellipse([x0, y0 - t, x0 + 2 * t, y0 + t], fill=255)
    b = Image.new("L", (lado, lado), 0); ImageDraw.Draw(b).ellipse([x0 - t, y0, x0 + t, y0 + 2 * t], fill=255)
    lente = ImageChops.multiply(a, b)
    im.paste(Image.new("RGBA", (lado, lado), HOJA + (255,)), (0, 0), lente)
    ImageDraw.Draw(im).line([x0 + t // 6, y0 + t - t // 6, x0 + t - t // 8, y0 + t // 8], fill=VERDE, width=max(2, lado // 70))
    return im


if __name__ == "__main__":
    base = dibujar()
    base.save(DESTINO, format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    base.resize((256, 256), Image.LANCZOS).save(os.path.join(os.environ.get("TEMP", "."), "EcoAccess_WEB_vista.png"))
    print("Generado", os.path.relpath(DESTINO, RAIZ))
