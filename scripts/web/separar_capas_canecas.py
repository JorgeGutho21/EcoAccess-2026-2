"""Separa cada caneca original (PNG/WebP transparente) en tres capas alineadas: tapa, cuerpo y boca interior.

La unión tapa/cuerpo es un arco: se detecta columna a columna (línea oscura de la junta) y se suaviza con un polinomio.
Uso: python scripts/web/separar_capas_canecas.py <carpeta_origen> <carpeta_destino>
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ANCHO_WEB = 540  # px; 2x para mostrarse a ~270 px


def separar(origen: Path, destino: Path, color: str) -> dict:
    im = Image.open(origen / f"{color}.webp").convert("RGBA")
    bbox = im.getbbox()
    im = im.crop(bbox)
    a = np.asarray(im).astype(float)
    h, w, _ = a.shape
    lum = a[:, :, :3].mean(axis=2)
    alfa = a[:, :, 3]
    cols = [x for x in range(w) if alfa[:, x].max() > 200]
    x0, x1 = cols[0], cols[-1]
    # Banda de búsqueda de la junta: entre el 22 % y el 36 % de la altura.
    y_a, y_b = int(h * 0.22), int(h * 0.36)
    xs, ys = [], []
    for x in range(x0 + int((x1 - x0) * 0.06), x1 - int((x1 - x0) * 0.06), 3):
        col = lum[y_a:y_b, max(0, x - 2):x + 3].mean(axis=1)
        col = np.convolve(col, np.ones(3) / 3, mode="same")
        xs.append(x); ys.append(y_a + int(np.argmin(col)))
    xs, ys = np.array(xs), np.array(ys)
    p = np.polyfit(xs, ys, 4)
    for _ in range(3):  # descarta valores atípicos (reflejos) y vuelve a ajustar
        r = np.abs(np.polyval(p, xs) - ys); m = r < max(4, np.percentile(r, 85))
        p = np.polyfit(xs[m], ys[m], 4)
    curva = np.clip(np.polyval(p, np.arange(w)), 0, h - 1)
    yy = np.arange(h)[:, None]
    solape = 3  # px de solape para que no aparezca una rendija entre capas
    mascara_tapa = yy <= (curva[None, :] + solape)
    tapa = a.copy(); tapa[:, :, 3] *= mascara_tapa
    cuerpo = a.copy(); cuerpo[:, :, 3] *= ~(yy < curva[None, :] - 1)
    alto_tapa = int(curva[x0:x1].max()) + solape + 1
    ancho = x1 - x0
    centro = (x0 + x1) / 2
    filas_tapa = [y for y in range(h) if alfa[y, x0:x1].max() > 200]
    tope = filas_tapa[0]
    junta_lados = curva[x0 + int(ancho * 0.08)]
    # Bisagra: borde trasero inferior de la tapa (en proyección, ~40 % entre el tope y la junta lateral).
    bisagra = tope + (junta_lados - tope) * 0.40
    # Color del cuerpo justo bajo la junta (para el borde de la boca y la cara interior de la tapa).
    muestra = a[int(curva[int(centro)]) + 8:int(curva[int(centro)]) + 30, int(centro - ancho * .3):int(centro + ancho * .3), :3].reshape(-1, 3)
    tono = np.median(muestra, axis=0)
    # Boca: abertura completa del cuerpo, desde el borde trasero (bisagra) hasta la junta frontal, con esquinas redondeadas.
    boca = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(boca)
    m = int(ancho * 0.035)
    xi, xd = x0 + m, x1 - m
    r = ancho * 0.09
    contorno = []
    for i in range(41):  # esquina trasera izquierda → borde trasero → esquina trasera derecha
        t = i / 40
        x = xi + t * (xd - xi)
        dx = min(x - xi, xd - x)
        y = bisagra + (r - np.sqrt(max(0.0, r * r - (r - dx) ** 2)) if dx < r else 0)
        contorno.append((x, y))
    contorno += [(x, curva[int(x)] + 1) for x in np.linspace(xd, xi, 60)]
    d.polygon(contorno, fill=(*[int(v) for v in tono], 255))  # borde del cuerpo
    interior = []
    k = ancho * 0.028
    for x, y in contorno:
        cx_ = min(max(x, xi + k), xd - k)
        interior.append((cx_, y + k * (0.6 if y <= bisagra + r else -0.9)))
    mascara_int = Image.new("L", (w, h), 0); ImageDraw.Draw(mascara_int).polygon(interior, fill=255)
    gradiente = Image.new("RGBA", (w, h))
    gy = np.linspace(0, 1, h)[:, None]
    y_a, y_b = bisagra / h, junta_lados / h
    tt = np.clip((gy - y_a) / max(1e-3, (y_b - y_a)), 0, 1)
    base = np.array([34, 40, 36]) * (1 - tt[..., None]) + np.array([6, 9, 8]) * tt[..., None]
    gradiente = Image.fromarray(np.broadcast_to(np.concatenate([base, np.full((h, 1, 1), 255)], axis=2), (h, w, 4)).astype("uint8"), "RGBA")
    boca.paste(gradiente, (0, 0), mascara_int)
    boca = boca.filter(ImageFilter.GaussianBlur(0.8))
    # Cara interior de la tapa: silueta de la tapa en el color del cuerpo oscurecido, con un panel rehundido.
    alfa_tapa = (tapa[:, :, 3]).clip(0, 255).astype("uint8")
    sil = Image.fromarray(alfa_tapa, "L")
    oscuro = np.clip(tono * 0.62 + 18, 0, 255).astype(int)
    interior_tapa = Image.new("RGBA", (w, h), (*oscuro, 0)); interior_tapa.putalpha(sil)
    panel = sil.filter(ImageFilter.MinFilter(int(ancho * 0.06) | 1)).filter(ImageFilter.GaussianBlur(3))
    capa_panel = Image.new("RGBA", (w, h), (*np.clip(tono * 0.48 + 10, 0, 255).astype(int), 255)); capa_panel.putalpha(panel)
    interior_tapa.alpha_composite(capa_panel)
    hueco = Image.fromarray(((lum < 28) & (alfa_tapa > 200)).astype("uint8") * 255, "L").filter(ImageFilter.GaussianBlur(1))
    capa_hueco = Image.new("RGBA", (w, h), (8, 10, 9, 255)); capa_hueco.putalpha(hueco)
    interior_tapa.alpha_composite(capa_hueco)
    escala = ANCHO_WEB / w
    nuevo = (ANCHO_WEB, round(h * escala))
    def guardar(arr_o_img, nombre):
        img = arr_o_img if isinstance(arr_o_img, Image.Image) else Image.fromarray(arr_o_img.clip(0, 255).astype("uint8"), "RGBA")
        img.resize(nuevo, Image.LANCZOS).save(destino / f"{color}-{nombre}.webp", "WEBP", quality=88, method=6)
    guardar(tapa, "tapa"); guardar(cuerpo, "cuerpo"); guardar(boca, "boca"); guardar(interior_tapa, "tapa-interior")
    # Punto de giro: borde posterior de la tapa = parte superior de la junta en los costados.
    return {"color": color, "ancho": nuevo[0], "alto": nuevo[1], "junta_centro": round(curva[int(centro)] * escala),
            "junta_lados": round(junta_lados * escala), "alto_tapa": round(alto_tapa * escala), "bisagra_pct": round(100 * bisagra / h, 2)}


if __name__ == "__main__":
    o, dst = Path(sys.argv[1]), Path(sys.argv[2])
    dst.mkdir(parents=True, exist_ok=True)
    for c in ("verde", "blanca", "negra"):
        print(separar(o, dst, c))
