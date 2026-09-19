---
id: E01-T07
epic: E01
titre: Soin, soin de zone et bouclier
type: Feature
priorité: P0
phase: 1
statut: À faire
taille: S
dépendances: E01-T04
---

# E01-T07 — Soin, soin de zone et bouclier

## Contexte
> **Portage Unity :** réalisé dans la version Phaser (dépôt `Bakou9/healer-game`, commit 91d7beb). À refaire et re-valider dans ce dépôt (epic E14).

Sorts de base du soigneur avec mana et temps de recharge.

## Critères d'acceptation
- [ ] Coût de mana et recharge respectés
- [ ] Bouclier absorbe avant les PV
- [ ] Soin plafonné aux PV max

## Tests automatiques exigés
`Battle.test.ts`.

## Impact équilibrage
Oui : valeurs de base des sorts.
