"""ECOACCESS P3 - Conjunto visual activo de 11 residuos (fotografías reales de la estación, 25/11/2026).

Uso (desde la raíz del repositorio):
    python scripts/vision/construir_dataset_expo11.py

Lee las fotos originales de ../../02_Imagenes_Reales_EcoAcess (fuera del repositorio; NUNCA se modifican) y genera
datasets/ecoaccess11 con la estructura acordada:
    verde/{papa,limon,manzana_verde,lulo,cascara_banano}/  blanca/{papel_aluminio,envase_vidrio_rosado,papel_cuaderno,papel_kraft}/
    negra/{envase_pegamento,servilleta_usada}/  negativos/  validacion/  manifiesto.csv  manifiesto.json
- Cada foto se identificó visualmente (hoja de contactos) y se asigna por NOMBRE DE ARCHIVO, no por orden ni color.
- Orientación EXIF corregida, lado mayor 1024 px, JPEG calidad 88. Duplicados exactos por SHA-256 excluidos.
- Las dos capturas de pantalla del error de Telegram no son datos de entrenamiento: se guardan como evidencia.
- validacion/: escenas con varios residuos COMPUESTAS digitalmente con fotos reales (rotuladas como compuestas).
- HONESTIDAD: hay un solo objeto físico por material y una sola sesión de captura; estas fotos se usaron para diseñar
  la instrucción y las preguntas de verificación del reconocedor (usada_en_ajuste=true). Sus aciertos NO son evidencia
  de generalización. Las negativas reales (cable, blíster, etc., del 23/11) nunca se usaron para ajustar.
"""
import csv, hashlib, json, os, random, shutil, sys
from PIL import Image, ImageOps

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
REALES = os.path.normpath(os.path.join(RAIZ, "..", "..", "02_Imagenes_Reales_EcoAcess"))
EXPO = os.path.join(REALES, "IMAGENES QUE SE VAN A UTILIZAR EN EXPO")
SALIDA = os.path.join(RAIZ, "datasets", "ecoaccess11")
EVIDENCIA = os.path.join(RAIZ, "docs", "informes", "telegram")
LADO = 1024
random.seed(20261125)

MAT = {  # codigo: (caneca, carpeta, nombre)
    "M01": ("verde", "papa", "Papa"), "M02": ("verde", "limon", "Limón"), "M03": ("verde", "manzana_verde", "Manzana verde"),
    "M04": ("verde", "lulo", "Lulo"), "M05": ("verde", "cascara_banano", "Cáscara de banano"),
    "M06": ("blanca", "papel_aluminio", "Papel aluminio"), "M07": ("blanca", "envase_vidrio_rosado", "Envase de vidrio rosado (sérum)"),
    "M08": ("blanca", "papel_cuaderno", "Papel de cuaderno"), "M09": ("blanca", "papel_kraft", "Papel kraft"),
    "M10": ("negra", "envase_pegamento", "Envase de pegamento"), "M11": ("negra", "servilleta_usada", "Servilleta usada"),
}
CANECA = {"verde": "VERDE", "blanca": "BLANCA", "negra": "NEGRA"}

def w(h, m, s, n=None):
    return f"WhatsApp Image 2026-11-25 at {h}.{m}.{s} AM" + (f" ({n})" if n else "") + ".jpeg"

# Identificación visual verificada (hoja de contactos numerada, 25/11/2026).
FOTOS = [
    (w(9, 33, "09"), "M01"), (w(9, 33, "09", 2), "M01"), (w(9, 33, "09", 3), "M01"), (w(9, 33, 10), "M01"), (w(9, 33, 10, 1), "M01"),
    (w(9, 33, 10, 2), "M02"), (w(9, 33, 10, 3), "M02"),
    (w(9, 33, 10, 4), "M03"), (w(9, 33, 11), "M03"),
    (w(9, 33, 11, 1), "M04"), (w(9, 33, 11, 2), "M04"),
    (w(9, 33, 11, 4), "M09"), (w(9, 33, 11, 5), "M09"),
    (w(9, 33, 11, 6), "M07"), (w(9, 33, 12), "M07"), (w(9, 33, 12, 1), "M07"),
    (w(9, 33, 12, 2), "M11"), (w(9, 33, 12, 3), "M11"),
    (w(9, 33, 12, 4), "M08"), (w(9, 33, 12, 5), "M08"),
    (w(9, 33, 12, 6), "M06"), (w(9, 33, 12, 7), "M06"),
    (w(9, 33, 13), "M10"), (w(9, 33, 13, 1), "M10"), (w(9, 33, 13, 2), "M10"),
]
# Las dos fotos restantes, 9.33.09 AM (1) y 9.33.11 AM (3), son capturas de pantalla del error de Telegram (evidencia).

