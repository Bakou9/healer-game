# Instructions pour un agent travaillant sur ce projet

Ce fichier s'adresse à un assistant IA (Claude Code ou équivalent) amené à
modifier ce dépôt. Contexte complet dans `README.md`.

## Ce qu'est ce projet

Un prototype de jeu gacha "healer" : combat en temps réel où le joueur ne
contrôle que le soigneur (le reste de l'équipe est en auto-battle). Stack :
TypeScript + Phaser 3 + Vite, packagé Android via Capacitor. Un futur build
Steam viendra plus tard (Electron/Tauri + steamworks.js), pas encore ici.

Le projet avance par phases courtes et testables (voir README). **Ne pas
anticiper des fonctionnalités de phases suivantes** (gacha, backend, IAP)
sans qu'on le demande explicitement : la priorité actuelle est de valider
que la boucle de combat est amusante.

## Règles d'architecture à respecter

1. **`src/sim/` ne doit jamais importer Phaser.** C'est la simulation de
   combat : pure, testable, déterministe. `src/scenes/` fait uniquement le
   rendu et la capture des taps ; il lit l'état de `Battle` et lui envoie des
   commandes via `issueCommand()`. Si une modification de gameplay nécessite
   de toucher à `BattleScene.ts` ET `Battle.ts`, c'est normal, mais la logique
   de règles (dégâts, mana, cooldowns) reste dans `sim/`.

2. **Tout le contenu de jeu est dans `src/data/*.json`**, pas codé en dur.
   Un nouveau personnage, sort ou boss s'ajoute en éditant ou en créant un
   JSON, pas en modifiant `Battle.ts`. Si une fonctionnalité ne peut pas être
   exprimée en données (ex : un nouveau *type* d'effet), étendre les types
   dans `src/sim/types.ts` et le traiter dans `Battle.ts` de façon générique
   (pas un `if` spécifique à un seul sort).

3. **Déterminisme.** `Battle` prend un `seed` et ne doit utiliser `Math.random()`
   nulle part — uniquement le `Rng` fourni (voir `rng.ts`). Toute nouvelle
   mécanique aléatoire doit passer par ce générateur, sinon les tests de
   déterminisme et un futur rejouable/anti-triche côté serveur cassent.

4. **Pas de calcul de gameplay côté client qui devra un jour être autoritatif.**
   Ce prototype n'a pas encore de serveur, mais on garde `Battle` écrite comme
   si elle allait tourner côté serveur un jour (pas d'accès DOM/window dedans).

## Patterns de développement (OBLIGATOIRE)

Le fichier de référence est **`docs/PATTERNS_JEU_VIDEO.md`**. Avant d'écrire du
code, le consulter : il dit quel pattern appliquer (Game Loop à pas fixe,
Command, Observer, data-driven, séparation modèle/vue…), lesquels sont différés
(ECS, Behavior Tree, Object Pool…) avec leur déclencheur, et lesquels sont à
éviter (Singleton, état global). Dans le compte rendu de chaque tâche, **citer
le ou les patterns appliqués**. Ne pas introduire un pattern différé avant que
son déclencheur soit atteint. Si on en adopte un, mettre le fichier à jour.

## Équilibrage et valeurs lisibles (OBLIGATOIRE)

- **`docs/EQUILIBRAGE.md`** définit ce qu'est un jeu équilibré pour ce projet
  (6 critères mesurables, profils de joueurs de référence, boutons de réglage,
  procédure). Le lire avant de toucher à `src/data/*.json`, à une règle de
  combat ou au bot de référence, et rendre compte de l'avant/après des mesures.
  Ne jamais relâcher une borne d'équilibrage pour faire passer un test.
- **Valeurs à hauteur humaine.** Tout nombre affiché au joueur est **tronqué**
  (jamais arrondi vers le haut), sans décimales inutiles, abrégé si long
  (`7000`, `12,3k`, `4,9s`) : toujours via `src/ui/format.ts`
  (`formatNumber`, `formatSeconds`, `formatRatio`), jamais de `toFixed`,
  `Math.round` ou nombre brut dans un texte affiché. Dans les données JSON,
  toute quantité de jeu est un entier rond ; seuls `multiplier` et les ratios
  (`…Ratio`) peuvent être décimaux (vérifié par un test).

## Protocole de non-régression (OBLIGATOIRE)

Les tests détectent les régressions ; l'utilisateur veut **comprendre chacune**.

- `npm run check` = types + tests + build. Doit être vert avant de conclure.
- Les combats de référence sont figés dans `src/testing/golden/*.txt` (un
  événement par ligne) et comparés à chaque `npm test`. Un test d'équilibrage
  vérifie aussi que le combat reste gagnable et tendu avec le bot de référence.
- **Quand un test échoue après une modification :**
  1. Ne JAMAIS modifier un test, une valeur d'équilibrage attendue ou les
     fichiers golden pour « faire passer ». Ne pas contourner le test.
  2. Rapporter à l'utilisateur : quel test, ce qui était attendu, ce qui est
     obtenu, **quelle modification en est la cause**, et un verdict argumenté :
     changement **voulu** (conséquence normale de la demande) ou **accidentel**.
  3. Accidentel : corriger le code. Voulu : attendre l'accord de l'utilisateur,
     puis `npm run test:update-golden`, et résumer ce qui a changé.
- Changement de règle voulu : annoncer à l'avance que les golden vont bouger et pourquoi.
- Nouvelle mécanique → nouveau test + nouveau scénario dans `src/testing/scenarios.ts`.
- Bug corrigé → ajouter un test qui échouait avant la correction.
- `src/testing/architecture.test.ts` interdit dans `src/sim/` : import de Phaser,
  `Math.random`, `Date.now`/`performance.now`, accès DOM. Ne pas l'affaiblir.

## Développement local

`npm run dev` (Vite, http://localhost:5173, port verrouillé) recharge la page
automatiquement à chaque modification de fichier, y compris dans un navigateur
externe : ne pas demander à l'utilisateur de rafraîchir, et ne pas relancer le
serveur après chaque changement.

## Workflow attendu pour toute modification

1. Avant de commencer, lancer `npm test` pour confirmer l'état de référence.
2. Modifier le code. Si l'équilibrage change (nouveaux nombres dans les JSON
   de `src/data/`, nouvelles compétences...), utiliser ou étendre
   `src/sim/referenceHealerBot.ts` : c'est un bot de soin "raisonnable" servant
   à vérifier qu'un combat reste gagnable (et pas trivial) sans avoir à jouer
   à la main à chaque changement.
3. `npm run check` (types + tests + build) doit passer avant de considérer une
   tâche terminée. Ajouter un test quand on change une règle de combat
   (dégâts, mana, cooldown, condition de victoire/défaite). En cas d'échec,
   appliquer le protocole de non-régression ci-dessus.
5. Ne pas committer `node_modules/`, `dist/`, ni le dossier `android/` généré
   sauf si des fichiers natifs y ont été modifiés intentionnellement.

## Pièges connus de cet environnement

- Les identifiants (`id`) dans les JSON de `src/data/` sont référencés en dur
  ailleurs (ex. `"healer"` dans `BattleScene.ts`, `"tank"` dans les tests).
  Renommer un id casse ces références — grep avant de renommer.
- `Battle.step(dtMs)` traite les attaques automatiques et le tick du boss une
  seule fois par appel : ne jamais appeler `step()` avec un `dtMs` plus grand
  que les intervalles définis dans les données (`tickMs` du boss, intervalle
  d'attaque des alliés), sous peine de "sauter" des actions. La scène passe par
  `FixedStepper` (pas fixe de `FIXED_STEP_MS`), et un test vérifie que ce pas
  reste inférieur aux intervalles des données. Les tests utilisent des pas de ~100ms.
- Les événements de combat (`sim/events.ts`) alimentent les tests golden : si on
  change leur format ou leur ordre d'émission, les golden changent — c'est un
  changement à signaler à l'utilisateur, pas à régénérer en silence.
