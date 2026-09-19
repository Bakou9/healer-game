---
id: E04-T13
epic: E04
titre: Tests visuels automatisés (captures déterministes)
type: Test
priorité: P1
phase: 2
statut: À faire
taille: M
dépendances: E13-T05
---

# E04-T13 — Tests visuels automatisés (captures déterministes)

## Contexte
Éviter les régressions d'interface : on a déjà vu le journal chevaucher les cartes.

## Critères d'acceptation
- [ ] Pas de temps piloté et graine fixe pour des captures reproductibles
- [ ] Références par taille d'écran
- [ ] Différences présentées et expliquées comme les goldens

## Tests automatiques exigés
Suite de captures dans `npm run check`.

## Impact équilibrage
Aucun.
