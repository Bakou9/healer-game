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

SEUIL_PLEIN = 0.42                               # distance au-delà de laquelle le pixel est totalement opaque
SEUIL_VIDE = 0.16                                # distance en-deçà de laquelle le pixel est totalement transparent
FOND_JEU = np.array([26, 24, 40, 255], dtype=np.uint8)


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


def couleur_de_fond(a, bande=4):
    """
    Couleur du fond, MESURÉE sur les bords au lieu d'être supposée : les générateurs d'images ne rendent jamais
    exactement le magenta demandé (l'un donne #FF00FF, l'autre un rose plus sombre). On prend la médiane du pourtour.
    """
    rgb = a[..., :3]
    bord = np.concatenate([
        rgb[:bande].reshape(-1, 3), rgb[-bande:].reshape(-1, 3),
        rgb[:, :bande].reshape(-1, 3), rgb[:, -bande:].reshape(-1, 3),
    ])
    fond = np.median(bord, axis=0)
    ecart = np.abs(bord - fond).mean()
    if ecart > 0.06:
        print(f"ATTENTION : le fond n'est pas uni (écart moyen {ecart:.3f}) — le détourage risque d'être imparfait.")
    return fond


def detourer(a):
    """Alpha depuis la distance à la couleur de fond mesurée, puis décontamination de la frange."""
    rgb = a[..., :3]
    FOND = couleur_de_fond(a)
    print(f"  fond mesuré : #{''.join(f'{int(c * 255):02X}' for c in FOND)}")
    dist = np.sqrt(((rgb - FOND) ** 2).sum(axis=2) / 3.0)
    alpha = np.clip((dist - SEUIL_VIDE) / (SEUIL_PLEIN - SEUIL_VIDE), 0.0, 1.0)

    # Ombre portée et halo : les générateurs en ajoutent malgré la consigne (ils ignorent les tournures négatives).
    # Or un pixel d'ombre, ou de halo, n'est que la couleur du fond assombrie ou éclaircie : il reste ALIGNÉ avec elle
    # (même teinte, intensité différente). On l'efface donc, sauf les pixels très sombres, qui appartiennent au personnage.
    norme = max((FOND ** 2).sum(), 1e-6)
    intensite = (rgb * FOND).sum(axis=2) / norme
    residu = np.sqrt(((rgb - intensite[..., None] * FOND) ** 2).sum(axis=2) / 3.0)
    modulation = (residu < 0.05) & (intensite > 0.35)
    efface = modulation.sum()
    if efface:
        print(f"  ombre/halo effacés : {efface} pixels ({100 * efface / modulation.size:.1f} %)")
    alpha[modulation] = 0.0
    # décontamination : couleur = (observée - fond * (1 - alpha)) / alpha, seulement là où le pixel est partiellement opaque
    partiel = (alpha > 0.02) & (alpha < 0.98)
    propre = rgb.copy()
    a_part = alpha[partiel][:, None]
    propre[partiel] = np.clip((rgb[partiel] - FOND * (1 - a_part)) / np.maximum(a_part, 1e-3), 0, 1)
    sortie = np.zeros_like(a)
    sortie[..., :3] = propre
    sortie[..., 3] = alpha
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
    fini = redimensionner(recadrer(detourer(brut)), HAUTEUR)
    octets = vers_octets(fini)
    chemin = os.path.join(OUT, f"{unite}_{pose}.png")
    ecrire_png(chemin, octets)
    ecrire_png(os.path.join(OUT, f"{unite}_{pose}_apercu.png"), sur_fond(octets))
    h, w = octets.shape[:2]
    lignes_index.append(f"{unite} {pose} {w} {h} {ancre_pieds(octets):.3f}")
    print(f"TRAITE {unite} {pose} : {brut.shape[1]}x{brut.shape[0]} -> {w}x{h}")
    if VERS_UNITY:
        os.makedirs(UNITY, exist_ok=True)
        ecrire_png(os.path.join(UNITY, f"{unite}_{pose}.png"), octets)

index = os.path.join(OUT, "art2d_units.txt")
with open(index, "w", encoding="utf-8") as f:
    f.write("\n".join(lignes_index) + "\n")
if VERS_UNITY and lignes_index:
    os.makedirs(UNITY, exist_ok=True)
    with open(os.path.join(UNITY, "art2d_units.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(lignes_index) + "\n")
print("FINI", len(lignes_index), "illustration(s), hauteur", HAUTEUR)
