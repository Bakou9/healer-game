---
id: E06
titre: Gacha et économie
---

# E06 — Gacha et économie

## Objectif
Une économie de collection saine : tirages transparents, progression équitable, puissance achetable bornée.

## Périmètre
Devises, tirages, bannières, récompenses, boutique, conformité, éthique, équilibre économique.

## Hors périmètre
Serveur (E10), interface (E04).

## Critères de sortie de l'epic
- Simulateur d'économie validé et publié.
- Les taux sont affichés et testés.
- L'achat ne casse pas l'équilibre (test).

## Tickets
<!-- TICKETS:START -->
| Ticket | Titre | Type | Priorité | Phase | Statut | Dépend de |
|---|---|---|---|---|---|---|
| [E06-T01](E06-T01-modele-economique-devises-sources-puits-rythme-c.md) | Modèle économique : devises, sources, puits, rythme cible | Design | P1 | 3 | À faire | — |
| [E06-T02](E06-T02-simulateur-d-economie-temps-de-progression-depen.md) | Simulateur d'économie (temps de progression, dépenses) | Test | P1 | 3 | À faire | E06-T01 |
| [E06-T03](E06-T03-tables-de-tirage-et-bannieres-en-donnees.md) | Tables de tirage et bannières en données | Feature | P2 | 3 | À faire | E09-T05 |
| [E06-T04](E06-T04-moteur-de-tirage-deterministe-avec-garanties-pit.md) | Moteur de tirage déterministe avec garanties (pity) | Feature | P1 | 3 | À faire | E06-T03 |
| [E06-T05](E06-T05-affichage-des-taux-et-conformite-reglementaire.md) | Affichage des taux et conformité réglementaire | Feature | P1 | 4 | À faire | E06-T04 |
| [E06-T06](E06-T06-recompenses-de-combat-et-de-progression.md) | Récompenses de combat et de progression | Feature | P1 | 3 | À faire | E01-T13 |
| [E06-T07](E06-T07-boutique-et-achats-integres-iap.md) | Boutique et achats intégrés (IAP) | Feature | P2 | 4 | À faire | E10-T05 |
| [E06-T08](E06-T08-garde-fous-ethiques.md) | Garde-fous éthiques | Design | P1 | 3 | À faire | E06-T01 |
| [E06-T09](E06-T09-equilibrage-inter-rarete-et-courbe-de-puissance.md) | Équilibrage inter-rareté et courbe de puissance (anti power creep) | Test | P1 | 3 | À faire | E08-T13 |
<!-- TICKETS:END -->
