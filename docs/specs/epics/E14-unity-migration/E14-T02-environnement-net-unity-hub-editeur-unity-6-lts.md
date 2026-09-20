---
id: E14-T02
epic: E14
titre: Environnement : .NET, Unity Hub, Éditeur Unity 6 LTS avec module Android
type: Tech
priorité: P0
phase: 2
statut: En cours
taille: M
dépendances: aucune
---

# E14-T02 — Environnement : .NET, Unity Hub, Éditeur Unity 6 LTS avec module Android

## Contexte
Aucun outil n'était installé. Le SDK .NET permet de compiler et tester le cœur C# sans Unity ; l'Éditeur Unity est nécessaire à la scène, aux modèles 3D et au MCP. Installation autorisée par l'utilisateur (via winget, serveurs officiels).

## Critères d'acceptation
- [x] SDK .NET 8 installé, `dotnet --version` fonctionne
- [x] Unity Hub installé
- [ ] Éditeur Unity 6 LTS et module Android installés
- [ ] Licence Unity activée par l'utilisateur (connexion à son compte, jamais saisie par l'agent)

## Tests automatiques exigés
Vérification par script (`dotnet --list-sdks`, éditeur présent).

## Impact équilibrage
Aucun.
