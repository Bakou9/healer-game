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
