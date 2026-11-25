"""ECOACCESS P3 - Construye el conjunto visual activo del Modo ExpoSoftware (9 materiales, 100 imagenes; limite absoluto 300).

Uso (desde la raiz del repositorio):
    python scripts/vision/construir_dataset_expo9.py

Lee las ilustraciones del catalogo (assets/imagenes/residuos) y las fotografias reales del stand
(../../02_Imagenes_Reales_EcoAcess, fuera del repositorio) y genera datasets/exposoftware9 con:
  referencias/      originales + variantes AUMENTADAS (brillo, contraste, rotacion, escala, sombra), 5 por material (45)
  evaluacion/       3 por material (27): originales NUNCA usados como referencia ni para ajustar reglas y, si faltan,
                    derivados DIGITALES (poca luz, sombra, lejania) de esos mismos originales de evaluacion
  multirresiduo/    fotografias reales con varios residuos + escenas COMPUESTAS a partir de originales de evaluacion
  negativas/        fotografias reales sin ninguno de los 9 materiales (o de gestion especial)
  manifiesto.json   nombre, material(es), caneca(s), origen, conjunto, etiqueta esperada y procedencia de cada imagen

No inventa fotografias: lo que falta queda en manifiesto.json["faltantes"]. Determinista (semilla fija).
"""
import hashlib, json, os, random, shutil, sys
from PIL import Image, ImageEnhance, ImageOps, ImageDraw, ImageFilter

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
REALES = os.path.normpath(os.path.join(RAIZ, "..", "..", "02_Imagenes_Reales_EcoAcess"))
ASSETS = os.path.join(RAIZ, "assets", "imagenes", "residuos")
SALIDA = os.path.join(RAIZ, "datasets", "exposoftware9")
LADO = 640
random.seed(20261125)

def real(n):  # nombre corto del archivo de WhatsApp (5.09.20 PM (2))
    return ("real", os.path.join(REALES, f"WhatsApp Image 2026-11-23 at {n}.jpeg"))
def ilu(rel):
    return ("ilustracion", os.path.join(ASSETS, rel))

CANECA = {"E01": "VERDE", "E02": "VERDE", "E03": "VERDE", "E04": "BLANCA", "E05": "BLANCA", "E06": "BLANCA", "E07": "NEGRA", "E08": "NEGRA", "E09": "NEGRA"}
NOMBRE = {"E01": "Media manzana", "E02": "Cascara de banano", "E03": "Papa o media papa", "E04": "Papel kraft o carton arrugado",
          "E05": "Botella plastica pequena", "E06": "Frasco pequeno de vidrio", "E07": "Papel higienico usado (muestra)",
          "E08": "Servilleta usada", "E09": "Envoltura metalizada de mecato"}

