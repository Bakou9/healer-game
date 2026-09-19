---
id: E01-T03
epic: E01
titre: Événements de combat (Observer)
type: Tech
priorité: P0
phase: 1
statut: Terminé
taille: M
dépendances: E01-T01
---

# E01-T03 — Événements de combat (Observer)

## Contexte
L'UI, les tests et les futurs sons doivent réagir au combat sans que la simulation les connaisse.

## Critères d'acceptation
- [x] `Battle.subscribe` avec des événements typés
- [x] Événements émis avec un état final cohérent (pas de PV négatifs)
- [x] Format texte stable pour les tests golden

## Tests automatiques exigés
`events.test.ts`, scénarios golden.

## Impact équilibrage
Aucun.
