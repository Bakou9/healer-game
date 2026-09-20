# Ressources externes (assets) : règles, candidats, outils

Recherche du 2026-09-20 (les prix et conditions changent : revérifier avant tout achat). Décision : D-060.

## Règles (obligatoires)

1. **Licence** : CC0 de préférence ; CC-BY, MIT, OFL-1.1 et Apache-2.0 acceptées (voir `CreditPolicy.AllowedLicenses`). Jamais de licence « non commercial » (CC-BY-NC), ni « usage personnel », ni GPL : le jeu est prévu payant. Autre licence = accord de l'utilisateur d'abord.
2. **Aucun fichier sans crédit** : une ressource importée dans `unity/HealerGame/Assets/Resources/Imported/<Dossier>/` exige une entrée dans `core/content/credits.json` (nom, auteur, licence SPDX, URL https, usage, dossier, « modifié » si retouchée). Un test échoue sinon.
3. **Générique** : l'écran « Crédits » du menu remercie chaque auteur, licence et source comprises. Il se met à jour tout seul avec `credits.json`.
4. **Téléchargement** : uniquement après l'accord de l'utilisateur (nom, source, taille). Jamais de compte créé ni de mot de passe saisi par l'agent.
5. **Conserver la preuve** : copier le fichier de licence du pack à côté des fichiers importés.

## Candidats vérifiés

| Pack | Auteur | Licence | Contenu | Pour nous |
|---|---|---|---|---|
| [KayKit — Adventurers](https://kaylousberg.com/game-assets/characters-adventurers) | Kay Lousberg | **CC0** | 5 personnages low-poly texturés, **riggés et animés** (Chevalier, Barbare, Voleur, Mage… ; palier supplémentaire : 3 de plus), 25+ armes et accessoires, FBX et glTF, **une seule texture dégradé 1024²** (facile à recolorer en palette sombre) | Nos 4 héros : Garde = Chevalier, Mage = Mage, Archère = Voleur à capuche, Soigneuse à choisir (Druide, palier supplémentaire). Style plus « jouet » que sombre, mais la couleur se change en une texture |
| [KayKit — Skeletons](https://kaylousberg.com/game-assets/characters-skeletons) | Kay Lousberg | **CC0** | 4 squelettes riggés et animés, 10+ armes, **90+ animations** (marche, combat, sorts) | Ennemis, sbires, futurs boss ; ambiance plus sombre |
| [Quaternius — Ultimate Monsters Bundle](https://poly.pizza/bundle/Ultimate-Monsters-Bundle-5oyGWAmOB6) | Quaternius | **CC0** | 45 monstres riggés (attaque, mort, marche…) : Goleling (golem de pierre), Démon, Démon bleu, Dragon, Orc, Roi champignon, Fantôme… FBX et glTF | Base pour Golem (Goleling évolué) et Seigneur de Cendre (Démon). Reine des Marais : rien d'équivalent |
| [Quaternius — RPG Character Pack](https://quaternius.com/packs/rpgcharacters.html) | Quaternius | **CC0** | 6 personnages fantasy riggés, animés, texturés | Alternative aux héros KayKit |
| Autres sources | — | selon fichier | [Poly Pizza](https://poly.pizza) (filtre CC0 / CC-BY), itch.io (filtre gratuit + 3D), OpenGameArt, Sketchfab (compte requis pour télécharger), Mixamo (animations gratuites, compte Adobe) | Compléments ponctuels |

**Recommandation** : KayKit Adventurers (recolorés) pour les héros + Quaternius Monsters pour les bases de boss ; nos boss actuels restent le repli.

## Créer des ressources sur mesure : ce que change un MCP

| Voie | Coût | Ce que ça apporte | Limites |
|---|---|---|---|
| **MCP officiel de Unity** | Unity 6 + projet connecté à Unity Cloud ; en Personal, abonnement **Unity AI : essai 14 jours (1 000 crédits) puis 10 $/mois** ; inclus avec Pro/Entreprise | L'agent pilote l'Éditeur : scènes, objets, composants, scripts, console | **Ne crée pas d'art.** Nous faisons déjà cela par scripts d'Éditeur en ligne de commande, sans abonnement |
| MCP communautaire Unity (ex. CoplayDev/unity-mcp) | gratuit, open source | Idem | Idem |
| **Blender + MCP Blender** (ahujasid/blender-mcp) | **0 €** (Blender, l'extension et Python sont gratuits) ; option payante : crédits Rodin | L'agent modélise dans Blender (formes, biseaux, subdivisions, matériaux, armature, animation) puis exporte en FBX/glTF pour Unity | Il faut installer Blender (téléchargement à valider) ; qualité meilleure que nos formes en C#, mais pas celle d'un artiste ; temps de mise au point |
| **Générateurs IA 3D** (Meshy, Tripo, Rodin) | Meshy : gratuit (100 crédits/mois) mais sortie sous **CC BY 4.0** (attribution → nous la mettrions au générique), **20 $/mois** Pro (1 000 crédits, vous possédez les modèles) ; Tripo : gratuit non commercial, payant **≈ 20 $/mois** | Un modèle depuis un texte ou une image, en minutes | Maillage à nettoyer, pas de squelette (à passer par Mixamo), style difficile à garder cohérent d'un modèle à l'autre |
| Générateurs de Unity AI | crédits Unity AI | Modèles, matériaux, sons « de remplacement » | Unity recommande de les traiter en **placeholders** à remplacer avant sortie |
| Artiste (commande) | variable (non vérifié ici) | Qualité et cohérence | Coût et délai les plus élevés |

## Importer une pièce : procédure (D-062)

1. Déposer les fichiers (FBX ou OBJ ; glTF exige un paquet Unity en plus) dans `unity/HealerGame/Assets/Resources/Imported/<Dossier>/` avec le fichier de licence du pack.
2. Ajouter l'entrée dans `core/content/credits.json` (auteur, licence SPDX autorisée, URL https, usage, `folder` = <Dossier>).
3. Dans `core/content/appearance.json`, sur l'entrée voulue (héros, emplacement, palier), ajouter :
   `"model": { "path": "Imported/<Dossier>/<fichier sans extension>", "size": 220, "offset": [0, 0, 30], "euler": [90, 0, 0] }`.
   `size` = plus grande dimension voulue en **centièmes** d'unité du pivot (220 = 2,2 ; le client **normalise** : l'unité du fichier ne compte pas) ; `offset` en centièmes d'unité et `euler` en degrés orientent le modèle par rapport à la main. Tout est en entiers (règle de toutes les données du jeu).
4. Aujourd'hui **seul l'emplacement `weapon`** peut être remplacé par un modèle importé (rattaché au pivot d'arme : il suit l'animation) ; l'armure et les corps complets viendront avec un squelette commun (voir docs/ART_3D.md).
5. Si le fichier est introuvable, le client garde la version dessinée par le code (avertissement dans le journal) : jamais de héros sans arme.
6. Vérifier : les tests (`npm run check`) refusent un modèle « Imported/… » sans entrée de crédit, et un dossier importé sans entrée ; capture d'écran en jeu (`-healer-shots` avec `-healer-equip` pour forcer le palier).

Chaîne validée le 2026-09-20 avec une lame OBJ créée par l'équipe (retirée ensuite) : chargement, normalisation de la taille, rattachement au pivot, animation.
