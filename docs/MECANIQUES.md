# Mécaniques de jeu — référence complète

Ce document décrit **exactement** ce que fait le jeu aujourd'hui : chaque règle de combat, chaque formule, chaque
valeur et où la changer. Il est écrit à partir du code (`core/Healer.Combat`, `core/Healer.Ui`) et des données
(`core/content/*.json`) ; en cas de doute, **le code et les tests font foi**. La méthode d'équilibrage (bornes,
profils de joueurs, mesures) est dans `docs/EQUILIBRAGE.md` ; les décisions dans `docs/DECISIONS.md`.

> **Règle de tenue à jour** : toute modification de règle ou de valeur de jeu met ce fichier à jour dans le même
> commit. Une règle non documentée ici n'existe pas pour l'équipe.


## 1. Mécaniques de combat : ce qui existe et ce qui n'existe pas

| Mécanique | État | Où |
|---|---|---|
| **Coups critiques** | **existe** : alliés (attaques), boss (attaques), soigneur (soins) ; chance en %, multiplicateur en % | §3 |
| **Résistances** par type de dégât | **existe** : alliés, boss et effets sur la durée ; de -100 % (vulnérable) à 90 % | §3 |
| **Armure en pourcentage** | **existe** : réduit les dégâts **physiques** reçus (0 à 80 %), avant la défense fixe | §3 |
| **Esquive** | **existe** : évite un coup direct en entier (dégâts et effet), 0 à 60 % | §3 |
| **Menace** | **existe** : le boss peut choisir sa cible en proportion de la menace (attaques, soins) | §3 |
| **Temps d'incantation** | **existe** : le Soin de base s'incante 1 s (plus de recharge) | §4 |
| **Enrage** | existe : dégâts du boss qui montent avec le temps | §6 |
| Esquive des effets sur la durée (poison, brûlure) | n'existe pas : ils ne s'esquivent pas, seulement se purgent ou se résistent | |
| Critiques sur les effets sur la durée et les boucliers | n'existe pas | |
| Vol de vie, soin sur la durée, régénération de PV | n'existe pas (le seul soin est celui des sorts du soigneur) | |
| Résurrection | n'existe pas : un allié K.O. le reste jusqu'à la fin du combat | |
| Cumul d'un même effet | n'existe pas : ré-appliquer un effet **rafraîchit** sa durée | |
| Bonus/malus temporaires (force, ralentissement, étourdissement) | n'existe pas ; seuls existent le bouclier (positif) et les dégâts sur la durée (négatif) | |
| Interruption d'une incantation par un coup | n'existe pas : seule la mort de la cible, la mort du soigneur ou un manque de mana la font échouer | |
| Limite de temps globale | n'existe pas ; seul l'enrage (§6) pousse à finir | |

**Principe commun** : toute nouvelle mécanique est **pilotée par les données** et **sans effet tant qu'aucune donnée ne
l'active** (une chance de 0 ne consomme aucun tirage aléatoire). Ajouter une mécanique change le combat, donc les
combats de référence (golden) : cela se décide avec l'utilisateur (voir `docs/QUESTIONS_EN_ATTENTE.md`).

## 2. Le temps

- La simulation avance par **pas fixes** (`FixedStepper`, 50 ms côté jeu ; 100 ms dans les tests et pour le bot). Tout est
  horodaté en **millisecondes depuis le début du combat**.
- **Un seul appel de `Battle.Step` traite au plus une action par catégorie** : les intervalles des données doivent
  rester supérieurs au pas (testé).
