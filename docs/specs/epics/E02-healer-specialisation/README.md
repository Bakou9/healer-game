---
id: E02
titre: Spécialisation et progression du soigneur
---

# E02 — Spécialisation et progression du soigneur

## Objectif
Permettre au joueur de spécialiser son soigneur par des choix significatifs, tous viables et validés par l'équilibrage.

## Périmètre
Modèle de talents, voies, paliers, points, respec, application des talents dans la simulation, niveaux.

## Hors périmètre
Interface visuelle détaillée (E04), stockage (E10), tests d'équilibrage (E08).

## Critères de sortie de l'epic
- Trois voies jouables, chacune viable sur tout le contenu de référence.
- Tous les builds passent la batterie E08.
- Les talents sont des données validées par schéma.

## Tickets
<!-- TICKETS:START -->
| Ticket | Titre | Type | Priorité | Phase | Statut | Dépend de |
|---|---|---|---|---|---|---|
| [E02-T01](E02-T01-modele-de-donnees-des-specialisations-et-talents.md) | Modèle de données des spécialisations et talents (schéma) | Tech | P0 | 2 | En cours | E09-T05 |
| [E02-T02](E02-T02-trois-voies-de-specialisation-lumiere-egide-puri.md) | Trois voies de spécialisation : Lumière, Égide, Purification | Design | P0 | 2 | En cours | E02-T01 |
| [E02-T03](E02-T03-points-de-talent-paliers-prerequis-et-reinitiali.md) | Points de talent, paliers, prérequis et réinitialisation (respec) | Feature | P1 | 2 | À faire | E02-T01 |
| [E02-T04](E02-T04-application-des-talents-dans-la-simulation.md) | Application des talents dans la simulation | Feature | P0 | 2 | En cours | E02-T01, E01-T08 |
| [E02-T05](E02-T05-competences-actives-debloquees-par-les-talents.md) | Compétences actives débloquées par les talents | Feature | P1 | 2 | En cours | E02-T04 |
| [E02-T06](E02-T06-niveaux-et-courbe-d-experience-du-soigneur.md) | Niveaux et courbe d'expérience du soigneur | Feature | P1 | 3 | À faire | E02-T03 |
| [E02-T07](E02-T07-equipement-et-reliques-du-soigneur.md) | Équipement et reliques du soigneur | Feature | P3 | 4 | À faire | E02-T04 |
| [E02-T08](E02-T08-presets-de-builds-et-partage-par-code.md) | Presets de builds et partage par code | Feature | P3 | 4 | À faire | E02-T03 |
| [E02-T09](E02-T09-budget-de-puissance-des-talents-declare-dans-les.md) | Budget de puissance des talents déclaré dans les données | Tech | P0 | 2 | En cours | E02-T01 |
| [E02-T10](E02-T10-interface-de-l-arbre-de-talents.md) | Interface de l'arbre de talents | Feature | P1 | 2 | À faire | E04-T03, E02-T03 |
<!-- TICKETS:END -->
