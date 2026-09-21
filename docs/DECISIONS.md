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
- Limites connues : GitHub garde les anciens commits accessibles par leur numéro exact pendant un temps (purge complète via le support GitHub) ; les numéros des commits de la version Unity ont changé, ceux de la version Phaser (dont `91d7beb`) sont inchangés ; le nom et l'email des auteurs de commits restent visibles.
- Tickets : E13-T04

### D-036 — Première version jouable : rendu intégré, interface IMGUI, modèles générés par code
- Date : 2026-09-19 · Statut : Ferme (choix réversible, voir D-034)
- Décision : tant que les paquets du registre Unity sont bloqués par Defender, la version jouable s'appuie sur ce qui est livré dans le cœur d'Unity : rendu intégré (pas d'URP), interface dessinée par IMGUI avec les jetons de `Healer.Ui` (aucune règle dans le client), modèles 3D « figurine » générés par code à chaque lancement (`MeshKit`, `ModelFactory`) plutôt que des préfabriqués : même reproductibilité, aucun fichier binaire, budget de triangles contrôlé à chaque build.
- Spec remise en cause (préambule A) : E14-T13 prévoyait des préfabriqués construits par scripts d'Éditeur ; la génération à l'exécution offre la même reproductibilité sans dépendre de l'Éditeur, et simplifie le build. À reconsidérer si des artistes doivent retoucher les modèles.
- Interface : le journal de combat à 3 lignes de la version Phaser est réduit à **une ligne** (dernière action), car les modèles 3D occupent la zone du journal ; les événements se lisent aussi par les animations et les chiffres flottants.
- Tickets : E14-T11, E14-T12, E14-T13

### D-037 — Outils de vérification du jeu Unity
- Date : 2026-09-19 · Statut : Ferme
- Décision : `tools/unity-cycle.ps1` (construit l'exécutable, le lance en mode capture avec le bot de référence, écrit des captures d'écran) et `tools/unity-clicktest.ps1` (clique réellement dans la fenêtre du jeu et vérifie les gestes par le journal du joueur) sont les vérifications visuelles et d'interaction du jeu Unity, en attendant des tests EditMode (le Test Framework vient du registre, donc bloqué par Defender).
- Tickets : E14-T11, E14-T12, E13-T05

### D-038 — Statistiques de combat dans le cœur, sons générés par code
- Date : 2026-09-19 · Statut : Ferme
- Décision : les statistiques de combat (`CombatStats`) vivent dans le cœur et sont calculées uniquement à partir des événements (testées : égalité avec la somme des événements) ; le télégraphe expose sa durée totale (`TotalMs`) pour les jauges ; les sons sont générés par code (`SoundKit`) faute de fichiers audio, avec la touche M pour couper le son.
- Tickets : E01-T13, E04-T07, E04-T09, E04-T11

## D-039 — Écran de démarrage, pause au changement de fenêtre, méthode de build Android
- **Décision** : le combat ne démarre plus tout seul : un écran « Jouer » explique les gestes (cible puis sort, bouclier avant l'attaque, purge, mana). Le combat se met en pause si la fenêtre perd le focus (sauf quand le bot joue en mode capture). `Builder.BuildAndroid` (IL2CPP arm64, portrait) et lecture de StreamingAssets par UnityWebRequest sur Android sont prêts.
- **Pourquoi** : un joueur qui lance le jeu ne doit pas perdre un combat avant d'avoir compris ; changer de fenêtre ne doit pas être puni.
- **Reste à faire** : le build Android exige le module Android d'Unity (installation = à valider avec l'utilisateur, cf. QUESTIONS_EN_ATTENTE).

## D-040 — Input System adopté ; le projet Unity se travaille dans C:\WhatTheHeal
- **Décision** : le paquet `com.unity.inputsystem` 1.14.2 remplace l'ancien Input Manager (`activeInputHandler: 1`). Un essai dans `C:\Users\banja\Documents\healer-game` échouait toujours (EPERM au renommage du paquet dans `Library/PackageCache`) ; le même dépôt cloné dans `C:\WhatTheHeal` s'installe sans erreur. Le dossier `Documents` (accès contrôlé aux dossiers ou OneDrive) est donc la cause probable.
- **Conséquence** : travailler et builder depuis `C:\WhatTheHeal` (clone de https://github.com/Bakou9/healer-game). Ferme D-034 pour l'Input System ; le clic réel n'est pas retesté (`unity-clicktest.ps1` ignore l'écran « Jouer »).

## D-041 — Raccourcis clavier PC
- **Décision** : `BattleKeys` traduit les touches en gestes du contrôleur (mêmes `TapAlly` / `TapSkill` que le toucher, aucune règle ajoutée) ; pastilles affichées sur les cartes seulement si un clavier est présent. Détail dans `docs/TESTER_LE_JEU.md`.
- **Équilibrage** : aucune règle ni valeur modifiée ; les gestes sont plus rapides au clavier, mais le bot de référence (500 ms) reste la borne de jeu attentif. Vérifié en jeu réel (Espace, 1, A → soin lancé sur le Garde) ; pas de test automatisé du clavier.

## D-042 — Jeu en paysage, PC d'abord, mobile toujours compatible
- **Décision (utilisateur)** : le jeu est pensé PC d'abord, en paysage ; il doit rester utilisable sur mobile : pas d'entrées trop complexes. Remplace la grille portrait 480×854 (D-013 / version Phaser) : `Layout` passe à **1280×720** (16:9), colonne centrale de 1000 px (cartes d'alliés, cible et mana, sorts). Fenêtre PC 1280×720 redimensionnable ; orientation mobile paysage.
- **Garde-fous mobile conservés** : cibles ≥ 48 px, texte ≥ 14 px, tout jouable en un tap ; le clavier (D-041) n'est qu'un bonus, jamais requis. Textes du tutoriel : « cliquez (ou touchez) ».
- **Tests** : les 114 tests du cœur passent ; seul le nom d'un test de mise en page a changé (« zone du pouce » → « moitié basse de l'écran »), ses assertions sont identiques. Golden inchangés (aucune règle de combat touchée). Équilibrage : aucun impact.
- **À valider avec l'utilisateur** : `docs/UX.md` et la vision (« Android d'abord ») restent à mettre à jour ; un essai sur téléphone en paysage reste à faire (E11-T01).

## D-043 — Reprise automatique et style graphique plus mature
- **Reprise** : la pause déclenchée par la perte de focus de la fenêtre se lève seule au retour ; une pause volontaire du joueur n'est jamais levée automatiquement.
- **Graphismes** : palette plus sourde, proportions adultes (tête plus petite, buste long, bras fins, regard en fente), armure métallique, décor de ruines sombres peint par code (piliers, brume, sol dallé), éclairage clé chaud + contre-jour froid, vignette d'ambiance, interface plus sobre (panneaux sombres, filets fins, or terni). À juger visuellement par l'utilisateur (D-030 « À préciser » : validation du style 3D).

## D-044 — Bug « Jouer ne démarre pas » et politique de tests automatisés maximale
- **Bug** : en paysage, le bouton « Jouer » (et « Recommencer ») chevauchait des cartes d'alliés dessinées avant lui ; leur zone cliquable captait le clic, et `TapAlly` l'ignorait tant que le combat n'avait pas commencé. Espace fonctionnait car il passait par un autre chemin.
- **Correction à la racine** : `Healer.Ui.InputGate` (cœur, testé) décide quels gestes sont permis selon l'état (démarrage, combat, pause, bilan). Souris, toucher ET clavier passent par elle ; un élément hors de son état ne capte plus aucun clic. Le bouton « Jouer » / « Recommencer » vient de `Layout` (rectangles testés).
- **Tests ajoutés** : 43 tests du cœur (157 au total) : règle d'entrée par état, priorité des états, touche de confirmation, régressions géométriques (Jouer / Recommencer vs cartes, avec un garde-fou qui prévient si la géométrie ne reproduit plus le scénario), ajustement à l'écran (`ScreenFit`, extrait de `ScreenMap`), mise en page paysage. Vérifié par mutation : réintroduire le défaut fait échouer 2 tests.
- **Bout en bout** : `npm run e2e` (`tools/unity-e2e.ps1`, remplace `unity-clicktest.ps1`) pilote la vraie souris et le vrai clavier sur le jeu Windows : clic sur Jouer par-dessus une carte, cibler, sort, pause Espace / Échap, défaite accélérée, Recommencer, Entrée. À lancer sans toucher souris ni clavier ; non inclus dans `npm run check` (dépend de l'écran et de Unity).
- **Règle** : tout bug corrigé reçoit un test qui échouait avant ; toute nouvelle logique d'entrée ou de mise en page va dans le cœur (testable sans Unity), le client ne fait que la relier.

## D-045 — Le dépôt ne contient plus que le projet Unity (Phaser retiré)
- **Décision (utilisateur)** : « clean le projet pour qu'il n'y ait que le projet Unity et plus rien en lien avec Phaser ». Remplace **D-027** (ne jamais toucher à Phaser) et **D-033** (Unity dans un sous-dossier `unity-version/`).
- **Fait** : étiquette Git `phaser-archive` posée sur le dernier état avec Phaser (publiée sur GitHub) ; tous les fichiers Phaser retirés (`src/`, `docs/` et `scripts/` Phaser, Vite, Capacitor, configs, ancien `CLAUDE.md`, ancien workflow) ; le contenu de `unity-version/` est remonté à la racine ; un seul workflow CI `check.yml`. Historique intact : rien n'est perdu.
- **Retrouver Phaser** : `git checkout phaser-archive` (ou `git show phaser-archive:<chemin>`).
- **Conséquences** : dossier de travail local = `C:\WhatTheHeal` (l'ancien dossier `Documents\healer-game` est obsolète et peut être supprimé à la main). Les mentions historiques de Phaser dans les décisions, revues et tickets sont conservées telles quelles (mémoire du projet) ; les golden restent la spécification de conformité (`core/golden`).

## D-046 — Interface à deux pouces : alliés à gauche, sorts à droite
- **Décision (utilisateur)** : boutons sur les côtés pour le jeu mobile en paysage, une seule fonction par pouce : à gauche les barres de vie des personnages, à droite les boutons de soin. Précise D-042.
- **Mise en page** (`Healer.Ui.Layout`, 1280×720) : colonne gauche de 264 px = cartes d'alliés empilées (cibler, lire les PV) ; colonne droite symétrique = mana puis sorts empilés ; scène 3D au centre (boss, alliés en ligne devant lui, cible affichée en bas). Barre de PV du boss et pause en haut. Les alliés 3D ne sont plus alignés sur leurs cartes (`Layout.AllyStageX`) ; un anneau doré marque l'allié ciblé.
- **Mobile** : chaque colonne est atteignable du pouce depuis son bord (testé : colonnes dans le quart de l'écran de chaque côté, aucune carte à droite, aucun sort à gauche, ≥ 72 px de haut par bouton). Le geste reste « toucher un allié, toucher un sort » ; le clavier (D-041) est inchangé.
- **Tests** : 163 tests du cœur verts, golden inchangés (aucune règle de combat touchée, aucun impact d'équilibrage). Tests de mise en page réécrits à dessein car ils décrivaient la disposition verticale : ordre vertical des zones → zones sans chevauchement et ordre gauche / centre / droite ; « sorts dans la moitié basse » → « sorts dans la colonne de droite ». Les tests d'entrée (InputGate) sont conservés ; le garde-fou « Jouer chevauche une carte » devient « plus aucun chevauchement ». Bout en bout (`npm run e2e`) adapté aux nouvelles zones et vert.
- **À valider avec l'utilisateur** : essai sur téléphone réel en paysage (taille des boutons au pouce), et si les cartes doivent rester aussi grandes (elles peuvent afficher plus d'informations : effets, buffs).

## D-047 — On commence en premium ; l'architecture garde la porte ouverte au gacha
- **Décision (utilisateur, après discussion)** : jeu **acheté une fois** d'abord ; passage possible à un modèle gacha plus tard. Précise la vision (« gacha »).
- **Trois garde-fous d'architecture, dès maintenant** (peu coûteux, évitent de tout refaire) :
  1. **Inventaire séparé des définitions** : `PlayerProfile.OwnedCharacters` dit ce que le joueur possède ; `CreateEncounter(boss, graine, possédés)` n'aligne que ces personnages (le soigneur est toujours présent). En premium il les possède tous ; un gacha n'aura qu'à remplir cet inventaire autrement. Posséder tout le monde ne change pas un seul événement de combat (testé).
  2. **Un seul point d'entrée pour l'argent** : `Wallet.Grant` / `TrySpend`, avec une raison obligatoire et un registre. Aucune récompense ni dépense n'y échappe (testé).
  3. **Équilibrage indépendant de la collection** : chaque boss doit rester gagnable avec les mêmes personnages ; le nombre de personnages exigés par niveau reste faible.
- **Ce qui demandera un vrai chantier au passage gacha** : serveur (comptes, sauvegarde en ligne, tirages côté serveur), achats intégrés, règles légales sur les tirages, économie (monnaies, taux, pitié), contenu régulier.

## D-048 — Jalon 1 : campagne de 3 niveaux, étoiles, récompenses, sauvegarde, menus
- **Contenu** (JSON, aucune règle en dur) : 3 boss et 3 niveaux (`core/content/boss2.json`, `boss3.json`, `levels.json`, effets `venom` et `burn`). Niveau n+1 verrouillé tant que le niveau n n'est pas terminé.
  - Golem Ancestral (référence, inchangé) ; **Reine des Marais** : coups faibles, venin très dangereux (la Purge est indispensable) ; **Seigneur de Cendre** : attaques de zone rapides (télégraphe 1,2 s), brûlures, trois phases.
- **Étoiles** : 1 = victoire ; 2 = sans allié K.O. ; 3 = en plus sous un seuil de dégâts encaissés par niveau (boucliers et purges le font baisser). Or : première victoire + bonus par étoile nouvelle, puis répétition.
- **Sauvegarde** : profil JSON versionné, tolérant (fichier illisible mis de côté, jamais écrasé en silence ; version future refusée ; niveau sauté par un fichier bricolé refusé), écriture atomique.
- **Navigation** : menu principal → choix du niveau → combat → bilan (étoiles, or, déblocages, Recommencer / Niveau suivant / Carte). Toutes les entrées (souris, toucher, clavier) passent par `InputGate`, étendu aux écrans de l'application.
- **Équilibrage** : les 3 boss passent les MÊMES bornes que le premier (§9 d'EQUILIBRAGE.md), réglées par balayage automatique (`ZBalayage`, outil explicite). Aucune borne relâchée. Golden d'origine inchangés ; deux nouveaux golden (boss 2 et 3) créés par ce projet.
- **Tests** : 317 tests du cœur (163 avant) + 5 scénarios de bout en bout (35 vérifications, dont sauvegarde sur disque et relance du jeu).
- **À valider avec l'utilisateur** : voir QUESTIONS_EN_ATTENTE.md (usage de l'or, durée des combats, boss avec limite de temps…).

## D-049 — Jalon 2 : atelier (équipement et talents achetés avec l'or), équilibré choix par choix
- **Décision (utilisateur)** : l'or achète des améliorations d'équipement et de talents, avec les mêmes tests d'équilibrage à chaque choix (proposition acceptée). Remplit l'inventaire prévu par D-047 ; toute dépense passe par `Wallet.TrySpend`.
- **Équipement** : 8 pistes (arme et armure de chacun des 4 personnages), 5 niveaux (50 / 80 / 110 / 150 / 200 or), +4 % par niveau (attaque ; PV et défense ; soins pour le bâton ; PV et mana pour la robe du soigneur).
- **Talents du soigneur** : 3 paliers (ouverts à 2, 5 et 8 étoiles, 150 / 300 / 500 or), deux options exclusives par palier ; le premier choix est payant, **changer d'option est gratuit** (on ne punit pas l'essai).
  1. Soins vifs (Soin +30 % et −15 % de mana) ou Économe (−9 % de mana sur tous les sorts) ;
  2. Rempart (Bouclier +70 %) ou Purge vive (Purge −30 % de mana, −10 % de recharge) ;
  3. Flux de mana (+5 % de régénération) ou Onde de vie (Soin de zone +60 % et −45 % de mana).
- **Règle de calcul** : les pourcentages de toutes les sources **s'additionnent** puis s'appliquent une fois à la valeur de base (pas d'effet boule de neige), arrondis au plus proche. Sans amélioration, le combat est strictement identique (testé : mêmes événements) : les golden n'ont pas bougé. Aucun effet n'est codé en dur : tout est dans `core/content/upgrades.json`.
- **Économie** : rejouer paie désormais 50 / 80 / 120 or (doublé : avec l'ancien tarif, tout acheter aurait demandé ~60 parcours). Tout acheter = ~20 parcours complets ; la première victoire paie déjà un premier niveau d'équipement ; deux niveaux à 2 étoiles ouvrent et paient le premier talent (tests).
- **Équilibrage** : voir docs/EQUILIBRAGE.md §10. Aucune borne du jeu de base relâchée ; les valeurs de talents ont été réglées par balayage automatique (`ZBalayageTalents`) après avoir constaté que des choix dominaient (Économe −15 % valait +0,24 de PV minimum contre +0,03 pour Soins vifs +15 %).
- **Écran Atelier** (menu principal) : achat, niveaux en pastilles, talents, messages de retour ; jouable à la souris et au toucher ; 6 scénarios de bout en bout (dont achat, refus, palier verrouillé, sauvegarde, relance, effet en combat).
- **À valider avec l'utilisateur** : voir QUESTIONS_EN_ATTENTE.md (§ Jalon 2).

## D-050 — Retours de jeu : mécaniques documentées, dégâts par membre, maintien des sorts, sélection stable, Seigneur de Cendre trop facile
Cinq demandes de l'utilisateur après avoir joué (statuts : faits ; le point 5 est **partiel**, voir plus bas).
1. **Fichier de référence des mécaniques** : `docs/MECANIQUES.md` (statistiques, formules, sorts, effets, boss, progression, améliorations, **et ce qui n'existe pas : critiques, résistances, armure en %, esquive…**). À tenir à jour à chaque changement de règle ou de valeur.
2. **Bilan de combat** : dégâts infligés au boss par chaque membre (barre, valeur tronquée, part en %). `CombatStats.DamageByAlly`, testé (le total égale les dégâts au boss, le soigneur n'inflige rien).
3. **Maintenir un sort l'enchaîne** (souris, doigt ou touche) : `HoldRepeat` (règle pure, testée) + client. Le maintien ne change pas les décisions du joueur (cible, sort) : maintenir un seul sort ne gagne aucun boss (testé, 30 combats × 3 boss). **Défaut trouvé par le test de bout en bout** : le client réémettait la commande à chaque image avant son traitement (16 tentatives en 5 s) ; corrigé (une seule commande par avancée de la simulation). Scénario e2e G.
4. **Re-toucher un allié sélectionné ne le désélectionne plus** (`TargetSelection.Tap`). Un test de l'ancienne version Phaser décrivait le contraire : modifié à dessein (changement voulu par l'utilisateur), plus trois tests ajoutés.
5. **Seigneur de Cendre trop facile, sans intensité** — **diagnostic** (100 combats, sans amélioration) : un joueur qui ne fait que « Soin + Soin de zone » (jamais de bouclier ni de purge) gagnait 100 % des combats en perdant un allié dans 7 % des cas : le boss n'exigeait rien. Mes bornes d'équilibrage ne le voyaient pas : elles ne mesuraient que le bot complet. **Ajouts** : (a) mécanique générique d'**enrage** (`enrage : { afterMs, everyMs, pct }` dans les données ; ses dégâts directs montent par paliers ; événement `bossEnraged` ; badge « ENRAGÉ +N % ») ; (b) réglage du boss 3 : brûlure 34 → 36, télégraphe de l'attaque de zone 1,2 s → 1,0 s, enrage à 65 s (+5 % toutes les 10 s) ; (c) **nouveaux tests permanents** : sur tout boss avancé, le soin de zone seul gagne ≤ 75 % des combats, soigner sans bouclier ni purge fait perdre un allié dans ≥ 25 % des combats, et le joueur attentif meurt nettement moins que le paresseux. Le Golem (tutoriel) est exempté. Les golden du boss 1 et 2 sont inchangés ; celui du boss 3 (créé par ce projet, pas issu de Phaser) a été régénéré, et contient 4 paliers d'enrage.
   **Résultats du boss 3** (100 combats) : joueur attentif 0,35 → 0,26 de PV minimum (10 % d'un allié K.O., à la borne) ; soin de zone seul : 84 % → 53 % de victoires ; soin sans bouclier ni purge : 7 % → 39 % d'un allié K.O. ; joueur lent : 0,05 de PV minimum.
   **Limite honnête** : le boss reste gagnable avec du soin seul (98 % de victoires, mais en perdant un allié dans 39 % des cas), et la sensation d'intensité dépend du joueur humain, pas du bot. La « zone de létalité » testée (allié K.O. ≤ 10 % pour le joueur attentif) empêche de durcir davantage sans nouvelles mécaniques : voir QUESTIONS_EN_ATTENTE.md.
- **Équilibrage** : oui, les réglages du boss 3 changent l'équilibre → mesuré avant/après (ci-dessus) et documenté dans docs/EQUILIBRAGE.md §11. Toutes les bornes existantes tiennent (516 tests).

## D-051 — Jalon 3 (habillage) : sons pilotés par le cœur, musique adaptative, animations, effets, réglages
- **Décision (utilisateur : « attaque le prochain jalon »)** : jalon 3 du plan, l'habillage. Périmètre retenu, ce qui est **testable sans Unity** d'abord (logique dans le cœur), le client ne fait que la jouer :
  1. **Sons** (`AudioCues`, `CueLimiter`, `Mix`) : table événement → son testée (un soin à vide reste muet, un coup entièrement absorbé aussi…), limiteur (un soin de zone sur 4 alliés = un son), volume par pas de 10. Nouveaux sons : lancer de sort, mort, enrage, clic, achat, refus.
  2. **Musique adaptative** (`MusicDirector`, `MusicKit`, `BattleMusic`) : boucle de 8 mesures générée par code, 4 couches synchrones (nappe, pulsation, mélodie, alarme). L'**intensité** (PV du plus blessé, attaque annoncée, phase du boss, enrage) est calculée dans le cœur ; les couches arrivent dans l'ordre avec le danger, la montée est rapide et le retour au calme lent.
  3. **Animations** (`UnitAnimator`) : l'attitude de chaque unité (élan d'attaque, recul, geste de lancer, halo de soin, chute à la mort) est une fonction pure des événements du combat, testée (bornes 0-1, déterminisme sur un combat complet). Le client n'invente plus d'attitude.
  4. **Effets visuels** : anneaux au sol par sort (soin vert, bouclier bleu, purge blanche/violette), faisceau soigneur → cible, **anneau de danger rouge** sous l'équipe pendant qu'une attaque de zone est annoncée, aura d'enrage.
  5. **Réglages** (nouvel écran) : volume de la musique, volume des effets, secousse d'écran (utile aux joueurs sensibles au mouvement) ; sauvegardés avec le profil, rechargés au démarrage ; « Son coupé » (touche M) ne perd pas les volumes choisis.
  6. **Reine des Marais** : nouveaux tentacules (plus de bâtons raides).
- **Non fait, à dessein** : reconstruction de l'interface en UI Toolkit, vrais modèles importés, animation squelettique. Ces chantiers demandent des choix artistiques (voir QUESTIONS_EN_ATTENTE.md).
- **Équilibrage** : aucun impact (aucune règle de combat modifiée ; sons, musique et animations ne lisent que des événements). Golden inchangés.
- **Tests** : 610 tests du cœur (94 de plus, dont 11 mises en page ; un test a trouvé une barre de réglage qui touchait le bouton « + ») ; 9 scénarios de bout en bout (nouveau : réglages sauvegardés et rechargés).
- **Non vérifiable par les tests** : le rendu sonore et visuel eux-mêmes (musique agréable ? effets lisibles ?) : à juger à l'oreille et à l'œil.

## D-052 — Nouvelles mécaniques de combat : critiques, résistances, armure en %, esquive, menace, temps d'incantation du Soin
- **Demande de l'utilisateur** : mettre en place critiques, résistances, armure en pourcentage, esquive, menace ; et un temps d'incantation pour le Soin de base plutôt qu'une recharge. Remplace la liste « n'existe pas » de docs/MECANIQUES.md §1.
- **Changement de règles voulu : les combats de référence (golden) changent tous.** Annoncé avant de coder ; régénérés après vérification. La version Phaser est retirée depuis D-045 : la conformité à Phaser n'est plus un objectif. Trois tests « retrouve les mesures de la version Phaser » sont devenus « mesures de référence du jeu actuel » (valeurs mises à jour).
- **Méthode** : (1) moteur d'abord, **sans effet tant que les données ne l'activent pas** (une chance de 0 ne consomme aucun tirage) : les 610 tests d'origine, golden compris, sont restés verts, ce qui prouve l'absence de régression ; (2) 75 tests par mécanique sur des arènes isolées ; (3) adoption dans le contenu ; (4) rééquilibrage ; (5) golden régénérés.
- **Choix de conception** (à confirmer) : armure en % **s'ajoute** à la défense fixe et ne protège que du physique ; l'esquive évite aussi l'effet ; un boss en « menace » cible en proportion de (1 + menace) ; les soins génèrent 20 % de leur valeur en menace ; l'incantation dépense le mana à l'**achèvement** et n'est **pas interrompue par les coups** ; les sorts instantanés restent possibles pendant une incantation.
- **Équilibrage** (mesuré avant/après, 100 combats, joueur attentif ; détail docs/EQUILIBRAGE.md §12) : sans retouche des boss, chaque mécanique rend le jeu plus facile et plus court (Golem 0,44 de PV minimum, 61 s ; Reine 0,50, 59 s : sous la borne des 60 s). Réglage par balayage automatique (`ZReglage`) : Golem 7 000 → 7 700 PV et attaque 55 → 58 ; Reine 6 500 → 8 450 PV et attaque 34 → 37 ; Seigneur 8 200 → 9 020 PV. Toutes les bornes tiennent (Reine et Seigneur proches de la limite de 10 % d'allié K.O.) ; seuil de 3ᵉ étoile du niveau 1 : 3 700 → 3 100.
- **Contenu activé** : Garde armure 25 %, menace × 5, résistance au feu 15 % ; Archère esquive 15 %, critique 20 % (× 2) ; Mage critique 12 % (× 2), dégâts magiques, résistance magie 25 % ; soigneur critique 10 %, esquive 8 %, résistance poison 15 % ; boss : ciblage par menace, résistances (le Golem est faible à la magie), le Seigneur inflige du feu et fait des critiques.
- **Interface** : barre d'incantation, marqueur « Menace », textes « CRITIQUE » et « Esquive », couleurs par type de dégâts, sons de critique et d'esquive, geste du soigneur pendant toute l'incantation, critiques et esquives dans le bilan.
- **À valider avec l'utilisateur** : voir QUESTIONS_EN_ATTENTE.md (§ Nouvelles mécaniques). Les talents et l'équipement n'ont **pas** été rééquilibrés autour des nouvelles statistiques (critique, esquive, armure) : ils passent toujours leurs bornes actuelles, mais aucun n'utilise ces statistiques encore.

## D-053 — Tests de bout en bout : entrées injectées, parallèles, silencieux, sans voler le focus

- **Date** : 2026-09-20 — **Statut** : Validée (demande de l'utilisateur : garder souris et clavier libres, accélérer).
- **Problème** : `tools/unity-e2e.ps1` pilotait la vraie souris et le vrai clavier de Windows : la machine était inutilisable pendant le test (~7 min) et les scénarios ne pouvaient qu'être séquentiels.
- **Décision** : le jeu accepte `-healer-e2e FICHIER` (`E2eInput`) : commandes `click X Y`, `down`, `up`, `key`, `keydown`, `keyup` jouées sur une souris et un clavier **virtuels** de l'Input System (vrais périphériques désactivés dans ce mode). Le pointeur du HUD passe désormais par l'Input System (`PointerInput`, zones de `Healer.Ui.Layout`) au lieu des événements IMGUI : souris, toucher et injection suivent le même chemin. Le script attend l'accusé `e2e N ok` du journal au lieu de pauses fixes, et les fins de combat par le journal.
- **Parallélisme** : un processus par scénario (`-Jobs 4`), journal `-logFile` propre à chacun. **Silence** : `AudioListener.volume = 0` en mode injecté, sauf `-healer-sound`. **Focus** : le jeu est lancé réduit (`SW_MINIMIZE` via `CreateProcess`) ; mesuré : ~5 échantillons sur 420 (quelques dixièmes de seconde au lancement de chaque fenêtre) contre un focus pris en continu avant.
- **Écarté** : `-batchmode` (l'IMGUI ne se dessine pas sans fenêtre : à réessayer avec UI Toolkit) ; lancer sans activation (`SW_SHOWNOACTIVATE`) : Unity prend quand même le focus.
- **Gardé** : `-RealInput` pilote la vraie souris (un scénario à la fois, personne ne touche à la machine) : vérification avant livraison de la livraison Windows → Unity.
- **Mesure** : suite complète de 9 scénarios : ~60 s en parallèle contre ~7 min avant, mêmes 9 verts.

## D-054 — Jalon 4 : interface en UI Toolkit, sorts en icônes, fiche des sorts, notes de playtest

- **Date** : 2026-09-20 — **Statut** : Validée pour l'architecture ; à confirmer au playtest pour les icônes (voir ci-dessous).
- **Demande de l'utilisateur** : passer d'IMGUI à UI Toolkit/uGUI ; icônes pour les sorts afin d'alléger l'interface ; à la pause, description détaillée des sorts avec et sans bonus (équipement, talents) ; noter les retours de playtest en jeu (F8) ET dans le chat.
- **Interface** : `UiRoot` (PanelSettings créé par le code, grille logique 1280×720 mise à l'échelle comme `ScreenFit`), `AppScreens` (menu, niveaux, atelier, réglages, reconstruits quand leur état change), `BattleScreen` (HUD construit une fois par combat puis lié à l'état ; départ, pause, fin reconstruits à la demande), kit `Ui`. Toute la mise en page reste celle de `Healer.Ui.Layout` (testée) : les coordonnées des tests e2e restent valides. Les anciens `AppHud`, `BattleHud`, `UiKit` et `PointerInput` sont supprimés.
- **Paquet ajouté : `com.unity.ugui` 2.0.0** (livré avec l'Éditeur, aucun téléchargement) : sans `EventSystem` + `InputSystemUIInputModule`, UI Toolkit ne reçoit aucun clic (constaté : clavier OK, souris muette). Le module est branché sur l'Input System, donc souris, toucher et entrées injectées passent par le même chemin.
- **Sorts en icônes** : grille de 2 colonnes de carrés de 128 px dans la colonne de droite (`Layout.SkillButtonRects`), icône dessinée par le code (`IconKit`, couleur d'accent par sort), pastille de raccourci, coût en mana, recharge en voile qui descend avec le temps. **Ce qui change** : le nom du sort n'est plus affiché en combat (il l'est à la pause). **Test amendé, volontairement** : `Les_boutons_de_sorts_sont_assez_grands_pour_le_pouce` exigeait 200×72 px ; il exige maintenant 96×96 px (les boutons font 128×128, plus grands pour le pouce).
- **Fiche des sorts (pause)** : `SkillDescriber` (cœur, testé) compare le sort de base (`GameContent.Skills`) au sort du combat (bonus d'équipement et de talents) : effet, coût, incantation, recharge, cible ; les valeurs modifiées apparaissent en vert. **Bug corrigé au passage** : `BattleController.Skills` renvoyait les sorts de base, donc le coût de mana affiché en combat ignorait les talents (le combat lui-même était correct) ; il renvoie désormais les sorts du combat.
- **Notes de playtest (F8)** : `PlaytestNotes` : capture d'écran, pause automatique, saisie (par `Keyboard.onTextInput`, accents compris), entrée datée ajoutée à `playtest/notes.md` avec l'état du combat (boss, équipe, mana, cible, statistiques). Les raccourcis du jeu sont suspendus pendant la saisie.
- **Layout** : `Healer.Ui` référence maintenant `Healer.Combat` (pour `SkillDescriber`) ; boutons de pause descendus (Reprendre y=380, Quitter y=452) pour laisser la place à la fiche.
- **Équilibrage (question B)** : aucune règle ni valeur de jeu modifiée ; golden inchangés (701 tests verts). Effet sur le ressenti : boutons de sorts plus grands, mais plus de nom en combat : à confirmer au playtest (lisibilité des 4 sorts par icône seule).
- **Vérification** : 10 scénarios e2e (dont la note F8 et la fiche des sorts) verts en ~63 s ; captures de contrôle du menu, du combat et de la pause.
- **Limites connues** : les icônes sont simples (formes dessinées par le code) ; la direction artistique « dark fantasy » (D-030) reste à faire (jalon suivant) ; la vérification à la vraie souris (`-RealInput`) n'a pas été rejouée après la refonte.

## D-055 — Fiche des sorts en description, raccourcis inversés (alliés sur les lettres, sorts sur les chiffres)

- **Date** : 2026-09-20 — **Statut** : Validée (demande de l'utilisateur après le premier essai de D-054 ; icônes approuvées).
- **Fiche des sorts (pause)** : remplace le tableau à deux colonnes. Une carte par sort : nom et coût (« 45(41) mana »), « Incantation : instantanée » et « CD : 5s », cible, description avec les valeurs insérées (« de 160(240) PV »). La valeur de base est écrite normalement, la valeur modifiée par l'équipement ou un talent **entre parenthèses, en vert**. Les descriptions de `skills.json` contiennent des marqueurs `{heal}` `{shield}` `{cast}` `{mana}` `{cd}` remplacés par `SkillDescriber.Sheet` (cœur, testé : 8 tests dont « aucun marqueur oublié dans le contenu »).
- **Raccourcis** : alliés sur la rangée de lettres physique (**A Z E R** en AZERTY, Q W E R en QWERTY, T pour un 5e allié : le libellé affiché suit la disposition du clavier détectée par l'Input System) ; sorts sur **1 à 6, avec le pavé numérique en doublon** (maintenir enchaîne le sort, y compris au pavé). Le départ de combat l'indique (« 1 2 3 4 (ou pavé numérique) »). E2E : scénario G adapté (touche A pour cibler, 1 pour maintenir, 1 du pavé pour lancer).
- **Question B (équilibre)** : aucun changement de règle ni de valeur ; raccourcis et texte seulement.

## D-056 — Soin de zone : plus de spam gagnant ; suite : mécaniques de boss avant la direction artistique

- **Date** : 2026-09-20 — **Statut** : Réglage validé par l'utilisateur (option J) ; mécaniques à venir.
- **Retour de playtest** : « le jeu est trop simple, il suffit de spammer Soin de zone (avec quelques purges) ». **Mesuré** : 94 / 80 / 89 % de victoires (Golem / Reine / Seigneur) sans aucune autre stratégie. Cause : le Soin de zone soigne 4 alliés pour 45 mana (≈ 14 PV par mana contre ≈ 9,4 pour le Soin ciblé) et n'exige aucun ciblage ; aucune borne ne couvrait « zone + purge ».
- **Décision** : Soin de zone **140 PV / 60 mana / 5 s** (avant : 160 / 45 / 5 s), choisi parmi 4 options mesurées (voir EQUILIBRAGE.md §13) ; nouvelles bornes de test ; golden des combats du Golem régénérés (4 fichiers : bot-seed7, bot-seed42, bot-lent-seed7, bot-sans-purge-seed7 ; les autres inchangés).
- **Effets secondaires acceptés** : le tutoriel est un peu plus tendu (attentif : PV minimum 0,32 → 0,28) ; un joueur lent y perd un allié dans 25 % des combats. Toutes les bornes du §2 tiennent.
- **Suite décidée par l'utilisateur** : durcir aussi par de nouvelles mécaniques de boss, **avant** la direction artistique dark fantasy (jalon suivant). Piste : attaque qui vise un allié précis (télégraphiée, cible visible) pour que le Soin de zone ne suffise plus.

## D-057 — Attaque ciblée du boss (première version)

- **Date** : 2026-09-20 — **Statut** : Livrée, à valider au playtest (levier faible mesuré, voir EQUILIBRAGE.md §14).
- **Demande** : durcir le jeu par de nouvelles mécaniques de boss avant la direction artistique (réponse de l'utilisateur à D-056).
- **Mécanique** : action de boss `focusAttack` (données : `telegraphMs`, `multiplier`), victime tirée au hasard parmi les alliés fragiles au début du télégraphe, annoncée (`Telegraph.TargetId`, événement `focusMarked`, trace de golden), coup sur elle seule. Bot de référence : Bouclier sur la victime annoncée (`ProtectFocusTarget`, désactivable pour mesurer). Interface : texte « ATTAQUE CIBLÉE sur X dans Ys », « CIBLÉ ! » et bordure rouge sur la carte, figurine rouge pulsante, son d'avertissement ; journal `attaque ciblée annoncée sur X` (e2e, scénario H).
- **Données** : Reine ×5 (ATK 37 → 24), Seigneur ×7 (ATK 34 → 19), Golem inchangé (tutoriel). Seuils d'étoiles l2 1500, l3 2000 ; talent Rempart +55 % (au lieu de +70 %).
- **Golden** : `boss2-bot-seed7` et `boss3-bot-seed7` régénérés (voulu) ; les autres inchangés. 715 tests verts, 10 scénarios e2e verts.
- **Question B** : l'équilibre a été mesuré avant/après (EQUILIBRAGE.md §14) ; toutes les bornes tiennent, y compris zone en boucle + purge ≤ 60 % (≤ 40 % dernier boss). **À valider avec l'utilisateur** : le levier de la mécanique est faible ; faut-il durcir la réponse (deuxième parade) ou accepter cette première version ?

## D-058 — Overheal à l'écran de fin ; direction artistique « dark fantasy » (modèles, animations, ambiance)

- **Date** : 2026-09-20 — **Statut** : Livrée, à valider au playtest (goût visuel).
- **Overheal** : l'événement `healed` porte désormais `Overheal` (part du soin au-delà des PV manquants ; **non écrite dans la trace** : les golden ne bougent pas) ; `CombatStats.Overheal` et `OverhealRatio` ; ligne « Soins gaspillés (excédent) 932 (29 %) » sur l'écran de fin. Tests : soin sur allié en pleine forme = 100 % d'overheal ; soin de zone après un coup = 180 effectifs et 100 d'excédent ; la trace n'affiche pas l'overheal ; état vide.
- **Style choisi par l'utilisateur** : « dark fantasy » (polygones plus profonds et anguleux), après « anime » écarté. Ce que cela change : formes à arêtes vives (`MeshKit` : prismes, cristaux, rochers taillés, lames, toits), armures noircies, capuches, cornes, cristaux et failles lumineuses ; palette sombre (acier terni, cuir noir, sang, os, or vieilli).
- **Modèles** (tous reconstruits, ≤ 750 triangles chacun, budgets 3 000 / 8 000 vérifiés par `Healer/Vérifier les modèles 3D`) : Garde (plaques, casque à cornes, bouclier runique, épée, cape), Archère (capuche, cape en lambeaux, arc, carquois), Mage (robe, chapeau tordu, bâton à cristal et éclats), Soigneuse (robe ivoire, halo, bâton à croix de cristal) ; Golem (roche taillée, mousse, cœur de cristal, failles), Reine des Marais (couronne de ronces, bois, voile, racines), Seigneur de Cendre (obsidienne, grandes cornes, épaulières hérissées, cape de cendre).
- **Animations** : `UnitRig` (tête, bras, cape, buste, arme) animé par l'attitude calculée par le cœur (`UnitAnimator` : élan, recul, lancer, chute) : bras levés à l'incantation, bras droit en avant à l'attaque, tête rejetée au coup, respiration, cape et arme qui ondulent. Aucun calcul de jeu côté client.
- **Ambiance** : lumière nocturne (lune froide, contre-jour violet, deux braseros vacillants), décor plus sombre avec fenêtre gothique, cercle runique au sol teinté par le boss, brume basse, poussières lumineuses ; allié ciblé par une attaque : rouge pulsant.
- **Limites connues** : modèles procéduraux, sans textures ni animation squelettique fine (pas de marche, pas de mouvement de doigts) ; ombres portées absentes (rendu intégré) ; la Reine reste la moins lisible (jupe de racines peu visible de face). Pistes : ombres douces, éclairage volumétrique, modèles importés si le budget le permet.
- **Question B** : aucune règle ni valeur modifiée (visuel et statistique d'affichage uniquement) ; golden inchangés.

## D-059 — Statistiques des personnages dans le menu de pause ; dégâts avant boucliers à l'écran de fin

- **Date** : 2026-09-20 — **Statut** : Livrée.
- **Pause** : sous les fiches de sorts, une carte par personnage : PV du moment et maximum, bouclier actuel, attaque (et type de dégâts), défense, armure, esquive, critique (chance et dégâts), menace, mana et régénération pour le soigneur, résistances. Comme pour les sorts, la valeur de base s'écrit normalement et la valeur modifiée par l'équipement ou les talents **entre parenthèses, en vert**. `CharacterDescriber` (cœur, testé : 5 tests + format décimal tronqué `Format.Decimal`). Le soigneur n'affiche pas d'attaque (il n'attaque pas). `BattleController.Characters` expose les personnages du combat en cours.
- **Mise en page** : boutons de pause descendus (Reprendre y=530, Quitter y=600), scénarios e2e adaptés.
- **Écran de fin** : « Dégâts encaissés 2694 (3763) » : PV perdus, puis le total avant boucliers (`CombatStats.DamageBeforeShields`, testé). Les seuils d'étoiles utilisent toujours les PV perdus.
- **Question B** : aucune règle ni valeur modifiée ; golden inchangés ; 727 tests et 10 scénarios e2e verts.

## D-060 — Générique des auteurs et règles pour les ressources externes

- **Date** : 2026-09-20 — **Statut** : Livrée (le mécanisme) ; aucune ressource externe importée pour l'instant.
- **Demande** : un générique qui remercie tous les auteurs de toutes les ressources ajoutées à l'avenir.
- **Mécanisme** : `core/content/credits.json` (ressources et outils : nom, auteur, licence SPDX, URL, usage, dossier, modifié) ; écran « Crédits » (bouton en bas à gauche du menu, défilement automatique en boucle, Échap pour revenir) ; `CreditsRoll` (lignes du générique) et `CreditPolicy` (licences autorisées, champs obligatoires) dans le cœur, testés (9 tests) ; **garde-fou** : un dossier sous `Assets/Resources/Imported` sans entrée fait échouer les tests. Outils déjà remerciés : Unity, Json.NET. Règle ajoutée à `CLAUDE.md`. Scénario e2e K.
- **Ressources** : voir `docs/ASSETS.md` (règles, candidats vérifiés, comparaison des voies de création sur mesure et de leurs coûts).
- **À préciser avec l'utilisateur** : le nom à afficher pour la conception du jeu (« Créé par … ») : je n'invente aucun nom.
- **Question B** : aucune règle ni valeur de jeu modifiée.

## D-061 — Apparence des héros selon l'équipement : système modulaire

- **Date** : 2026-09-20 — **Statut** : Livrée avec les modèles procéduraux actuels ; le contrat (pièce, palier, palette) permet d'y brancher des pièces importées.
- **Demande** : beaucoup de héros à venir, des skins qui évoluent avec les objets équipés, une direction artistique cohérente ; décision de bâtir un système modulaire indépendant de la source des modèles avant de trancher le kit final (artiste, Blender + MCP, packs CC0).
- **Cœur** : `AppearanceCatalog` (`core/content/appearance.json`) : paliers (Ordinaire, Raffiné à partir du niveau 2, Légendaire à partir de 4) et, par héros et par emplacement (arme, armure), une pièce et une palette par palier ; `Resolve(héros, équipement)` renvoie `AppearanceSet` (pièces, paliers, signature). 9 tests : paliers, complétude pour chaque héros, correspondance avec les pistes de l'Atelier, palettes valides, indépendance entre héros, catalogue vide.
- **Client** : les 4 héros sont reconstruits en corps commun + pièces (`ModelFactory.Look`) : Garde (cornes, crête, épaulières à pointes, cape, lame runique, bouclier à pointes), Archère (foulard, cape en lambeaux, mantelet, arc plus grand et lumineux), Mage (liserés dorés, runes de robe, cristal de chapeau, cristal de bâton et éclats), Soigneuse (ceinture, halo, ailes de lumière, croix plus grande et anneaux). La scène redessine les héros quand l'équipement a changé entre deux combats ; l'Atelier affiche « Aspect : Raffiné ». Journal `apparence : …` et scénario e2e F.
- **Charte artistique** : ajoutée à `docs/ART_3D.md` (formes, palette, budgets, modularité, comment ajouter un emplacement ou un héros, comment remplacer par des pièces importées).
- **Limites** : trois paliers seulement ; pas de skins « cosmétiques » indépendants de l'équipement ni d'aperçu 3D dans l'Atelier (à faire) ; les talents ne changent pas l'apparence ; les pièces restent procédurales.
- **Question B** : aucune règle ni valeur de jeu modifiée ; 748 tests et 11 scénarios e2e verts.

## D-062 — Modèles importés : chaîne d'import de l'arme

- **Date** : 2026-09-20 — **Statut** : Livrée et validée avec une pièce de test ; aucune ressource externe importée (téléchargement en attente d'accord).
- **Mécanique** : une entrée d'apparence peut désigner un modèle (`AppearanceModel` : chemin sous Resources, taille voulue, décalage, rotation) qui remplace l'arme dessinée ; racine des ressources importées : `Assets/Resources/Imported/<Dossier>` (chargeables dans le jeu compilé ; ancien chemin Art/Imported abandonné). `ModelFactory` normalise la taille sur les maillages (l'unité FBX/OBJ/glTF ne compte pas), rattache le modèle au pivot d'arme et garde la version dessinée si le fichier manque. Garde-fous testés : un modèle « Imported/X/… » exige une entrée de crédit « X ». 3 tests ajoutés (traversée du catalogue, dossier déduit du chemin, crédit exigé).
- **Bug trouvé par la vérification** : les pièces dessinées détruites en fin d'image restaient listées comme rendus du héros et faisaient échouer la scène à chaque image ; corrigé par une destruction immédiate.
- **Question B** : aucune règle ni valeur de jeu modifiée.

## D-063 — Mode développeur

- **Date** : 2026-09-20 — **Statut** : Livré.
- **Demande** : savoir lancer un mode développeur ; pouvoir monter ou descendre le niveau des équipements et mettre l'or à 99 999.
- **Lancement** : option `-healer-dev` ; `lancer-le-jeu-dev.bat` (et `lancer-le-jeu.bat` pour le lancement normal) ; `npm run build:unity` construit le jeu en ligne de commande.
- **Sécurité de la progression** : sauvegarde **séparée** (`dev-profile`), jamais la vraie ; aucun bouton du mode n'existe hors de ce mode.
- **Atelier** : « Or → 99,9k » (`Workshop.DevSetGold`, par le portefeuille, donc journalisé « mode développeur ») et − / + par piste d'équipement (`Workshop.DevSetEquipmentLevel`, borné à 0..maximum, sans paiement). Le cœur porte ces deux fonctions (testées : bornes, aucun paiement, piste inconnue, or exact depuis 0 / 400 / 500 000, effet sur l'apparence et les statistiques) ; le jeu normal ne les appelle jamais.
- **Vérification** : scénario e2e L (or à 99 999, trois + puis un −, aucun achat, sauvegarde correcte). 761 tests et 12 scénarios verts.
- **Question B** : aucune règle ni valeur de jeu modifiée.

## D-064 — Armes importées pour les héros (KayKit Adventurers)

- **Date** : 2026-09-20 — **Statut** : Livrée pour Garde, Mage et Soigneuse ; Archère inchangée faute d'arc dans le pack.
- **Contenu importé** (CC0, crédité) : `sword_1handed`, `sword_2handed`, `sword_2handed_color`, `staff`, `wand` (non utilisé), textures et licence, dans `Assets/Resources/Imported/KayKitAdventurers`.
- **Mise en jeu** : Garde : épée courte (Ordinaire), épée à deux mains (Raffiné), épée à deux mains colorée (Légendaire), portée en diagonale vers l'avant ; Mage et Soigneuse : bâton KayKit à trois tailles ; la Soigneuse **garde sa croix de cristal** dessinée par le code (nouvelle option `keep` : pièces dessinées à conserver sur le pivot). Le modèle est **recentré** (option `offset` = où va son centre) : l'origine du fichier ne compte plus, et sa taille est normalisée.
- **Archère** : le pack ne contient pas d'arc (arbalètes seulement) ; on garde l'arc dessiné par le code plutôt que de changer d'arme selon le palier. À combler avec un autre pack ou une pièce faite sur mesure.
- **Corps et armures** : toujours dessinés par le code ; les remplacer demande un squelette commun (voir docs/ART_3D.md).
- **Question B** : aucune règle ni valeur de jeu modifiée ; 761 tests et 12 scénarios e2e verts.

## D-065 — Galerie de modèles (mode développeur)

- **Date** : 2026-09-20 — **Statut** : Livrée.
- **Demande** : un menu, dans le mode développeur, pour consulter tous les modèles de personnages.
- **Contenu** : nouvel écran `AppScreen.Gallery`, bouton au menu visible seulement avec `-healer-dev` ; `ModelGallery` affiche seul, au centre, chaque héros (avec l'aspect de son arme et de son armure aux trois paliers, modèles importés compris) et chaque boss (calme ou fureur), en rotation automatique ou manuelle, avec zoom et les poses d'animation du jeu (via `UnitRig`). Ligne d'information : triangles et origine de l'arme. Mêmes fabriques que le combat : ce qu'on voit est ce qui est joué.
- **Cœur** : navigation (`OpenGallery`, retour, aucun combat depuis la galerie), actions `MenuGallery` et `GalleryControl` (seuls ses boutons et le retour passent), emplacement du bouton ; 4 tests. L'ancien test du menu principal a reçu la nouvelle action (voulu : c'est un nouveau bouton).
- **Piège rencontré** : masquer « tous les enfants de la scène » masquait aussi le système d'événements de l'interface (enfant du même objet) et coupait les clics ; exclu explicitement. Le scénario e2e M l'a révélé.
- **Question B** : aucune règle ni valeur de jeu modifiée ; 765 tests et 13 scénarios e2e verts.

## D-066 — Druide de la Soigneuse fait avec Blender (chaîne Blender en ligne de commande)

- **Date** : 2026-09-20 — **Statut** : Livrée (version de base unique).
- **Demande** : suite au problème de prise en main de l'épée, prendre en main la génération de modèles avec Blender et faire « le plus beau druide possible » pour la Soigneuse, **sans** déclinaisons par équipement.
- **Outil** : Blender 5.2 installé par winget avec l'accord de l'utilisateur. Pas de serveur MCP Blender : scripts Python lancés en ligne de commande (`blender --background --python art/blender/druid.py`), donc reproductibles et versionnés (`art/blender/lib.py` = outils, `druid.py` = le druide). Rendus de contrôle EEVEE dans `art/blender/out/`.
- **Modèle** : capuche vide aux yeux lumineux, bois de cerf, épaulières de feuilles, robe mousse, cape, racines au pied, bâton de cristal, lucioles. ~3 000 triangles (budget 3 300). Pièces nommées `<Pivot>__<Pièce>` et objets vides `Pivot_<Nom>` : Unity reconstruit les pivots (Torso, Head, ArmL, ArmR, Cape, Weapon) pour que `UnitRig` anime le modèle comme les autres.
- **Import** : `ModelFactory.ImportedDruid()` ; couleurs du FBX (linéaires) remises en couleurs d'écran puis éclaircies (×1,7 : la scène est plus sombre que le rendu Blender) ; taille normalisée. Le druide remplace le corps de la Soigneuse à tous les paliers d'équipement (apparence par équipement volontairement ignorée pour elle, comme demandé). Création propre au projet : `Assets/Resources/Parts/`, aucun crédit externe.
- **Correctif annexe** : le compteur de triangles de la galerie affichait 0 pour les maillages importés (lecture interdite dans l'exécutable) ; il utilise désormais le nombre d'indices.
- **Question B** : aucune règle ni valeur de jeu modifiée.

## D-067 — Blender piloté par MCP : druide v2 (crâne de cerf)

- **Date** : 2026-09-20 — **Statut** : Livrée ; le druide v1 (`art/blender/druid.py`, `Parts/Druid.fbx`) est gardé de côté comme repli.
- **Demande** : réessayer la génération de modèle avec le MCP Blender.
- **Installation** (accord de l'utilisateur) : `uv` (winget), Python 3.12 géré par uv, serveur `blender-mcp` lancé par `uvx` (déclaré dans `.mcp.json`, exclu de git : chemins propres à la machine), add-on « MCP for Blender » 1.7 (ahujasid) dans les add-ons de Blender 5.2. **Télémétrie désactivée** (l'add-on la propose cochée ; elle enverrait prompts, code et captures à un tiers).
- **Méthode** : `art/blender/druid_mcp.py` s'exécute dans le Blender ouvert via l'outil `execute_blender_code` (rendus de contrôle relus à chaque itération) ; le même script tourne aussi en ligne de commande. Il n'utilise pas `read_factory_settings` (qui coupe le serveur MCP).
- **Nouveautés** : masque de crâne de cerf (crâne, museau, arcades, orbites aux yeux lumineux), robe plissée (`folds`), bâton à croissant de lune en bois. 3 298 triangles (budget 3 300, contrôlé par le build).
- **Import** : `ModelFactory.ImportedDruid()` charge `Parts/DruidMcp`, à défaut `Parts/Druid`.
- **Question B** : aucune règle ni valeur de jeu modifiée.

## D-068 — Druide v3 : méthode « corps d'abord » avec contrôle automatique des dégagements

- **Date** : 2026-09-20 — **Statut** : Livrée ; v1 (`druid.py`) et v2 (`druid_mcp.py`) gardées comme replis (le jeu charge `Parts/DruidV3`, à défaut `DruidMcp`, à défaut `Druid`).
- **Retour de l'utilisateur sur la v2** : le bâton rentrait dans la robe (porté de façon peu naturelle) et les mains étaient minuscules. **Cause** : pièces posées à la main par coordonnées, sans vérifier les rapports entre elles ; poing de 10 cm sur un personnage de 2,6 m.
- **Méthode** : pose imposée d'abord (épaule, coude, poignet, prise du bâton), puis vêtements et ornements autour ; mains à taille de figurine (poing 0,16 m pour une tête de 0,4 m, quatre doigts refermés et pouce) ; bras gauche tendu sur le côté, paume ouverte, visible de face ; robe resserrée. **Contrôle automatique** dans le script : distance minimale entre le bâton (tige et croissant) et toutes les autres pièces, avec refus d'exporter sous 7 cm (arbre BVH de Blender) ; il a déjà détecté deux collisions (racine décorative, feuilles de la mante), résolues en écartant ces éléments. Dégagement obtenu : 11 cm.
- **Bois de cerf** (idée retenue par l'utilisateur) : agrandis, un broc frontal et quatre cors par côté.
- **Budget** : 3 298 triangles (limite 3 300, contrôlée par le build) ; feuilles réduites en conséquence.
- **Question B** : aucune règle ni valeur de jeu modifiée.


## D-069 — Druid v4 : high-definition smooth-surface model, higher triangle budget for Blender-made heroes

- **Date** : 2026-09-21 — **Statut** : Livrée (v1 à v3 conservées comme replis : le jeu charge `DruidV4`, à défaut `DruidV3`, `DruidMcp`, `Druid`).
- **Retour de l'utilisateur** sur la v3 : pas à son goût ; il faut un druide entièrement nouveau et beaucoup plus de polygones. Il a aussi demandé pourquoi la limite de 3 300 triangles.
- **Réponse sur la limite** : c'était un choix prudent (mobile d'abord) pour les figurines à facettes dessinées par le code, pas une contrainte technique. Un héros de 30 000 triangles passe sans difficulté sur un téléphone milieu de gamme. Nouveau budget `ModeledBudget = 40 000` (Builder) pour les héros modélisés sous Blender ; `CharacterBudget = 3 300` reste pour les figurines du code.
- **Méthode** : `art/blender/lib2.py` (balayage de tubes à section variable, surfaces par anneaux, feuilles à deux faces, blobs lissés, boîtes arrondies) et `art/blender/druid_v4.py` : capuche drapée, masque de crâne de cerf, grands bois avec vignes et fleurs lumineuses, robe plissée, mains modelées (doigts qui enserrent le bâton, paume ouverte), bâton en cage de branches avec breloques, mante de feuilles, cape plissée. 30 830 triangles, normales exportées telles quelles (`mesh_smooth_type="OFF"`). Le contrôle automatique de dégagement du bâton (D-068) est conservé (11 cm).
- **Question B** : aucune règle ni valeur de jeu modifiée.

## D-070 — Phase-based attack and cast animations

- **Date** : 2026-09-21 — **Statut** : Livrée en première version ; l'utilisateur veut approfondir plus tard.
- **Constat** : les gestes étaient de simples allers-retours (sinus) ; une incantation était coupée net à son achèvement, et le bâton du druide balayait le sol en levant le bras.
- **Cœur** (`UnitAnimator` / `UnitPose`, sans effet sur les golden) : nouveaux canaux `LungeProgress` (0 à 1 pendant le coup), `CastElapsedMs`, `CastDurationMs`, `Channeling`, et `Release` (lâcher du sort à la fin d'une incantation, s'éteint en 420 ms). Anciens canaux inchangés. 6 tests ajoutés.
- **Client** : `UnitRig.ApplyPose` avec un style par héros (`RigStyle` : Melee pour le Garde, Bow pour l'Archère, Staff pour le Mage et la Soigneuse). Coup d'épée : levée, frappe rapide, retour, avec torsion du buste. Tir à l'arc : bras tendu, corde tirée, lâcher. Incantation : montée, tenue avec tremblement, lâcher vers l'avant (cape qui se soulève, main libre ouverte, bâton compensé pour rester presque droit). Les boss gardent l'ancien mouvement.
- **Outil de développement** : dans la galerie, bouton « Ralenti » (1/10) et touches « . » / « , » pour avancer/reculer image par image de 50 ms (fige l'animation) ; utile pour examiner et capturer une phase précise.
- **Question B** : aucune règle ni valeur de jeu modifiée ; seuls des canaux de présentation ont été ajoutés.


## D-071 — Pixel-art side-view battle (FFVI style) : 3D-to-sprite pipeline experiment

- **Date** : 2026-09-21 — **Statut** : Proposition en cours d'essai (branche `pixel-art`) ; rien n'est branché dans le jeu.
- **Demande** de l'utilisateur : ranger la partie modélisation 3D dans un dossier à part, et tenter une approche pixel art en vue de côté (bataille comme Final Fantasy VI) avec la méthode Dead Cells ; à l'avenir, utiliser les termes techniques anglais.
- **Organisation** : `art/` renommé `art-3d/` (scripts Blender, FBX sources) ; nouveau `art-pixel/` (pipeline de sprites). Les anciennes décisions gardent leurs chemins d'origine (`art/blender`) : lire `art-3d/blender`.
- **Avis donné avant de commencer** : la performance n'est pas le vrai enjeu (un héros de 30 000 triangles passe sur mobile) ; le pixel art dessiné à la main n'est pas praticable pour Claude (pas d'outil de dessin, cohérence entre frames) ; le 3D-to-pixel l'est, et il permet de re-rendre automatiquement les variantes d'équipement.
- **Résultat du premier essai** : `art-pixel/blender/sprites.py` produit 6 poses du druide v4 en 128 px, palette de 28 couleurs, contour 1 px. Vue de profil pur : le bâton cache le corps ; vue trois-quarts depuis le côté de la main libre, personnage tourné vers la gauche : lisible (crâne, bois, orbe, cristal). Voir `art-pixel/README.md`.
- **À décider ensuite** (par l'utilisateur) : taille des sprites (128 ou moins, FFVI est plus petit), rendu des ombres et des contours, intégration Unity (sprite renderer + pixel-perfect camera), remplacement ou coexistence avec le rendu 3D.
- **Question B** : aucune règle ni valeur de jeu modifiée.


## D-072 — Side-view battle layout (team on the left, boss on the right)

- **Date** : 2026-09-21 — **Statut** : Livrée sur la branche `pixel-art` (première version, à affiner à l'oeil).
- **Demande** : réorganiser la scène comme Final Fantasy VI : équipe à gauche, boss à droite (précision de l'utilisateur : à gauche pour l'équipe, à droite pour le boss).
- **Changements** (client et layout, aucune règle de combat) : `Layout.AllyStageX` / `AllyStageFeetY` placent les alliés en colonne diagonale à gauche (l'allié d'indice 0, devant, est le plus proche du boss), `Layout.BossStageX/FeetY` place le boss à droite. Les héros regardent vers la droite (yaw 145°, trois-quarts face caméra), le boss vers la gauche (215°). Un coup porté avance vers le boss, un coup reçu recule vers la gauche ; le coup du boss avance vers l'équipe. Les héros sont plus petits (échelle 1,45 au lieu de 1,85) pour tenir dans une colonne ; profondeur décalée par allié pour un ordre d'affichage correct. Anneau de danger sous la colonne d'alliés, cercle rituel entre l'équipe et le boss. Les sprites pixel art sont mis en miroir (dessinés face à gauche).
- **Test remplacé (voulu)** : `Les_allies_de_la_scene_sont_alignes_au_centre_sans_se_chevaucher` exprimait l'ancienne exigence (ligne centrée) ; il est remplacé par `Les_allies_de_la_scene_forment_une_colonne_a_gauche_face_au_boss` et `Le_boss_est_a_droite_dans_la_zone_centrale`. C'est une conséquence directe de la demande de l'utilisateur, pas un contournement.
- **Question B** : aucune règle ni valeur de jeu modifiée ; 772 tests et 13 scénarios e2e verts.


## D-073 — Rigify rig and animation pipeline for the pixel-art druid, ffmpeg previews, pixel-perfect placement

- **Date** : 2026-09-21 — **Statut** : Livrée sur la branche `pixel-art` (test de rendu, druide uniquement).
- **Demande** : utiliser Rigify pour les animations, Pixel Perfect Camera, et ffmpeg pour les previews si peu coûteux.
- **Rigify** (`art-pixel/blender/rig_druid.py`) : le modèle est découpé en pièces indépendantes (229), le metarig humain de Rigify est ajusté au druide (colonne, tête, bras dans leur pose pliée, jambes cachées par la robe), le rig est généré (706 bones), puis chaque pièce est skinnée : pièces rigides sur un os de déformation (selon leur nom et leur position), manches avec des poids lissés à travers le coude pour un pli propre.
- **Animations** (`art-pixel/blender/animate_druid.py`) : poses planes (angles autour de l'axe X du monde, sur les contrôles FK de Rigify), donc quelques clés de quelques nombres par animation : idle (6 frames, boucle), cast_raise (4), cast_hold (6, boucle), cast_release (5), attack (6), hit (3), death (7). Le bras libre est tourné vers l'avant dans le plan de côté (sinon il pointe vers la caméra et disparaît). Le bâton reste presque droit pendant que la main bouge. Sorties : une planche par animation, `druid_anims.txt` (frames, fps, boucle), une planche de contact, et `--unity` copie le tout dans `Resources/Sprites/Druid`.
- **ffmpeg** (installé par winget avec l'accord de l'utilisateur, `Gyan.FFmpeg`) : `sprite_lib.encode_previews` produit `druid_preview.gif` et `.mp4` (playlist de toutes les animations, plus de 40 images, quelques secondes de CPU).
- **Unity** : `SpriteHero` joue maintenant les planches (animation et frame choisies uniquement à partir de `UnitPose` : mort, coup reçu, incantation avec montée, tenue et lâcher, attaque, repos).
- **Pixel-perfect** : le paquet `com.unity.2d.pixel-perfect` n'est PAS installé, volontairement : il exige une caméra orthographique, et la caméra de combat est encore en perspective (héros et boss 3D). À la place, `SpriteHero` fait ce que fait cette caméra : chaque pixel de sprite couvre un nombre entier de pixels d'écran (2 à 1280x720, 3 en 1080p), et le coin du cadre est calé sur la grille de pixels d'écran ; le sprite reste net et ne scintille pas. Le paquet et une caméra orthographique deviendront pertinents quand tous les héros et les boss seront en sprites.
- **Question B** : aucune règle ni valeur de jeu modifiée.


## D-074 — Direction artistique : illustrations 2D animées par le code (abandon de la fabrication 3D par héros)

- **Date** : 2026-09-21 — **Statut** : Livrée pour la Soigneuse sur la branche `pixel-art` ; direction à confirmer sur les autres unités.
- **Constat de l'utilisateur** : la fabrication 3D d'un héros (modélisation, rig, animations, puis sprites) est **trop coûteuse** pour le résultat
  obtenu, y compris avec un sous-agent Opus (Archère : 40 min, 316 k tokens, 20 lancements Blender, qualité jugée insuffisante). Le pixel art
  n'est pas un prérequis : pleine liberté de style.
- **Décision** : rester dans Unity (le cœur et ses 774 tests sont indépendants du rendu ; quitter le moteur imposerait de tout porter pour zéro gain
  sur l'art) et changer la SOURCE de l'art : **une seule illustration par unité**, générée avec un outil d'image, et **l'animation faite par le code**
  à partir de `UnitPose`. Plus de modélisation, plus de rig, plus d'images d'animation à dessiner. Ces illustrations serviront aussi de portraits gacha.
- **Chaîne** : `art-2d/inbox/<unité>_<pose>.png` (fond magenta uni) → `art-2d/tools/process.py` (détourage par distance au fond, décontamination de la
  frange, recadrage, mise à l'échelle par Blender, aperçu sur le fond du jeu) → `Resources/Art2D/`. `Art2DUnit` (option `-healer-art2d`,
  `lancer-le-jeu-2d.bat`) pose l'illustration sur une grille face caméra et la déforme : respiration, ondulation du tissu (le bas reste planté au sol),
  inclinaison vers le boss à l'attaque et au lâcher de sort, recul au coup reçu, bascule à la chute. Une unité sans illustration garde son modèle 3D :
  les deux rendus coexistent.
- **Remise en cause de spec (préambule A)** : la politique de crédits (D-060) n'autorisait pour les outils que des licences permissives, ce qui
  refusait Blender (GPL-3.0) et FFmpeg (LGPL-2.1). La règle visait les bibliothèques **liées** au jeu ; un outil de **production**, qui tourne sur
  notre machine et n'est jamais distribué, peut être sous copyleft sans toucher le jeu ni ses fichiers produits. `ToolLicenses` étendu avec
  GPL-3.0 et LGPL-2.1, la distinction documentée et **vérifiée par deux nouveaux tests** (un outil copyleft passe, une ressource importée copyleft
  est refusée, une licence « À préciser » est refusée). La règle sur les ressources importées n'a pas bougé.
- **À préciser (information manquante)** : le nom de l'outil qui a généré l'illustration de la Soigneuse, son URL et ses conditions d'usage
  commercial. Tant qu'ils manquent, l'entrée ne peut pas entrer au générique (le test la refuse, à raison). À demander à l'utilisateur.
- **À prévoir avant publication** : Steam et Google Play imposent de déclarer les contenus générés par IA ; tenir la liste outil + prompt (elle est
  dans `art-2d/README.md`).
- **Le 3D reste en place** comme repli (`art-3d/`, `art-pixel/`, modèles et sprites commités) : aucun retour en arrière n'est fermé.
- **Question B** : aucune règle ni valeur de jeu modifiée ; seules la présentation et la politique de crédits ont changé.
