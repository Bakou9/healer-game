---
id: E13-T01
epic: E13
titre: Filet de non-régression : golden, équilibrage, données, architecture
type: Test
priorité: P0
phase: 1
statut: Terminé
taille: L
dépendances: aucune
---

# E13-T01 — Filet de non-régression : golden, équilibrage, données, architecture

## Contexte
Toute modification doit être vérifiée automatiquement et les régressions expliquées.

## Critères d'acceptation
- [x] Goldens des combats de référence
- [x] Tests d'équilibrage par profils de joueurs
- [x] Tests d'intégrité des données et garde-fous d'architecture
- [x] Protocole d'explication des régressions dans CLAUDE.md

## Tests automatiques exigés
`npm run check`.

## Impact équilibrage
Oui.
