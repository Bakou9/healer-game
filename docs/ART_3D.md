# Modèles 3D « user friendly » : guide de style

Statut : **direction proposée par l'agent** (l'utilisateur délègue le choix du
style : « je compte sur toi »), décision D-030, à valider après le premier test.

## Ce que « user friendly » veut dire ici

1. **Lisible avant tout** : on reconnaît chaque personnage et chaque danger d'un
   coup d'œil, en petit, sur un écran de téléphone en portrait.
2. **Accueillant** : formes rondes, proportions « jouet », couleurs vives, aucun
   visuel effrayant ; même le boss est imposant mais sympathique.
3. **Distinguable sans la couleur seule** : chaque rôle a une **silhouette**
   propre (accessibilité, principe UX n°4).
4. **Léger** : peu de polygones, pas de textures obligatoires, chargement
   instantané, batterie ménagée.
5. **Reproductible** : les modèles sont construits par des scripts, pas dessinés
   à la main dans un coin (voir « Fabrication »).

## Style

**Low-poly stylisé « figurine »** : primitives arrondies (sphères, capsules,
cylindres, cubes biseautés), ombrage doux, contour de lumière (*rim light*),
matériaux unis (couleurs de sommets ou matériau simple URP), **accents
émissifs** pour l'information (cœur du Golem, orbe du mage, croix du soigneur).

Proportions des personnages : grosse tête (~40 % de la hauteur), corps compact,
mains et pieds simples.

## Le langage visuel (repris de docs/UX.md)

| Sens | Couleur | Forme d'appoint |
|---|---|---|
| Soin | vert `#7CFFB2` | croix, feuille |
| Bouclier | bleu `#7CC8FF` | blason |
| Poison | violet `#C78CFF` | goutte |
| Danger | rouge `#FF5B5B` | pointes, halo pulsant |
| Phase 2 « Fureur » | orange `#FF6A3D` | cœur et yeux embrasés |

## Les modèles de départ

| Modèle | Silhouette | Signes distinctifs | Triangles max |
|---|---|---|---|
| **Golem Ancestral** (boss) | large, trapu, épaules rondes | cœur lumineux cyan (orange en phase 2), yeux, runes des bras, fissures | 8 000 |
| **Garde** (tank) | large et bas | grand bouclier rond, casque | 3 000 |
| **Archère** (dégâts) | fine et élancée | arc, carquois, queue de cheval | 3 000 |
| **Mage** (dégâts) | haute, chapeau pointu | bâton à orbe violet | 3 000 |
| **Soigneuse** (le joueur) | moyenne, aérienne | bâton à orbe vert, tenue claire, croix | 3 000 |

Budget d'écran : ≤ 30 000 triangles visibles, ≤ 3 matériaux par modèle, aucun
modèle ne dépend d'une texture (sinon, textures ≤ 512 px).

## Caméra et mise en scène (portrait)

Vue **trois quarts** légèrement plongeante : le boss en haut, l'équipe en bas au
premier plan, l'interface superposée dans la zone du pouce (docs/UX.md §4). Les
cartes d'interface restent les cibles tactiles : on ne touche pas les modèles.

## Fabrication : reproductible et testée

- Les modèles sont assemblés par des **scripts d'Éditeur** (`Assets/Editor/Build*.cs`)
  à partir de primitives et de maillages procéduraux, et enregistrés en
  **préfabriqués**. Une seule commande reconstruit tout (ticket E14-T13).
- Le **MCP** sert à inspecter et ajuster, jamais à être la seule trace.
- **Tests** (EditMode) : budget de triangles par modèle, nombre de matériaux,
  présence des sous-objets requis (ex. `Core`, `Eyes`, `Staff`), échelle et point
  d'ancrage cohérents.

## Ce qu'on évite

Textures lourdes, détails minuscules illisibles à l'échelle d'un téléphone,
dépendance à un logiciel de modélisation externe pour les modèles de départ,
effets qui masquent l'information (docs/UX.md, principe 6).

## Charte « dark fantasy » et système d'apparence modulaire (D-058, D-061)

**Langage de formes** : arêtes vives et grosses facettes (jamais de sphères lisses pour le corps) ; silhouettes lisibles à 3 m d'écran : cornes, capuches, épaulières hérissées, capes en lambeaux, cristaux. Le sombre domine, le lumineux est réservé à ce qui compte (yeux, runes, cristaux, arme magique).
**Palette** : acier terni, cuir noir, sang (`8E2B34`), os (`CFC6B0`), or vieilli (`B8924A`) ; couleurs lumineuses par rôle (soin vert, bouclier bleu, poison violet, ambre pour l'Archère). Un boss a une couleur calme et une couleur de fureur.
**Budgets** : personnage ≤ 3 000 triangles, boss ≤ 8 000 (vérifiés par « Healer/Vérifier les modèles 3D »). **Proportions** : héros ≈ 2,9 unités avec cornes ou chapeau, tête petite, bras longs.

**Modularité** : chaque héros = un **corps commun** (jambes, torse, tête, bras, pivots animables du `UnitRig`) + des **pièces d'équipement** par emplacement (`weapon`, `armor`). Le niveau acheté à l'Atelier (`{héros}_{emplacement}`) choisit un **palier** (Ordinaire 0-1, Raffiné 2-3, Légendaire 4-5 ; `core/content/appearance.json`) : forme de la pièce (cornes, halo, ailes, runes, taille de l'arme…) et **palette** (primary, secondary, accent, glow, trim). Le cœur ne fait que résoudre « quelle pièce, quel palier, quelles couleurs » (`AppearanceCatalog.Resolve`, testé) ; le client fabrique la pièce (`ModelFactory`, classe `Look`).
**Ajouter un emplacement ou un héros** : une piste d'équipement dans `upgrades.json`, une entrée par palier dans `appearance.json`, une fonction de construction ; les tests refusent un héros ou un emplacement sans version pour chaque palier.
**Remplacer par des pièces importées** : l'identifiant de pièce (`tank.weapon`) et la palette restent le contrat ; seule la fabrication change (mesh importé attaché au même pivot). Toute ressource importée passe par `docs/ASSETS.md` et `credits.json`.
