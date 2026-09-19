---
id: E13
titre: Qualité, outillage et gouvernance
---

# E13 — Qualité, outillage et gouvernance

## Objectif
Un projet qui garde sa qualité en grossissant : tests, CI, décisions et specs persistantes.

## Périmètre
Filet de tests, journal des décisions, specs, git/GitHub, CI, tests visuels, couverture, outils de débogage.

## Hors périmètre
Règles de jeu.

## Critères de sortie de l'epic
- Aucune régression n'entre sans explication.
- Toute décision est consignée.
- La CI exécute `npm run check` à chaque envoi.

## Tickets
<!-- TICKETS:START -->
| Ticket | Titre | Type | Priorité | Phase | Statut | Dépend de |
|---|---|---|---|---|---|---|
| [E13-T01](E13-T01-filet-de-non-regression-golden-equilibrage-donne.md) | Filet de non-régression : golden, équilibrage, données, architecture | Test | P0 | 1 | Terminé | — |
| [E13-T02](E13-T02-journal-des-decisions-persistant-et-regle-de-mis.md) | Journal des décisions persistant et règle de mise à jour | Tech | P0 | 2 | Terminé | — |
| [E13-T03](E13-T03-cadre-de-specifications-epics-tickets-valide-aut.md) | Cadre de spécifications (epics/tickets) validé automatiquement | Tech | P0 | 2 | Terminé | E13-T02 |
| [E13-T04](E13-T04-depot-git-et-github.md) | Dépôt git et GitHub | Tech | P0 | 1 | Terminé | — |
| [E13-T05](E13-T05-banc-de-test-navigateur-pilotage-et-pas-de-temps.md) | Banc de test navigateur (pilotage et pas de temps contrôlé) | Test | P1 | 2 | À faire | E13-T04 |
| [E13-T06](E13-T06-couverture-de-code-et-tests-de-mutation-sur-la-s.md) | Couverture de code et tests de mutation sur la simulation | Test | P2 | 3 | À faire | — |
| [E13-T07](E13-T07-visionneuse-de-replay-et-surcouche-de-debogage.md) | Visionneuse de replay et surcouche de débogage | Tech | P2 | 3 | À faire | E01-T12 |
| [E13-T08](E13-T08-conventions-de-branches-pr-et-revue.md) | Conventions de branches, PR et revue | Tech | P1 | 2 | À faire | E13-T04 |
| [E13-T09](E13-T09-integration-continue-workflow-github-actions.md) | Intégration continue (workflow GitHub Actions) | Tech | P0 | 2 | En cours | E13-T04 |
<!-- TICKETS:END -->
