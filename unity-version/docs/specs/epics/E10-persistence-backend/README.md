---
id: E10
titre: Persistance et backend
---

# E10 — Persistance et backend

## Objectif
Sauvegarde fiable, puis serveur autoritaire qui valide combats et tirages.

## Périmètre
Sauvegarde locale, stockage, comptes, validation des combats, économie serveur, API, anti-triche, télémétrie, conformité.

## Hors périmètre
Règles de jeu (E01-E05).

## Critères de sortie de l'epic
- Les sauvegardes survivent aux mises à jour.
- Aucun résultat de combat ni tirage n'est accepté sans validation serveur.

## Tickets
<!-- TICKETS:START -->
| Ticket | Titre | Type | Priorité | Phase | Statut | Dépend de |
|---|---|---|---|---|---|---|
| [E10-T01](E10-T01-sauvegarde-locale-versionnee-avec-migrations.md) | Sauvegarde locale versionnée avec migrations | Feature | P1 | 3 | À faire | E09-T05 |
| [E10-T02](E10-T02-abstraction-de-stockage-par-plateforme.md) | Abstraction de stockage par plateforme | Tech | P1 | 3 | À faire | E09-T07 |
| [E10-T03](E10-T03-comptes-et-synchronisation-cloud.md) | Comptes et synchronisation cloud | Feature | P2 | 4 | À faire | E10-T01 |
| [E10-T04](E10-T04-serveur-autoritaire-validation-des-combats-par-r.md) | Serveur autoritaire : validation des combats par rejeu | Feature | P1 | 4 | À faire | E01-T12 |
| [E10-T05](E10-T05-serveur-tirages-gacha-et-economie.md) | Serveur : tirages gacha et économie | Feature | P1 | 4 | À faire | E06-T04 |
| [E10-T06](E10-T06-contrats-d-api-et-versionnage.md) | Contrats d'API et versionnage | Tech | P2 | 4 | À faire | E10-T04 |
| [E10-T07](E10-T07-anti-triche-et-limitation-de-debit.md) | Anti-triche et limitation de débit | Feature | P2 | 4 | À faire | E10-T04 |
| [E10-T08](E10-T08-telemetrie-et-analytique-respectueuses-de-la-vie.md) | Télémétrie et analytique respectueuses de la vie privée | Feature | P2 | 4 | À faire | E10-T09 |
| [E10-T09](E10-T09-conformite-donnees-personnelles-rgpd.md) | Conformité données personnelles (RGPD) | Design | P1 | 4 | À faire | — |
<!-- TICKETS:END -->
