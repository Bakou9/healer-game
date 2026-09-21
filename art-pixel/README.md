# art-pixel : 3D-to-pixel-art sprite pipeline (Dead Cells method)

Experiment started 2026-09-21 (decision D-071). The 3D models stay the source of truth (`art-3d/`); this folder turns
them into pixel-art sprite sheets for a side-view battle like Final Fantasy VI (party on the right facing the boss on the left).

## Run

```
blender --background --python art-pixel/blender/sprites.py -- <size> <colors> <yaw> <frame> <side>
```

| arg | meaning | tested |
|---|---|---|
| size | square frame in pixels | 128 |
| colors | shared palette size (k-means) | 28 |
| yaw | camera orbit toward the front, degrees (0 = pure side view) | 30 |
| frame | orthographic height covered, meters | 4.3 |
| side | -1 camera on the staff side (faces right), +1 free-hand side (faces left) | +1 |

Outputs in `art-pixel/out/`: one PNG per pose, a sprite sheet, and a x3 preview on the game background.

## Pipeline

1. Parent the meshes to their pivot empties (same pivots as the game rig).
2. Pose the pivots (same angles as `UnitRig.ApplyPose`).
3. Render with a transparent film at 4x the target size (orthographic camera).
4. Alpha-aware box downscale.
5. One shared palette per character (k-means, fixed seed), applied to every frame.
6. 1 px dark outline.

Deterministic: same inputs give the same sprites.

## Findings of the first test

- Pure side view hides the face and the free hand behind the staff: use a three-quarter view (yaw about 30) from the free-hand side.
- 128 px frames keep the skull mask, the antlers and the crystal readable; the palette of 28 colors is enough.
- The palette and outline steps are the main look levers (selective outline, hue-shifted shadows and per-hero palettes are next).
- Not done yet: Unity integration (sprite renderer, pixel-perfect camera), full animation sets (attack, hit, death), other heroes and bosses, equipment variants (re-render in batch).
