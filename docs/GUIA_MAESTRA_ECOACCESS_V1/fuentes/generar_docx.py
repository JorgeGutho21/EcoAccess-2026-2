"""Convierte el Markdown de la guía en un documento Word de estilos editables."""
from datetime import datetime, timezone
from pathlib import Path
import re

from PIL import Image
from docx import Document
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.opc.constants import RELATIONSHIP_TYPE
from docx.shared import Inches, Pt, RGBColor

FUENTES = Path(__file__).resolve().parent
CARPETA = FUENTES.parent
REPO = CARPETA.parents[1]
BASE_WEB = "https://github.com/JorgeGutho21/EcoAccess-2026-2"
BASE_SHA = "a6875cce9f9c5b1c08fba2b4862111fa9121da5f"
RAMA = "docs/guia-maestra-ecoaccess-v1"
TOKEN = re.compile(r"(\*\*[^*]+\*\*|\x60[^\x60]+\x60|\[[^\]]+\]\([^)]+\))")


def nodo(nombre, **atributos):
    elemento = OxmlElement("w:" + nombre)
    for atributo, valor in atributos.items():
        elemento.set(qn("w:" + atributo), str(valor))
    return elemento


def campo(parrafo, instruccion):
    inicio = nodo("fldChar", fldCharType="begin")
    texto = nodo("instrText")
    texto.set(qn("xml:space"), "preserve")
    texto.text = " " + instruccion + " "
    separador = nodo("fldChar", fldCharType="separate")
    for elemento in (inicio, texto, separador):
        parrafo.add_run()._r.append(elemento)
    parrafo.add_run()._r.append(nodo("fldChar", fldCharType="end"))


def enlace(parrafo, etiqueta, destino):
    if not destino.startswith(("https://", "http://")):
        ruta = (FUENTES / destino).resolve()
        relativo = ruta.relative_to(REPO).as_posix()
        if not ruta.exists():
            raise FileNotFoundError(ruta)
        ref = RAMA if ruta.is_relative_to(CARPETA) else BASE_SHA
        destino = f"{BASE_WEB}/blob/{ref}/{relativo}"
    relacion = parrafo.part.relate_to(destino, RELATIONSHIP_TYPE.HYPERLINK, is_external=True)
    hipervinculo = nodo("hyperlink")
    hipervinculo.set(qn("r:id"), relacion)
    run = nodo("r")
    props = nodo("rPr")
    props.append(nodo("color", val="245749"))
    props.append(nodo("u", val="single"))
    run.append(props)
    texto = nodo("t")
    texto.text = etiqueta
    run.append(texto)
    hipervinculo.append(run)
    parrafo._p.append(hipervinculo)


def formato(parrafo, linea):
    for trozo in TOKEN.split(linea):
        if trozo.startswith("**") and trozo.endswith("**"):
            parrafo.add_run(trozo[2:-2]).bold = True
        elif trozo.startswith("\x60") and trozo.endswith("\x60"):
            run = parrafo.add_run(trozo[1:-1])
            run.font.name = "Consolas"
            run.font.size = Pt(9.4)
            run.font.color.rgb = RGBColor.from_string("244139")
        elif trozo.startswith("[") and "](" in trozo and trozo.endswith(")"):
            etiqueta, destino = trozo[1:-1].split("](", 1)
            enlace(parrafo, etiqueta, destino)
        elif trozo:
            parrafo.add_run(trozo)


