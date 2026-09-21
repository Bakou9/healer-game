"""Prépare les illustrations 2D pour le jeu (D-074) : détourage du fond magenta, recadrage, mise à l'échelle, export vers Unity.

Lancer :  blender --background --python art-2d/tools/process.py -- [hauteur] [--unity]
Entrée :  art-2d/inbox/<unité>_<pose>.png   (fond magenta uni, personnage tourné vers la droite)
Sortie :  art-2d/out/<unité>_<pose>.png     (RGBA détouré, recadré, hauteur normalisée)
          art-2d/out/<unité>_apercu.png     (sur le fond sombre du jeu, pour juger le détourage)
          art-2d/out/art2d_units.txt        (une ligne « unité pose largeur hauteur ancre_pieds » par image)
          avec --unity : copie dans unity/HealerGame/Assets/Resources/Art2D/

Blender ne sert ici que de moteur d'image (numpy + mise à l'échelle de qualité) : aucune scène 3D.

Détourage : le fond est un magenta plat, donc l'alpha vient de la distance à cette couleur (bord progressif), puis les pixels
semi-transparents sont « décontaminés » (on retire la couleur de fond qui a bavé dedans), sinon il reste un liseré rose.
"""
import os
import struct
import sys
import zlib

import bpy
import numpy as np

ICI = os.path.dirname(os.path.abspath(__file__))
RACINE = os.path.normpath(os.path.join(ICI, "..", ".."))
INBOX = os.path.join(RACINE, "art-2d", "inbox")
OUT = os.path.join(RACINE, "art-2d", "out")
UNITY = os.path.join(RACINE, "unity", "HealerGame", "Assets", "Resources", "Art2D")
os.makedirs(OUT, exist_ok=True)

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
nums = [a for a in args if not a.startswith("--")]
HAUTEUR = int(nums[0]) if nums else 512          # hauteur finale de l'illustration, en pixels
VERS_UNITY = "--unity" in args

SEUIL_TEINTE = 0.075     # écart de teinte au fond en-deçà duquel un pixel est considéré comme du fond
INTENSITE_MIN = 0.35     # en-dessous, le pixel est trop sombre pour être du fond : c'est le personnage
FOND_JEU = np.array([26, 24, 40, 255], dtype=np.uint8)

VERS_JEU = {"golem": "boss1", "marsh": "boss2", "ash": "boss3"}   # nom d'art -> identifiant du jeu (core/content/bossN.json)


def nom_jeu(unite):
    return VERS_JEU.get(unite, unite)



def lire_png(chemin):
    """Renvoie un tableau (h, w, 4) de flottants, valeurs telles que stockées dans le PNG, première ligne en haut."""
    img = bpy.data.images.load(chemin)
    img.colorspace_settings.name = "Non-Color"
    w, h = img.size
    a = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]
    bpy.data.images.remove(img)
    return a


