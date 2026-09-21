"""Archere (dps1), high-definition model : hooded crimson ranger at full draw.

Run :  blender --background --python art-3d/blender/archer.py
Out  :  art-3d/blender/out/archer.blend and art-3d/blender/out/archer_*.png (control renders)

Same conventions as druid_v4.py : metres, Z up, the character faces -Y, feet at z = 0, pieces named "<Pivot>__<Piece>"
(pivots Root, Torso, Head, ArmR, ArmL, Cape, Weapon), a material whose name contains "Glow" is emissive.
Body-first method (D-068) : the pose is fixed first (skeleton points below), then the body, then clothes and props
around it, then an automatic clearance check between the weapon and the body.

Handedness : the bow is in the character's LEFT hand (x > 0). The party sprites are shot from the +X side
(sprite_lib.setup_camera side=+1), so the left arm is the one nearest the camera : the bow, the wrapped grip and
the fingers stay unobstructed, and the drawing arm reads as a silhouette behind the head. This is also the standard
right-handed archer (left hand on the bow, right hand on the string).
"""
import math
import os
import random
import sys

import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(__file__))
import importlib

import lib
import lib2

importlib.reload(lib)
importlib.reload(lib2)
from lib import join, material, pivot
from lib2 import blob, rbox, ring, spline, surface, sweep

RNG = random.Random(7)
HERE = os.path.dirname(__file__)
OUT = os.path.join(HERE, "out")
os.makedirs(OUT, exist_ok=True)

# Cleanup without read_factory_settings : it would kill the MCP server if the script ran inside an open Blender.
for _o in list(bpy.data.objects):
    bpy.data.objects.remove(_o, do_unlink=True)
for _coll in (bpy.data.meshes, bpy.data.materials, bpy.data.curves, bpy.data.lights, bpy.data.cameras):
    for _d in list(_coll):
        _coll.remove(_d)
lib._materials.clear()

# ---- Materials -------------------------------------------------------------------------------
# Values are deliberately lighter than the druid's : the first sprite pass came out almost invisible on the game's
# dark background (the 3D renders lied, they have a warm lamp two metres away that the sprite rig does not).
CRIMSON = material("Crimson", "A44557", roughness=0.85)          # identity colour of the Archere (dps1), kept as is
CRIMSON_DARK = material("CrimsonDark", "7E3240", roughness=0.9)
CLOTH = material("Cloth", "473C4A", roughness=0.95)              # hood and shirt : dark plum-grey (a bluer value stole the show from the crimson)
CLOTH_DARK = material("ClothDark", "2C2534", roughness=0.95)
LEATHER = material("Leather", "6F5439", roughness=0.75)
LEATHER_MID = material("LeatherMid", "9B7448", roughness=0.7)
LEATHER_DARK = material("LeatherDark", "4A382B", roughness=0.8)
WOOD = material("Wood", "805C38", roughness=0.7)                 # bow limbs
WOOD_DARK = material("WoodDark", "4E3722", roughness=0.8)
BONE = material("Bone", "D9CEB3", roughness=0.55)
BONE_DARK = material("BoneDark", "A99C7E", roughness=0.6)
STEEL = material("Steel", "8A8F99", metallic=0.85, roughness=0.3)
STEEL_DARK = material("SteelDark", "4A5058", metallic=0.7, roughness=0.45)
GOLD = material("Gold", "B8924A", metallic=0.7, roughness=0.35)
SKIN = material("Skin", "C79A72", roughness=0.8)
VOID = material("Void", "06080A", roughness=1.0)
STRING = material("String", "C8BE9E", roughness=0.6)
BRACED = material("Braced", "C8BE9E", roughness=0.6)   # same look, own material so the merge keeps it a separate piece
ARROW_SHAFT = material("ArrowShaft", "8A6A42", roughness=0.7)    # own materials : the merge keeps the arrow separate
ARROW_HEAD = material("ArrowHead", "9AA0AA", metallic=0.85, roughness=0.3)
ARROW_FLETCH = material("ArrowFletch", "B04A54", roughness=0.9)
GLOW = material("Glow", "FFB347", glow=4.0)                      # warm ember : the Archere's glow colour (docs/ART_3D.md)
GLOW_EYE = material("GlowEye", "FFC27A", glow=4.5)   # strong enough to read at 128 px, weak enough not to blow out to white

objects = {}   # pivot -> pieces to merge by material


def add(pivot_name, obj):
    objects.setdefault(pivot_name, []).append(obj)
    return obj


def lerp(a, b, t):
    return a + (b - a) * t


def frame(x_axis, y_axis, z_axis):
    """Euler angles of the orientation whose local axes are the three given (orthonormal) vectors."""
    m = Matrix(((x_axis.x, y_axis.x, z_axis.x), (x_axis.y, y_axis.y, z_axis.y), (x_axis.z, y_axis.z, z_axis.z)))
    return m.to_euler()


# =============================================================================================
# POSE : every joint of the skeleton, fixed before any geometry (body-first method).
# The torso twists (yaw about Z) so that the bow shoulder is forward : archery is a side-on action,
# and the twist is what makes the draw readable while the character still faces -Y.
# =============================================================================================
SL = Vector((0.208, -0.156, 1.79))     # left shoulder  (bow arm, camera side)
EL = Vector((0.190, -0.470, 1.780))    # left elbow  (slightly out : a dead straight arm looks stiff)
WL = Vector((0.060, -0.745, 1.755))    # left wrist
GRIP = Vector((0.050, -0.865, 1.745))  # centre of the bow grip, in the left fist

