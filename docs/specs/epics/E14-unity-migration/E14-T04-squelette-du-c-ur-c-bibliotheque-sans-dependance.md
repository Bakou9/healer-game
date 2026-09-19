---
id: E14-T04
epic: E14
titre: Squelette du cœur C# : bibliothèque sans dépendance à Unity
type: Tech
priorité: P0
phase: 2
statut: Terminé
taille: M
dépendances: E14-T01
---

# E14-T04 — Squelette du cœur C# : bibliothèque sans dépendance à Unity

## Contexte
Seul le SDK .NET est nécessaire (pas l'Éditeur Unity) : le cœur (simulation, contenu, bots d'équilibrage) est écrit en C# pur, compilé à la fois par dotnet (tests rapides, CI) et par Unity (paquet local). Voir docs/ARCHITECTURE_UNITY.md.

## Critères d'acceptation
- [x] Projet `core/Healer.Combat` (netstandard2.1, compatible Unity) et projet de tests `core/Healer.Combat.Tests`
- [x] Aucune référence à UnityEngine ni à System.Random/DateTime dans le cœur
- [x] `dotnet test` s'exécute (même à vide) et `npm run check` l'appelle

## Tests automatiques exigés
`dotnet test`.

## Impact équilibrage
Aucun.
