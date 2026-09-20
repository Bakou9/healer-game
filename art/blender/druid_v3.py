"""Druide de la Soigneuse, version 3 (D-068, « corps d'abord ») : pose imposée, mains à taille de figurine, bâton tenu au poing, grands bois, contrôle automatique des dégagements.
Modèle de base, sans variantes d'équipement. Les versions 1 (druid.py) et 2 (druid_mcp.py) sont gardées comme replis.

Lancer :  blender --background --python art/blender/druid_v3.py   (ou depuis un Blender ouvert via le MCP : exec(open(...).read()))
Sorties : art/blender/out/druid_v3_*.png (rendus de contrôle) et unity/HealerGame/Assets/Resources/Parts/DruidV3.fbx

Personnage qui regarde vers -Y. Pièces nommées « <Pivot>__<Pièce> » ; « Glow » = lumineux (voir lib.py).
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(__file__))
import lib
from lib import material, cone, sphere, box, crystal, leaf, tube, join, pivot

random.seed(7)
HERE = os.path.dirname(__file__)
OUT = os.path.join(HERE, "out")
os.makedirs(OUT, exist_ok=True)
UNITY_FBX = os.path.normpath(os.path.join(HERE, "..", "..", "unity", "HealerGame", "Assets", "Resources", "Parts", "DruidV3.fbx"))

# Nettoyage sans read_factory_settings : il couperait le serveur MCP quand le script tourne dans un Blender ouvert.
for _o in list(bpy.data.objects):
    bpy.data.objects.remove(_o, do_unlink=True)
for _coll in (bpy.data.meshes, bpy.data.materials, bpy.data.curves, bpy.data.lights, bpy.data.cameras):
    for _d in list(_coll):
        _coll.remove(_d)
lib._materials.clear()

# ---- Matériaux -------------------------------------------------------------------------------
ROBE = material("Robe", "34402C", roughness=0.9)
ROBE_DARK = material("RobeDark", "1E271B", roughness=0.9)
MOSS = material("Moss", "56753F", roughness=0.95)
LEAF_A = material("LeafA", "6F9B45", roughness=0.7)
LEAF_B = material("LeafB", "4C7A38", roughness=0.7)
LEAF_C = material("LeafC", "8CB04E", roughness=0.7)
BARK = material("Bark", "4A3826", roughness=0.95)
BARK_DARK = material("BarkDark", "2B2016", roughness=0.95)
LEATHER = material("Leather", "3A2A1E", roughness=0.8)
BONE = material("Bone", "CDC3AA", roughness=0.6)
SKIN = material("Skin", "B59672", roughness=0.8)
VOID = material("Void", "07090A", roughness=1.0)
GOLD = material("Gold", "B8924A", metallic=0.7, roughness=0.4)
RUST = material("Rust", "A5563A", roughness=0.8)
GLOW = material("Glow", "7CFFB2", glow=4.0)
GLOW2 = material("GlowEye", "B8FFD8", glow=6.0)

LEAF_MATS = (LEAF_A, LEAF_B, LEAF_C)


def lathe(name, mat, profile, sides=12, zigzag=0.0, jitter=0.0, scale=(1, 1), center=(0, 0), cap_top=True, cap_bottom=False, folds=0, fold_depth=0.0):
    """Solide de révolution : profile = [(rayon, z), ...] du bas vers le haut. `folds` plis verticaux de profondeur relative `fold_depth`."""
    bm = bmesh.new()
    rings = []
    for k, (r, z) in enumerate(profile):
        ring = []
        for i in range(sides):
            a = 2 * math.pi * i / sides
            rr = r * (1 + random.uniform(-jitter, jitter))
            if folds:
                rr *= 1 + fold_depth * math.cos(folds * a) * (1.0 - 0.5 * k / max(1, len(profile) - 1))
            zz = z + (zigzag if (k == 0 and i % 2 == 0) else 0)
            ring.append(bm.verts.new((center[0] + rr * math.cos(a) * scale[0], center[1] + rr * math.sin(a) * scale[1], zz)))
        rings.append(ring)
    for k in range(len(rings) - 1):
        for i in range(sides):
            j = (i + 1) % sides
            bm.faces.new((rings[k][i], rings[k][j], rings[k + 1][j], rings[k + 1][i]))
    if cap_top:
        bm.faces.new(rings[-1][::-1])
    if cap_bottom:
        bm.faces.new(rings[0])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    o = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(o)
    bpy.context.view_layer.objects.active = o
    return lib.finish(o, mat, name)


def oriented_leaf(name, mat, length, width, pos, direction):
    rot = Vector(direction).normalized().to_track_quat("-Z", "Y").to_euler()
    return leaf(name, mat, length, width, pos, rot)


objects = {}   # pivot -> liste d'objets à fusionner par matériau


def add(pivot_name, obj):
    objects.setdefault(pivot_name, []).append(obj)
    return obj


# =============================================================================================
# ROOT : robe, racines, runes de robe
# =============================================================================================
robe = lathe("Root__Robe", ROBE, [(0.62, 0.05), (0.52, 0.45), (0.42, 0.95), (0.34, 1.35), (0.28, 1.55)], sides=16, zigzag=0.10, jitter=0.03, folds=9, fold_depth=0.07)
add("Root", robe)
hem = lathe("Root__Hem", ROBE_DARK, [(0.64, 0.03), (0.63, 0.13)], sides=16, zigzag=0.06, jitter=0.03, cap_top=False, folds=9, fold_depth=0.07)
add("Root", hem)
# jupe de feuilles à la taille
for k in range(2):
    n = 7 + k
    for i in range(n):
        a = 2 * math.pi * (i + 0.5 * k) / n
        r = 0.40 + 0.06 * k
        z = 1.20 - 0.10 * k
        pos = (r * math.cos(a), r * math.sin(a), z)
        d = (math.cos(a) * 0.55, math.sin(a) * 0.55, -0.83)
        add("Root", oriented_leaf("Root__Leaf", LEAF_MATS[(i + k) % 3], 0.34 - 0.03 * k, 0.085, pos, d))
# racines aux pieds
for i in range(6):
    a = 2 * math.pi * i / 6 + 0.3
    r0 = 0.54
    if abs((math.degrees(a) - 205 + 180) % 360 - 180) < 32:   # pas de racine sous le bâton (contrôle de dégagement)
        continue
    pts = [(r0 * math.cos(a), r0 * math.sin(a), 0.14), ((r0 + 0.16) * math.cos(a + 0.12), (r0 + 0.16) * math.sin(a + 0.12), 0.07),
           ((r0 + 0.34) * math.cos(a + 0.05), (r0 + 0.34) * math.sin(a + 0.05), 0.03), ((r0 + 0.5) * math.cos(a - 0.1), (r0 + 0.5) * math.sin(a - 0.1), 0.02)]
    add("Root", tube("Root__Bark", BARK, pts, [0.05, 0.04, 0.03, 0.012], sides=5))
# runes lumineuses sur le devant de la robe
for k, z in enumerate((0.35, 0.62, 0.90)):
    r = 0.58 - 0.13 * (z - 0.45) / 0.5 if z > 0.45 else 0.58
    add("Root", crystal("Root__Glow_Rune", GLOW, 0.05, 0.17, (0, -(r * 0.93 + 0.02), z), sides=4, rot=(math.radians(90), 0, 0)))

# tablier brodé sur le devant de la robe
tab = lathe("Root__Tabard", BARK, [(0.60, 0.16), (0.50, 0.7), (0.40, 1.1)], sides=12, cap_top=False)
bm = bmesh.new()
bm.from_mesh(tab.data)
front = [f for f in bm.faces if not (f.calc_center_median().y < -0.05 and abs(f.calc_center_median().x) < 0.16)]
bmesh.ops.delete(bm, geom=front, context="FACES")
for v in bm.verts:
    v.co.y *= 1.04
    v.co.x *= 1.0
bm.to_mesh(tab.data)
bm.free()
add("Root", tab)
add("Root", box("Root__Trim", GOLD, (0.035, 0.03, 0.9), (-0.17, -0.505, 0.62), rot=(math.radians(12), 0, 0)))
add("Root", box("Root__Trim", GOLD, (0.035, 0.03, 0.9), (0.17, -0.505, 0.62), rot=(math.radians(12), 0, 0)))

# =============================================================================================
# TORSO : corselet, ceinture, sacoche, herbes, mante de feuilles, épaules moussues
# =============================================================================================
chest = lathe("Torso__Chest", LEATHER, [(0.31, 1.12), (0.35, 1.32), (0.39, 1.62), (0.33, 1.86), (0.16, 1.96)], sides=10, scale=(1.0, 0.72), jitter=0.02)
add("Torso", chest)
for sgn in (-1, 1):
    add("Torso", box("Torso__Strap", BARK_DARK, (0.07, 0.03, 0.85), (sgn * 0.06, -0.26, 1.5), rot=(0, math.radians(sgn * 25), 0)))
add("Torso", lathe("Torso__Belt", GOLD, [(0.36, 1.08), (0.37, 1.16)], sides=12, scale=(1.0, 0.75), cap_top=False))
add("Torso", box("Torso__Buckle", GOLD, (0.11, 0.05, 0.11), (0, -0.28, 1.12)))
add("Torso", box("Torso__Pouch", LEATHER, (0.17, 0.11, 0.2), (0.27, -0.2, 0.98), rot=(0, 0, math.radians(-10))))
for i, (dx, m) in enumerate(((-0.24, LEAF_A), (-0.30, LEAF_B), (-0.18, LEAF_C))):
    add("Torso", cone("Torso__Herb", m, 0.045, 0.0, 0.3, (dx, -0.2 - 0.02 * i, 0.95), verts=5, rot=(math.radians(180), 0, math.radians(8 * (i - 1)))))
# mante : trois rangs de feuilles autour des épaules
for k in range(2):
    n = 8 + 2 * k
    for i in range(n):
        a = 2 * math.pi * (i + 0.5 * k) / n
        R = 0.40 + 0.085 * k
        if abs((math.degrees(a) - 203 + 180) % 360 - 180) < 38:   # la mante s'écarte du bâton (contrôle de dégagement)
            continue
        z = 1.99 - 0.10 * k
        pos = (R * math.cos(a), R * math.sin(a) * 0.85, z)
        d = (math.cos(a) * (0.75 + 0.1 * k), math.sin(a) * (0.75 + 0.1 * k), -0.65)
        add("Torso", oriented_leaf("Torso__Leaf", LEAF_MATS[(i + k) % 3], 0.24 + 0.05 * k, 0.075, pos, d))
for sgn in (-1, 1):
    add("Torso", sphere("Torso__Moss", MOSS, 0.2, (sgn * 0.43, 0.0, 1.96), scale=(1.15, 1.0, 0.5), segs=7, rings=4))
# lucioles autour du personnage
for i in range(7):
    a = 2 * math.pi * i / 7 + 0.5
    add("Torso", crystal("Torso__Glow_Mote", GLOW, 0.022, 0.07, (0.85 * math.cos(a), 0.85 * math.sin(a) - 0.1, 0.9 + 0.35 * i * 0.6 + 0.2 * math.sin(i)), sides=4, rot=(0.3 * i, 0.2, a)))
# champignons sur l'épaule droite (côté -X)
for (dx, dy, h, r) in ((-0.42, -0.06, 0.16, 0.07), (-0.5, 0.05, 0.11, 0.05), (-0.36, 0.06, 0.09, 0.04)):
    add("Torso", cone("Torso__Stem", BONE, 0.022, 0.018, h, (dx, dy, 2.05 + h / 2), verts=5))
    add("Torso", sphere("Torso__Cap", RUST, r, (dx, dy, 2.05 + h), scale=(1, 1, 0.55), segs=6, rings=3))

# =============================================================================================
# HEAD : capuche, visage d'ombre, yeux, bois de cerf, couronne de feuilles
# =============================================================================================
hood_bm = bmesh.new()
bmesh.ops.create_uvsphere(hood_bm, u_segments=10, v_segments=8, radius=1.0)
for v in hood_bm.verts:
    x, y, z = v.co
    v.co = Vector((x * 0.31, y * 0.33 + (0.10 if z > 0.2 else 0.0) * (z - 0.2), 2.30 + z * 0.36))
    if z > 0.55:
        v.co.y += 0.16 * (z - 0.55)          # pointe de capuche penchée vers l'arrière
        v.co.z += 0.07 * (z - 0.55)
# ouverture du visage : on retire les faces de devant
front_faces = [f for f in hood_bm.faces if f.calc_center_median().y < -0.10 and abs(f.calc_center_median().x) < 0.17 and 2.16 < f.calc_center_median().z < 2.5]
bmesh.ops.delete(hood_bm, geom=front_faces, context="FACES")
hood_mesh = bpy.data.meshes.new("Head__Hood")
hood_bm.to_mesh(hood_mesh)
hood_bm.free()
hood = bpy.data.objects.new("Head__Hood", hood_mesh)
bpy.context.collection.objects.link(hood)
bpy.context.view_layer.objects.active = hood
add("Head", lib.finish(hood, ROBE, "Head__Hood"))
add("Head", sphere("Head__Face", VOID, 0.21, (0, 0.02, 2.27), scale=(0.95, 0.95, 1.1), segs=8, rings=6))
# masque : crâne de cerf (crâne, museau, arcades, orbites sombres, truffe)
add("Head", sphere("Head__Skull", BONE, 0.185, (0, -0.13, 2.33), scale=(0.9, 1.15, 1.0), segs=8, rings=6))
add("Head", cone("Head__Snout", BONE, 0.105, 0.05, 0.30, (0, -0.30, 2.235), verts=6, rot=(math.radians(96), 0, 0)))
add("Head", sphere("Head__Nose", VOID, 0.05, (0, -0.455, 2.215), scale=(1, 0.7, 0.8), segs=5, rings=3))
for sgn in (-1, 1):
    add("Head", box("Head__Brow", BONE, (0.11, 0.06, 0.035), (sgn * 0.085, -0.27, 2.41), rot=(0, 0, math.radians(sgn * -18))))
    add("Head", sphere("Head__Socket", VOID, 0.062, (sgn * 0.085, -0.27, 2.36), scale=(1.0, 0.6, 1.1), segs=6, rings=4))
    add("Head", box("Head__Cheek", BONE, (0.025, 0.14, 0.035), (sgn * 0.09, -0.2, 2.24), rot=(0, 0, math.radians(sgn * 8))))
rim = [(0.185 * math.cos(t), -0.235 + 0.03 * math.cos(t) ** 2, 2.34 + 0.2 * math.sin(t)) for t in [i * 2 * math.pi / 14 for i in range(14)]]
add("Head", tube("Head__Rim", MOSS, rim, [0.024] * 14, sides=5, closed=True))
add("Head", sphere("Head__Glow_EyeL", GLOW2, 0.032, (0.085, -0.295, 2.36), segs=5, rings=3))
add("Head", sphere("Head__Glow_EyeR", GLOW2, 0.032, (-0.085, -0.295, 2.36), segs=5, rings=3))
add("Head", crystal("Head__Glow_Forehead", GLOW, 0.04, 0.14, (0, -0.15, 2.5), sides=4, rot=(math.radians(-20), 0, 0)))
# bois de cerf (grands, à andouillers : broc frontal, cors et empaumure de feuilles)
for sgn in (-1, 1):
    beam = [(0.12, 0.02, 2.50), (0.20, 0.0, 2.72), (0.33, -0.02, 2.98), (0.40, 0.0, 3.25), (0.36, 0.06, 3.52), (0.30, 0.10, 3.74)]
    tines = [
        [(0.14, -0.02, 2.56), (0.16, -0.15, 2.67), (0.13, -0.25, 2.82)],
        [(0.20, 0.0, 2.72), (0.36, -0.08, 2.86), (0.47, -0.10, 3.08)],
        [(0.33, -0.02, 2.98), (0.52, 0.0, 3.06), (0.62, 0.02, 3.30)],
        [(0.40, 0.0, 3.25), (0.55, 0.04, 3.40), (0.60, 0.06, 3.64)],
        [(0.36, 0.06, 3.52), (0.24, 0.12, 3.62), (0.20, 0.14, 3.86)],
    ]
    add("Head", tube("Head__Antler", BONE, [(sgn * x, y, z) for x, y, z in beam], [0.05, 0.042, 0.034, 0.026, 0.018, 0.008], sides=5))
    for t in tines:
        add("Head", tube("Head__Antler", BONE, [(sgn * x, y, z) for x, y, z in t], [0.03, 0.02, 0.008], sides=4))
    for i in range(1):
        a = math.radians(50 + 40 * i)
        add("Head", oriented_leaf("Head__Leaf", LEAF_MATS[i], 0.13, 0.04, (sgn * 0.18, 0.02, 2.52 + 0.03 * i), (sgn * math.sin(a), 0.2, -0.3 * math.cos(a))))
    add("Head", oriented_leaf("Head__Leaf", LEAF_MATS[sgn % 3], 0.16, 0.05, (sgn * 0.40, 0.0, 3.25), (sgn * 0.3, 0.1, -1)))


# =============================================================================================
# ARMS (v3, « corps d'abord ») : la pose est imposée. Bras droit plié vers l'avant, poing SERRÉ sur le bâton, à distance
# de la robe ; bras gauche tendu, paume ouverte sous l'orbe. Mains à taille de figurine (poing ≈ 0,16 m, tête ≈ 0,4 m).
# =============================================================================================
def limb(name, mat, a, b, r1, r2, verts=8):
    """Cône entre deux points : rayon r1 en a, r2 en b."""
    A, B = Vector(a), Vector(b)
    d = B - A
    rot = d.to_track_quat("Z", "Y").to_euler()
    return cone(name, mat, r1, r2, d.length, (A + B) / 2, verts=verts, rot=rot)


# -- bras droit (x < 0) : porte le bâton
SR = (-0.46, 0.0, 1.80)
ER = (-0.57, -0.12, 1.37)
WR = (-0.66, -0.28, 1.24)
STAFF_X, STAFF_Y = -0.71, -0.33
GRIP_Z = 1.10
add("ArmR", limb("ArmR__Sleeve", ROBE, SR, ER, 0.14, 0.125))
add("ArmR", sphere("ArmR__Elbow", ROBE, 0.13, ER, segs=6, rings=4))
add("ArmR", limb("ArmR__Sleeve", ROBE, ER, WR, 0.125, 0.175))
add("ArmR", limb("ArmR__Cuff", MOSS, (WR[0], WR[1], WR[2] - 0.01), (WR[0] - 0.02, WR[1] - 0.03, WR[2] - 0.07), 0.18, 0.17))
# poing : bloc + quatre doigts refermés devant le bâton + pouce dessus
grip = (STAFF_X, STAFF_Y, GRIP_Z)
add("ArmR", box("ArmR__Hand", SKIN, (0.15, 0.14, 0.17), grip, bevel=0.012))
for i in range(4):
    add("ArmR", box("ArmR__Finger", SKIN, (0.15, 0.06, 0.038), (grip[0], grip[1] - 0.10, grip[2] + (i - 1.5) * 0.043), bevel=0.008))
add("ArmR", box("ArmR__Thumb", SKIN, (0.045, 0.09, 0.055), (grip[0] + 0.05, grip[1] - 0.06, grip[2] + 0.11), rot=(math.radians(-20), 0, 0), bevel=0.008))

# -- bras gauche (x > 0) : avant-bras tendu sur le côté (bien visible de face), paume ouverte, l'orbe de lumière flotte au-dessus
SL = (0.46, 0.0, 1.80)
EL = (0.68, -0.10, 1.42)
WL = (0.90, -0.24, 1.44)
add("ArmL", limb("ArmL__Sleeve", ROBE, SL, EL, 0.14, 0.125))
add("ArmL", sphere("ArmL__Elbow", ROBE, 0.13, EL, segs=6, rings=4))
add("ArmL", limb("ArmL__Sleeve", ROBE, EL, WL, 0.125, 0.165))
add("ArmL", limb("ArmL__Cuff", MOSS, (WL[0] - 0.01, WL[1] + 0.01, WL[2]), (WL[0] + 0.05, WL[1] - 0.03, WL[2] + 0.005), 0.17, 0.165))
_d = Vector((WL[0] - EL[0], WL[1] - EL[1], 0)).normalized()          # direction de l'avant-bras à plat
_th = math.atan2(_d.x, -_d.y)                                        # angle pour que « -Y local » (les doigts) suive _d


def hand_pt(lx, ly, lz):
    """Point de la main : (lx, ly) dans le repère de la paume (ly < 0 = vers les doigts), tourné pour suivre l'avant-bras."""
    c, s = math.cos(_th), math.sin(_th)
    return (palm[0] + c * lx - s * ly, palm[1] + s * lx + c * ly, palm[2] + lz)


