# Mécaniques de jeu — référence complète

Ce document décrit **exactement** ce que fait le jeu aujourd'hui : chaque règle de combat, chaque formule, chaque
valeur et où la changer. Il est écrit à partir du code (`core/Healer.Combat`, `core/Healer.Ui`) et des données
(`core/content/*.json`) ; en cas de doute, **le code et les tests font foi**. La méthode d'équilibrage (bornes,
profils de joueurs, mesures) est dans `docs/EQUILIBRAGE.md` ; les décisions dans `docs/DECISIONS.md`.

> **Règle de tenue à jour** : toute modification de règle ou de valeur de jeu met ce fichier à jour dans le même
> commit. Une règle non documentée ici n'existe pas pour l'équipe.

## 1. Ce qui n'existe PAS (encore)

Pour éviter tout malentendu, voici ce que le jeu **ne contient pas** :

| Mécanique | État |
|---|---|
| **Coups critiques** | n'existe pas : tous les dégâts sont déterministes (pas de hasard hors choix de la cible du boss) |
| **Résistances** (feu, poison, magie…) | n'existe pas : un effet inflige toujours ses dégâts bruts |
| **Armure en pourcentage** | n'existe pas : la défense est une **soustraction fixe** (voir §3) |
| **Esquive, parade, coup manqué** | n'existe pas |
| **Vol de vie, soin sur la durée, régénération de PV** | n'existe pas (le seul soin est celui des sorts du soigneur) |
| **Menace / aggro** | n'existe pas : le boss choisit sa cible **au hasard** parmi les alliés vivants |
| **Résurrection** | n'existe pas : un allié K.O. le reste jusqu'à la fin du combat |
| **Cumul d'un même effet** | n'existe pas : ré-appliquer un effet **rafraîchit** sa durée |
| **Bonus/malus temporaires** (force, ralentissement, étourdissement) | n'existe pas ; seuls existent le **bouclier** (positif) et les **dégâts sur la durée** (négatif) |
| **Limite de temps globale** | n'existe pas ; seul l'**enrage** (§6) pousse à finir |

Ces mécaniques sont des pistes de jalons futurs ; en ajouter une change le combat, donc les combats de référence
(golden) : cela se décide avec l'utilisateur (voir `docs/QUESTIONS_EN_ATTENTE.md`).

## 2. Le temps

- La simulation avance par **pas fixes** (`FixedStepper`, 50 ms côté jeu ; 100 ms dans les tests et pour le bot). Tout est
  horodaté en **millisecondes depuis le début du combat**.
- **Un seul appel de `Battle.Step` traite au plus une action par catégorie** : les intervalles des données doivent
  rester supérieurs au pas (testé).
