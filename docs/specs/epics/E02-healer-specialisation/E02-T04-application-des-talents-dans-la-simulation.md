---
id: E02-T04
epic: E02
titre: Application des talents dans la simulation
type: Feature
priorité: P0
phase: 2
statut: En cours
taille: L
dépendances: E02-T01, E01-T08
---

# E02-T04 — Application des talents dans la simulation

## Contexte
Les talents modifient les sorts (valeurs, coûts, recharges) ou ajoutent des effets.

## Critères d'acceptation
- [x] Modificateurs appliqués sans `if` par talent (`LoadoutApplier.Apply`, générique sur `Stat`/`Skill.Field`,
      y compris le déverrouillage de sort par `UnlocksSkill` — un seul bloc générique, pas un `if` par capstone ;
      étend désormais aussi les reliques, E02-T07, D-084, avec le même modèle générique)
- [x] La simulation reçoit un build en entrée (déterminisme conservé — `LoadoutApplier` clone, ne mute jamais
      le contenu source ; `Battle` reste seedée)
- [x] Événements inchangés ou étendus proprement (aucun nouveau type d'événement ; les capstones émettent les
      événements `skillUsed`/`healed`/`shielded`/`castStarted` existants)

## Tests automatiques exigés
Tests unitaires par type de modificateur (`LoadoutApplierTests`, verts) ; goldens par voie : pas encore faits
(pas de golden dédié « avec talents » — seuls les goldens de base, sans loadout, existent). À ajouter si l'UI
de choix de talents (T05/T10) est reprise.

## Impact équilibrage
Oui. Mesuré (D-083 puis D-084, `UpgradeBalanceTests`, 100 seeds × 3 boss) : bug de fond trouvé et corrigé (les
sorts capstone ne se déclenchaient jamais — famine de mana, cf. D-083), puis rééquilibrage à l'échelle 4 voies
× 12 paliers (D-084). Verdict partiel : 23 mesures encore hors bornes sur 470, à valider avec l'utilisateur
avant de clore.
