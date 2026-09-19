---
id: E04-T05
epic: E04
titre: Cartes d'alliés lisibles
type: Feature
priorité: P0
phase: 2
statut: Terminé
taille: M
dépendances: E04-T03
---

# E04-T05 — Cartes d'alliés lisibles

## Contexte
Les informations des alliés étaient noyées, petites et peu contrastées.

## Critères d'acceptation
- [x] Barre de PV large (18 px) avec seuils de couleur (vert, orange, rouge) et valeur numérique toujours affichée (la couleur ne porte jamais seule l'information)
- [x] Statut affiché en pastille lisible avec sa durée (icônes finales : E12)
- [x] Rôle identifiable par un libellé (Tank, Dégâts, Soin) en plus de la couleur du portrait (formes distinctives : E04-T12 et E12)
- [x] État K.O. évident (carte assombrie, libellé « K.O. »), bouclier affiché avec sa valeur

## Tests automatiques exigés
`src/ui/layout.test.ts` (seuils de couleur des PV) ; `src/ui/format.test.ts` (valeurs tronquées). Captures automatiques : E04-T13.

## Impact équilibrage
Aucun sur les règles. Renforce le critère d'équité (`docs/EQUILIBRAGE.md` §2.5) : les états dangereux sont plus visibles.
