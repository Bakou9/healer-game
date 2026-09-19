---
id: E08
titre: Cadre d'équilibrage automatisé
---

# E08 — Cadre d'équilibrage automatisé

## Objectif
Valider automatiquement l'équilibre du jeu entre chaque décision de spécialisation que peut faire le joueur, et le garder à chaque changement.

## Périmètre
Joueurs de référence, espace de builds, métriques, tests de viabilité, de non-dominance, de niche, d'ablation, de parité, de chemins, rapport versionné, outils de balayage.

## Hors périmètre
Définition du contenu (E02, E03, E05), économie (E06).

## Critères de sortie de l'epic
- Aucun changement ne peut dégrader l'équilibre sans qu'un test le signale et qu'on l'explique.
- Tout build atteignable est testé (énumération ou échantillonnage seedé).
- Un rapport d'équilibrage versionné est produit et commité.

## Tickets
<!-- TICKETS:START -->
| Ticket | Titre | Type | Priorité | Phase | Statut | Dépend de |
|---|---|---|---|---|---|---|
| [E08-T01](E08-T01-profils-de-joueurs-de-reference-attentif-lent-sa.md) | Profils de joueurs de référence (attentif, lent, sans purge, passif, spam) | Test | P0 | 1 | À faire | E01-T01 |
| [E08-T02](E08-T02-un-bot-raisonnable-par-specialisation.md) | Un bot « raisonnable » par spécialisation | Test | P0 | 2 | À faire | E02-T04, E08-T01 |
| [E08-T03](E08-T03-generateur-de-l-espace-de-builds-enumeration-ou.md) | Générateur de l'espace de builds (énumération ou échantillonnage seedé) | Test | P0 | 2 | À faire | E02-T01 |
| [E08-T04](E08-T04-metriques-standard-d-equilibrage.md) | Métriques standard d'équilibrage | Test | P0 | 2 | À faire | E01-T13 |
| [E08-T05](E08-T05-test-de-viabilite-chaque-build-atteint-un-planch.md) | Test de viabilité : chaque build atteint un plancher | Test | P0 | 2 | À faire | E08-T03, E08-T04 |
| [E08-T06](E08-T06-test-de-non-dominance-ecart-borne-et-absence-de.md) | Test de non-dominance : écart borné et absence de build strictement supérieur | Test | P0 | 2 | À faire | E08-T05 |
| [E08-T07](E08-T07-test-de-niche-chaque-voie-est-la-meilleure-sur-a.md) | Test de niche : chaque voie est la meilleure sur au moins un archétype | Test | P0 | 2 | À faire | E03-T01, E08-T05 |
| [E08-T08](E08-T08-tests-d-ablation-impact-marginal-borne-de-chaque.md) | Tests d'ablation : impact marginal borné de chaque talent | Test | P1 | 2 | À faire | E08-T03 |
| [E08-T09](E08-T09-budget-de-puissance-des-talents-et-test-de-parit.md) | Budget de puissance des talents et test de parité | Test | P1 | 2 | À faire | E02-T09 |
| [E08-T10](E08-T10-rapport-d-equilibrage-versionne-et-diff-de-regre.md) | Rapport d'équilibrage versionné et diff de régression expliqué | Test | P0 | 2 | À faire | E08-T04 |
| [E08-T11](E08-T11-tests-de-chemins-chaque-suite-de-decisions-de-sp.md) | Tests de chemins : chaque suite de décisions de spécialisation reste équilibrée | Test | P0 | 2 | À faire | E02-T03 |
| [E08-T12](E08-T12-niveaux-de-test-rapide-a-chaque-commit-complet-c.md) | Niveaux de test : rapide à chaque commit, complet chaque nuit | Tech | P1 | 2 | À faire | E08-T03 |
| [E08-T13](E08-T13-equilibrage-de-la-progression-puissance-par-nive.md) | Équilibrage de la progression (puissance par niveau, contenu par niveau) | Test | P1 | 3 | À faire | E02-T06 |
| [E08-T14](E08-T14-outil-de-balayage-de-parametres-reutilisable.md) | Outil de balayage de paramètres réutilisable | Tech | P1 | 2 | À faire | E08-T04 |
| [E08-T15](E08-T15-profils-humains-a-delais-variables.md) | Profils humains à délais de décision variables | Test | P1 | 2 | À faire | E08-T01 |
<!-- TICKETS:END -->
