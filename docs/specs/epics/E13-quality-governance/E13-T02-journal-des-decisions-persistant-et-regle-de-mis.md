---
id: E13-T02
epic: E13
titre: Journal des décisions persistant et règle de mise à jour
type: Tech
priorité: P0
phase: 2
statut: Terminé
taille: S
dépendances: aucune
---

# E13-T02 — Journal des décisions persistant et règle de mise à jour

## Contexte
Les décisions de l'utilisateur doivent survivre aux conversations (docs/DECISIONS.md).

## Critères d'acceptation
- [x] Journal daté et numéroté
- [x] Règle dans CLAUDE.md : consigner chaque nouvelle décision et l'associer à un ticket
- [x] Vérifié par test (ids uniques)

## Tests automatiques exigés
`specs.test.ts`.

## Impact équilibrage
Aucun.
