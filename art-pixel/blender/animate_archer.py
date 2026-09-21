"""Animate the Rigify-rigged Archere and render pixel-art sprite sheets (same pipeline as animate_druid.py).

Run :  blender --background --python art-pixel/blender/animate_archer.py -- [size] [colors] [yaw]
Needs : art-pixel/out/archer_rigged.blend (made by rig_archer.py)
Out   : art-pixel/out/archer_<anim>.png (sprite sheet, frames side by side), archer_anims.txt / .json,
        archer_contact.png (contact sheet) and the ffmpeg previews archer_preview.gif / .mp4.
Nothing is copied into the Unity folders.

The archer rests at full draw, so the poses are small deltas around it :
- the bow arm and the spine are FK rotations about the world X axis (as for the druid) ;
- the DRAWING hand is driven in IK and slides along the arrow axis (`draw` in metres, + = toward the bow). The
  bowstring is skinned between the two hands, so it opens into a V by itself ; on release the drawn string and the
  arrow are hidden and the braced string is shown, otherwise the string would follow the hand backwards.
"""
import json
import math
import os
import shutil
import sys

import bpy
import numpy as np
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import importlib

import sprite_lib as sl

importlib.reload(sl)

ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "art-pixel", "out")
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
nums = [a for a in args if not a.startswith("--")]
SIZE = int(nums[0]) if len(nums) > 0 else 128
COLORS = int(nums[1]) if len(nums) > 1 else 28
YAW = float(nums[2]) if len(nums) > 2 else 30.0
# The archer is 2.6 m tall bow included, against 4.0 m for the antlered druid : keeping the druid's 4.3 m frame
# would waste 40 % of the sprite. Same rule for the feet (0.2 m above the bottom edge), tighter frame.
FRAME = 3.2
CENTER_Z = FRAME / 2 - 0.2

bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT, "archer_rigged.blend"))
scene = bpy.context.scene
rig = bpy.data.objects["rig"]
bpy.context.view_layer.objects.active = rig
POSE = {name: Vector(value) for name, value in scene["archer_pose"].items()}
A = POSE["BOW_A"]           # arrow axis, pointing away from the archer

for side in ("L", "R"):
    for name in (f"upper_arm_parent.{side}", f"thigh_parent.{side}"):
        if name in rig.pose.bones:
            rig.pose.bones[name]["IK_FK"] = 1.0                  # FK arms and legs by default
rig.pose.bones["upper_arm_parent.R"]["IK_FK"] = 0.0              # drawing arm : IK, the hand slides along the arrow
for required in ("hand_ik.R", "upper_arm_fk.L", "torso", "spine_fk"):
    if required not in rig.pose.bones:                           # fail loudly : a silently missing control = a dead animation
        raise RuntimeError(f"control {required} missing from the rig : {sorted(b.name for b in rig.pose.bones)[:40]}")

# pieces whose visibility changes during the shot
DRAWN = [o for o in scene.objects if o.type == "MESH" and "Weapon__String" in o.name]
BRACED = [o for o in scene.objects if o.type == "MESH" and "Weapon__Braced" in o.name]
ARROW = [o for o in scene.objects if o.type == "MESH" and "Weapon__Arrow" in o.name]
print("PIECES string", len(DRAWN), "braced", len(BRACED), "arrow", len(ARROW))


def show(nocked):
    """nocked : arrow on the string and string drawn ; otherwise the shot is gone and the string is braced."""
    for o in DRAWN + ARROW:
        o.hide_render = not nocked
    for o in BRACED:
        o.hide_render = nocked


# ---- posing (world-space transforms, so it does not depend on the bones' local axes) ------------
def reset_pose():
    for pb in rig.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()


def rot_axis(name, axis, deg):
    if abs(deg) < 1e-6 or name not in rig.pose.bones:
        return
    pb = rig.pose.bones[name]
    m = rig.matrix_world @ pb.matrix
    head = m.translation.copy()
    rot = Matrix.Translation(head) @ Matrix.Rotation(math.radians(deg), 4, axis) @ Matrix.Translation(-head)
    pb.matrix = rig.matrix_world.inverted() @ (rot @ m)
    bpy.context.view_layer.update()


def rot_x(name, deg):
    rot_axis(name, "X", deg)


