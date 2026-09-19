---
id: E01-T01
epic: E01
titre: Simulation de combat déterministe et pure
type: Tech
priorité: P0
phase: 1
statut: Terminé
taille: M
dépendances: aucune
---

# E01-T01 — Simulation de combat déterministe et pure

## Contexte
Fondation : un combat doit être testable sans navigateur, rejouable et validable côté serveur.

## Critères d'acceptation
- [x] Battle sans import de Phaser, DOM, Math.random ni horloge réelle
- [x] Même seed + mêmes commandes = même résultat
- [x] Vérifié automatiquement par les tests d'architecture

## Tests automatiques exigés
`Battle.test.ts`, `architecture.test.ts`, scénarios golden.

## Impact équilibrage
Aucun (fondation).
