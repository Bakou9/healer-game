---
id: E13-T01
epic: E13
titre: Filet de non-régression : golden, équilibrage, données, architecture
type: Test
priorité: P0
phase: 1
statut: À faire
taille: L
dépendances: aucune
---

# E13-T01 — Filet de non-régression : golden, équilibrage, données, architecture

## Contexte
> **Portage Unity :** réalisé dans la version Phaser (dépôt `Bakou9/healer-game`, commit 91d7beb). À refaire et re-valider dans ce dépôt (epic E14).

Toute modification doit être vérifiée automatiquement et les régressions expliquées.

## Critères d'acceptation
- [ ] Goldens des combats de référence
- [ ] Tests d'équilibrage par profils de joueurs
- [ ] Tests d'intégrité des données et garde-fous d'architecture
- [ ] Protocole d'explication des régressions dans CLAUDE.md

## Tests automatiques exigés
`npm run check`.

## Impact équilibrage
Oui.