SR = Vector((-0.208, 0.156, 1.79))     # right shoulder (drawing arm)
ER = Vector((-0.345, -0.060, 2.010))   # right elbow, out and up : the classic high-elbow draw
# Anchor point : in front of the jaw rather than against the cheek. The hood is big and stylised, so a cheek anchor
# would bury the string and the fist in it (the clearance check caught exactly that) ; in front of the throat the
# draw stays readable and the hand keeps its own silhouette at 128 px.
# The bow hand is deliberately lower than the anchor : at the same height the near arm hid the drawing hand
# completely in the sprite view, and the draw stopped reading.
ANCHOR = Vector((-0.020, -0.330, 1.900))   # where the string is pulled to (nock offset added once the bow frame is known)

HIP_L, HIP_R = Vector((0.125, -0.02, 1.02)), Vector((-0.135, 0.02, 1.02))
# knees clearly forward of the hip-ankle line : a soft stance, and Rigify needs a bend direction to build leg IK
KNEE_L, KNEE_R = Vector((0.160, -0.145, 0.60)), Vector((-0.180, 0.025, 0.60))
ANK_L, ANK_R = Vector((0.170, -0.16, 0.135)), Vector((-0.190, 0.13, 0.135))
TOE_L, TOE_R = Vector((0.175, -0.42, 0.055)), Vector((-0.195, -0.13, 0.055))
HEAD_C = Vector((0.0, -0.02, 2.10))

# bow frame : "along" = the arrow, "up" = the limbs, "side" = the normal of the bow plane
BOW_A = (GRIP - ANCHOR).normalized()
BOW_U = (Vector((0, 0, 1)) - BOW_A * BOW_A.z).normalized()
BOW_U = (Matrix.Rotation(math.radians(7.0), 4, BOW_A) @ BOW_U).normalized()   # a slight cant, as archers hold it
BOW_V = BOW_A.cross(BOW_U).normalized()
# The arrow rests on the SIDE of the riser, never through it : without this offset the riser hides the whole arrow
# in the sprite view (first render). BOW_V points toward -X, so -0.032 puts the arrow on the camera side.
NOCK = ANCHOR + BOW_V * -0.032
WR = NOCK + Vector((-0.070, 0.115, 0.030))   # right wrist, just behind the hooked fingers


def bowp(u, d=0.0, s=0.0):
    """Point of the bow frame : u along the limbs, d forward (away from the archer), s across the bow plane."""
    return GRIP + BOW_U * u + BOW_A * d + BOW_V * s


# =============================================================================================
# LEGS AND HIPS (Root) : lithe ranger, staggered stance, tall boots
# =============================================================================================
def leg(hip, knee, ankle, toe, sgn):
    # thigh and calf : clearly tapered (a constant radius reads as a sausage, first render)
    pts, rad = spline([hip, (hip + knee) / 2 + Vector((0, -0.02, 0)), knee, (knee + ankle) / 2 + Vector((0, 0.025, 0)), ankle],
                      [0.108, 0.094, 0.068, 0.062, 0.048], per=4)
    add("Root", sweep("Root__Legging", CLOTH, pts, rad, sides=10, cap="flat"))
    # boot : low shaft (mid-calf), foot, heel
    top = ankle + Vector((0, 0.01, 0.37))
    pts, rad = spline([top, ankle + Vector((0, 0, 0.14)), ankle, ankle + Vector((0, -0.10, -0.045)), toe],
                      [0.072, 0.066, 0.072, 0.070, 0.048], per=4)
    add("Root", sweep("Root__Boot", LEATHER_DARK, pts, rad, sides=10, aspect=0.85))
    add("Root", blob("Root__Heel", LEATHER_DARK, 0.058, ankle + Vector((0, 0.045, -0.065)), scale=(0.9, 1.0, 0.5), segs=10, rings=6))
    # folded-over boot cuff and a flat knee guard
    add("Root", surface("Root__Cuff", LEATHER, [
        [(top.x + 0.080 * math.cos(a), top.y + 0.072 * math.sin(a), top.z - 0.02) for a in [2 * math.pi * i / 14 for i in range(14)]],
        [(top.x + 0.098 * math.cos(a), top.y + 0.090 * math.sin(a), top.z + 0.055) for a in [2 * math.pi * i / 14 for i in range(14)]],
    ], closed_top=False, subsurf=1, solidify=0.010))
    add("Root", rbox("Root__Knee", LEATHER_MID, (0.095, 0.075, 0.105), knee + Vector((sgn * 0.015, -0.055, 0.015)), bevel=0.022))
    # single strap around the boot shaft
    z = ankle.z + 0.20
    add("Root", surface("Root__Strap", LEATHER_DARK, [
        [(ankle.x + 0.078 * math.cos(a), ankle.y + 0.072 * math.sin(a), z) for a in [2 * math.pi * i / 12 for i in range(12)]],
        [(ankle.x + 0.078 * math.cos(a), ankle.y + 0.072 * math.sin(a), z + 0.032) for a in [2 * math.pi * i / 12 for i in range(12)]],
    ], closed_top=False, subsurf=1, solidify=0.010))


leg(HIP_L, KNEE_L, ANK_L, TOE_L, 1)
leg(HIP_R, KNEE_R, ANK_R, TOE_R, -1)

