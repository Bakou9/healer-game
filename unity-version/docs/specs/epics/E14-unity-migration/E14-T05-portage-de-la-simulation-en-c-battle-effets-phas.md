---
id: E14-T05
epic: E14
titre: Portage de la simulation en C# (Battle, effets, phases, événements, RNG, pas fixe)
type: Feature
priorité: P0
phase: 2
statut: Terminé
taille: L
dépendances: E14-T04
---

# E14-T05 — Portage de la simulation en C# (Battle, effets, phases, événements, RNG, pas fixe)

## Contexte
Traduction fidèle de la simulation TypeScript (référence : dépôt Phaser, dossier src/sim) : même algorithme, même ordre des événements, même RNG (mulberry32).

Différences assumées et signalées : les sorts sont fournis par la rencontre (injection de dépendances, ticket E09-T07 avancé) au lieu d'un import global ; le journal texte `getLog` de la version TypeScript n'est pas porté (inutilisé par les références golden).

## Critères d'acceptation
- [x] Types, RNG seedé, pas fixe, événements typés, Battle avec commandes, sorts, effets sur la durée, phases de boss
- [x] Ordre d'émission des événements identique à la version TypeScript
- [x] Aucune règle inventée : toute différence est signalée et expliquée

## Tests automatiques exigés
Conformité golden (E14-T06).

## Impact équilibrage
Oui : doit rester strictement identique (vérifié par les golden).
