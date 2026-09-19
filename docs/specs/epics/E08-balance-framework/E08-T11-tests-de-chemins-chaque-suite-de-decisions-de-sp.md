---
id: E08-T11
epic: E08
titre: Tests de chemins : chaque suite de décisions de spécialisation reste équilibrée
type: Test
priorité: P0
phase: 2
statut: À faire
taille: L
dépendances: E02-T03
---

# E08-T11 — Tests de chemins : chaque suite de décisions de spécialisation reste équilibrée

## Contexte
L'équilibre doit tenir à chaque niveau, pas seulement pour les builds complets : chaque suite de choix possible est validée contre le contenu de ce niveau.

## Critères d'acceptation
- [ ] Chaque build partiel atteignable est évalué au niveau où il existe
- [ ] Transitions et respec testées
- [ ] Aucun choix précoce ne condamne le joueur

## Tests automatiques exigés
Test de chemins par niveau.

## Impact équilibrage
Oui : c'est l'exigence « entre chaque décision de spécialisation ».