# =============================================================================================
# TORSO : elliptic profile (rx, ry) per height, with a yaw that grows from the hips to the shoulders (twist)
# =============================================================================================
PROFILE = [  # z, rx (half width), ry (half depth), yaw (deg)
    (0.90, 0.175, 0.145, -4),
    (1.00, 0.205, 0.160, -7),
    (1.10, 0.200, 0.152, -13),
    (1.26, 0.178, 0.135, -21),
    (1.42, 0.208, 0.150, -29),
    (1.58, 0.240, 0.168, -34),
    (1.74, 0.258, 0.175, -37),
    (1.83, 0.205, 0.150, -37),
    (1.91, 0.105, 0.098, -36),
]
NS = 26


def prof(z):
    for a, b in zip(PROFILE, PROFILE[1:]):
        if a[0] <= z <= b[0]:
            t = (z - a[0]) / (b[0] - a[0])
            return lerp(a[1], b[1], t), lerp(a[2], b[2], t), lerp(a[3], b[3], t)
    return (PROFILE[0] if z < PROFILE[0][0] else PROFILE[-1])[1:]


def body_ring(z, grow=0.0, n=NS, fold=0, fold_depth=0.0):
    rx, ry, yaw = prof(z)
    pts = ring((0, 0), rx + grow, ry + grow, 0.0, n=n, fold=fold, fold_depth=fold_depth, phase=0.5)
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    return [(x * c - y * s, x * s + y * c, z) for (x, y, _) in pts]


BODY_Z = [z for z, *_ in PROFILE]
add("Root", surface("Root__Hips", CLOTH, [body_ring(z) for z in (0.90, 1.00, 1.10)], closed_top=False, closed_bottom=True, subsurf=1))
add("Torso", surface("Torso__Shirt", CLOTH, [body_ring(z) for z in BODY_Z if z >= 1.10], closed_top=True, subsurf=1))

# leather cuirass : a second skin from the belt to the chest, open in a V at the front (skip)
CUIRASS_Z = (1.13, 1.26, 1.42, 1.58, 1.70, 1.78)
front = int(NS * 0.75)   # index of the -Y direction : the V opening is centred there


def vee(k, i):
    d = min((i - front) % NS, (front - i) % NS)
    return d < (0.4 + 0.85 * k)          # narrow V : a wide one turned the crimson tunic into a big flat bib


add("Torso", surface("Torso__Cuirass", LEATHER, [body_ring(z, grow=0.022) for z in CUIRASS_Z],
                     closed_top=False, subsurf=1, solidify=0.016, skip=vee))
# crimson cloth showing through the opening of the cuirass
add("Torso", surface("Torso__Tunic", CRIMSON, [body_ring(z, grow=0.008) for z in CUIRASS_Z], closed_top=False, subsurf=1,
                     skip=lambda k, i: not vee(k, i)))
# collar and shoulder caps
add("Torso", surface("Torso__Collar", LEATHER_DARK, [body_ring(1.80, grow=0.03), body_ring(1.87, grow=0.015)], closed_top=False, subsurf=1, solidify=0.014))
for name, S, sgn in (("L", SL, 1), ("R", SR, -1)):
    d = (Vector((S.x, S.y, 0))).normalized()
    # crimson shoulder caps : the identity colour needs to reach the silhouette from every angle (head trim,
    # shoulders, scarf, fletchings), not only the chest, which the bow arm hides in the sprite view
    add("Torso", blob("Torso__Pauldron", CRIMSON_DARK, 0.120, S + d * 0.02 + Vector((0, 0, 0.020)), scale=(1.0, 1.05, 0.55), segs=14, rings=8))
    add("Torso", blob("Torso__PauldronTrim", LEATHER_DARK, 0.124, S + d * 0.025 + Vector((0, 0, -0.030)), scale=(1.0, 1.05, 0.26), segs=14, rings=6))

# belt, buckle, pouches, hanging knife
add("Torso", surface("Torso__Belt", LEATHER_DARK, [body_ring(1.10, grow=0.028), body_ring(1.20, grow=0.028)], closed_top=False, subsurf=1))
buckle = Vector(body_ring(1.15, grow=0.03)[front])
add("Torso", rbox("Torso__Buckle", GOLD, (0.075, 0.03, 0.075), buckle, rot=(0, 0, math.radians(-37)), bevel=0.012))
pouch = Vector(body_ring(1.08, grow=0.02)[(front + 5) % NS])
add("Torso", rbox("Torso__Pouch", LEATHER, (0.14, 0.09, 0.15), pouch + Vector((0, 0, -0.06)), rot=(0, 0, math.radians(20)), bevel=0.022))
add("Torso", rbox("Torso__PouchFlap", LEATHER_DARK, (0.15, 0.10, 0.05), pouch + Vector((0, 0, 0.015)), rot=(0, 0, math.radians(20)), bevel=0.016))
# trophy : three bone fangs on a cord at the bow-side hip. Small, but it is the only bright value low on the
# body and it breaks the dark leather mass on the camera side.
troph = Vector(body_ring(1.10, grow=0.035)[(front - 4) % NS])
for k in range(3):
    o = Vector((0.035 * (k - 1), -0.012 * abs(k - 1), 0))
    add("Torso", sweep("Torso__Cord", LEATHER_DARK, [troph + o, troph + o + Vector((0, 0, -0.055))], [0.005, 0.005], sides=4))
    add("Torso", sweep("Torso__Fang", BONE, *spline([troph + o + Vector((0, 0, -0.055)), troph + o + Vector((0.004, 0.004, -0.10)), troph + o + Vector((0.006, 0.006, -0.145 - 0.02 * (1 - abs(k - 1))))],
                                                    [0.020, 0.015, 0.004], per=3), sides=7))
