---
id: E04-T02
epic: E04
titre: Rendu net sur écrans haute densité et mise à l'échelle
type: Tech
priorité: P0
phase: 2
statut: À faire
taille: S
dépendances: E04-T01
---

# E04-T02 — Rendu net sur écrans haute densité et mise à l'échelle

## Contexte
> **Portage Unity :** réalisé dans la version Phaser (dépôt `Bakou9/healer-game`, commit 91d7beb). À refaire et re-valider dans ce dépôt (epic E14).

Le texte était petit et flou sur mobile (toile 1x, polices de 11 à 13 px).

## Critères d'acceptation
- [ ] Toile et texte rendus à la résolution de l'appareil (ratio de pixels borné entre 1 et 3), la scène dessinant en pixels logiques 480×854 via le zoom de la caméra
- [ ] Marges de sécurité (haut 8, bas 40 px logiques) dans la mise en page ; vérification sur appareil réel renvoyée à E11-T01
- [ ] Tailles de texte définies par jetons, aucune sous 14 px, tout texte créé via `addText` (résolution de l'appareil) ; test de conformité
- [ ] Vérification du rendu haute densité sur un écran normal avec `?dpr=2` (mode développement)

## Tests automatiques exigés
`src/ui/layout.test.ts` (jetons de taille, échelle de rendu, marges). Captures automatiques : E04-T13.

## Impact équilibrage
Aucun (rendu uniquement, simulation inchangée : références golden et bornes d'équilibrage identiques).
