"""Druide de la Soigneuse, version 4 (D-069) : modèle haute définition en surfaces lissées.

Lancer :  blender --background --python art/blender/druid_v4.py   (ou dans un Blender ouvert via le MCP : exec(open(...).read()))
Sorties : art/blender/out/druid_v4_*.png (rendus de contrôle) et unity/HealerGame/Assets/Resources/Parts/DruidV4.fbx

Personnage qui regarde vers -Y ; côté droit du personnage = x < 0 (il y tient le bâton). Pièces « <Pivot>__<Pièce> ».
Méthode « corps d'abord » (D-068) : pose imposée, puis vêtements et ornements autour, puis contrôle automatique des dégagements.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(__file__))
import importlib

import lib
import lib2

importlib.reload(lib)
importlib.reload(lib2)
from lib import material, join, pivot
from lib2 import surface, ring, sweep, spline, blob, rbox, leaf

RNG = random.Random(11)
HERE = os.path.dirname(__file__)
OUT = os.path.join(HERE, "out")
os.makedirs(OUT, exist_ok=True)
UNITY_FBX = os.path.normpath(os.path.join(HERE, "..", "..", "unity", "HealerGame", "Assets", "Resources", "Parts", "DruidV4.fbx"))

# Nettoyage sans read_factory_settings : il couperait le serveur MCP quand le script tourne dans un Blender ouvert.
for _o in list(bpy.data.objects):
    bpy.data.objects.remove(_o, do_unlink=True)
for _coll in (bpy.data.meshes, bpy.data.materials, bpy.data.curves, bpy.data.lights, bpy.data.cameras):
    for _d in list(_coll):
        _coll.remove(_d)
lib._materials.clear()

# ---- Matériaux -------------------------------------------------------------------------------
ROBE = material("Robe", "2C3A29", roughness=0.9)
ROBE_LIGHT = material("RobeLight", "41573A", roughness=0.9)
CLOAK = material("Cloak", "1F2B1E", roughness=0.95)
LEATHER = material("Leather", "4A3524", roughness=0.75)
LEATHER_DARK = material("LeatherDark", "2E2117", roughness=0.8)
BARK = material("Bark", "5B4431", roughness=0.95)
BARK_DARK = material("BarkDark", "34271B", roughness=0.95)
MOSS = material("Moss", "5E8240", roughness=0.95)
LEAF_A = material("LeafA", "6F9B45", roughness=0.7)
LEAF_B = material("LeafB", "4A7A38", roughness=0.7)
LEAF_C = material("LeafC", "93B54F", roughness=0.7)
LEAF_D = material("LeafD", "C79B45", roughness=0.7)
BONE = material("Bone", "D9CEB3", roughness=0.55)
BONE_DARK = material("BoneDark", "A99C7E", roughness=0.6)
SKIN = material("Skin", "A88463", roughness=0.8)
VOID = material("Void", "06080A", roughness=1.0)
GOLD = material("Gold", "B8924A", metallic=0.7, roughness=0.35)
RUST = material("Rust", "A5563A", roughness=0.8)
GLOW = material("Glow", "7CFFB2", glow=4.0)
GLOW2 = material("GlowEye", "D2FFE6", glow=6.0)
GLOWW = material("GlowWarm", "FFD98A", glow=5.0)
LEAVES = (LEAF_A, LEAF_B, LEAF_C, LEAF_B)

objects = {}   # pivot -> objets à fusionner par matériau


def add(pivot_name, obj):
    objects.setdefault(pivot_name, []).append(obj)
    return obj


def lerp(a, b, t):
    return a + (b - a) * t


# =============================================================================================
# CORPS : robe (Root en bas, Torso en haut). Profil elliptique (rx, ry) par hauteur.
# =============================================================================================
PROFILE = [  # z, rx, ry, plis (profondeur relative)
    (0.02, 0.68, 0.62, 0.16),
    (0.30, 0.57, 0.52, 0.13),
    (0.70, 0.47, 0.42, 0.10),
    (1.05, 0.37, 0.32, 0.06),
    (1.16, 0.34, 0.29, 0.02),
    (1.35, 0.36, 0.29, 0.0),
    (1.55, 0.39, 0.31, 0.0),
    (1.74, 0.38, 0.28, 0.0),
    (1.86, 0.30, 0.22, 0.0),
    (1.95, 0.13, 0.12, 0.0),
]
NS = 28


def prof(z):
    for a, b in zip(PROFILE, PROFILE[1:]):
        if a[0] <= z <= b[0]:
            t = (z - a[0]) / (b[0] - a[0])
            return lerp(a[1], b[1], t), lerp(a[2], b[2], t), lerp(a[3], b[3], t)
    return PROFILE[-1][1:]


def body_ring(z, grow=0.0, hem_wave=0.0):
    rx, ry, fd = prof(z)
    r = ring((0, 0), rx + grow, ry + grow, z, n=NS, fold=9, fold_depth=fd, phase=0.3)
    if hem_wave:
        r = [(x, y, zz + hem_wave * (1 if i % 2 else 0)) for i, (x, y, zz) in enumerate(r)]
    return r


lower = [z for z, *_ in PROFILE if z <= 1.16]
upper = [z for z, *_ in PROFILE if z >= 1.16]
add("Root", surface("Root__Robe", ROBE, [body_ring(lower[0], hem_wave=0.06)] + [body_ring(z) for z in lower[1:]], closed_top=False, subsurf=1))
add("Torso", surface("Torso__Chest", ROBE_LIGHT, [body_ring(z) for z in upper], closed_top=True, subsurf=1))
# ourlet foncé
add("Root", surface("Root__Hem", CLOAK, [body_ring(0.0, grow=0.012, hem_wave=0.06), body_ring(0.16, grow=0.012)], closed_top=False, subsurf=1))
# pointes de bottes qui dépassent sous l'ourlet
for sgn in (-1, 1):
    pts, rad = spline([(sgn * 0.17, -0.42, 0.06), (sgn * 0.17, -0.58, 0.05), (sgn * 0.16, -0.72, 0.045)], [0.085, 0.07, 0.035], per=3)
    add("Root", sweep("Root__Boot", LEATHER_DARK, pts, rad, sides=10, aspect=0.75))

# tablier de cuir sur le devant (bande ouverte qui suit le corps)
def apron_ring(z):
    rx, ry, fd = prof(z)
    pts = []
    for i in range(9):
        a = math.radians(-90 - 34 + 68 * i / 8)
        pts.append(((rx + 0.03) * math.cos(a), (ry + 0.03) * math.sin(a), z))
    return pts


add("Root", surface("Root__Apron", LEATHER, [apron_ring(z) for z in (0.16, 0.4, 0.7, 1.0, 1.16)], closed_top=False, wrap=False, subsurf=1, solidify=0.012))
# ceinture, boucle, sacoche
add("Torso", surface("Torso__Belt", LEATHER_DARK, [body_ring(1.11, grow=0.02), body_ring(1.2, grow=0.02)], closed_top=False, subsurf=1))
add("Torso", rbox("Torso__Buckle", GOLD, (0.09, 0.03, 0.09), (0, -0.325, 1.155), bevel=0.012))
add("Torso", rbox("Torso__Pouch", LEATHER, (0.16, 0.10, 0.19), (0.30, -0.17, 1.0), rot=(0, 0, math.radians(-25)), bevel=0.02))
add("Torso", rbox("Torso__PouchFlap", LEATHER_DARK, (0.17, 0.11, 0.06), (0.30, -0.17, 1.075), rot=(0, 0, math.radians(-25)), bevel=0.015))
# sangle en bandoulière
strap = [(-0.30, -0.16, 1.85), (-0.12, -0.30, 1.6), (0.10, -0.32, 1.38), (0.31, -0.2, 1.12)]
pts, rad = spline(strap, [0.03] * 4, per=4)
add("Torso", sweep("Torso__Strap", LEATHER_DARK, pts, rad, sides=6, aspect=0.35))


# =============================================================================================
# TÊTE : capuche drapée, masque de crâne de cerf, grands bois (vignes et fleurs lumineuses)
# =============================================================================================
HOOD = [(1.90, 0.27, 0.25, 0.02), (2.02, 0.28, 0.26, 0.03), (2.16, 0.26, 0.27, 0.05), (2.32, 0.21, 0.24, 0.08),
        (2.48, 0.14, 0.17, 0.12), (2.60, 0.06, 0.08, 0.17), (2.66, 0.015, 0.02, 0.20)]
hood_rings = [ring((0, cy), rx, ry, z, n=20, fold=5, fold_depth=0.05 if z > 2.0 else 0.0, phase=0.6) for z, rx, ry, cy in HOOD]
add("Head", surface("Head__Hood", ROBE_LIGHT, hood_rings, closed_top=True, subsurf=1, solidify=0.02,
                    skip=lambda k, i: 1 <= k <= 4 and 12 <= i <= 18))
add("Head", blob("Head__Inside", VOID, 0.19, (0, 0.05, 2.27), scale=(0.95, 0.95, 1.05), segs=14, rings=8))
# masque de crâne (grand, bien lisible sous la capuche)
add("Head", blob("Head__Skull", BONE, 0.15, (0, -0.075, 2.27), scale=(0.95, 1.25, 1.05), segs=20, rings=14))
pts, rad = spline([(0, -0.18, 2.26), (0, -0.33, 2.2), (0, -0.48, 2.14)], [0.092, 0.07, 0.044], per=4)
add("Head", sweep("Head__Snout", BONE, pts, rad, sides=14, aspect=0.85))
pts, rad = spline([(0, -0.14, 2.15), (0, -0.30, 2.1), (0, -0.44, 2.1)], [0.05, 0.036, 0.024], per=4)
add("Head", sweep("Head__Jaw", BONE_DARK, pts, rad, sides=10, aspect=0.7))
add("Head", blob("Head__Nose", VOID, 0.036, (0, -0.52, 2.145), scale=(1, 0.7, 0.8), segs=8, rings=6))
for sgn in (-1, 1):
    add("Head", blob("Head__Socket", VOID, 0.052, (sgn * 0.078, -0.19, 2.31), scale=(1, 0.6, 1.1), segs=12, rings=8))
    add("Head", blob("Head__Glow_Eye", GLOW2, 0.026, (sgn * 0.078, -0.225, 2.31), segs=8, rings=6))
    pts, rad = spline([(sgn * 0.02, -0.2, 2.375), (sgn * 0.078, -0.21, 2.385), (sgn * 0.13, -0.15, 2.35)], [0.02, 0.024, 0.017], per=3)
    add("Head", sweep("Head__Brow", BONE, pts, rad, sides=6))
    pts, rad = spline([(sgn * 0.1, -0.18, 2.24), (sgn * 0.13, -0.11, 2.24), (sgn * 0.125, -0.04, 2.26)], [0.014, 0.014, 0.012], per=3)
    add("Head", sweep("Head__Cheek", BONE_DARK, pts, rad, sides=6))
add("Head", blob("Head__Glow_Rune", GLOW, 0.026, (0, -0.175, 2.43), scale=(0.6, 0.4, 1.5), segs=8, rings=6))


def antler(sgn):
    S = lambda p: (sgn * p[0], p[1], p[2])
    beam = [(0.06, -0.02, 2.36), (0.14, -0.03, 2.55), (0.24, -0.05, 2.78), (0.31, -0.03, 3.02), (0.36, 0.0, 3.28), (0.34, 0.05, 3.55), (0.28, 0.08, 3.78)]
    br = [0.052, 0.046, 0.04, 0.034, 0.028, 0.02, 0.008]
    pts, rad = spline([S(p) for p in beam], br, per=5)
    add("Head", sweep("Head__Antler", BONE, pts, rad, sides=9))
    add("Head", blob("Head__Coronet", BONE_DARK, 0.062, S((0.07, -0.02, 2.37)), scale=(1, 1, 0.7), segs=10, rings=6))
    tines = [
        ([(0.08, -0.03, 2.44), (0.1, -0.14, 2.52), (0.09, -0.24, 2.66), (0.07, -0.29, 2.8)], [0.03, 0.026, 0.02, 0.006]),
        ([(0.24, -0.05, 2.78), (0.34, -0.12, 2.88), (0.42, -0.14, 3.06), (0.44, -0.13, 3.2)], [0.03, 0.024, 0.016, 0.005]),
        ([(0.31, -0.03, 3.02), (0.44, 0.02, 3.08), (0.55, 0.03, 3.24), (0.6, 0.03, 3.44)], [0.028, 0.022, 0.015, 0.005]),
        ([(0.36, 0.0, 3.28), (0.5, 0.05, 3.4), (0.56, 0.07, 3.62), (0.54, 0.07, 3.8)], [0.026, 0.02, 0.014, 0.005]),
        ([(0.34, 0.05, 3.55), (0.22, 0.1, 3.62), (0.16, 0.13, 3.78), (0.16, 0.13, 3.95)], [0.02, 0.016, 0.012, 0.005]),
        ([(0.24, -0.05, 2.78), (0.3, 0.08, 2.86), (0.34, 0.2, 3.0), (0.33, 0.28, 3.14)], [0.028, 0.022, 0.016, 0.006]),
    ]
    tips = []
    for pts_, r_ in tines:
        pts, rad = spline([S(p) for p in pts_], r_, per=4)
        add("Head", sweep("Head__Tine", BONE, pts, rad, sides=7))
        tips.append(pts[-1])
    for tp in tips[1:5]:
        add("Head", blob("Head__Glow_Bloom", GLOW, 0.026, tp + Vector((0, 0, 0.02)), scale=(1, 1, 1.4), segs=8, rings=6))
        for k in range(3):
            a = 2 * math.pi * k / 3 + 0.5
            add("Head", leaf("Head__Leaf", LEAVES[k], 0.08, 0.028, tp + Vector((0, 0, -0.03)), (math.cos(a), math.sin(a), -0.7), rng=RNG, across=2, along=4))
    # vigne enroulée autour du merrain
    path, _ = spline([S(p) for p in beam], br, per=8)
    helix = []
    for j in range(8, 40):
        t = j / 40
        c = path[j]
        tng = (path[j + 1] - path[j - 1]).normalized()
        n1 = tng.cross(Vector((0, 1, 0))).normalized()
        n2 = tng.cross(n1).normalized()
        ang = 2 * math.pi * 2.5 * t
        helix.append(c + (n1 * math.cos(ang) + n2 * math.sin(ang)) * 0.058)
    add("Head", sweep("Head__Vine", MOSS, helix, [0.011] * len(helix), sides=5))
    for j in range(2, len(helix), 6):
        add("Head", leaf("Head__Leaf", LEAVES[j % 4], 0.10, 0.035, helix[j], (sgn * 0.5, RNG.uniform(-0.6, 0.6), -0.5), rng=RNG, across=2, along=4))


antler(-1)
antler(1)

# =============================================================================================
# BRAS ET MAINS : bras droit plié, poing SERRÉ sur le bâton (doigts modelés qui l'enserrent) ; bras gauche tendu, paume ouverte
# =============================================================================================
SR = (-0.42, 0.0, 1.80)
ER = (-0.53, -0.13, 1.36)
WR = (-0.665, -0.30, 1.20)
SL = (0.42, 0.0, 1.80)
EL = (0.64, -0.10, 1.42)
WL = (0.88, -0.24, 1.44)
STAFF_X, STAFF_Y = -0.71, -0.34
GRIP_Z = 1.10


def sign(v):
    return 1 if v >= 0 else -1


def sleeve(pivot_name, S, E, W):
    S, E, W = Vector(S), Vector(E), Vector(W)
    out = (W - E).normalized()
    path = [S, (S + E) / 2 + Vector((sign(S.x) * 0.02, -0.02, 0)), E, (E + W) / 2, W, W + out * 0.07]
    radii = [0.135, 0.13, 0.12, 0.115, 0.13, 0.155]
    pts, rad = spline(path, radii, per=4)
    add(pivot_name, sweep(pivot_name + "__Sleeve", ROBE_LIGHT, pts, rad, sides=16, cap="flat"))
    add(pivot_name, sweep(pivot_name + "__Cuff", MOSS, [W - out * 0.02, W + out * 0.04, W + out * 0.07], [0.14, 0.165, 0.165], sides=16, cap="flat"))
    add(pivot_name, blob(pivot_name + "__Elbow", ROBE_LIGHT, 0.125, E, segs=12, rings=8))


sleeve("ArmR", SR, ER, WR)
sleeve("ArmL", SL, EL, WL)

# -- poing droit : paume + quatre doigts qui font le tour du bâton + pouce dessus
gx, gy, gz = STAFF_X, STAFF_Y, GRIP_Z
add("ArmR", rbox("ArmR__Palm", SKIN, (0.085, 0.14, 0.18), (gx + 0.09, gy + 0.02, gz + 0.02), bevel=0.025))
for i in range(4):
    z = gz + (i - 1.5) * 0.048
    knuckle = [(gx + 0.105, gy + 0.055, z)]
    arc = [(gx + 0.088 * math.cos(math.radians(a)), gy + 0.088 * math.sin(math.radians(a)), z - 0.004 * n) for n, a in enumerate((12, -25, -65, -105, -135))]
    pts, rad = spline(knuckle + arc, [0.027, 0.026, 0.024, 0.022, 0.019, 0.015], per=2)
    add("ArmR", sweep("ArmR__Finger", SKIN, pts, rad, sides=8))
thumb = [(gx + 0.1, gy + 0.03, gz + 0.09)] + [(gx + 0.082 * math.cos(math.radians(a)), gy + 0.082 * math.sin(math.radians(a)), gz + 0.108 - 0.004 * n) for n, a in enumerate((35, 0, -35, -70))]
pts, rad = spline(thumb, [0.033, 0.03, 0.026, 0.022, 0.016], per=2)
add("ArmR", sweep("ArmR__Thumb", SKIN, pts, rad, sides=8))

# -- main gauche : paume ouverte vers le haut, doigts légèrement recourbés, l'orbe flotte au-dessus
d = Vector((WL[0] - EL[0], WL[1] - EL[1], 0)).normalized()
perp = Vector((-d.y, d.x, 0))
palm = Vector(WL) + d * 0.12 + Vector((0, 0, -0.025))
add("ArmL", rbox("ArmL__Palm", SKIN, (0.19, 0.19, 0.06), palm, rot=(0, 0, math.atan2(d.y, d.x)), bevel=0.022))
for i in range(4):
    s = palm + d * 0.09 + perp * ((i - 1.5) * 0.048)
    path = [s, s + d * 0.075 + Vector((0, 0, 0.014)), s + d * 0.14 + Vector((0, 0, 0.038)), s + d * 0.18 + Vector((0, 0, 0.07))]
    pts, rad = spline(path, [0.026, 0.023, 0.02, 0.015], per=3)
    add("ArmL", sweep("ArmL__Finger", SKIN, pts, rad, sides=8))
s = palm - perp * 0.09
path = [s, s - perp * 0.06 + d * 0.06 + Vector((0, 0, 0.012)), s - perp * 0.1 + d * 0.14 + Vector((0, 0, 0.038))]
pts, rad = spline(path, [0.031, 0.026, 0.017], per=3)
add("ArmL", sweep("ArmL__Thumb", SKIN, pts, rad, sides=8))
orb = palm + d * 0.03 + Vector((0, 0, 0.27))
add("ArmL", blob("ArmL__Glow_Orb", GLOW, 0.09, orb, segs=16, rings=10))
for i in range(6):
    a = i * math.pi / 3 + 0.4
    add("ArmL", lib.crystal("ArmL__Glow_Shard", GLOW, 0.02, 0.09, (orb.x + 0.17 * math.cos(a), orb.y + 0.17 * math.sin(a), orb.z + 0.04 * (i % 2)), sides=4, rot=(0, 0, a)))
for i in range(4):
    a = i * 1.7 + 0.9
    add("ArmL", leaf("ArmL__Leaf", LEAVES[i], 0.09, 0.03, palm + Vector((0.1 * math.cos(a), 0.1 * math.sin(a), 0.05)), (math.cos(a), math.sin(a), -0.5), rng=RNG, across=2, along=4))

# =============================================================================================
# BÂTON : tenu au poing, planté au sol à l'écart de la robe ; cage de branches qui enserre un cristal, breloques d'os, vignes
# =============================================================================================
bx, by = STAFF_X, STAFF_Y
zs = [i * 0.3 for i in range(9)]
shaft = [(bx + 0.02 * math.sin(z * 3), by + 0.015 * math.cos(z * 2.5), z) for z in zs]
pts, rad = spline(shaft, [0.06 - 0.014 * (z / 2.4) + 0.004 * math.sin(z * 9) for z in zs], per=5)
add("Weapon", sweep("Weapon__Shaft", BARK, pts, rad, sides=12))
top = Vector(shaft[-1])
cc = Vector((bx, by, 2.72))
for k in range(4):
    al = math.pi / 4 + k * math.pi / 2
    c, s_ = math.cos(al), math.sin(al)
    br = [(top.x, top.y, 2.30), (bx + 0.10 * c, by + 0.10 * s_, 2.42), (bx + 0.20 * c, by + 0.20 * s_, 2.62), (bx + 0.19 * c, by + 0.19 * s_, 2.86), (bx + 0.07 * c, by + 0.07 * s_, 3.02)]
    pts, rad = spline(br, [0.04, 0.032, 0.026, 0.02, 0.007], per=4)
    add("Weapon", sweep("Weapon__Branch", BARK_DARK, pts, rad, sides=8))
add("Weapon", lib.crystal("Weapon__Glow_Crystal", GLOW, 0.09, 0.46, tuple(cc), sides=6, waist=0.45))
for k in range(3):
    al = k * 2.1 + 0.4
    top_p = Vector((bx + 0.2 * math.cos(al), by + 0.2 * math.sin(al), 2.55))
    pts, rad = spline([top_p, top_p + Vector((0.0, 0.0, -0.12)), top_p + Vector((0.01, 0, -0.24))], [0.004] * 3, per=2)
    add("Weapon", sweep("Weapon__String", LEATHER_DARK, pts, rad, sides=4))
    add("Weapon", blob("Weapon__Charm", BONE, 0.028, top_p + Vector((0.01, 0, -0.27)), scale=(0.7, 0.7, 1.5), segs=8, rings=6))
for (z0, z1, turns) in ((0.28, 1.0, 3), (1.28, 2.2, 3)):
    hel = []
    for j in range(40):
        t = j / 39
        z = lerp(z0, z1, t)
        ang = 2 * math.pi * turns * t
        hel.append((bx + 0.02 * math.sin(z * 3) + 0.07 * math.cos(ang), by + 0.015 * math.cos(z * 2.5) + 0.07 * math.sin(ang), z))
    add("Weapon", sweep("Weapon__Vine", MOSS, hel, [0.013] * len(hel), sides=5))
    for j in range(3, 40, 7):
        p = Vector(hel[j])
        a = 2 * math.pi * turns * j / 39
        add("Weapon", leaf("Weapon__Leaf", LEAVES[j % 4], 0.13, 0.045, p, (math.cos(a), math.sin(a), -0.5), rng=RNG, across=2, along=5))

# =============================================================================================
# MANTE de feuilles, épaulières de mousse, champignons ; CAPE plissée ; petits objets de ceinture
# =============================================================================================
for k in range(3):
    n = (10, 13, 16)[k]
    for i in range(n):
        a = 2 * math.pi * (i + 0.5 * k) / n
        if abs((math.degrees(a) - 203 + 180) % 360 - 180) < 40:   # la mante s'écarte du bâton (contrôle de dégagement)
            continue
        R = 0.30 + 0.075 * k
        pos = (R * math.cos(a), R * math.sin(a) * 0.8, 1.98 - 0.12 * k)
        add("Torso", leaf("Torso__Leaf", LEAF_D if (i + k) % 7 == 0 else LEAVES[(i + k) % 4], 0.24 + 0.05 * k, 0.075, pos, (math.cos(a) * 0.8, math.sin(a) * 0.7, -0.6), rng=RNG, across=3, along=5))
for sgn in (-1, 1):
    add("Torso", blob("Torso__Moss", MOSS, 0.17, (sgn * 0.39, 0.0, 1.93), scale=(1.25, 1.0, 0.55), segs=14, rings=8))
for (dx, dy, h, r) in ((-0.42, -0.06, 0.15, 0.07), (-0.5, 0.05, 0.1, 0.05), (-0.35, 0.05, 0.08, 0.04)):
    pts, rad = spline([(dx, dy, 2.0), (dx + 0.005, dy, 2.0 + h / 2), (dx, dy, 2.0 + h)], [0.02, 0.016, 0.014], per=2)
    add("Torso", sweep("Torso__Stem", BONE, pts, rad, sides=7))
    add("Torso", blob("Torso__Cap", RUST, r, (dx, dy, 2.0 + h), scale=(1, 1, 0.55), segs=10, rings=6))

cape_rows = []
for z in (1.93, 1.75, 1.55, 1.3, 1.05, 0.8, 0.55, 0.3, 0.1):
    rx, ry, _ = prof(z)
    tt = 1.0 - z / 1.95
    span = 62 + 30 * tt
    row = []
    for c in range(19):
        a = math.radians(90 - span + 2 * span * c / 18)
        f = 1.0 + (0.02 + 0.12 * tt) * math.cos(11 * a + 0.4)
        row.append(((rx + 0.05 + 0.11 * tt) * f * math.cos(a), (ry + 0.05 + 0.11 * tt) * f * math.sin(a) + 0.02, z))
    cape_rows.append(row)
cape_rows = cape_rows[::-1]
add("Cape", surface("Cape__Cloak", CLOAK, cape_rows, closed_top=False, wrap=False, subsurf=1, solidify=0.02))
for c in range(3, 16, 2):
    add("Cape", leaf("Cape__Leaf", LEAVES[c % 4], 0.26, 0.07, Vector(cape_rows[0][c]), (0, 0.15, -1), rng=RNG, across=3, along=5))

add("Torso", sweep("Torso__Glow_Vial", GLOW, [(0.13, -0.335, 0.93), (0.13, -0.335, 1.07)], [0.032, 0.032], sides=10))
add("Torso", blob("Torso__Cork", BARK, 0.024, (0.13, -0.335, 1.09), segs=8, rings=6))
for i in range(3):
    a = math.radians(-20 + 20 * i)
    add("Torso", leaf("Torso__Herb", LEAVES[i], 0.24, 0.05, (-0.18 - 0.03 * i, -0.335, 1.1), (math.sin(a), -0.15, -1), rng=RNG, across=2, along=5))

# =============================================================================================
# Fusion par pivot et matériau, articulations, contrôle des dégagements, rendus, export
# =============================================================================================
for pivot_name, objs in objects.items():
    by_mat = {}
    for o in objs:
        by_mat.setdefault(o.data.materials[0].name, []).append(o)
    for mat_name, group in by_mat.items():
        is_glow = "Glow" in mat_name
        name = f"{pivot_name}__{'Glow_' if is_glow else ''}{mat_name.replace('GlowEye', 'Eye').replace('GlowWarm', 'Warm').replace('Glow', '') or 'Light'}"
        if len(group) > 1:
            join(group, name)
        else:
            group[0].name = name
            group[0].data.name = name

pivot("Torso", (0, 0, 1.5))
pivot("Head", (0, 0, 2.05))
pivot("ArmR", SR)
pivot("ArmL", SL)
pivot("Cape", (0, 0.28, 1.9))
pivot("Weapon", (gx, gy, gz))

from mathutils.bvhtree import BVHTree
bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()
bodies = []
for o in bpy.context.scene.objects:
    if o.type == "MESH" and o.name.split("__")[0] in ("Root", "Torso", "Head", "Cape", "ArmL"):
        me = o.evaluated_get(dg).to_mesh()
        bodies.append((o.name, BVHTree.FromPolygons([v.co[:] for v in me.vertices], [p.vertices[:] for p in me.polygons])))
worst = (9.0, "")
for p in [(bx, by, z * 0.1) for z in range(0, 31)]:
    for name, tree in bodies:
        hit = tree.find_nearest(Vector(p))
        if hit[0] is not None and hit[3] < worst[0]:
            worst = (hit[3], name)
print("DEGAGEMENT BATON-CORPS min", round(worst[0], 3), "avec", worst[1])
if worst[0] < 0.07:
    raise RuntimeError(f"le bâton est trop près de {worst[1]} ({worst[0]:.3f} m) : ne pas exporter")

print("DRUIDE V4 TRIANGLES", lib.triangle_count())
lib.setup_render(560, 900)
lib.render_views(os.path.join(OUT, "druid_v4"), center=(0, 0, 1.75), distance=10.0, lens=80)
lib.render_views(os.path.join(OUT, "druid_v4_head"), views=(("front", 0), ("three_quarter", 40)), center=(0, -0.1, 2.3), distance=3.2, lens=80)
lib.render_views(os.path.join(OUT, "druid_v4_hands"), views=(("front", 0), ("side", 90)), center=(-0.6, -0.3, 1.2), distance=3.0, lens=75)

os.makedirs(os.path.dirname(UNITY_FBX), exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=UNITY_FBX, use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="OFF", path_mode="AUTO")
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "druid_v4.blend"))
print("EXPORT", UNITY_FBX)
