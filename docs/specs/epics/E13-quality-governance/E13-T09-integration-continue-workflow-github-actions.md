---
id: E13-T09
epic: E13
titre: Intégration continue (workflow GitHub Actions)
type: Tech
priorité: P0
phase: 2
statut: En cours
taille: S
dépendances: E13-T04
---

# E13-T09 — Intégration continue (workflow GitHub Actions)

## Contexte
Le filet de tests ne sert que s'il s'exécute toujours, pas seulement sur le poste de l'agent.

## Critères d'acceptation
- [x] Workflow `.github/workflows/check.yml` : `npm ci` puis `npm run check` à chaque envoi sur `main` et chaque demande de fusion
- [ ] Première exécution vérifiée verte sur GitHub
- [ ] Tests d'équilibrage complets de nuit (E08-T12)
- [ ] Échec visible avec message explicatif

## Tests automatiques exigés
Le workflow lui-même.

## Impact équilibrage
Aucun.
