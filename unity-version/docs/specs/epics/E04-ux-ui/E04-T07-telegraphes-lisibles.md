---
id: E04-T07
epic: E04
titre: Télégraphes lisibles
type: Feature
priorité: P0
phase: 2
statut: En cours
taille: M
dépendances: E03-T01
---

# E04-T07 — Télégraphes lisibles

## Contexte
Le danger doit être perceptible sans lire un texte.

## Critères d'acceptation
- [ ] Jauge de compte à rebours + icône + flash de bord d'écran — **Unity : jauge et liseré rouge pulsé réalisés, icône restante** (durée totale du télégraphe exposée par le cœur, `Telegraph.TotalMs`)
- [ ] Cible/zone annoncée
- [x] Doublé par un signal non visuel (accessibilité) — bip d'alerte au début du télégraphe et grondement à l'impact (`SoundKit`)

## Tests automatiques exigés
Captures ; test du minuteur affiché.

## Impact équilibrage
Oui : fait respecter le critère d'équité (EQUILIBRAGE.md §2.5).
