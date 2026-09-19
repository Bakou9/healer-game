# Healer Game — Prototype (Phase 1)

Prototype de combat pour un jeu gacha où l'on incarne un·e soigneur·se en
temps réel. Le reste de l'équipe (tank, 2 DPS) est en auto-battle : le
joueur ne gère que les soins, boucliers et purges, pendant qu'un boss
télégraphie ses grosses attaques.

Ceci correspond à la **phase 1** du plan : un seul écran, sans monétisation,
pour vérifier que le combat est amusant avant d'aller plus loin.

## Démarrer

Prérequis : [Node.js](https://nodejs.org) (LTS, 20 ou plus).

```bash
npm install
npm run dev
```

Ouvre ensuite l'URL affichée (en général `http://localhost:5173`) dans un
navigateur. Le jeu tourne aussi bien à la souris (clic) qu'au tactile.

## Comment on joue

- Le tank et les DPS attaquent automatiquement.
- Le boss télégraphie sa grosse attaque de zone quelques secondes à l'avance
  (texte d'avertissement + contour rouge).
- Le joueur choisit une compétence de soin en bas de l'écran :
  - **Soin de zone** se lance immédiatement sur tout le groupe.
  - **Soin**, **Bouclier**, **Purge** demandent une cible : appuyer sur le
    bouton "arme" la compétence, puis un tap sur un allié la lance sur lui.
- Chaque compétence a un coût en mana et un temps de recharge, affichés sur
  son bouton.

## Structure du projet

```
src/
  sim/            Simulation de combat, en TypeScript pur, sans Phaser.
    Battle.ts           La classe principale : step(dt), issueCommand(...), getters d'état.
    rng.ts              PRNG déterministe (seedé) pour un combat rejouable à l'identique.
    encounter.ts        Assemble alliés + boss + seed en une "rencontre".
    referenceHealerBot.ts  Un bot de soin "raisonnable", utilisé pour vérifier l'équilibrage.
    Battle.test.ts      Tests (déterminisme, victoire/défaite, mana, bouclier, télégraphie).
  scenes/
    BattleScene.ts  Rendu Phaser + gestion des taps. Ne fait AUCUN calcul de jeu :
                    il lit l'état de Battle et lui envoie des commandes.
  data/
    characters.json, skills.json, boss1.json   Toutes les valeurs de jeu (pas de code en dur).
  main.ts           Point d'entrée, config Phaser.
```

**Pourquoi séparer `sim/` du rendu ?** La simulation est testable sans
navigateur, rejouable à l'identique (même seed + mêmes commandes = même
résultat), et réutilisable telle quelle si un jour la validation des combats
doit se faire côté serveur (indispensable pour un gacha, afin d'éviter la
triche sur les tirages et les récompenses).

## Documents de référence

- `CLAUDE.md` : règles de travail (architecture, non-régression, valeurs lisibles).
- `docs/PATTERNS_JEU_VIDEO.md` : patterns de développement à appliquer.
- `docs/specs/README.md` : spécifications (vision, 13 epics, tickets) ; `docs/DECISIONS.md` : journal des décisions.
- `docs/ARCHITECTURE.md` (architecture modulaire cible) et `docs/UX.md` (audit et principes UX).
- `docs/EQUILIBRAGE.md` : ce qu'est un combat équilibré ici, mesures et réglages.

## Scripts utiles

| Commande | Effet |
|---|---|
| `npm run dev` | Lance le serveur de développement (rechargement à chaud) |
| `npm test` | Lance les tests de la simulation (Vitest) |
| `npm run check` | Types + tests + build : à passer avant de considérer un changement terminé |
| `npm run test:update-golden` | Régénère les combats de référence (uniquement après validation d'un changement voulu) |
| `npm run specs:index` | Régénère l'index des spécifications après un changement de ticket |
| `npm run typecheck` | Vérifie les types TypeScript sans rien construire |
| `npm run build` | Vérifie les types puis construit `dist/` (web + base pour Android) |

## Publier sur Android (test)

1. Installer [Android Studio](https://developer.android.com/studio) (inclut le SDK).
2. `npm run cap:add:android` — génère le dossier `android/` (à committer).
3. `npm run cap:sync` — build web + copie dans le projet Android.
4. `npm run cap:open:android` — ouvre Android Studio pour lancer sur un
   appareil ou un émulateur, ou produire un AAB à envoyer sur Google Play
   Console (test fermé).

À tester tôt sur un vrai téléphone d'entrée de gamme : c'est le principal
risque de cette stack (performances dans la WebView Android).

## Et pour Steam ?

Pas encore dans ce prototype. Le plan (voir la conversation) prévoit un
enrobage Electron ou Tauri une fois le vertical slice (phase 2) prêt, avec
`steamworks.js` pour l'intégration Steamworks.

## Prochaines étapes (phase 2)

- Plus de niveaux, plus de personnages avec des synergies.
- Un arbre de compétences pour le soigneur.
- Sauvegarde locale.
- Premiers vrais visuels (les rectangles colorés sont volontairement
  temporaires : l'important est de valider le gameplay avant l'art).
