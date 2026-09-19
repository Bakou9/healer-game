---
id: E14-T10
epic: E14
titre: Projet Unity : URP, portrait, Input System, cœur en paquet local
type: Tech
priorité: P1
phase: 2
statut: En cours
taille: M
dépendances: E14-T02, E14-T04
---

# E14-T10 — Projet Unity : URP, portrait, Input System, cœur en paquet local

## Contexte
Base du projet Unity dans `unity/HealerGame`.

État au 2026-09-19 : projet créé (Unity 6000.3.24f1), licence Personal active. **Unity compile le cœur** (`Healer.Combat.dll` et `Healer.Ui.dll`, 0 erreur, ouverture en mode automatique). Contournement des refus de renommage de Windows Defender : plus aucun paquet du registre Unity (Newtonsoft.Json est intégré au paquet du cœur, décision D-034). URP, Input System et portrait restent à ajouter : ils viennent du registre, donc l'exclusion Defender du dossier `Library` sera nécessaire (voir `docs/UNITY_SETUP.md`).

## Critères d'acceptation
- [ ] Projet Unity 6 créé (URP, orientation portrait, Input System), versionné avec les .meta
- [x] Cœur intégré comme paquet local (asmdef sans références moteur)
- [x] Compilation vérifiée par l'Éditeur en mode batch (2026-09-19 : Healer.Combat et Healer.Ui compilés, 0 erreur)

## Tests automatiques exigés
Compilation batch ; tests EditMode Unity.

## Impact équilibrage
Aucun.
