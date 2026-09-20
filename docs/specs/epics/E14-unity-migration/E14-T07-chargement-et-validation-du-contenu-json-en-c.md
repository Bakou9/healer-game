---
id: E14-T07
epic: E14
titre: Chargement et validation du contenu JSON en C#
type: Feature
priorité: P0
phase: 2
statut: Terminé
taille: M
dépendances: E14-T04
---

# E14-T07 — Chargement et validation du contenu JSON en C#

## Contexte
Le contenu (`core/content/*.json`) reste la source de vérité partagée : mêmes fichiers pour le cœur et pour Unity.

## Critères d'acceptation
- [x] Chargement typé (Newtonsoft.Json), erreurs avec le chemin exact
- [x] Tests d'intégrité repris : ids uniques, ids référencés, effets existants, phases triées, entiers ronds (sauf multiplicateurs et ratios), pas de simulation vs intervalles
- [x] Aucune donnée de jeu copiée dans le code C#

## Tests automatiques exigés
Tests d'intégrité C#.

## Impact équilibrage
Non (mêmes données).
