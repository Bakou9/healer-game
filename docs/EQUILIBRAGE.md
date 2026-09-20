# Ce qu'est un jeu « équilibré » pour ce projet

Ce fichier est **contractuel** : tout agent (ou humain) qui touche à des valeurs
de jeu (`src/data/*.json`), à une règle de combat ou au bot de référence le lit
avant, et vérifie après que les bornes ci-dessous tiennent. Il est référencé
depuis `CLAUDE.md`. Les bornes sont encodées dans
`src/testing/regression.test.ts` : si ce fichier et les tests divergent, c'est
une erreur à corriger, pas à contourner.

## 1. Notre besoin, en une phrase

Un combat de **60 à 120 secondes** sur mobile où le joueur ne contrôle que le
**soigneur** (le reste de l'équipe est en auto-battle) : il doit sentir qu'**il
sauve l'équipe par ses décisions**, ni qu'il regarde un combat qui se gagne
seul, ni qu'il subit un combat injuste.

## 2. Ce que « équilibré » veut dire ici (les 6 critères)

**1. Gagnable mais pas trivial (zone de flow).**
Un joueur attentif gagne presque toujours, mais l'équipe passe régulièrement
près de la rupture. Trop facile = ennui ; trop dur = frustration.
→ Mesure : victoires ≥ 95 %, PV minimum moyen de l'équipe entre 15 % et 45 %.

**2. La compétence se voit.**
Mieux jouer donne un meilleur résultat, moins bien jouer un moins bon. Si un
joueur lent ou distrait obtient le même résultat qu'un joueur attentif, les
décisions n'ont aucun poids.
→ Mesure : un joueur qui réagit en 1,5 s au lieu de 0,5 s descend nettement plus bas (≥ 5 points de PV minimum).

**3. La passivité est punie.**
Ne rien faire, ou répéter un seul sort, ne peut pas suffire à gagner.
→ Mesure : sans soigneur, ou en spammant le soin simple, 0 % de victoires.

**4. Chaque sort a sa place (pas de stratégie dominante).**
Aucune compétence n'est inutile, aucune ne rend les autres superflues :
- **Soin** : réactif et économe en mana, mais un seul allié à la fois.
- **Soin de zone** : cher et lent, rentable quand plusieurs alliés souffrent.
- **Bouclier** : anticipatif, à poser avant la grosse attaque télégraphiée.
- **Purge** : peu chère et puissante, mais utile **seulement** contre le poison.
→ Mesure : ignorer le poison (ne jamais purger) fait descendre l'équipe nettement plus bas et augmente les morts.

**5. Les défaites sont « justes » et lisibles.**
Le joueur doit pouvoir anticiper ce qui le tue : grosses attaques **télégraphiées**
(≥ 1,4 s), effets visibles (statut affiché avec sa durée), chiffres de dégâts et
de soins à l'écran. Un joueur attentif perd rarement un allié.
→ Mesure : morts ≤ 10 % des combats avec le bot attentif. Toute nouvelle
mécanique offensive a un signal visible avant ou pendant son effet.

**6. Le rythme respecte l'humain.**
Le temps de réaction humain est de ~0,25 s, plus le temps de viser un tap : un
danger qu'il faut contrer doit laisser **au moins ~1 s**. Les décisions
arrivent au rythme de un ordre toutes les 1 à 3 secondes, pas 10 par seconde.
→ Mesure : durée du combat 60 à 120 s ; le bot de référence décide toutes les
500 ms (joueur attentif) ; télégraphes ≥ 1,4 s.

## 3. Valeurs à hauteur humaine (règle d'affichage et de données)

- **Ce que le joueur voit est tronqué** (jamais arrondi vers le haut), sans
  décimales inutiles, abrégé quand c'est long : `7000`, `12,3k`, `2,5M`,
  durées à une décimale (`4,9s`). Aucun nombre brut (`7.199999`) à l'écran.
  Toujours passer par `src/ui/format.ts` (`formatNumber`, `formatSeconds`,
  `formatRatio`).
- **Les données de jeu sont des entiers ronds** : PV, dégâts, mana, durées
  en ms (multiples de 100 ou de 500 de préférence). Seuls les multiplicateurs
  et les ratios (`multiplier`, `atHpRatio`) peuvent être décimaux. Vérifié par
  `src/testing/data-effects.test.ts`.
- La simulation peut garder des valeurs fractionnaires en interne (régénération
  de mana) : **seul l'affichage** est tronqué.
- Un nombre affiché doit se comprendre d'un coup d'œil : si un ordre de grandeur
  dépasse 5 chiffres, il faut l'abréger ou revoir l'échelle des données.

## 4. Les « joueurs de référence » (profils de test)

| Profil | Comportement | Doit… |
|---|---|---|
| Attentif | décide toutes les 500 ms, purge, boucliers, soins | gagner ≥ 95 %, PV min moyen 15–45 %, morts ≤ 10 % |
| Lent | décide toutes les 1500 ms | descendre nettement plus bas que l'attentif |
| Sans purge | ignore le poison | descendre nettement plus bas, plus de morts |
| Passif | aucun ordre | perdre toujours |
| Spam | répète le soin simple sur le tank | perdre toujours |

Ces profils sont dans `src/sim/referenceHealerBot.ts` et
`src/testing/regression.test.ts`. Un bon test d'équilibrage compare **des
comportements de joueurs**, pas seulement des nombres.

## 5. Mesures actuelles (200 combats par profil, seeds différents)

| Profil | Victoires | Combats avec un mort | PV min moyen | Durée |
|---|---|---|---|---|
| Attentif (500 ms) | 100 % | 1 % | 28 % | 83–86 s |
| Lent (1,5 s) | 100 % | 11 % | 16 % | 83–98 s |
| Sans purge | 100 % | 8 % | 17 % | 83–110 s |
| Spam de soin simple | 0 % | 100 % | — | 62–70 s |
| Sans soigneur | 0 % | 100 % | — | 46–55 s |

La phase 2 (« Fureur ») commence vers 42 s ; le poison (20 dégâts par seconde
pendant 8 s, soit 160 par application non purgée) y apparaît.

## 6. Boutons de réglage (quoi changer pour obtenir quel effet)

| Je veux… | Je change… |
|---|---|
| Combat globalement plus dur | `boss1.json` : `atk`, `multiplier`, `tickMs` (plus petit = plus dur) |
| Plus de tension en phase 2 | `phases[].tickMs`, fréquence des `poison` dans le pattern |
| Rendre la Purge plus indispensable | `effects.json` : `damagePerTick`, `durationMs` |
| Rendre le mana plus contraignant | `characters.json` : `manaRegenPerSec`, `maxMana` ; `skills.json` : `manaCost` |
| Rendre un sort moins/plus rentable | `skills.json` : `healAmount`, `shieldAmount`, `cooldownMs`, `manaCost` |
| Combat plus court/long | `boss1.json` : `maxHp`, ou `atk` des alliés dans `characters.json` |

## 7. Procédure à chaque changement de valeurs

1. Lancer `npm test` **avant** (état de référence).
2. Modifier. Mesurer avec les profils (au besoin un script jetable qui balaie
   plusieurs valeurs sur ≥ 100 seeds, comme pour le poison).
3. `npm run check`. Si un test d'équilibrage échoue : appliquer le protocole de
   non-régression de `CLAUDE.md` (expliquer la cause, verdict voulu/accidentel,
   accord avant de changer une borne ou une référence).
4. Rendre compte de l'avant/après des mesures de la section 5 et mettre ce
   tableau à jour.
5. Ne **jamais** relâcher une borne pour faire passer un test : soit la valeur
   est mauvaise, soit la définition de « équilibré » change — et dans ce cas
   c'est une décision de l'utilisateur, à noter ici.

## 8. Choix de spécialisation : équilibre entre chaque décision

Le joueur spécialise son soigneur par des **choix** (voies, talents, paliers).
L'équilibre doit tenir **pour chaque choix et chaque suite de choix**, pas
seulement pour le build final. Décision D-011 ; tickets E08-T02 à T11.
Statut : **à construire** (phase 2) — les seuils ci-dessous sont des valeurs
initiales *proposées*, à calibrer par mesure puis à figer ici.

| Exigence | Ce que le test vérifie | Seuil initial proposé | Ticket |
|---|---|---|---|
| **Viabilité** | chaque build, joué par son bot, atteint un plancher sur le contenu de référence | ≥ 85 % de victoires | E08-T05 |
| **Non-dominance** | pas de « meilleur build » : écart borné sur le contenu générique, et aucun build supérieur sur toutes les métriques (dominance de Pareto) | écart de victoires ≤ 10 points ; de PV minimum ≤ 12 points | E08-T06 |
| **Niche** | chaque voie est la meilleure sur ≥ 1 archétype de boss, sans y être obligatoire | dans le meilleur quart, d'au moins 5 points | E08-T07 |
| **Ablation** | retirer un talent ne change ni trop (écrasant) ni trop peu (mort) | 1 à 8 points sur son terrain | E08-T08 |
| **Parité de budget** | les choix d'un même palier ont des budgets de puissance comparables | ±10 % | E08-T09 |
| **Chemins** | chaque suite de choix atteignable est viable **au niveau où elle existe**, y compris après respec | plancher du niveau | E08-T11 |
| **Rapport versionné** | toute évolution du tableau d'équilibre est signalée et expliquée | diff dans `docs/balance/report.md` | E08-T10 |

Principes :
- **Un build se juge joué comme il se joue** : un bot par spécialisation
  (E08-T02), avec le **même délai humain** pour tous.
- **Espace de builds couvert de façon reproductible** : énumération si petit,
  sinon échantillonnage seedé qui couvre chaque talent au moins N fois (E08-T03).
- **Un choix sans intérêt est un défaut**, au même titre qu'un choix écrasant.
- **Une régression d'équilibre entre builds se traite comme une régression
  golden** : cause, verdict voulu/accidentel, accord avant de bouger une borne.
- Deux niveaux d'exécution : rapide à chaque commit (échantillon), complet la
  nuit (E08-T12).

## 9. Hors périmètre pour l'instant

Progression entre combats, puissance des personnages, gacha, économie : **pas
encore** (voir `CLAUDE.md`, « phases »). Quand ces sujets arrivent, ce fichier
devra définir l'équilibre *entre* combats (courbe de puissance, « power creep »,
gratuit vs payant : E06-T09, E08-T13), pas seulement *dans* un combat.


