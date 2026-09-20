---
id: E01-T13
epic: E01
titre: Statistiques de combat (soins effectifs, surplus, dégâts évités)
type: Feature
priorité: P1
phase: 2
statut: En cours
taille: S
dépendances: E01-T03
---

# E01-T13 — Statistiques de combat (soins effectifs, surplus, dégâts évités)

## Contexte
Alimente l'écran de bilan (E04-T09) et les métriques d'équilibrage (E08-T04).

## Critères d'acceptation
- [ ] Soins effectifs vs surplus, dégâts absorbés, morts, mana dépensé — **soins effectifs, boucliers accordés, dégâts encaissés et absorbés, dégâts au boss, sorts par type, K.O., purges : faits ; surplus et mana dépensé restants**
- [x] Calculées à partir des événements (`CombatStats`, tests d'égalité avec la somme des événements)
- [x] Exposées par un module dédié, sans logique dans la scène (classe pure `CombatStats` dans le cœur)

## Tests automatiques exigés
Tests de cohérence avec les événements.

## Impact équilibrage
Aucun (mesure).
