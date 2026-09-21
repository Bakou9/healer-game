"""Rig the Archere with Rigify (same method as rig_druid.py) : the loose parts of the 3D model are skinned to the
generated deform bones.

Run :  blender --background --python art-pixel/blender/rig_archer.py
In   : art-3d/blender/out/archer.blend
Out  : art-pixel/out/archer_rigged.blend (the Rigify rig `rig` with the model skinned to it ; not committed, regenerate with this script)

Steps : 1) split every mesh into loose parts (fingers, arrows, straps...), 2) add Rigify's human metarig and fit the main
bones to the archer (spine, head, arms in their drawn pose, real legs), 3) generate the rig, 4) skin :
  - rigid parts go to one deform bone, chosen by pivot prefix then by distance to the bone segment ;
  - sleeves are blended across the elbow (smooth per-vertex weights) so the arm bends cleanly ;
  - the BOW follows the bow hand, the ARROW follows the drawing hand ;
  - the BOWSTRING is blended between the two hands along its own geometry, so it stretches into a V when the
    drawing hand pulls back. The two tips and the nocking point are found from the mesh itself (no constant copied
    from the model script), which makes the trick reusable for any bow.
Unlike rig_druid.py, the pose points are not copied here : archer.py stores them in the .blend (scene["archer_pose"]).
"""
import math
import os

import addon_utils
import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
BLEND = os.path.join(ROOT, "art-3d", "blender", "out", "archer.blend")
OUT = os.path.join(ROOT, "art-pixel", "out")
os.makedirs(OUT, exist_ok=True)

addon_utils.enable("rigify", default_set=True, persistent=False)
bpy.ops.wm.open_mainfile(filepath=BLEND)
scene = bpy.context.scene
P = {name: Vector(value) for name, value in scene["archer_pose"].items()}


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

# ---- 2) metarig, fitted to the archer ------------------------------------------------------------
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


spine_z = [0.98, 1.12, 1.30, 1.50, 1.70, 1.82, 1.92, 2.40]
for i, name in enumerate(["spine", "spine.001", "spine.002", "spine.003", "spine.004", "spine.005", "spine.006"]):
    fit(name, (0, 0, spine_z[i]), (0, 0, spine_z[i + 1]))
# arms in their drawn pose (shoulder, elbow, wrist, hand end)
hand_l = P["GRIP"] + (P["GRIP"] - P["WL"]).normalized() * 0.06
hand_r = P["NOCK"] + (P["NOCK"] - P["WR"]).normalized() * 0.05
for side, (s, e, w, h) in {"L": (P["SL"], P["EL"], P["WL"], hand_l), "R": (P["SR"], P["ER"], P["WR"], hand_r)}.items():
    fit(f"shoulder.{side}", (0.03 if side == "L" else -0.03, -0.05 if side == "L" else 0.05, 1.82), s)
    fit(f"upper_arm.{side}", s, e)
    fit(f"forearm.{side}", e, w)
    fit(f"hand.{side}", w, h)
# real legs this time (the druid's were hidden under a robe)
for side in ("L", "R"):
    hip, knee, ankle, toe = P[f"HIP_{side}"], P[f"KNEE_{side}"], P[f"ANK_{side}"], P[f"TOE_{side}"]
    fit(f"thigh.{side}", hip, knee)
    fit(f"shin.{side}", knee, ankle)
    fit(f"foot.{side}", ankle, toe)
    fit(f"toe.{side}", toe, toe + Vector((0, -0.09, 0)))
bpy.ops.object.mode_set(mode="OBJECT")

# ---- 3) generate the Rigify rig ------------------------------------------------------------------
select_only(meta)
bpy.ops.pose.rigify_generate()
rig = next(o for o in scene.objects if o.type == "ARMATURE" and o.name != meta.name)
def_bones = {b.name: b for b in rig.data.bones if b.name.startswith("DEF-")}
print("RIG", rig.name, len(rig.data.bones), "bones,", len(def_bones), "deform bones")

# ---- 4) skinning -----------------------------------------------------------------------------------
SPINE = ["DEF-spine", "DEF-spine.001", "DEF-spine.002", "DEF-spine.003"]
LEGS = [n for n in def_bones if n.split(".")[0] in ("DEF-thigh", "DEF-shin", "DEF-foot", "DEF-toe")] + ["DEF-spine"]


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


def nearest(cands, point):
    return min([c for c in cands if c in def_bones], key=lambda bn: dist_to_segment(point, *rest_segment(bn)))


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


def string_weights(o):
    """Bowstring : find its two tips and its nocking point from the mesh, then blend each vertex linearly between
    the bow hand (tips) and the drawing hand (nock) so that the string stretches into a V during the draw."""
    pts = [o.matrix_world @ v.co for v in o.data.vertices]
    c = sum(pts, Vector()) / len(pts)
    a = max(pts, key=lambda p: (p - c).length)
    b = max(pts, key=lambda p: (p - a).length)
    nock = max(pts, key=lambda p: dist_to_segment(p, a, b))
    draw = []
    for p in pts:
        tip = a if (p - a).length < (p - b).length else b
        seg = nock - tip
        t = max(0.0, min(1.0, (p - tip).dot(seg) / max(seg.length_squared, 1e-9)))
        draw.append(t)
    print("STRING tips", [round(v, 2) for v in a], [round(v, 2) for v in b], "nock", [round(v, 2) for v in nock])
    return {"DEF-hand.R": draw, "DEF-hand.L": [1 - t for t in draw]}


rigid = blended = 0
for o in parts:
    prefix = o.name.split("__")[0]
    n = len(o.data.vertices)
    c = centroid(o)
    if prefix == "Head":
        bone = "DEF-spine.006"
    elif prefix == "Cape":
        bone = "DEF-spine.003"
    elif prefix == "Root":
        # hips, legs and boots : the nearest leg bone, the belt area falls back on the lowest spine bone
        bone = nearest(LEGS, c)
    elif prefix == "Torso":
        z = c.z
        bone = SPINE[0] if z < 1.22 else SPINE[1] if z < 1.42 else SPINE[2] if z < 1.62 else SPINE[3]
    elif prefix == "Weapon":
        if "Braced" in o.name:
            bone = "DEF-hand.L"                      # released string : rigid on the bow
        elif "String" in o.name:
            set_weights(o, string_weights(o))
            blended += 1
            continue
        else:
            bone = "DEF-hand.R" if "Arrow" in o.name else "DEF-hand.L"
    elif prefix in ("ArmR", "ArmL"):
        side = prefix[-1]
        up, fo, ha = f"DEF-upper_arm.{side}", f"DEF-forearm.{side}", f"DEF-hand.{side}"
        if "__Cloth" in o.name:
            # sleeve and elbow : smooth blend between upper arm and forearm across the elbow (bisector plane)
            (ua, ub), (fa, fb) = rest_segment(up), rest_segment(fo)
            nrm = ((ub - ua).normalized() + (fb - fa).normalized()).normalized()
            wf = [smoothstep(((o.matrix_world @ v.co - ub).dot(nrm) + 0.10) / 0.20) for v in o.data.vertices]
            set_weights(o, {up: [1 - w for w in wf], fo: wf})
            blended += 1
            continue
        bone = nearest([up, f"{up}.001", fo, f"{fo}.001", ha], c)
    else:
        bone = "DEF-spine.003"
    set_weights(o, {bone: [1.0] * n})
    rigid += 1
print("SKIN", rigid, "rigid parts,", blended, "blended parts")

meta.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "archer_rigged.blend"))
print("SAVED", os.path.join(OUT, "archer_rigged.blend"))