palm = (WL[0] + _d.x * 0.15, WL[1] + _d.y * 0.15, WL[2] - 0.03)
add("ArmL", box("ArmL__Hand", SKIN, (0.19, 0.22, 0.06), palm, rot=(0, 0, _th), bevel=0.012))
for i in range(4):
    add("ArmL", box("ArmL__Finger", SKIN, (0.04, 0.13, 0.04), hand_pt(-0.066 + i * 0.044, -0.17, 0.03 + 0.008 * (i % 2)), rot=(0, 0, _th), bevel=0.006))
add("ArmL", box("ArmL__Thumb", SKIN, (0.05, 0.10, 0.04), hand_pt(-0.125, -0.07, 0.035), rot=(0, 0, _th), bevel=0.006))
orb = (palm[0] + _d.x * 0.08, palm[1] + _d.y * 0.08, palm[2] + 0.27)
add("ArmL", sphere("ArmL__Glow_Orb", GLOW, 0.09, orb, segs=8, rings=5))
for i in range(4):
    a = i * math.pi / 2 + 0.4
    add("ArmL", crystal("ArmL__Glow_Shard", GLOW, 0.02, 0.085, (orb[0] + 0.16 * math.cos(a), orb[1] + 0.16 * math.sin(a), orb[2] + 0.03 * (i % 2)), sides=4, rot=(0, 0, a)))
