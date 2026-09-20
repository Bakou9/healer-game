# Questions en attente pour l'utilisateur

Fichier tenu pendant l'absence de l'utilisateur (2026-09-19) : chaque question ou décision à prendre y est notée,
avec ma recommandation. À passer en revue ensemble au retour, puis à consigner dans `DECISIONS.md`.

1. **Windows Defender** : accepter l'exclusion du dossier `Library` pour permettre URP / Input System ?
   Recommandation : oui (dossier de cache régénérable). En attendant, le jeu fonctionne avec le rendu intégré.
2. **MCP** (D-028) : officiel (abonnement éventuel) ou communautaire libre ? À trancher avant toute utilisation.
3. **Essai Unity** (D-032) : quel type d'essai, date de fin, renouvellement automatique ?
4. **Style visuel 3D** (D-030) : « figurine » low-poly arrondi validé, ou autre direction ?
5. **Interface** : l'interface actuelle est dessinée en IMGUI (module intégré, zéro paquet). Elle est fonctionnelle mais
   pas la plus élégante. Après la décision Defender (question 1), la refaire en UI Toolkit/uGUI ? Recommandation : oui, plus tard.
6. **Sons et musique** : j'ajoute des effets sonores générés par code (bips, tintements). Voulez-vous de vrais sons/musique (fichiers à fournir ou à choisir) ?
7. **Ressenti** : premier vrai test de jeu à faire ensemble (fun, lisibilité, difficulté) — le plus utile pour la suite.
8. **Android** : le module est installé ; on tente un APK de test sur un vrai téléphone ? (nécessite le téléphone en mode développeur)

- **Android** : `BuildAndroid` est écrit mais non exécuté. Installer le module Android (SDK/NDK/JDK, plusieurs Go) via Unity Hub pour produire un APK de test téléphone ? À valider avec vous.


## Jalon 1 (campagne) — à trancher ensemble

1. **À quoi sert l'or ?** Il s'accumule mais rien ne s'achète encore. Jalon 2 : équipement et talents ? Cela change le rythme de gain (aujourd'hui 100 à 220 or par première victoire).
2. **Pas de limite de temps** : un combat où l'équipe ne peut pas finir le boss dure indéfiniment (constaté avec une équipe réduite à 2 personnages). Faut-il une « rage » du boss après ~2 minutes ? À décider avant d'introduire des équipes réduites (gacha) : c'est une règle de combat qui changerait les golden.
3. **Durée des combats** : 75 à 105 s (borne du projet : 60 à 120 s). Trop long pour du mobile en trajet ?
4. **Critères d'étoiles** : victoire / sans K.O. / peu de dégâts encaissés. Le 3ᵉ critère dépend du bon usage des boucliers et des purges. Vous convient-il, ou préférez-vous un critère de vitesse ou de mana ?
5. **Direction artistique des boss 2 et 3** (modèles procéduraux) : à juger à l'œil ; la Reine des Marais est la moins aboutie (bras en bâtons).
6. **Niveaux à venir** : combien pour une première version vendable (10 ? 30 ?), et faut-il des niveaux « héroïques » (mêmes boss, plus durs) ?
7. **Prix cible du jeu premium** (Steam, mobile) : influe sur la quantité de contenu attendue.


## Jalon 2 (atelier) — à trancher ensemble

1. **Les talents sont-ils assez excitants ?** Les gains sont modestes (+0,04 à +0,22 de PV minimum) car le mana est la contrainte du jeu : tout ce qui touche au mana pèse lourd, tout ce qui touche à la quantité de soin pèse peu. Faut-il des talents plus « exotiques » (ex. Soin qui se répercute sur un second allié, bouclier qui renvoie des dégâts) ? Cela demande de nouvelles mécaniques de combat, donc de nouveaux golden.
2. **Changer de talent est gratuit** : vous convient-il, ou préférez-vous un coût de « réinitialisation » ?
3. **Le jeu devient nettement plus facile avec tout au maximum** (PV minimum 0,63 à 0,74 contre 0,28 à 0,35). Voulez-vous des niveaux « héroïques » pour garder de la tension aux joueurs avancés (jalon suivant) ?
4. **Le grind** : ~20 parcours complets pour tout acheter (~1 h 30 à 2 h avec seulement 3 niveaux). Acceptable pour une première version, mais il faudra plus de niveaux pour que ce soit un plaisir et non une corvée.
5. **Talents uniquement pour le soigneur** : les autres personnages n'ont que de l'équipement. À élargir quand on introduira d'autres soigneurs ou des classes.
