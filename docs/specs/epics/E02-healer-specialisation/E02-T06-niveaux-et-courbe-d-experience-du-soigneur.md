---
id: E02-T06
epic: E02
titre: Niveaux et courbe d'expérience du soigneur
type: Feature
priorité: P1
phase: 3
statut: En cours
taille: M
dépendances: aucune
---

# E02-T06 — Niveaux et courbe d'expérience du soigneur

## Contexte
Rythme de déblocage des paliers.

## Critères d'acceptation
- [x] Courbe d'XP en données (`core/content/leveling.json`, `HealerLeveling` — niveaux 2 à 25)
- [x] Un point de talent aux niveaux définis (1 point/niveau, 24 au total sur toute la courbe)
- [ ] **Simulateur de progression (E06-T02)** : pas fait, E06 n'existe pas encore dans ce dépôt

## Tests automatiques exigés
Tests de courbe (`HealerLeveling`, `PlayerProfile`) — verts.

## Impact équilibrage
Oui : puissance par niveau (E08-T13, pas encore commencé). **Proposition non validée par playtest** (D-084) :
la courbe (25 niveaux, XP 60→8190, facteur ~1,13) est un premier jet raisonné, pas mesuré sur un joueur réel —
à ajuster une fois joué.
