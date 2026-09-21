# Journal de fabrication de l'Archère (dps1) — modèle haute définition + sprites

Journal honnête, tenu pour comparer le coût et la qualité d'un second héros « fait main par script » après
le Druide (`druid_v4.py`, D-069/D-073). Tout est mesuré, rien n'est arrondi à l'avantage du résultat.

## Chronologie

| | |
|---|---|
| Début (wall clock) | lundi 21 septembre 2026, **18 h 51** |
| Fin | lundi 21 septembre 2026, **19 h 33** |
| Durée totale | **~42 minutes** (lecture des scripts du Druide comprise) |
| Lancements de Blender (headless) | **20** — 10 × `archer.py`, 4 × `rig_archer.py`, 3 × `animate_archer.py`, 3 × un script de zoom jetable (scratchpad) |
| Itérations « rendre puis critiquer » | modèle **5**, rig **0** (validé indirectement par les sprites), animation **3** |
| Triangles | **25 996** (budget demandé 20 000 – 40 000, plafond 40 000 ; le Druide est à ~30 000) |

Le worktree était parti de `main` et non de `pixel-art` : la chaîne `art-pixel/` n'existait pas. J'ai dû
récupérer la branche `pixel-art` depuis `origin` (fetch en lecture seule, aucun push) avant de commencer.

## Ce qui a été produit

- `art-3d/blender/archer.py` — modèle, contrôle de dégagement automatique, 9 rendus de contrôle dans `out/`.
- `art-pixel/blender/rig_archer.py` — metarig Rigify ajusté, skinning des 289 morceaux séparés.
- `art-pixel/blender/animate_archer.py` — 4 animations, feuilles de sprites 128 px, palette partagée de 28 couleurs.
- `art-3d/blender/out/archer_{front,three_quarter,sprite,side,back}.png`, `archer_head_*`, `archer_hands_*`.
- `art-pixel/out/archer_{idle,attack,hit,death}.png`, `archer_anims.txt/.json`, `archer_contact.png`,
  `archer_preview.gif/.mp4`.

Rien n'a été copié dans `unity/`, aucun FBX n'est exporté (contrairement à `druid_v4.py`).

## Choix de la main qui tient l'arc (demandé explicitement)

Les sprites d'équipe sont pris depuis **+X** (`sprite_lib.setup_camera(side=+1)`), et le personnage regarde
vers **-Y** : le côté du corps le plus proche de la caméra est donc son côté **gauche** (x > 0). L'arc est mis
dans la **main gauche** pour trois raisons :