knife = Vector(body_ring(1.12, grow=0.03)[(front + 11) % NS])
add("Torso", sweep("Torso__Sheath", LEATHER_DARK, *spline([knife + Vector((0, 0, -0.02)), knife + Vector((0.02, 0.03, -0.16)), knife + Vector((0.03, 0.05, -0.28))],
                                                          [0.035, 0.030, 0.018], per=3), sides=8, aspect=0.55))
add("Torso", sweep("Torso__Hilt", STEEL_DARK, *spline([knife + Vector((-0.01, -0.01, 0.02)), knife + Vector((-0.02, -0.02, 0.10))], [0.018, 0.016], per=3), sides=7))

# short tassets : leather panels hanging from the belt, cut short in front so the legs stay readable
for i in range(NS):
    d = min((i - front) % NS, (front - i) % NS)
    if d < 3 or i % 2:
        continue
    a0 = Vector(body_ring(1.12, grow=0.030)[i])
    a1 = Vector(body_ring(1.12, grow=0.030)[(i + 1) % NS])
    drop = 0.20 + 0.10 * min(1.0, d / 6.0)
    out = Vector((a0.x + a1.x, a0.y + a1.y, 0)).normalized() * 0.03
    rows = [[a0, a1],
            [a0 + out * 0.6 + Vector((0, 0, -drop * 0.55)), a1 + out * 0.6 + Vector((0, 0, -drop * 0.55))],
            [a0 + out + Vector((0, 0, -drop)), a1 + out + Vector((0, 0, -drop))]]
    add("Root", surface("Root__Tasset", LEATHER, rows, closed_top=False, wrap=False, subsurf=1, solidify=0.012))

# =============================================================================================
# ARMS : sleeve + bracer, then the hands (fist on the grip, hook on the string)
# =============================================================================================
def arm(pivot_name, S, E, W, r0=0.078, r1=0.056):
    pts, rad = spline([S, (S + E) / 2, E, (E + W) / 2, W], [r0, r0 * 0.92, r0 * 0.82, r1 * 1.05, r1], per=4)
    add(pivot_name, sweep(pivot_name + "__Sleeve", CLOTH, pts, rad, sides=12, cap="flat"))
    add(pivot_name, blob(pivot_name + "__Elbow", CLOTH, r0 * 0.85, E, segs=12, rings=8))
    # bracer over the forearm : the archer's signature piece, and it reads at 128 px
    f = (W - E).normalized()
    b0, b1 = E + f * 0.06, W - f * 0.01
    pts, rad = spline([b0, lerp(b0, b1, 0.5), b1], [0.074, 0.070, 0.062], per=4)
    add(pivot_name, sweep(pivot_name + "__Bracer", LEATHER_MID, pts, rad, sides=12, cap="flat"))
    for t in (0.25, 0.75):
        c = b0 + (b1 - b0) * t
        add(pivot_name, sweep(pivot_name + "__BracerStrap", LEATHER_DARK, [c - f * 0.010, c + f * 0.010], [0.077, 0.077], sides=12, cap="flat"))


arm("ArmL", SL, EL, WL)
arm("ArmR", SR, ER, WR)


def wrap_hand(pivot_name, centre, axis, palm_dir, grip_r, flip=False, fingers=4, spacing=0.046, span=(22, -150),
              finger_r=0.026, palm=(0.055, 0.15, 0.17), thumb=True, mat=SKIN):
    """Hand closed around a cylinder (bow grip, string) : palm on the `palm_dir` side, fingers wrapped around `axis`.
    `flip` mirrors the wrap (left hand / right hand). Figurine scale : a fist is about 0.15 m wide."""
    axis = Vector(axis).normalized()
    e1 = (Vector(palm_dir) - axis * Vector(palm_dir).dot(axis)).normalized()
    e2 = axis.cross(e1).normalized() * (-1 if flip else 1)
    centre = Vector(centre)
    add(pivot_name, rbox(pivot_name + "__Palm", mat, palm, centre + e1 * (grip_r + palm[0] * 0.5),
                         rot=frame(e1, e2, axis), bevel=0.022))
    for i in range(fingers):
        u = (i - (fingers - 1) / 2) * spacing
        r = grip_r + finger_r * 0.85
        path = [centre + axis * u + e1 * (r + 0.028) + e2 * 0.045]
        for n, deg in enumerate((span[0], (2 * span[0] + span[1]) / 3, (span[0] + 2 * span[1]) / 3, span[1])):
            a = math.radians(deg)
            path.append(centre + axis * (u - 0.004 * n) + (e1 * math.cos(a) + e2 * math.sin(a)) * r)
        pts, rad = spline(path, [finger_r, finger_r, finger_r * 0.92, finger_r * 0.82, finger_r * 0.62], per=3)
        add(pivot_name, sweep(pivot_name + "__Finger", mat, pts, rad, sides=8))
    if thumb:
        u = (fingers - 1) / 2 * spacing + 0.052
        r = grip_r + finger_r * 0.9
        path = [centre + axis * u + e1 * (r + 0.02) + e2 * 0.04]
        for n, deg in enumerate((35, 5, -32)):
            a = math.radians(deg)
            path.append(centre + axis * (u - 0.02 * n) + (e1 * math.cos(a) + e2 * math.sin(a)) * r)
        pts, rad = spline(path, [0.032, 0.030, 0.026, 0.018], per=3)
        add(pivot_name, sweep(pivot_name + "__Thumb", mat, pts, rad, sides=8))


