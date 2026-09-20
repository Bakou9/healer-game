---
id: E14-T01
epic: E14
titre: Dépôt parallèle : reprise des documents, du contenu et des références golden
type: Tech
priorité: P0
phase: 2
statut: Terminé
taille: M
dépendances: aucune
---

# E14-T01 — Dépôt parallèle : reprise des documents, du contenu et des références golden

## Contexte
Le projet Phaser est conservé intact (dépôt Bakou9/healer-game) ; la version Unity vit dans un dépôt séparé qui reprend toutes les règles, les specs, le contenu JSON et les combats de référence (décision D-027).

## Critères d'acceptation
- [x] Documents, specs, journal des décisions et registre des revues repris ; ancien CLAUDE.md archivé dans docs/legacy
- [x] Contenu de jeu (core/content) et références golden (core/golden) repris tels quels : ils servent de spécification de conformité indépendante du langage
- [x] Outillage de cohérence des specs opérationnel dans le nouveau dépôt
- [x] Ancien dépôt vérifié sans aucune modification

## Tests automatiques exigés
`tools/specs/specs.test.ts`.

## Impact équilibrage
Aucun (reprise à l'identique).
