"""Animate the Rigify-rigged druid and render pixel-art sprite sheets (D-073).

Run :  blender --background --python art-pixel/blender/animate_druid.py -- [size] [colors] [yaw] [--unity]
Needs : art-pixel/out/druid_rigged.blend (made by rig_druid.py)
Out    : art-pixel/out/druid_<anim>.png (sprite sheet, frames side by side), druid_anims.json (frame counts, fps, loop),
         druid_preview.gif / .mp4 (ffmpeg), and with --unity the sheets are copied to unity/HealerGame/Assets/Resources/Sprites/Druid/.

Poses are planar (side plane), expressed as angles about the world X axis (negative = forward / up) on the Rigify FK controls, so each
animation is a few keyframes of a few numbers. Sleeves bend at the elbow thanks to the blended weights of rig_druid.py.
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
UNITY_DIR = os.path.join(ROOT, "unity", "HealerGame", "Assets", "Resources", "Sprites", "Druid")
args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
nums = [a for a in args if not a.startswith("--")]
SIZE = int(nums[0]) if len(nums) > 0 else 128
COLORS = int(nums[1]) if len(nums) > 1 else 28
YAW = float(nums[2]) if len(nums) > 2 else 30.0
TO_UNITY = "--unity" in args

bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT, "druid_rigged.blend"))
scene = bpy.context.scene
rig = bpy.data.objects["rig"]
bpy.context.view_layer.objects.active = rig

for side in ("L", "R"):
    rig.pose.bones[f"upper_arm_parent.{side}"]["IK_FK"] = 1.0    # arms driven by their FK controls


SWING_L = -55.0   # degrees about the world Z axis at the left shoulder (negative = toward the front)


# ---- posing (world-space rotations about the joint, so it does not depend on the bones' local axes) ------------
def reset_pose():
    for pb in rig.pose.bones:
        pb.location = (0, 0, 0)
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()


def rot_axis(name, axis, deg):
    if abs(deg) < 1e-6:
        return
    pb = rig.pose.bones[name]
    m = rig.matrix_world @ pb.matrix
    head = m.translation.copy()
    rot = Matrix.Translation(head) @ Matrix.Rotation(math.radians(deg), 4, axis) @ Matrix.Translation(-head)
    pb.matrix = rig.matrix_world.inverted() @ (rot @ m)
    bpy.context.view_layer.update()


def rot_x(name, deg):
    rot_axis(name, "X", deg)


def move_z(name, dz):
    if abs(dz) < 1e-6:
        return
    pb = rig.pose.bones[name]
    m = rig.matrix_world @ pb.matrix
    pb.matrix = rig.matrix_world.inverted() @ (Matrix.Translation((0, 0, dz)) @ m)
    bpy.context.view_layer.update()


def apply(p):
    """p : dict of degrees (spine, head, armR, elbowR, wristR, armL, elbowL, wristL), drop (m), keep_staff (0/1)."""
    reset_pose()
    move_z("torso", -p.get("drop", 0.0))
    spine = p.get("spine", 0.0)
    for name in ("spine_fk", "spine_fk.001", "spine_fk.002", "spine_fk.003"):
        rot_x(name, spine / 4.0)
    head = p.get("head", 0.0)
    rot_x("neck", head * 0.35)
    rot_x("head", head * 0.65)
    for side in ("R", "L"):
        arm, elbow, wrist = p.get("arm" + side, 0.0), p.get("elbow" + side, 0.0), p.get("wrist" + side, 0.0)
        if side == "R" and p.get("keep_staff", 1.0):
            wrist += -0.9 * (arm + elbow)            # the hand keeps the staff almost upright while the arm moves
        if side == "L":
            rot_axis("upper_arm_fk.L", "Z", SWING_L)      # side-view sprite : the free arm points forward (in the side plane), not toward the camera
        rot_x(f"upper_arm_fk.{side}", arm)
        rot_x(f"forearm_fk.{side}", elbow)
        rot_x(f"hand_fk.{side}", wrist)


# ---- animations : parameters as a function of u (0..1) -----------------------------------------------------------
def smooth(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


def mix(a, b, t):
    keys = set(a) | set(b)
    return {k: a.get(k, 0.0) + (b.get(k, 0.0) - a.get(k, 0.0)) * t for k in keys}


def scaled(p, s):
    return {k: v * s for k, v in p.items()}


CAST = dict(spine=-6, head=-9, armR=-30, elbowR=-8, armL=-85, elbowL=-15, wristL=-10)
THRUST = dict(spine=9, head=4, armR=-50, elbowR=-20, armL=-100, elbowL=-35, wristL=-5)


def idle(u):
    b = math.sin(2 * math.pi * u)
    return dict(spine=-1.2 * b, head=1.4 * b, armL=2.5 * b, armR=1.2 * b, elbowL=-2 * b)


def cast_raise(u):
    return scaled(CAST, smooth(u))


def cast_hold(u):
    p = dict(CAST)
    p["armL"] += 3 * math.sin(4 * math.pi * u)
    p["wristL"] += 7 * math.sin(2 * math.pi * u)
    p["spine"] += 1.2 * math.sin(2 * math.pi * u)
    return p


def cast_release(u):
    return mix(mix(CAST, THRUST, smooth(u / 0.4)), {}, smooth((u - 0.4) / 0.6))


def attack(u):
    wind = dict(spine=-8, head=-4, armR=22, elbowR=-30, armL=-20)
    strike = dict(spine=14, head=6, armR=-70, elbowR=-30, armL=-40, elbowL=-10)
    if u < 0.35:
        return mix({}, wind, smooth(u / 0.35))
    if u < 0.6:
        return mix(wind, strike, smooth((u - 0.35) / 0.25))
    return mix(strike, {}, smooth((u - 0.6) / 0.4))


def hit(u):
    peak = dict(spine=12, head=18, armL=10, armR=8, drop=0.03)
    return mix({}, peak, smooth(u / 0.35)) if u < 0.35 else mix(peak, {}, smooth((u - 0.35) / 0.65))


def death(u):
    s = smooth(u)
    return dict(spine=52 * s, head=32 * s, armR=28 * s, armL=30 * s, elbowL=-10 * s, drop=0.62 * s * s, keep_staff=0.0)


# name : (function, frames, fps, loop)
ANIMS = {
    "idle": (idle, 6, 6, True),
    "cast_raise": (cast_raise, 4, 12, False),
    "cast_hold": (cast_hold, 6, 10, True),
    "cast_release": (cast_release, 5, 14, False),
    "attack": (attack, 6, 14, False),
    "hit": (hit, 3, 12, False),
    "death": (death, 7, 8, False),
}

# ---- render every frame ------------------------------------------------------------------------------------------
sl.setup_camera(scene, SIZE, yaw=YAW, side=1)
raw = {}
for name, (fn, n, fps, loop) in ANIMS.items():
    raw[name] = []
    for i in range(n):
        u = i / n if loop else i / max(1, n - 1)
        apply(fn(u))
        raw[name].append(sl.render_raw(scene, SIZE, os.path.join(OUT, "_frame.png")))
print("RENDERED", sum(len(v) for v in raw.values()), "frames")

small = {name: [sl.downscale(a) for a in frames] for name, frames in raw.items()}
palette = sl.build_palette([f for frames in small.values() for f in frames], COLORS)
sprites = {name: [sl.quantize(rgb, mask, palette) for rgb, mask in frames] for name, frames in small.items()}

meta = {"size": SIZE, "colors": COLORS, "yaw": YAW, "facing": "left", "anims": {}}
for name, frames in sprites.items():
    sheet = np.concatenate(frames, axis=1)
    sl.write_png(os.path.join(OUT, f"druid_{name}.png"), sheet)
    _, n, fps, loop = ANIMS[name]
    meta["anims"][name] = {"frames": n, "fps": fps, "loop": loop}
with open(os.path.join(OUT, "druid_anims.json"), "w") as f:
    json.dump(meta, f, indent=2)
with open(os.path.join(OUT, "druid_anims.txt"), "w") as f:   # simple format for Unity (no JSON dependency in the client) : size, then "name frames fps loop" per animation
    f.write(f"size {SIZE}\n")
    for name, info in meta["anims"].items():
        f.write(f"{name} {info['frames']} {info['fps']} {int(info['loop'])}\n")

# ---- contact sheet : every frame of every animation on the game background (one row per animation), for a quick look --------
row_w = max(len(f) for f in sprites.values()) * SIZE * 2
rows = []
for name, frames in sprites.items():
    row = np.concatenate([sl.on_background(fr, 2) for fr in frames], axis=1)
    pad = np.tile(np.array(sl.BACKGROUND8, dtype=np.uint8), (row.shape[0], row_w - row.shape[1], 1))
    rows.append(np.concatenate([row, pad], axis=1))
sl.write_png(os.path.join(OUT, "druid_contact.png"), np.concatenate(rows, axis=0))

# ---- preview (ffmpeg) : a playlist of the animations on the game background ----------------------------------------
frames_dir = os.path.join(OUT, "_preview_frames")
shutil.rmtree(frames_dir, ignore_errors=True)
os.makedirs(frames_dir)
PREVIEW_FPS = 12
playlist = [("idle", 2), ("cast_raise", 1), ("cast_hold", 2), ("cast_release", 1), ("idle", 1), ("attack", 1), ("idle", 1), ("hit", 1), ("idle", 1), ("death", 1)]
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
paths = sl.encode_previews(frames_dir, "f_%05d.png", PREVIEW_FPS, os.path.join(OUT, "druid_preview"))
shutil.rmtree(frames_dir, ignore_errors=True)
print("PREVIEW", paths)

if TO_UNITY:
    os.makedirs(UNITY_DIR, exist_ok=True)
    for name in ANIMS:
        shutil.copy(os.path.join(OUT, f"druid_{name}.png"), os.path.join(UNITY_DIR, f"druid_{name}.png"))
    shutil.copy(os.path.join(OUT, "druid_anims.txt"), os.path.join(UNITY_DIR, "druid_anims.txt"))
    print("UNITY", UNITY_DIR)
print("DONE", SIZE, "px,", COLORS, "colors,", len(ANIMS), "animations")
