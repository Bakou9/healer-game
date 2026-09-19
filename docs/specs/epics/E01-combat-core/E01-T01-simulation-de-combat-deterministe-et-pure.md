---
id: E01-T01
epic: E01
titre: Simulation de combat déterministe et pure
type: Tech
priorité: P0
phase: 1
statut: À faire
taille: M
dépendances: aucune
---

# E01-T01 — Simulation de combat déterministe et pure

## Contexte
> **Portage Unity :** réalisé dans la version Phaser (dépôt `Bakou9/healer-game`, commit 91d7beb). À refaire et re-valider dans ce dépôt (epic E14).

Fondation : un combat doit être testable sans navigateur, rejouable et validable côté serveur.

## Critères d'acceptation
- [ ] Battle sans import de Phaser, DOM, Math.random ni horloge réelle
- [ ] Même seed + mêmes commandes = même résultat
- [ ] Vérifié automatiquement par les tests d'architecture

## Tests automatiques exigés
`Battle.test.ts`, `architecture.test.ts`, scénarios golden.

## Impact équilibrage
Aucun (fondation).
