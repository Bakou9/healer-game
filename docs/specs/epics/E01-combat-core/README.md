---
id: E01
titre: Cœur du combat
---

# E01 — Cœur du combat

## Objectif
Un moteur de combat déterministe, pur et extensible, qui porte toutes les règles du jeu.

## Périmètre
Simulation, événements, commandes, effets, phases, ciblage, replay, statistiques de combat.

## Hors périmètre
Rendu (E04), contenu de boss (E03), spécialisation (E02).

## Critères de sortie de l'epic
- Toutes les règles de combat passent par `src/sim/` sans dépendance au rendu.
- Un combat est rejouable à l'identique depuis (seed, commandes).
- Chaque règle a un test et un scénario golden.

## Tickets
<!-- TICKETS:START -->
| Ticket | Titre | Type | Priorité | Phase | Statut | Dépend de |
|---|---|---|---|---|---|---|
| [E01-T01](E01-T01-simulation-de-combat-deterministe-et-pure.md) | Simulation de combat déterministe et pure | Tech | P0 | 1 | Terminé | — |
| [E01-T02](E01-T02-pas-de-simulation-fixe-game-loop.md) | Pas de simulation fixe (Game Loop) | Tech | P0 | 1 | Terminé | E01-T01 |
| [E01-T03](E01-T03-evenements-de-combat-observer.md) | Événements de combat (Observer) | Tech | P0 | 1 | Terminé | E01-T01 |
| [E01-T04](E01-T04-commandes-du-joueur-command-horodatees.md) | Commandes du joueur (Command) horodatées | Tech | P0 | 1 | Terminé | E01-T01 |
| [E01-T05](E01-T05-effets-sur-la-duree-pilotes-par-les-donnees-pois.md) | Effets sur la durée pilotés par les données (poison) et Purge | Feature | P1 | 1 | Terminé | E01-T03 |
| [E01-T06](E01-T06-phases-de-boss-table-de-transitions-en-donnees.md) | Phases de boss (table de transitions en données) | Feature | P1 | 1 | Terminé | E01-T03 |
| [E01-T07](E01-T07-soin-soin-de-zone-et-bouclier.md) | Soin, soin de zone et bouclier | Feature | P0 | 1 | Terminé | E01-T04 |
| [E01-T08](E01-T08-registre-d-effets-extensible-soin-sur-la-duree-s.md) | Registre d'effets extensible (soin sur la durée, statistiques, étourdissement, vulnérabilité) | Feature | P1 | 2 | À faire | E01-T05, E09-T04 |
| [E01-T09](E01-T09-k-o-et-resurrection.md) | K.O. et résurrection | Feature | P2 | 2 | À faire | E01-T03 |
| [E01-T10](E01-T10-ciblage-du-boss-menace-focus-du-plus-faible-alea.md) | Ciblage du boss : menace, focus du plus faible, aléatoire pondéré | Feature | P1 | 2 | À faire | E01-T06 |
| [E01-T11](E01-T11-coups-critiques-et-variance-maitrisee.md) | Coups critiques et variance maîtrisée | Feature | P2 | 2 | À faire | E01-T01 |
| [E01-T12](E01-T12-serialisation-et-replay-d-un-combat-seed-command.md) | Sérialisation et replay d'un combat (seed + commandes) | Tech | P1 | 2 | À faire | E01-T04 |
| [E01-T13](E01-T13-statistiques-de-combat-soins-effectifs-surplus-d.md) | Statistiques de combat (soins effectifs, surplus, dégâts évités) | Feature | P1 | 2 | À faire | E01-T03 |
<!-- TICKETS:END -->
