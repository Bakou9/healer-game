"""Outils communs des scripts Blender du projet (exécutés en ligne de commande : blender --background --python <script>).

Conventions (voir docs/ART_3D.md) :
- unités = mètres, Z vers le haut, le personnage regarde vers -Y (de face = vue depuis -Y), pieds à z = 0 ;
- chaque maillage s'appelle « <Pivot>__<Pièce> » (Pivot = Root, Torso, Head, ArmL, ArmR, Cape, Weapon) ;
  une pièce dont le nom contient « Glow » est lumineuse (Unity la remplace par un matériau sans éclairage) ;
- un objet vide « Pivot_<Pivot> » marque l'articulation de chaque pivot (le jeu s'en sert pour animer le personnage).
"""
import math
import random
import bpy
import bmesh
from mathutils import Vector, Matrix, Euler


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for coll in list(bpy.data.collections):
        bpy.data.collections.remove(coll)


_materials = {}


def material(name, color, metallic=0.0, roughness=0.8, glow=0.0):
    """Matériau simple : couleur de base (hex sans #) ; glow > 0 = émissif (couleur de base = couleur de lueur)."""
    key = (name, color, metallic, roughness, glow)
    if key in _materials:
        return _materials[key]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    r, g, b = (int(color[i:i + 2], 16) / 255.0 for i in (0, 2, 4))
    lin = tuple(c ** 2.2 for c in (r, g, b))
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*lin, 1)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if glow > 0:
        bsdf.inputs["Emission Color"].default_value = (*lin, 1)
        bsdf.inputs["Emission Strength"].default_value = glow
    m.diffuse_color = (*lin, 1)
    _materials[key] = m
    return m


def finish(obj, mat, name, flat=True, bevel=0.0):
    """Nomme l'objet, applique les transformations, lui donne son matériau et l'ombrage à facettes."""
    obj.name = name
    obj.data.name = name
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    if bevel > 0:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        mod.limit_method = "ANGLE"
        bpy.ops.object.modifier_apply(modifier="Bevel")
    if flat:
        for p in obj.data.polygons:
            p.use_smooth = False
    obj.select_set(False)
    return obj


def cone(name, mat, r1, r2, depth, loc, verts=8, rot=(0, 0, 0), scale=(1, 1, 1), bevel=0.0):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.scale = scale
    return finish(o, mat, name, bevel=bevel)


def sphere(name, mat, radius, loc, scale=(1, 1, 1), segs=8, rings=6, rot=(0, 0, 0), bevel=0.0):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings, radius=radius, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.scale = scale
    return finish(o, mat, name, bevel=bevel)


