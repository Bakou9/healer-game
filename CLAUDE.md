# Instructions pour un agent travaillant sur ce projet (version Unity + MCP)

Ce fichier s'adresse à un assistant IA (Claude Code ou équivalent) amené à
modifier ce dépôt. Contexte complet dans `README.md` et `docs/`.

**Ce dépôt est le jeu Unity, et rien d'autre.** Racine du dépôt
https://github.com/Bakou9/healer-game ; en local : `C:\WhatTheHeal`.
La version Phaser (TypeScript) a été **retirée** du dépôt (décision D-045, qui
remplace D-027 et D-033). Elle reste consultable dans l'historique Git :
`git show phaser-archive:src/sim/Battle.ts`, ou `git checkout phaser-archive`
pour tout retrouver. Ne rien recréer de Phaser ici. Table de correspondance
des anciens chemins : `docs/MIGRATION_UNITY.md`.

## PRÉAMBULE SYSTÉMATIQUE (à appliquer AVANT et APRÈS chaque ticket)

Décisions D-016 et D-017. Ce préambule prime sur l'envie d'« exécuter la spec
telle quelle ».

**A. Remettre en question les specs.** Avant de coder un ticket, se demander :
*« Ce que dit la spec (ce ticket, ses voisins, la vision, les tickets déjà
terminés) est-il encore la meilleure option pour le gameplay ? »* Si une
alternative apporte quelque chose au gameplay (plaisir, lisibilité, décisions
plus intéressantes, équilibre, simplicité), **je la propose et j'amende la spec**
— y compris pour du travail déjà fait — au lieu de suivre la spec par
habitude. Toute remise en cause est **expliquée** (ce qui change, pourquoi c'est
mieux, ce que ça coûte), consignée dans `docs/DECISIONS.md`, reportée dans les
tickets touchés, et signalée à l'utilisateur. Si la remise en cause change une
décision **ferme** de l'utilisateur, je ne l'applique pas seul : je la lui
soumets.

**B. Question d'équilibrage à chaque ticket.** Pour **chaque** ticket, avant
puis après l'implémentation, se demander : *« Est-ce que cela peut changer
l'équilibre du jeu (règles, valeurs, rythme d'action possible, lisibilité des
dangers, difficulté ressentie) ? »*
- Oui ou doute → mesurer (profils de `docs/EQUILIBRAGE.md`, analyse de
  sensibilité si c'est de l'UX) et rapporter l'avant/après.
- **Si le jeu ne me paraît plus équilibré** (borne dépassée, critère du §2
  d'EQUILIBRAGE.md menacé, dérive suspectée) : **je ne tranche pas seul.** Je
  l'explique à l'utilisateur (quoi, pourquoi, chiffres, options) et on valide
  ensemble avant de considérer le ticket terminé ou de bouger une borne.
- Dans tous les cas, la réponse est consignée dans **`docs/REVUES.md`** (une
  ligne par ticket terminé ; un test refuse un ticket « Terminé » sans ligne).

**C. Compte rendu.** Chaque compte rendu de ticket dit explicitement : (1) ce
que j'ai remis en cause dans les specs, (2) le verdict d'équilibrage et les
mesures, (3) ce que l'utilisateur doit valider.

## Ce qu'est ce projet

Un jeu gacha « healer » : combat en temps réel où le joueur ne contrôle que le
soigneur (le reste de l'équipe est en auto-battle). Version Unity (C#, 3D
stylisée, URP), pilotée avec un serveur MCP, Android d'abord, Steam ensuite.
Vision : `docs/specs/VISION.md`.

Le projet avance par phases courtes et testables. **Ne pas anticiper des
fonctionnalités de phases suivantes** sans qu'on le demande. La priorité
actuelle est la **migration à parité** (epic E14) : mêmes règles, mêmes tests,
mêmes valeurs, puis nouveautés.

## Règles d'architecture (détail : `docs/ARCHITECTURE_UNITY.md`)

1. **Le cœur est en C# pur, sans Unity.** `core/Healer.Combat` (simulation) et
   `core/Healer.Ui` (formatage, mise en page, ciblage) n'utilisent jamais
   `UnityEngine`. Ils sont compilés par dotnet (tests) **et** par Unity (paquets
   locaux). Toute règle de jeu y vit ; le client Unity n'en contient aucune.
2. **Tout le contenu de jeu est dans `core/content/*.json`**, pas codé en dur ;
   un nouveau personnage, sort, effet ou boss s'ajoute en éditant un JSON. Un
   nouveau *type* d'effet étend le cœur de façon générique (pas de `if` propre à
   un seul sort).
