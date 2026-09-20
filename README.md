# Healer Game — version Unity + MCP

Jeu de combat en temps réel où l'on incarne uniquement le **soigneur** (le reste de
l'équipe est en auto-battle). Cette version refait le jeu avec **Unity** (C#, 3D
stylisée) piloté avec un serveur **MCP**, en reprenant toutes les règles, la
documentation et la mécanique de test de la version Phaser.

> Ce dépôt (https://github.com/Bakou9/healer-game) ne contient que le jeu Unity. L'ancienne
> version **Phaser** (TypeScript) en a été retirée (D-045) ; elle reste dans l'historique Git,
> à l'étiquette `phaser-archive`.

## Où en est-on ?

| Élément | État |
|---|---|
| Documents, specs (14 epics, 142 tickets), décisions, registre des revues | repris et adaptés |
| Cœur C# pur (`core/Healer.Combat`) | **porté ; reproduit à l'identique les 7 combats de référence** |
| Tests C# (`core/Healer.Combat.Tests`) | 691 tests : conformité golden, règles, équilibrage de chaque boss et de chaque choix, stratégies limitées, enrage, progression, atelier, sauvegarde, navigation, entrées, mise en page, sons, musique, animations, réglages |
| Environnement | SDK .NET installé ; Unity Hub installé ; Éditeur Unity 6.3 LTS en cours d'installation |
| Projet Unity (`unity/HealerGame`, Unity 6.3 LTS) | **créé** ; licence Unity Personal active |
| **Jeu Unity jouable** (scène 3D, modèles « figurine », interface, exécutable Windows) | **oui** : voir `docs/TESTER_LE_JEU.md` |
| MCP | à choisir (D-028) |

## Documents à lire

- `CLAUDE.md` : règles de travail (préambule systématique : remise en question des specs, question d'équilibrage à chaque ticket).
- `docs/MIGRATION_UNITY.md` : correspondance Phaser → Unity (chemins, commandes, concepts).
- `docs/ARCHITECTURE_UNITY.md` : cœur C# pur partagé, Unity en couche de présentation.
- `docs/UNITY_SETUP.md` : installation et choix du MCP.
- `docs/ART_3D.md` : guide des modèles 3D « user friendly ».
- `docs/specs/README.md` : vision, epics, tickets. `docs/DECISIONS.md` : journal des décisions. `docs/REVUES.md` : registre des revues.
- `docs/MECANIQUES.md` : **référence complète des mécaniques** (formules, valeurs, sorts, effets, boss, progression, et ce qui n'existe pas : critiques, résistances, armure en %…).
- `docs/EQUILIBRAGE.md`, `docs/UX.md`, `docs/PATTERNS_JEU_VIDEO.md` : équilibre, UX, patterns (repris de la version Phaser).

## Commandes

| Commande | Effet |
|---|---|
| `npm install` | installe l'outillage Node (cohérence des specs) |
| `npm run check` | specs + tests du cœur C# (`dotnet test`) : **doit être vert avant de conclure** |
| `npm run specs:index` | régénère l'index des specs après un changement de ticket |
| `dotnet test core/Healer.Combat.Tests` | tests du cœur seuls (secondes) |

Prérequis : Node.js 20, SDK .NET 8. Pour Unity : Unity 6 (6000.x) et un MCP (voir
`docs/UNITY_SETUP.md`).

## Structure

```
CLAUDE.md, README.md
docs/                 specs, décisions, équilibrage, UX, architecture, art 3D
core/
  content/*.json      contenu de jeu (source de vérité partagée, repris de Phaser)
  golden/*.txt        combats de référence (spécification de conformité)
  Healer.Combat/      simulation, RNG, pas fixe, contenu, bot de référence (C# pur)
  Healer.Combat.Tests/  tests C#
tools/                Node : cohérence des specs, orchestration des vérifications
unity/                projet Unity (à créer, voir docs/UNITY_SETUP.md)
```
