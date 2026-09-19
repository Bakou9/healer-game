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
- Date : 2026-09-19 · Statut : Remplacée par D-025 (la phrase coupée de l'utilisateur est abandonnée)
- Décision provisoire : monolithe modulaire à frontières strictes (`docs/ARCHITECTURE.md`), avec extension par registres et contenus validés par schémas.
- Question ouverte : quelle contrainte exacte l'utilisateur voulait-il ajouter ?
- Tickets : E09-T01, E09-T02, E09-T03, E09-T04, E09-T05

### D-015 — Hypothèses de conception à valider
- Date : 2026-09-19 · Statut : Proposition
- Contenu : trois voies de spécialisation (Lumière, Égide, Purification) × 4 paliers × 2 choix ; archétypes de boss (Burst, Attrition, Multi-cibles, Punisseur, Compte à rebours) ; équipe de 4 emplacements ; questions ouvertes de `VISION.md` §16.
- Tickets : E02-T02, E03-T01, E05-T02

### D-016 — Remettre systématiquement en question les specs
- Date : 2026-09-19 · Statut : Ferme
- Décision : avant chaque ticket, je me demande si la spec est encore la meilleure option pour le gameplay ; si une alternative apporte quelque chose, je la propose et j'amende la spec (même pour du travail déjà fait), en expliquant, en consignant et en le signalant. Une décision ferme de l'utilisateur ne se change pas sans son accord.
- Réalisé : préambule systématique dans `CLAUDE.md` ; première application sur E04-T04 (voir `docs/REVUES.md`).
- Tickets : E13-T02, E04-T04

### D-017 — Question d'équilibrage à chaque ticket, validation conjointe en cas de doute
- Date : 2026-09-19 · Statut : Ferme
- Décision : pour chaque ticket, se demander si l'équilibre peut changer ; mesurer ; si le jeu ne paraît plus équilibré, l'expliquer à l'utilisateur et valider ensemble avant de continuer. La réponse est consignée dans `docs/REVUES.md`, et un test refuse un ticket terminé sans revue.
- Tickets : E13-T02, E08-T10

### D-018 — Priorité à la série UX E04-T02 à T05
- Date : 2026-09-19 · Statut : Ferme
- Décision : commencer par la netteté, la mise en page, le ciblage en un geste et les cartes d'alliés lisibles.
- Tickets : E04-T02, E04-T03, E04-T04, E04-T05

### D-019 — Amendement de E04-T04 : sélection persistante, pas de soin par tap
- Date : 2026-09-19 · Statut : Ferme (première application de D-016)
- Décision : le « geste rapide pour le soin par défaut » est retiré de la spec du ciblage ; remplacé par une sélection persistante (toucher un allié le sélectionne, les sorts s'y appliquent, la sélection reste après le sort). Aucun ciblage automatique.
- Pourquoi : le tap-pour-soigner entre en conflit avec la sélection (viser un bouclier lancerait un soin et gaspillerait du mana) et rapproche du soin automatique, alors que choisir qui soigner est la décision centrale du jeu (pilier 1).
- Tickets : E04-T04

### D-020 — Profils humains à délais variables
- Date : 2026-09-19 · Statut : Proposition
- Constat : le bot de référence décide à intervalle fixe ; ses résultats sont non monotones (800 ms pire que 1000 ms) car son rythme se cale sur la fenêtre de 1,4 s des télégraphes. Un humain a un délai moyen avec dispersion.
- Proposition : modéliser les profils avec un délai tiré d'une distribution (Rng seedé). Les bornes d'équilibrage seront ré-évaluées et les écarts validés avec l'utilisateur.
- Tickets : E08-T15

### D-021 — Toujours rendre la main à l'utilisateur
- Date : 2026-09-19 · Statut : Ferme
- Constat : le serveur de développement lancé comme tâche suivie gardait la session « occupée » pendant des dizaines de minutes ; l'utilisateur ne pouvait pas envoyer de nouvelle demande.
- Décision : le serveur est lancé détaché de la session ; aucune tâche suivie ne reste active à la fin d'un tour.
- Tickets : E13-T05

### D-022 — Dépôt GitHub en ligne
- Date : 2026-09-19 · Statut : Ferme (remplace la partie « en attente » de D-008)
- Décision : le projet est poussé sur https://github.com/Bakou9/healer-game (branche `main`) ; une intégration continue exécute `npm run check` à chaque envoi.
- Tickets : E13-T04, E13-T09

### D-023 — Direction artistique déléguée à l'agent
- Date : 2026-09-19 · Statut : Ferme (provisoire, à valider après le test de jeu)
- Décision : l'utilisateur laisse l'agent choisir des « graphismes sympas et adaptés ». Style retenu : fantasy stylisée en formes vectorielles dessinées en code (aucun fichier d'image), fond sombre avec lueurs, langage visuel par couleur (soin vert, bouclier bleu, poison violet, danger rouge, phase 2 orange), coins arrondis, Golem de pierre au cœur lumineux, icônes par rôle et par sort, particules pour soins, boucliers, poisons et impacts.
- Pourquoi : lisible, léger pour mobile, modifiable en code, sans dépendance à des ressources graphiques.
- Tickets : E12-T01, E12-T03

### D-024 — Ordre de travail : test de jeu d'abord
- Date : 2026-09-19 · Statut : Ferme
- Décision : on commence par le point 1 de la revue de mon travail : un vrai test de jeu de l'utilisateur, avec des graphismes soignés, avant d'élaguer les specs, d'ajouter des tests visuels et de refactorer.
- Tickets : E04-T14, E13-T05, E08-T15, E01-T08

### D-025 — Question d'architecture abandonnée
- Date : 2026-09-19 · Statut : Ferme (remplace D-014)
- Décision : la phrase coupée de l'utilisateur (« Le projet va être large, il faut… ») « n'a plus de sens » : on l'oublie, aucune contrainte supplémentaire. La proposition provisoire de `docs/ARCHITECTURE.md` (monolithe modulaire, frontières testées) reste la cible ; elle sera réévaluée si le moteur change (D-026).
- Tickets : E09-T01

### D-026 — Unity et son MCP : point de décision, pas de bascule maintenant
- Date : 2026-09-19 · Statut : Remplacée par D-027
- Contexte : l'utilisateur demande si utiliser Unity via son MCP (serveur officiel, plugin officiel pour Claude Code) a du sens.
- Position : rester sur TypeScript + Phaser + Capacitor pour la phase 1-3 ; décider d'un éventuel passage à Unity à une porte précise, après le test sur appareil Android d'entrée de gamme et les budgets de performance, selon des critères posés à l'avance. Si bascule, les références golden (indépendantes du langage) servent de spécification de conformité pour porter la simulation en C#.
- Tickets : E11-T07, E11-T01, E11-T02

### D-027 — Version Unity + MCP en parallèle, sans toucher à la version Phaser
- Date : 2026-09-19 · Statut : Ferme (remplace D-026 : l'utilisateur veut « tenter » Unity dès maintenant)
- Décision : « garder le projet actuel de côté et ne plus y toucher », et refaire le jeu avec Unity et un serveur MCP en reprenant toutes les règles, tous les `.md` et toute la mécanique de test. Ne jamais supprimer la version Phaser : un retour reste possible. Nouveau dépôt : `%USERPROFILE%\Documents\healer-game-unity`.
- Tickets : E14-T01, E14-T17, E11-T07

### D-028 — Choix du MCP : à trancher avec l'utilisateur
- Date : 2026-09-19 · Statut : À préciser
- Constat : le MCP officiel de Unity demande Unity 6+, un projet connecté à Unity Cloud et un essai ou un abonnement aux outils IA (bêta), donc un coût possible ; des MCP communautaires libres existent. Voir `docs/UNITY_SETUP.md`.
- Question ouverte : officiel (abonnement éventuel) ou communautaire libre ?
- Tickets : E14-T03

### D-029 — Architecture Unity : cœur C# pur partagé, Unity en présentation
- Date : 2026-09-19 · Statut : Proposition
- Décision : les règles vivent dans un cœur C# sans dépendance à Unity, compilé par dotnet (tests, CI, futur serveur) et par Unity (paquet local) ; contenu JSON et références golden partagés, repris de la version Phaser ; Unity ne contient aucune règle. Voir `docs/ARCHITECTURE_UNITY.md`.
- Réalisé : cœur porté, **7 combats de référence reproduits à l'identique**, 62 tests.
- Tickets : E14-T04, E14-T05, E14-T06, E14-T07, E14-T08, E14-T09

### D-030 — Modèles 3D « user friendly » : style figurine low-poly, construits par scripts
- Date : 2026-09-19 · Statut : Proposition (style délégué à l'agent, à valider après le premier test)
- Décision : formes simples et arrondies, silhouettes lisibles en portrait, couleurs par rôle, faible budget de triangles, matériaux unis ; modèles construits par scripts d'Éditeur reproductibles (préfabriqués), avec budget de triangles testé. Voir `docs/ART_3D.md`.
- Tickets : E14-T13

### D-031 — Installation des outils autorisée
- Date : 2026-09-19 · Statut : Ferme
- Décision : l'utilisateur a autorisé l'installation du SDK .NET 8, d'Unity Hub et de l'Éditeur Unity 6 LTS avec module Android (via winget et le Hub, sources officielles). La connexion au compte Unity et l'activation de la licence restent à faire par l'utilisateur ; l'agent ne saisit jamais d'identifiant.
- Tickets : E14-T02

### D-032 — Compte Unity créé et essai démarré par l'utilisateur
- Date : 2026-09-19 · Statut : À préciser
- Constat : l'utilisateur a créé son compte Unity et s'est inscrit à un essai (type d'essai non précisé). Un essai peut se transformer en abonnement payant à son terme.
- À préciser par l'utilisateur (sans jamais donner de mot de passe ni de coordonnées bancaires à l'agent) : quel essai (Unity Pro, outils IA, autre) ; date de fin ; renouvellement automatique ou non ; carte bancaire demandée ou non ; coût après l'essai.
- Règle : l'utilisateur note la date de fin dans son calendrier et annule avant l'échéance s'il ne veut pas payer ; l'agent rappelle cette date à chaque étape qui dépend de l'essai (MCP officiel).
- Tickets : E14-T02, E14-T03

### D-033 — Un seul dépôt GitHub : la version Unity vit dans unity-version/
- Date : 2026-09-19 · Statut : Ferme (précise D-027 : la version Phaser reste intacte, mais la version Unity n'a plus de dépôt séparé)
- Décision : sur demande de l'utilisateur, la version Unity est poussée dans un répertoire imbriqué du dépôt https://github.com/Bakou9/healer-game, dossier `unity-version/` (historique complet conservé, import par `git subtree add`). Les fichiers Phaser ne sont pas modifiés ; ajouts : `vitest.config.ts` (les tests Phaser ignorent le sous-dossier), un workflow `unity-version.yml`, et un pointeur en tête du `CLAUDE.md` racine. Le dossier local `%USERPROFILE%\Documents\healer-game-unity` devient une copie de secours : on ne travaille plus que dans `unity-version/`.
- Tickets : E14-T01, E14-T16

### D-034 — Newtonsoft.Json intégré au cœur, aucun paquet du registre Unity pour l'instant
- Date : 2026-09-19 · Statut : Ferme (contournement temporaire, réversible)
- Constat : Windows Defender fait échouer le renommage des paquets téléchargés du registre Unity (EPERM). Ce n'est pas un jugement sur le projet : l'analyse en temps réel tient les fichiers fraîchement écrits.
- Décision : intégrer la DLL Newtonsoft.Json 13.0.3 (identique à celle des tests dotnet, licence MIT) au paquet du cœur et ne dépendre d'aucun paquet du registre, ce qui permet à Unity d'ouvrir et de compiler le projet sans toucher aux réglages de sécurité de l'utilisateur. Une exclusion Defender du dossier `Library` (cache régénérable) sera demandée à l'utilisateur au moment d'ajouter URP, Input System ou Test Framework.
- Tickets : E14-T10

### D-035 — Historique du dépôt nettoyé (nom d'utilisateur Windows retiré)
- Date : 2026-09-19 · Statut : Ferme
- Constat : d'anciens commits (documentation) contenaient des chemins avec le nom d'utilisateur Windows de l'utilisateur, dans un dépôt public.
- Décision : l'historique de `main` a été réécrit (17 commits, `git filter-branch`) pour remplacer ces chemins par `%USERPROFILE%`, puis publié par un push forcé sécurisé (`--force-with-lease`) sur ordre explicite de l'utilisateur. Contenu actuel strictement identique ; tests verts ; intégrations continues vertes. Sauvegarde complète avant l'opération : `Documents/healer-game-sauvegarde-avant-nettoyage.bundle` (restaurable par `git clone`).
- Limites connues : GitHub garde les anciens commits accessibles par leur numéro exact pendant un temps (purge complète via le support GitHub) ; les numéros de commits ont changé (état Phaser repris : `91d7beb`) ; le nom et l'email des auteurs de commits restent visibles.
- Tickets : E13-T04