for i in range(3):
    a = i * 2.1 + 0.9
    add("ArmL", oriented_leaf("ArmL__Leaf", LEAF_MATS[i % 3], 0.08, 0.028, (palm[0] + 0.1 * math.cos(a), palm[1] + 0.1 * math.sin(a), palm[2] + 0.05), (math.cos(a), math.sin(a), -0.5)))

# -- bâton (pièce « Weapon », rattachée au bras droit) : tenu au poing, planté au sol à l'écart de la robe
bx, by = STAFF_X, STAFF_Y
shaft = [(bx + 0.012 * math.sin(z * 3), by + 0.010 * math.cos(z * 3), z) for z in [i * 0.27 for i in range(0, 10)]]
add("Weapon", tube("Weapon__Shaft", BARK, shaft, [0.05 - 0.0025 * i for i in range(10)], sides=6))
cc = (bx, by, 2.66)
arc = [(cc[0] + 0.25 * math.cos(math.radians(t)), cc[1], cc[2] + 0.25 * math.sin(math.radians(t))) for t in range(-70, 251, 40)]
add("Weapon", tube("Weapon__Crescent", BARK, arc, [0.012, 0.03, 0.038, 0.04, 0.038, 0.03, 0.022, 0.012, 0.008], sides=5))
add("Weapon", tube("Weapon__Crescent", BARK, [(bx, by, 2.4), (bx, by, 2.5), (cc[0], cc[1], 2.44)], [0.045, 0.035, 0.02], sides=5))
add("Weapon", crystal("Weapon__Glow_Crystal", GLOW, 0.08, 0.38, (bx, by, 2.62), sides=6, waist=0.45))
vine = [(bx + 0.075 * math.cos(t), by + 0.075 * math.sin(t), 0.35 + t * 0.10) for t in [i * 0.9 for i in range(0, 10)]]
add("Weapon", tube("Weapon__Vine", MOSS, vine, [0.014] * len(vine), sides=4))
for i in range(3):
    t = 1.0 + i * 2.9
    add("Weapon", oriented_leaf("Weapon__Leaf", LEAF_MATS[i % 3], 0.12, 0.042, (bx + 0.075 * math.cos(t), by + 0.075 * math.sin(t), 0.35 + t * 0.10), (math.cos(t), math.sin(t), -0.4)))


