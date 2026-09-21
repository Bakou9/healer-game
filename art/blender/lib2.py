"""Outils de modélisation « haute définition » (surfaces lissées) pour les héros faits sous Blender (D-069).

Complète lib.py : balayage de tubes à section variable (doigts, branches, bois), surfaces par anneaux (robe, capuche, manches),
feuilles incurvées à deux faces, blobs lissés. Tout est en maillage lissé (normales exportées telles quelles vers Unity).
Mêmes conventions que lib.py (mètres, Z haut, face vers -Y, pièces « <Pivot>__<Pièce> »).
"""
import math
import random

import bmesh
import bpy
from mathutils import Vector

import lib


def _link(name, bm, mat, smooth=True, subsurf=0, solidify=0.0):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    o = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(o)
    o.data.materials.append(mat)
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    if solidify:
        m = o.modifiers.new("Solid", "SOLIDIFY")
        m.thickness = solidify
        m.offset = 0
        bpy.ops.object.modifier_apply(modifier="Solid")
    if subsurf:
        m = o.modifiers.new("Sub", "SUBSURF")
        m.levels = subsurf
        m.render_levels = subsurf
        bpy.ops.object.modifier_apply(modifier="Sub")
    for p in o.data.polygons:
        p.use_smooth = smooth
    o.select_set(False)
    return o


def spline(points, radii=None, per=4):
    """Lisse un chemin par Catmull-Rom : renvoie (points, rayons) plus denses (`per` points par segment)."""
    P = [Vector(p) for p in points]
    R = list(radii) if radii is not None else None
    out, rout = [], []
    n = len(P)
    for i in range(n - 1):
        p0, p1, p2, p3 = P[max(0, i - 1)], P[i], P[i + 1], P[min(n - 1, i + 2)]
        for s in range(per):
            t = s / per
            q = 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3)
            out.append(q)
            if R is not None:
                rout.append(R[i] + (R[i + 1] - R[i]) * t)
    out.append(P[-1])
    if R is not None:
        rout.append(R[-1])
    return out, rout


def surface(name, mat, rings, closed_top=True, closed_bottom=False, smooth=True, subsurf=0, solidify=0.0, flip=False, skip=None, wrap=True):
    """Surface par anneaux : `rings` = liste d'anneaux (listes de points de même longueur), du bas vers le haut.
    `skip(k, i)` = True retire la face (anneau k, colonne i) ; `wrap=False` n'referme pas la surface autour (bande ouverte)."""
    bm = bmesh.new()
    vr = [[bm.verts.new(p) for p in ring] for ring in rings]
    n = len(vr[0])
    for k in range(len(vr) - 1):
        for i in range(n if wrap else n - 1):
            if skip and skip(k, i):
                continue
            j = (i + 1) % n
            face = (vr[k][i], vr[k][j], vr[k + 1][j], vr[k + 1][i])
            bm.faces.new(face[::-1] if flip else face)
    if closed_top:
        bm.faces.new(vr[-1] if flip else vr[-1][::-1])
    if closed_bottom:
        bm.faces.new(vr[0][::-1] if flip else vr[0])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _link(name, bm, mat, smooth, subsurf, solidify)


def ring(center, rx, ry, z, n=24, fold=0, fold_depth=0.0, phase=0.0, noise=0.0, rng=None, tilt=(0.0, 0.0)):
    """Anneau elliptique autour de `center` (x, y) à la hauteur z ; plis verticaux `fold` de profondeur relative `fold_depth`."""
    pts = []
    for i in range(n):
        a = 2 * math.pi * i / n
        f = 1.0 + (fold_depth * math.cos(fold * a + phase) if fold else 0.0)
        if noise and rng:
            f *= 1.0 + rng.uniform(-noise, noise)
        pts.append((center[0] + rx * f * math.cos(a) + tilt[0] * (z - 1.0), center[1] + ry * f * math.sin(a) + tilt[1] * (z - 1.0), z))
    return pts


