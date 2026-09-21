"""Rig the druid with Rigify (D-073) : loose parts of the 3D model are skinned to the generated deform bones.

Run :  blender --background --python art-pixel/blender/rig_druid.py
In   : art-3d/blender/out/druid_v4.blend
Out  : art-pixel/out/druid_rigged.blend (the Rigify rig `rig` with the model skinned to it ; not committed, regenerate with this script)

Steps : 1) split every mesh into loose parts (leaves, fingers, antlers...), 2) add Rigify's human metarig and fit the main bones to the
druid (spine, head, arms in their bent pose, legs), 3) generate the rig, 4) skin : rigid parts go to one deform bone (by piece name and position),
sleeves are blended across the elbow with smooth per-vertex weights so the arm bends cleanly.
"""
import math
import os

import addon_utils
import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
BLEND = os.path.join(ROOT, "art-3d", "blender", "out", "druid_v4.blend")
OUT = os.path.join(ROOT, "art-pixel", "out")
os.makedirs(OUT, exist_ok=True)

addon_utils.enable("rigify", default_set=True, persistent=False)
bpy.ops.wm.open_mainfile(filepath=BLEND)
scene = bpy.context.scene


def select_only(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


# ---- 1) split into loose parts -------------------------------------------------------------------
for o in [o for o in scene.objects if o.type == "EMPTY"]:
    bpy.data.objects.remove(o, do_unlink=True)          # the old pivot empties : the rig replaces them
originals = [o for o in scene.objects if o.type == "MESH"]
for o in originals:
    select_only(o)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.separate(type="LOOSE")
    bpy.ops.object.mode_set(mode="OBJECT")
parts = [o for o in scene.objects if o.type == "MESH"]
print("PARTS", len(originals), "meshes ->", len(parts), "loose parts")

# ---- 2) metarig, fitted to the druid ------------------------------------------------------------
bpy.ops.object.armature_human_metarig_add()
meta = bpy.context.active_object
bpy.ops.object.mode_set(mode="EDIT")
eb = meta.data.edit_bones


def fit(name, head=None, tail=None):
    b = eb[name]
    if head is not None:
        b.head = Vector(head)
    if tail is not None:
        b.tail = Vector(tail)


spine_z = [1.00, 1.16, 1.36, 1.58, 1.80, 1.90, 2.00, 2.55]
for i, name in enumerate(["spine", "spine.001", "spine.002", "spine.003", "spine.004", "spine.005", "spine.006"]):
    fit(name, (0, 0, spine_z[i]), (0, 0, spine_z[i + 1]))
# arms in their bent pose (same points as druid_v4.py : shoulder, elbow, wrist, hand end)
arms = {
    "L": [(0.42, 0.0, 1.80), (0.64, -0.10, 1.42), (0.88, -0.24, 1.44), (1.03, -0.31, 1.42)],
    "R": [(-0.42, 0.0, 1.80), (-0.53, -0.13, 1.36), (-0.665, -0.30, 1.20), (-0.71, -0.34, 1.08)],
}
for side, (s, e, w, h) in arms.items():
    fit(f"shoulder.{side}", (0.02 if side == "L" else -0.02, -0.02, 1.84), s)
    fit(f"upper_arm.{side}", s, e)
    fit(f"forearm.{side}", e, w)
    fit(f"hand.{side}", w, h)
# legs hidden under the robe : just under the hips
for side, x in (("L", 0.16), ("R", -0.16)):
    fit(f"thigh.{side}", (x, 0.0, 1.0), (x, -0.03, 0.55))
    fit(f"shin.{side}", (x, -0.03, 0.55), (x, 0.0, 0.09))
    fit(f"foot.{side}", (x, 0.0, 0.09), (x, -0.10, 0.03))
    fit(f"toe.{side}", (x, -0.10, 0.03), (x, -0.17, 0.03))
bpy.ops.object.mode_set(mode="OBJECT")

# ---- 3) generate the Rigify rig ------------------------------------------------------------------
select_only(meta)
bpy.ops.pose.rigify_generate()
rig = next(o for o in scene.objects if o.type == "ARMATURE" and o.name != meta.name)
print("RIG", rig.name, len(rig.data.bones), "bones")
def_bones = {b.name: b for b in rig.data.bones if b.name.startswith("DEF-")}
print("DEF", sorted(def_bones))

# ---- 4) skinning -----------------------------------------------------------------------------------
SPINE = ["DEF-spine", "DEF-spine.001", "DEF-spine.002", "DEF-spine.003"]


def centroid(o):
    pts = [o.matrix_world @ v.co for v in o.data.vertices]
    return sum(pts, Vector()) / max(1, len(pts))


def dist_to_segment(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
    return (p - (a + ab * t)).length


def rest_segment(bone_name):
    b = def_bones[bone_name]
    return rig.matrix_world @ b.head_local, rig.matrix_world @ b.tail_local


def set_weights(o, weights_by_bone):
    """weights_by_bone : bone name -> list of weights per vertex."""
    for bone_name, ws in weights_by_bone.items():
        vg = o.vertex_groups.new(name=bone_name)
        for i, w in enumerate(ws):
            if w > 1e-4:
                vg.add([i], w, "REPLACE")
    mod = o.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    o.parent = rig


def smoothstep(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


rigid = blended = 0
for o in parts:
    prefix = o.name.split("__")[0]
    n = len(o.data.vertices)
    if prefix == "Head":
        bone = "DEF-spine.006"
    elif prefix == "Cape":
        bone = "DEF-spine.003"
    elif prefix == "Root":
        bone = "DEF-spine"
    elif prefix == "Torso":
        z = centroid(o).z
        bone = SPINE[0] if z < 1.16 else SPINE[1] if z < 1.36 else SPINE[2] if z < 1.58 else SPINE[3]
    elif prefix == "Weapon":
        bone = "DEF-hand.R"
    elif prefix in ("ArmR", "ArmL"):
        side = prefix[-1]
        up, fo, ha = f"DEF-upper_arm.{side}", f"DEF-forearm.{side}", f"DEF-hand.{side}"
        cands = [up, f"{up}.001", fo, f"{fo}.001", ha]
        c = centroid(o)
        if "RobeLight" in o.name and n > 40:
            # sleeve : smooth blend between upper arm and forearm across the elbow (bisector plane)
            (ua, ub), (fa, fb) = rest_segment(up + ".001") if False else rest_segment(up), rest_segment(fo)
            elbow = ub
            nrm = ((ub - ua).normalized() + (fb - fa).normalized()).normalized()
            wf = []
            for v in o.data.vertices:
                s = (o.matrix_world @ v.co - elbow).dot(nrm)
                wf.append(smoothstep((s + 0.14) / 0.28))
            set_weights(o, {up: [1 - w for w in wf], fo: wf})
            blended += 1
            continue
        bone = min(cands, key=lambda bn: dist_to_segment(c, *rest_segment(bn)))
    else:
        bone = "DEF-spine.003"
    set_weights(o, {bone: [1.0] * n})
    rigid += 1
print("SKIN", rigid, "rigid parts,", blended, "blended sleeves")

# hide the metarig, keep the rig
meta.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "druid_rigged.blend"))
print("SAVED", os.path.join(OUT, "druid_rigged.blend"))