# left fist on the bow grip : the grip is a cylinder along the limbs (BOW_U), the palm pushes from the archer's side
wrap_hand("ArmL", GRIP, BOW_U, -BOW_A, 0.040, flip=False, fingers=4, spacing=0.045, span=(25, -160))
# right hand hooked on the string : three fingers around the string, thumb folded ; the palm faces the face
wrap_hand("ArmR", NOCK + BOW_U * -0.005, BOW_U, -BOW_A * 0.4 + Vector((-0.9, 0, 0)), 0.020, flip=True,
          fingers=3, spacing=0.043, span=(35, -145), finger_r=0.024, palm=(0.05, 0.13, 0.15), thumb=True)

# =============================================================================================
# HEAD : hood with a torn peak, shadowed face, lower-face mask, two ember eyes, braid
# =============================================================================================
HOOD = [  # z, rx, ry, centre y : narrower than a monk's hood, tip low and falling backward (the first version read as an egg)
    (1.76, 0.212, 0.200, 0.015),
    (1.90, 0.230, 0.226, 0.000),
    (2.04, 0.234, 0.240, -0.012),
    (2.18, 0.214, 0.230, -0.008),
    (2.30, 0.166, 0.188, 0.030),
    (2.39, 0.092, 0.115, 0.080),
    (2.45, 0.028, 0.042, 0.125),
]
NH = 22
hood_front = int(NH * 0.75)
hood_rings = [ring((0, cy), rx, ry, z, n=NH, fold=5, fold_depth=0.035 if z > 2.0 else 0.0, phase=0.4) for z, rx, ry, cy in HOOD]


def hood_skip(k, i):
    d = min((i - hood_front) % NH, (hood_front - i) % NH)
    return 1 <= k <= 2 and d <= 3        # opening from z = 1.90 to 2.18 : eyes and mask show, the chin stays covered


add("Head", surface("Head__Hood", CLOTH, hood_rings, closed_top=True, subsurf=1, solidify=0.018, skip=hood_skip))
# brim : a wide short visor over the eyes, not a point (a long pointed brim plus the hood tip made the head
# read as a bird's beak). It keeps the face in shadow without hiding the eyes.
add("Head", lib2.leaf("Head__Brim", CLOTH, 0.17, 0.135, HEAD_C + Vector((0, -0.150, 0.145)), (0, -0.75, -1.0),
                      curl=0.75, droop=0.10, thick=0.016, across=5, along=4))
add("Head", blob("Head__Inside", VOID, 0.180, HEAD_C + Vector((0, 0.01, 0.0)), scale=(1.0, 1.0, 1.05), segs=14, rings=8))
# lower-face mask : cloth over nose and mouth, held by a strap
add("Head", blob("Head__Mask", CLOTH_DARK, 0.145, HEAD_C + Vector((0, -0.045, -0.080)), scale=(1.02, 1.12, 0.80), segs=14, rings=8))
add("Head", sweep("Head__MaskStrap", CLOTH_DARK, *spline([HEAD_C + Vector((-0.16, -0.05, -0.04)), HEAD_C + Vector((0, 0.10, -0.02)), HEAD_C + Vector((0.16, -0.05, -0.04))],
                                                         [0.022, 0.024, 0.022], per=4), sides=8, aspect=0.5))
add("Head", sweep("Head__Nasal", BONE_DARK, *spline([HEAD_C + Vector((0, -0.150, 0.085)), HEAD_C + Vector((0, -0.185, 0.010)), HEAD_C + Vector((0, -0.175, -0.045))],
                                                    [0.022, 0.019, 0.014], per=3), sides=6, aspect=0.5))
for sgn in (-1, 1):
    # at 128 px the head is ~18 px : an eye slit under 5 cm simply disappears
    add("Head", blob("Head__Glow_Eye", GLOW_EYE, 0.042, HEAD_C + Vector((sgn * 0.076, -0.180, 0.020)), scale=(1.45, 0.45, 0.62), segs=10, rings=6))
# hood trim : a thin crimson cord that follows the real border of the opening (computed from the removed faces,
# so it sits on the edge instead of floating in front of the face like the hand-placed U of the first version)
def hood_pt(k, i, out=1.035):
    x, y, z = hood_rings[k][i % NH]
    cy = HOOD[k][3]
    return (x * out, (y - cy) * out + cy, z)


# Only the bottom and the sides : closing the loop on top drew a rectangular "spectacle frame" around the face.
border = [hood_pt(3, hood_front - 3)] + [hood_pt(2, hood_front - 3)]
border += [hood_pt(1, i) for i in range(hood_front - 3, hood_front + 5)]          # bottom edge, left to right
border += [hood_pt(2, hood_front + 4), hood_pt(3, hood_front + 4)]                # right side, going up
pts, rad = spline(border, [0.012] * len(border), per=3)
add("Head", sweep("Head__Trim", CRIMSON, pts, rad, sides=7, aspect=0.9))
# braid falling behind the shoulder, outside the cape so that it shows in the side view
braid = [HEAD_C + Vector((0.05, 0.17, -0.04)), HEAD_C + Vector((0.08, 0.30, -0.28)), HEAD_C + Vector((0.09, 0.33, -0.50)), HEAD_C + Vector((0.07, 0.29, -0.68))]
pts, rad = spline(braid, [0.052, 0.048, 0.040, 0.020], per=5)
add("Head", sweep("Head__Braid", CRIMSON_DARK, pts, rad, sides=8, twist=0.35))
for t in (0.35, 0.8):
    c = braid[0] + (braid[-1] - braid[0]) * t
    add("Head", blob("Head__Binding", LEATHER_DARK, 0.042 - 0.012 * t, c, scale=(1, 1, 0.45), segs=10, rings=6))

