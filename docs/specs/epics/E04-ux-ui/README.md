---
id: E04
titre: Expérience et interface (UX/UI)
---

# E04 — Expérience et interface (UX/UI)

## Objectif
Une interface lisible, rapide à utiliser d'une main sur mobile, qui laisse le joueur décider plutôt que chercher.

## Périmètre
Mise en page, ciblage, cartes, sorts, télégraphes, retours, menus, tutoriel, accessibilité, tests visuels.

## Hors périmètre
Art final (E12), contenus (E02, E03).

## Critères de sortie de l'epic
- Toute action de combat en au plus 2 gestes, zone du pouce.
- Toute information critique lisible en moins d'une seconde.
- Aucun texte de moins de 14 px effectifs.
- Tests visuels automatisés en place.

## Tickets
<!-- TICKETS:START -->
| Ticket | Titre | Type | Priorité | Phase | Statut | Dépend de |
|---|---|---|---|---|---|---|
| [E04-T01](E04-T01-audit-ux-et-principes-de-conception-docs-ux-md.md) | Audit UX et principes de conception (docs/UX.md) | Design | P0 | 2 | Terminé | — |
| [E04-T02](E04-T02-rendu-net-sur-ecrans-haute-densite-et-mise-a-l-e.md) | Rendu net sur écrans haute densité et mise à l'échelle | Tech | P0 | 2 | Terminé | E04-T01 |
| [E04-T03](E04-T03-refonte-de-la-mise-en-page-portrait-zone-du-pouc.md) | Refonte de la mise en page portrait (zone du pouce) | Feature | P0 | 2 | Terminé | E04-T01 |
| [E04-T04](E04-T04-ciblage-en-un-geste-selection-puis-sorts-ou-tap.md) | Ciblage en un geste (sélection persistante puis sorts) | Feature | P0 | 2 | Terminé | E04-T01 |
| [E04-T05](E04-T05-cartes-d-allies-lisibles.md) | Cartes d'alliés lisibles | Feature | P0 | 2 | Terminé | E04-T03 |
| [E04-T06](E04-T06-barre-de-sorts-recharge-radiale-cout-de-mana-eta.md) | Barre de sorts : recharge radiale, coût de mana, états, raccourcis clavier | Feature | P1 | 2 | À faire | E04-T03 |
| [E04-T07](E04-T07-telegraphes-lisibles.md) | Télégraphes lisibles | Feature | P0 | 2 | À faire | E03-T01 |
| [E04-T08](E04-T08-retours-visuels-maitrises-agregation-et-plafond.md) | Retours visuels maîtrisés (agrégation et plafond, flash, secousse, haptique) | Feature | P1 | 2 | À faire | E04-T03 |
| [E04-T09](E04-T09-ecran-de-bilan-de-combat.md) | Écran de bilan de combat | Feature | P1 | 2 | À faire | E01-T13 |
| [E04-T10](E04-T10-menu-principal-choix-du-combat-pause-et-reglages.md) | Menu principal, choix du combat, pause et réglages | Feature | P1 | 2 | À faire | E04-T03 |
| [E04-T11](E04-T11-tutoriel-et-introduction-progressive-des-sorts.md) | Tutoriel et introduction progressive des sorts | Feature | P1 | 2 | À faire | E04-T04 |
| [E04-T12](E04-T12-accessibilite-daltonisme-taille-du-texte-contras.md) | Accessibilité (daltonisme, taille du texte, contraste, gaucher, animations réduites) | Feature | P1 | 3 | À faire | E04-T05 |
| [E04-T13](E04-T13-tests-visuels-automatises-captures-deterministes.md) | Tests visuels automatisés (captures déterministes) | Test | P1 | 2 | À faire | E13-T05 |
| [E04-T14](E04-T14-protocole-de-playtest-et-metriques-ux.md) | Protocole de playtest et métriques UX | Design | P2 | 3 | À faire | E04-T03 |
<!-- TICKETS:END -->