## 9. Campagne : équilibrage de chaque boss (jalon 1, D-048)

Les bornes du §2 et du §4 s'appliquent **à chaque boss** (`BossRosterBalanceTests`, 100 combats par profil). Un boss se règle, on ne relâche jamais une borne. Outil de réglage : `ZBalayage` (test explicite) balaie atk, multiplicateur et télégraphe de l'attaque de zone, dégâts de l'effet.

| Boss | Profil | Victoires | Un allié K.O. | PV minimum moyen | Durée |
|---|---|---|---|---|---|
| Golem Ancestral | attentif | 100 % | 1 % | 0,28 | 83-86 s |
| | lent (1,5 s) | 100 % | 11 % | 0,16 | 83-98 s |
| | sans Purge | 100 % | 8 % | 0,18 | 83-110 s |
| Reine des Marais | attentif | 100 % | 9 % | 0,31 | 74-90 s |
| | lent | 100 % | 19 % | 0,21 | 74-88 s |
| | sans Purge | 91 % | 62 % | 0,04 | 74-150 s |
| Seigneur de Cendre | attentif | 100 % | 1 % | 0,35 | 102-104 s |
| | lent | 100 % | 15 % | 0,17 | 102-114 s |
| | sans Purge | 100 % | 5 % | 0,23 | 102-112 s |