NEGATIVAS = [  # fotos reales del 23/11 que nunca se usaron para ajustar el reconocedor de 11
    ("WhatsApp Image 2026-11-23 at 5.09.17 PM.jpeg", "cable USB (gestión especial)", True),
    ("WhatsApp Image 2026-11-23 at 5.09.18 PM.jpeg", "blíster de medicamentos (gestión especial)", True),
    ("WhatsApp Image 2026-11-23 at 5.09.18 PM (2).jpeg", "máquina de afeitar (gestión especial)", True),
    ("WhatsApp Image 2026-11-23 at 5.09.17 PM (1).jpeg", "carta de juego", False),
    ("WhatsApp Image 2026-11-23 at 5.09.18 PM (1).jpeg", "aplicador de hilo dental", False),
    ("WhatsApp Image 2026-11-23 at 5.09.19 PM (1).jpeg", "esponja de limpieza", False),
    ("WhatsApp Image 2026-11-23 at 5.09.19 PM.jpeg", "bolsa plástica arrugada", False),
    ("WhatsApp Image 2026-11-23 at 5.09.20 PM (3).jpeg", "medio tomate", False),
    ("WhatsApp Image 2026-11-23 at 5.09.22 PM (4).jpeg", "cebolla morada", False),
]

ESCENAS = [["M01", "M02", "M03"], ["M02", "M06"], ["M06", "M10"], ["M01", "M06", "M11"], ["M04", "M09"], ["M07", "M08"],
           ["M03", "M10"], ["M09", "M11"], ["M02", "M07", "M10"], ["M04", "M06", "M11"]]

def abrir(ruta):
    im = ImageOps.exif_transpose(Image.open(ruta)).convert("RGB")
    im.thumbnail((LADO, LADO))
    return im

def huella(ruta):
    return hashlib.sha256(open(ruta, "rb").read()).hexdigest()

