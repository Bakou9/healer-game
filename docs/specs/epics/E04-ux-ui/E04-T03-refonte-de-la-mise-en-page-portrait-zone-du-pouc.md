---
id: E04-T03
epic: E04
titre: Refonte de la mise en page portrait (zone du pouce)
type: Feature
priorité: P0
phase: 2
statut: À faire
taille: L
dépendances: E04-T01
---

# E04-T03 — Refonte de la mise en page portrait (zone du pouce)

## Contexte
> **Portage Unity :** réalisé dans la version Phaser (dépôt `Bakou9/healer-game`, commit 91d7beb). À refaire et re-valider dans ce dépôt (epic E14).

Le centre de l'écran était vide tandis que les actions étaient éparpillées et petites.

## Critères d'acceptation
- [ ] Ordre vertical : barre haute (nom et PV du boss, pause), boss et télégraphe, bandeau de journal, équipe, cible et mana, barre de sorts
- [ ] Barre de sorts dans la moitié basse, avec marge de sécurité en bas
- [ ] Aucune zone morte : journal court (3 lignes) sous le boss, sans chevauchement avec les cartes
- [ ] Cibles tactiles ≥ 48 px (cartes 108×200, sorts 108×164, pause 48×48)

## Tests automatiques exigés
`src/ui/layout.test.ts` : zones dans l'écran, sans chevauchement, dans l'ordre, cibles ≥ 48 px, barre de PV du boss ne chevauchant pas la pause. Captures de référence sur plusieurs tailles d'écran : E04-T13.

## Impact équilibrage
Aucun sur les règles (simulation inchangée). Rythme d'action : voir E04-T04.