# =============================================================================================
# CAPE : cape de mousse à franges de feuilles
# =============================================================================================
cape_bm = bmesh.new()
rows, cols = 6, 6
grid = []
for r in range(rows):
    t = r / (rows - 1)
    z = 1.98 - t * 1.75
    half = 0.24 + 0.34 * t
    row = []
    for c in range(cols):
        u = (c / (cols - 1)) * 2 - 1
        x = u * half
        y = 0.27 + 0.09 * t + 0.08 * (u * u) * (0.4 + t)
        row.append(cape_bm.verts.new((x, y, z)))
    grid.append(row)
for r in range(rows - 1):
    for c in range(cols - 1):
        cape_bm.faces.new((grid[r][c], grid[r][c + 1], grid[r + 1][c + 1], grid[r + 1][c]))
cape_mesh = bpy.data.meshes.new("Cape__Cloak")
cape_bm.to_mesh(cape_mesh)
cape_bm.free()
cape = bpy.data.objects.new("Cape__Cloak", cape_mesh)
bpy.context.collection.objects.link(cape)
bpy.context.view_layer.objects.active = cape
sol = cape.modifiers.new("Solid", "SOLIDIFY")
sol.thickness = 0.03
bpy.ops.object.modifier_apply(modifier="Solid")
add("Cape", lib.finish(cape, ROBE_DARK, "Cape__Cloak"))
for i in range(4):
    u = (i / 3) * 2 - 1
    x = u * 0.56
    add("Cape", oriented_leaf("Cape__Leaf", LEAF_MATS[i % 3], 0.3, 0.09, (x, 0.40 + 0.06 * u * u, 0.26), (u * 0.25, 0.1, -1)))
