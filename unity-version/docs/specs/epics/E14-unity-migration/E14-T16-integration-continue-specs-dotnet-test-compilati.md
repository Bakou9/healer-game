---
id: E14-T16
epic: E14
titre: Intégration continue : specs, dotnet test, compilation Unity en option
type: Tech
priorité: P1
phase: 2
statut: À faire
taille: S
dépendances: E14-T04
---

# E14-T16 — Intégration continue : specs, dotnet test, compilation Unity en option

## Contexte
Le filet ne sert que s'il s'exécute toujours.

## Critères d'acceptation
- [ ] Première exécution du workflow vérifiée verte sur GitHub
- [ ] Compilation Unity en mode batch en option (licence requise)
- [x] Dépôt unique : le workflow `.github/workflows/unity-version.yml` (racine du dépôt) ne se déclenche que pour `unity-version/` (décision D-033)

## Tests automatiques exigés
Le workflow.

## Impact équilibrage
Aucun.
