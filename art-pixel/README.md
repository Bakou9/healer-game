# art-pixel : 3D-to-pixel-art sprite pipeline (Dead Cells method)

Experiment started 2026-09-21 (decision D-071). The 3D models stay the source of truth (`art-3d/`); this folder turns
them into pixel-art sprite sheets for a side-view battle like Final Fantasy VI (party on the right facing the boss on the left).

## Run (Rigify animation pipeline, D-073)

```
blender --background --python art-pixel/blender/rig_druid.py                       # once : rig + skinning -> art-pixel/out/druid_rigged.blend (not committed)
blender --background --python art-pixel/blender/animate_druid.py -- 128 28 30 --unity   # size colors yaw ; --unity copies the sheets into the game
```

Outputs in `art-pixel/out/`: one sprite sheet per animation (`druid_<anim>.png`, frames side by side), `druid_anims.txt`, `druid_contact.png`, and ffmpeg previews `druid_preview.gif` / `.mp4`.

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

## In-game test (Unity)

`lancer-le-jeu-sprites.bat` (or `HealerGame.exe -healer-sprites`) shows the Healer as the pixel-art druid: `SpriteHero` swaps the 3D model for a camera-facing quad
and picks the frame from the core `UnitPose` (idle, cast raise / hold / release, hit, fall). The other heroes and the boss stay 3D, so the mix is only a rendering test.
Copy new frames with: `art-pixel/out/druid_128_y30l_<pose>.png` -> `unity/HealerGame/Assets/Resources/Sprites/Druid/druid_<pose>.png`
(the importer in `Assets/Editor/SpriteImport.cs` sets point filtering and no compression).
