---
id: E12-T03
epic: E12
titre: Effets visuels : soins, boucliers, poison, télégraphes
type: Feature
priorité: P2
phase: 3
statut: En cours
taille: L
dépendances: E12-T01
---

# E12-T03 — Effets visuels : soins, boucliers, poison, télégraphes

## Contexte
Le feedback du soigneur est le cœur du plaisir de jeu.

## Critères d'acceptation
- [x] Jets de particules pour soin, bouclier (avec anneau), poison, purge et impact, réutilisant les particules (Object Pool)
- [x] Cohérents avec les états de la simulation (déclenchés par les événements de combat)
- [x] Le Golem réagit : cœur qui pulse, s'emballe pendant un télégraphe, orange en phase 2 ; secousse d'écran brève sur l'attaque de zone et le changement de phase
- [ ] Lisibles et désactivables (réglage joueur, mode animations réduites : E04-T12)
- [ ] Plafond de particules et mesure de coût sur appareil d'entrée de gamme (E11-T02)

## Tests automatiques exigés
Captures (E04-T13).

## Impact équilibrage
Aucun sur les règles. Risque de lisibilité (secousse, particules) surveillé : effets brefs, translucides, secousse de 240 ms à faible amplitude ; à confirmer au test de jeu.
