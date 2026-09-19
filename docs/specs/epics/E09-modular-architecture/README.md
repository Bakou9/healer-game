---
id: E09
titre: Architecture modulaire
---

# E09 — Architecture modulaire

## Objectif
Une architecture qui supporte un gros projet : modules à frontières strictes, extensions par registres, contenus validés.

## Périmètre
Découpage, règles de dépendance, registres, schémas, injection, migration, documentation.

## Hors périmètre
Nouvelles fonctionnalités de jeu.

## Critères de sortie de l'epic
- Chaque module a une API publique et des dépendances déclarées.
- Une violation de frontière échoue aux tests.
- Le code existant est migré sans changer les références golden.

## Tickets
<!-- TICKETS:START -->
| Ticket | Titre | Type | Priorité | Phase | Statut | Dépend de |
|---|---|---|---|---|---|---|
| [E09-T01](E09-T01-decision-d-architecture-monolithe-modulaire-a-fr.md) | Décision d'architecture : monolithe modulaire à frontières strictes | Design | P0 | 2 | Terminé | — |
| [E09-T02](E09-T02-decoupage-en-modules.md) | Découpage en modules | Tech | P0 | 2 | À faire | E09-T01 |
| [E09-T03](E09-T03-frontieres-appliquees-automatiquement.md) | Frontières appliquées automatiquement | Tech | P0 | 2 | À faire | E09-T02 |
| [E09-T04](E09-T04-registres-d-extension-effets-actions-de-boss-sor.md) | Registres d'extension (effets, actions de boss, sorts, talents) | Tech | P0 | 2 | À faire | E09-T02 |
| [E09-T05](E09-T05-schemas-et-validation-des-contenus-par-domaine.md) | Schémas et validation des contenus par domaine | Tech | P0 | 2 | À faire | E09-T02 |
| [E09-T06](E09-T06-contrats-d-evenements-versionnes-entre-modules.md) | Contrats d'événements versionnés entre modules | Tech | P2 | 3 | À faire | E01-T03 |
| [E09-T07](E09-T07-injection-de-dependances-catalogues-horloge-rng.md) | Injection de dépendances (catalogues, horloge, RNG, stockage) | Tech | P1 | 2 | À faire | E09-T02 |
| [E09-T08](E09-T08-migration-incrementale-sans-casser-les-reference.md) | Migration incrémentale sans casser les références golden | Tech | P0 | 2 | À faire | E09-T02 |
| [E09-T09](E09-T09-structure-du-client-par-fonctionnalites-et-kit-d.md) | Structure du client par fonctionnalités et kit d'UI | Tech | P1 | 2 | À faire | E04-T03 |
| [E09-T10](E09-T10-preparer-l-extraction-en-paquets-npm-et-l-execut.md) | Préparer l'extraction en paquets npm et l'exécution côté serveur | Tech | P2 | 4 | À faire | E10-T04 |
| [E09-T11](E09-T11-documentation-par-module.md) | Documentation par module | Design | P2 | 2 | À faire | E09-T02 |
<!-- TICKETS:END -->
