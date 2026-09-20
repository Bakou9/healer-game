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

## Raccourcis clavier (PC)

| Touche | Action |
|---|---|
| `1` `2` `3` `4` (ou pavé numérique) | Cibler l'allié n° 1 à 4 |
| `Tab` / `Maj+Tab` | Allié vivant suivant / précédent |
| `Q` `W` `E` `R` (`A` `Z` `E` `R` en AZERTY) | Lancer le sort n° 1 à 4 (Soin, Soin de zone, Bouclier, Purge) |
| `Espace` ou `Entrée` | Jouer ; pause / reprise ; rejouer en fin de combat |
| `Échap` ou `P` | Pause |
| `M` | Couper / rétablir le son |
| `F8` | **Noter un retour de playtest** : capture d'écran, pause, saisie d'une remarque (Entrée pour enregistrer, Échap pour annuler). Tout va dans `playtest/notes.md` (dossier des données du jeu : `%USERPROFILE%\AppData\LocalLow\Healer\Healer Game\playtest\`), avec l'état du combat |

Les touches sont physiques (même position sur QWERTY et AZERTY) ; les pastilles sur les cartes affichent la lettre de votre disposition. Elles n'apparaissent pas sur mobile.

## Tests automatisés

| Commande | Ce qu'elle vérifie | Durée |
|---|---|---|
| `npm run check` | cohérence des specs + 701 tests du cœur (règles, golden, équilibrage de chaque boss et de chaque choix, stratégies limitées, enrage, mécaniques de combat, progression, atelier, sauvegarde, navigation, entrées, mise en page, sons, musique, animations, réglages) | ~15 s |
| `npm run e2e` | le vrai jeu Windows, gestes **injectés** (D-053) : menu, choix du niveau, niveau verrouillé, combat, pause et fiche des sorts, victoire, atelier (achats, talents), maintien des sorts, enrage, réglages, note F8, sauvegarde sur disque, relance. 10 scénarios en parallèle | ~1 min |

Vous pouvez continuer à utiliser votre souris et votre clavier pendant `npm run e2e` : les fenêtres du jeu sont réduites et silencieuses. Pour vérifier la vraie souris de Windows (avant une livraison), `powershell -File tools/unity-e2e.ps1 -RealInput -Scenario A` : ne touchez à rien pendant ce test.