def ecrire_png(chemin, arr8):
    h, w = arr8.shape[:2]
    lignes = b"".join(b"\x00" + arr8[y].tobytes() for y in range(h))

    def bloc(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    with open(chemin, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + bloc(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + bloc(b"IDAT", zlib.compress(lignes, 9)) + bloc(b"IEND", b""))


def couleur_de_fond(a, coin=80):
    """
    Carte du fond, MESURÉE au lieu d'être supposée. Les générateurs ne rendent jamais le magenta demandé tel quel :
    la couleur change d'un modèle à l'autre, et surtout elle VARIE dans l'image (dégradé, vignette, lueur colorée).
    On mesure donc les quatre coins et on interpole entre eux : chaque pixel est comparé au fond de sa région,
    ce qui évite de laisser de grands aplats de fond accrochés au personnage.
    """
    rgb = a[..., :3]
    h, w = rgb.shape[:2]
    hg = np.median(rgb[:coin, :coin].reshape(-1, 3), axis=0)
    hd = np.median(rgb[:coin, -coin:].reshape(-1, 3), axis=0)
    bg = np.median(rgb[-coin:, :coin].reshape(-1, 3), axis=0)
    bd = np.median(rgb[-coin:, -coin:].reshape(-1, 3), axis=0)
    u = np.linspace(0, 1, w)[None, :, None]
    v = np.linspace(0, 1, h)[:, None, None]
    haut = hg[None, None, :] * (1 - u) + hd[None, None, :] * u
    bas = bg[None, None, :] * (1 - u) + bd[None, None, :] * u
    carte = haut * (1 - v) + bas * v
    ecart = np.abs(np.stack([hg, hd, bg, bd]) - np.median(np.stack([hg, hd, bg, bd]), axis=0)).mean()
    moyen = (hg + hd + bg + bd) / 4
    print(f"  fond mesuré : #{''.join(f'{int(c * 255):02X}' for c in moyen)}" + (f" (dégradé, écart {ecart:.2f})" if ecart > 0.03 else ""))
    return carte


def detourer(a):
    """
    Sépare le personnage de son fond.

    On ne compare PAS à une distance absolue : selon le générateur, le fond est un magenta vif ou un violet sombre,
    souvent en dégradé, avec une ombre portée et un halo ajoutés d'office. Le point commun de tous ces pixels de fond,
    c'est d'avoir la TEINTE du fond, à l'intensité près (fond assombri = ombre, fond éclairci = halo). On efface donc
    les pixels alignés avec la couleur du fond, en épargnant les plus sombres, qui appartiennent au personnage.

    Une distance absolue, elle, rendait à moitié transparentes les teintes moyennes du personnage, puis leur retirait
    du magenta : les armures gris-bleu viraient au vert.
    """
    rgb = a[..., :3]
    FOND = couleur_de_fond(a)

    norme = np.maximum((FOND ** 2).sum(axis=2), 1e-6)
    intensite = (rgb * FOND).sum(axis=2) / norme                      # 1 = la couleur du fond, < 1 plus sombre, > 1 plus clair
    residu = np.sqrt(((rgb - intensite[..., None] * FOND) ** 2).sum(axis=2) / 3.0)   # écart de teinte au fond
    est_fond = (residu < SEUIL_TEINTE) & (intensite > INTENSITE_MIN)
    part = 100 * est_fond.mean()
    print(f"  fond (dégradé, ombre et halo compris) : {part:.1f} %")
    if part > 97 or part < 25:
        print("  ATTENTION : proportion de fond inhabituelle — vérifiez l'aperçu.")

    # Bord adouci d'un pixel : sans cela la découpe est en escalier sur les diagonales.
    plein = (~est_fond).astype(np.float32)
    lisse = plein.copy()
    lisse[1:, :] += plein[:-1, :]
    lisse[:-1, :] += plein[1:, :]
    lisse[:, 1:] += plein[:, :-1]
    lisse[:, :-1] += plein[:, 1:]
    alpha = np.clip(lisse / 5.0 * 1.8, 0.0, 1.0)
    alpha[plein > 0.5] = 1.0                                          # l'intérieur reste pleinement opaque

    # Décontamination : uniquement sur la frange, là où la couleur du fond a bavé dans celle du personnage.
    partiel = (alpha > 0.02) & (alpha < 0.98)
    propre = rgb.copy()
    a_part = alpha[partiel][:, None]
    propre[partiel] = np.clip((rgb[partiel] - FOND[partiel] * (1 - a_part)) / np.maximum(a_part, 1e-3), 0, 1)
    sortie = np.zeros_like(a)
    sortie[..., :3] = propre
    sortie[..., 3] = alpha
    return sortie


def garder_le_personnage(a, reduction=4, rayon=14):
    """
    Ne garde que le personnage et ce qui le touche : les générateurs ajoutent volontiers un élément de décor
    flottant (un soleil, une lune, une volute), qui deviendrait une tache isolée une fois le fond retiré.

    On étiquette les zones opaques sur une image réduite (assez précis, et bien plus rapide), on garde la plus
    grande, puis toute zone qui la touche à moins de `rayon` pixels — ainsi un orbe tenu en main reste, mais un
    astre à l'autre bout de l'image disparaît.
    """
    plein = a[..., 3] > 0.5
    if not plein.any():
        return a
    h, w = plein.shape
    hr, wr = h // reduction, w // reduction
    petit = plein[:hr * reduction, :wr * reduction].reshape(hr, reduction, wr, reduction).any(axis=(1, 3))

    # Étiquetage : chaque pixel prend le plus grand numéro de son voisinage, jusqu'à stabilité.
    lab = np.where(petit, np.arange(petit.size).reshape(petit.shape), -1)
    for _ in range(400):
        avant = lab
        v = lab.copy()
        v[1:, :] = np.maximum(v[1:, :], lab[:-1, :])
        v[:-1, :] = np.maximum(v[:-1, :], lab[1:, :])
        v[:, 1:] = np.maximum(v[:, 1:], lab[:, :-1])
        v[:, :-1] = np.maximum(v[:, :-1], lab[:, 1:])
        lab = np.where(petit, v, -1)
        if np.array_equal(lab, avant):
            break

    numeros, tailles = np.unique(lab[petit], return_counts=True)
    principal = numeros[tailles.argmax()]
    garde = lab == principal
    # dilatation du personnage, pour rattraper ce qu'il touche presque
    rr = max(1, rayon // reduction)
    proche = garde.copy()
    for _ in range(rr):
        d = proche.copy()
        d[1:, :] |= proche[:-1, :]
        d[:-1, :] |= proche[1:, :]
        d[:, 1:] |= proche[:, :-1]
        d[:, :-1] |= proche[:, 1:]
        proche = d
    for n, t in zip(numeros, tailles):
        if n != principal and (proche & (lab == n)).any():
            garde |= lab == n

    rejete = (~garde) & petit
    if rejete.any():
        print(f"  éléments détachés retirés : {100 * rejete.sum() / max(1, petit.sum()):.1f} % de la matière")
    masque = np.zeros((h, w), dtype=bool)
    masque[:hr * reduction, :wr * reduction] = np.repeat(np.repeat(garde, reduction, axis=0), reduction, axis=1)
    sortie = a.copy()
    sortie[..., 3] *= masque
    return sortie


def recadrer(a, marge=2):
    """Recadre sur la boîte englobante des pixels visibles (avec une petite marge)."""
    opaque = a[..., 3] > 0.03
    ys, xs = np.where(opaque)
    if len(ys) == 0:
        return a
    y0, y1 = max(0, ys.min() - marge), min(a.shape[0], ys.max() + 1 + marge)
    x0, x1 = max(0, xs.min() - marge), min(a.shape[1], xs.max() + 1 + marge)
    return a[y0:y1, x0:x1]


def redimensionner(a, hauteur):
    """Mise à l'échelle par Blender (bonne qualité) en couleurs prémultipliées, pour ne pas tirer du magenta dans la frange."""
    h, w = a.shape[:2]
    if h == hauteur:
        return a
    largeur = max(1, int(round(w * hauteur / h)))
    pre = a.copy()
    pre[..., :3] *= pre[..., 3:4]
    img = bpy.data.images.new("redim", width=w, height=h, alpha=True)
    img.colorspace_settings.name = "Non-Color"
    img.pixels = pre[::-1].ravel().tolist()
    img.scale(largeur, hauteur)
    petit = np.array(img.pixels[:], dtype=np.float32).reshape(hauteur, largeur, 4)[::-1]
    bpy.data.images.remove(img)
    alpha = petit[..., 3:4]
    sortie = petit.copy()
    sortie[..., :3] = np.clip(petit[..., :3] / np.maximum(alpha, 1e-4), 0, 1)
    return sortie


def vers_octets(a):
    return (np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8)


def sur_fond(arr8):
    toile = np.tile(FOND_JEU, (arr8.shape[0], arr8.shape[1], 1))
    al = arr8[..., 3:4].astype(np.float32) / 255
    toile[..., :3] = (arr8[..., :3] * al + toile[..., :3] * (1 - al)).astype(np.uint8)
    return toile


def ancre_pieds(arr8):
    """Fraction de la hauteur occupée par le personnage sous son centre : sert à poser les pieds au sol dans le jeu.
    Ici l'image est déjà recadrée, donc les pieds sont tout en bas (1.0) ; la valeur reste explicite pour les poses où ce n'est pas le cas."""
    return 1.0


lignes_index = []
for nom in sorted(os.listdir(INBOX)):
    if not nom.lower().endswith(".png"):
        continue
    base = os.path.splitext(nom)[0]
    unite, _, pose = base.partition("_")
    pose = pose or "idle"
    brut = lire_png(os.path.join(INBOX, nom))
    fini = redimensionner(recadrer(garder_le_personnage(detourer(brut))), HAUTEUR)
    octets = vers_octets(fini)
    chemin = os.path.join(OUT, f"{unite}_{pose}.png")
    ecrire_png(chemin, octets)
    ecrire_png(os.path.join(OUT, f"{unite}_{pose}_apercu.png"), sur_fond(octets))
    h, w = octets.shape[:2]
    lignes_index.append(f"{unite} {pose} {w} {h} {ancre_pieds(octets):.3f}")
    print(f"TRAITE {unite} {pose} : {brut.shape[1]}x{brut.shape[0]} -> {w}x{h}")
    if VERS_UNITY:
        os.makedirs(UNITY, exist_ok=True)
        ecrire_png(os.path.join(UNITY, f"{nom_jeu(unite)}_{pose}.png"), octets)

index = os.path.join(OUT, "art2d_units.txt")
with open(index, "w", encoding="utf-8") as f:
    f.write("\n".join(lignes_index) + "\n")
if VERS_UNITY and lignes_index:
    os.makedirs(UNITY, exist_ok=True)
    with open(os.path.join(UNITY, "art2d_units.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(lignes_index) + "\n")
print("FINI", len(lignes_index), "illustration(s), hauteur", HAUTEUR)
