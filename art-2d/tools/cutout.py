"""Découpe une illustration en parties articulées (D-075) : « cutout animation », comme Spine ou DragonBones.

Lancer :  blender --background --python art-2d/tools/cutout.py -- <unité> [--grille] [--unity]
Entrée :  art-2d/out/<unité>_idle.png (illustration déjà détourée par process.py)
          art-2d/rigs/<unité>.txt     (description des parties, écrite à la main, voir le format plus bas)
Sortie :  art-2d/out/parts/<unité>_<partie>.png  (une image par partie, recadrée)
          art-2d/out/<unité>_rig.txt             (pour le jeu : parent, pivot, position, ordre d'affichage)
          art-2d/out/<unité>_parts_apercu.png    (les parties réassemblées, écartées, pour vérifier la découpe)
          avec --grille : art-2d/out/<unité>_grille.png, l'illustration avec un repère en pourcentage (sert à écrire le .txt)
          avec --unity  : copie dans unity/HealerGame/Assets/Resources/Art2D/parts/

Format de art-2d/rigs/<unité>.txt (coordonnées en POURCENTAGE de l'image, origine en haut à gauche) :
    partie <nom> <parent|-> <ordre> <pivot_x> <pivot_y>
    poly <x> <y> <x> <y> ...
Les polygones PEUVENT se chevaucher : chaque partie garde ce qui est sous les autres, ce qui évite les trous quand un
membre bouge. L'ordre d'affichage (petit = derrière) décide de ce qu'on voit.
"""
import os
import struct
import sys
import zlib

import bpy
import numpy as np

ICI = os.path.dirname(os.path.abspath(__file__))
RACINE = os.path.normpath(os.path.join(ICI, "..", ".."))
OUT = os.path.join(RACINE, "art-2d", "out")
RIGS = os.path.join(RACINE, "art-2d", "rigs")
PARTS = os.path.join(OUT, "parts")
UNITY = os.path.join(RACINE, "unity", "HealerGame", "Assets", "Resources", "Art2D")
os.makedirs(PARTS, exist_ok=True)

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
noms = [a for a in args if not a.startswith("--")]
UNITE = noms[0] if noms else "healer"
GRILLE = "--grille" in args
VERS_UNITY = "--unity" in args
FOND_JEU = np.array([26, 24, 40, 255], dtype=np.uint8)


def lire_png(chemin):
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


def vers_octets(a):
    return (np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8)


def sur_fond(arr8, fond=None):
    f = FOND_JEU if fond is None else fond
    toile = np.tile(f, (arr8.shape[0], arr8.shape[1], 1))
    al = arr8[..., 3:4].astype(np.float32) / 255
    toile[..., :3] = (arr8[..., :3] * al + toile[..., :3] * (1 - al)).astype(np.uint8)
    return toile


img = lire_png(os.path.join(OUT, f"{UNITE}_idle.png"))
H, W = img.shape[:2]

# ---- repère de travail : l'illustration avec une grille en pourcentage, pour écrire les polygones ----------------
if GRILLE:
    vue = sur_fond(vers_octets(img)).astype(np.int16)
    for p in range(0, 101, 5):
        x = min(W - 1, int(p * W / 100))
        y = min(H - 1, int(p * H / 100))
        fort = (p % 25 == 0)
        teinte = np.array([255, 90, 90] if fort else [90, 200, 255], dtype=np.int16)
        poids = 0.85 if fort else 0.35
        vue[:, x, :3] = (vue[:, x, :3] * (1 - poids) + teinte * poids)
        vue[y, :, :3] = (vue[y, :, :3] * (1 - poids) + teinte * poids)
    ecrire_png(os.path.join(OUT, f"{UNITE}_grille.png"), vue.astype(np.uint8))
    print("GRILLE", f"{UNITE}_grille.png", W, "x", H, "px ; lignes tous les 5 %, rouges tous les 25 %")

# ---- lecture de la description des parties ------------------------------------------------------------------------
chemin_rig = os.path.join(RIGS, f"{UNITE}.txt")
if not os.path.exists(chemin_rig):
    print("PAS DE RIG", chemin_rig, ": seule la grille a été produite")
    sys.exit(0)

parties = []
animations = {}
for ligne in open(chemin_rig, encoding="utf-8"):
    ligne = ligne.strip()
    if not ligne or ligne.startswith("#"):
        continue
    t = ligne.split()
    if t[0] == "partie":
        parties.append({"nom": t[1], "parent": None if t[2] == "-" else t[2], "ordre": int(t[3]),
                        "pivot": (float(t[4]), float(t[5])), "poly": []})
    elif t[0] == "anim":
        animations[t[1]] = " ".join(t[2:])
    elif t[0] == "poly":
        vals = [float(v) for v in t[1:]]
        parties[-1]["poly"] = [(vals[i], vals[i + 1]) for i in range(0, len(vals), 2)]

