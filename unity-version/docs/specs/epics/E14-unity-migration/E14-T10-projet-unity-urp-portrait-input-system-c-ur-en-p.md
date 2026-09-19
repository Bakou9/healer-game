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

État au 2026-09-19 : projet créé (Unity 6000.3.24f1), licence Personal active, cœur et interface déclarés comme paquets locaux dans `Packages/manifest.json`. **Bloqué** par les refus de renommage de Windows Defender lors de la résolution des paquets du registre (voir `docs/UNITY_SETUP.md`, « Piège Windows Defender ») : l'utilisateur doit ajouter une exclusion sur le dossier `Library`.

## Critères d'acceptation
- [ ] Projet Unity 6 créé (URP, orientation portrait, Input System), versionné avec les .meta
- [ ] Cœur intégré comme paquet local (asmdef sans références moteur)
- [ ] Compilation vérifiée par l'Éditeur en mode batch

## Tests automatiques exigés
Compilation batch ; tests EditMode Unity.

## Impact équilibrage
Aucun.