# (origen, ruta, usada_en_ajuste_de_reglas). Las marcadas True se usaron al disenar las reglas C#: nunca van a evaluacion.
REF = {
    "E01": [(*real("5.09.20 PM (2)"), True), (*ilu("verde/fotos/fruta-danada-1.jpg"), False)],
    "E02": [(*ilu("verde/fotos/cascara-banano-1.jpg"), True), (*ilu("verde/banano.jpg"), False)],
    "E03": [(*real("5.09.21 PM (2)"), True), (*ilu("verde/fotos/cascara-papa-1.jpg"), False)],
    "E04": [(*real("5.09.19 PM (2)"), True), (*ilu("blanca/fotos/carton-limpio-1.jpg"), False)],
    "E05": [(*ilu("blanca/botella-agua.jpg"), True), (*ilu("blanca/fotos/envase-plastico-3.jpg"), False)],
    "E06": [(*ilu("blanca/fotos/vidrio-1.jpg"), True)],
    "E07": [(*ilu("negra/fotos/papel-higienico-1.jpg"), True), (*ilu("negra/papel-higienico.jpg"), False)],
    "E08": [(*real("5.09.16 PM (1)"), True), (*ilu("negra/servilletas.jpg"), False), (*ilu("negra/fotos/servilletas-usadas-1.jpg"), False),
            (*ilu("negra/fotos/servilletas-usadas-2.jpg"), False), (*ilu("negra/fotos/servilletas-usadas-3.jpg"), False)],
    "E09": [(*ilu("negra/fotos/envolturas-1.jpg"), True), (*ilu("negra/fotos/envolturas-2.jpg"), False)],
}
EVAL = {
    "E01": [real("5.09.20 PM (1)"), real("5.09.20 PM")],
    "E02": [ilu("verde/fotos/cascara-banano-2.jpg")],
    "E03": [real("5.09.21 PM (1)"), real("5.09.21 PM (3)"), ilu("verde/fotos/cascara-papa-2.jpg")],
    "E04": [real("5.09.19 PM (3)"), ilu("blanca/fotos/carton-limpio-2.jpg"), ilu("blanca/fotos/carton-limpio-3.jpg")],
    "E05": [ilu("blanca/botella-gaseosa.jpg"), ilu("blanca/fotos/envase-plastico-2.jpg")],
    "E06": [ilu("blanca/fotos/vidrio-2.jpg")],
    "E07": [ilu("negra/fotos/papel-higienico-2.jpg"), ilu("negra/fotos/papel-higienico-3.jpg"), ilu("negra/fotos/papel-higienico-4.jpg")],
    "E08": [ilu(f"negra/fotos/servilletas-usadas-{i}.jpg") for i in range(4, 9)],
    "E09": [ilu("negra/fotos/envolturas-3.jpg"), ilu("negra/fotos/envolturas-4.jpg")],
}
# Fotografias reales con varios residuos: materiales de los 9 esperados y si hay un objeto fuera del alcance.
MULTI_REAL = [
    ("5.09.22 PM (7)", ["E01", "E04"], [], True),
    ("5.09.23 PM", ["E01", "E04"], [], False),
    ("5.09.23 PM (1)", ["E01"], ["bola de papel cuadriculado"], False),
    ("5.09.23 PM (2)", ["E01"], ["bola de papel cuadriculado"], False),
]
NEGATIVAS = [  # (origen, ruta, descripcion, gestion_especial, usada_en_ajuste) - 7: gestion especial, desconocidos y ambiguos
    (*real("5.09.17 PM"), "cable USB (RAEE)", True, True),
    (*real("5.09.18 PM (2)"), "maquina de afeitar desechable", True, False),
    (*real("5.09.18 PM"), "blister de medicamentos", True, True),
    (*real("5.09.16 PM (2)"), "bola de papel cuadriculado (fuera de los 9)", False, False),
    (*real("5.09.19 PM"), "bolsa plastica arrugada", False, False),
    (*real("5.09.20 PM (3)"), "medio tomate", False, False),
    (*ilu("negra/pendientes/pendiente-2.jpg"), "papel aluminio sucio (no es envoltura de mecato)", False, False),
]
COMBOS = [  # 17 escenas compuestas: misma caneca (3), dos canecas (7), tres canecas (7)
    ["E01", "E02"], ["E04", "E05"], ["E07", "E08"],
    ["E01", "E05"], ["E02", "E04"], ["E03", "E06"], ["E01", "E08"], ["E03", "E09"], ["E05", "E08"], ["E06", "E09"],
    ["E01", "E05", "E08"], ["E02", "E04", "E09"], ["E03", "E06", "E07"], ["E01", "E02", "E05"], ["E04", "E07", "E08"],
    ["E01", "E03", "E05", "E08"], ["E02", "E04", "E06", "E09"]]