def move(name, delta):
    """Translate a control in world space (used for the torso and for the IK hand)."""
    if delta.length < 1e-6 or name not in rig.pose.bones:
        return
    pb = rig.pose.bones[name]
    m = rig.matrix_world @ pb.matrix
    pb.matrix = rig.matrix_world.inverted() @ (Matrix.Translation(delta) @ m)
    bpy.context.view_layer.update()


def apply(p):
    """p : degrees (spine, head, armL, elbowL, wristL, thigh, shin), metres (drop, draw : + = hand toward the bow),
    handR : extra world offset of the drawing hand, bow : degrees of bow roll about the arrow axis."""
    reset_pose()
    move("torso", Vector((0, 0, -p.get("drop", 0.0))))
    spine = p.get("spine", 0.0)
    for name in ("spine_fk", "spine_fk.001", "spine_fk.002", "spine_fk.003"):
        rot_x(name, spine / 4.0)
    head = p.get("head", 0.0)
    rot_x("neck", head * 0.35)
    rot_x("head", head * 0.65)
    for side in ("L", "R"):
        rot_x(f"thigh_fk.{side}", p.get("thigh", 0.0))
        rot_x(f"shin_fk.{side}", p.get("shin", 0.0))
    # bow arm : FK, the bow rises and falls with it
    rot_x("upper_arm_fk.L", p.get("armL", 0.0))
    rot_x("forearm_fk.L", p.get("elbowL", 0.0))
    rot_x("hand_fk.L", p.get("wristL", 0.0))
    rot_axis("hand_fk.L", A, p.get("bow", 0.0))
    # drawing hand : IK, sliding along the arrow
    move("hand_ik.R", A * p.get("draw", 0.0) + p.get("handR", Vector((0, 0, 0))))
    rot_axis("hand_ik.R", "X", p.get("wristR", 0.0))