- **Déterminisme** : mêmes données + même graine + mêmes commandes = même combat, à l'événement près. Le seul
  aléa est le choix de la cible des attaques du boss (générateur seedé `Rng`, jamais d'aléa système).


## 3. Statistiques et dégâts

### Les personnages (`core/content/characters.json`)

| Personnage | Rôle | PV | Att. | Déf. | Armure % | Esquive % | Critique | Menace | Type | Résistances |
|---|---|---|---|---|---|---|---|---|---|---|
| Garde (`tank`) | tank | 900 | 35 | 28 | **25** | 0 | 0 % | **× 5** | physique | feu 15 |
| Archère (`dps1`) | dégâts | 420 | 70 | 10 | 0 | **15** | **20 %, × 2** | × 1 | physique | – |
| Mage (`dps2`) | dégâts | 360 | 85 | 8 | 0 | 0 | **12 %, × 2** | × 1 | **magie** | magie 25 |
| Vous (`healer`) | soigneur | 480 | 12 | 14 | 0 | **8** | **10 %, × 1,5** (soins) | × 1 | – | poison 15 |

Mana du soigneur : 100 au départ, maximum 100, +6 par seconde. Le **soigneur n'attaque jamais** ; les trois autres
attaquent le boss **automatiquement**, toutes les **1 600 ms** (première frappe à 1 600 ms).

### Types de dégâts

`physical` (défaut), `magic`, `fire`, `poison`. Le type d'une attaque du boss est celui de son action, sinon celui du boss ; celui
d'un allié est son `damageType`. Un effet sur la durée a son propre type.

### Un coup direct du boss sur un allié, dans l'ordre

1. **Base** = attaque du boss × multiplicateur de l'action × (1 + palier d'enrage × pourcentage).
2. **Esquive** : si la cible a une chance d'esquive, un tirage ; esquivé = **aucun dégât et aucun effet** (poison compris) ; événement « esquive ».
3. **Critique du boss** (s'il en a) : un tirage ; si critique, base × multiplicateur (150 % par défaut).
4. **Résistance de la cible au type** : × (1 − résistance/100), la résistance étant bornée entre -100 et 90 (un vulnérable prend jusqu'à le double).
5. **Armure en %** : uniquement si le type est **physique** : × (1 − armure/100), armure bornée à 80.
6. **Arrondi** « demi vers le haut » (`floor(x + 0,5)`).
7. **Défense fixe** : − défense de la cible, **minimum 1 dégât**.
8. **Bouclier** : absorbe d'abord ; le reste va aux PV.

Exemple : le Golem (attaque 58, physique) frappe le Garde (armure 25 %, défense 28) : 58 × 0,75 = 43,5 → 44, moins 28 = **16** ;
il frappe l'Archère (défense 10) : 58 − 10 = **48**.

### Un allié qui frappe le boss

Base = attaque de l'allié ; **critique** de l'allié (un tirage s'il a une chance) ; **résistance du boss** au type de l'allié ;
arrondi demi-haut ; − défense du boss ; minimum 1. Ex. : le Mage (85, magie) sur le Golem (résistance magie -25 %, défense 18) :
85 × 1,25 = 106,25 → 106 − 18 = **88** (sur critique : 170 × 1,25 = 212,5 → 213 − 18 = **195**) ; le Garde (35, physique, résistance du Golem 15 %) : 35 × 0,85 = 29,75 → 30 − 18 = **12**.

### Effets sur la durée

Seules les **résistances** s'appliquent (jamais l'armure, l'esquive ni les critiques) : `dégâts par tick × (1 − résistance/100)`,
arrondi. Un effet sans type ignore les résistances ; le bouclier absorbe.

### Soin

Ajoute des PV jusqu'au maximum ; l'événement rapporte les PV **réellement** rendus. Le soigneur peut faire un **soin critique**
(× 1,5 par défaut) : **un seul tirage par lancer** (un soin de zone est critique pour tous ou pour personne).

### Bouclier et absorption

- Le **Bouclier** ajoute des points de bouclier (+260 de base) ; ils **s'additionnent** et **n'expirent pas**.
- Tout dégât (coup direct **ou** tick d'effet) est **absorbé d'abord** par le bouclier.
- Les événements de dégâts rapportent `Amount` (PV perdus) et `Absorbed` ; aucune conversion, aucune limite.

### Menace et choix des cibles

- Chaque unité accumule de la **menace** : une attaque d'allié ajoute (dégâts infligés × modificateur de menace / 100) ; un soin **effectif**
  ajoute (PV rendus × **0,2** × modificateur du soigneur / 100) à la menace du soigneur. Le Garde génère **× 5**.
- Un boss dont le ciblage est `random` (défaut) ignore la menace : cible au hasard parmi les vivants.
- Un boss dont le ciblage est `threat` tire sa cible **en proportion de (1 + menace)** parmi les alliés vivants : le Garde attire la plupart
  des coups à cible unique, mais tout allié reste une cible possible (le poids de base est 1). Les attaques de zone touchent tout le monde.
- L'interface marque l'allié le plus menacé (« Menace »).

### Mort

Un allié à 0 PV **meurt** : il ne reçoit plus de soin, n'attaque plus, ses effets sont supprimés. Victoire quand les PV du boss atteignent 0 ;
défaite quand plus aucun allié n'est vivant.

## 4. Le soigneur : sorts, mana, incantation, recharge

Données : `core/content/skills.json`. Le lancer d'un sort est une **commande horodatée** (`Command`) traitée par la simulation ; un lancer
impossible (mana insuffisant, recharge en cours, incantation déjà en cours) est simplement **ignoré**.

| Sort | Mana | Incantation | Recharge | Cible | Effet de base |
|---|---|---|---|---|---|
| Soin (`heal_single`) | 18 | **1 s** | **aucune** | 1 allié | +170 PV |
| Soin de zone (`heal_aoe`) | 60 | instantané | 5 s | tous les vivants | +140 PV chacun |
| Bouclier (`shield`) | 28 | instantané | 7 s | 1 allié | +260 de bouclier |
| Purge (`purge`) | 12 | instantané | 5 s | 1 allié | retire **tous** les effets négatifs de la cible |

- **Mana** : dépensé au moment où le sort **aboutit** (pas au début d'une incantation).
- **Incantation** (`castMs` > 0) : on choisit la cible, le sort **démarre** (événement `castStarted`) et **aboutit** au bout du temps (`skillUsed`,
  effets, mana, recharge). Le mana doit suffire au départ ET à l'achèvement.
- **Une seule incantation à la fois** ; les sorts **instantanés restent possibles** pendant qu'on incante, sans l'annuler.
- L'incantation **échoue** (`castFailed`, sans coût) si la cible meurt (raison « target »), si le soigneur meurt (« died ») ou si un sort
  instantané a vidé le mana entre-temps (« mana »). Aucun coup ne l'interrompt.
- **Recharge** propre à chaque sort, qui démarre à l'achèvement ; pas de recharge globale. Un sort peut avoir une incantation, une recharge, ou les deux.
- Un sort ciblé sans cible valide se lance sur le soigneur lui-même (règle de la simulation) ; le client, lui, **exige** une cible.

### Ciblage et gestes (`core/Healer.Ui/Targeting.cs`, `HoldRepeat.cs`)

- Toucher une carte d'allié (ou sa touche `1`–`4`) le **sélectionne** ; le re-toucher le garde sélectionné (D-050). Un allié K.O. ne se sélectionne
  pas ; la sélection tombe seule si la cible meurt. Elle **reste** après un sort.
- **Maintenir** un bouton de sort ou sa touche l'**enchaîne** dès qu'il est lançable : pour le Soin, une incantation bout à bout. Jamais en pause,
  jamais sans cible pour un sort ciblé. Maintenir un seul sort ne suffit à gagner aucun boss (testé).
- Pendant une incantation, l'interface affiche une **barre d'incantation** (sort, cible, progression).

## 5. Les effets sur la durée (`core/content/effects.json`)

Un effet est une donnée : `id`, `name`, `kind` (aujourd'hui : dégâts sur la durée), `damagePerTick`, `tickMs`, `durationMs`, **`damageType`**.

| Effet | Type | Dégâts par tick | Tick | Durée | Total maximal |
|---|---|---|---|---|---|
| Poison (`poison`) | poison | 20 | 1 s | 8 s | 160 |
| Venin (`venom`) | poison | 30 | 1 s | 9 s | 270 |
| Brûlure (`burn`) | feu | 36 | 1 s | 6 s | 216 |

- Le premier tick a lieu **une période après** l'application. À la fin de la durée, l'effet **expire**.
- Ré-appliquer le même effet **rafraîchit** sa durée sans cumuler les dégâts.
- La **Purge** retire tous les effets de sa cible immédiatement.
- Un allié qui meurt perd ses effets. Un coup **esquivé** n'applique pas son effet.

## 6. Les boss (`core/content/boss*.json`)

Un boss est entièrement décrit en données : PV, attaque, défense, cadence (`tickMs`), **motif** (actions répétées en boucle), **phases**, **enrage**,
et désormais **type de dégâts**, **critique**, **résistances** et **ciblage**.

- **Cadence** : à chaque `tickMs`, le boss exécute l'action suivante de son motif.
- **Actions** : `attack` (1 cible), `bigAttack` (toutes les cibles vivantes, **télégraphiée**), `poison` (multiplicateur 0 : n'inflige que l'effet indiqué),
  **`focusAttack`** (une seule victime, **annoncée**, voir ci-dessous).
- **Télégraphe** : une action avec `telegraphMs > 0` est **annoncée** cette durée avant son tick (jauge « ATTAQUE DE ZONE dans X s ») : la fenêtre pour
  poser un Bouclier. Toujours plus court que la cadence (testé).
- **Phases** : sous `atHpRatio` de PV, le boss change de motif et de cadence.
- **Enrage** (optionnel) : `enrage = { afterMs, everyMs, pct }` : dégâts directs + `pct` % par palier, cumulés ; pas les effets sur la durée.
- **Ciblage** : `random` ou `threat` (voir §3). **Type de dégâts** : `damageType` du boss ou de chaque action. **Critique** : `critPct`, `critMultPct`.
  **Résistances** : par type de dégât **reçu** (celui des alliés), en % (négatif = vulnérable).

| Boss | PV | Att. | Déf. | Cadence | Type | Critique | Résistances | Ciblage | Enrage |
|---|---|---|---|---|---|---|---|---|---|
| Golem Ancestral | 7 700 | 58 | 18 | 2,2 s | physique | – | physique 15 %, **magie -25 %** | menace | non |
| Reine des Marais | 8 450 | **24** | 16 | 2,0 s | physique + venin | 8 % | physique -10 %, magie 20 % | menace | non |
| Seigneur de Cendre | 9 020 | **19** | 20 | 1,9 s | **feu** | 12 %, × 1,75 | physique 10 %, magie -10 % | menace | 65 s, +5 % / 10 s |

Motifs et phases (inchangés) : Golem : 3 attaques + 1 attaque de zone ×2,5 (1,4 s), « Fureur » à 50 % (2,0 s, + poison) ; Reine : 2 attaques, 2 venins,
1 attaque de zone ×2,0 (1,4 s), « Marée toxique » à 50 % (1,8 s) ; Seigneur : 3 attaques + 1 attaque de zone ×2,4 (1,0 s), « Braise » à 66 % puis « Brasier »
à 33 % (brûlures).

**Attaque ciblée (`focusAttack`, D-056)** : au début de son télégraphe (1,5 s), le boss **choisit au hasard une victime parmi les alliés fragiles**
(tout sauf le Garde, sauf s'il ne reste que lui) et l'**annonce** : « ATTAQUE CIBLÉE sur Vous dans 1,1 s », étiquette « CIBLÉ ! » et bordure rouge sur sa carte, figurine
rouge pulsante. Le coup tombe sur elle et personne d'autre, avec le calcul de dégâts habituel (esquive, critique, résistance, armure, défense, bouclier). Si la victime
meurt avant, une autre est choisie. Réponse : **Bouclier** (260) sur la victime, ou la soigner à temps ; un Soin de zone ne suffit pas à lui seul.
Multiplicateur : **Reine ×5, Seigneur ×7** (Golem : aucun, tutoriel) ; une par cycle du motif, à la place d'une attaque simple. L'attaque de base des deux boss a été
abaissée (37 → 24, 34 → 19) pour que le total reste dans les bornes : le danger passe des petits coups constants aux pointes qu'il faut anticiper.

**Ce que cela change à jouer** : le Mage est l'atout contre le Golem (faible à la magie) ; le Garde y fait peu de dégâts mais protège ; contre le Seigneur
(feu), l'armure du Garde ne sert à rien (seule sa résistance au feu de 15 % compte) et ses coups critiques font mal.

## 7. Progression

### Étoiles et récompenses (`core/content/levels.json`, `Progression`)

- **Étoiles** : 1 = victoire ; 2 = victoire **sans allié K.O.** ; 3 = en plus **dégâts encaissés ≤ seuil du niveau**
  (les boucliers et les purges font baisser ce total). Seuils : 3 700 / 2 000 / 3 000.
- **Or** : première victoire (100 / 150 / 220) + bonus par étoile **nouvelle** (20 / 30 / 40) ; ensuite chaque victoire
  rapporte la récompense de répétition (50 / 80 / 120) + le bonus des étoiles nouvelles. Une défaite ne change rien.
- **Déblocage** : le niveau *n+1* s'ouvre à la première victoire du niveau *n*.
- **Meilleur temps** : le plus court des combats gagnés.
- Toute récompense et toute dépense passent par le **portefeuille** (`Wallet`) avec une raison écrite dans un
  registre ; aucun solde négatif, plafond à 999 999 999.

### L'atelier (`core/content/upgrades.json`, `Workshop`, `LoadoutApplier`)

- **Équipement** : 8 pistes (arme et armure de chaque personnage), 5 niveaux (50 / 80 / 110 / 150 / 200 or),
  **+4 % par niveau** : armes = attaque (Bâton = soins) ; armures = PV et défense (Robe du soigneur = PV et mana).
- **Talents du soigneur** : 3 paliers (2 / 5 / 8 étoiles requises, 150 / 300 / 500 or), un choix parmi deux par
  palier ; premier choix payant, **changement gratuit**.
- **Règle de calcul** : les pourcentages de toutes les sources **s'additionnent** puis s'appliquent **une fois** à la
  valeur de base (pas d'effet boule de neige), arrondis au plus proche ; le coût en mana ne descend jamais sous 0, la
  recharge jamais sous 1 ms. Sans amélioration, le combat est **identique au contenu de base** (testé).
- Les améliorations touchent les **alliés et les sorts**, jamais le boss.


## 8. Ce que l'interface affiche

- Tout nombre affiché est **tronqué** (jamais arrondi vers le haut), abrégé s'il est long (`7000`, `12,3k`, `4,9s`).
- Bilan de combat : durée, soins effectifs, dégâts encaissés (dont absorbés), sorts lancés, alliés K.O., effets
  purgés, **dégâts infligés au boss par membre** (avec part en %), étoiles, or, déblocages.


## 9. Sons, musique, animations, réglages (présentation)

Rien de tout cela ne change le combat : ces systèmes **lisent** les événements. Leur logique vit dans le cœur
(`core/Healer.Combat/Presentation`) pour être testée ; le client Unity ne fait que jouer le résultat.

| Sujet | Règle |
|---|---|
| **Quel événement fait quel son** (`AudioCues`) | soin qui rend des PV → soin ; bouclier ; purge (pas l'expiration ni la mort d'un effet) ; effet appliqué ; coup avec PV perdus (un coup entièrement absorbé est muet) ; mort ; action du boss (zone = grondement, sinon un tic) ; changement de phase et enrage = rugissements ; fin = victoire ou défaite ; lancer d'un sort = petit son de confirmation |
| **Anti-répétition** (`CueLimiter`) | un même son ne se rejoue pas avant 60 ms (150 ms pour le tic du boss, 1,5 s pour victoire, défaite, rugissements) ; un soin de zone sur 4 alliés = un seul son |
| **Volumes** (`Mix`, réglages) | musique 60 et effets 80 par défaut, de 0 à 100 par pas de 10 ; « Son coupé » (touche M) met tout à zéro sans effacer les volumes |
| **Musique adaptative** (`MusicDirector`) | intensité = 0,25 + 0,5 × (1 − PV du plus blessé) + 0,12 si une attaque de zone est annoncée + 0,08 × phase du boss (max 3) + 0,05 × palier d'enrage (max 4), bornée à 0-1 ; 0 dans les menus. Quatre couches : nappe (toujours), pulsation grave (à partir de 0,25), mélodie (0,50), alarme aiguë (0,78) ; montée en 0,6 s, retour au calme en 2,5 s |
| **Animations** (`UnitAnimator`) | élan d'attaque 380 ms, recul 320 ms, geste de lancer 500 ms, halo de soin ou de bouclier 700 ms, chute à la mort 750 ms puis reste à terre ; le boss avance quand il attaque et recule quand il est touché |
| **Effets visuels** (client) | anneau au sol par sort ; faisceau du soigneur vers sa cible ; anneau de danger rouge sous l'équipe pendant l'annonce d'une attaque de zone ; aura d'enrage |
| **Secousse d'écran** | à l'attaque de zone, au changement de phase, à l'enrage ; désactivable dans les réglages |


## 10. Où changer quoi

| Je veux… | Je modifie |
|---|---|
| Changer une statistique de personnage ou de sort | `core/content/characters.json` / `skills.json` |
| Ajouter un boss, changer son motif, ses phases, son enrage | `core/content/boss*.json` + `levels.json` |
| Ajouter un effet | `core/content/effects.json` (puis un boss l'utilise par `effectId`) |
| Changer un prix, un palier, un pourcentage d'amélioration | `core/content/upgrades.json` |
| Changer une récompense ou un seuil d'étoile | `core/content/levels.json` |
| Ajouter une **nouvelle mécanique** (critique, résistance…) | code dans `core/Healer.Combat` + test + nouveaux golden **avec l'accord de l'utilisateur** |

Après **toute** modification de valeur : lancer `dotnet test core/Healer.Combat.Tests` (les bornes d'équilibrage de
`docs/EQUILIBRAGE.md` échouent si un réglage sort du cadre) et mettre ce document à jour.

