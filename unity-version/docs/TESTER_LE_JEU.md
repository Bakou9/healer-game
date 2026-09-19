# Tester le jeu Unity

## Le plus simple : lancer l'exécutable Windows

1. Construire (une fois, ou après un changement) : `powershell -File tools/unity-cycle.ps1`
   (compile, contrôle les modèles 3D, construit `unity/HealerGame/Builds/Windows/HealerGame.exe`, lance une démo automatique).
2. Double-cliquer sur **`unity/HealerGame/Builds/Windows/HealerGame.exe`**.

Le dossier `Builds/` n'est pas versionné (il est régénéré par la commande ci-dessus).

## Comment jouer

- Vous êtes la **soigneuse** (carte « Vous », la robe verte). Les trois autres attaquent seuls.
- **Touchez un allié** (sa carte) pour le sélectionner (bordure jaune) ; **touchez un sort** pour le lancer sur lui.
  La sélection reste : re-soigner la même cible = un seul geste. Retoucher la carte annule.
- **Soin de zone** se lance sans cible. **Bouclier** : à poser AVANT l'attaque annoncée (« ATTAQUE DE ZONE dans… »).
- **Purge** retire le poison (phase 2, « Fureur », à mi-vie du Golem).
- Le **mana** limite le nombre de sorts : ne pas gaspiller.
- Victoire : le Golem tombe à 0. Défaite : toute l'équipe à 0. « Recommencer » relance un combat.

## Dans l'Éditeur Unity

Ouvrir `unity/HealerGame` avec Unity Hub (Unity 6000.3.24f1), ouvrir la scène `Assets/Scenes/Battle.unity`, appuyer sur Play.
Menu **Healer** : synchroniser le contenu, configurer le projet, créer la scène, vérifier les modèles, tout reconstruire.

## Vérifications automatiques

- `npm run check` : specs + tests du cœur (108 tests, dont les 7 combats de référence).
- `tools/unity-cycle.ps1` : build + captures d'écran (`%TEMP%\\healer-captures`).
- `tools/unity-clicktest.ps1` : vrais clics dans la fenêtre du jeu (déplace la souris : ne pas y toucher pendant le test).