1. l'arc, la poignée enroulée de cuir et les doigts refermés dessus sont **au premier plan**, jamais masqués ;
2. c'est la prise standard d'un archer droitier (main gauche sur l'arc, main droite sur la corde) ;
3. le bras de corde part vers le fond : il ne cache rien et son coude haut dessine une bosse lisible
   derrière la capuche.

Le Druide fait l'inverse (bâton à sa droite, x < 0) parce qu'un bâton vertical au premier plan couperait le
visage en deux — un arc, lui, est large et ajouré : il encadre le personnage au lieu de le barrer.

## Défauts trouvés et corrigés (et ce qui les avait causés)

| # | Défaut | Cause | Correction |
|---|---|---|---|
| 1 | La corde traversait le bord de la capuche | ancrage « à la joue » recopié de la réalité, alors que la capuche stylisée est énorme | ancrage descendu devant la gorge, ouverture de capuche remontée ; **trouvé par le contrôle de dégagement automatique**, pas à l'œil |
| 2 | Visage entièrement bouché par la doublure cramoisie | la doublure reprenait les anneaux de la capuche × 0,93, donc passait **devant** le masque | doublure supprimée, remplacée par une visière courte + un liseré posé sur le vrai bord de l'ouverture |
| 3 | Flèche invisible dans la vue sprite | flèche construite dans le plan de l'arc, donc **dans** la poignée | décalage latéral de 3,2 cm : la flèche repose à côté du riser, comme en vrai |
| 4 | Arc en « bâton rond » | `lib2.sweep` applique `aspect` à l'axe transporté le long du chemin : j'avais mis un aspect < 1 en croyant aplatir dans le plan de l'arc | rayon petit + `aspect=2,6` : la branche est une lame large dans le plan, fine en travers |
| 5 | Silhouette de Druide (gros œuf sombre) | cape complète descendant aux chevilles | demi-cape courte, déchirée, arrêtée aux hanches |
| 6 | Carquois invisible | placé du côté opposé à la caméra, derrière la cape | déplacé côté caméra ; les empennages rouges sortent au-dessus de l'épaule, sur le fond |
| 7 | Main de corde totalement cachée | les deux mains à la même hauteur : le bras avant recouvrait exactement l'autre | main d'arc descendue de 5 cm, ancrage monté de 9 cm |
| 8 | La tête lisait comme un **bec d'oiseau** | visière longue et pointue + pointe de capuche en arrière | visière courte et large, inclinée vers le bas |
| 9 | Liseré cramoisi en « monture de lunettes » | boucle fermée autour de l'ouverture | boucle ouverte (côtés + bas seulement) |
| 10 | Sprites presque invisibles sur fond sombre | les rendus 3D ont une lampe chaude à 2 m qui n'existe pas dans `sprite_lib` : ils mentaient sur les valeurs | toute la palette remontée (tissu, cuirs, bois, peau) |
| 11 | Yeux invisibles à 128 px | fente de 2,7 cm ≈ 1 px | yeux plus hauts et plus lumineux (glow 3,2 → 4,5) |
| 12 | Torse = un bloc fauve plat | la cuirasse claire occupait tout le buste visible | cuirasse rabaissée d'une valeur + **baudrier cramoisi** en diagonale (la seule ligne d'identité visible de profil) |
| 13 | À la décoche, la corde repartait **en arrière** avec la main | la corde est skinnée entre les deux mains | seconde corde « au repos » (droite) rigide sur l'arc, échangée avec la première pendant les 2 images de décoche, flèche masquée |

## Défauts que je n'ai pas corrigés

- **Vue de face** : les deux bras barrent le torse à l'horizontale. C'est inhérent à un archer bandé face
  caméra ; les vues 3/4, profil et sprite sont nettes, la face l'est moins.
- **Yeux cramés en blanc** dans les gros plans 3D : le glow est réglé pour le pipeline sprite (éclairage
  différent). Il faudrait deux réglages, ou un rendu de contrôle avec l'éclairage des sprites.
- **La tresse est quasi invisible** (coincée entre capuche et cape) : ~300 triangles pour rien.
- **Mort** : elle se plie vers l'avant et finit à genoux, jamais franchement au sol ; à 8 i/s les deux
  dernières images se ressemblent trop. Le Druide avait exactement le même défaut.
- **Aucune animation secondaire** : cape, écharpe et carquois sont skinnés rigidement à la colonne, donc
  parfaitement raides pendant le recul et la chute. C'est ce qui manque le plus pour que ça « vive ».
- **Franges de hanches** (tassets) : du bruit illisible à 128 px, gardées parce qu'elles servent en 3D.
- **Carquois du mauvais côté** pour une archère droitière (choix assumé de lisibilité, pas de réalisme).
- **`idle` très discret** (±1,6°) : à 6 i/s on peut le prendre pour un sprite fixe. Volontaire (elle tient
  sa visée), mais discutable pour un jeu.

## Qualité par rapport au Druide : honnêtement, **égale, avec des forces différentes**

**Mieux que le Druide :**
- la **pose raconte une action** (viser, tirer) au lieu d'une pose de présentation ; le tir est lisible à 128 px ;
- les **mains tiennent vraiment** : poing refermé sur une poignée de la bonne taille, trois doigts crochetés
  sur la corde, flèche posée à côté du riser (le Druide a un beau poing, mais son bâton ne demande rien) ;
- la **corde qui s'étire** entre les deux mains est une vraie trouvaille réutilisable (arme à deux points d'attache) ;
- **silhouette distincte** : élancée, jambes visibles, arc large — impossible de confondre avec la robe du Druide ;
- **moins de triangles** (26 k contre 30 k) pour autant de lisibilité.

**Moins bien que le Druide :**
- le Druide a une **idée forte** (crâne de cerf, bois lumineux, vignes) ; l'Archère est une bonne archère
  générique : rien d'aussi mémorable qu'un bois de cerf fleuri ;
