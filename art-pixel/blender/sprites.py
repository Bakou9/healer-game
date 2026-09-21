"""Dead Cells-style sprite pipeline test (D-071) : render a 3D hero from a fixed side camera, then turn each render into pixel art.

Run :  blender --background --python art-pixel/blender/sprites.py -- [size] [colors]
In   : art-3d/blender/out/druid_v4.blend (pivots as empties Pivot_<Name>, meshes named <Pivot>__<Piece>)
Out  : art-pixel/out/druid_<size>_<pose>.png (one sprite per pose, native resolution), druid_<size>_sheet.png (sprite sheet),
       druid_<size>_preview.png (sheet upscaled x3 on the game background, for quick review)

Pipeline : 1) parent the meshes to their pivot empties, 2) pose the pivots (same angles as UnitRig.ApplyPose), 3) render with a
transparent film at 4x the target size (orthographic, pure side view, character facing right), 4) alpha-aware box downscale,
5) one shared palette per character (k-means), 6) 1 px dark outline. Everything is deterministic (no random without a seed).
"""
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
BLEND = os.path.join(ROOT, "art-3d", "blender", "out", "druid_v4.blend")
OUT = os.path.join(ROOT, "art-pixel", "out")
os.makedirs(OUT, exist_ok=True)

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
SIZE = int(args[0]) if len(args) > 0 else 128        # sprite frame (square), pixels
COLORS = int(args[1]) if len(args) > 1 else 28       # palette size
YAW = float(args[2]) if len(args) > 2 else 0.0         # camera orbit toward the front (degrees) : 0 = pure side view, ~35 = three-quarter
FRAME = float(args[3]) if len(args) > 3 else 4.7       # orthographic height covered (meters)
SIDE = int(args[4]) if len(args) > 4 else -1            # -1 : camera on the staff side, character faces right ; +1 : camera on the free-hand side, character faces left (toward the boss, like the FFVI party)
OVERSAMPLE = 4

bpy.ops.wm.open_mainfile(filepath=BLEND)

# ---- 1) rig : parent every mesh to its pivot empty, and pivots to each other -------------------
scene = bpy.context.scene
empties = {o.name[len("Pivot_"):]: o for o in scene.objects if o.type == "EMPTY" and o.name.startswith("Pivot_")}


def parent_keep(child, parent):
    child.parent = parent
    child.matrix_parent_inverse = parent.matrix_world.inverted()


for child, parent in (("Head", "Torso"), ("ArmR", "Torso"), ("ArmL", "Torso"), ("Cape", "Torso"), ("Weapon", "ArmR")):
    parent_keep(empties[child], empties[parent])
for o in scene.objects:
    if o.type == "MESH":
        pivot_name = o.name.split("__")[0]
        if pivot_name in empties:
            parent_keep(o, empties[pivot_name])


