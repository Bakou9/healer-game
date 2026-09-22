---
id: E02-T09
epic: E02
titre: Budget de puissance des talents déclaré dans les données
type: Tech
priorité: P0
phase: 2
statut: En cours
taille: S
dépendances: E02-T01
---

# E02-T09 — Budget de puissance des talents déclaré dans les données

## Contexte
Chaque talent déclare une valeur de puissance ; l'équilibrage compare des budgets, pas des impressions.

## Critères d'acceptation
- [x] Champ obligatoire dans le schéma (`TalentOptionDef.Power`, int)
- [x] Règle de calcul documentée : puissance croissante par rang de palier (`6 + rang × 1,7`, arrondi ; rang 1
      ≈ 8 à rang 12 ≈ 26 — D-084, remplace l'ancienne échelle à 4 rangs 9/11/13/17) — voir `core/content/upgrades.json`
- [ ] **Exposé pour les tests de parité (E08-T09)** : E08 n'est pas encore commencé, rien à brancher dessus
      pour l'instant (pas anticipé, conformément à CLAUDE.md)

## Tests automatiques exigés
Test de schéma — vert (`Chaque_effet_est_valide...`, présence et validité de `Power`).

## Impact équilibrage
Oui. Le budget de puissance n'est pour l'instant qu'une étiquette déclarative (pas encore utilisé par un test
automatique qui vérifierait que « puissance égale ⇒ impact mesuré égal ») : c'est la batterie `UpgradeBalanceTests`
(mesure directe par simulation, pas le champ `Power`) qui sert de garde-fou réel aujourd'hui. Voir D-083.
