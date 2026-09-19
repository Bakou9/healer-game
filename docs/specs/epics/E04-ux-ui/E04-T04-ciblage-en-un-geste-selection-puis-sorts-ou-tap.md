---
id: E04-T04
epic: E04
titre: Ciblage en un geste (sélection persistante puis sorts)
type: Feature
priorité: P0
phase: 2
statut: Terminé
taille: M
dépendances: E04-T01
---

# E04-T04 — Ciblage en un geste (sélection persistante puis sorts)

## Contexte
Avant : armer un sort puis toucher un allié = 2 étapes à chaque ordre, trop lent en temps réel.

**Spec amendée (préambule A, décision D-019).** La spec initiale prévoyait aussi un « geste rapide pour le soin par défaut » (toucher une carte lance le soin). Remis en cause et **retiré** : il entre en conflit avec la sélection (toucher une carte pour viser un bouclier gaspillerait du mana en lançant un soin) et il rapproche du « soin automatique », alors que choisir QUI soigner est la décision centrale du jeu (pilier 1). Remplacé par une **sélection persistante** : re-soigner la même cible ne coûte qu'un geste.

## Critères d'acceptation
- [x] Toucher un allié vivant le sélectionne (bordure jaune, « Cible : nom ») ; un allié K.O. ne se sélectionne pas
- [x] Les sorts ciblés s'appliquent à l'allié sélectionné ; la sélection reste après un sort (re-soigner la même cible = 1 geste)
- [x] Annulation évidente : retoucher la carte annule ; la sélection est abandonnée à la mort de l'allié
- [x] Un sort ciblé sans sélection ne lance rien et affiche « Choisissez d'abord un allié ! » ; un sort de zone se lance sans cible
- [x] Aucun ciblage automatique (préserve la décision centrale)
- [x] Sensibilité de l'équilibre au délai de décision mesurée (voir revue)

## Tests automatiques exigés
`src/ui/targeting.test.ts` (sélection, annulation, mort de la cible, résolution des sorts, persistance).

## Impact équilibrage
Oui, indirect : la simulation ne change pas, mais le rythme d'action d'un humain augmente. Mesuré : de 100 à 500 ms de délai de décision, résultats identiques (100 % de victoires, PV minimum moyen ≈ 28 %) ; un joueur plus rapide ne rend donc pas le jeu plus facile que le modèle actuel. Détail et points à valider dans `docs/REVUES.md`.