# =============================================================================================
# QUIVER (Torso) : on the back, opening over the drawing shoulder, arrows with crimson fletching
# =============================================================================================
# Placed on the bow-arm side (x > 0) on purpose : that is the side the sprite camera looks from, so the fletchings
# read against the background instead of hiding behind the body and the cape (first render : quiver invisible).
Q0 = Vector((-0.04, 0.30, 1.16))    # bottom of the quiver, on the lower back
Q1 = Vector((0.21, 0.23, 1.84))     # rim, above the bow shoulder
qdir = (Q1 - Q0).normalized()
pts, rad = spline([Q0 - qdir * 0.03, lerp(Q0, Q1, 0.5), Q1], [0.070, 0.082, 0.090], per=5)
add("Torso", sweep("Torso__Quiver", LEATHER, pts, rad, sides=12, cap="flat"))
add("Torso", sweep("Torso__QuiverRim", LEATHER_DARK, [Q1 - qdir * 0.05, Q1 + qdir * 0.015], [0.098, 0.098], sides=12, cap="flat"))
add("Torso", sweep("Torso__QuiverBand", LEATHER_DARK, [lerp(Q0, Q1, 0.32) - qdir * 0.02, lerp(Q0, Q1, 0.32) + qdir * 0.02], [0.086, 0.086], sides=12, cap="flat"))
qe1 = qdir.cross(Vector((0, 0, 1))).normalized()
qe2 = qdir.cross(qe1).normalized()
for i in range(4):
    a = 2 * math.pi * i / 4 + 0.6
    off = (qe1 * math.cos(a) + qe2 * math.sin(a)) * 0.048
    base = Q1 + off - qdir * 0.10
    tip = Q1 + off * 1.3 + qdir * (0.40 + 0.06 * (i % 2))
    pts, rad = spline([base, lerp(base, tip, 0.5), tip], [0.014, 0.013, 0.012], per=3)
    add("Torso", sweep("Torso__ArrowShaft", ARROW_SHAFT, pts, rad, sides=6))
    for k in range(3):
        b = 2 * math.pi * k / 3 + a
        d = (qe1 * math.cos(b) + qe2 * math.sin(b))
        add("Torso", lib2.leaf("Torso__ArrowFletch", ARROW_FLETCH, 0.125, 0.038, tip - qdir * 0.105, d - qdir * 1.4, curl=0.15, droop=0.05, thick=0.005, across=2, along=3))
# straps holding the quiver across the chest
strap = [Vector((0.10, 0.30, 1.76)), Vector((-0.18, 0.06, 1.62)), Vector((-0.10, -0.18, 1.38)), Vector((0.06, -0.16, 1.18))]
pts, rad = spline(strap, [0.028] * 4, per=5)
# crimson baldric : the one strong identity line across the torso in the sprite view, where the chest tunic is
# hidden behind the bow arm (a dark strap there read as a hole, a leather one as noise)
add("Torso", sweep("Torso__Baldric", CRIMSON, pts, rad, sides=6, aspect=0.45))
for t in (0.25, 0.62):
    c = pts[int(t * (len(pts) - 1))]
    add("Torso", rbox("Torso__Rivet", GOLD, (0.035, 0.035, 0.035), c, bevel=0.008))

# =============================================================================================
# CAPE : short torn half-cape blown back from the shoulders (screen-right in the sprite : reads as speed)
# =============================================================================================
# Short torn half-cape : the first version was a full cloak and gave the druid's egg silhouette. This one only
# covers the back, stops at the hips and ends in points, so the lithe body stays visible.
cape_rows = []
for k, z in enumerate((1.82, 1.66, 1.48, 1.30, 1.14)):
    t = k / 4.0
    rx, ry, yaw = prof(min(1.74, z))
    row = []
    for c in range(13):
        u = c / 12.0
        a = math.radians(90 - 58 + 116 * u - 24 * t)               # the cape drifts toward the back as it falls
        f = 1.0 + (0.04 + 0.12 * t) * math.cos(8 * a + 0.3)
        tear = 1.0 - 0.22 * t * (1 + math.cos(9 * math.pi * u)) / 2   # torn lower edge
        row.append(((rx + 0.040 + 0.055 * t) * f * math.cos(a), (ry + 0.040 + 0.13 * t) * f * math.sin(a) + 0.02 + 0.09 * t,
                    z + (1 - tear) * 0.22 + 0.03 * t * math.sin(3 * a)))
    cape_rows.append(row)
