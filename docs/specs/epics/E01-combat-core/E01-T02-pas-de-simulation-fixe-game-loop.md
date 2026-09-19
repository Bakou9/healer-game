---
id: E01-T02
epic: E01
titre: Pas de simulation fixe (Game Loop)
type: Tech
priorité: P0
phase: 1
statut: À faire
taille: S
dépendances: E01-T01
---

# E01-T02 — Pas de simulation fixe (Game Loop)

## Contexte
> **Portage Unity :** réalisé dans la version Phaser (dépôt `Bakou9/healer-game`, commit 91d7beb). À refaire et re-valider dans ce dépôt (epic E14).

Le résultat ne doit pas dépendre du FPS de l'appareil ; `Battle.step` ne traite qu'une action par appel.

## Critères d'acceptation
- [ ] `FixedStepper` avance la simulation par pas de `FIXED_STEP_MS`
- [ ] Garde-fou contre la spirale de la mort
- [ ] Test que le pas est inférieur aux plus petits intervalles des données

## Tests automatiques exigés
`fixedStep.test.ts`, `data.test.ts`.

## Impact équilibrage
Aucun.
