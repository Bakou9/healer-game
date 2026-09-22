---
id: E02-T02
epic: E02
titre: Trois voies de spécialisation : Lumière, Égide, Purification
type: Design
priorité: P0
phase: 2
statut: En cours
taille: L
dépendances: E02-T01
---

# E02-T02 — Trois voies de spécialisation : Lumière, Égide, Purification

## Contexte
Définir l'identité et les talents de chaque voie (voir VISION.md §6).

## Critères d'acceptation
- [x] 3 voies × 4 paliers × 2 choix documentés (`core/content/upgrades.json`, 12 paliers, 24 options) :
      Lumière (soin), Égide (bouclier), Purification (purge/mana) — chaque voie a son palier 4 « capstone »
      qui débloque un sort exclusif (Miracle / Dôme / Renaissance, E02-T05)
- [x] Chaque voie a un fantasme (soin brut / mitigation / utilité-mana), une force et un point faible propres
      aux deux options de chaque palier (efficacité vs puissance brute, etc.)
- [ ] **Décision de l'utilisateur consignée (nombre de voies/paliers)** : 3 voies × 4 paliers est une
      **Proposition** de ma part (VISION.md §6 ne fixait pas ce nombre) — pas encore validée par l'utilisateur.
      Voir D-083.

## Tests automatiques exigés
Tests d'intégrité des données — verts. Batterie d'équilibrage (`UpgradeBalanceTests`) : voir D-083, 3 tensions
ouvertes sur le palier capstone (10/11/12), pas encore résolues.

## Impact équilibrage
Oui : contenu de départ de la batterie E08. Mesuré (D-083) ; verdict partiel, à valider avec l'utilisateur.
