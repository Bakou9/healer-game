# Spécification du jeu — Vision (GDD)

Statut : **brouillon de travail, à valider par l'utilisateur**. Ce qui est
marqué *Proposition* est une hypothèse de conception, pas une décision.
Les décisions fermes sont dans [`../DECISIONS.md`](../DECISIONS.md). Le détail
de réalisation est dans les epics et tickets ([index](README.md)).

## 1. Pitch

Un jeu de combat **en temps réel où l'on incarne uniquement le soigneur**. Le
reste de l'équipe se bat seul (auto-battle) ; le joueur sauve l'équipe par ses
décisions : quel allié soigner, quand poser un bouclier avant une attaque
télégraphiée, quand purger, comment gérer son mana. Le jeu est un **gacha** :
on collectionne des personnages pour composer l'équipe qu'on soigne, et on
**spécialise son soigneur** pour affronter des boss aux comportements variés.

## 2. Piliers de conception

1. **Des décisions de soigneur qui comptent.** Chaque sort a une place ; la
   compétence du joueur se voit dans le résultat.
2. **Lisibilité et équité.** Le joueur comprend ce qui se passe et pourquoi il
   perd. Les dangers sont annoncés (télégraphes), les effets sont visibles.
3. **Équilibrage mesuré, jamais supposé.** Chaque décision de spécialisation
   est validée par des tests automatiques (voir `docs/EQUILIBRAGE.md`, E08).
4. **Déterministe et autoritatif.** Un combat = une graine + des commandes ; il
   se rejoue à l'identique, donc se valide côté serveur (anti-triche, gacha
   honnête).
5. **Mobile d'abord, Steam ensuite.** Sessions courtes (60-120 s par combat),
   une main, portrait ; adaptation clavier/manette pour Steam.
6. **Respect du joueur.** Aucun mécanisme opaque : taux de tirage affichés,
   pas de puissance achetable qui casse l'équilibre, pas de dark patterns.

## 3. Public et plateformes

- Public : joueurs mobiles de jeux de collection/RPG, appréciant l'optimisation
  de builds ; joueurs de « healer » de MMO.
- Plateformes : **Android** d'abord (Capacitor), **Steam** ensuite
  (Electron/Tauri + steamworks.js). iOS : hors périmètre initial.
- Langues : français puis anglais.

## 4. Boucles de jeu

- **Micro (60-120 s)** : un combat contre un boss. Lire les télégraphes,
  répartir soins/boucliers/purges, tenir le mana, garder l'équipe en vie.
- **Méso (5-15 min)** : enchaîner des combats, gagner des récompenses,
  améliorer le soigneur et l'équipe, réessayer un boss avec un autre build.
- **Macro (jours/semaines)** : progresser dans la campagne, compléter la
  collection, viser des boss plus durs, événements et défis.

## 5. Le combat (règles)

Déjà réalisé (phase 1) : simulation déterministe ; alliés en auto-attaque ;
boss avec **pattern** d'actions et **télégraphe** des grosses attaques ;
**phases** (changement de comportement sous un seuil de PV) ; sorts de
soigneur (Soin, Soin de zone, Bouclier, Purge) avec **mana** et **temps de
recharge** ; **effets sur la durée** (poison) ; victoire = boss à 0 PV,
défaite = équipe à 0 PV.

À venir : effets supplémentaires (soin sur la durée, augmentation/réduction de
statistiques, étourdissement, vulnérabilité), résurrection, ciblage du boss
(menace, focus du plus faible), coups critiques à variance maîtrisée, boss
invoquant des ennemis, rencontres à plusieurs vagues.

**Règle d'or de présentation** : toutes les valeurs affichées sont tronquées à
hauteur humaine (voir `docs/EQUILIBRAGE.md` §3).

## 6. Le soigneur et sa spécialisation

Le joueur fait progresser **un** soigneur. À chaque palier de niveau, il fait
des **choix de spécialisation** (talents) parmi trois voies. *Proposition :*

| Voie | Fantasme | Force | Sorts/Talents typiques (exemples à valider) |
|---|---|---|---|
| **Lumière** | soigneur direct et réactif | soigne vite ce qui vient d'être touché | Soin rapide ou puissant ; bonus sous 35 % PV ; soin qui ricoche ; capstone « Miracle » |
| **Égide** | protecteur qui prévient | absorbe les grosses attaques annoncées | Bouclier lourd ou rapide ; bouclier offert pendant un télégraphe ; bouclier de zone ; capstone « Dôme » |
| **Purification** | gestionnaire de statuts et de mana | annule le poison et les effets, tient sur la durée | Purge économe ou de zone ; soin sur la durée ; régénération de mana ; capstone « Renaissance » |

Chaque voie comporte des **paliers** (ex. 4) avec un **choix entre 2 talents**
par palier. Les talents modifient des sorts existants ou en débloquent. Une
**réinitialisation** (respec) permet d'essayer un autre build.

**Exigence d'équilibre (non négociable)** : aucun build n'est inutile, aucun
n'est écrasant ; chaque voie a un **terrain de prédilection** (un type de boss
où elle brille) tout en restant viable partout. Toute la validation est
automatisée (epic E08).

