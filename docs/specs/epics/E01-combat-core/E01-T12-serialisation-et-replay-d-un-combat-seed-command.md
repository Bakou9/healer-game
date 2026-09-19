---
id: E01-T12
epic: E01
titre: Sérialisation et replay d'un combat (seed + commandes)
type: Tech
priorité: P1
phase: 2
statut: À faire
taille: M
dépendances: E01-T04
---

# E01-T12 — Sérialisation et replay d'un combat (seed + commandes)

## Contexte
Base de la validation serveur, des défis quotidiens, des classements et du débogage.

## Critères d'acceptation
- [ ] Format versionné (JSON) d'un combat rejouable
- [ ] Rejouer un fichier redonne exactement les mêmes événements
- [ ] Refus clair si la version du format ou du contenu ne correspond pas

## Tests automatiques exigés
Test aller-retour sur tous les scénarios golden.

## Impact équilibrage
Aucun.
