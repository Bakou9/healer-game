---
id: E08-T01
epic: E08
titre: Profils de joueurs de référence (attentif, lent, sans purge, passif, spam)
type: Test
priorité: P0
phase: 1
statut: Terminé
taille: M
dépendances: E01-T01
---

# E08-T01 — Profils de joueurs de référence (attentif, lent, sans purge, passif, spam)

## Contexte
Un test d'équilibrage compare des comportements de joueurs, pas des nombres seuls.

## Critères d'acceptation
- [x] Bot avec délai de décision humain (500 ms)
- [x] Profils lent, sans purge, passif et spam
- [x] Bornes encodées dans `regression.test.ts`

## Tests automatiques exigés
`regression.test.ts` (équilibrage).

## Impact équilibrage
Oui : c'est la base de l'équilibrage.
