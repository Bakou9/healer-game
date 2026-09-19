# Architecture Unity : cœur C# pur partagé, Unity en couche de présentation

Statut : **proposition d'architecture pour la version Unity** (décision D-029),
qui complète `docs/ARCHITECTURE.md` (monolithe modulaire à frontières strictes,
registres d'extension, contenus par schémas). Tout ce que dit ce dernier reste
vrai ; ce document dit **comment il s'incarne avec Unity**.

## Principe

> Unity affiche et capte les gestes. Il ne décide jamais d'une règle.

Toutes les règles vivent dans un **cœur C# pur** qui n'a **aucune dépendance à
Unity**. Ce cœur est compilé de deux façons à partir des **mêmes sources** :

| Compilé par | Pour | Avantage |
|---|---|---|
| **dotnet** (SDK .NET 8) | tests unitaires, conformité golden, équilibrage, intégration continue, futur serveur | rapide (secondes), sans licence Unity, sans Éditeur ouvert |
| **Unity** (paquet local, asmdef sans références moteur) | le jeu | une seule vérité pour les règles |

Conséquence directe : **la mécanique de test de la version Phaser est reprise
intégralement**, sans dépendre d'un Éditeur Unity lancé.

## Structure du dépôt

```
healer-game-unity/
  CLAUDE.md                règles de travail (préambule systématique inclus)
  docs/                    specs, décisions, équilibrage, UX, patterns, architecture…
  core/                    tout ce qui est indépendant d'Unity
    content/*.json         contenu de jeu : SOURCE DE VÉRITÉ partagée (repris de Phaser)
    golden/*.txt           combats de référence : spécification de conformité (repris de Phaser)
    Healer.Combat/         simulation, événements, RNG, pas fixe, contenu, bots
    Healer.Ui/             logique d'interface pure : formatage tronqué, jetons de mise en page, ciblage
    Healer.Combat.Tests/   tests C# (NUnit ou xUnit) : règles, golden, équilibrage, architecture
  tools/                   Node : cohérence des specs, orchestration des vérifications
  unity/HealerGame/        projet Unity (Assets, Packages, ProjectSettings)
```

Le projet Unity référence `core/Healer.Combat` et `core/Healer.Ui` comme
**paquets locaux** (`file:` dans `Packages/manifest.json`), chacun avec un
`.asmdef` en `noEngineReferences: true`.

## Couches et règles de dépendance (mêmes règles qu'en TypeScript)

1. Dépendances vers le bas uniquement, jamais de cycle :
   `Healer.Combat` ← `Healer.Ui` ← **Client Unity** (scène, UI, VFX, audio, entrées, plateforme).
2. `Healer.Combat` et `Healer.Ui` : **aucun** `UnityEngine`, `System.Random`,
   `DateTime`/`Stopwatch`, accès disque ou réseau. Vérifié par test (E14-T09).
3. Le client ne contient **aucune règle** : il lit l'état de `Battle`, envoie des
   `Command`, écoute les événements.
4. Les `MonoBehaviour` sont **minces** : ils relient, ils ne calculent pas.
5. `Time.deltaTime` n'est utilisé que dans le client, pour alimenter le pas fixe
   (`FixedStepper`) ; la simulation ne lit jamais l'horloge.
6. Les `ScriptableObject` servent à la **présentation** (préréglages d'effets,
   jetons de design), jamais aux données de jeu, qui restent en JSON partagé.

## Patterns (voir `docs/PATTERNS_JEU_VIDEO.md`)

Identiques à la version Phaser : pas fixe, **Command**, **Observer**
(`event Action<BattleEvent>`), data-driven, effets sur la durée (Decorator),
phases de boss (table de transitions), Object Pool (particules, chiffres
flottants). Ajout propre à Unity : **préfabriqués reproductibles** (voir plus bas).

## Utiliser le MCP sans perdre la reproductibilité

Le MCP donne à l'agent la main sur l'Éditeur (scènes, objets, composants,
scripts, console). Risque : un état « fait à la main » que personne ne peut
reconstruire ni tester. Règles :

1. **Le code d'abord.** Tout ce qui peut être un fichier `.cs` en est un ; on
   l'édite comme un fichier, pas via l'interface.
2. **Scènes et préfabriqués reconstruisibles.** Les modèles 3D et les scènes de
   combat sont construits par des **scripts d'Éditeur** (`Assets/Editor/Build*.cs`)
   exécutables en mode batch. Le MCP sert à explorer, ajuster et vérifier, pas
   à être la seule trace de ce qui existe.
3. **Tout est versionné**, `.meta` inclus. Pas de changement d'Éditeur non commité.
4. **Vérifier par les tests**, pas à l'œil seul : compilation batch, tests
   EditMode (budgets de triangles, conformité des préfabriqués), et lecture de
   la console via le MCP.
5. Le MCP donne un accès étendu au poste : on en règle les permissions et on ne
   l'expose jamais hors de la machine (voir `docs/UNITY_SETUP.md`).

## Niveaux de test

| Niveau | Outil | Contenu | Coût |
|---|---|---|---|
| 1. Cœur | `dotnet test` | règles, **golden**, équilibrage, intégrité des données, architecture | secondes |
| 2. Éditeur | Unity EditMode (batch) | compilation, préfabriqués, budgets de triangles, jetons | minutes |
| 3. Jeu | Unity PlayMode / captures | scène jouable, lisibilité, performances | minutes |
| 4. Appareil | Android réel | images par seconde, mémoire, ressenti | manuel |

`npm run check` enchaîne les niveaux disponibles et **échoue** plutôt que de
« passer » quand un outil requis manque.

## Décision de sortie

Ce dépôt existe pour **essayer** Unity (D-027). La version Phaser est conservée.
Le ticket E14-T17 et E11-T07 posent la comparaison et la décision finale, prise
avec l'utilisateur.
