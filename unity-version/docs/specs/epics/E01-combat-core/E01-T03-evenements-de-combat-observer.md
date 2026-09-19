---
id: E01-T03
epic: E01
titre: Événements de combat (Observer)
type: Tech
priorité: P0
phase: 1
statut: À faire
taille: M
dépendances: E01-T01
---

# E01-T03 — Événements de combat (Observer)

## Contexte
> **Portage Unity :** réalisé dans la version Phaser (dépôt `Bakou9/healer-game`, commit 91d7beb). À refaire et re-valider dans ce dépôt (epic E14).

L'UI, les tests et les futurs sons doivent réagir au combat sans que la simulation les connaisse.

## Critères d'acceptation
- [ ] `Battle.subscribe` avec des événements typés
- [ ] Événements émis avec un état final cohérent (pas de PV négatifs)
- [ ] Format texte stable pour les tests golden

## Tests automatiques exigés
`events.test.ts`, scénarios golden.

## Impact équilibrage
Aucun.