# ---- rasterisation des polygones (test du rayon horizontal, vectorisé par ligne) ------------------------------------
yy, xx = np.mgrid[0:H, 0:W]
px = xx * 100.0 / W
py = yy * 100.0 / H


def masque_polygone(poly):
    dedans = np.zeros((H, W), dtype=bool)
    n = len(poly)
    for i in range(n):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % n]
        if y1 == y2:
            continue
        coupe = ((py >= np.minimum(y1, y2)) & (py < np.maximum(y1, y2)))
        xinter = x1 + (py - y1) * (x2 - x1) / (y2 - y1)
        dedans ^= coupe & (px < xinter)
    return dedans


def adoucir(masque, passes=2):
    """Bord légèrement fondu : évite une couture nette entre deux parties voisines."""
    m = masque.astype(np.float32)
    for _ in range(passes):
        acc = m.copy()
        acc[1:, :] += m[:-1, :]
        acc[:-1, :] += m[1:, :]
        acc[:, 1:] += m[:, :-1]
        acc[:, :-1] += m[:, 1:]
        m = acc / 5.0
    return np.clip(m * 1.6, 0, 1)


lignes_rig = [f"image {W} {H}"]
apercus = []
for p in parties:
    masque = adoucir(masque_polygone(p["poly"]))
    part = img.copy()
    part[..., 3] *= masque
    visible = part[..., 3] > 0.02
    ys, xs = np.where(visible)
    if len(ys) == 0:
        print("VIDE", p["nom"], ": polygone hors de l'illustration")
        continue
    y0, y1 = ys.min(), ys.max() + 1
    x0, x1 = xs.min(), xs.max() + 1
    decoupe = vers_octets(part[y0:y1, x0:x1])
    ecrire_png(os.path.join(PARTS, f"{UNITE}_{p['nom']}.png"), decoupe)
    if VERS_UNITY:
        os.makedirs(os.path.join(UNITY, "parts"), exist_ok=True)
        ecrire_png(os.path.join(UNITY, "parts", f"{UNITE}_{p['nom']}.png"), decoupe)
    # coordonnées transmises au jeu, en fraction de l'illustration entière (origine en BAS à gauche, comme Unity)
    fx0, fy0 = x0 / W, 1.0 - y1 / H
    fw, fh = (x1 - x0) / W, (y1 - y0) / H
    pvx, pvy = p["pivot"][0] / 100.0, 1.0 - p["pivot"][1] / 100.0
    lignes_rig.append(f"partie {p['nom']} {p['parent'] or '-'} {p['ordre']} {fx0:.5f} {fy0:.5f} {fw:.5f} {fh:.5f} {pvx:.5f} {pvy:.5f}")
    if p["nom"] in animations:
        lignes_rig.append(f"anim {p['nom']} {animations[p['nom']]}")
    apercus.append((p, decoupe, x0, y0))
    print(f"PARTIE {p['nom']:<12} {x1 - x0:4d}x{y1 - y0:4d} px  pivot {p['pivot']}")

chemin_sortie = os.path.join(OUT, f"{UNITE}_rig.txt")
with open(chemin_sortie, "w", encoding="utf-8") as f:
    f.write("\n".join(lignes_rig) + "\n")
if VERS_UNITY:
    with open(os.path.join(UNITY, f"{UNITE}_rig.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(lignes_rig) + "\n")

# ---- aperçu : les parties réassemblées, chacune décalée, pour vérifier la découpe d'un coup d'oeil -------------------
if apercus:
    marge = 30
    toile = np.tile(FOND_JEU, (H + marge * 2, (W + marge) * len(apercus), 1))
    for i, (p, decoupe, x0, y0) in enumerate(apercus):
        ox = i * (W + marge) + marge // 2 + x0
        oy = marge + y0
        zone = toile[oy:oy + decoupe.shape[0], ox:ox + decoupe.shape[1]]
        al = decoupe[..., 3:4].astype(np.float32) / 255
        zone[..., :3] = (decoupe[..., :3] * al + zone[..., :3] * (1 - al)).astype(np.uint8)
        # croix au pivot
        cx = int(p["pivot"][0] * W / 100) + i * (W + marge) + marge // 2
        cy = int(p["pivot"][1] * H / 100) + marge
        toile[max(0, cy - 4):cy + 5, max(0, cx - 1):cx + 2, :3] = np.array([255, 60, 60], dtype=np.uint8)
        toile[max(0, cy - 1):cy + 2, max(0, cx - 4):cx + 5, :3] = np.array([255, 60, 60], dtype=np.uint8)
    ecrire_png(os.path.join(OUT, f"{UNITE}_parts_apercu.png"), toile)
print("FINI", len(apercus), "partie(s) ->", chemin_sortie)
