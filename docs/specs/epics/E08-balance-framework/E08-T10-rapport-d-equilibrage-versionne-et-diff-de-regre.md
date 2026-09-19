---
id: E08-T10
epic: E08
titre: Rapport d'équilibrage versionné et diff de régression expliqué
type: Test
priorité: P0
phase: 2
statut: À faire
taille: M
dépendances: E08-T04
---

# E08-T10 — Rapport d'équilibrage versionné et diff de régression expliqué

## Contexte
Comme les goldens : toute évolution du tableau d'équilibre est signalée, expliquée et validée.

## Critères d'acceptation
- [ ] Rapport généré (`docs/balance/report.md`) et commité
- [ ] Test qui compare l'état courant au rapport
- [ ] Message d'échec : quels builds ont bougé, de combien, à cause de quoi

## Tests automatiques exigés
Test de comparaison.

## Impact équilibrage
Oui.