# ---- animations : parameters as a function of the frame -----------------------------------------------------------
def smooth(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


def mix(a, b, t):
    keys = set(a) | set(b)
    zero = {k: (Vector((0, 0, 0)) if k == "handR" else 0.0) for k in keys}
    return {k: a.get(k, zero[k]) + (b.get(k, zero[k]) - a.get(k, zero[k])) * t for k in keys}


def idle(u):
    """Held at full draw : breathing, a slow tremor in the bow arm, the aim drifting by a hair."""
    b = math.sin(2 * math.pi * u)
    c = math.sin(4 * math.pi * u + 1.0)
    return dict(spine=-0.8 * b, head=0.7 * b, armL=1.6 * b, elbowL=-0.8 * b, draw=0.008 * c, wristR=1.2 * b)


# nock -> draw -> hold -> release -> recoil -> a new arrow on the string
ATTACK = [
    dict(draw=0.34, spine=2.0, armL=3.0, head=-2.0, elbowL=2.0, wristR=-8.0),   # 0 nocking, bow slightly lowered
    dict(draw=0.23, spine=1.2, armL=1.0, head=-1.0, wristR=-4.0),               # 1 starting to pull
    dict(draw=0.12, spine=0.4, armL=0.0),                                       # 2 pulling
    dict(draw=0.03, spine=0.0, armL=-0.5),                                      # 3 almost at full draw
    dict(draw=-0.05, spine=1.4, armL=-1.0, head=1.0),                           # 4 full draw, leaning into the shot
    dict(draw=-0.17, spine=-5.0, armL=-6.0, head=-3.0, bow=-9.0,                # 5 release : recoil
         handR=Vector((-0.05, 0.05, 0.02)), wristR=22.0),
    dict(draw=-0.12, spine=-2.0, armL=-2.5, head=-1.0, bow=-4.0,                # 6 settling
         handR=Vector((-0.03, 0.03, 0.01)), wristR=12.0),
    dict(draw=0.05, spine=0.5, armL=0.5, wristR=-2.0),                          # 7 a new arrow is on the string
]
NOCKED = [True, True, True, True, True, False, False, True]   # frame 5 and 6 : the shot is gone


def attack(i):
    return ATTACK[i]


def hit(u):
    peak = dict(spine=-17.0, head=-22.0, armL=12.0, elbowL=9.0, draw=0.13, drop=0.05, thigh=6.0,
                handR=Vector((0.03, 0.09, -0.05)), wristR=10.0, bow=8.0)
    return mix({}, peak, smooth(u / 0.4)) if u < 0.4 else mix(peak, {}, smooth((u - 0.4) / 0.6))


def death(u):
    s = smooth(u)
    return dict(spine=76 * s, head=38 * s, armL=68 * s ** 1.3, elbowL=22 * s, wristL=-25 * s,
                thigh=-30 * s, shin=58 * s, drop=0.60 * s * s, draw=0.30 * s,
                handR=Vector((0.10, -0.06, -0.60)) * s, bow=-35 * s)


# name : (function, frames, fps, loop) ; `attack` is keyed frame by frame, the others by a 0..1 parameter
ANIMS = {
    "idle": (idle, 6, 6, True),
    "attack": (attack, 8, 14, False),
    "hit": (hit, 3, 12, False),
    "death": (death, 7, 8, False),
}

# ---- render every frame ------------------------------------------------------------------------------------------
sl.setup_camera(scene, SIZE, yaw=YAW, side=1, frame=FRAME, center_z=CENTER_Z)
raw = {}
for name, (fn, n, fps, loop) in ANIMS.items():
    raw[name] = []
    for i in range(n):
        if name == "attack":
            show(NOCKED[i])
            pose = fn(i)
        else:
            show(not (name == "death" and i >= 1))       # she lets go of the string as she falls
            pose = fn(i / n if loop else i / max(1, n - 1))
        apply(pose)
        raw[name].append(sl.render_raw(scene, SIZE, os.path.join(OUT, "_frame.png")))
print("RENDERED", sum(len(v) for v in raw.values()), "frames")

small = {name: [sl.downscale(a) for a in frames] for name, frames in raw.items()}
palette = sl.build_palette([f for frames in small.values() for f in frames], COLORS)
sprites = {name: [sl.quantize(rgb, mask, palette) for rgb, mask in frames] for name, frames in small.items()}

meta = {"size": SIZE, "colors": COLORS, "yaw": YAW, "facing": "left", "frame_units": FRAME,
        "feet_offset_units": FRAME / 2 - CENTER_Z, "anims": {}}
for name, frames in sprites.items():
    sheet = np.concatenate(frames, axis=1)
    sl.write_png(os.path.join(OUT, f"archer_{name}.png"), sheet)
    _, n, fps, loop = ANIMS[name]
    meta["anims"][name] = {"frames": n, "fps": fps, "loop": loop}
with open(os.path.join(OUT, "archer_anims.json"), "w") as f:
    json.dump(meta, f, indent=2)
with open(os.path.join(OUT, "archer_anims.txt"), "w") as f:   # same simple format as druid_anims.txt
    f.write(f"size {SIZE}\n")
    for name, info in meta["anims"].items():
        f.write(f"{name} {info['frames']} {info['fps']} {int(info['loop'])}\n")

# ---- contact sheet : every frame of every animation on the game background (one row per animation) ----------------
row_w = max(len(f) for f in sprites.values()) * SIZE * 2
rows = []
for name, frames in sprites.items():
    row = np.concatenate([sl.on_background(fr, 2) for fr in frames], axis=1)
    pad = np.tile(np.array(sl.BACKGROUND8, dtype=np.uint8), (row.shape[0], row_w - row.shape[1], 1))
    rows.append(np.concatenate([row, pad], axis=1))
sl.write_png(os.path.join(OUT, "archer_contact.png"), np.concatenate(rows, axis=0))

# ---- preview (ffmpeg) : a playlist of the animations on the game background ----------------------------------------
frames_dir = os.path.join(OUT, "_preview_frames_archer")
shutil.rmtree(frames_dir, ignore_errors=True)
os.makedirs(frames_dir)
PREVIEW_FPS = 12
playlist = [("idle", 2), ("attack", 1), ("idle", 1), ("attack", 1), ("idle", 1), ("hit", 1), ("idle", 1), ("death", 1)]
k = 0
for name, times in playlist:
    _, n, fps, loop = ANIMS[name]
    hold = max(1, round(PREVIEW_FPS / fps))
    for _ in range(times):
        for fr in sprites[name]:
            img = sl.on_background(fr, 3)
            for _ in range(hold):
                k += 1
                sl.write_png(os.path.join(frames_dir, f"f_{k:05d}.png"), img)
paths = sl.encode_previews(frames_dir, "f_%05d.png", PREVIEW_FPS, os.path.join(OUT, "archer_preview"))
shutil.rmtree(frames_dir, ignore_errors=True)
print("PREVIEW", paths)
print("DONE", SIZE, "px,", COLORS, "colors,", len(ANIMS), "animations")
