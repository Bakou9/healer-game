---
id: E02-T05
epic: E02
titre: Compétences actives débloquées par les talents
type: Feature
priorité: P1
phase: 2
statut: En cours
taille: M
dépendances: E02-T04
---

# E02-T05 — Compétences actives débloquées par les talents

## Contexte
Certains talents ajoutent un sort (ex. « Miracle », « Dôme »).

## Critères d'acceptation
- [x] Sorts déclarés en données avec conditions de déblocage (`SkillDef.Capstone`, `TalentOptionDef.UnlocksSkill` ;
      Miracle/Dôme/Renaissance dans `core/content/skills.json`, un talent capstone par voie dans `upgrades.json`)
- [ ] **Barre de sorts dynamique (contrat avec E04-T06)** : pas fait — E04-T06 n'existe plus tel quel après le
      pivot PC (voir D-0xx pivot), le contrat est à redéfinir avant de construire l'UI qui affiche ces sorts
- [ ] **Limite d'emplacements actifs définie** : pas encore posée (aujourd'hui, un sort débloqué s'ajoute
      simplement au kit, sans limite ni remplacement d'un autre sort actif) — à trancher avec T10

## Tests automatiques exigés
Tests de règles — le bot de référence utilise les 3 capstones quand ils sont débloqués (`UpgradeBalanceTests`).
Pas de test dédié à une « barre dynamique » puisqu'elle n'existe pas encore côté présentation.

## Impact équilibrage
Oui. Voir D-083 : bug de famine de mana trouvé et corrigé (les capstones ne se déclenchaient jamais), verdict
partiel restant à valider.