def box(name, mat, size, loc, rot=(0, 0, 0), bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.scale = size
    return finish(o, mat, name, bevel=bevel)


def crystal(name, mat, radius, height, loc, sides=6, rot=(0, 0, 0), waist=0.5):
    """Double pyramide (cristal) de hauteur `height`."""
    bm = bmesh.new()
    top = bm.verts.new((0, 0, height * (1 - waist)))
    bottom = bm.verts.new((0, 0, -height * waist))
    ring = [bm.verts.new((radius * math.cos(2 * math.pi * i / sides), radius * math.sin(2 * math.pi * i / sides), 0)) for i in range(sides)]
    for i in range(sides):
        a, b = ring[i], ring[(i + 1) % sides]
        bm.faces.new((top, a, b))
        bm.faces.new((bottom, b, a))
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    o = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(o)
    o.location = loc
    o.rotation_euler = rot
    bpy.context.view_layer.objects.active = o
    return finish(o, mat, name)


def leaf(name, mat, length, width, loc, rot=(0, 0, 0), thickness=0.012):
    """Feuille : losange allongé, légèrement épais et courbé (pointe vers -Z local puis tournée par `rot`)."""
    bm = bmesh.new()
    tip = bm.verts.new((0, 0, -length))
    base = bm.verts.new((0, 0, 0))
    l = bm.verts.new((-width, 0, -length * 0.38))
    r = bm.verts.new((width, 0, -length * 0.38))
    f = bm.verts.new((0, thickness, -length * 0.35))
    b = bm.verts.new((0, -thickness, -length * 0.4))
    for tri in ((tip, l, f), (tip, f, r), (base, f, l), (base, r, f), (tip, b, l), (tip, r, b), (base, l, b), (base, b, r)):
        bm.faces.new(tri)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    o = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(o)
    o.location = loc
    o.rotation_euler = rot
    bpy.context.view_layer.objects.active = o
    return finish(o, mat, name)


def tube(name, mat, points, radii, sides=6, closed=False):
    """Tube courbe passant par `points` (liste de (x, y, z)), de rayon variable (`radii` : un par point). Converti en maillage à facettes."""
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = 1.0
    curve.bevel_resolution = max(0, sides // 2 - 2)
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for i, (p, r) in enumerate(zip(points, radii)):
        spline.points[i].co = (*p, 1)
        spline.points[i].radius = r
    spline.use_cyclic_u = closed
    o = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(o)
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    bpy.ops.object.convert(target="MESH")
    o = bpy.context.active_object
    return finish(o, mat, name)


def join(objs, name, mat=None):
    """Fusionne des objets en un seul maillage nommé `name`."""
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    o.data.name = name
    if mat is not None:
        o.data.materials.clear()
        o.data.materials.append(mat)
    o.select_set(False)
    return o


def pivot(name, loc):
    bpy.ops.object.empty_add(type="PLAIN_AXES", location=loc)
    e = bpy.context.active_object
    e.name = "Pivot_" + name
    e.empty_display_size = 0.1
    return e


def triangle_count():
    bpy.context.view_layer.update()
    total = 0
    dg = bpy.context.evaluated_depsgraph_get()
    for o in bpy.context.scene.objects:
        if o.type == "MESH":
            me = o.evaluated_get(dg).to_mesh()
            total += sum(len(p.vertices) - 2 for p in me.polygons)
            o.evaluated_get(dg).to_mesh_clear()
    return total


def setup_render(width=640, height=900):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.render.resolution_x = width
    sc.render.resolution_y = height
    sc.render.image_settings.file_format = "PNG"
    world = bpy.data.worlds.new("W")
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.02, 0.025, 0.05, 1)
    bg.inputs["Strength"].default_value = 1.0
    sc.world = world
    # éclairage « lune froide + contre-jour violet + lueur chaude », comme le jeu
    def light(name, kind, loc, energy, color, rot=(0, 0, 0)):
        data = bpy.data.lights.new(name, kind)
        data.energy = energy
        data.color = color
        o = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(o)
        o.location = loc
        o.rotation_euler = rot
    light("Moon", "SUN", (0, 0, 5), 3.0, (0.66, 0.74, 1.0), (math.radians(50), 0, math.radians(-35)))
    light("Rim", "SUN", (0, 0, 5), 2.0, (0.58, 0.36, 0.95), (math.radians(60), 0, math.radians(150)))
    light("Warm", "POINT", (-2.0, -2.0, 1.2), 300, (1.0, 0.6, 0.3))


def render_views(path_prefix, views=(("front", 0), ("three_quarter", 40), ("side", 90), ("back", 180)), center=(0, 0, 1.25), distance=7.5, lens=70):
    sc = bpy.context.scene
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = lens
    cam = bpy.data.objects.new("Cam", cam_data)
    bpy.context.collection.objects.link(cam)
    sc.camera = cam
    target = Vector(center)
    for name, angle in views:
        a = math.radians(angle)
        cam.location = target + Vector((math.sin(a) * distance, -math.cos(a) * distance, 0.3))
        direction = target - cam.location
        cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
        sc.render.filepath = f"{path_prefix}_{name}.png"
        bpy.ops.render.render(write_still=True)


def export_fbx(path):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        path_mode="AUTO",
    )
