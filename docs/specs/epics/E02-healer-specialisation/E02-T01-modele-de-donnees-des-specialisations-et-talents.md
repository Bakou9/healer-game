---
id: E02-T01
epic: E02
titre: Modèle de données des spécialisations et talents (schéma)
type: Tech
priorité: P0
phase: 2
statut: En cours
taille: M
dépendances: E09-T05
---

# E02-T01 — Modèle de données des spécialisations et talents (schéma)

## Contexte
Les talents doivent être ajoutés en données, comme le reste du contenu.

## Critères d'acceptation
- [x] Schéma : voie (`TalentTierDef.Voie`), palier (`PalierDansVoie`), choix (2 `TalentOptionDef` par palier),
      prérequis (palier précédent DANS LA MÊME voie), effets sur les sorts (`UpgradeEffect`, y compris `castMs`),
      coût de puissance (`TalentOptionDef.Power`)
- [x] Validation avec erreurs lisibles : `UpgradeCatalogTests` (effet stat/sort connu, % raisonnable, descriptions
      cohérentes avec les effets cités)
- [x] Ids stables et uniques (`Chaque_palier_propose_exactement_deux_options_aux_ids_uniques_dans_tout_le_catalogue`)

## Tests automatiques exigés
Tests de schéma et d'intégrité — `UpgradeCatalogTests` (core/Healer.Combat.Tests/UpgradeTests.cs), verts.

## Impact équilibrage
Oui : le schéma porte le budget de puissance (E02-T09). Voir D-083 pour le verdict — 9 mesures de la batterie
d'équilibrage restent hors bornes, 6 sont la tension boss2/boss3 déjà connue (D-082), 3 sont une tension propre
au palier « capstone » (E02-T05) qui reste à trancher avec l'utilisateur avant de clore ce ticket.
