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

## 8. Hors périmètre pour l'instant

Progression, puissance des personnages, gacha, économie : **pas encore**
(voir `CLAUDE.md`, « phases »). Quand ces sujets arrivent, ce fichier devra
définir l'équilibre *entre* combats (courbe de puissance, « power creep »), pas
seulement *dans* un combat.