Pour tous : sans soigneur ou en spammant un seul sort, 0 % de victoire.

**Étoiles** (seuil de dégâts encaissés pour la 3ᵉ étoile : 3700 / 2000 / 3000) : un joueur attentif obtient au moins 2 étoiles dans 85 % des combats et 3 étoiles dans 20 à 85 % ; un joueur lent en obtient moins, et ignorer poisons et brûlures fait perdre les 3 étoiles (testé pour chaque boss).

**Identité des boss** : le 2ᵉ punit l'oubli de la Purge (91 % de victoires sans Purge, mais 62 % de morts) ; le 3ᵉ punit la lenteur (télégraphe de 1,2 s) et enchaîne trois phases.


## 10. Améliorations : équilibrage des choix et de l'économie (jalon 2, D-049)

Joueur de référence : bot attentif (500 ms), 100 combats. Mesure : PV minimum moyen de l'équipe (plus haut = plus facile).

| | Golem | Reine | Seigneur |
|---|---|---|---|
| Jeu de base | 0,28 | 0,31 | 0,35 |
| Soins vifs (T1) | 0,43 | 0,43 | 0,48 |
| Économe (T1) | 0,45 | 0,43 | 0,46 |
| Rempart (T2) | 0,45 | 0,45 | 0,43 |
| Purge vive (T2) | 0,39 | 0,53 | 0,47 |
| Flux de mana (T3) | 0,34 | 0,35 | 0,39 |
| Onde de vie (T3) | 0,48 | 0,31 | 0,35 |
| Équipement maximum seul | 0,47 | 0,61 | 0,52 |
| Tout au maximum | 0,63 | 0,74 | 0,69 |

