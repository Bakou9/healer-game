---
id: E08-T03
epic: E08
titre: Générateur de l'espace de builds (énumération ou échantillonnage seedé)
type: Test
priorité: P0
phase: 2
statut: À faire
taille: M
dépendances: E02-T01
---

# E08-T03 — Générateur de l'espace de builds (énumération ou échantillonnage seedé)

## Contexte
Le nombre de builds explose ; il faut couvrir l'espace de façon reproductible.

## Critères d'acceptation
- [ ] Énumération exhaustive si l'espace est petit, sinon échantillonnage seedé stratifié
- [ ] Couvre chaque talent au moins N fois
- [ ] Reproductible à graine égale

## Tests automatiques exigés
Tests de couverture de l'espace.

## Impact équilibrage
Oui.
