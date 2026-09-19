---
id: E08-T05
epic: E08
titre: Test de viabilité : chaque build atteint un plancher
type: Test
priorité: P0
phase: 2
statut: À faire
taille: M
dépendances: E08-T03, E08-T04
---

# E08-T05 — Test de viabilité : chaque build atteint un plancher

## Contexte
Aucun choix de spécialisation ne doit être un piège. Plancher initial proposé : ≥ 85 % de victoires avec son bot sur le contenu de référence.

## Critères d'acceptation
- [ ] Tous les builds échantillonnés passent le plancher
- [ ] Échec = liste des builds fautifs avec leurs métriques
- [ ] Plancher documenté et modifiable seulement avec accord

## Tests automatiques exigés
Test dans la suite d'équilibrage.

## Impact équilibrage
Oui.