3. **Déterminisme.** `Battle` prend un `seed` ; **jamais** `System.Random`,
   `UnityEngine.Random`, `DateTime`, `Stopwatch` dans le cœur : uniquement le RNG
   seedé fourni. Toute mécanique aléatoire passe par lui.
4. **Rien de gameplay côté client qui devra un jour être autoritatif** : le cœur
   est écrit comme s'il tournait un jour sur un serveur (pas d'accès fichier,
   réseau ou moteur).
5. **`MonoBehaviour` minces** : ils relient (lecture d'état, commandes,
   événements), ils ne calculent pas. `Time.deltaTime` n'alimente que le pas fixe
   du client.
6. **Utiliser le MCP sans perdre la reproductibilité** : le code d'abord (fichiers
   `.cs`) ; scènes et préfabriqués **reconstruisibles par scripts d'Éditeur** ;
   tout versionné, `.meta` compris ; vérifier par tests et par la console, pas à
   l'œil seul.

## Patterns de développement (OBLIGATOIRE)

Référence : **`docs/PATTERNS_JEU_VIDEO.md`** (pas fixe, Command, Observer,
data-driven, séparation modèle/vue, effets sur la durée, phases de boss, Object
Pool ; différés : ECS, Behavior Tree, Strategy…). Le consulter avant d'écrire du
code ; **citer les patterns appliqués** dans chaque compte rendu ; n'adopter un
pattern différé qu'à l'atteinte de son déclencheur ; le mettre à jour si on en
adopte un.

## Specs, décisions et mémoire persistante (OBLIGATOIRE)

- **Mémoire du projet = fichiers du dépôt, pas la conversation.** Avant de
  travailler, lire `docs/DECISIONS.md` et le ticket concerné (`docs/specs/`,
  index dans `docs/specs/README.md`, vision dans `VISION.md`).
- **Toute nouvelle décision, exigence ou correction de l'utilisateur** est
  ajoutée à `docs/DECISIONS.md` (daté, numéroté, statut) et rattachée à un ticket.
  On ne supprime pas une décision : on la remplace par une nouvelle qui la cite.
  Information manquante = « À préciser » (le dire) ; hypothèse à moi = « Proposition ».
- **Un ticket = une unité de travail.** Critères cochés seulement s'ils sont vrais
  et testés ; définition de « terminé » dans `docs/specs/README.md`. Après tout
  changement de ticket ou de statut : `npm run specs:index`.
  `tools/specs/specs.test.ts` échoue si les specs sont incohérentes.
- **Dans ce dépôt, « Terminé » signifie fait, testé et revu ICI.** Les tickets
  réalisés en Phaser ont été remis à « À faire » (voir epic E14).
- **Spécifier n'autorise pas à implémenter.** Ne développer que les tickets de la
  phase en cours ou explicitement demandés.
- **UX : `docs/UX.md`.** Deux gestes au plus, cibles ≥ 48 px, texte ≥ 14 px,
  jamais la couleur seule, retours plafonnés. **Modèles 3D : `docs/ART_3D.md`.**
- **Équilibrage entre choix de spécialisation** : toute option, talent ou build
  passe la batterie d'équilibrage (E08 et `docs/EQUILIBRAGE.md` §8).

## Équilibrage et valeurs lisibles (OBLIGATOIRE)

- **`docs/MECANIQUES.md`** décrit chaque règle, formule et valeur du jeu (et ce qui n'existe pas). **Toute modification de règle ou
  de valeur le met à jour dans le même commit** ; une règle absente de ce fichier est considérée comme inexistante.
- **Une borne d'équilibrage doit aussi punir les mauvais choix** : ne pas seulement mesurer le joueur de référence complet, mais
  vérifier qu'une stratégie paresseuse (un seul sort, pas de bouclier ni de purge) échoue sur les boss avancés
  (`StrategyDiversityTests`, D-050).

- **`docs/EQUILIBRAGE.md`** définit l'équilibre (6 critères mesurables, profils de
  joueurs de référence, boutons de réglage, procédure). Le lire avant de toucher
  à `core/content/*.json`, à une règle de combat ou au bot de référence ;
  rendre compte de l'avant/après. Ne jamais relâcher une borne pour faire passer
  un test. Le portage doit **retrouver à l'identique** les mesures de la version Phaser.