def tabla(doc, filas):
    encabezado = filas[0]
    if doc.paragraphs:
        previo = doc.paragraphs[-1]
        if previo.text and previo.style.name == "Normal" and encabezado[0] == "Columna":
            previo.paragraph_format.keep_with_next = True
    resultado = doc.add_table(rows=0, cols=len(encabezado))
    resultado.autofit = False
    anchos = {
        "Columna": (2.30, 1.32, 0.77, 2.36),
        "Tapa": (0.76, 1.65, 0.78, 0.57, 2.99),
        "Capa": (1.02, 2.64, 3.09),
        "Partición v2b": (1.65, 0.71, 1.14, 1.64, 1.61),
    }.get(encabezado[0])
    if not anchos:
        if len(encabezado) == 2:
            anchos = (2.0, 4.75)
        elif len(encabezado) == 3:
            anchos = (1.46, 2.45, 2.84)
        else:
            anchos = (6.75 / len(encabezado),) * len(encabezado)
    for col, ancho in zip(resultado.columns, anchos):
        col.width = Inches(ancho)
    propiedades = resultado._tbl.tblPr
    bordes = nodo("tblBorders")
    for borde in ("top", "left", "bottom", "right", "insideH", "insideV"):
        bordes.append(nodo(borde, val="single", sz=4, color="D9D9D9"))
    propiedades.append(bordes)
    margen = nodo("tblCellMar")
    for lado, espacio in (("top", 63), ("bottom", 63), ("start", 120), ("end", 120)):
        margen.append(nodo(lado, w=espacio, type="dxa"))
    propiedades.append(margen)
    for indice, valores in enumerate(filas):
        fila = resultado.add_row()
        fila._tr.get_or_add_trPr().append(nodo("cantSplit"))
        if indice == 0:
            fila._tr.get_or_add_trPr().append(nodo("tblHeader"))
        for i, (celda, texto) in enumerate(zip(fila.cells, valores)):
            celda.width = Inches(anchos[i])
            celda.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
            fill = "DCEAF1" if indice == 0 else ("F5F8FA" if indice % 2 == 0 else "FFFFFF")
            celda._tc.get_or_add_tcPr().append(nodo("shd", fill=fill))
            p = celda.paragraphs[0]
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.space_after = Pt(1)
            p.paragraph_format.line_spacing = 1.05
            if indice <= 1 and len(filas) > 2:
                p.paragraph_format.keep_with_next = True
            formato(p, texto)
            for run in p.runs:
                run.font.size = Pt(9)
                if indice == 0:
                    run.bold = True
                    run.font.color.rgb = RGBColor(0, 0, 0)
            if re.fullmatch(r"[\d,. %]+|Sí|No", texto):
                p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p = doc.add_paragraph()
    p.paragraph_format.space_after = Pt(0)
    p.paragraph_format.line_spacing = Pt(5)


def estilos(doc):
    normal = doc.styles["Normal"]
    normal.font.name = "Aptos"
    normal.font.size = Pt(11)
    normal.font.color.rgb = RGBColor.from_string("202620")
    normal.paragraph_format.space_after = Pt(6)
    normal.paragraph_format.line_spacing = 1.10
    for i, tam in ((1, 22), (2, 15), (3, 12)):
        estilo = doc.styles[f"Heading {i}"]
        estilo.font.name = "Aptos Display"
        estilo.font.size = Pt(tam)
        estilo.font.bold = True
        estilo.font.color.rgb = RGBColor(0, 0, 0)
        estilo.paragraph_format.space_before = Pt(13 if i < 3 else 10)
        estilo.paragraph_format.space_after = Pt(5)
        estilo.paragraph_format.keep_with_next = True
    for nombre, tam in (("Title", 33), ("Subtitle", 15)):
        estilo = doc.styles[nombre]
        estilo.font.name = "Aptos Display"
        estilo.font.size = Pt(tam)
        estilo.font.color.rgb = RGBColor(0, 0, 0)
        ppr = estilo._element.get_or_add_pPr()
        for borde in ppr.findall(qn("w:pBdr")):
            ppr.remove(borde)
    for nombre in ("Header", "Footer", "Caption"):
        estilo = doc.styles[nombre]
        estilo.font.name = "Aptos"
        estilo.font.size = Pt(9)
        estilo.font.color.rgb = RGBColor(0, 0, 0)
    for nombre in ("Header", "Footer"):
        doc.styles[nombre].paragraph_format.space_after = Pt(0)
    doc.styles["Caption"].font.italic = True
    doc.styles["Caption"].paragraph_format.space_before = Pt(4)
    doc.styles["Caption"].paragraph_format.space_after = Pt(9)
    configuracion = doc.settings.element
    configuracion.append(nodo("updateFields", val="true"))


