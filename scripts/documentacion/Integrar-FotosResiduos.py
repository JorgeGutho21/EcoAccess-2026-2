"""
Integra las fotografías de las carpetas CANECA BLANCA, CANECA VERDE y CANECA NEGRA al catálogo de EcoAccess.

Uso (Python 3 con Pillow):
    python scripts/documentacion/Integrar-FotosResiduos.py "<carpeta que contiene CANECA BLANCA, CANECA VERDE y CANECA NEGRA>"

Qué hace:
  * Recorre las tres carpetas (recursivamente), cuenta, mide y detecta duplicados (mismo contenido) y archivos dañados.
  * NO modifica los originales. Genera versiones de 360 px (JPEG) en assets/imagenes/residuos/<caneca>/fotos/ y las
    guías visuales (láminas con varios residuos) en assets/imagenes/residuos/<caneca>/guias/.
  * Cada foto se asocia a un residuo del catálogo (CatalogoPrototipo) según lo que muestra la imagen, revisada una por
    una el 21/11/2026; el nombre del archivo NO decide la clasificación. Lo que no tiene un residuo equivalente en el
    catálogo queda «pendiente» con la razón, y no se muestra como material depositable.
  * Escribe docs/evidencias/catalogo_fotos/inventario_fotos.csv y src/EcoAccess.P3.Logica/Servicios/CatalogoFotos.g.cs.

Si se agregan fotos nuevas, añádelas a ASIGNACION (archivo -> residuo) después de mirarlas; las que no estén en la
tabla se reportan como «sin revisar» y no se integran.
"""
import csv
import hashlib
import os
import sys
import unicodedata

from PIL import Image, ImageOps

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CARPETAS = {'CANECA BLANCA': 'blanca', 'CANECA VERDE': 'verde', 'CANECA NEGRA': 'negra'}
LADO = 360

# Guía visual (lámina con varios residuos): se conserva como guía de la caneca, no como un material.
GUIA = 'guia'