for i in range(4):
    u = (i / 3) * 2 - 1
    add("Cape", oriented_leaf("Cape__Leaf", LEAF_MATS[(i + 1) % 3], 0.22, 0.07, (u * 0.34, 0.31, 1.95), (u * 0.5, 0.35, -0.7)))

# =============================================================================================
# Fusion par pivot et matériau, articulations
# =============================================================================================
for pivot_name, objs in objects.items():
    by_mat = {}
    for o in objs:
        by_mat.setdefault(o.data.materials[0].name, []).append(o)
    for mat_name, group in by_mat.items():
        first = group[0]
        # nom de la pièce : « Pivot__Glow » si lumineux, sinon « Pivot__<Matériau> »
        is_glow = "Glow" in mat_name
        name = f"{pivot_name}__{'Glow_' if is_glow else ''}{mat_name.replace('Glow', '').replace('GlowEye', 'Eye') or 'Light'}"
        join(group, name)

pivot("Torso", (0, 0, 1.5))
pivot("Head", (0, 0, 2.05))
pivot("ArmR", SR)
pivot("ArmL", SL)
pivot("Cape", (0, 0.28, 1.9))
pivot("Weapon", grip)


# ---- Contrôle automatique (D-068) : le bâton ne doit traverser aucune autre pièce -------------
from mathutils.bvhtree import BVHTree
bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()
bodies = []
for o in bpy.context.scene.objects:
    if o.type == "MESH" and o.name.split("__")[0] in ("Root", "Torso", "Head", "Cape", "ArmL"):
        me = o.evaluated_get(dg).to_mesh()
        bodies.append((o.name, BVHTree.FromPolygons([v.co[:] for v in me.vertices], [p.vertices[:] for p in me.polygons])))
