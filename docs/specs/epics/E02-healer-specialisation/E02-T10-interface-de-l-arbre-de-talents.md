---
id: E02-T10
epic: E02
titre: Interface de l'arbre de talents
type: Feature
priorité: P1
phase: 2
statut: En cours
taille: L
dépendances: E02-T03
---

# E02-T10 — Interface de l'arbre de talents

## Contexte
Le joueur doit comprendre ses choix et leurs conséquences avant de les faire.

**Dépendance à E04-T03 retirée (D-084)** : ce ticket visait « la refonte de la mise en page PORTRAIT » côté
Phaser/mobile. La version Unity actuelle (D-033) est en paysage 1280×720 (PC d'abord, cf. `unity-version/CLAUDE.md`) :
la dépendance ne s'applique plus telle quelle. L'écran construit ici (voir ci-dessous) vit dans cette mise en
page paysage existante ; une passe mobile/portrait dédiée reste à faire séparément si besoin.

## Critères d'acceptation
- [x] Arbre lisible : une voie (12 paliers) à la fois par onglets, liste verticale défilable — PAS un graphe
      à embranchements (le modèle de prérequis actuel est linéaire par voie, pas un DAG ; voir D-084)
- [x] Aperçu de l'effet et du coût avant validation : nom, description, coût en points, statut (verrouillé/
      acquis/actif) affichés sur chaque option avant de cliquer
- [ ] **Comparaison avec le build actuel** : pas fait — pas de vue « avant/après » chiffrée d'un changement de
      talent avant de le valider (changer coûte de toute façon rien une fois le palier acheté, donc le risque
      d'un mauvais choix est faible, mais l'aperçu n'existe pas)

## Tests automatiques exigés
Tests visuels (E04-T13) : n'existe pas dans ce dépôt. **Aucune vérification visuelle faite** dans cette session
(pas d'accès à l'Éditeur Unity) — seule la compilation (build Unity headless) a été vérifiée. Voir D-084,
section « Non vérifié ».

## Impact équilibrage
Aucun (présentation seule).