# Archivo original -> id del residuo en CatalogoPrototipo, GUIA, o ('pendiente', razón).
ASIGNACION = {
    # ---------------- CANECA BLANCA ----------------
    'Guía visual de reciclaje de papel y cartón-2.png': GUIA,
    'Imagen de ChatGPT 21 nov 2026, 10_37_16 a.m-1.png': 'envase-plastico',      # tapa/tarro plástico blanco
    'Imagen de ChatGPT 21 nov 2026, 10_37_17 a.m-2.png': 'vasos-plasticos',      # vaso plástico transparente limpio
    'Imagen de ChatGPT 21 nov 2026, 10_37_17 a.m-3.png': 'envase-plastico',      # frasco dispensador (jabón/champú)
    'Imagen de ChatGPT 21 nov 2026, 10_37_18 a.m-4.png': 'envase-plastico',      # galón de detergente
    'Imagen de ChatGPT 21 nov 2026, 10_37_19 a.m-5.png': 'envase-plastico',      # tarro plástico blanco
    'Imagen de ChatGPT 21 nov 2026, 10_37_19 a.m-6.png': ('pendiente', 'Bolsa plástica: Oracle tiene BOLSA_PLASTICA, pero el catálogo del piloto aún no la incluye.'),
    'Imagen de ChatGPT 21 nov 2026, 10_37_20 a.m-8.png': 'envase-plastico',      # recipiente plástico con tapa
    'Imagen de ChatGPT 21 nov 2026, 10_37_21 a.m-10.png': 'papel-limpio',        # resma de hojas
    'Imagen de ChatGPT 21 nov 2026, 10_37_21 a.m-9.png': ('pendiente', 'Bolsa blanca de material ambiguo (papel, tela o plástico): confirmar antes de clasificar.'),
    'Mano Eco sosteniendo caja de cartón-10.png': 'carton-limpio',
    'Mano robótica ecológica con periódico-3.png': 'papel-limpio',
    'Mano robótica ecológica con refresco frío-1.png': 'lata-aluminio',
    'Mano robótica ecológica sosteniendo frasco de vidrio-5.png': 'vidrio',
    'Mano robótica ecológica sosteniendo frasco de vidrio.png': 'vidrio',
    'Mano robótica sosteniendo botella de vidrio-4.png': 'vidrio',
    'Mano robótica sosteniendo caja de cereal-12.png': 'carton-limpio',
    'Mano robótica sosteniendo caja de cereal-13.png': 'carton-limpio',
    'Mano robótica sosteniendo caja ecológica-11.png': 'carton-limpio',
    'Mano robótica sosteniendo cartulina verde-8.png': 'carton-limpio',
    'Mano robótica sosteniendo cartulina verde-9.png': 'carton-limpio',
    'Mano robótica sosteniendo folletos ecológicos-7.png': 'papel-limpio',
    'Mano robótica sosteniendo maíz enlatado-3.png': ('pendiente', 'Lata de conserva (acero) llena: el catálogo solo tiene lata de aluminio; además debe mostrarse vacía.'),
    'Mano robótica sosteniendo periódico reciclado-1.png': 'papel-limpio',
    'Mano robótica sosteniendo revista ecológica-4.png': 'papel-limpio',
    'Mano robótica sosteniendo revista ecológica-5.png': 'papel-limpio',
    'Mano robótica sosteniendo una tapa dorada-2.png': ('pendiente', 'Tapa metálica: Oracle tiene METAL, pero el catálogo del piloto aún no la incluye.'),
    'Volantes promocionales en mano robótica ecológica-6.png': 'papel-limpio',
    # ---------------- CANECA VERDE ----------------
    'Imagen de ChatGPT 21 nov 2026, 11_31_34 a.m-1.png': 'restos-comida',        # arroz
    'Imagen de ChatGPT 21 nov 2026, 11_31_35 a.m-2.png': 'cafe-te',              # hojas de té usadas
    'Imagen de ChatGPT 21 nov 2026, 11_31_36 a.m-3.png': 'restos-verdura',       # pepino
    'Imagen de ChatGPT 21 nov 2026, 11_31_37 a.m-4.png': 'restos-verdura',       # cáscaras de cebolla
    'Imagen de ChatGPT 21 nov 2026, 11_31_37 a.m-5.png': 'cafe-te',              # borra de café
    'Imagen de ChatGPT 21 nov 2026, 11_31_38 a.m-6.png': 'poda-jardin',          # pasto
    'Imagen de ChatGPT 21 nov 2026, 11_31_39 a.m-7.png': 'poda-jardin',          # flores marchitas
    'Imagen de ChatGPT 21 nov 2026, 11_31_39 a.m-8.png': 'poda-jardin',          # ramas pequeñas
    'Imagen de ChatGPT 21 nov 2026, 11_31_40 a.m-9.png': 'restos-verdura',       # verduras mixtas
    'Imagen de ChatGPT 21 nov 2026, 11_31_41 a.m-10.png': 'hojas-secas',
    'Mano robótica ECO con cáscaras de yuca-12.png': 'cascaras-alimentos',
    'Mano robótica ECO con fruta fresca-5.png': 'restos-fruta',
    'Mano robótica ECO y manzana podrida-6.png': 'restos-fruta',
    'Mano robótica con cáscara de mandarina-5.png': 'cascara-mandarina',
    'Mano robótica con frutas coloridas-7.png': 'restos-fruta',
    'Mano robótica con frutas frescas-3.png': 'restos-fruta',
    'Mano robótica con frutas frescas-9.png': 'restos-fruta',
    'Mano robótica ecológica con cáscara de banana-1.png': 'cascara-banano',
    'Mano robótica ecológica con cáscara de naranja-3.png': 'cascara-naranja',
    'Mano robótica sosteniendo cáscara de mango-7.png': 'cascaras-alimentos',
    'Mano robótica sosteniendo cáscara de mango-8.png': 'cascaras-alimentos',
    'Mano robótica sosteniendo cáscara de naranja-4.png': 'cascara-naranja',
    'Mano robótica sosteniendo cáscara de papaya-9.png': 'cascaras-alimentos',
    'Mano robótica sosteniendo cáscara de piña-10.png': 'cascaras-alimentos',
    'Mano robótica sosteniendo espirales de mandarina-6.png': 'cascara-mandarina',
    'Mano robótica sosteniendo frutas frescas-13.png': 'restos-fruta',
    'Mano robótica sosteniendo restos de fruta-8.png': 'restos-fruta',
    'Mano robótica sosteniendo una cáscara de aguacate-4.png': 'cascaras-alimentos',
    'Mano robótica sosteniendo una cáscara de banana-2.png': 'cascara-banano',
    'Mano robótica sosteniendo una cáscara de lima-1.png': 'cascaras-alimentos',
    'Mano robótica sosteniendo una cáscara de papa-10.png': 'cascaras-alimentos',
    'Manos ECO con residuos compostables-2.png': GUIA,
    'Residuos orgánicos en manos robóticasitsoq-11.png': GUIA,
    # ---------------- CANECA NEGRA ----------------
    'Gemini_Generated_Image_a3r2spa3r2spa3r2.jpg': 'condon-usado',
    'Gemini_Generated_Image_w01sf3w01sf3w01s.jpg': 'colilla',
    'Imagen de ChatGPT 21 nov 2026, 12_05_00 p.m-1.png': 'icopor-sucio',         # bandeja desechable con comida
    'Imagen de ChatGPT 21 nov 2026, 12_05_01 p.m-2.png': 'cubiertos-desechables', # pitillo
    'Imagen de ChatGPT 21 nov 2026, 12_05_02 p.m-3.png': 'cubiertos-desechables', # cubiertos plásticos
    'Imagen de ChatGPT 21 nov 2026, 12_05_03 p.m-4.png': ('pendiente', 'Palillo de madera usado: no hay residuo de madera en el catálogo del piloto.'),
    'Imagen de ChatGPT 21 nov 2026, 12_05_03 p.m-5.png': 'vaso-contaminado',     # vaso desechable sucio
    'Imagen de ChatGPT 21 nov 2026, 12_05_05 p.m-6.png': 'papel-contaminado',    # papel con grasa
    'Imagen de ChatGPT 21 nov 2026, 12_05_06 p.m-7.png': ('pendiente', 'Papel aluminio con comida: no hay un residuo equivalente en el catálogo del piloto.'),
    'Imagen de ChatGPT 21 nov 2026, 12_05_07 p.m-8.png': ('pendiente', 'Película plástica (vinipel) sucia: no hay un residuo equivalente en el catálogo del piloto.'),
    'Imagen de ChatGPT 21 nov 2026, 12_05_08 p.m-9.png': ('pendiente', 'Plástico/cinta sucia de material no identificable: confirmar antes de clasificar.'),
    'Imagen de ChatGPT 21 nov 2026, 12_05_09 p.m-10.png': ('pendiente', 'Plato desechable sucio de material ambiguo (icopor, papel o plástico): confirmar antes de clasificar.'),
    'Mano ECO sosteniendo servilleta usada-10.png': 'servilletas-usadas',
    'Mano Robótica Sosteniendo Caja de Pizza Usada-23.png': 'papel-contaminado',
    'Mano eco robótica con rollo usado-7.png': 'papel-higienico',
    'Mano robótica ECO con servilleta usada-9.png': 'servilletas-usadas',
    'Mano robótica ECO con toalla grasienta-20.png': 'papel-contaminado',
    'Mano robótica ECO sostiene rollo usado-6.png': 'papel-higienico',
    'Mano robótica Eco con cartón grasiento-22.png': 'papel-contaminado',
    'Mano robótica con bolsa de basura 白小姐-15.png': ('pendiente', 'Bolsa negra de basura con residuos: muestra una mezcla, no un material.'),
    'Mano robótica con envoltura de galletas-3.png': 'envolturas',
    'Mano robótica ecológica con bolsa de chips-26.png': 'envolturas',
    'Mano robótica ecológica con papel arrugado-5.png': 'papel-contaminado',     # el nombre dice «papel arrugado», pero se ve manchado
    'Mano robótica ecológica con papel usado-1.png': 'servilletas-usadas',
    'Mano robótica ecológica con papel usado-4.png': 'papel-higienico',
    'Mano robótica ecológica con pañuelo usado-16.png': 'servilletas-usadas',
    'Mano robótica ecológica con toalla grasienta-17.png': 'papel-contaminado',
    'Mano robótica sosteniendo bolsa crujiente-25.png': 'envolturas',
    'Mano robótica sosteniendo bolsa de papas-28.png': 'envolturas',
    'Mano robótica sosteniendo caja de pizza usada-24.png': 'papel-contaminado',
    'Mano robótica sosteniendo caja grasienta-21.png': 'papel-contaminado',
    'Mano robótica sosteniendo paño engrasado-19.png': 'papel-contaminado',
    'Mano robótica sosteniendo pañuelo usado-14.png': 'servilletas-usadas',
    'Mano robótica sosteniendo residuos-11.png': ('pendiente', 'Bolsa negra con una servilleta: muestra una mezcla, no un material.'),
    'Mano robótica sosteniendo residuos-27.png': ('pendiente', 'Bolsa negra con una envoltura: muestra una mezcla, no un material.'),
    'Mano robótica sosteniendo rollo usado-8.png': 'papel-higienico',
    'Mano robótica sosteniendo servilleta grasienta-18.png': 'servilletas-usadas',
    'Mano robótica sosteniendo servilleta manchada-12.png': 'servilletas-usadas',
    'Puño robótico exprimiendo pañuelo manchado-13.png': 'servilletas-usadas',
    'Robomano ecológica con servilleta manchada-2.png': 'servilletas-usadas',
}

