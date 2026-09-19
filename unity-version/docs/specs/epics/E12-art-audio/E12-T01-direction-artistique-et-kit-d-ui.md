---
id: E12-T01
epic: E12
titre: Direction artistique et kit d'UI
type: Design
priorité: P1
phase: 3
statut: À faire
taille: L
dépendances: aucune
---

# E12-T01 — Direction artistique et kit d'UI

## Contexte
> **Portage Unity :** réalisé dans la version Phaser (dépôt `Bakou9/healer-game`, commit 91d7beb). À refaire et re-valider dans ce dépôt (epic E14).

Avant : des rectangles colorés provisoires. Direction artistique **déléguée à l'agent** par l'utilisateur (« quelques graphismes sympas que tu trouves adaptés », décision D-023) ; à valider ou à réorienter après le premier test de jeu.

## Critères d'acceptation
- [ ] Style retenu (provisoire) : fantasy stylisée en formes vectorielles dessinées en code (aucun fichier d'image), fond sombre avec lueurs, accents lumineux par langage visuel (soin vert, bouclier bleu, poison violet, danger rouge, phase 2 orange), coins arrondis
- [ ] Jetons de design partagés (`src/ui/layout.ts`) et briques réutilisables : panneaux, icônes de portrait et de sort, décor (`src/scenes/art/`)
- [ ] Palette et typographies validées par l'utilisateur après le test de jeu
- [ ] Alternatives d'accessibilité (daltonisme, contrastes) : voir E04-T12

## Tests automatiques exigés
`src/ui/artMap.test.ts` (chaque personnage et chaque sort a son icône) ; `src/ui/layout.test.ts` (jetons).

## Impact équilibrage
Aucun (rendu uniquement).
