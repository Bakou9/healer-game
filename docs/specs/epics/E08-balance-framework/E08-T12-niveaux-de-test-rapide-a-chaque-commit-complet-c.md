---
id: E08-T12
epic: E08
titre: Niveaux de test : rapide à chaque commit, complet chaque nuit
type: Tech
priorité: P1
phase: 2
statut: À faire
taille: M
dépendances: E08-T03
---

# E08-T12 — Niveaux de test : rapide à chaque commit, complet chaque nuit

## Contexte
La batterie complète est coûteuse ; le retour doit rester rapide.

## Critères d'acceptation
- [ ] Mode rapide (échantillon réduit, graines fixes) dans `npm test`
- [ ] Mode complet planifié (CI nocturne)
- [ ] Budget de temps du simulateur mesuré

## Tests automatiques exigés
Mesure de performance.

## Impact équilibrage
Aucun.