# Caneca de cada residuo (debe coincidir con CatalogoPrototipo; una prueba unitaria lo comprueba).
CANECA_RESIDUO = {
    **{r: 'verde' for r in ['cascara-banano', 'cascara-naranja', 'hojas-secas', 'mitad-kiwi', 'cascara-mandarina', 'mitad-limon',
                             'restos-fruta', 'restos-verdura', 'cascaras-alimentos', 'restos-comida', 'poda-jardin', 'cafe-te', 'semillas-tusas']},
    **{r: 'blanca' for r in ['botella-agua', 'botella-gaseosa', 'papel-cuaderno', 'caja-jugo', 'vasos-plasticos', 'botella-pet',
                              'envase-plastico', 'carton-limpio', 'papel-limpio', 'vidrio', 'lata-aluminio', 'caja-leche']},
    **{r: 'negra' for r in ['papel-higienico', 'servilletas-usadas', 'vaso-contaminado', 'plato-icopor', 'papel-contaminado', 'envolturas',
                             'cubiertos-desechables', 'icopor-sucio', 'condon-usado', 'toalla-higienica', 'tampon-usado', 'panal-usado', 'colilla']},
}


def nfc(texto):
    return unicodedata.normalize('NFC', texto)


def main(origen):
    asignacion = {nfc(k): v for k, v in ASIGNACION.items()}
    filas, vistos, generado = [], {}, []
    contadores = {}
    for carpeta, caneca in CARPETAS.items():
        ruta = os.path.join(origen, carpeta)
        if not os.path.isdir(ruta):
            sys.exit(f'No existe la carpeta {ruta}')
        archivos = sorted(os.path.join(d, f) for d, _, fs in os.walk(ruta) for f in fs)
        for archivo in archivos:
            nombre = nfc(os.path.basename(archivo))
            fila = {'carpeta': carpeta, 'archivo': nombre, 'ancho': '', 'alto': '', 'sha1': '', 'estado': '', 'residuo': '', 'recurso': '', 'nota': ''}
            try:
                with Image.open(archivo) as im:
                    im.load()
                    fila['ancho'], fila['alto'] = im.size
                    imagen = ImageOps.exif_transpose(im).convert('RGB')
            except Exception as error:  # archivo dañado o que no es imagen
                fila['estado'], fila['nota'] = 'descartada', f'No se pudo leer: {error}'
                filas.append(fila)
                continue
            with open(archivo, 'rb') as f:
                fila['sha1'] = hashlib.sha1(f.read()).hexdigest()
            if fila['sha1'] in vistos:
                fila['estado'], fila['nota'] = 'duplicada', f'Mismo contenido que {vistos[fila["sha1"]]}'
                filas.append(fila)
                continue
            vistos[fila['sha1']] = nombre
            destino = asignacion.get(nombre)
            if destino is None:
                fila['estado'], fila['nota'] = 'sin revisar', 'Agregar a ASIGNACION después de mirar la foto.'
                filas.append(fila)
                continue
            if isinstance(destino, tuple):
                fila['estado'], fila['nota'] = destino
                filas.append(fila)
                continue
            if destino == GUIA:
                clave = f'{caneca}-guia'
                n = contadores[clave] = contadores.get(clave, 0) + 1
                relativa = f'residuos/{caneca}/guias/guia-{n}.jpg'
                imagen.thumbnail((1100, 1100), Image.LANCZOS)
                fila['estado'], fila['nota'] = 'guía', 'Lámina con varios residuos: se muestra como guía de la caneca.'
            else:
                if CANECA_RESIDUO.get(destino) != caneca:
                    sys.exit(f'{nombre}: el residuo {destino} no pertenece a la caneca {caneca}')
                n = contadores[destino] = contadores.get(destino, 0) + 1
                relativa = f'residuos/{caneca}/fotos/{destino}-{n}.jpg'
                imagen = ImageOps.fit(imagen, (LADO, LADO), Image.LANCZOS, centering=(0.5, 0.5))
                fila['estado'], fila['residuo'] = 'integrada', destino
                generado.append((destino, caneca, f'{caneca}.fotos.{destino}-{n}'))
            salida = os.path.join(RAIZ, 'assets', 'imagenes', *relativa.split('/'))
            os.makedirs(os.path.dirname(salida), exist_ok=True)
            imagen.save(salida, 'JPEG', quality=85, optimize=True, progressive=True)
            fila['recurso'] = 'assets/imagenes/' + relativa
            filas.append(fila)

    informe = os.path.join(RAIZ, 'docs', 'evidencias', 'catalogo_fotos')
    os.makedirs(informe, exist_ok=True)
    with open(os.path.join(informe, 'inventario_fotos.csv'), 'w', newline='', encoding='utf-8-sig') as f:
        w = csv.DictWriter(f, fieldnames=list(filas[0].keys()))
        w.writeheader()
        w.writerows(filas)

    guias = sorted({(c, f'{c}.guias.guia-{i}') for c in CARPETAS.values() for i in range(1, contadores.get(f'{c}-guia', 0) + 1)})
    lineas = [
        '// <auto-generated>',
        '// Generado por scripts/documentacion/Integrar-FotosResiduos.py: no editar a mano.',
        '// </auto-generated>',
        'using System.Collections.Generic;',
        '',
        'namespace EcoAccess.P3.Logica.Servicios',
        '{',
        '    public static partial class CatalogoFotos',
        '    {',
        '        /// <summary>(residuo, caneca, clave de imagen) de cada fotografía integrada, en el orden de las carpetas.</summary>',
        '        private static readonly (string Residuo, string Caneca, string Clave)[] Generadas =',
        '        {',
        *[f'            ("{r}", "{c}", "{k}"),' for r, c, k in generado],
        '        };',
        '',
        '        /// <summary>Guías visuales (láminas con varios residuos) de cada caneca.</summary>',
        '        private static readonly (string Caneca, string Clave)[] GuiasGeneradas =',
        '        {',
        *[f'            ("{c}", "{k}"),' for c, k in guias],
        '        };',
        '    }',
        '}',
        '',
    ]
    with open(os.path.join(RAIZ, 'src', 'EcoAccess.P3.Logica', 'Servicios', 'CatalogoFotos.g.cs'), 'w', newline='\r\n', encoding='utf-8-sig') as f:
        f.write('\n'.join(lineas))

    resumen = {}
    for fila in filas:
        clave = (fila['carpeta'], fila['estado'])
        resumen[clave] = resumen.get(clave, 0) + 1
    for (carpeta, estado), n in sorted(resumen.items()):
        print(f'{carpeta:14} {estado:12} {n}')
    print('Total:', len(filas))


if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    main(sys.argv[1])
