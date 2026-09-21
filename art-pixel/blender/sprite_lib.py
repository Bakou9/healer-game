"""Shared sprite-pipeline helpers (D-071, D-073) : orthographic camera, lighting, transparent render, pixelization, shared palette,
PNG writing, ffmpeg previews. Used by animate_druid.py (Blender-side, numpy only : no other Python dependency).
"""
import math
import os
import shutil
import struct
import subprocess
import zlib

import bpy
import numpy as np
from mathutils import Vector

OVERSAMPLE = 4
OUTLINE = np.array([0.02, 0.025, 0.04])
BACKGROUND8 = (26, 24, 40, 255)


def setup_camera(scene, size, yaw=30.0, side=1, frame=4.3, center_z=1.95):
    """Orthographic camera. yaw : orbit toward the front (deg) ; side +1 : free-hand side, character faces left ; -1 : staff side, faces right."""
    cam_data = bpy.data.cameras.new("SpriteCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = frame
    cam = bpy.data.objects.new("SpriteCam", cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = Vector((side * 12.0 * math.cos(math.radians(yaw)), -12.0 * math.sin(math.radians(yaw)), center_z))
    cam.rotation_euler = (math.radians(90), 0, math.radians(-90 + yaw if side < 0 else 90 - yaw))
    scene.camera = cam
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.film_transparent = True
    scene.render.resolution_x = scene.render.resolution_y = size * OVERSAMPLE
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

    def light(name, energy, color, rot):
        data = bpy.data.lights.new(name, "SUN")
        data.energy = energy
        data.color = color
        o = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(o)
        o.location = (0, 0, 5)
        o.rotation_euler = rot

    # readable sprites : warm key from the front-top, cool fill, purple rim from behind
    light("Key", 3.6, (1.0, 0.95, 0.88), (math.radians(55), 0, math.radians(-70)))
    light("Fill", 1.5, (0.62, 0.72, 1.0), (math.radians(70), 0, math.radians(110)))
    light("Rim", 2.4, (0.65, 0.4, 1.0), (math.radians(60), 0, math.radians(-160)))


def render_raw(scene, size, tmp_path):
    """Render the current pose at OVERSAMPLE x size ; returns a float array (H, W, 4), raw PNG values, top row first."""
    scene.render.filepath = tmp_path
    bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(tmp_path)
    img.colorspace_settings.name = "Non-Color"        # raw values : no color-space conversion when reading
    n = size * OVERSAMPLE
    a = np.array(img.pixels[:], dtype=np.float32).reshape(n, n, 4)[::-1]
    bpy.data.images.remove(img)
    os.remove(tmp_path)
    return a


def downscale(a):
    """Alpha-aware box downscale by OVERSAMPLE : returns (rgb, opaque mask)."""
    n = a.shape[0]
    f = OVERSAMPLE
    pre = a.copy()
    pre[..., :3] *= pre[..., 3:4]
    small = pre.reshape(n // f, f, n // f, f, 4).mean(axis=(1, 3))
    alpha = small[..., 3]
    rgb = small[..., :3] / np.maximum(alpha[..., None], 1e-6)
    return rgb, alpha >= 0.5


def build_palette(frames, colors, seed=3):
    """One shared palette for every frame of every animation (k-means with a fixed seed)."""
    samples = np.concatenate([rgb[mask] for rgb, mask in frames])
    rng = np.random.default_rng(seed)
    centers = samples[rng.choice(len(samples), colors, replace=False)]
    for _ in range(12):
        d = ((samples[:, None, :] - centers[None, :, :]) ** 2).sum(axis=2)
        lab = d.argmin(axis=1)
        for k in range(colors):
            pts = samples[lab == k]
            if len(pts):
                centers[k] = pts.mean(axis=0)
    return centers


def quantize(rgb, mask, centers):
    """Map to the palette and add a 1 px dark outline around the silhouette."""
    d = ((rgb[..., None, :] - centers[None, None, :, :]) ** 2).sum(axis=3)
    out = np.zeros(rgb.shape[:2] + (4,), dtype=np.float32)
    out[..., :3] = centers[d.argmin(axis=2)]
    out[..., 3] = mask
    grown = mask.copy()
    grown[1:, :] |= mask[:-1, :]
    grown[:-1, :] |= mask[1:, :]
    grown[:, 1:] |= mask[:, :-1]
    grown[:, :-1] |= mask[:, 1:]
    edge = grown & ~mask
    out[edge, :3] = OUTLINE
    out[edge, 3] = 1.0
    return (np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8)


def write_png(path, arr8):
    h, w = arr8.shape[:2]
    rows = b"".join(b"\x00" + arr8[y].tobytes() for y in range(h))

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(rows, 9)) + chunk(b"IEND", b""))


def on_background(sprite8, scale=3):
    """Sprite (RGBA) scaled with nearest neighbour and composited on the game background."""
    big = np.repeat(np.repeat(sprite8, scale, axis=0), scale, axis=1)
    canvas = np.tile(np.array(BACKGROUND8, dtype=np.uint8), (big.shape[0], big.shape[1], 1))
    a = big[..., 3:4].astype(np.float32) / 255
    canvas[..., :3] = (big[..., :3] * a + canvas[..., :3] * (1 - a)).astype(np.uint8)
    return canvas


def find_ffmpeg():
    exe = shutil.which("ffmpeg")
    if exe:
        return exe
    import glob
    root = os.path.expandvars(r"%LOCALAPPDATA%\Microsoft\WinGet")
    for pattern in (os.path.join(root, "Links", "ffmpeg.exe"), os.path.join(root, "Packages", "Gyan.FFmpeg*", "**", "bin", "ffmpeg.exe")):
        found = glob.glob(pattern, recursive=True)
        if found:
            return found[0]
    return None


def encode_previews(frames_dir, pattern, fps, out_base):
    """GIF + MP4 from a PNG sequence with ffmpeg (nearest-neighbour, small : a few seconds of CPU). Returns the written paths."""
    ffmpeg = find_ffmpeg()
    if not ffmpeg:
        print("FFMPEG not found : previews skipped")
        return []
    written = []
    gif = out_base + ".gif"
    subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-framerate", str(fps), "-i", os.path.join(frames_dir, pattern),
                    "-vf", "split[a][b];[a]palettegen=max_colors=64[p];[b][p]paletteuse=dither=none", gif], check=True)
    written.append(gif)
    mp4 = out_base + ".mp4"
    subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-framerate", str(fps), "-i", os.path.join(frames_dir, pattern),
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-vf", "scale=trunc(iw/2)*2:trunc(ih/2)*2", mp4], check=True)
    written.append(mp4)
    return written