def sweep(name, mat, points, radii, sides=8, cap="round", aspect=1.0, smooth=True, subsurf=0, twist=0.0):
    """Tube lissé le long d'un chemin (rayon variable, bouts arrondis). `aspect` aplatit la section ; `twist` (rad) la fait tourner."""
    pts = [Vector(p) for p in points]
    n = len(pts)
    tang = []
    for i in range(n):
        a = pts[max(0, i - 1)]
        b = pts[min(n - 1, i + 1)]
        tang.append((b - a).normalized())
    ref = Vector((0, 0, 1)) if abs(tang[0].z) < 0.9 else Vector((1, 0, 0))
    nrm = (ref - tang[0] * ref.dot(tang[0])).normalized()
    bm = bmesh.new()
    rings_v = []
    for i in range(n):
        if i:
            nrm = (nrm - tang[i] * nrm.dot(tang[i]))
            nrm = nrm.normalized() if nrm.length > 1e-6 else nrm
        bino = tang[i].cross(nrm).normalized()
        r = radii[i] if isinstance(radii, (list, tuple)) else radii
        row = []
        for s in range(sides):
            a = 2 * math.pi * s / sides + twist * i
            row.append(bm.verts.new(pts[i] + nrm * (math.cos(a) * r) + bino * (math.sin(a) * r * aspect)))
        rings_v.append(row)
    for i in range(n - 1):
        for s in range(sides):
            t = (s + 1) % sides
            bm.faces.new((rings_v[i][s], rings_v[i][t], rings_v[i + 1][t], rings_v[i + 1][s]))
    for end, direction in ((0, -1), (n - 1, 1)):
        r = radii[end] if isinstance(radii, (list, tuple)) else radii
        if cap == "round":
            tip = bm.verts.new(pts[end] + tang[end] * (direction * r * 0.9))
            row = rings_v[end]
            for s in range(sides):
                t = (s + 1) % sides
                bm.faces.new((row[t], row[s], tip) if direction > 0 else (row[s], row[t], tip))
        else:
            bm.faces.new(rings_v[end][::-1] if direction > 0 else rings_v[end])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _link(name, bm, mat, smooth, subsurf)


def blob(name, mat, radius, loc, scale=(1, 1, 1), segs=16, rings=10, rot=(0, 0, 0), subsurf=0, smooth=True):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=radius)
    for v in bm.verts:
        v.co = Vector((v.co.x * scale[0], v.co.y * scale[1], v.co.z * scale[2]))
    o = _link(name, bm, mat, smooth, subsurf)
    o.rotation_euler = rot
    o.location = loc
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    o.select_set(False)
    return o


def rbox(name, mat, size, loc, rot=(0, 0, 0), bevel=0.01, subsurf=1, smooth=True):
    """Boîte à angles arrondis (subdivisée) : sacoches, paumes, sangles."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2]))
    o = _link(name, bm, mat, smooth, 0)
    o.rotation_euler = rot
    o.location = loc
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if bevel:
        m = o.modifiers.new("Bevel", "BEVEL")
        m.width = bevel
        m.segments = 3
        m.limit_method = "ANGLE"
        bpy.ops.object.modifier_apply(modifier="Bevel")
    for p in o.data.polygons:
        p.use_smooth = smooth
    o.select_set(False)
    return o


def leaf(name, mat, length, width, pos, direction, curl=0.25, droop=0.25, thick=0.006, across=4, along=6, rng=None):
    """Feuille incurvée à deux faces avec nervure centrale. `direction` = où pointe la pointe ; `pos` = attache."""
    bm = bmesh.new()
    grid = []
    for j in range(along + 1):
        v = j / along
        w = width * math.sin(math.pi * min(1.0, v ** 0.75)) * (1.0 if v < 1 else 0.02)
        row = []
        for i in range(across + 1):
            u = (i / across) * 2 - 1
            x = u * w
            y = -curl * (u * u) * width * (1.0 - v * 0.3) - droop * (v ** 2) * length * 0.35
            row.append(bm.verts.new((x, y, -length * v)))
        grid.append(row)
    for j in range(along):
        for i in range(across):
            bm.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    o = _link(name, bm, mat, True, 0, thick)
    rot = Vector(direction).normalized().to_track_quat("-Z", "Y").to_euler()
    if rng:
        rot.z += rng.uniform(-0.3, 0.3)
    o.rotation_euler = rot
    o.location = pos
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    o.select_set(False)
    return o
