---
id: E02-T03
epic: E02
titre: Points de talent, paliers, prérequis et réinitialisation (respec)
type: Feature
priorité: P1
phase: 2
statut: En cours
taille: M
dépendances: E02-T01, E02-T06
---

# E02-T03 — Points de talent, paliers, prérequis et réinitialisation (respec)

## Contexte
Le joueur doit pouvoir essayer d'autres builds sans blocage.

## Critères d'acceptation
- [x] Règles d'attribution des points (E02-T06 : niveau du Soigneur → `Wallet.TalentPoints`) et de prérequis
      (palier N exige le palier N-1 DE LA MÊME voie — pas de DAG/branchement, une voie reste une liste linéaire)
- [x] Respec complet (`Workshop.RespecTalents`, gratuit, vide `Loadout.Talents` — les points reviennent car ils
      sont recalculés depuis ce dictionnaire, pas un registre séparé). Pas de respec PARTIEL (palier par
      palier) distinct : changer une option dans un palier déjà acheté était déjà gratuit avant ce ticket.
- [x] État de spécialisation pur et sérialisable (`Loadout.Talents`, `ProfileStore` — testé)

## Tests automatiques exigés
Tests de règles (`WorkshopTests`, dont un bug corrigé au passage dans `PlayerProfile.Repair` : validation des
paliers achetés par voie, pas par un compteur global — voir D-084). Tests de chemins (E08-T11) : pas encore
(E08 non commencé).

## Impact équilibrage
Oui : les chemins de choix sont testés à chaque niveau (D-084, `UpgradeBalanceTests`, 470 mesures). Verdict
partiel — voir D-084.
