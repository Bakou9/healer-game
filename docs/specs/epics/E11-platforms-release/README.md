---
id: E11
titre: Plateformes et publication
---

# E11 — Plateformes et publication

## Objectif
Livrer sur Android puis Steam avec des performances et une qualité mesurées.

## Périmètre
Builds, performances, publication, localisation, contrôles.

## Hors périmètre
Contenu du jeu.

## Critères de sortie de l'epic
- Build Android jouable à 60 FPS sur appareil d'entrée de gamme.
- Chaque publication passe `npm run check` et les budgets de performance.

## Tickets
<!-- TICKETS:START -->
| Ticket | Titre | Type | Priorité | Phase | Statut | Dépend de |
|---|---|---|---|---|---|---|
| [E11-T01](E11-T01-build-android-capacitor-et-test-sur-appareil-d-e.md) | Build Android (Capacitor) et test sur appareil d'entrée de gamme | Tech | P1 | 4 | À faire | E04-T02 |
| [E11-T02](E11-T02-budgets-de-performance-mesures-automatiquement.md) | Budgets de performance mesurés automatiquement | Test | P1 | 4 | À faire | E11-T01 |
| [E11-T03](E11-T03-build-steam-electron-ou-tauri-steamworks-js.md) | Build Steam (Electron ou Tauri + steamworks.js) | Tech | P2 | 5 | À faire | E09-T10 |
| [E11-T04](E11-T04-publication-google-play-test-ferme-puis-producti.md) | Publication Google Play (test fermé puis production) | Feature | P2 | 4 | À faire | E11-T01 |
| [E11-T05](E11-T05-localisation-fr-en.md) | Localisation FR/EN | Feature | P2 | 4 | À faire | E04-T10 |
| [E11-T06](E11-T06-controles-clavier-et-manette-steam.md) | Contrôles clavier et manette (Steam) | Feature | P3 | 5 | À faire | E04-T06 |
| [E11-T07](E11-T07-point-de-decision-moteur-phaser-ou-unity.md) | Point de décision moteur : Phaser ou Unity | Design | P2 | 4 | À faire | E11-T01, E11-T02 |
<!-- TICKETS:END -->