worst = (9.0, "")
samples = [(bx, by, z * 0.1) for z in range(0, 25)] + [(c[0] + 0.25 * math.cos(t), by, cc[2] + 0.25 * math.sin(t)) for t in [i * 0.4 for i in range(16)] for c in [cc]]
for p in samples:
    for name, tree in bodies:
        hit = tree.find_nearest(Vector(p))
        if hit[0] is not None and hit[3] < worst[0]:
            worst = (hit[3], name)
print("DEGAGEMENT BATON-CORPS min", round(worst[0], 3), "avec", worst[1])
if worst[0] < 0.07:
    raise RuntimeError(f"le bâton est trop près de {worst[1]} ({worst[0]:.3f} m) : ne pas exporter")

tris = lib.triangle_count()
print("DRUIDE V3 TRIANGLES", tris)

lib.setup_render(560, 860)
lib.render_views(os.path.join(OUT, "druid_v3"), center=(0, 0, 1.6), distance=9.0, lens=75)
lib.render_views(os.path.join(OUT, "druid_v3_hands"), views=(("front", 0), ("side", 90)), center=(-0.6, -0.3, 1.2), distance=3.0, lens=75)

os.makedirs(os.path.dirname(UNITY_FBX), exist_ok=True)
lib.export_fbx(UNITY_FBX)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "druid_v3.blend"))
print("EXPORT", UNITY_FBX)
