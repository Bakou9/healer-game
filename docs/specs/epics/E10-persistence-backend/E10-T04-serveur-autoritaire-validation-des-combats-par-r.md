---
id: E10-T04
epic: E10
titre: Serveur autoritaire : validation des combats par rejeu
type: Feature
priorité: P1
phase: 4
statut: À faire
taille: L
dépendances: E01-T12
---

# E10-T04 — Serveur autoritaire : validation des combats par rejeu

## Contexte
Empêcher la triche sans dupliquer les règles : le serveur rejoue seed + commandes.

## Critères d'acceptation
- [ ] Rejeu identique côté serveur
- [ ] Refus si le résultat diffère ou si les commandes sont impossibles (recharge, mana)
- [ ] Coût de calcul mesuré

## Tests automatiques exigés
Tests de rejeu et de commandes invalides.

## Impact équilibrage
Aucun.