- **moins de matière vivante** : le Druide a des feuilles, des champignons, des vignes enroulées, une vraie
  variété de formes ; ici beaucoup de sangles et de cylindres ;
- **un seul point lumineux** (rune de l'arc) et deux yeux, contre les floraisons du Druide : moins
  d'accroche dans le noir ;
- le **visage** est un trou noir avec deux fentes ; le masque de crâne du Druide est plus expressif.

Le coût est du même ordre (~40 min ici contre plusieurs sessions pour le Druide, mais je partais d'une
bibliothèque et d'une méthode déjà éprouvées : le vrai gain est là, pas dans mon talent).

## Ce qui a été dur à réutiliser (pour généraliser le pipeline)

1. **`rig_druid.py` recopie les points de pose du modèle** (épaule, coude, poignet). J'ai changé de méthode :
   `archer.py` écrit sa pose dans le `.blend` (`scene["archer_pose"]`) et `rig_archer.py` la relit. À reprendre
   pour le Druide : c'est la principale source de désynchronisation possible.
2. **L'identité d'une pièce passe par son matériau.** La fusion par pivot+matériau de `druid_v4.py` est aussi
   ce qui décide de ce qu'on pourra skinner séparément : pour que la flèche suive la main de corde, il a fallu
   lui donner ses propres matériaux (`ArrowShaft`, `ArrowHead`, `ArrowFletch`, `Braced`). Non documenté, piège certain.
3. **`sprite_lib.setup_camera` avait le cadrage du Druide en dur** (`frame=4.3`, `center_z=1.95`, calés sur ses
   bois). Un personnage de 2,6 m n'occupait que 57 % de l'image. J'ai passé `frame`/`center_z` explicitement et
   ajouté `frame_units` / `feet_offset_units` dans le JSON : sans ça le client ne peut pas poser les pieds au
   bon endroit. **À faire** : ces deux nombres doivent devenir des données par personnage, pas des constantes C#.
4. **`lib2.sweep` ne laisse pas choisir l'orientation de la section** (transport parallèle) : impossible de dire
   « plat dans ce plan-là ». Un paramètre `normal=` supprimerait les tâtonnements (défaut n° 4 ci-dessus).
5. **La palette k-means est globale et non ancrée** : sur un personnage sombre elle dépense ses 28 couleurs dans
   les noirs. Un ancrage (fond, contour, 2-3 teintes d'identité imposées) donnerait des sprites plus francs.
6. **Poser en FK autour de X ne suffit pas pour un archer.** Le geste du tir se fait le long de la flèche :
   j'ai basculé le bras de corde en **IK** (`upper_arm_parent.R["IK_FK"] = 0`) et je déplace `hand_ik.R` le long
   de l'axe de la flèche, en mètres. Rigify n'a pas fait « claquer » le coude au passage IK/FK. C'est plus simple
   à régler que des angles et ça devrait devenir la règle pour toute arme à deux mains.
7. **Aucun garde-fou automatique côté sprites** : le contrôle de dégagement (7 cm) est la seule vérification
   automatique de toute la chaîne, et c'est lui qui a trouvé le défaut n° 1. Il manque l'équivalent pour les
   sprites (p. ex. « la silhouette occupe entre 45 % et 85 % de la hauteur de l'image », « au moins N pixels
   changent entre deux images d'une animation »).

## Détails techniques utiles

- Repère de l'arc : `BOW_A` (axe de la flèche), `BOW_U` (branches), `BOW_V` (normale au plan de l'arc), tous
  construits à partir de la poignée et du point d'ancrage. Toute la géométrie de l'arme est exprimée dedans
  (`bowp(u, d, s)`), ce qui garantit que corde, flèche et branches restent cohérentes quand on bouge la pose.
- La corde tendue est skinnée par géométrie : le script **retrouve tout seul** ses deux pointes et son point
  d'encochage dans le maillage (pas de constante recopiée), puis répartit les poids linéairement entre
  `DEF-hand.L` et `DEF-hand.R`.
- Les `.blend` ne sont pas commités (`art-3d/.gitignore` complété avec `*.blend` ; ceux du Druide restent suivis
  car déjà dans l'index). `archer_rigged.blend` se régénère avec `rig_archer.py`.
