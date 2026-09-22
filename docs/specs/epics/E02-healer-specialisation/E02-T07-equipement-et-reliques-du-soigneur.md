---
id: E02-T07
epic: E02
titre: Équipement et reliques du soigneur
type: Feature
priorité: P3
phase: 4
statut: En cours
taille: L
dépendances: E02-T04
---

# E02-T07 — Équipement et reliques du soigneur

## Contexte
Couche de personnalisation supplémentaire, à n'ouvrir qu'après validation du cœur. L'équipement (piste par
personnage) existait déjà avant ce ticket ; ce qui est nouveau ici, ce sont les RELIQUES.

## Critères d'acceptation
- [x] Emplacements et raretés définis : pas de rareté (hors scope actuel), mais un vrai choix — au plus
      `UpgradeCatalog.MaxEquippedRelics` (2) équipées à la fois parmi celles possédées, achetées à l'or
      (`Workshop.BuyRelic`/`EquipRelic`/`UnequipRelic`)
- [x] Effets exprimés comme des talents (même modèle générique `UpgradeEffect`, appliqués par `LoadoutApplier`)
- [ ] **Intégrés à l'espace de builds testé (E08-T03)** : pas fait, E08 n'existe pas encore

## Tests automatiques exigés
Tests E08 étendus à l'équipement : pas encore (E08 non commencé). `UpgradeTests`/`WorkshopTests` couvrent
l'achat, l'équipement et l'application des reliques.

## Impact équilibrage
Oui : augmente l'espace de builds. **Pas encore mesuré séparément** (D-084) : les reliques n'ont pas d'écran
pour les acheter/équiper (voir critère d'acceptation manquant), donc un joueur ne peut pas encore les utiliser
en jeu — l'impact réel sur l'équilibrage attendra que l'UI existe.

## À faire avant de considérer ce ticket terminé
Écran d'achat/équipement des reliques dans l'Atelier (pas fait faute de place à l'écran — D-084).