**Bornes testées** (`UpgradeBalanceTests`, `UpgradeTests`) :
- **Chaque talent seul** : ≥ 95 % de victoires, ≤ 10 % de combats avec un allié K.O., aide d'au moins +0,02 sur un boss (sinon c'est un piège), ne rend pas le jeu trivial (≤ 0,70).
- **Chaque paire d'un palier** : écart ≤ 0,15 sur chaque boss, écart moyen ≤ 0,05 sur la campagne, **aucune option meilleure partout** (le choix existe).
- **Les 8 combinaisons de talents** : toutes viables (≥ 95 %), écart entre la meilleure et la pire ≤ 0,20 par boss, et la meilleure combinaison n'est pas la même partout.
- **Équipement** : plus il y en a, plus c'est facile (niveaux 0, 2, 5 : hausse d'au moins 0,02 à chaque étape) ; aucune piste n'est inutile ; aucune ne rend le jeu trivial seule ; les combats restent d'au moins 45 s.
- **Plafond de puissance** : tout au maximum reste gagnable (≥ 99 %) mais pas invincible (PV minimum ≤ 0,85) ; **sans soigneur : 0 % de victoire, en spammant un sort : ≤ 2 %**, même au maximum.

**Honnêteté sur les bornes** : les bornes du jeu de base (§2, §4) sont fixées a priori. Certaines bornes des améliorations (écart de combinaisons 0,20, plafonds 0,65 / 0,70 / 0,85) ont été posées en connaissant l'ordre de grandeur des mesures : elles décrivent l'intention de conception (« puissant mais jamais trivial ») plutôt qu'une contrainte découverte. Les valeurs de talents ont, elles, été corrigées par les tests (choix dominants).

**Limite connue** : le bot de référence utilise peu le Soin de zone ; la valeur d'Onde de vie repose sur son usage réel, à valider avec un vrai joueur. Les talents dépendent du bot : à réajuster si le bot évolue.

**Économie** : premier niveau d'équipement (50 or) dès la première victoire (100 or) ; équipement complet 4 720 or, talents 950 or, soit ~20 parcours complets de la campagne (rejouer : 50 / 80 / 120 or) au-delà des premières victoires (740 or au maximum).


## 11. Stratégies limitées et enrage (D-050)

**Défaut constaté** : les bornes du §2 et du §4 mesuraient le bot complet (bouclier, purge, soins). Un joueur humain qui n'utilise que le soin de zone gagnait le dernier boss sans y penser : rien ne l'en empêchait. Les bornes ne parlaient que de « l'équipe survit-elle avec un bon joueur », jamais de « un mauvais choix est-il puni ».

