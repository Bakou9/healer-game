---
id: E01-T04
epic: E01
titre: Commandes du joueur (Command) horodatées
type: Tech
priorité: P0
phase: 1
statut: Terminé
taille: S
dépendances: E01-T01
---

# E01-T04 — Commandes du joueur (Command) horodatées

## Contexte
Le joueur, le bot et les tests utilisent le même chemin ; base du replay et de la validation serveur.

## Critères d'acceptation
- [x] `Command { timeMs, skillId, targetId }` via `issueCommand`
- [x] Insertion triée, traitement déterministe

## Tests automatiques exigés
`Battle.test.ts`.

## Impact équilibrage
Aucun.
