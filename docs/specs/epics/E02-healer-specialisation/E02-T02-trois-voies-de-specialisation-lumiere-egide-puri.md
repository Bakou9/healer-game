---
id: E02-T02
epic: E02
titre: Quatre voies de spécialisation : Lumière, Égide, Purification, Vitalité
type: Design
priorité: P0
phase: 2
statut: En cours
taille: L
dépendances: E02-T01
---

# E02-T02 — Quatre voies de spécialisation : Lumière, Égide, Purification, Vitalité

## Contexte
Définir l'identité et les talents de chaque voie (voir VISION.md §6).

**Titre et périmètre mis à jour (D-084)** : décision ferme de l'utilisateur, 4 voies × 12 paliers (remplace la
Proposition « 3 voies × 4 paliers » de D-083, elle-même remplaçant le nombre non fixé de VISION.md §6).

## Critères d'acceptation
- [x] 4 voies × 12 paliers × 2 choix documentés (`core/content/upgrades.json`, 48 paliers, 96 options) :
      Lumière (soin), Égide (bouclier), Purification (purge/mana), **Vitalité** (robustesse + soutien généraliste,
      nouvelle voie, D-084) — chaque voie a son palier 12 « capstone » qui débloque un sort exclusif (Miracle /
      Dôme / Renaissance / Sève Vitale, E02-T05)
- [x] Chaque voie a un fantasme, une force et un point faible propres aux deux options de chaque palier
      (efficacité vs puissance brute, etc.)
- [x] **Décision de l'utilisateur consignée (nombre de voies/paliers)** : 4 voies × 12 paliers, ferme (D-084,
      question directe posée et tranchée — pas une Proposition).

## Tests automatiques exigés
Tests d'intégrité des données — verts. Batterie d'équilibrage (`UpgradeBalanceTests`, 470 mesures) : voir D-084,
23 échecs restants (dominance/écart entre options sur 16+8 paliers, 2 « pas trivial », 2 sur le plafond de
puissance) — causes systémiques déjà corrigées à plusieurs reprises, reste un réglage fin à trancher avec
l'utilisateur avant de clore ce ticket.

## Impact équilibrage
Oui : contenu de départ de la batterie E08. Mesuré (D-084) ; verdict partiel, à valider avec l'utilisateur.
