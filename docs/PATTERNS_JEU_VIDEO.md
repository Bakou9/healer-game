# Patterns de développement de jeu vidéo — référence du projet

Ce fichier est **contractuel** : tout agent (ou humain) qui ajoute du code dans
ce dépôt s'y réfère avant d'écrire, et cite dans son compte rendu le ou les
patterns appliqués. Il est référencé depuis `CLAUDE.md`.

## Comment l'utiliser

1. Avant d'ajouter une fonctionnalité, chercher dans le **tableau de décision**
   le pattern qui correspond au problème.
2. Un pattern **Adopté** s'applique tel que décrit. Un pattern **Différé** ne
   s'introduit **que si son déclencheur est atteint** (sinon c'est de la
   sur-ingénierie : le prototype doit rester petit et testable).
3. Un pattern **À éviter** ne s'utilise pas sans en discuter avec l'utilisateur.
4. Tout nouveau pattern adopté vient avec ses tests, et ce tableau est mis à jour.

Principe directeur : **le pattern le plus simple qui résout le problème
d'aujourd'hui**. Les sections « Quand NE PAS l'utiliser » comptent autant que
les autres.

## Tableau de décision

| Pattern | Statut | Où dans le projet | Déclencheur (si différé) |
|---|---|---|---|
| Game Loop à pas fixe | **Adopté** | `sim/fixedStep.ts`, `BattleScene.update` | — |
| Command | **Adopté** | `Command`, `Battle.issueCommand` | — |
| Observer (événements) | **Adopté** | `sim/events.ts`, `Battle.subscribe` | — |
| Data-driven (Prototype / Factory par données) | **Adopté** | `src/data/*.json`, `sim/encounter.ts` | — |
| Séparation Modèle / Vue (MVC/MVP) | **Adopté** | `sim/` (modèle) vs `scenes/` (vue + saisie) | — |
| Facade | **Adopté** | getters de `Battle` (`getAllies`, `getTelegraph`…) | — |
| State / State Machine | Partiel | `BattleResult` | Phases de boss, ou états d'un personnage (étourdi, mort…) |
| Dependency Injection | Partiel | `Rng` et `EncounterDef` injectés | Besoin de tester avec un autre catalogue de compétences |
| Strategy | Différé | — | Un 2e type d'effet de compétence ou de comportement de boss qui exige du code |
| Decorator (buffs/debuffs) | Différé | — | Arrivée des effets sur la durée (poison, marque) : liste d'effets actifs pilotée par les données |
| Object Pool | Différé | — | Objets Phaser créés/détruits en boucle (chiffres de dégâts, particules) |
| Adapter | Différé | — | Services de plateforme (Steam, Android, sauvegarde) |
| ECS | Différé | — | Dizaines d'entités hétérogènes, ou combinatoire de comportements (vagues d'ennemis) |
| Behavior Tree / Utility AI / GOAP | Différé | — | Décisions du boss/alliés dépendant de l'état du combat, pas d'un pattern fixe |
| Builder, Abstract Factory | Non nécessaire | — | — |
| Singleton, Service Locator, état global | **À éviter** | — | — |
| Bus d'événements global (Mediator) | **À éviter** (pour l'instant) | — | Plusieurs scènes qui doivent communiquer |
| Spatial partitioning, LOD, multithread | Non pertinent | — | Combat sans déplacement, peu d'entités |

## Patterns adoptés

### Game Loop à pas fixe (Fixed Timestep)
- **Problème :** le rendu tourne à une cadence variable (30/60/120 FPS, ralentissements) ; si la simulation suit ce delta, le résultat dépend de l'appareil.
- **Application :** `FixedStepper.advance(delta, step)` accumule le temps et appelle `Battle.step(FIXED_STEP_MS)` par pas identiques. La scène ne passe **jamais** un delta brut à `Battle`.
- **Garde-fous :** limite de pas par frame (pas de « spirale de la mort »), test que le pas est inférieur aux plus petits intervalles des données.
- **Ne pas :** mettre de logique de jeu dans `update()` de la scène, ni lire l'horloge réelle dans `sim/`.
- **Anti-pattern :** `position += vitesse` multiplié par le delta de rendu pour de la logique de règles.

### Command
- **Problème :** découpler l'intention (tap du joueur, bot, replay) de l'exécution.
- **Application :** une action du joueur est un objet `Command { timeMs, skillId, targetId }` envoyé à `issueCommand`. Le joueur, le bot de référence et les tests utilisent le **même** chemin. Conséquences : replay, tests déterministes, validation serveur possible.
- **Ne pas :** appeler directement des méthodes internes de `Battle` depuis la scène.
- **Étendre :** une nouvelle action = un nouveau type de commande sérialisable (données simples, pas de fonctions).

