---
id: E01-T05
epic: E01
titre: Effets sur la durée pilotés par les données (poison) et Purge
type: Feature
priorité: P1
phase: 1
statut: À faire
taille: M
dépendances: E01-T03
---

# E01-T05 — Effets sur la durée pilotés par les données (poison) et Purge

## Contexte
> **Portage Unity :** réalisé dans la version Phaser (dépôt `Bakou9/healer-game`, commit 91d7beb). À refaire et re-valider dans ce dépôt (epic E14).

Donner un rôle à la Purge et créer une pression d'attrition (patron Decorator).

## Critères d'acceptation
- [ ] Effet défini dans `effects.json` (dégâts par tick, tick, durée)
- [ ] Ré-application = rafraîchissement, sans empilement
- [ ] Purge retire les effets de la cible visée uniquement
- [ ] Mort = effets retirés

## Tests automatiques exigés
`effects.test.ts`, `data-effects.test.ts`.

## Impact équilibrage
Oui : la Purge devient indispensable contre le poison (mesuré, voir EQUILIBRAGE.md §5).
