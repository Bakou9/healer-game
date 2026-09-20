---
id: E08-T01
epic: E08
titre: Profils de joueurs de référence (attentif, lent, sans purge, passif, spam)
type: Test
priorité: P0
phase: 1
statut: À faire
taille: M
dépendances: E01-T01
---

# E08-T01 — Profils de joueurs de référence (attentif, lent, sans purge, passif, spam)

## Contexte
> **Portage Unity :** réalisé dans la version Phaser (dépôt `Bakou9/healer-game`, commit 91d7beb). À refaire et re-valider dans ce dépôt (epic E14).

Un test d'équilibrage compare des comportements de joueurs, pas des nombres seuls.

## Critères d'acceptation
- [ ] Bot avec délai de décision humain (500 ms)
- [ ] Profils lent, sans purge, passif et spam
- [ ] Bornes encodées dans `regression.test.ts`

## Tests automatiques exigés
`regression.test.ts` (équilibrage).

## Impact équilibrage
Oui : c'est la base de l'équilibrage.
