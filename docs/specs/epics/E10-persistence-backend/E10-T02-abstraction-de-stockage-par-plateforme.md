---
id: E10-T02
epic: E10
titre: Abstraction de stockage par plateforme
type: Tech
priorité: P1
phase: 3
statut: À faire
taille: S
dépendances: E09-T07
---

# E10-T02 — Abstraction de stockage par plateforme

## Contexte
Web, Android et Steam n'ont pas le même stockage (patron Adapter).

## Critères d'acceptation
- [ ] Interface de stockage injectée
- [ ] Implémentations web et mémoire (tests)
- [ ] Aucun accès direct au stockage hors de l'adaptateur

## Tests automatiques exigés
Tests avec stockage mémoire.

## Impact équilibrage
Aucun.