add("Cape", surface("Cape__Cloak", CLOTH_DARK, cape_rows[::-1], closed_top=False, wrap=False, subsurf=1, solidify=0.014))
# torn scarf tail, flying back and up (single ribbon : two of them read as tentacles)
tail = [Vector((-0.04, 0.14, 1.87)), Vector((0.02, 0.36, 1.82)), Vector((0.08, 0.58, 1.90)), Vector((0.10, 0.78, 1.83)), Vector((0.07, 0.94, 1.90))]
pts, rad = spline(tail, [0.060, 0.055, 0.046, 0.034, 0.012], per=4)
add("Cape", sweep("Cape__Scarf", CRIMSON, pts, rad, sides=8, aspect=0.30, twist=0.12))
add("Cape", sweep("Cape__ScarfKnot", CRIMSON_DARK, *spline([Vector((-0.05, 0.10, 1.88)), Vector((0.02, 0.18, 1.86))], [0.062, 0.058], per=3), sides=10))

# =============================================================================================
# BOW (Weapon) : recurve built in the bow frame, wrapped grip, carved limbs, ember rune, string and nocked arrow
# =============================================================================================
LIMB = [  # (u along the limb, d forward) : half a recurve, the tip curls back toward the string
    (0.00, 0.020), (0.10, 0.032), (0.24, 0.062), (0.40, 0.100), (0.56, 0.130), (0.68, 0.138), (0.76, 0.118), (0.80, 0.082),
]
# A limb is a flat blade : thin across the bow plane, wide inside it. `sweep` puts the radius across the plane and
# radius * aspect inside it, so a wide limb needs a small radius and a big aspect (the first version had it the
# other way round and the bow read as a round stick).
LIMB_R = [0.019, 0.018, 0.0165, 0.015, 0.0135, 0.0115, 0.0095, 0.0075]
for sgn in (1, -1):
    path = [bowp(sgn * u, d) for u, d in LIMB]
    pts, rad = spline(path, LIMB_R, per=5)
    add("Weapon", sweep("Weapon__Limb", WOOD, pts, rad, sides=10, aspect=2.6))
    # flared bone tip (nock groove of the bow) and a pale inlay running along the visible face of the limb
    tip = bowp(sgn * LIMB[-1][0], LIMB[-1][1])
    add("Weapon", blob("Weapon__Tip", BONE, 0.020, tip, scale=(0.5, 1.7, 1.4), rot=frame(BOW_V, BOW_A, BOW_U), segs=10, rings=6))
    inlay = [bowp(sgn * u, d) - BOW_V * 0.019 for u, d in LIMB[1:-1]]
    pts, rad = spline(inlay, [0.008, 0.007, 0.006, 0.005, 0.0045, 0.004], per=4)
    add("Weapon", sweep("Weapon__Inlay", BONE_DARK, pts, rad, sides=6, aspect=1.6))
# riser and wrapped grip (rounder than the limbs : the fist has to close on it)
pts, rad = spline([bowp(-0.21, 0.010), bowp(-0.12, 0.016), bowp(0.0, 0.019), bowp(0.12, 0.016), bowp(0.21, 0.010)],
                  [0.024, 0.029, 0.031, 0.029, 0.024], per=4)
add("Weapon", sweep("Weapon__Riser", WOOD_DARK, pts, rad, sides=10, aspect=1.35))
for i in range(6):
    u = -0.075 + 0.030 * i
    add("Weapon", sweep("Weapon__Wrap", LEATHER, [bowp(u - 0.011, 0.019), bowp(u + 0.011, 0.019)], [0.033, 0.033], sides=10, cap="flat", aspect=1.25))
add("Weapon", blob("Weapon__Glow_Rune", GLOW, 0.030, bowp(0.32, 0.084) - BOW_V * 0.016, scale=(0.35, 0.55, 1.6), rot=frame(BOW_V, BOW_A, BOW_U), segs=8, rings=6))
# arrow shelf, on the side where the arrow rests
add("Weapon", blob("Weapon__Shelf", BONE_DARK, 0.026, bowp(0.055, 0.026) - BOW_V * 0.022, scale=(1.3, 0.9, 0.5), rot=frame(BOW_V, BOW_A, BOW_U), segs=8, rings=6))
# string : upper tip -> nock -> lower tip (rig_archer.py stretches it between the two hands)
up_tip, lo_tip = bowp(LIMB[-1][0], LIMB[-1][1]), bowp(-LIMB[-1][0], LIMB[-1][1])
string_path = [up_tip, lerp(up_tip, NOCK, 0.5), NOCK + BOW_U * 0.02, NOCK, NOCK - BOW_U * 0.02, lerp(lo_tip, NOCK, 0.5), lo_tip]
add("Weapon", sweep("Weapon__String", STRING, string_path, [0.006, 0.006, 0.008, 0.009, 0.008, 0.006, 0.006], sides=5))
# released (braced) string : straight from tip to tip. animate_archer.py swaps the two after the shot, otherwise
# the string would follow the drawing hand backwards instead of snapping forward.
braced_path = [up_tip, lerp(up_tip, lo_tip, 0.35), bowp(0.0, LIMB[-1][1] - 0.004), lerp(up_tip, lo_tip, 0.65), lo_tip]
add("Weapon", sweep("Weapon__Braced", BRACED, braced_path, [0.006, 0.006, 0.007, 0.006, 0.006], sides=5))
# nocked arrow : shaft along the bow axis, steel head past the grip, three crimson fletchings at the nock
head_end = NOCK + BOW_A * 1.00
pts, rad = spline([NOCK, lerp(NOCK, head_end, 0.5), head_end - BOW_A * 0.07], [0.014, 0.013, 0.012], per=4)
add("Weapon", sweep("Weapon__ArrowShaft", ARROW_SHAFT, pts, rad, sides=7))
add("Weapon", lib.crystal("Weapon__ArrowHead", ARROW_HEAD, 0.038, 0.155, tuple(head_end - BOW_A * 0.075), sides=4, waist=0.16,
                          rot=frame(BOW_V, BOW_U, BOW_A)))
