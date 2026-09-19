---
id: E09-T07
epic: E09
titre: Injection de dépendances (catalogues, horloge, RNG, stockage)
type: Tech
priorité: P1
phase: 2
statut: À faire
taille: M
dépendances: E09-T02
---

# E09-T07 — Injection de dépendances (catalogues, horloge, RNG, stockage)

## Contexte
Fin des imports globaux de JSON dans `Battle` ; permet les tests avec catalogues alternatifs.

## Critères d'acceptation
- [ ] Battle reçoit ses catalogues par la rencontre
- [ ] Aucun singleton
- [ ] Tests avec des catalogues minimaux

## Tests automatiques exigés
Tests unitaires ciblés.

## Impact équilibrage
Aucun.
