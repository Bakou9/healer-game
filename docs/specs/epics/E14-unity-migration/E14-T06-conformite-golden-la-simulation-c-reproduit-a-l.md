---
id: E14-T06
epic: E14
titre: Conformité golden : la simulation C# reproduit à l'identique les combats de référence
type: Test
priorité: P0
phase: 2
statut: Terminé
taille: M
dépendances: E14-T05
---

# E14-T06 — Conformité golden : la simulation C# reproduit à l'identique les combats de référence

## Contexte
Les 7 fichiers de `core/golden` (un événement par ligne) sont la spécification de conformité. Le portage est correct si et seulement s'il les reproduit exactement.

## Critères d'acceptation
- [x] Les 7 scénarios (bots, sans soigneur, spam, bouclier initial…) rejoués en C# et comparés ligne à ligne
- [x] Message d'échec : première ligne divergente, comme en TypeScript
- [x] Aucun mécanisme de régénération automatique : les références ne changent que par décision explicite de l'utilisateur (un script sera écrit le jour où une régression voulue se présente)

## Tests automatiques exigés
`dotnet test` (conformité).

## Impact équilibrage
Oui : c'est la garantie que l'équilibre est préservé par construction.
