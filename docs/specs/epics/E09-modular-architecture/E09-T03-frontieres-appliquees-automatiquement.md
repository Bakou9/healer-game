---
id: E09-T03
epic: E09
titre: Frontières appliquées automatiquement
type: Tech
priorité: P0
phase: 2
statut: À faire
taille: M
dépendances: E09-T02
---

# E09-T03 — Frontières appliquées automatiquement

## Contexte
Une règle non testée n'existe pas.

## Critères d'acceptation
- [ ] Règles de dépendance entre couches vérifiées par test
- [ ] Interdit : cycles, imports profonds, accès du combat au client
- [ ] Message d'échec indiquant l'import fautif

## Tests automatiques exigés
`architecture.test.ts` étendu.

## Impact équilibrage
Aucun.
