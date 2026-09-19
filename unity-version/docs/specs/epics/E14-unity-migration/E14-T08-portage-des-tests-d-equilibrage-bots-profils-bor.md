---
id: E14-T08
epic: E14
titre: Portage des tests d'équilibrage : bots, profils, bornes, sensibilité
type: Test
priorité: P0
phase: 2
statut: Terminé
taille: L
dépendances: E14-T06
---

# E14-T08 — Portage des tests d'équilibrage : bots, profils, bornes, sensibilité

## Contexte
Reprise du cadre de docs/EQUILIBRAGE.md : joueur attentif/lent/sans purge/passif/spam et leurs bornes.

## Critères d'acceptation
- [x] Bot de référence porté à l'identique (décision toutes les 500 ms, purge, bouclier sur télégraphe)
- [x] Bornes de docs/EQUILIBRAGE.md §2 exécutées sur 100 combats par profil
- [x] Résultats identiques à la version TypeScript (mêmes mesures)

## Tests automatiques exigés
Tests d'équilibrage C#.

## Impact équilibrage
Oui : vérifie que rien n'a bougé.
