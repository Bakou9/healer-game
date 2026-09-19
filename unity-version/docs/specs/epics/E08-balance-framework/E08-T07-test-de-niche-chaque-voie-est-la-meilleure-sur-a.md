---
id: E08-T07
epic: E08
titre: Test de niche : chaque voie est la meilleure sur au moins un archétype
type: Test
priorité: P0
phase: 2
statut: À faire
taille: M
dépendances: E03-T01, E08-T05
---

# E08-T07 — Test de niche : chaque voie est la meilleure sur au moins un archétype

## Contexte
Chaque choix doit avoir une raison d'être : un terrain où il brille. Seuil initial proposé : voie dans le meilleur quart des builds sur au moins un archétype, d'au moins 5 points de victoires.

## Critères d'acceptation
- [ ] Matrice voie × archétype publiée dans le rapport
- [ ] Échec si une voie n'est jamais la meilleure
- [ ] Échec si une voie devient obligatoire pour un archétype

## Tests automatiques exigés
Test dans la suite d'équilibrage.

## Impact équilibrage
Oui.
