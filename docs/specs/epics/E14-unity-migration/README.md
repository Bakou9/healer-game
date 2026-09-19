---
id: E14
titre: Migration Unity et MCP
---

# E14 — Migration Unity et MCP

## Objectif
Refaire le jeu avec Unity et un serveur MCP, en reprenant toutes les règles, la documentation, le contenu et la mécanique de test de la version Phaser, sans jamais supprimer celle-ci (décision D-027).

## Périmètre
Environnement (Unity, .NET, MCP), cœur C# pur partagé (simulation, contenu, bots), conformité aux combats de référence, projet Unity, scène et interface 3D, modèles 3D stylisés, effets, build Android, intégration continue, décision finale.

## Hors périmètre
Nouvelles mécaniques de jeu (spécialisation, gacha…) : elles suivent les epics E01 à E13 une fois la parité atteinte.

## Critères de sortie de l'epic
- Le cœur C# reproduit à l'identique les combats de référence et passe les bornes d'équilibrage.
- La scène Unity est jouable avec les mêmes règles UX que la version Phaser.
- Les modèles 3D sont lisibles en portrait et respectent leur budget.
- Une décision Phaser ou Unity, argumentée et validée par l'utilisateur, est consignée.

## Tickets
<!-- TICKETS:START -->
| Ticket | Titre | Type | Priorité | Phase | Statut | Dépend de |
|---|---|---|---|---|---|---|
| [E14-T01](E14-T01-depot-parallele-reprise-des-documents-du-contenu.md) | Dépôt parallèle : reprise des documents, du contenu et des références golden | Tech | P0 | 2 | Terminé | — |
| [E14-T02](E14-T02-environnement-net-unity-hub-editeur-unity-6-lts.md) | Environnement : .NET, Unity Hub, Éditeur Unity 6 LTS avec module Android | Tech | P0 | 2 | En cours | — |
| [E14-T03](E14-T03-choix-du-mcp-officiel-unity-ai-beta-ou-communaut.md) | Choix du MCP : officiel (Unity AI, bêta) ou communautaire libre | Design | P0 | 2 | À faire | E14-T02 |
| [E14-T04](E14-T04-squelette-du-c-ur-c-bibliotheque-sans-dependance.md) | Squelette du cœur C# : bibliothèque sans dépendance à Unity | Tech | P0 | 2 | Terminé | E14-T01 |
| [E14-T05](E14-T05-portage-de-la-simulation-en-c-battle-effets-phas.md) | Portage de la simulation en C# (Battle, effets, phases, événements, RNG, pas fixe) | Feature | P0 | 2 | Terminé | E14-T04 |
| [E14-T06](E14-T06-conformite-golden-la-simulation-c-reproduit-a-l.md) | Conformité golden : la simulation C# reproduit à l'identique les combats de référence | Test | P0 | 2 | Terminé | E14-T05 |
| [E14-T07](E14-T07-chargement-et-validation-du-contenu-json-en-c.md) | Chargement et validation du contenu JSON en C# | Feature | P0 | 2 | Terminé | E14-T04 |
| [E14-T08](E14-T08-portage-des-tests-d-equilibrage-bots-profils-bor.md) | Portage des tests d'équilibrage : bots, profils, bornes, sensibilité | Test | P0 | 2 | Terminé | E14-T06 |
| [E14-T09](E14-T09-garde-fous-d-architecture-en-c.md) | Garde-fous d'architecture en C# | Test | P0 | 2 | À faire | E14-T04 |
| [E14-T10](E14-T10-projet-unity-urp-portrait-input-system-c-ur-en-p.md) | Projet Unity : URP, portrait, Input System, cœur en paquet local | Tech | P1 | 2 | À faire | E14-T02, E14-T04 |
| [E14-T11](E14-T11-scene-de-combat-3d-camera-eclairage-lecture-de-b.md) | Scène de combat 3D : caméra, éclairage, lecture de Battle, événements vers la présentation | Feature | P1 | 2 | À faire | E14-T10, E14-T05 |
| [E14-T12](E14-T12-interface-de-combat-unity-mise-en-page-portrait.md) | Interface de combat Unity : mise en page portrait, ciblage en un geste, cartes lisibles | Feature | P1 | 2 | À faire | E14-T11 |
| [E14-T13](E14-T13-modeles-3d-stylises-user-friendly-golem-et-4-per.md) | Modèles 3D stylisés « user friendly » : Golem et 4 personnages, construits par scripts reproductibles | Feature | P1 | 2 | À faire | E14-T10 |
| [E14-T14](E14-T14-effets-visuels-et-retours-en-unity.md) | Effets visuels et retours en Unity | Feature | P2 | 2 | À faire | E14-T11, E14-T13 |
| [E14-T15](E14-T15-build-android-et-test-sur-appareil-d-entree-de-g.md) | Build Android et test sur appareil d'entrée de gamme | Tech | P2 | 2 | À faire | E14-T12, E11-T01 |
| [E14-T16](E14-T16-integration-continue-specs-dotnet-test-compilati.md) | Intégration continue : specs, dotnet test, compilation Unity en option | Tech | P1 | 2 | À faire | E14-T04 |
| [E14-T17](E14-T17-parite-fonctionnelle-et-decision-finale-phaser-o.md) | Parité fonctionnelle et décision finale Phaser ou Unity | Design | P1 | 2 | À faire | E14-T12, E14-T13, E11-T07 |
<!-- TICKETS:END -->