add("Weapon", blob("Weapon__ArrowNock", WOOD_DARK, 0.018, NOCK + BOW_A * 0.012, scale=(1, 1, 1.2), rot=frame(BOW_V, BOW_A, BOW_U), segs=8, rings=6))
for k in range(3):
    b = 2 * math.pi * k / 3 + 0.35
    d = (BOW_U * math.cos(b) + BOW_V * math.sin(b))
    add("Weapon", lib2.leaf("Weapon__ArrowFletch", ARROW_FLETCH, 0.145, 0.042, NOCK + BOW_A * 0.095 + d * 0.012,
                            d - BOW_A * 1.5, curl=0.12, droop=0.05, thick=0.005, across=2, along=4))

# =============================================================================================
# Merge by pivot and material, joints, clearance check, renders
# =============================================================================================
for pivot_name, objs in objects.items():
    by_mat = {}
    for o in objs:
        by_mat.setdefault(o.data.materials[0].name, []).append(o)
    for mat_name, group in by_mat.items():
        is_glow = "Glow" in mat_name
        name = f"{pivot_name}__{'Glow_' if is_glow else ''}{mat_name.replace('GlowEye', 'Eye').replace('Glow', '') or 'Light'}"
        if len(group) > 1:
            join(group, name)
        else:
            group[0].name = name
            group[0].data.name = name

bpy.data.objects["Weapon__Braced"].hide_render = True   # only shown by the animation, after the release

pivot("Torso", (0, 0, 1.30))
pivot("Head", (0, 0, 1.88))
pivot("ArmR", tuple(SR))
pivot("ArmL", tuple(SL))
pivot("Cape", (0, 0.18, 1.82))
pivot("Weapon", tuple(GRIP))

# ---- automatic clearance check : the bow, the string and the arrow must stay 7 cm away from the body ----
from mathutils.bvhtree import BVHTree

bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()
bodies = []
for o in bpy.context.scene.objects:
    if o.type == "MESH" and o.name.split("__")[0] in ("Root", "Torso", "Head", "Cape"):
        me = o.evaluated_get(dg).to_mesh()
        bodies.append((o.name, BVHTree.FromPolygons([v.co[:] for v in me.vertices], [p.vertices[:] for p in me.polygons])))
probes = [bowp(u, lerp(LIMB[0][1], LIMB[-1][1], abs(u) / 0.8)) for u in [(-8 + i) * 0.1 for i in range(17)]]
probes += [NOCK + BOW_A * (0.05 * i) for i in range(20)]                         # the arrow
probes += [lerp(up_tip, NOCK, i / 8) for i in range(9)] + [lerp(lo_tip, NOCK, i / 8) for i in range(9)]   # the string
worst = (9.0, "", None)
for p in probes:
    for name, tree in bodies:
        hit = tree.find_nearest(Vector(p))
        if hit[0] is not None and hit[3] < worst[0]:
            worst = (hit[3], name, tuple(round(c, 2) for c in p))
print("CLEARANCE bow/body min", round(worst[0], 3), "with", worst[1], "at", worst[2])
if worst[0] < 0.07:
    raise RuntimeError(f"the bow is too close to {worst[1]} ({worst[0]:.3f} m at {worst[2]}) : do not export")

# The pose points travel with the .blend : rig_archer.py fits the metarig from them instead of copying the
# numbers again (the druid pipeline duplicated its arm points in the rig script).
bpy.context.scene["archer_pose"] = {name: list(value) for name, value in (
    ("SL", SL), ("EL", EL), ("WL", WL), ("GRIP", GRIP), ("SR", SR), ("ER", ER), ("WR", WR), ("NOCK", NOCK),
    ("HIP_L", HIP_L), ("KNEE_L", KNEE_L), ("ANK_L", ANK_L), ("TOE_L", TOE_L),
    ("HIP_R", HIP_R), ("KNEE_R", KNEE_R), ("ANK_R", ANK_R), ("TOE_R", TOE_R),
    ("HEAD_C", HEAD_C), ("TIP_UP", up_tip), ("TIP_LO", lo_tip), ("HEAD_END", head_end),
    ("BOW_A", BOW_A), ("BOW_U", BOW_U), ("BOW_V", BOW_V))}

print("ARCHER TRIANGLES", lib.triangle_count())
lib.setup_render(560, 900)
lib.render_views(os.path.join(OUT, "archer"), views=(("front", 0), ("three_quarter", 40), ("sprite", 60), ("side", 90), ("back", 180)),
                 center=(0, -0.10, 1.32), distance=8.0, lens=80)
lib.render_views(os.path.join(OUT, "archer_head"), views=(("front", 0), ("three_quarter", 40)), center=(0, -0.12, 2.05), distance=3.0, lens=80)
lib.render_views(os.path.join(OUT, "archer_hands"), views=(("front", 0), ("sprite", 60)), center=(0.02, -0.60, 1.82), distance=2.6, lens=75)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "archer.blend"))
print("SAVED", os.path.join(OUT, "archer.blend"))
