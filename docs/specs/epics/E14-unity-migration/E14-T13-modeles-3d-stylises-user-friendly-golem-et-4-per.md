---
id: E14-T13
epic: E14
titre: Modèles 3D stylisés « user friendly » : Golem et 4 personnages, construits par scripts reproductibles
type: Feature
priorité: P1
phase: 2
statut: En cours
taille: L
dépendances: E14-T10
---

# E14-T13 — Modèles 3D stylisés « user friendly » : Golem et 4 personnages, construits par scripts reproductibles

## Contexte
Direction : formes simples et arrondies, silhouettes lisibles en petit, couleurs par rôle, faible nombre de polygones (docs/ART_3D.md). Modèles construits par des scripts d'Éditeur (préfabriqués reproductibles), pas à la main.

## Critères d'acceptation
- [x] Golem Ancestral (cœur lumineux, phase 2) et 4 personnages (tank, archère, mage, soigneuse) reconnaissables au premier coup d'œil (captures : bouclier, arc, chapeau de mage, robe et croix verte)
- [x] Budget de triangles par modèle respecté et testé (contrôle à chaque build : `Builder.CheckModels`, échec du build si dépassement)
- [x] Reconstruction complète des modèles par une seule commande : les modèles sont générés par code à chaque lancement (`MeshKit`, `ModelFactory`), aucun fichier de modèle (spec amendée, voir D-036)
- [ ] Lisibilité vérifiée en portrait sur téléphone

## Tests automatiques exigés
Tests de budget de triangles et de conformité des préfabriqués.

## Impact équilibrage
Aucun.