def portada(doc):
    seccion = doc.sections[0]
    seccion.page_width = Inches(8.5)
    seccion.page_height = Inches(11)
    seccion.left_margin = Inches(0.875)
    seccion.right_margin = Inches(0.875)
    seccion.top_margin = Inches(0.85)
    seccion.bottom_margin = Inches(0.78)
    seccion.header_distance = Inches(0.43)
    seccion.footer_distance = Inches(0.43)
    seccion.different_first_page_header_footer = True
    cabecera = seccion.header.paragraphs[0]
    cabecera.add_run("UNIVERSIDAD POPULAR DEL CESAR   /   PROGRAMACIÓN DE COMPUTADORES III")
    pie = seccion.footer.paragraphs[0]
    pie.add_run("EcoAccess P3 V1     /     Guía maestra")
    pie.paragraph_format.tab_stops.add_tab_stop(Inches(6.35))
    pie.add_run("\t")
    campo(pie, "PAGE")
    bloque = doc.add_paragraph()
    bloque.paragraph_format.space_before = Pt(40)
    marca = bloque.add_run("UNIVERSIDAD POPULAR DEL CESAR")
    marca.bold = True
    marca.font.size = Pt(12)
    marca.font.color.rgb = RGBColor.from_string("22584B")
    doc.add_paragraph("Programación de Computadores III")
    logo = REPO / "assets/identidad/Logo_EcoAccess.png"
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(30)
    p.paragraph_format.space_after = Pt(26)
    p.add_run().add_picture(str(logo), width=Inches(2.45))
    doc.add_paragraph("EcoAccess P3 V1", "Title")
    doc.add_paragraph("Guía maestra de instalación arquitectura y operación", "Subtitle")
    p = doc.add_paragraph("Jorge Gutierrez Thomas\nHeyder Canchila De Angel\nSaray Cadena Palomino")
    p.paragraph_format.space_before = Pt(38)
    p.paragraph_format.space_after = Pt(24)
    doc.add_paragraph("16 de noviembre de 2026   ·   Edición 1.0")
    doc.add_paragraph("Revisión técnica del código a6875cc")
    doc.add_page_break()
    doc.add_paragraph("Contenido", "Title")
    campo(doc.add_paragraph(), 'TOC \\o "1-1" \\h \\z')
    doc.add_page_break()


def imagen(doc, ruta, alt):
    archivo = FUENTES / ruta
    with Image.open(archivo) as bitmap:
        w, h = bitmap.size
    ancho = min(6.68, 4.5 * w / h)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.keep_with_next = True
    forma = p.add_run().add_picture(str(archivo), width=Inches(ancho))
    forma._inline.docPr.set("descr", alt)


def generar():
    doc = Document()
    doc.core_properties.title = "EcoAccess P3 V1 Guía maestra"
    doc.core_properties.subject = "Instalación, arquitectura, funcionamiento y operación"
    doc.core_properties.author = "Jorge Gutierrez Thomas, Heyder Canchila De Angel y Saray Cadena Palomino"
    doc.core_properties.comments = ""
    doc.core_properties.created = datetime.now(timezone.utc)
    doc.core_properties.modified = doc.core_properties.created
    estilos(doc)
    portada(doc)
    lineas = (FUENTES / "GUIA_MAESTRA.md").read_text(encoding="utf-8-sig").splitlines()
    i = 0
    while i < len(lineas):
        linea = lineas[i].strip()
        i += 1
        if not linea:
            continue
        if linea.startswith("#"):
            marca, titulo = linea.split(" ", 1)
            p = doc.add_heading(titulo, level=len(marca))
            if marca == "#" and titulo.startswith(("07 ", "13 ")):
                p.paragraph_format.page_break_before = True
        elif linea.startswith("!\x5b"):
            alt, ruta = linea[2:-1].split("](", 1)
            imagen(doc, ruta, alt)
        elif linea.startswith("|"):
            filas = [linea]
            while i < len(lineas) and lineas[i].startswith("|"):
                filas.append(lineas[i])
                i += 1
            celdas = [[valor.strip() for valor in fila.strip("|").split("|")] for fila in filas]
            tabla(doc, [celdas[0], *celdas[2:]])
        elif linea.startswith("\x60\x60\x60"):
            while i < len(lineas) and not lineas[i].strip().startswith("\x60\x60\x60"):
                linea_codigo = lineas[i]
                i += 1
                p = doc.add_paragraph()
                pf = p.paragraph_format
                pf.space_before = Pt(0)
                pf.space_after = Pt(0)
                pf.line_spacing = 1.1
                pf.left_indent = Inches(0.12)
                pf.right_indent = Inches(0.12)
                p._p.get_or_add_pPr().append(nodo("shd", fill="F0F4F1"))
                run = p.add_run(linea_codigo or " ")
                run.font.name = "Consolas"
                run.font.size = Pt(9)
                run.font.color.rgb = RGBColor.from_string("17362D")
                if i < len(lineas) and not lineas[i].strip().startswith("\x60\x60\x60"):
                    pf.keep_with_next = True
                else:
                    pf.space_after = Pt(7)
            i += 1
        elif linea.startswith("*Figura") and linea.endswith("*"):
            p = doc.add_paragraph(linea[1:-1], "Caption")
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        elif linea.startswith("- "):
            formato(doc.add_paragraph(style="List Bullet"), linea[2:])
        else:
            formato(doc.add_paragraph(), linea)
    destino = CARPETA / "EcoAccess_V1_GUIA_MAESTRA.docx"
    doc.save(destino)
    print(destino)


if __name__ == "__main__":
    generar()
