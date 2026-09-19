---
id: E01-T02
epic: E01
titre: Pas de simulation fixe (Game Loop)
type: Tech
priorité: P0
phase: 1
statut: Terminé
taille: S
dépendances: E01-T01
---

# E01-T02 — Pas de simulation fixe (Game Loop)

## Contexte
Le résultat ne doit pas dépendre du FPS de l'appareil ; `Battle.step` ne traite qu'une action par appel.

## Critères d'acceptation
- [x] `FixedStepper` avance la simulation par pas de `FIXED_STEP_MS`
- [x] Garde-fou contre la spirale de la mort
- [x] Test que le pas est inférieur aux plus petits intervalles des données

## Tests automatiques exigés
`fixedStep.test.ts`, `data.test.ts`.

## Impact équilibrage
Aucun.