POR_MATERIAL_REF, POR_MATERIAL_EVAL, LIMITE = 5, 3, 300
DERIVADOS_EVAL = [("poca_luz", lambda x: ImageEnhance.Brightness(x).enhance(0.5)), ("sombra", lambda x: sombra(x)),
                  ("lejano", lambda x: ImageOps.pad(x.resize((x.width // 2, x.height // 2)), x.size, color=(70, 70, 70)))]

def abrir(ruta):
    im = ImageOps.exif_transpose(Image.open(ruta)).convert("RGB")
    im.thumbnail((LADO, LADO))
    return im

def aumentar(im, k):
    """Variantes controladas y etiquetadas: nunca se presentan como fotografias fisicas independientes."""
    ops = [
        ("brillo_alto", lambda x: ImageEnhance.Brightness(x).enhance(1.35)),
        ("brillo_bajo", lambda x: ImageEnhance.Brightness(x).enhance(0.55)),
        ("contraste", lambda x: ImageEnhance.Contrast(x).enhance(1.4)),
        ("rotacion_15", lambda x: x.rotate(15, expand=True, fillcolor=(235, 235, 235))),
        ("rotacion_-20", lambda x: x.rotate(-20, expand=True, fillcolor=(40, 40, 40))),
        ("rotacion_90", lambda x: x.rotate(90, expand=True)),
        ("escala_lejana", lambda x: ImageOps.pad(x.resize((x.width // 2, x.height // 2)), x.size, color=(200, 200, 200))),
        ("recorte_cercano", lambda x: x.crop((x.width // 8, x.height // 8, x.width * 7 // 8, x.height * 7 // 8))),
        ("sombra", lambda x: sombra(x)),
        ("espejo_fondo_oscuro", lambda x: ImageOps.pad(ImageOps.mirror(x).resize((x.width * 3 // 4, x.height * 3 // 4)), x.size, color=(25, 25, 25))),
        ("desenfoque_leve", lambda x: x.filter(ImageFilter.GaussianBlur(1.2))),
    ]
    nombre, f = ops[k % len(ops)]
    return nombre, f(im)

def sombra(x):
    capa = Image.new("L", x.size, 0); d = ImageDraw.Draw(capa)
    d.polygon([(0, 0), (x.width * 0.6, 0), (x.width * 0.2, x.height), (0, x.height)], fill=110)
    return Image.composite(Image.new("RGB", x.size, (0, 0, 0)), x, capa.filter(ImageFilter.GaussianBlur(25)))

def guardar(im, conjunto, nombre):
    os.makedirs(os.path.join(SALIDA, conjunto), exist_ok=True)
    rel = f"{conjunto}/{nombre}.jpg"
    im.save(os.path.join(SALIDA, rel), "JPEG", quality=85)
    return rel

def huella(ruta):
    return hashlib.sha256(open(ruta, "rb").read()).hexdigest()

def fuente(ruta):
    return os.path.relpath(ruta, RAIZ).replace("\\", "/")

def main():
    if not os.path.isdir(REALES):
        sys.exit("No se encuentra la carpeta de fotografias reales: " + REALES)
    if os.path.isdir(SALIDA):
        for sub in ("referencias", "evaluacion", "multirresiduo", "negativas"):
            shutil.rmtree(os.path.join(SALIDA, sub), ignore_errors=True)
    entradas, faltantes = [], []
    usados_eval = set()

    for cod, lista in REF.items():
        originales = []
        for i, (origen, ruta, ajuste) in enumerate(lista[:POR_MATERIAL_REF], 1):
            im = abrir(ruta); originales.append((ruta, im))
            rel = guardar(im, "referencias", f"{cod}_ref_{i:02d}_{origen}")
            entradas.append(dict(archivo=rel, conjunto="referencia", origen=origen, materiales=[cod], canecas=[CANECA[cod]],
                                 fuente=fuente(ruta), sha256_fuente=huella(ruta), usada_en_ajuste=ajuste))
        k = 0
        while sum(1 for e in entradas if e["conjunto"] == "referencia" and e["materiales"] == [cod]) < POR_MATERIAL_REF:
            ruta, im = originales[k % len(originales)]
            nombre, var = aumentar(im, k)
            rel = guardar(var, "referencias", f"{cod}_aum_{k + 1:02d}_{nombre}")
            entradas.append(dict(archivo=rel, conjunto="referencia", origen="aumentada", transformacion=nombre, materiales=[cod],
                                 canecas=[CANECA[cod]], deriva_de=fuente(ruta), usada_en_ajuste=False))
            k += 1

    eval_im = {}
    for cod, lista in EVAL.items():
        for i, (origen, ruta) in enumerate(lista[:POR_MATERIAL_EVAL], 1):
            im = abrir(ruta); eval_im.setdefault(cod, []).append(im); usados_eval.add(ruta)
            rel = guardar(im, "evaluacion", f"{cod}_eval_{i:02d}_{origen}")
            entradas.append(dict(archivo=rel, conjunto="evaluacion", origen=origen, materiales=[cod], canecas=[CANECA[cod]],
                                 fuente=fuente(ruta), sha256_fuente=huella(ruta), usada_en_ajuste=False))
        k = 0
        while sum(1 for e in entradas if e["conjunto"] == "evaluacion" and e["materiales"] == [cod]) < POR_MATERIAL_EVAL:
            ruta = lista[k % len(lista)][1]; nombre, f = DERIVADOS_EVAL[k % len(DERIVADOS_EVAL)]
            rel = guardar(f(abrir(ruta)), "evaluacion", f"{cod}_evald_{k + 1:02d}_{nombre}")
            entradas.append(dict(archivo=rel, conjunto="evaluacion", origen="aumentada", transformacion=nombre, materiales=[cod],
                                 canecas=[CANECA[cod]], deriva_de=fuente(ruta), usada_en_ajuste=False,
                                 nota="derivado digital de un original de evaluacion; no es una fotografia fisica independiente"))
            k += 1
        if len(lista) < POR_MATERIAL_EVAL:
            faltantes.append(dict(material=cod, nombre=NOMBRE[cod], conjunto="evaluacion", disponibles=len(lista), meta=POR_MATERIAL_EVAL,
                                  motivo="faltan fotografias reales independientes del objeto fisico"))
        reales_ref = sum(1 for o, _, _ in REF[cod] if o == "real")
        reales_eval = sum(1 for o, _ in lista if o == "real")
        if reales_ref + reales_eval == 0:
            faltantes.append(dict(material=cod, nombre=NOMBRE[cod], conjunto="real", disponibles=0, meta=15,
                                  motivo="no hay ninguna fotografia real del objeto en el stand; solo ilustraciones del catalogo"))

    for i, (n, mats, otros, ajuste) in enumerate(MULTI_REAL, 1):
        ruta = real(n)[1]
        rel = guardar(abrir(ruta), "multirresiduo", f"M{i:02d}_real_{'_'.join(mats)}")
        entradas.append(dict(archivo=rel, conjunto="multirresiduo", origen="real", materiales=mats, objetos_fuera_de_alcance=otros,
                             canecas=sorted({CANECA[m] for m in mats}, key=["VERDE", "BLANCA", "NEGRA"].index),
                             fuente=fuente(ruta), sha256_fuente=huella(ruta), usada_en_ajuste=ajuste))
    for j, combo in enumerate(COMBOS, len(MULTI_REAL) + 1):
        piezas = [random.choice(eval_im[c]) for c in combo]
        celda = 320 if len(piezas) <= 2 else 300
        cols = len(piezas) if len(piezas) <= 3 else 2
        filas = (len(piezas) + cols - 1) // cols
        fondo = random.choice([(236, 236, 232), (60, 60, 62), (205, 190, 165)])
        lienzo = Image.new("RGB", (cols * celda, filas * celda), fondo)
        orden = list(range(len(piezas))); random.shuffle(orden)
        for pos, idx in enumerate(orden):
            p = piezas[idx].copy(); p.thumbnail((celda - 16, celda - 16))
            x = (pos % cols) * celda + (celda - p.width) // 2; y = (pos // cols) * celda + (celda - p.height) // 2
            lienzo.paste(p, (x, y))
        rel = guardar(lienzo, "multirresiduo", f"M{j:02d}_compuesta_{'_'.join(combo)}")
        entradas.append(dict(archivo=rel, conjunto="multirresiduo", origen="compuesta", materiales=combo,
                             canecas=sorted({CANECA[m] for m in combo}, key=["VERDE", "BLANCA", "NEGRA"].index),
                             nota="escena compuesta digitalmente con originales del conjunto de evaluacion; no es una fotografia fisica",
                             usada_en_ajuste=False))

    for i, (origen, ruta, desc, especial, ajuste) in enumerate(NEGATIVAS, 1):
        rel = guardar(abrir(ruta), "negativas", f"X{i:02d}_{origen}")
        entradas.append(dict(archivo=rel, conjunto="negativa", origen=origen, materiales=[], canecas=[], descripcion=desc,
                             gestion_especial=especial, fuente=fuente(ruta), sha256_fuente=huella(ruta), usada_en_ajuste=ajuste))

    ref_fuentes = {e.get("fuente") for e in entradas if e["conjunto"] == "referencia"} | {e.get("deriva_de") for e in entradas if e["conjunto"] == "referencia"}
    ref_fuentes.discard(None)
    fuga = [e["archivo"] for e in entradas if e["conjunto"] == "evaluacion" and e.get("fuente") in ref_fuentes]
    assert not fuga, f"Fuga entre referencia y evaluacion: {fuga}"
    ref_deriv = {e.get("deriva_de") for e in entradas if e["conjunto"] == "evaluacion"}
    assert not (ref_deriv - {None}) & ref_fuentes, "Derivado de evaluacion con fuente de referencia"
    assert len(entradas) <= LIMITE, len(entradas)

    resumen = {c: sum(1 for e in entradas if e["conjunto"] == c) for c in ("referencia", "evaluacion", "multirresiduo", "negativa")}
    resumen["total"] = len(entradas)
    resumen["por_origen"] = {o: sum(1 for e in entradas if e["origen"] == o) for o in ("real", "ilustracion", "aumentada", "compuesta")}
    doc = dict(nombre="ECOACCESS_EXPOSOFTWARE_9", version="2026.11.25-expo9-100", objetivo=100, limite=LIMITE, resumen=resumen,
               materiales=[dict(codigo=c, nombre=NOMBRE[c], caneca=CANECA[c]) for c in NOMBRE],
               faltantes=faltantes, entradas=entradas)
    with open(os.path.join(SALIDA, "manifiesto.json"), "w", encoding="utf-8") as f:
        json.dump(doc, f, ensure_ascii=False, indent=1)
    print(json.dumps(resumen, ensure_ascii=False))
    print("faltantes:", len(faltantes))

if __name__ == "__main__":
    main()
