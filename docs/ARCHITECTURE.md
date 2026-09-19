# Architecture modulaire

Statut : **décision E09-T01, cible de migration (E09-T02 à T11)**. Le code actuel
est plus simple (`src/sim`, `src/scenes`, `src/data`) et sera migré par étapes,
avec les références golden comme filet (E09-T08).

## Décision (ADR-001) : monolithe modulaire à frontières strictes

**Problème.** Le projet va devenir large : combat, spécialisation, équipe,
gacha, sauvegarde, serveur, deux plateformes. Sans structure, tout finit par
dépendre de tout.

**Choix.** Un seul dépôt et un seul paquet, organisés en **modules** avec une
API publique et des règles de dépendance **vérifiées automatiquement**. On
n'éclate en paquets npm distincts (espaces de travail) que quand le serveur ou
Steam l'exigent réellement (E09-T10).

**Pourquoi pas des paquets npm dès maintenant ?** Chaque paquet ajoute de la
configuration (Vite, Capacitor, tests, versions) sans bénéfice tant qu'une seule
équipe et une seule application consomment le code. Les frontières testées
donnent 90 % du bénéfice pour 10 % du coût, et la migration ultérieure vers des
paquets est mécanique si les frontières sont déjà propres.

## Couches et modules

```
                 app/  (racine de composition : assemble tout, seul endroit autorisé)
                   │
   ┌───────────────┼──────────────────────────────┐
 client         balance                        (serveur, plus tard)
   │  \            │                                 │
   │   platform    │                                 │
   │      │        │                                 │
 progression   economy                               │
   │   \        /                                    │
   │    combat                                       │
   │      │                                          │
   └───── content ── core ───────────────────────────┘
```

| Module | Rôle | Peut dépendre de |
|---|---|---|
| `core` | RNG seedé, pas fixe, utilitaires purs, types communs | — |
| `content` | Schémas, validation, chargement des données par domaine (`data/`) | `core` |
| `combat` | Moteur de combat : Battle, effets, boss, phases, événements, replay | `core`, `content` |
| `progression` | Spécialisation, talents, niveaux, progression des personnages | `core`, `content`, `combat` (types) |
| `economy` | Devises, tirages, récompenses | `core`, `content` |
| `balance` | Bots, métriques, générateur de builds, rapports (outil de dev) | `core`, `content`, `combat`, `progression` |
| `platform` | Adaptateurs : stockage, services de plateforme | `core` |
| `client` | Scènes Phaser, kit d'UI, audio, saisie | tous sauf `balance` |
| `app` | Point d'entrée : assemble modules, registres, injection | tous |

### Règles de dépendance (vérifiées par test, E09-T03)

1. Les dépendances vont **vers le bas** du schéma ; jamais de cycle.
2. `core`, `content`, `combat`, `progression`, `economy` : **aucun** import de
   `client`, de `platform`, de Phaser, du DOM ni de l'horloge réelle.
3. On importe un module **uniquement par son `index.ts`** (API publique).
   Aucun import profond dans les fichiers d'un autre module.
4. Rien n'importe `client` ; seul `app` assemble les modules.
5. Les tests d'un module ne dépendent que de ce module et de ses dépendances autorisées.
6. `balance` ne modifie jamais les règles : il observe (événements, métriques).

## Extension par registres (Strategy, E09-T04)

Ajouter un **type d'effet**, d'**action de boss**, de **sort** ou de **talent**
ne doit pas modifier le moteur :

- chaque domaine expose un **registre** `type → gestionnaire` ;
- les gestionnaires sont **enregistrés explicitement** à l'initialisation par
  `app` (pas d'état global caché) ;
- un type inconnu échoue avec un message clair au chargement du contenu, pas au
  milieu d'un combat ;
- chaque gestionnaire vient avec ses données, ses événements et ses tests.

## Contenu (E09-T05)

`content` possède un **schéma par type de contenu** (personnage, sort, effet,
boss, talent, bannière…) : validation à la construction avec le chemin exact de
l'erreur. Les tests d'intégrité actuels (`data.test.ts`, `data-effects.test.ts`)
sont absorbés par les schémas.

## Injection de dépendances (E09-T07)

`Battle` reçoit ses catalogues, son RNG et son horloge par sa rencontre : plus
d'import global de JSON. Idem pour le stockage (`platform`). Cela permet des
tests avec des catalogues minimaux et l'exécution du même code côté serveur.

## Événements (E09-T06)

Chaque module émet ses événements typés ; les autres s'abonnent. Les formats
d'événements et de replay portent un **numéro de version** ; tout changement de
format est une décision, testée par des contrats de compatibilité.

## Correspondance avec le code actuel

| Aujourd'hui | Cible |
|---|---|
| `src/sim/rng.ts`, `fixedStep.ts` | `modules/core` |
| `src/data/*.json` + validation dans `data*.test.ts` | `modules/content` |
| `src/sim/Battle.ts`, `types.ts`, `events.ts`, `encounter.ts` | `modules/combat` (+ types de contenu dans `content`) |
| `src/sim/referenceHealerBot.ts`, `src/testing/*` (scénarios, golden, équilibrage) | `modules/balance` et `src/testing` |
| `src/scenes/*`, `src/ui/*` | `modules/client` (scènes par fonctionnalité + kit d'UI) |
| `src/main.ts` | `app/` (racine de composition) |

## Plan de migration (E09-T08)

1. Créer `modules/*` avec des `index.ts` vides et le **test de frontières**
   (d'abord en mode « avertissement » : liste des violations existantes).
2. Déplacer `core` (RNG, pas fixe) — aucune logique modifiée ; goldens inchangés.
3. Introduire `content` : schémas, chargeur ; `Battle` reçoit ses catalogues.
4. Déplacer `combat` puis `balance` ; introduire les registres (E01-T08).
5. Réorganiser `client` par fonctionnalités (E09-T09) avec l'UX (E04).
6. Passer le test de frontières en mode strict.

À chaque étape : `npm run check` vert, **goldens inchangés** (sinon régression
à expliquer), un commit par étape.

## Ce qui ne change pas

Simulation pure et déterministe ; contenu en données ; événements ; pas fixe ;
protocole de non-régression ; valeurs tronquées ; équilibrage mesuré.
