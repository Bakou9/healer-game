# Spécifications du jeu — index

Ce dossier est la **mémoire persistante du projet**. Il contient :

| Élément | Rôle |
|---|---|
| [`VISION.md`](VISION.md) | Spécification générale du jeu (GDD) : piliers, règles, systèmes, feuille de route, questions ouvertes |
| `epics/E##-nom/` | Un dossier par **epic** ; son `README.md` décrit l'objectif, le périmètre et les critères de sortie |
| `epics/E##-nom/E##-T##-….md` | Un fichier par **ticket** (unité de travail testable) |
| [`../DECISIONS.md`](../DECISIONS.md) | Journal des décisions de l'utilisateur, daté et numéroté |
| [`../EQUILIBRAGE.md`](../EQUILIBRAGE.md) | Définition de l'équilibre et bornes mesurées |
| [`../ARCHITECTURE.md`](../ARCHITECTURE.md) | Architecture modulaire cible et règles de dépendance |
| [`../UX.md`](../UX.md) | Audit et principes UX |
| [`../PATTERNS_JEU_VIDEO.md`](../PATTERNS_JEU_VIDEO.md) | Patterns de développement de jeu à appliquer |

Les tableaux ci-dessous sont **générés** : ne pas les modifier à la main,
lancer `npm run specs:index` après tout changement de statut ou de ticket.
Le test `src/testing/specs.test.ts` échoue si les specs sont incohérentes ou
si l'index n'est pas à jour.

## Règles de travail

1. **Un ticket = une unité de travail vérifiable.** Ses critères d'acceptation
   sont cochés (`- [x]`) uniquement quand ils sont vrais et testés.
2. **Spécifier n'autorise pas à implémenter.** On ne développe que les tickets
   de la phase en cours ou explicitement demandés (voir `CLAUDE.md`).
3. **Toute nouvelle exigence ou décision** de l'utilisateur est ajoutée à
   `DECISIONS.md` et rattachée à un ticket (nouveau ou existant).
4. **Un ticket est « Terminé »** seulement si la définition ci-dessous est remplie.

## Définition de « terminé » (DoD)

- [ ] Tous les critères d'acceptation sont cochés et vrais
- [ ] `npm run check` est vert (types, tests, build)
- [ ] Les tests exigés par le ticket existent
- [ ] **Impact équilibrage évalué** : si « Oui », les mesures de `docs/EQUILIBRAGE.md` sont rejouées, l'avant/après est rapporté et les bornes tiennent
- [ ] **Spec remise en question** (préambule A de `CLAUDE.md`) : la spec du ticket et de ses voisins est-elle encore la meilleure option pour le gameplay ? Amendements expliqués et consignés
- [ ] **Question d'équilibrage posée** (préambule B) : mesurée si oui/doute ; si le jeu ne paraît plus équilibré, expliqué à l'utilisateur et validé ensemble
- [ ] Une ligne dans `docs/REVUES.md` (vérifié par test)
- [ ] Les patterns de `docs/PATTERNS_JEU_VIDEO.md` appliqués sont cités
- [ ] Valeurs affichées tronquées à hauteur humaine (`src/ui/format.ts`)
- [ ] Les régressions détectées sont expliquées (voulu/accidentel) selon `CLAUDE.md`
- [ ] Statut mis à jour, `npm run specs:index` lancé, décision consignée si besoin

## Modèle de ticket

```markdown
---
id: E##-T##
epic: E##
titre: …
type: Feature | Tech | Test | Design | Bug
priorité: P0 | P1 | P2 | P3
phase: 1..5
statut: À faire | En cours | Terminé | Abandonné
taille: S | M | L | XL
dépendances: E##-T##, … (ou « aucune »)
---

# E##-T## — titre

## Contexte
## Critères d'acceptation
- [ ] …
## Tests automatiques exigés
## Impact équilibrage
```

Priorités : **P0** fondation bloquante · **P1** nécessaire à la phase · **P2**
nécessaire au lancement · **P3** plus tard. Phases : voir `VISION.md` §15.

## Index

<!-- TICKETS:START -->
### Epics

