---
id: E14-T03
epic: E14
titre: Choix du MCP : officiel (Unity AI, bêta) ou communautaire libre
type: Design
priorité: P0
phase: 2
statut: À faire
taille: S
dépendances: E14-T02
---

# E14-T03 — Choix du MCP : officiel (Unity AI, bêta) ou communautaire libre

## Contexte
Le serveur MCP officiel de Unity demande Unity 6+, un projet connecté à Unity Cloud et un essai ou un abonnement aux outils IA (bêta). Des MCP communautaires libres (ex. mcp-unity) offrent l'essentiel sans abonnement. La décision engage un coût et une dépendance : elle se prend avec l'utilisateur.

## Critères d'acceptation
- [ ] Comparaison écrite : coût, capacités (scènes, préfabriqués, scripts, console, tests), maturité, sécurité (accès au poste), maintenance
- [ ] Décision de l'utilisateur consignée (D-028)
- [ ] MCP configuré et connexion approuvée côté Unity
- [ ] Test de bout en bout : l'agent lit la hiérarchie d'une scène et crée un objet

## Tests automatiques exigés
Test de fumée manuel documenté dans `docs/UNITY_SETUP.md`.

## Impact équilibrage
Aucun.
