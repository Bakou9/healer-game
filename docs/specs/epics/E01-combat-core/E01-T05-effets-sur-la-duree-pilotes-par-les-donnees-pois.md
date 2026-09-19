---
id: E01-T05
epic: E01
titre: Effets sur la durée pilotés par les données (poison) et Purge
type: Feature
priorité: P1
phase: 1
statut: Terminé
taille: M
dépendances: E01-T03
---

# E01-T05 — Effets sur la durée pilotés par les données (poison) et Purge

## Contexte
Donner un rôle à la Purge et créer une pression d'attrition (patron Decorator).

## Critères d'acceptation
- [x] Effet défini dans `effects.json` (dégâts par tick, tick, durée)
- [x] Ré-application = rafraîchissement, sans empilement
- [x] Purge retire les effets de la cible visée uniquement
- [x] Mort = effets retirés

## Tests automatiques exigés
`effects.test.ts`, `data-effects.test.ts`.

## Impact équilibrage
Oui : la Purge devient indispensable contre le poison (mesuré, voir EQUILIBRAGE.md §5).
