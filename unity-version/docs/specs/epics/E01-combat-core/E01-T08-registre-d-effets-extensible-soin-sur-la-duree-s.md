---
id: E01-T08
epic: E01
titre: Registre d'effets extensible (soin sur la durée, statistiques, étourdissement, vulnérabilité)
type: Feature
priorité: P1
phase: 2
statut: À faire
taille: L
dépendances: E01-T05, E09-T04
---

# E01-T08 — Registre d'effets extensible (soin sur la durée, statistiques, étourdissement, vulnérabilité)

## Contexte
Ajouter des types d'effets sans multiplier les `if` dans Battle : table type → gestionnaire (patron Strategy, déclencheur atteint).

## Critères d'acceptation
- [ ] Registre `kind → gestionnaire` dans `sim/`
- [ ] Chaque nouveau type d'effet = données + test + événements
- [ ] L'effet de poison existant migré sur le registre sans changer les goldens

## Tests automatiques exigés
Tests par type d'effet ; goldens inchangés après migration.

## Impact équilibrage
Oui : chaque nouvel effet est mesuré (E08).
