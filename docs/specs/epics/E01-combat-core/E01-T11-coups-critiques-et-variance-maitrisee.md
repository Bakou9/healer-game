---
id: E01-T11
epic: E01
titre: Coups critiques et variance maîtrisée
type: Feature
priorité: P2
phase: 2
statut: À faire
taille: S
dépendances: E01-T01
---

# E01-T11 — Coups critiques et variance maîtrisée

## Contexte
Ajouter de la variance sans rendre l'équilibrage imprévisible.

## Critères d'acceptation
- [ ] Critiques via le Rng seedé
- [ ] Variance mesurée et bornée (écart-type du résultat)
- [ ] Réglable en données

## Tests automatiques exigés
Tests de distribution sur 1000 seeds.

## Impact équilibrage
Oui : la variance entre dans les métriques (E08-T04).
