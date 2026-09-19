---
id: E13-T03
epic: E13
titre: Cadre de spécifications (epics/tickets) validé automatiquement
type: Tech
priorité: P0
phase: 2
statut: Terminé
taille: M
dépendances: E13-T02
---

# E13-T03 — Cadre de spécifications (epics/tickets) validé automatiquement

## Contexte
Des specs qui dérivent sont pires que pas de specs.

## Critères d'acceptation
- [x] Un dossier par epic, un fichier par ticket, en-tête normalisé
- [x] Index régénérable (`npm run specs:index`)
- [x] Test : ids uniques, dépendances valides sans cycle, sections requises, index à jour

## Tests automatiques exigés
`specs.test.ts`.

## Impact équilibrage
Aucun.
