---
id: E02-T04
epic: E02
titre: Application des talents dans la simulation
type: Feature
priorité: P0
phase: 2
statut: À faire
taille: L
dépendances: E02-T01, E01-T08
---

# E02-T04 — Application des talents dans la simulation

## Contexte
Les talents modifient les sorts (valeurs, coûts, recharges) ou ajoutent des effets.

## Critères d'acceptation
- [ ] Modificateurs appliqués sans `if` par talent
- [ ] La simulation reçoit un build en entrée (déterminisme conservé)
- [ ] Événements inchangés ou étendus proprement

## Tests automatiques exigés
Tests unitaires par type de modificateur ; goldens par voie.

## Impact équilibrage
Oui.
