# Migration Phaser → Unity : table de correspondance

La version **Phaser** reste intacte et consultable : c'est **le reste du dépôt unique**
https://github.com/Bakou9/healer-game (racine, `src/`, `docs/`…), dont la version Unity
est un sous-dossier (`unity-version/`, décision D-033). Dernier état de Phaser repris : `91d7beb`.
**Ne jamais modifier ni supprimer la version Phaser.** Les chemins Phaser ci-dessous sont
relatifs à la **racine du dépôt** (`..` depuis `unity-version/`).

Les documents de ce dépôt ont été repris tels quels ; beaucoup citent encore des
chemins et des commandes de la version Phaser. **Règle** : quand un document
cite un chemin, une commande ou un test Phaser, appliquer cette table. Un
document est corrigé au fil des tickets E14, pas en bloc.

## Chemins

| Phaser (TypeScript) | Unity (ce dépôt) |
|---|---|
| `src/sim/*.ts` (Battle, events, rng, fixedStep, types, encounter) | `core/Healer.Combat/` |
| `src/sim/referenceHealerBot.ts` | `core/Healer.Combat/Balance/` |
| `src/data/*.json` | `core/content/*.json` (repris à l'identique) |
| `src/testing/golden/*.txt` | `core/golden/*.txt` (repris à l'identique) |
| `src/testing/*.test.ts`, `src/sim/*.test.ts` | `core/Healer.Combat.Tests/` |
| `src/ui/format.ts`, `layout.ts`, `targeting.ts` | `core/Healer.Ui/` |
| `src/scenes/*` (Phaser) | `unity/HealerGame/Assets/` (scènes, UI, VFX) |
| `src/testing/specs*.ts`, `scripts/specs-index.ts` | `tools/specs/` |

## Commandes

| Phaser | Unity (ce dépôt) |
|---|---|
| `npm run check` | `npm run check` (specs, puis `dotnet test` du cœur ; Unity batch en option) |
| `npm test` | `npm run specs:test` et `dotnet test core/Healer.Combat.Tests` |
| `npm run specs:index` | `npm run specs:index` (identique) |
| `npm run test:update-golden` | script de régénération des golden **à écrire en C#** (E14-T06), soumis au même protocole (accord de l'utilisateur) |
| `npm run dev` (rechargement à chaud) | Unity Éditeur ouvert ; les scripts C# se recompilent à l'enregistrement |

## Concepts

| Phaser | Unity |
|---|---|
| `Phaser.Scene` | scène Unity + `MonoBehaviour` minces |
| `FixedStepper` (accumulateur) | même classe dans le cœur, alimentée par `Time.deltaTime` du client |
| `battle.subscribe(listener)` | `event Action<BattleEvent>` |
| `FloatingTextPool`, particules Phaser | pool d'objets + `ParticleSystem` / VFX Graph |
| zoom de caméra pour la netteté | résolution native Unity (Canvas Scaler, URP) |
| `?dpr=2` de vérification | *Device Simulator* de l'Éditeur |

## Ce qui ne change pas (à respecter à l'identique)

Le préambule systématique (remise en question des specs, question d'équilibrage),
le journal des décisions, le registre des revues, la définition de « terminé »,
le protocole de non-régression (golden, jamais modifiés pour « faire passer »),
les valeurs tronquées à hauteur humaine, les principes UX, la définition de
l'équilibre (`docs/EQUILIBRAGE.md`), les patterns de jeu vidéo.

## Vérification de fidélité du portage

Le portage du cœur est **correct si et seulement si** :
1. les **7 fichiers de `core/golden`** sont reproduits ligne à ligne (E14-T06) ;
2. les **bornes d'équilibrage** donnent les mêmes mesures que celles de `docs/EQUILIBRAGE.md` §5 (E14-T08) ;
3. les **tests d'intégrité des données** passent sur les mêmes fichiers JSON (E14-T07).