- **Valeurs à hauteur humaine.** Tout nombre affiché est **tronqué** (jamais arrondi
  vers le haut), sans décimales inutiles, abrégé si long (`7000`, `12,3k`,
  `4,9s`), via le formatage de `core/Healer.Ui` ; jamais de `ToString("F1")`,
  `Mathf.Round` ou nombre brut dans un texte affiché. Dans les JSON, toute
  quantité de jeu est un entier rond ; seuls `multiplier` et les ratios peuvent
  être décimaux (vérifié par test).

## Protocole de non-régression (OBLIGATOIRE)

Les tests détectent les régressions ; l'utilisateur veut **comprendre chacune**.

- `npm run check` = specs + tests du cœur (`dotnet test`) + (en option) Unity en
  mode batch. Doit être vert avant de conclure. Il **échoue** si un outil requis
  manque : ne jamais le contourner.
- Les combats de référence sont figés dans **`core/golden/*.txt`** (un événement par
  ligne) et comparés à chaque test. Un test d'équilibrage vérifie que le combat
  reste gagnable et tendu avec le bot de référence.
- **Quand un test échoue après une modification :**
  1. Ne JAMAIS modifier un test, une valeur d'équilibrage attendue ou les
     fichiers golden pour « faire passer ».
  2. Rapporter : quel test, attendu, obtenu, **quelle modification en est la cause**,
     et un verdict argumenté : **voulu** ou **accidentel**.
  3. Accidentel : corriger. Voulu : attendre l'accord de l'utilisateur, puis
     régénérer les références et résumer ce qui a changé.
- Nouvelle mécanique → nouveau test + nouveau scénario ; bug corrigé → test qui
  échouait avant.
- Le cœur ne doit jamais référencer Unity ni l'horloge/l'aléa système : ne pas
  affaiblir les tests d'architecture (E14-T09).

## Développement local

- Unity : Éditeur ouvert sur `unity/HealerGame` ; les scripts se recompilent à
  l'enregistrement. Cœur : `dotnet test core/Healer.Combat.Tests`.
- **Ne jamais lancer une tâche longue comme tâche d'arrière-plan suivie par la
  session** : elle empêche de rendre la main à l'utilisateur (D-021). Lancer
  **détaché** (`Start-Process` caché, journal dans `%TEMP%`) et surveiller le
  journal. Ne jamais laisser une tâche suivie active à la fin d'un tour.
- **Installer un logiciel ou télécharger un fichier exige l'accord explicite de
  l'utilisateur** (nom, source, taille). Ne **jamais** saisir d'identifiant, de mot
  de passe ni de clé (compte Unity, licence : c'est l'utilisateur qui les saisit).

## Workflow attendu pour toute modification

1. Lire le ticket et `docs/DECISIONS.md` ; lancer `npm run check` pour l'état de référence.
2. Appliquer le préambule (A : remise en cause, B : équilibrage).
3. Modifier. Si l'équilibrage change, utiliser ou étendre le bot de référence.
4. `npm run check` doit passer ; ajouter les tests ; expliquer toute régression.
5. Mettre à jour le ticket, `docs/REVUES.md`, `docs/DECISIONS.md`, l'index, puis committer.

## Pièges connus

- `Battle.Step(dtMs)` ne traite qu'une action par appel : ne jamais l'appeler avec un
  `dt` plus grand que les intervalles des données. Le client passe par `FixedStepper`
  (pas de 50 ms) ; un test vérifie que le pas reste inférieur aux intervalles.
- Les événements de combat alimentent les golden : changer leur format ou leur ordre
  change les golden — à signaler, pas à régénérer en silence.
- Les identifiants (`id`) des JSON sont référencés en dur ailleurs (`healer`, `tank`) :
  chercher avant de renommer.
- Le MCP donne accès à l'Éditeur : ne rien y faire qui ne soit reconstruisible par
  script ou versionné.