**Nouveaux profils de référence** (`ZMesureStrategies.Limited`, décision toutes les 500 ms) :
- **zone seul (< 75 %)** : soin de zone dès qu'un allié est sous 75 % et que le sort est prêt ; rien d'autre ;
- **paresseux** : soin de zone ou soin simple selon les blessés, **jamais de bouclier ni de purge**.

**Bornes ajoutées** (`StrategyDiversityTests`, pour tout boss sauf le premier, tutoriel) :
- le soin de zone seul gagne ≤ 75 % des combats ;
- le profil paresseux perd un allié dans ≥ 25 % des combats ;
- l'écart de morts paresseux − attentif ≥ 15 points ;
- le dernier boss perd ≥ 25 % des combats en soin de zone seul.

| Boss (100 combats) | soin de zone seul | paresseux : allié K.O. | attentif : PV min. |
|---|---|---|---|
| Golem (exempté) | 100 % de victoires | 2 % | 0,28 |
| Reine des Marais | 70 % | 59 % | 0,31 |
| Seigneur de Cendre (avant) | 84 % | 7 % | 0,35 |
| Seigneur de Cendre (après, avec enrage) | 53 % | 39 % | 0,26 |

**Enrage** : mécanique générique (données : `afterMs`, `everyMs`, `pct`). Choix de la valeur par balayage de paramètres avec les contraintes du §4 **plus** celles ci-dessus. Deux constats : accélérer simplement le boss fait basculer le jeu d'un coup (un joueur attentif perd trop d'alliés) ; l'enrage, lui, punit précisément les combats qui s'éternisent, donc les stratégies lentes ou passives, sans toucher au joueur attentif qui finit à 102-114 s.

**Limite** : le bot attentif meurt dans 10 % des combats sur ce boss (exactement la borne). Durcir davantage exige de nouvelles mécaniques (attaques ciblées sur un allié précis, purges plus urgentes…), pas seulement des chiffres.


## 12. Nouvelles mécaniques : mesures avant et après (D-052)

Joueur attentif, 100 combats. Les mesures « avant » sont celles du §9 et du §11.

| Boss | | PV minimum | Allié K.O. | Durée |
|---|---|---|---|---|
| Golem | avant | 0,28 | 1 % | 83-86 s |
| | mécaniques activées, boss non retouché | 0,44 | 0 % | 61-75 s |
| | **après réglage** (7 700 PV, attaque 58) | **0,32** | 3 % | 67-83 s |
| Reine | avant | 0,31 | 9 % | 74-90 s |
| | mécaniques activées, boss non retouché | 0,50 | 1 % | **59-74 s** (sous la borne de 60 s) |
| | **après réglage** (8 450 PV, attaque 37) | **0,29** | 10 % | 78-102 s |
| Seigneur | avant | 0,26 | 10 % | 102-114 s |
| | mécaniques activées, boss non retouché | 0,35 | 9 % | 80-102 s |
| | **après réglage** (9 020 PV) | **0,30** | 7 % | 86-117 s |

**Pourquoi tout devenait plus facile** : l'armure du Garde et la menace concentrent les coups sur le Garde (le plus résistant) ; les critiques du
Mage et la faiblesse magique du Golem accélèrent le combat ; les esquives évitent des coups. **Contre-mesure** : plus de PV au boss (durées de 75-100 s
retrouvées) et une attaque un peu plus forte, réglées ensemble par balayage sous **toutes** les bornes : victoire ≥ 97 % (60 combats), allié K.O. ≤ 9 %, PV minimum
0,18 à 0,36, durée 62-115 s, écart avec le joueur lent et sans purge, et stratégies paresseuses (§11) punies sur les boss avancés.

**Sensibilité** : l'attaque du boss est très sensible (un pas de 10 % fait passer le joueur attentif de « à l'aise » à « il perd un allié sur deux ») : les
valeurs finales sont dans un intervalle étroit. Reine des Marais : 10 % d'allié K.O., exactement la borne.

**Non rééquilibré** : talents et équipement (§10) n'utilisent pas encore critique, esquive ni armure ; ils passent leurs bornes, mesurées avec les nouveaux boss.
