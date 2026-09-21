"""Test de chaîne : un cristal, un rendu, une exportation. Lancer : blender --background --python art-3d/blender/smoke_test.py"""
import os
import sys
sys.path.insert(0, os.path.dirname(__file__))
import lib

lib.reset()
lib.setup_render(320, 320)
m = lib.material("Glow", "7CFFB2", glow=4.0)
lib.crystal("Head__Glow_Test", m, 0.3, 0.8, (0, 0, 1.0))
lib.pivot("Head", (0, 0, 1.0))
out = os.path.join(os.path.dirname(__file__), "out")
os.makedirs(out, exist_ok=True)
lib.render_views(os.path.join(out, "smoke"), views=(("front", 0),), center=(0, 0, 1.0), distance=4, lens=50)
lib.export_fbx(os.path.join(out, "smoke.fbx"))
print("TRIANGLES", lib.triangle_count())