def main():
    if not os.path.isdir(EXPO):
        sys.exit("No se encuentra la carpeta de fotos: " + EXPO)
    for sub in ("verde", "blanca", "negra", "negativos", "validacion"):
        shutil.rmtree(os.path.join(SALIDA, sub), ignore_errors=True)
    filas, vistos, duplicados, faltan = [], {}, [], []
    asignadas = {f for f, _ in FOTOS}
    capturas = [f for f in os.listdir(EXPO) if f.lower().endswith((".jpg", ".jpeg", ".png")) and f not in asignadas]
    for f in capturas:  # las dos capturas de pantalla del error de Telegram (verificadas visualmente)
        os.makedirs(EVIDENCIA, exist_ok=True)
        im = abrir(os.path.join(EXPO, f)); im.thumbnail((720, 720))
        im.save(os.path.join(EVIDENCIA, "error-descarga-foto-" + str(capturas.index(f) + 1) + ".jpg"), "JPEG", quality=85)
    contador = {}
    for archivo, cod in FOTOS:
        ruta = os.path.join(EXPO, archivo)
        if not os.path.exists(ruta):
            faltan.append(archivo); continue
        h = huella(ruta)
        if h in vistos:
            duplicados.append((archivo, vistos[h])); continue
        vistos[h] = archivo
        caneca, carpeta, nombre = MAT[cod]
        contador[cod] = contador.get(cod, 0) + 1
        rel = f"{caneca}/{carpeta}/{carpeta}_{contador[cod]:02d}.jpg"
        os.makedirs(os.path.join(SALIDA, caneca, carpeta), exist_ok=True)
        abrir(ruta).save(os.path.join(SALIDA, rel), "JPEG", quality=88)
        filas.append(dict(archivo=rel, material=nombre, codigo=cod, categoria=caneca, caneca=CANECA[caneca], conjunto="real_ajuste",
                          origen="real", grupo_objeto=f"{cod}-objeto1-sesion20261125", fuente=os.path.relpath(ruta, RAIZ).replace("\\", "/"),
                          sha256_fuente=h, usada_en_ajuste=True))
    for i, (archivo, desc, especial) in enumerate(NEGATIVAS, 1):
        ruta = os.path.join(REALES, archivo)
        if not os.path.exists(ruta):
            faltan.append(archivo); continue
        rel = f"negativos/negativo_{i:02d}.jpg"
        os.makedirs(os.path.join(SALIDA, "negativos"), exist_ok=True)
        abrir(ruta).save(os.path.join(SALIDA, rel), "JPEG", quality=88)
        filas.append(dict(archivo=rel, material=desc, codigo="", categoria="gestion_especial" if especial else "desconocido", caneca="",
                          conjunto="negativa", origen="real", grupo_objeto=f"negativo-{i:02d}-sesion20261123", fuente=os.path.relpath(ruta, RAIZ).replace("\\", "/"),
                          sha256_fuente=huella(ruta), usada_en_ajuste=False, gestion_especial=especial))
    # Escenas con varios residuos: composición digital de fotos reales (un recorte central de cada una).
    por_mat = {}
    for f in filas:
        if f["codigo"]: por_mat.setdefault(f["codigo"], []).append(f["archivo"])
    os.makedirs(os.path.join(SALIDA, "validacion"), exist_ok=True)
    for j, combo in enumerate(ESCENAS, 1):
        if any(c not in por_mat for c in combo): continue
        piezas = []
        for c in combo:
            im = Image.open(os.path.join(SALIDA, random.choice(por_mat[c]))).convert("RGB")
            wv, hv = im.size; im = im.crop((int(wv * .12), int(hv * .25), int(wv * .88), int(hv * .85))); im.thumbnail((420, 420)); piezas.append(im)
        lienzo = Image.new("RGB", (sum(p.width for p in piezas) + 10 * (len(piezas) + 1), max(p.height for p in piezas) + 20), (228, 226, 222))
        x = 10
        for p in piezas: lienzo.paste(p, (x, 10)); x += p.width + 10
        rel = f"validacion/escena_{j:02d}_{'_'.join(combo)}.jpg"
        lienzo.save(os.path.join(SALIDA, rel), "JPEG", quality=88)
        canecas = sorted({CANECA[MAT[c][0]] for c in combo}, key=["VERDE", "BLANCA", "NEGRA"].index)
        filas.append(dict(archivo=rel, material=" + ".join(MAT[c][2] for c in combo), codigo="|".join(combo), categoria="varios",
                          caneca=",".join(canecas), conjunto="compuesta", origen="compuesta", grupo_objeto="composicion-de-fotos-de-ajuste",
                          fuente="", sha256_fuente="", usada_en_ajuste=True))
    for cod, (caneca, carpeta, nombre) in MAT.items():
        n = sum(1 for f in filas if f["codigo"] == cod)
        if n < 5:
            faltan.append(f"{nombre}: {n} foto(s) real(es); se necesitan al menos 5 de 2 objetos distintos y otra sesión para evaluar de forma independiente")
    campos = ["archivo", "material", "codigo", "categoria", "caneca", "conjunto", "origen", "grupo_objeto", "usada_en_ajuste", "fuente", "sha256_fuente"]
    with open(os.path.join(SALIDA, "manifiesto.csv"), "w", encoding="utf-8", newline="") as fh:
        wr = csv.DictWriter(fh, fieldnames=campos, extrasaction="ignore"); wr.writeheader(); wr.writerows(filas)
    # manifiesto.json compatible con EcoAccess.P3.Bot.exe --evaluar-expo9
    entradas = [dict(archivo=f["archivo"], conjunto=f["conjunto"], origen=f["origen"], materiales=[c for c in f["codigo"].split("|") if c],
                     canecas=[c for c in f["caneca"].split(",") if c], usada_en_ajuste=f["usada_en_ajuste"],
                     gestion_especial=f.get("gestion_especial", False), descripcion=f["material"]) for f in filas]
    resumen = {c: sum(1 for f in filas if f["conjunto"] == c) for c in ("real_ajuste", "compuesta", "negativa")}
    resumen.update(total=len(filas), duplicados_excluidos=len(duplicados), capturas_de_error_como_evidencia=len(capturas))
    doc = dict(nombre="ECOACCESS_EXPOSOFTWARE_11", version="2026.11.25-expo11", resumen=resumen,
               materiales=[dict(codigo=c, nombre=v[2], caneca=CANECA[v[0]]) for c, v in MAT.items()], faltantes=faltan,
               nota="Un objeto físico por material, una sesión: las fotos reales se usaron para ajustar; no prueban generalización.",
               entradas=entradas)
    with open(os.path.join(SALIDA, "manifiesto.json"), "w", encoding="utf-8") as fh:
        json.dump(doc, fh, ensure_ascii=False, indent=1)
    print(json.dumps(resumen, ensure_ascii=False))
    print("faltantes:", *faltan, sep="\n - ")

if __name__ == "__main__":
    main()
