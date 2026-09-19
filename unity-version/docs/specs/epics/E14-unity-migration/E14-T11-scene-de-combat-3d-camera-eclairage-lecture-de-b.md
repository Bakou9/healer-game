---
id: E14-T11
epic: E14
titre: Scène de combat 3D : caméra, éclairage, lecture de Battle, événements vers la présentation
type: Feature
priorité: P1
phase: 2
statut: À faire
taille: L
dépendances: E14-T10, E14-T05
---

# E14-T11 — Scène de combat 3D : caméra, éclairage, lecture de Battle, événements vers la présentation

## Contexte
La scène ne contient aucune règle : elle lit l'état de Battle et lui envoie des commandes, comme la scène Phaser (patrons Observer, Command, pas fixe).

## Critères d'acceptation
- [ ] Pas fixe de simulation découplé du rendu
- [ ] Caméra et éclairage lisibles en portrait
- [ ] Événements de combat consommés par une couche de présentation testable

## Tests automatiques exigés
Tests EditMode/PlayMode.

## Impact équilibrage
Aucun sur les règles.
