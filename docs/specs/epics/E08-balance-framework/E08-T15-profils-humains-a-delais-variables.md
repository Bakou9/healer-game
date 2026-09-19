---
id: E08-T15
epic: E08
titre: Profils humains à délais de décision variables
type: Test
priorité: P1
phase: 2
statut: À faire
taille: M
dépendances: E08-T01
---

# E08-T15 — Profils humains à délais de décision variables

## Contexte
Issu de l'analyse de sensibilité de E04-T04 (`docs/REVUES.md`) : le bot de référence décide à intervalle **fixe** (100 à 2500 ms). Résultat non monotone : à 800 ms il fait pire (15 % de combats avec un mort) qu'à 1000 ms (1 %) ou 2500 ms (5 %), parce que son rythme se cale ou non sur la fenêtre de 1,4 s des télégraphes. Un humain n'a pas un métronome : il a un délai **moyen et une dispersion**. Un modèle à pas fixe donne des conclusions artificielles et peut cacher ou inventer des déséquilibres.

## Critères d'acceptation
- [ ] Les profils de joueurs décident après un délai tiré d'une distribution (moyenne + dispersion) via le Rng seedé, donc reproductible
- [ ] Profils rapide, attentif, lent et distrait définis dans `docs/EQUILIBRAGE.md` §4
- [ ] Les résultats deviennent monotones : plus le joueur est lent, moins bon est le résultat (borne testée)
- [ ] Les bornes d'équilibrage (§2) sont ré-évaluées avec ces profils et les écarts éventuels expliqués et validés avec l'utilisateur

## Tests automatiques exigés
Tests de monotonie et de reproductibilité ; rapport de sensibilité par profil.

## Impact équilibrage
Oui : change les profils de référence sur lesquels reposent toutes les bornes. Les références golden des bots changeront (voulu, à expliquer et valider).
