# UX — audit, principes et recommandations

Statut : audit réalisé (ticket E04-T01). Les corrections sont des tickets de
l'epic E04 ([index](specs/README.md)). L'utilisateur juge l'UX actuelle
« catastrophique » (décision D-013) : ce document nomme pourquoi et propose
comment y remédier.

## 1. Audit de l'interface actuelle

| # | Constat | Effet sur le joueur | Ticket |
|---|---|---|---|
| 1 | Toile 480×854 à résolution 1x, textes de 11 à 14 px | Texte flou et minuscule sur mobile haute densité | E04-T02 |
| 2 | Le centre de l'écran est vide ; boss, équipe et sorts sont éparpillés et petits | Regard qui cherche ; cibles tactiles petites | E04-T03 |
| 3 | Ciblage en deux étapes : armer un sort, puis toucher un allié | Trop lent en temps réel ; pas d'annulation évidente | E04-T04 |
| 4 | Pas de hiérarchie : PV du boss, PV des alliés, mana et journal ont le même poids | Il faut lire pour comprendre ; l'urgent n'est pas prioritaire | E04-T03, T05 |
| 5 | Le télégraphe est une ligne de texte rouge et un contour | Le danger est manqué en jouant | E04-T07 |
| 6 | Chiffres flottants illimités et minuscules (12 px sur le boss) | Écran encombré, rien de lisible en combat intense | E04-T08 |
| 7 | Pas de menu, de tutoriel, de bilan ; pause = une icône | Le joueur ne sait ni quoi faire ni pourquoi il perd | E04-T09, T10, T11 |
| 8 | Rouge/vert comme seule différenciation ; rôles distingués par la couleur seule | Inutilisable pour un joueur daltonien | E04-T12 |
| 9 | Personnages et boss = rectangles | Aucune identité, rôles peu identifiables | E12-T01, T02 |
| 10 | Sort indisponible = simple transparence | On ne sait pas si c'est le mana ou la recharge | E04-T06 |
| 11 | Statuts en texte (« Poison 5,3s ») | Illisible d'un coup d'œil | E04-T05 |

## 2. Principes de conception

1. **Règle des 3 secondes.** Depuis n'importe quel instant du combat, le joueur
   sait en 3 s : qui est en danger, quel danger arrive, ce qu'il peut lancer.
2. **Deux gestes au plus** pour toute action de combat, dans la zone du pouce.
3. **Hiérarchie de l'urgent.** Danger imminent > PV bas d'un allié > état des
   sorts > le reste. L'urgent est plus gros, plus contrasté, plus haut dans la
   hiérarchie visuelle.
4. **Un langage visuel unique.** Danger = rouge + icône + son ; soin = vert +
   « + » ; bouclier = bleu + icône ; poison = violet + icône ; jamais la couleur
   seule.
5. **Tailles minimales.** Cibles tactiles ≥ 48 px ; texte ≥ 14 px effectifs
   (16 px pour l'essentiel) ; contraste ≥ 4,5:1.
6. **Le retour ne doit pas nuire à la lecture.** Nombre de chiffres simultanés
   plafonné, agrégation des coups rapprochés, effets désactivables.
7. **Valeurs à hauteur humaine.** Tronquées, courtes, unités claires (D-006).
8. **Toujours pardonner.** Annulation d'une sélection, confirmation seulement
   pour l'irréversible, pause fiable.
9. **Mesurer.** Les décisions UX se valident par playtest et métriques (E04-T14),
   les régressions visuelles par captures automatiques (E04-T13).

## 3. Recommandations, par ordre de valeur

1. **Ciblage en un geste (E04-T04) — réalisé.** Toucher un allié le sélectionne ;
   les sorts s'appliquent à la cible sélectionnée et **la sélection reste** après un
   sort (re-soigner la même cible = un seul geste). Le sort de zone reste en un tap.
   Annulation : retoucher la carte. *Amendé (D-019)* : le « toucher rapide = soin par
   défaut » initialement prévu est retiré (il gaspillerait du mana en cas de
   sélection pour un bouclier, et rapproche du soin automatique).
2. **Refonte de la mise en page (E04-T03).** Boss et jauge de télégraphe en haut ;
   équipe dans le tiers inférieur ; **barre de sorts dans la zone du pouce** ; le
   mana du soigneur directement au-dessus des sorts.
3. **Netteté (E04-T02).** Résolution adaptée au ratio de pixels, zones sûres,
   tailles de texte par jetons de design.
4. **Télégraphes (E04-T07).** Jauge qui se vide sous le boss, icône de danger,
   liseré rouge en bord d'écran, son ; la cible ou la zone visée est mise en évidence.
5. **Cartes d'alliés (E04-T05).** Barre de PV large avec seuils (vert/orange/rouge
   + motif), statuts en icônes avec durée, rôle par forme, K.O. évident.
6. **Barre de sorts (E04-T06).** Recharge en balayage radial, mana insuffisant
   en état distinct avec le coût mis en évidence, icône de sort, raccourcis clavier.
7. **Retours maîtrisés (E04-T08).** Plafond de chiffres, agrégation, priorité aux
   soins et dégâts sur alliés, discrétion sur les coups au boss.
8. **Parcours complet (E04-T09, T10, T11).** Menu → équipe/build → boss → combat
   → bilan ; tutoriel qui introduit un sort à la fois ; bilan expliquant la défaite.
9. **Accessibilité (E04-T12).** Palette et formes alternatives, texte réglable,
   gaucher, animations réduites.
10. **Identité (E12).** Direction artistique et portraits : les rectangles sont provisoires.

## 4. Disposition cible (portrait, 480×854, indicative)

```
  0–56   Nom du boss · barre de PV large · pause
 56–250  Boss (zone visuelle) + jauge de télégraphe sous le boss
250–320  Bandeau : phase du boss · alertes · journal court (2 lignes)
320–600  Équipe : 4 cartes larges (PV, statuts en icônes, rôle)
600–690  Mana du soigneur (barre large) · cible sélectionnée
690–854  Barre de sorts : 4 gros boutons (zone du pouce)
```

## 5. Impact sur l'équilibrage

Changer le ciblage (rythme d'action possible) ou ajouter des contrôles clavier
**modifie ce qu'un humain peut réaliser** : les profils de référence de
`docs/EQUILIBRAGE.md` (délai de décision) doivent être ré-évalués (E04-T04,
E11-T06). Un télégraphe plus lisible renforce le critère d'équité (§2.5 de
EQUILIBRAGE.md).

## 6. Avancement

Réalisés (2026-09-19) : E04-T02 (netteté), E04-T03 (mise en page), E04-T04
(ciblage), E04-T05 (cartes). Restent notamment : télégraphes (T07), barre de
sorts (T06), retours maîtrisés (T08), bilan (T09), menus (T10), tutoriel (T11).

## 7. Prochaine tranche recommandée (historique)

E04-T02 (netteté) → E04-T03 (mise en page) → E04-T04 (ciblage) → E04-T05
(cartes) → E04-T07 (télégraphes). C'est la plus petite série qui transforme
l'expérience ; elle se vérifie avec des captures automatiques (E13-T05, E04-T13).