| Epic | Titre | Tickets | Terminés |
|---|---|---|---|
| [E01](epics/E01-combat-core/README.md) | Cœur du combat | 13 | 0/13 |
| [E02](epics/E02-healer-specialisation/README.md) | Spécialisation et progression du soigneur | 10 | 0/10 |
| [E03](epics/E03-bosses-encounters/README.md) | Boss et rencontres | 9 | 0/9 |
| [E04](epics/E04-ux-ui/README.md) | Expérience et interface (UX/UI) | 14 | 1/14 |
| [E05](epics/E05-roster-team/README.md) | Personnages et équipe | 8 | 0/8 |
| [E06](epics/E06-gacha-economy/README.md) | Gacha et économie | 9 | 0/9 |
| [E07](epics/E07-game-modes/README.md) | Modes de jeu et méta-progression | 6 | 0/6 |
| [E08](epics/E08-balance-framework/README.md) | Cadre d'équilibrage automatisé | 15 | 0/15 |
| [E09](epics/E09-modular-architecture/README.md) | Architecture modulaire | 11 | 1/11 |
| [E10](epics/E10-persistence-backend/README.md) | Persistance et backend | 9 | 0/9 |
| [E11](epics/E11-platforms-release/README.md) | Plateformes et publication | 7 | 0/7 |
| [E12](epics/E12-art-audio/README.md) | Direction artistique et audio | 5 | 0/5 |
| [E13](epics/E13-quality-governance/README.md) | Qualité, outillage et gouvernance | 9 | 2/9 |
| [E14](epics/E14-unity-migration/README.md) | Migration Unity et MCP | 17 | 7/17 |

Total : 142 tickets, 11/142 terminés.

### Tous les tickets

