# art-2d : illustrations 2D des personnages

Direction prise le 2026-09-21 (décision D-074), après que la voie « 3D puis sprites » se soit révélée trop coûteuse par héros.
Principe : **une seule illustration de qualité par unité** (elle servira aussi de portrait pour le gacha), et **l'animation est faite
par le code** dans Unity à partir de l'attitude calculée par le cœur (`UnitPose`) : respiration, ondulation du tissu, élan d'attaque,
recul, geste d'incantation, chute. Aucune modélisation, aucun rig, aucune image d'animation à dessiner.

## Marche à suivre (environ 1 h pour le premier héros, moins ensuite)

1. Générer l'image avec un outil d'image (voir plus bas), en utilisant les prompts de ce fichier. Le **même bloc de style** pour toutes les unités.
2. Enregistrer le PNG dans `art-2d/inbox/<unité>_idle.png` (et si besoin `<unité>_attack.png`, `<unité>_cast.png`).
3. Lancer le traitement :
   ```
   blender --background --python art-2d/tools/process.py -- 512 --unity
   ```
   Il détoure le fond magenta, décontamine la frange, recadre, met à la hauteur demandée, écrit un aperçu sur le fond du jeu
   dans `art-2d/out/` et copie le résultat dans `unity/HealerGame/Assets/Resources/Art2D/`.
4. Juger dans le combat : `lancer-le-jeu-2d.bat` (ou `HealerGame.exe -healer-art2d`), puis ajuster le prompt si besoin.

## Outils (vérifier les limites gratuites et les conditions d'usage commercial de l'outil que VOUS utilisez : elles changent souvent)

- Commencer par une **offre gratuite** (génération d'images de ChatGPT, Google Gemini, Microsoft Copilot / Designer). Coût nul, prise en main d'un quart d'heure.
- Si la cohérence entre héros ne suffit pas, l'étape suivante habituelle est un abonnement (Midjourney, environ 10 € par mois) : il tient un style
  à partir d'une image de référence (`--sref`) et un personnage avec `--cref`. Aucun outil ne garantit un résultat ; le vrai test est le premier héros.
- Garder la **première illustration acceptée** et la joindre comme référence de style pour toutes les autres unités (« même style que l'image jointe »).

## À savoir une fois pour toutes (juridique)

- **Steam** et **Google Play** imposent de déclarer les contenus générés par IA : tenir la liste de l'outil et du prompt de chaque image.
- Dans beaucoup de pays, une image purement générée par IA peut **ne pas être protégée par le droit d'auteur** : quelqu'un pourrait la copier.
  Cela pèse surtout pour les portraits du gacha ; un artiste pourra plus tard refaire les illustrations clés en s'appuyant sur celles-ci.
- Chaque outil a ses propres conditions d'usage commercial. Les conserver (capture ou PDF) au moment de s'abonner.
- **Chaque outil et chaque source d'image va au générique** (`core/content/credits.json`). Un test refuse une ressource dont la licence
  n'est pas vérifiée : tant que l'outil n'est pas renseigné, l'entrée ne peut pas être livrée.

## Bloc de style (à copier au début de chaque prompt)

> Dark fantasy game character illustration, painterly digital art, clean readable silhouette, limited moody palette with one glowing accent color,
> strong rim light from behind, full body, standing idle pose, three-quarter view facing RIGHT, centered, isolated on a perfectly flat solid
> magenta (#FF00FF) background, no ground, no cast shadow, no text, no border, no watermark. Consistent proportions : slightly stylized,
> about 5 heads tall, large readable hands and weapon.

## Prompts par unité (à ajouter après le bloc de style)

**Soigneuse / Druide (id `healer`)** — *fait* : a druid healer wearing a pale deer-skull mask under a deep hood, tall branching antlers with small
glowing green blossoms and vines, mossy dark green layered robe, mantle of leaves, leather bandolier, gnarled wooden staff held in one hand topped
with a cage of branches around a glowing green crystal (7CFFB2), the other hand open palm up holding a small floating green light.

**Garde (id `tank`)** : a heavy knight in dark steel plate, great helm with a single glowing cyan visor slit, large tower shield with a glowing cyan
diamond emblem, short broad sword in the other hand, tattered dark blue cloth at the belt. Steel blue and black palette, cyan accent.

**Archère (id `dps1`)** : a lithe hooded ranger, crimson scarf blowing in the wind, dark layered leather armor with bracers, recurve bow held in the
left hand with a faint ember rune on the limb, quiver of fletched arrows on the back, face in shadow with two small glowing eyes. Crimson and dark
brown palette, ember orange accent.

**Mage (id `dps2`)** : a mage in deep violet robes with a tall pointed hat, floating spellbook at the hip, staff topped with a glowing teal orb held
in one hand, arcane runes glowing on the sleeves. Violet and indigo palette, teal accent.

**Boss, Golem Ancestral** (préciser *facing LEFT*) : a huge ancient stone golem, cracked mossy granite body, massive fists, glowing cyan core in the
chest and cyan runes along the arms, small spikes on the head, menacing, fills the frame. Grey-blue stone, cyan accent.

Les autres boss viendront ensuite : Reine des Marais (souveraine des marécages, lueur violette), Seigneur de Cendre (seigneur de braise, lueur orangée).

## Poses supplémentaires (seulement si l'image de repos est bonne)

Donner l'illustration acceptée en référence et demander : « same character, same outfit, same style, now in an attack pose (mid strike / shooting /
casting) facing right, same flat magenta background ». Deux images de plus par héros suffisent : `attack` et `cast`.

## État

| Unité | Illustration | En jeu |
|---|---|---|
| Soigneuse (`healer`) | faite | oui |
| Garde (`tank`) | à faire | — |
| Archère (`dps1`) | à faire | — |
| Mage (`dps2`) | à faire | — |
| Boss (3) | à faire | — |

Tant qu'une unité n'a pas d'illustration, le jeu affiche son modèle 3D : les deux rendus coexistent sans rien casser.