### Observer (événements)
- **Problème :** l'UI, les sons, les stats et les tests doivent réagir au combat sans que `Battle` les connaisse.
- **Application :** `battle.subscribe(listener)` reçoit des `BattleEvent` typés (`healed`, `unitDamaged`, `battleEnded`…). Les tests de non-régression enregistrent ce flux. Les chiffres flottants et sons futurs s'y abonneront.
- **Règles :** un listener **ne modifie jamais** la simulation ; tout nouvel événement est ajouté à `events.ts` (union typée + `formatEvent`) et couvert par un test ; toujours se désabonner à la destruction d'une scène.
- **Ne pas :** créer un bus global. L'émetteur est l'objet `Battle`, pas un singleton.

### Data-driven (Prototype / Factory par données)
- **Problème :** ajouter du contenu (personnage, sort, boss) sans toucher au code.
- **Application :** le contenu vit dans `src/data/*.json` ; `createEncounter` est la fabrique qui assemble une rencontre. Ajouter du contenu = ajouter/éditer un JSON.
- **Règle :** si le contenu demande un *nouveau type* d'effet, étendre les types de `sim/types.ts` et le traiter **génériquement** dans `Battle` (jamais de `if (skill.id === "…")`).
- **Garde-fous :** `testing/data.test.ts` valide la structure et les ids référencés en dur.

### Séparation Modèle / Vue
- `sim/` = modèle pur (aucun import Phaser, aucun DOM, aucune horloge réelle, aucun `Math.random`) — **vérifié automatiquement** par `testing/architecture.test.ts`.
- `scenes/` = vue et saisie : lit l'état, envoie des `Command`, ne calcule aucune règle.

### Facade
- `Battle` expose des getters qui renvoient des copies/vues (`UnitState`), pas ses structures internes. L'UI ne dépend donc pas de la représentation interne.

## Patterns partiels

### State / State Machine
- **Aujourd'hui :** `BattleResult` (`ongoing` → `victory` | `defeat`) suffit.
- **Quand passer à une vraie FSM :** dès qu'il y a des phases de boss ou des états de personnage avec transitions. Alors : états explicites (enum ou union typée), une table de transitions, un test par transition. Éviter les booléens en cascade (`isStunned && !isDead && …`).

### Dependency Injection
- **Aujourd'hui :** le générateur aléatoire et la rencontre sont injectés dans `Battle` ; le catalogue de compétences est importé directement.
- **Règle :** toute nouvelle dépendance de `Battle` (horloge, catalogue, générateur) s'injecte par le constructeur ou `EncounterDef` — jamais d'import de singleton.

## Patterns différés (avec leur déclencheur)

- **Strategy** — Si un deuxième type d'effet ou de comportement de boss nécessite du code, remplacer les `if` par une table `type → gestionnaire`, dans `sim/`, et non par un `switch` dispersé.
- **Decorator / effets actifs** — Poison, marque, étourdissement : modéliser comme une **liste d'effets actifs** sur l'unité (données : durée, tick, modificateur), pas comme des sous-classes. La compétence Purge (déjà dans les données, sans effet à retirer aujourd'hui) s'appuiera dessus.
- **Object Pool** — Dès que la scène crée puis détruit des objets Phaser à chaque événement (chiffres de dégâts, particules) : réserve d'objets réutilisés, sinon saccades sur mobile d'entrée de gamme.
- **Adapter** — Au moment de Steam/Android : une interface `PlatformServices` (sauvegarde, succès…) implémentée par plateforme. Aucun appel Capacitor/Steam dans `sim/`.
- **ECS** — Pas justifié pour 4 alliés + 1 boss. À envisager pour des dizaines d'entités hétérogènes.
- **Behavior Tree / Utility AI / GOAP** — Le boss suit un pattern en données. Une Utility AI (score par action) est le premier candidat si le boss doit décider selon l'état du combat (qui viser, quand enrager). GOAP : pas pertinent pour ce type de jeu.

## À éviter

- **Singleton / Service Locator / état global mutable** : dépendances cachées, tests fragiles, incompatibles avec une simulation rejouable côté serveur.
- **Aléatoire ou horloge réelle dans `sim/`** : casse le déterminisme (seul `Rng` est autorisé).
- **Hériter pour varier un comportement** (`class FireBoss extends Boss`) : préférer les données et la composition.
- **Logique de règles dans la scène**, ou lecture de l'état de rendu pour décider d'une règle.

## Checklist avant de rendre du code

- [ ] Quel pattern de ce fichier s'applique ? Est-il cité dans mon compte rendu ?
- [ ] `sim/` reste pur (test d'architecture vert) ?
- [ ] Contenu ajouté via `src/data/*.json` plutôt qu'en dur ?
- [ ] Nouvelle règle ou événement → test ajouté ? Nouvelle mécanique → scénario golden ajouté ?
- [ ] `npm run check` vert ; sinon, régressions expliquées à l'utilisateur (voir `CLAUDE.md`, « Protocole de non-régression »).
- [ ] Ce tableau est à jour si j'ai adopté un pattern différé.