| Ticket | Titre | Priorité | Phase | Statut |
|---|---|---|---|---|
| [E01-T01](epics/E01-combat-core/E01-T01-simulation-de-combat-deterministe-et-pure.md) | Simulation de combat déterministe et pure | P0 | 1 | À faire |
| [E01-T02](epics/E01-combat-core/E01-T02-pas-de-simulation-fixe-game-loop.md) | Pas de simulation fixe (Game Loop) | P0 | 1 | À faire |
| [E01-T03](epics/E01-combat-core/E01-T03-evenements-de-combat-observer.md) | Événements de combat (Observer) | P0 | 1 | À faire |
| [E01-T04](epics/E01-combat-core/E01-T04-commandes-du-joueur-command-horodatees.md) | Commandes du joueur (Command) horodatées | P0 | 1 | À faire |
| [E01-T05](epics/E01-combat-core/E01-T05-effets-sur-la-duree-pilotes-par-les-donnees-pois.md) | Effets sur la durée pilotés par les données (poison) et Purge | P1 | 1 | À faire |
| [E01-T06](epics/E01-combat-core/E01-T06-phases-de-boss-table-de-transitions-en-donnees.md) | Phases de boss (table de transitions en données) | P1 | 1 | À faire |
| [E01-T07](epics/E01-combat-core/E01-T07-soin-soin-de-zone-et-bouclier.md) | Soin, soin de zone et bouclier | P0 | 1 | À faire |
| [E01-T08](epics/E01-combat-core/E01-T08-registre-d-effets-extensible-soin-sur-la-duree-s.md) | Registre d'effets extensible (soin sur la durée, statistiques, étourdissement, vulnérabilité) | P1 | 2 | À faire |
| [E01-T09](epics/E01-combat-core/E01-T09-k-o-et-resurrection.md) | K.O. et résurrection | P2 | 2 | À faire |
| [E01-T10](epics/E01-combat-core/E01-T10-ciblage-du-boss-menace-focus-du-plus-faible-alea.md) | Ciblage du boss : menace, focus du plus faible, aléatoire pondéré | P1 | 2 | À faire |
| [E01-T11](epics/E01-combat-core/E01-T11-coups-critiques-et-variance-maitrisee.md) | Coups critiques et variance maîtrisée | P2 | 2 | À faire |
| [E01-T12](epics/E01-combat-core/E01-T12-serialisation-et-replay-d-un-combat-seed-command.md) | Sérialisation et replay d'un combat (seed + commandes) | P1 | 2 | À faire |
| [E01-T13](epics/E01-combat-core/E01-T13-statistiques-de-combat-soins-effectifs-surplus-d.md) | Statistiques de combat (soins effectifs, surplus, dégâts évités) | P1 | 2 | À faire |
| [E02-T01](epics/E02-healer-specialisation/E02-T01-modele-de-donnees-des-specialisations-et-talents.md) | Modèle de données des spécialisations et talents (schéma) | P0 | 2 | À faire |
| [E02-T02](epics/E02-healer-specialisation/E02-T02-trois-voies-de-specialisation-lumiere-egide-puri.md) | Trois voies de spécialisation : Lumière, Égide, Purification | P0 | 2 | À faire |
| [E02-T03](epics/E02-healer-specialisation/E02-T03-points-de-talent-paliers-prerequis-et-reinitiali.md) | Points de talent, paliers, prérequis et réinitialisation (respec) | P1 | 2 | À faire |
| [E02-T04](epics/E02-healer-specialisation/E02-T04-application-des-talents-dans-la-simulation.md) | Application des talents dans la simulation | P0 | 2 | À faire |
| [E02-T05](epics/E02-healer-specialisation/E02-T05-competences-actives-debloquees-par-les-talents.md) | Compétences actives débloquées par les talents | P1 | 2 | À faire |
| [E02-T06](epics/E02-healer-specialisation/E02-T06-niveaux-et-courbe-d-experience-du-soigneur.md) | Niveaux et courbe d'expérience du soigneur | P1 | 3 | À faire |
| [E02-T07](epics/E02-healer-specialisation/E02-T07-equipement-et-reliques-du-soigneur.md) | Équipement et reliques du soigneur | P3 | 4 | À faire |
| [E02-T08](epics/E02-healer-specialisation/E02-T08-presets-de-builds-et-partage-par-code.md) | Presets de builds et partage par code | P3 | 4 | À faire |
| [E02-T09](epics/E02-healer-specialisation/E02-T09-budget-de-puissance-des-talents-declare-dans-les.md) | Budget de puissance des talents déclaré dans les données | P0 | 2 | À faire |
| [E02-T10](epics/E02-healer-specialisation/E02-T10-interface-de-l-arbre-de-talents.md) | Interface de l'arbre de talents | P1 | 2 | À faire |
| [E03-T01](epics/E03-bosses-encounters/E03-T01-archetypes-de-boss-definition-et-donnees.md) | Archétypes de boss : définition et données | P0 | 2 | À faire |
| [E03-T02](epics/E03-bosses-encounters/E03-T02-boss-1-golem-ancestral.md) | Boss 1 « Golem Ancestral » | P0 | 1 | À faire |
| [E03-T03](epics/E03-bosses-encounters/E03-T03-boss-2-archetype-attrition.md) | Boss 2 : archétype Attrition | P1 | 2 | À faire |
| [E03-T04](epics/E03-bosses-encounters/E03-T04-boss-3-archetype-burst-a-telegraphes-multiples.md) | Boss 3 : archétype Burst à télégraphes multiples | P1 | 2 | À faire |
| [E03-T05](epics/E03-bosses-encounters/E03-T05-boss-4-archetype-multi-cibles-avec-ennemis-invoq.md) | Boss 4 : archétype Multi-cibles avec ennemis invoqués | P2 | 2 | À faire |
| [E03-T06](epics/E03-bosses-encounters/E03-T06-mecaniques-de-phase-avancees-enrage-invocations.md) | Mécaniques de phase avancées (enrage, invocations, changement de règles) | P2 | 3 | À faire |
| [E03-T07](epics/E03-bosses-encounters/E03-T07-rencontres-a-plusieurs-vagues.md) | Rencontres à plusieurs vagues | P2 | 3 | À faire |
| [E03-T08](epics/E03-bosses-encounters/E03-T08-modificateurs-de-difficulte-affixes.md) | Modificateurs de difficulté (affixes) | P3 | 4 | À faire |
| [E03-T09](epics/E03-bosses-encounters/E03-T09-bestiaire-et-recit-des-boss.md) | Bestiaire et récit des boss | P3 | 4 | À faire |
| [E04-T01](epics/E04-ux-ui/E04-T01-audit-ux-et-principes-de-conception-docs-ux-md.md) | Audit UX et principes de conception (docs/UX.md) | P0 | 2 | Terminé |
| [E04-T02](epics/E04-ux-ui/E04-T02-rendu-net-sur-ecrans-haute-densite-et-mise-a-l-e.md) | Rendu net sur écrans haute densité et mise à l'échelle | P0 | 2 | À faire |
| [E04-T03](epics/E04-ux-ui/E04-T03-refonte-de-la-mise-en-page-portrait-zone-du-pouc.md) | Refonte de la mise en page portrait (zone du pouce) | P0 | 2 | À faire |
| [E04-T04](epics/E04-ux-ui/E04-T04-ciblage-en-un-geste-selection-puis-sorts-ou-tap.md) | Ciblage en un geste (sélection persistante puis sorts) | P0 | 2 | À faire |
| [E04-T05](epics/E04-ux-ui/E04-T05-cartes-d-allies-lisibles.md) | Cartes d'alliés lisibles | P0 | 2 | À faire |
| [E04-T06](epics/E04-ux-ui/E04-T06-barre-de-sorts-recharge-radiale-cout-de-mana-eta.md) | Barre de sorts : recharge radiale, coût de mana, états, raccourcis clavier | P1 | 2 | À faire |
| [E04-T07](epics/E04-ux-ui/E04-T07-telegraphes-lisibles.md) | Télégraphes lisibles | P0 | 2 | À faire |
| [E04-T08](epics/E04-ux-ui/E04-T08-retours-visuels-maitrises-agregation-et-plafond.md) | Retours visuels maîtrisés (agrégation et plafond, flash, secousse, haptique) | P1 | 2 | À faire |
| [E04-T09](epics/E04-ux-ui/E04-T09-ecran-de-bilan-de-combat.md) | Écran de bilan de combat | P1 | 2 | À faire |
| [E04-T10](epics/E04-ux-ui/E04-T10-menu-principal-choix-du-combat-pause-et-reglages.md) | Menu principal, choix du combat, pause et réglages | P1 | 2 | À faire |
| [E04-T11](epics/E04-ux-ui/E04-T11-tutoriel-et-introduction-progressive-des-sorts.md) | Tutoriel et introduction progressive des sorts | P1 | 2 | À faire |
| [E04-T12](epics/E04-ux-ui/E04-T12-accessibilite-daltonisme-taille-du-texte-contras.md) | Accessibilité (daltonisme, taille du texte, contraste, gaucher, animations réduites) | P1 | 3 | À faire |
| [E04-T13](epics/E04-ux-ui/E04-T13-tests-visuels-automatises-captures-deterministes.md) | Tests visuels automatisés (captures déterministes) | P1 | 2 | À faire |
| [E04-T14](epics/E04-ux-ui/E04-T14-protocole-de-playtest-et-metriques-ux.md) | Protocole de playtest et métriques UX | P2 | 3 | À faire |
| [E05-T01](epics/E05-roster-team/E05-T01-modele-de-personnage-etendu.md) | Modèle de personnage étendu | P1 | 3 | À faire |
| [E05-T02](epics/E05-roster-team/E05-T02-classes-et-roles-tank-degats-soutien-leger.md) | Classes et rôles : tank, dégâts, soutien léger | P1 | 3 | À faire |
| [E05-T03](epics/E05-roster-team/E05-T03-composition-d-equipe-4-emplacements-et-regles-de.md) | Composition d'équipe (4 emplacements) et règles de validité | P1 | 3 | À faire |
| [E05-T04](epics/E05-roster-team/E05-T04-synergies-d-equipe-pilotees-par-les-donnees.md) | Synergies d'équipe pilotées par les données | P2 | 3 | À faire |
| [E05-T05](epics/E05-roster-team/E05-T05-ia-d-auto-battle-priorites-de-cible-et-usage-des.md) | IA d'auto-battle : priorités de cible et usage des compétences | P1 | 3 | À faire |
| [E05-T06](epics/E05-roster-team/E05-T06-progression-des-personnages-niveaux-etoiles-equi.md) | Progression des personnages (niveaux, étoiles, équipement) | P2 | 3 | À faire |
| [E05-T07](epics/E05-roster-team/E05-T07-roster-de-lancement.md) | Roster de lancement | P2 | 4 | À faire |
| [E05-T08](epics/E05-roster-team/E05-T08-interface-d-equipe-et-de-collection.md) | Interface d'équipe et de collection | P2 | 3 | À faire |
| [E06-T01](epics/E06-gacha-economy/E06-T01-modele-economique-devises-sources-puits-rythme-c.md) | Modèle économique : devises, sources, puits, rythme cible | P1 | 3 | À faire |
| [E06-T02](epics/E06-gacha-economy/E06-T02-simulateur-d-economie-temps-de-progression-depen.md) | Simulateur d'économie (temps de progression, dépenses) | P1 | 3 | À faire |
| [E06-T03](epics/E06-gacha-economy/E06-T03-tables-de-tirage-et-bannieres-en-donnees.md) | Tables de tirage et bannières en données | P2 | 3 | À faire |
| [E06-T04](epics/E06-gacha-economy/E06-T04-moteur-de-tirage-deterministe-avec-garanties-pit.md) | Moteur de tirage déterministe avec garanties (pity) | P1 | 3 | À faire |
| [E06-T05](epics/E06-gacha-economy/E06-T05-affichage-des-taux-et-conformite-reglementaire.md) | Affichage des taux et conformité réglementaire | P1 | 4 | À faire |
| [E06-T06](epics/E06-gacha-economy/E06-T06-recompenses-de-combat-et-de-progression.md) | Récompenses de combat et de progression | P1 | 3 | À faire |
| [E06-T07](epics/E06-gacha-economy/E06-T07-boutique-et-achats-integres-iap.md) | Boutique et achats intégrés (IAP) | P2 | 4 | À faire |
| [E06-T08](epics/E06-gacha-economy/E06-T08-garde-fous-ethiques.md) | Garde-fous éthiques | P1 | 3 | À faire |
| [E06-T09](epics/E06-gacha-economy/E06-T09-equilibrage-inter-rarete-et-courbe-de-puissance.md) | Équilibrage inter-rareté et courbe de puissance (anti power creep) | P1 | 3 | À faire |
| [E07-T01](epics/E07-game-modes/E07-T01-campagne-chapitres-et-etapes.md) | Campagne : chapitres et étapes | P1 | 3 | À faire |
| [E07-T02](epics/E07-game-modes/E07-T02-defi-quotidien-graine-du-jour.md) | Défi quotidien (graine du jour) | P2 | 3 | À faire |
| [E07-T03](epics/E07-game-modes/E07-T03-tour-infinie-et-boss-rush.md) | Tour infinie et boss rush | P3 | 4 | À faire |
| [E07-T04](epics/E07-game-modes/E07-T04-classements-asynchrones-avec-rejeux-valides.md) | Classements asynchrones avec rejeux validés | P3 | 5 | À faire |
| [E07-T05](epics/E07-game-modes/E07-T05-evenements-temporaires.md) | Événements temporaires | P3 | 5 | À faire |
| [E07-T06](epics/E07-game-modes/E07-T06-succes-et-quetes.md) | Succès et quêtes | P3 | 4 | À faire |
| [E08-T01](epics/E08-balance-framework/E08-T01-profils-de-joueurs-de-reference-attentif-lent-sa.md) | Profils de joueurs de référence (attentif, lent, sans purge, passif, spam) | P0 | 1 | À faire |
| [E08-T02](epics/E08-balance-framework/E08-T02-un-bot-raisonnable-par-specialisation.md) | Un bot « raisonnable » par spécialisation | P0 | 2 | À faire |
| [E08-T03](epics/E08-balance-framework/E08-T03-generateur-de-l-espace-de-builds-enumeration-ou.md) | Générateur de l'espace de builds (énumération ou échantillonnage seedé) | P0 | 2 | À faire |
| [E08-T04](epics/E08-balance-framework/E08-T04-metriques-standard-d-equilibrage.md) | Métriques standard d'équilibrage | P0 | 2 | À faire |
| [E08-T05](epics/E08-balance-framework/E08-T05-test-de-viabilite-chaque-build-atteint-un-planch.md) | Test de viabilité : chaque build atteint un plancher | P0 | 2 | À faire |
| [E08-T06](epics/E08-balance-framework/E08-T06-test-de-non-dominance-ecart-borne-et-absence-de.md) | Test de non-dominance : écart borné et absence de build strictement supérieur | P0 | 2 | À faire |
| [E08-T07](epics/E08-balance-framework/E08-T07-test-de-niche-chaque-voie-est-la-meilleure-sur-a.md) | Test de niche : chaque voie est la meilleure sur au moins un archétype | P0 | 2 | À faire |
| [E08-T08](epics/E08-balance-framework/E08-T08-tests-d-ablation-impact-marginal-borne-de-chaque.md) | Tests d'ablation : impact marginal borné de chaque talent | P1 | 2 | À faire |
| [E08-T09](epics/E08-balance-framework/E08-T09-budget-de-puissance-des-talents-et-test-de-parit.md) | Budget de puissance des talents et test de parité | P1 | 2 | À faire |
| [E08-T10](epics/E08-balance-framework/E08-T10-rapport-d-equilibrage-versionne-et-diff-de-regre.md) | Rapport d'équilibrage versionné et diff de régression expliqué | P0 | 2 | À faire |
| [E08-T11](epics/E08-balance-framework/E08-T11-tests-de-chemins-chaque-suite-de-decisions-de-sp.md) | Tests de chemins : chaque suite de décisions de spécialisation reste équilibrée | P0 | 2 | À faire |
| [E08-T12](epics/E08-balance-framework/E08-T12-niveaux-de-test-rapide-a-chaque-commit-complet-c.md) | Niveaux de test : rapide à chaque commit, complet chaque nuit | P1 | 2 | À faire |
| [E08-T13](epics/E08-balance-framework/E08-T13-equilibrage-de-la-progression-puissance-par-nive.md) | Équilibrage de la progression (puissance par niveau, contenu par niveau) | P1 | 3 | À faire |
| [E08-T14](epics/E08-balance-framework/E08-T14-outil-de-balayage-de-parametres-reutilisable.md) | Outil de balayage de paramètres réutilisable | P1 | 2 | À faire |
| [E08-T15](epics/E08-balance-framework/E08-T15-profils-humains-a-delais-variables.md) | Profils humains à délais de décision variables | P1 | 2 | À faire |
| [E09-T01](epics/E09-modular-architecture/E09-T01-decision-d-architecture-monolithe-modulaire-a-fr.md) | Décision d'architecture : monolithe modulaire à frontières strictes | P0 | 2 | Terminé |
| [E09-T02](epics/E09-modular-architecture/E09-T02-decoupage-en-modules.md) | Découpage en modules | P0 | 2 | À faire |
| [E09-T03](epics/E09-modular-architecture/E09-T03-frontieres-appliquees-automatiquement.md) | Frontières appliquées automatiquement | P0 | 2 | À faire |
| [E09-T04](epics/E09-modular-architecture/E09-T04-registres-d-extension-effets-actions-de-boss-sor.md) | Registres d'extension (effets, actions de boss, sorts, talents) | P0 | 2 | À faire |
| [E09-T05](epics/E09-modular-architecture/E09-T05-schemas-et-validation-des-contenus-par-domaine.md) | Schémas et validation des contenus par domaine | P0 | 2 | À faire |
| [E09-T06](epics/E09-modular-architecture/E09-T06-contrats-d-evenements-versionnes-entre-modules.md) | Contrats d'événements versionnés entre modules | P2 | 3 | À faire |
| [E09-T07](epics/E09-modular-architecture/E09-T07-injection-de-dependances-catalogues-horloge-rng.md) | Injection de dépendances (catalogues, horloge, RNG, stockage) | P1 | 2 | À faire |
| [E09-T08](epics/E09-modular-architecture/E09-T08-migration-incrementale-sans-casser-les-reference.md) | Migration incrémentale sans casser les références golden | P0 | 2 | À faire |
| [E09-T09](epics/E09-modular-architecture/E09-T09-structure-du-client-par-fonctionnalites-et-kit-d.md) | Structure du client par fonctionnalités et kit d'UI | P1 | 2 | À faire |
| [E09-T10](epics/E09-modular-architecture/E09-T10-preparer-l-extraction-en-paquets-npm-et-l-execut.md) | Préparer l'extraction en paquets npm et l'exécution côté serveur | P2 | 4 | À faire |
| [E09-T11](epics/E09-modular-architecture/E09-T11-documentation-par-module.md) | Documentation par module | P2 | 2 | À faire |
| [E10-T01](epics/E10-persistence-backend/E10-T01-sauvegarde-locale-versionnee-avec-migrations.md) | Sauvegarde locale versionnée avec migrations | P1 | 3 | À faire |
| [E10-T02](epics/E10-persistence-backend/E10-T02-abstraction-de-stockage-par-plateforme.md) | Abstraction de stockage par plateforme | P1 | 3 | À faire |
| [E10-T03](epics/E10-persistence-backend/E10-T03-comptes-et-synchronisation-cloud.md) | Comptes et synchronisation cloud | P2 | 4 | À faire |
| [E10-T04](epics/E10-persistence-backend/E10-T04-serveur-autoritaire-validation-des-combats-par-r.md) | Serveur autoritaire : validation des combats par rejeu | P1 | 4 | À faire |
| [E10-T05](epics/E10-persistence-backend/E10-T05-serveur-tirages-gacha-et-economie.md) | Serveur : tirages gacha et économie | P1 | 4 | À faire |
| [E10-T06](epics/E10-persistence-backend/E10-T06-contrats-d-api-et-versionnage.md) | Contrats d'API et versionnage | P2 | 4 | À faire |
| [E10-T07](epics/E10-persistence-backend/E10-T07-anti-triche-et-limitation-de-debit.md) | Anti-triche et limitation de débit | P2 | 4 | À faire |
| [E10-T08](epics/E10-persistence-backend/E10-T08-telemetrie-et-analytique-respectueuses-de-la-vie.md) | Télémétrie et analytique respectueuses de la vie privée | P2 | 4 | À faire |
| [E10-T09](epics/E10-persistence-backend/E10-T09-conformite-donnees-personnelles-rgpd.md) | Conformité données personnelles (RGPD) | P1 | 4 | À faire |
| [E11-T01](epics/E11-platforms-release/E11-T01-build-android-capacitor-et-test-sur-appareil-d-e.md) | Build Android (Capacitor) et test sur appareil d'entrée de gamme | P1 | 4 | À faire |
| [E11-T02](epics/E11-platforms-release/E11-T02-budgets-de-performance-mesures-automatiquement.md) | Budgets de performance mesurés automatiquement | P1 | 4 | À faire |
| [E11-T03](epics/E11-platforms-release/E11-T03-build-steam-electron-ou-tauri-steamworks-js.md) | Build Steam (Electron ou Tauri + steamworks.js) | P2 | 5 | À faire |
| [E11-T04](epics/E11-platforms-release/E11-T04-publication-google-play-test-ferme-puis-producti.md) | Publication Google Play (test fermé puis production) | P2 | 4 | À faire |
| [E11-T05](epics/E11-platforms-release/E11-T05-localisation-fr-en.md) | Localisation FR/EN | P2 | 4 | À faire |
| [E11-T06](epics/E11-platforms-release/E11-T06-controles-clavier-et-manette-steam.md) | Contrôles clavier et manette (Steam) | P3 | 5 | À faire |
| [E11-T07](epics/E11-platforms-release/E11-T07-point-de-decision-moteur-phaser-ou-unity.md) | Point de décision moteur : Phaser ou Unity | P2 | 4 | À faire |
| [E12-T01](epics/E12-art-audio/E12-T01-direction-artistique-et-kit-d-ui.md) | Direction artistique et kit d'UI | P1 | 3 | À faire |
| [E12-T02](epics/E12-art-audio/E12-T02-portraits-et-sprites-des-personnages-et-des-boss.md) | Portraits et sprites des personnages et des boss | P2 | 3 | À faire |
| [E12-T03](epics/E12-art-audio/E12-T03-effets-visuels-soins-boucliers-poison-telegraphe.md) | Effets visuels : soins, boucliers, poison, télégraphes | P2 | 3 | À faire |
| [E12-T04](epics/E12-art-audio/E12-T04-audio-effets-musique-mixage.md) | Audio : effets, musique, mixage | P2 | 3 | À faire |
| [E12-T05](epics/E12-art-audio/E12-T05-animations-et-juice-sans-nuire-a-la-lisibilite.md) | Animations et « juice » sans nuire à la lisibilité | P3 | 4 | À faire |
| [E13-T01](epics/E13-quality-governance/E13-T01-filet-de-non-regression-golden-equilibrage-donne.md) | Filet de non-régression : golden, équilibrage, données, architecture | P0 | 1 | À faire |
| [E13-T02](epics/E13-quality-governance/E13-T02-journal-des-decisions-persistant-et-regle-de-mis.md) | Journal des décisions persistant et règle de mise à jour | P0 | 2 | Terminé |
| [E13-T03](epics/E13-quality-governance/E13-T03-cadre-de-specifications-epics-tickets-valide-aut.md) | Cadre de spécifications (epics/tickets) validé automatiquement | P0 | 2 | Terminé |
| [E13-T04](epics/E13-quality-governance/E13-T04-depot-git-et-github.md) | Dépôt git et GitHub | P0 | 1 | À faire |
| [E13-T05](epics/E13-quality-governance/E13-T05-banc-de-test-navigateur-pilotage-et-pas-de-temps.md) | Banc de test navigateur (pilotage et pas de temps contrôlé) | P1 | 2 | À faire |
| [E13-T06](epics/E13-quality-governance/E13-T06-couverture-de-code-et-tests-de-mutation-sur-la-s.md) | Couverture de code et tests de mutation sur la simulation | P2 | 3 | À faire |
| [E13-T07](epics/E13-quality-governance/E13-T07-visionneuse-de-replay-et-surcouche-de-debogage.md) | Visionneuse de replay et surcouche de débogage | P2 | 3 | À faire |
| [E13-T08](epics/E13-quality-governance/E13-T08-conventions-de-branches-pr-et-revue.md) | Conventions de branches, PR et revue | P1 | 2 | À faire |
| [E13-T09](epics/E13-quality-governance/E13-T09-integration-continue-workflow-github-actions.md) | Intégration continue (workflow GitHub Actions) | P0 | 2 | À faire |
| [E14-T01](epics/E14-unity-migration/E14-T01-depot-parallele-reprise-des-documents-du-contenu.md) | Dépôt parallèle : reprise des documents, du contenu et des références golden | P0 | 2 | Terminé |
| [E14-T02](epics/E14-unity-migration/E14-T02-environnement-net-unity-hub-editeur-unity-6-lts.md) | Environnement : .NET, Unity Hub, Éditeur Unity 6 LTS avec module Android | P0 | 2 | En cours |
| [E14-T03](epics/E14-unity-migration/E14-T03-choix-du-mcp-officiel-unity-ai-beta-ou-communaut.md) | Choix du MCP : officiel (Unity AI, bêta) ou communautaire libre | P0 | 2 | À faire |
| [E14-T04](epics/E14-unity-migration/E14-T04-squelette-du-c-ur-c-bibliotheque-sans-dependance.md) | Squelette du cœur C# : bibliothèque sans dépendance à Unity | P0 | 2 | Terminé |
| [E14-T05](epics/E14-unity-migration/E14-T05-portage-de-la-simulation-en-c-battle-effets-phas.md) | Portage de la simulation en C# (Battle, effets, phases, événements, RNG, pas fixe) | P0 | 2 | Terminé |
| [E14-T06](epics/E14-unity-migration/E14-T06-conformite-golden-la-simulation-c-reproduit-a-l.md) | Conformité golden : la simulation C# reproduit à l'identique les combats de référence | P0 | 2 | Terminé |
| [E14-T07](epics/E14-unity-migration/E14-T07-chargement-et-validation-du-contenu-json-en-c.md) | Chargement et validation du contenu JSON en C# | P0 | 2 | Terminé |
| [E14-T08](epics/E14-unity-migration/E14-T08-portage-des-tests-d-equilibrage-bots-profils-bor.md) | Portage des tests d'équilibrage : bots, profils, bornes, sensibilité | P0 | 2 | Terminé |
| [E14-T09](epics/E14-unity-migration/E14-T09-garde-fous-d-architecture-en-c.md) | Garde-fous d'architecture en C# | P0 | 2 | Terminé |
| [E14-T10](epics/E14-unity-migration/E14-T10-projet-unity-urp-portrait-input-system-c-ur-en-p.md) | Projet Unity : URP, portrait, Input System, cœur en paquet local | P1 | 2 | À faire |
| [E14-T11](epics/E14-unity-migration/E14-T11-scene-de-combat-3d-camera-eclairage-lecture-de-b.md) | Scène de combat 3D : caméra, éclairage, lecture de Battle, événements vers la présentation | P1 | 2 | À faire |
| [E14-T12](epics/E14-unity-migration/E14-T12-interface-de-combat-unity-mise-en-page-portrait.md) | Interface de combat Unity : mise en page portrait, ciblage en un geste, cartes lisibles | P1 | 2 | En cours |
| [E14-T13](epics/E14-unity-migration/E14-T13-modeles-3d-stylises-user-friendly-golem-et-4-per.md) | Modèles 3D stylisés « user friendly » : Golem et 4 personnages, construits par scripts reproductibles | P1 | 2 | À faire |
| [E14-T14](epics/E14-unity-migration/E14-T14-effets-visuels-et-retours-en-unity.md) | Effets visuels et retours en Unity | P2 | 2 | À faire |
| [E14-T15](epics/E14-unity-migration/E14-T15-build-android-et-test-sur-appareil-d-entree-de-g.md) | Build Android et test sur appareil d'entrée de gamme | P2 | 2 | À faire |
| [E14-T16](epics/E14-unity-migration/E14-T16-integration-continue-specs-dotnet-test-compilati.md) | Intégration continue : specs, dotnet test, compilation Unity en option | P1 | 2 | À faire |
| [E14-T17](epics/E14-unity-migration/E14-T17-parite-fonctionnelle-et-decision-finale-phaser-o.md) | Parité fonctionnelle et décision finale Phaser ou Unity | P1 | 2 | À faire |
<!-- TICKETS:END -->
