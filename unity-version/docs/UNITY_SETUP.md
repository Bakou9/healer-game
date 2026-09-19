# Installation de Unity, .NET et connexion du MCP

Ticket : E14-T02 (environnement) et E14-T03 (choix du MCP). Ce fichier est la
trace de ce qui est installé et de ce que **seul l'utilisateur** peut faire.

## État de l'environnement (mis à jour à chaque étape)

| Élément | État | Comment |
|---|---|---|
| SDK .NET 8 (8.0.425) | installé | `winget install Microsoft.DotNet.SDK.8` (autorisé par l'utilisateur) |
| Unity Hub 3.21.3 | installé (paquet MSIX) | `winget install Unity.UnityHub` (autorisé par l'utilisateur) |
| Éditeur Unity 6.3 LTS (6000.3.24f1) + module Android | **téléchargement en cours** (Hub en ligne de commande, tâche détachée, ~4 Mo/s) | plusieurs Go, plusieurs dizaines de minutes |
| Licence Unity | **active** (Unity Personal) | activée par l'utilisateur ; voir « Piège MSIX » plus bas |
| Compte Unity et essai | compte créé, essai démarré le 2026-09-19 ; **type et date de fin à préciser** (D-032) | à noter dans un calendrier : un essai peut devenir payant |
| Projet Unity (`unity/HealerGame`) | **créé** (Unity 6000.3.24f1, mode automatique) | à ouvrir avec le Hub |
| MCP | à choisir (E14-T03) | voir plus bas |

Règle de sécurité : l'agent **ne saisit jamais** d'identifiant, de mot de passe
ni de clé. La connexion au compte Unity et l'activation de la licence sont faites
par l'utilisateur dans le Hub.

## Ce que l'utilisateur doit faire

1. **Ouvrir Unity Hub** et **se connecter** à son compte Unity (gratuit). Accepter
   la licence *Personal* si demandé.
2. Vérifier qu'un **Éditeur Unity 6 (6000.x LTS)** avec le module **Android Build
   Support** (SDK, NDK, OpenJDK) est présent ; sinon, l'installer depuis le Hub
   (onglet *Installs*). Compter plusieurs Go.
3. **Créer le projet** : *Projects → New project* → modèle **Universal 3D (URP)**,
   nom `HealerGame`, emplacement
   `%USERPROFILE%\Documents\healer-game-unity\unity\` (Unity crée le dossier
   `HealerGame` ; il doit être vide au départ).
4. Laisser l'Éditeur ouvert sur le projet : le MCP a besoin d'un Éditeur lancé.

## Choix du MCP (E14-T03) : deux options

### Option A — MCP officiel de Unity (Unity AI)
- Exige **Unity 6 ou plus**, un projet **connecté à Unity Cloud**, et un **essai
  ou un abonnement aux outils IA de Unity (bêta)** : possible coût.
- Paquet : *AI Assistant* (`com.unity.ai.assistant`, version ≥ 2.11).
- Réglage : *Edit → Project Settings → AI → Unity MCP* : vérifier que le pont
  (*Unity Bridge*) est démarré (voyant vert), ouvrir *Integrations*, choisir
  *Claude Code* puis *Configure*. À la première connexion, approuver le client
  (*Accept*).
- Configuration manuelle : serveur pointant vers `%USERPROFILE%\.unity\relay\relay_win.exe`
  avec l'argument `--mcp`.
- Capacités annoncées : hiérarchie de scène, création/modification/suppression
  d'objets, lecture/écriture de composants, création/édition de scripts C#,
  console, informations de build.

### Option B — MCP communautaire libre (ex. `mcp-unity`)
- Sans abonnement ; installé comme paquet dans Unity, avec un petit serveur Node.
- À vérifier avant adoption : licence, maintenance active, sécurité, capacités
  réelles (scènes, préfabriqués, scripts, console, tests).
- Le README du dépôt fait foi pour l'installation exacte.

### Comment trancher
La décision engage un coût éventuel et un accès étendu à votre poste : elle se
prend **avec l'utilisateur** (décision D-028). Critères : coût, capacités,
maturité, sécurité, maintenance. Test de bout en bout à réussir avant de valider :
« lire la hiérarchie d'une scène et y créer un objet ».

## Sécurité du MCP

Le MCP permet à un agent d'agir dans l'Éditeur (donc sur des fichiers du projet).
On le configure au niveau **du projet**, on relit ce qu'il autorise, on ne
l'expose jamais en dehors de la machine, et on garde tout sous git pour pouvoir
annuler.

## Vérifications

```powershell
dotnet --list-sdks          # 8.0.x attendu
```

Le dépôt compte sur `npm run check` : il **échoue** si des tests C# existent alors
que `dotnet` manque, au lieu de « passer » en silence.

## Piège MSIX : la licence activée dans le Hub n'est pas vue par l'Éditeur en ligne de commande

Le Hub installé par winget est un paquet **MSIX** : il enregistre la licence dans un dossier
virtualisé (`%LOCALAPPDATA%\Packages\UnityTechnologies.UnityHub_…\LocalCache\Local\Unity\licenses`),
que l'Éditeur lancé **en dehors du Hub** (mode automatique, MCP, CI locale) ne lit pas : il répond
« No valid Unity Editor license found ». Contournement appliqué le 2026-09-19 : copier
`UnityEntitlementLicense.xml` dans `%LOCALAPPDATA%\Unity\licenses\` (même machine, même utilisateur ;
réversible en supprimant la copie). Vérification : `Unity.Licensing.Client.exe --showEntitlements`
doit afficher « Unity Personal ». À refaire si la licence est renouvelée.

## Piège Windows Defender : « EPERM: operation not permitted, rename » à la résolution des paquets

Constat du 2026-09-19 : à l'ouverture du projet, Unity résout ses paquets mais échoue à **renommer** les
dossiers extraits des paquets téléchargés du registre (`Library\PackageCache\.tmp-…\package` vers
`com.unity.…@hash`) avec « EPERM: operation not permitted » ; les paquets intégrés (`com.unity.modules.*`)
passent. La protection en temps réel de Windows Defender (et l'indexeur de recherche) tient les fichiers
fraîchement écrits au moment du renommage. Un déplacement manuel du dossier fonctionne, mais Unity retente
et rebute à chaque ouverture : ce n'est pas tenable pour URP, Input System, etc.

**Correctif recommandé par Unity, à faire par l'utilisateur** (réglage de sécurité : jamais fait par l'agent) :
Sécurité Windows → Protection contre les virus et menaces → Gérer les paramètres → Exclusions → Ajouter une
exclusion → Dossier → `%USERPROFILE%\Documents\healer-game\unity-version\unity\HealerGame\Library`
(le dossier `Library` est entièrement régénérable ; on n'exclut PAS le code source).
Vérification : ouvrir le projet en mode automatique ne doit plus afficher « An error occurred while resolving packages ».
