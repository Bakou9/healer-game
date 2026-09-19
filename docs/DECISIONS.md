# Journal des décisions

Mémoire persistante des décisions et exigences de l'utilisateur. **Règle
(voir `CLAUDE.md`)** : toute nouvelle décision, exigence ou correction de
l'utilisateur est ajoutée ici (daté, numéroté), rattachée à un ou plusieurs
tickets (`docs/specs/`), et respectée ensuite. On n'efface pas une décision :
on la **remplace** par une nouvelle qui la cite.

Statuts : **Ferme** (à appliquer) · **À préciser** (information manquante) ·
**Proposition** (hypothèse de l'agent, à valider) · **Remplacée**.

---

### D-001 — Pile technique
- Date : origine du projet · Statut : Ferme
- Décision : TypeScript + Phaser 3 + Vite ; Android via Capacitor ; build Steam plus tard (Electron ou Tauri + steamworks.js).
- Pourquoi : un seul code pour mobile et PC, itération rapide.
- Tickets : E11-T01, E11-T03

### D-002 — Simulation pure, déterministe, pilotée par les données
- Date : origine du projet · Statut : Ferme
- Décision : `src/sim/` sans Phaser/DOM/horloge/`Math.random` ; tout le contenu de jeu en JSON ; combat rejouable depuis (seed, commandes).
- Pourquoi : testabilité, validation serveur future, gacha honnête.
- Tickets : E01-T01, E01-T12, E09-T01

### D-003 — Rechargement automatique sans F5
- Date : 2026-09-19 · Statut : Ferme
- Décision : le jeu se recharge tout seul à chaque modification, y compris dans un navigateur externe ; port verrouillé ; lancement par `lancer-le-jeu.bat`.
- Réalisé : Vite (rechargement à chaud), `strictPort`, script de lancement.
- Tickets : E13-T05

### D-004 — Non-régression obligatoire et régressions expliquées
- Date : 2026-09-19 · Statut : Ferme (priorité « TRÈS TRÈS TRÈS importante »)
- Décision : des tests automatiques détectent les régressions ; chaque régression est **expliquée** à l'utilisateur (cause, verdict voulu ou accidentel) ; on ne modifie jamais un test ou une référence pour faire passer.
- Réalisé : références golden, tests d'équilibrage, d'intégrité des données, d'architecture ; protocole dans `CLAUDE.md`.
- Tickets : E13-T01, E13-T09

### D-005 — Patterns de développement de jeu vidéo
- Date : 2026-09-19 · Statut : Ferme
- Décision : `docs/PATTERNS_JEU_VIDEO.md` est la référence ; on cite les patterns appliqués dans chaque compte rendu ; on n'adopte un pattern différé qu'à l'atteinte de son déclencheur.
- Tickets : E09-T04, E01-T08

### D-006 — Valeurs à hauteur humaine
- Date : 2026-09-19 · Statut : Ferme
- Décision : toute valeur affichée est **tronquée** et lisible (`7000`, `12,3k`, `4,9s`) via `src/ui/format.ts` ; les données de jeu sont des entiers ronds (sauf multiplicateurs et ratios).
- Tickets : E04-T05, E11-T05

### D-007 — L'équilibrage est un critère central
- Date : 2026-09-19 · Statut : Ferme
- Décision : `docs/EQUILIBRAGE.md` définit un jeu équilibré (6 critères mesurables) et sert de référence à chaque changement de valeurs ; on ne relâche jamais une borne pour faire passer un test.
- Tickets : E08-T01, E08-T10

### D-008 — Dépôt git et GitHub
- Date : 2026-09-19 · Statut : Ferme (en cours)
- Décision : dépôt git initialisé ; poussé sur GitHub (privé). En attente de l'adresse du dépôt distant créé par l'utilisateur.
- Tickets : E13-T04

### D-009 — Améliorations de l'alpha
- Date : 2026-09-19 · Statut : Ferme
- Décision : poison et Purge fonctionnelle, phase 2 du boss, bot de référence « humain » (500 ms), chiffres flottants, statuts, compte à rebours, journal de combat, valeurs tronquées.
- Réalisé : voir commit « Poison, phase de boss, chiffres lisibles… ».
- Tickets : E01-T05, E01-T06, E03-T02, E08-T01

### D-010 — Spécification complète en epics et tickets, persistante
- Date : 2026-09-19 · Statut : Ferme
- Décision : le jeu est spécifié en entier (`docs/specs/VISION.md`) et découpé en epics (dossiers) et tickets (fichiers), validés par un test de cohérence.
- Tickets : E13-T03

### D-011 — Équilibrage validé entre chaque décision de spécialisation
- Date : 2026-09-19 · Statut : Ferme
- Décision : le joueur spécialise son soigneur par des choix ; **chaque choix et chaque suite de choix** est validé automatiquement (viabilité, non-dominance, niche, ablation, parité, chemins par niveau) et le rapport d'équilibrage est versionné.
- Tickets : E08-T02, E08-T03, E08-T05, E08-T06, E08-T07, E08-T08, E08-T09, E08-T10, E08-T11, E02-T02

### D-012 — Tout ce qui viendra doit être noté dans un support persistant
- Date : 2026-09-19 · Statut : Ferme
- Décision : ce journal et les tickets sont la mémoire du projet ; `CLAUDE.md` oblige à les tenir à jour.
- Tickets : E13-T02

### D-013 — L'UX actuelle est jugée catastrophique : refonte
- Date : 2026-09-19 · Statut : Ferme
- Décision : refaire l'UX selon `docs/UX.md` (audit et recommandations) ; epic E04.
- Tickets : E04-T01, E04-T02, E04-T03, E04-T04, E04-T05, E04-T07

### D-014 — Architecture plus modulaire
- Date : 2026-09-19 · Statut : À préciser (la phrase de l'utilisateur est coupée : « Le projet va être large, il faut… »)
- Décision provisoire : monolithe modulaire à frontières strictes (`docs/ARCHITECTURE.md`), avec extension par registres et contenus validés par schémas.
- Question ouverte : quelle contrainte exacte l'utilisateur voulait-il ajouter ?
- Tickets : E09-T01, E09-T02, E09-T03, E09-T04, E09-T05

### D-015 — Hypothèses de conception à valider
- Date : 2026-09-19 · Statut : Proposition
- Contenu : trois voies de spécialisation (Lumière, Égide, Purification) × 4 paliers × 2 choix ; archétypes de boss (Burst, Attrition, Multi-cibles, Punisseur, Compte à rebours) ; équipe de 4 emplacements ; questions ouvertes de `VISION.md` §16.
- Tickets : E02-T02, E03-T01, E05-T02