## 7. Boss et archétypes

Chaque boss appartient à un **archétype** qui met la pression sur un aspect du
soin. *Proposition :*

| Archétype | Pression | Voie avantagée (sans être obligatoire) |
|---|---|---|
| **Burst** | grosses attaques de zone télégraphées | Égide |
| **Attrition** | poisons/dégâts sur la durée, combat long, mana limité | Purification |
| **Multi-cibles** | beaucoup de petits dégâts sur plusieurs alliés, adds | Lumière |
| **Punisseur** | cible le plus faible / le soigneur | selon build |
| **Compte à rebours** | enrage, phases rapides | selon build |

Boss 1 « Golem Ancestral » (réalisé) : attaques simples, attaque de zone
télégraphiée, phase « Fureur » à 50 % PV avec poison.

## 8. Équipe et personnages

- Équipe : le soigneur + **3 alliés** (4 emplacements au total).
- Rôles : **tank** (encaisse), **dégâts**, **soutien léger** (*Proposition*).
- Chaque personnage : classe, rareté, statistiques, une compétence automatique,
  éventuellement une synergie d'équipe. Auto-battle avec priorités de cible.
- Progression : niveaux, étoiles (doublons), équipement (*Proposition*).
- La puissance de l'équipe entre dans l'équilibrage (E06-T09, E08-T13).

## 9. Gacha et économie

- **Devises** (*Proposition*) : monnaie douce (gagnée en jouant), monnaie dure
  (tirages), jetons de bannière. Pas d'énergie/stamina bloquante (*à décider*).
- **Tirages** : tables de probabilités publiques, **garantie** (pity),
  **tirage effectué côté serveur** avec la même mécanique déterministe.
- **Éthique** : la puissance de combat achetable est **bornée** ; l'équilibre
  est testé rareté par rareté ; pas de mécanisme de pression abusive ; contrôle
  parental et affichage des dépenses ; conformité aux règles des magasins et
  aux réglementations sur les « loot boxes ».
- **Boutique/IAP** : Google Play Billing puis Steam. Contenu cosmétique et
  commodité en priorité.

## 10. Modes de jeu

Campagne (chapitres/étapes) ; défi quotidien (graine du jour) ; tour infinie
/ boss rush ; classements asynchrones avec replays validés ; événements
temporaires ; succès et quêtes. Ordre de livraison dans la feuille de route.

## 11. Expérience utilisateur (résumé)

Portrait, une main, zone du pouce ; ciblage en un geste ; informations
critiques toujours visibles ; télégraphes clairs ; accessibilité (daltonisme,
taille du texte, gaucher). Détail : `docs/UX.md` et epic E04.

## 12. Architecture (résumé)

Monolithe **modulaire** à frontières strictes ; simulation pure réutilisable
côté serveur ; contenu piloté par données validées par schémas ; registres
d'extension pour ajouter effets/talents/boss sans toucher au moteur. Détail :
`docs/ARCHITECTURE.md` et epic E09.

## 13. Persistance et serveur

Phase locale : sauvegarde versionnée. Phase en ligne : comptes, synchronisation,
**validation des combats par rejeu**, tirages et économie côté serveur,
télémétrie respectueuse de la vie privée (RGPD). Epic E10.

## 14. Non-objectifs (pour l'instant)

Multijoueur en temps réel ; contrôle direct des alliés ; monde ouvert ; iOS ;
échanges entre joueurs ; contenu généré par les joueurs.

## 15. Feuille de route par phases

| Phase | Objectif | Epics principaux |
|---|---|---|
| **1 — Prototype** (en cours) | Valider que le combat est amusant ; filet de tests | E01, E08 (base), E13 |
| **2 — Tranche verticale** | Spécialisation, 3-4 boss, UX refaite, architecture modulaire | E02, E03, E04, E08, E09 |
| **3 — Méta** | Équipe/roster, économie et tirages locaux, campagne | E05, E06, E07 |
| **4 — En ligne et Android** | Serveur autoritaire, sauvegarde cloud, bêta Android | E10, E11, E12 |
| **5 — Steam et exploitation** | Steam, classements, événements | E11, E07 |

Rappel : ce document **n'autorise pas** à implémenter une phase future avant
qu'elle soit demandée (voir `CLAUDE.md`).

## 16. Questions ouvertes (décisions attendues de l'utilisateur)

1. Nombre de voies et de paliers de spécialisation (3 voies × 4 paliers proposés) ?
2. Le soigneur est-il un personnage unique ou choisit-on parmi plusieurs soigneurs ?
3. Énergie/stamina : oui ou non ?
4. Monétisation : IAP dès la bêta ou seulement après le lancement ?
5. Direction artistique (2D dessinée, pixel art, minimaliste) ?
6. Combien de personnages de lancement, et quelles raretés ?
7. Cibler d'abord Android seul, ou Android et Steam en parallèle ?
8. Mode hors ligne complet ou connexion obligatoire ?
