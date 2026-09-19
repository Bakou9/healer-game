---
id: E01-T06
epic: E01
titre: Phases de boss (table de transitions en données)
type: Feature
priorité: P1
phase: 1
statut: Terminé
taille: M
dépendances: E01-T03
---

# E01-T06 — Phases de boss (table de transitions en données)

## Contexte
Faire évoluer le combat au lieu de répéter le même pattern 83 s.

## Critères d'acceptation
- [x] `phases[]` dans les données (seuil, rythme, pattern)
- [x] Une seule transition par phase, au bon seuil
- [x] Événement `bossPhaseChanged`

## Tests automatiques exigés
`effects.test.ts` (phases), goldens.

## Impact équilibrage
Oui : phase 2 « Fureur » à 50 % PV.
