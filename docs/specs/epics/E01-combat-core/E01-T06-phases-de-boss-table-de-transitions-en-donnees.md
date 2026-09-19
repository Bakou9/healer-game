---
id: E01-T06
epic: E01
titre: Phases de boss (table de transitions en données)
type: Feature
priorité: P1
phase: 1
statut: À faire
taille: M
dépendances: E01-T03
---

# E01-T06 — Phases de boss (table de transitions en données)

## Contexte
> **Portage Unity :** réalisé dans la version Phaser (dépôt `Bakou9/healer-game`, commit 91d7beb). À refaire et re-valider dans ce dépôt (epic E14).

Faire évoluer le combat au lieu de répéter le même pattern 83 s.

## Critères d'acceptation
- [ ] `phases[]` dans les données (seuil, rythme, pattern)
- [ ] Une seule transition par phase, au bon seuil
- [ ] Événement `bossPhaseChanged`

## Tests automatiques exigés
`effects.test.ts` (phases), goldens.

## Impact équilibrage
Oui : phase 2 « Fureur » à 50 % PV.