# ---- 2) poses (degrees ; negative pitch = forward/up, same convention as UnitRig) ---------------
def smooth(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


POSES = {
    #            torsoX, headX, armR, armRz, armL, armLz, weaponX, capeX
    "idle": dict(),
    "cast_raise": dict(torsoX=-4, headX=-6, armR=-19, armRz=-3, armL=-50, armLz=8, weaponX=16, capeX=6),
    "cast_hold": dict(torsoX=-7, headX=-10, armR=-38, armRz=-6, armL=-100, armLz=16, weaponX=27, capeX=12),
    "cast_release": dict(torsoX=8, headX=2, armR=-62, armRz=-6, armL=-120, armLz=16, weaponX=45, capeX=35),
    "hit": dict(torsoX=10, headX=16, armR=8, armRz=-8, armL=8, armLz=10),
    "fall": dict(torsoX=30, headX=25, armR=-30, armL=-30),
}


def apply_pose(p):
    def rot(name, x=0.0, z=0.0):
        empties[name].rotation_euler = (math.radians(x), 0.0, math.radians(z))
    rot("Torso", p.get("torsoX", 0))
    rot("Head", p.get("headX", 0))
    rot("ArmR", p.get("armR", 0), p.get("armRz", 0))
    rot("ArmL", p.get("armL", 0), p.get("armLz", 0))
    rot("Weapon", p.get("weaponX", 0))
    rot("Cape", p.get("capeX", 0))
    bpy.context.view_layer.update()


# ---- 3) camera and render -----------------------------------------------------------------------
cam_data = bpy.data.cameras.new("SpriteCam")
cam_data.type = "ORTHO"
cam_data.ortho_scale = FRAME
cam = bpy.data.objects.new("SpriteCam", cam_data)
bpy.context.collection.objects.link(cam)
cam.location = Vector((SIDE * 12.0 * math.cos(math.radians(YAW)), -12.0 * math.sin(math.radians(YAW)), 1.95))
cam.rotation_euler = (math.radians(90), 0, math.radians(-90 + YAW if SIDE < 0 else 90 - YAW))     # looks toward +X : character (facing -Y) faces screen right
scene.camera = cam
scene.render.engine = "BLENDER_EEVEE"
scene.render.film_transparent = True
scene.render.resolution_x = scene.render.resolution_y = SIZE * OVERSAMPLE
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGBA"
world = bpy.data.worlds.new("SpriteWorld")
world.use_nodes = True
bg = next(n for n in world.node_tree.nodes if n.type == "BACKGROUND")
bg.inputs["Color"].default_value = (0.09, 0.10, 0.14, 1)
bg.inputs["Strength"].default_value = 1.0
scene.world = world
for o in list(scene.objects):
    if o.type == "LIGHT":
        bpy.data.objects.remove(o, do_unlink=True)


def light(name, kind, loc, energy, color, rot=(0, 0, 0)):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    data.color = color
    o = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(o)
    o.location = loc
    o.rotation_euler = rot


# lighting for readable sprites : strong key from the front-top, cool fill, purple rim from behind
light("Key", "SUN", (0, 0, 5), 3.6, (1.0, 0.95, 0.88), (math.radians(55), 0, math.radians(-70)))
light("Fill", "SUN", (0, 0, 5), 1.5, (0.62, 0.72, 1.0), (math.radians(70), 0, math.radians(110)))
light("Rim", "SUN", (0, 0, 5), 2.4, (0.65, 0.4, 1.0), (math.radians(60), 0, math.radians(-160)))

raw = {}
for name, pose in POSES.items():
    apply_pose(pose)
    path = os.path.join(OUT, f"_raw_{name}.png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(path)
    img.colorspace_settings.name = "Non-Color"        # raw values : no color-space conversion when reading
    n = SIZE * OVERSAMPLE
    a = np.array(img.pixels[:], dtype=np.float32).reshape(n, n, 4)[::-1]
    raw[name] = a
    bpy.data.images.remove(img)
    os.remove(path)


# ---- 4) alpha-aware downscale ---------------------------------------------------------------------
def downscale(a):
    n = a.shape[0]
    f = OVERSAMPLE
    pre = a.copy()
    pre[..., :3] *= pre[..., 3:4]
    small = pre.reshape(n // f, f, n // f, f, 4).mean(axis=(1, 3))
    alpha = small[..., 3]
    rgb = small[..., :3] / np.maximum(alpha[..., None], 1e-6)
    return rgb, alpha


frames = {}
for name, a in raw.items():
    rgb, alpha = downscale(a)
    frames[name] = (rgb, alpha >= 0.5)

# ---- 5) one shared palette (k-means, fixed seed), applied to every frame ---------------------------
samples = np.concatenate([rgb[mask] for rgb, mask in frames.values()])
rng = np.random.default_rng(3)
centers = samples[rng.choice(len(samples), COLORS, replace=False)]
for _ in range(12):
    d = ((samples[:, None, :] - centers[None, :, :]) ** 2).sum(axis=2)
    lab = d.argmin(axis=1)
    for k in range(COLORS):
        pts = samples[lab == k]
        if len(pts):
            centers[k] = pts.mean(axis=0)

OUTLINE = np.array([0.02, 0.025, 0.04])


def quantize(rgb, mask):
    d = ((rgb[..., None, :] - centers[None, None, :, :]) ** 2).sum(axis=3)
    q = centers[d.argmin(axis=2)]
    out = np.zeros(rgb.shape[:2] + (4,), dtype=np.float32)
    out[..., :3] = q
    out[..., 3] = mask
    # 1 px outline around the silhouette (4-neighbour dilation)
    grown = mask.copy()
    grown[1:, :] |= mask[:-1, :]
    grown[:-1, :] |= mask[1:, :]
    grown[:, 1:] |= mask[:, :-1]
    grown[:, :-1] |= mask[:, 1:]
    edge = grown & ~mask
    out[edge, :3] = OUTLINE
    out[edge, 3] = 1.0
    return out


def to_srgb8(a):
    """Raw values are already in the PNG's stored (sRGB) encoding : just clamp to bytes."""
    return (np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8)


def write_png(path, arr8):
    import struct
    import zlib
    h, w = arr8.shape[:2]
    raw_rows = b"".join(b"\x00" + arr8[y].tobytes() for y in range(h))

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw_rows, 9)) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


sprites = {name: to_srgb8(quantize(rgb, mask)) for name, (rgb, mask) in frames.items()}
for name, s in sprites.items():
    write_png(os.path.join(OUT, f"druid_{SIZE}_y{int(YAW)}{"l" if SIDE > 0 else "r"}_{name}.png"), s)
sheet = np.concatenate(list(sprites.values()), axis=1)
write_png(os.path.join(OUT, f"druid_{SIZE}_y{int(YAW)}{"l" if SIDE > 0 else "r"}_sheet.png"), sheet)

# preview : x3 nearest-neighbour on the game background colour
bgc = np.array([26, 24, 40, 255], dtype=np.uint8)
prev = np.repeat(np.repeat(sheet, 3, axis=0), 3, axis=1)
canvas = np.tile(bgc, (prev.shape[0], prev.shape[1], 1))
alpha = prev[..., 3:4].astype(np.float32) / 255
canvas[..., :3] = (prev[..., :3] * alpha + canvas[..., :3] * (1 - alpha)).astype(np.uint8)
write_png(os.path.join(OUT, f"druid_{SIZE}_y{int(YAW)}{"l" if SIDE > 0 else "r"}_preview.png"), canvas)
print("SPRITES", SIZE, "px,", COLORS, "colors,", len(sprites), "poses ->", OUT)
