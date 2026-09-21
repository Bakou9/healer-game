# art-2d : illustrated 2D characters (one key illustration per unit + procedural animation)

Direction started 2026-09-21 (decision D-074) after the 3D-to-sprite route proved too expensive per hero. Idea : one high-quality illustration per
hero and boss (also used for gacha portraits later), a few optional extra poses, and animation done in Unity from the core `UnitPose`
(breathing, lunge, recoil, cast glow, fall). No modeling, no rigging, no frame-by-frame drawing.

## Workflow (about 1 hour for the first hero, less afterwards)

1. Generate the image with an image tool (see below) using the prompts of this file. Same style block for every unit.
2. Save the PNG in `art-2d/inbox/<unit>_idle.png` (and optionally `<unit>_attack.png`, `<unit>_cast.png`).
3. Ask Claude to process them : background key-out (flat magenta), crop, normalize height, color-match, import into Unity, credits entry.
4. Judge it in the battle scene (`HealerGame.exe -healer-art2d`), then iterate on the prompt if needed.

## Tools (check the current free limits and the commercial-use terms of the tool YOU use, they change often)

- Try a **free tier first** (ChatGPT image generation, Google Gemini, Microsoft Copilot / Designer). Cost 0, learning time about 15 minutes.
- If consistency across heroes is not good enough, a paid plan (Midjourney at about 10 EUR / month) is the usual next step : it keeps a style with a
  reference image (`--sref`) and a character with `--cref`. No tool can guarantee a result ; the honest test is the first hero.
- Keep the first accepted illustration and attach it as a **style reference** for every other unit ("same art style as the attached image").

## Legal notes (please read once)

- **Steam** and **Google Play** require you to disclose AI-generated content ; keep a list of which tool and which prompt made which asset.
- In many countries purely AI-generated images may **not be protected by copyright** : someone could copy the art. Matters most for the gacha
  portraits ; a paid artist can later redo the key illustrations on top of these as references.
- Each tool has its own terms for commercial use. Save them (screenshot or PDF) when you subscribe.
- Add every tool to the credits screen (`core/content/credits.json`) ; Claude does it during step 3.

## Style block (copy at the start of every prompt)

> Dark fantasy game character illustration, painterly digital art, clean readable silhouette, limited moody palette with one glowing accent color,
> strong rim light from behind, full body, standing idle pose, three-quarter view facing RIGHT, centered, isolated on a perfectly flat solid
> magenta (#FF00FF) background, no ground, no cast shadow, no text, no border, no watermark. Consistent proportions : slightly stylized,
> about 5 heads tall, large readable hands and weapon.

## Unit prompts (add after the style block)

**Healer / Druid (id healer)** : a druid healer wearing a pale deer-skull mask under a deep hood, tall branching antlers with small glowing green
blossoms and vines, mossy dark green layered robe, mantle of leaves, leather bandolier, gnarled wooden staff held in one hand topped with a cage
of branches around a glowing green crystal (7CFFB2), the other hand open palm up holding a small floating green light. Green and dark wood palette.

**Guard (id tank)** : a heavy knight in dark steel plate, great helm with a single glowing cyan visor slit, large tower shield with a glowing cyan
diamond emblem, short broad sword in the other hand, tattered dark blue cloth at the belt. Steel blue and black palette, cyan accent.

**Archer (id dps1)** : a lithe hooded ranger, crimson scarf blowing in the wind, dark layered leather armor with bracers, recurve bow held in the
left hand with a faint ember rune on the limb, quiver of fletched arrows on the back, face in shadow with two small glowing eyes. Crimson and
dark brown palette, ember orange accent.

**Mage (id dps2)** : a mage in deep violet robes with a tall pointed hat, floating spellbook at the hip, staff topped with a glowing teal orb held in
one hand, arcane runes glowing on the sleeves. Violet and indigo palette, teal accent.

**Boss, Ancestral Golem (facing LEFT, say it in the prompt)** : a huge ancient stone golem, cracked mossy granite body, massive fists, glowing cyan
core in the chest and cyan runes along the arms, small spikes on the head. Grey-blue stone, cyan accent. Also ask for : "menacing, fills the frame".

Other bosses later : Marsh Queen (poison swamp queen, purple glow), Ash Lord (ember knight lord, orange glow).

## Optional extra poses (only if the idle image is good)

Give the accepted idle image as reference and ask : "same character, same outfit, same style, now in an attack pose (mid strike / shooting / casting)
facing right, same flat magenta background". Two extra images per hero are enough : `attack` and `cast`.
