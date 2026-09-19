---
id: E08-T06
epic: E08
titre: Test de non-dominance : écart borné et absence de build strictement supérieur
type: Test
priorité: P0
phase: 2
statut: À faire
taille: L
dépendances: E08-T05
---

# E08-T06 — Test de non-dominance : écart borné et absence de build strictement supérieur

## Contexte
Pas de « meilleur build » évident. Bornes initiales proposées : écart de victoires ≤ 10 points, de PV minimum ≤ 12 points, aucun build supérieur sur toutes les métriques (dominance de Pareto).

## Critères d'acceptation
- [ ] Écarts calculés sur le contenu générique
- [ ] Dominance de Pareto détectée par comparaison deux à deux
- [ ] Cas assumés (capstone premium) déclarés explicitement dans les données

## Tests automatiques exigés
Test dans la suite d'équilibrage.

## Impact équilibrage
Oui.
