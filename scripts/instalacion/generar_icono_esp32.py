"""Genera assets/identidad/EcoAccess_ESP32.ico: icono del acceso directo «ECOACCESS ESP32».

Placa verde institucional con forma de chip (patillas a los lados) y las tres canecas originales encima.
Uso (desde la raíz del repositorio): python scripts/instalacion/generar_icono_esp32.py
"""
import os
from PIL import Image, ImageDraw

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CANECAS = [os.path.join(RAIZ, "assets", "imagenes", "inicio", f"caneca_{c}.png") for c in ("verde", "blanca", "negra")]
DESTINO = os.path.join(RAIZ, "assets", "identidad", "EcoAccess_ESP32.ico")
VERDE, VERDE_OSC, HOJA = (22, 138, 85), (16, 92, 59), (183, 228, 203)


def dibujar(lado=512):
    im = Image.new("RGBA", (lado, lado), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    m = lado // 10                      # margen para las patillas
    # Patillas del chip (4 por lado, arriba y abajo).
    for i in range(4):
        x = m + (lado - 2 * m) * (i + 1) // 5
        w = lado // 22
        d.rounded_rectangle([x - w, 0, x + w, m + 8], radius=w // 2, fill=HOJA)
        d.rounded_rectangle([x - w, lado - m - 8, x + w, lado], radius=w // 2, fill=HOJA)
    # Cuerpo del chip.
    d.rounded_rectangle([m // 3, m, lado - m // 3, lado - m], radius=lado // 9, fill=VERDE_OSC)
    d.rounded_rectangle([m // 3 + 10, m + 10, lado - m // 3 - 10, lado - m - 10], radius=lado // 11, outline=VERDE, width=max(2, lado // 64))
    # Tres canecas originales.
    alto = int((lado - 2 * m) * 0.78)
    piezas = []
    for ruta in CANECAS:
        c = Image.open(ruta).convert("RGBA")
        c = c.crop(c.getbbox())
        c.thumbnail((lado, alto), Image.LANCZOS)
        piezas.append(c)
    total = sum(p.width for p in piezas) + (len(piezas) - 1) * (lado // 40)
    escala = min(1.0, (lado - 2 * m) / total)
    if escala < 1.0:
        piezas = [p.resize((int(p.width * escala), int(p.height * escala)), Image.LANCZOS) for p in piezas]
        total = sum(p.width for p in piezas) + (len(piezas) - 1) * (lado // 40)
    x = (lado - total) // 2
    for p in piezas:
        y = (lado - p.height) // 2 + lado // 40
        im.alpha_composite(p, (x, y))
        x += p.width + lado // 40
    return im


if __name__ == "__main__":
    base = dibujar(512)
    base.save(DESTINO, format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    base.resize((256, 256), Image.LANCZOS).save(os.path.join(os.environ.get("TEMP", "."), "EcoAccess_ESP32_vista.png"))
    print("Generado", os.path.relpath(DESTINO, RAIZ))
