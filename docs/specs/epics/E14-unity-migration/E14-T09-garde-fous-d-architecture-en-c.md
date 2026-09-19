---
id: E14-T09
epic: E14
titre: Garde-fous d'architecture en C#
type: Test
priorité: P0
phase: 2
statut: À faire
taille: M
dépendances: E14-T04
---

# E14-T09 — Garde-fous d'architecture en C#

## Contexte
Reprise des interdits : la simulation reste pure et déterministe.

## Critères d'acceptation
- [x] Le cœur ne référence ni UnityEngine, ni System.Random, ni DateTime/Stopwatch, ni accès fichier/réseau
- [x] Vérifié par test (analyse des sources et des références du projet)
- [ ] Frontières entre modules de docs/ARCHITECTURE_UNITY.md testées (asmdef)

## Tests automatiques exigés
Tests d'architecture C#.

## Impact équilibrage
Aucun.