- **Déterminisme** : mêmes données + même graine + mêmes commandes = même combat, à l'événement près. Le seul
  aléa est le choix de la cible des attaques du boss (générateur seedé `Rng`, jamais d'aléa système).

## 3. Statistiques et dégâts

### Les personnages (`core/content/characters.json`)

| Personnage | Rôle | PV | Attaque | Défense | Mana | Régén. mana |
|---|---|---|---|---|---|---|
| Garde (`tank`) | tank | 900 | 35 | 28 | – | – |
| Archère (`dps1`) | dégâts | 420 | 70 | 10 | – | – |
| Mage (`dps2`) | dégâts | 360 | 85 | 8 | – | – |
| Vous (`healer`) | soigneur | 480 | 12 | 14 | 100 | 6 / s |

- Le **soigneur n'attaque jamais** (son attaque de 12 ne sert pas) ; il soigne, protège, purge. C'est le joueur.
- Les trois autres attaquent le boss **automatiquement** (auto-battle).

### Formules de dégâts

| Situation | Formule |
|---|---|
| Un allié frappe le boss | `max(1, attaque de l'allié − défense du boss)`, une fois **toutes les 1 600 ms** (première frappe à 1 600 ms) |
| Une attaque du boss touche un allié | `max(1, arrondi(attaque du boss × multiplicateur de l'action × enrage) − défense de l'allié)` |
| Arrondi | « demi vers le haut » (`floor(x + 0,5)`), comme `Math.round` de JavaScript (référence de portage) |
| Dégât d'un effet (poison, venin, brûlure) | `dégâts par tick` **bruts** : la défense ne s'applique pas ; le bouclier, si |
| Soin | ajoute des PV jusqu'à un maximum de `PV max` ; l'événement rapporte les PV **réellement** rendus |

**La défense est donc une soustraction fixe, pas un pourcentage** : 28 de défense retire 28 points à chaque coup
reçu (minimum 1 dégât). Exemple : une attaque du Golem (55) inflige 27 au Garde (défense 28), 45 à l'Archère (10),
47 au Mage (8).

### Bouclier et absorption

- Le **Bouclier** ajoute des points de bouclier à un allié (`+260` de base). Les boucliers **s'additionnent** et
  **n'expirent pas** : ils durent jusqu'à être consommés.
- Tout dégât (coup direct **ou** tick d'effet) est **absorbé d'abord** par le bouclier, le reste va aux PV.
- Les événements de dégâts rapportent `Amount` (PV perdus) et `Absorbed` (points absorbés) ; c'est ce qu'affiche le
  bilan (« dont absorbés par boucliers »).
- Aucune conversion (un bouclier n'est jamais transformé en soin) et aucune limite de bouclier.

### Mort

Un allié à 0 PV **meurt** : il ne reçoit plus de soin, n'attaque plus, ses effets sont supprimés (raison « died »).
Victoire quand les PV du boss atteignent 0 ; défaite quand plus aucun allié n'est vivant.

## 4. Le soigneur : sorts, mana, recharge

Données : `core/content/skills.json`. Le lancer d'un sort est une **commande horodatée** (`Command`) traitée par la
simulation ; un lancer impossible (mana insuffisant, recharge en cours) est simplement **ignoré**.

| Sort | Mana | Recharge | Cible | Effet de base |
|---|---|---|---|---|
| Soin (`heal_single`) | 18 | 1,2 s | 1 allié | +170 PV |
| Soin de zone (`heal_aoe`) | 45 | 5 s | tous les vivants | +160 PV chacun |
| Bouclier (`shield`) | 28 | 7 s | 1 allié | +260 de bouclier |
| Purge (`purge`) | 12 | 5 s | 1 allié | retire **tous** les effets négatifs de la cible |

- **Mana** : 100 au départ, maximum 100, **+6 par seconde** en continu. Le mana est dépensé au moment du lancer.
- **Recharge** : propre à chaque sort, démarre au lancer. **Pas de recharge globale** : on peut enchaîner des sorts
  différents.
- Un sort ciblé sans cible valide se lance sur le soigneur lui-même (règle de la simulation) ; le client, lui,
  **exige** une cible (message « Choisissez d'abord un allié »).

### Ciblage et gestes (`core/Healer.Ui/Targeting.cs`, `HoldRepeat.cs`)

- Toucher une carte d'allié (ou sa touche `1`–`4`) le **sélectionne**. **Le re-toucher le garde sélectionné**
  (D-050). Un allié K.O. ne se sélectionne pas ; la sélection tombe seule si la cible meurt.
- La sélection **reste** après un sort : soigner plusieurs fois la même cible ne demande qu'un geste.
- **Maintenir** un bouton de sort (souris, doigt) ou sa touche l'**enchaîne** dès qu'il est lançable (recharge et
  mana respectés). Ne s'enchaîne qu'en combat actif (jamais en pause), jamais sans cible pour un sort ciblé.
  Choisir la cible et *quel* sort maintenir reste la décision du joueur (maintenir un seul sort ne suffit à gagner
  aucun boss : testé).

## 5. Les effets sur la durée (`core/content/effects.json`)

Un effet est une donnée : `id`, `name`, `kind` (aujourd'hui : dégâts sur la durée), `damagePerTick`, `tickMs`, `durationMs`.

| Effet | Dégâts par tick | Tick | Durée | Total maximal |
|---|---|---|---|---|
| Poison (`poison`) | 20 | 1 s | 8 s | 160 |
| Venin (`venom`) | 30 | 1 s | 9 s | 270 |
| Brûlure (`burn`) | 36 | 1 s | 6 s | 216 |

- Le premier tick a lieu **une période après** l'application. À la fin de la durée, l'effet **expire**.
- Ré-appliquer le même effet **rafraîchit** sa durée sans cumuler les dégâts.
- La **Purge** retire tous les effets de sa cible immédiatement (raison « cleansed » dans les événements).
- Un allié qui meurt perd ses effets.

## 6. Les boss (`core/content/boss*.json`)

Un boss est entièrement décrit en données : PV, attaque, défense, cadence (`tickMs`), **motif** (liste d'actions
répétée en boucle), **phases** et **enrage**.

- **Cadence** : à chaque `tickMs`, le boss exécute l'action suivante de son motif (en boucle).
- **Types d'actions** : `attack` (1 cible au hasard), `bigAttack` (toutes les cibles vivantes, **télégraphiée**),
  `poison` (multiplicateur 0 : n'inflige que l'effet indiqué, à une cible au hasard).
- **Télégraphe** : une action avec `telegraphMs > 0` est **annoncée** cette durée avant son tick (l'interface affiche
  « ATTAQUE DE ZONE dans X s » et une jauge) : c'est la fenêtre pour poser un Bouclier. Un télégraphe est toujours
  plus court que la cadence (testé).
- **Phases** : quand les PV du boss passent sous `atHpRatio`, le boss change de motif et de cadence (le motif repart
  de son début).
- **Enrage** (optionnel, D-050) : `enrage = { afterMs, everyMs, pct }`. À `afterMs`, puis toutes les `everyMs`, les
  **dégâts directs** du boss augmentent de `pct` % (cumulés : au palier *n*, `+ n × pct %`). Il n'affecte **pas** les
  effets sur la durée. Le jeu affiche « ENRAGÉ +N % » et joue un rugissement. Absent pour le Golem (tutoriel).

| Boss | PV | Att. | Déf. | Cadence | Motif de base | Phases | Enrage |
|---|---|---|---|---|---|---|---|
| Golem Ancestral | 7 000 | 55 | 18 | 2,2 s | 3 attaques, 1 attaque de zone ×2,5 (1,4 s) | « Fureur » à 50 % : 2,0 s, + poison | non |
| Reine des Marais | 6 500 | 34 | 16 | 2,0 s | 2 attaques, 2 venins, 1 attaque de zone ×2,0 (1,4 s) | « Marée toxique » à 50 % : 1,8 s, plus de venin | non |
| Seigneur de Cendre | 8 200 | 34 | 20 | 1,9 s | 3 attaques, 1 attaque de zone ×2,4 (1,0 s) | « Braise » 66 % (1,8 s, brûlure), « Brasier » 33 % (1,6 s, 2 brûlures) | à 65 s, +5 % toutes les 10 s |

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
