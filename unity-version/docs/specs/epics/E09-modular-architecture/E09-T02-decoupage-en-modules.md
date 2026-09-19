---
id: E09-T02
epic: E09
titre: Découpage en modules
type: Tech
priorité: P0
phase: 2
statut: À faire
taille: L
dépendances: E09-T01
---

# E09-T02 — Découpage en modules

## Contexte
core, combat, content, progression, economy, balance, client, platform.

## Critères d'acceptation
- [ ] Arborescence `src/modules/*` créée
- [ ] Chaque module a `index.ts` (API publique)
- [ ] Aucun import profond entre modules

## Tests automatiques exigés
Tests d'architecture étendus.

## Impact équilibrage
Aucun.
