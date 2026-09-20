# Registre des revues de tickets

Une ligne par ticket **terminé dans CE dépôt** (version Unity). Ce registre
matérialise le préambule systématique de `CLAUDE.md` (décisions D-016 et D-017) :

- **Specs remises en question ?** Ce que j'ai contesté dans la spec du ticket
  ou de ses voisins parce qu'une alternative apporte au gameplay, et ce qui a
  été amendé (avec renvoi à la décision).
- **Équilibrage.** La réponse à « est-ce que cela peut changer l'équilibre ? »,
  avec les mesures. Si le jeu ne paraît plus équilibré, le ticket n'est **pas**
  terminé tant que l'utilisateur n'a pas validé.
- **Validation.** `Non requise` (aucun doute), `À valider` (information ou
  hypothèse à confirmer, non bloquante) ou `Validé` (accord de l'utilisateur).

`tools/specs/specs.test.ts` échoue si un ticket « Terminé » n'a pas de ligne ici.

L'historique des revues de la version Phaser est conservé dans
[`legacy/REVUES.phaser.md`](legacy/REVUES.phaser.md) : il documente ce qui a été
mesuré et décidé, et reste valable comme référence (les mesures d'équilibrage
doivent être **retrouvées à l'identique** par le portage, ticket E14-T08).

| Ticket | Specs remises en question | Équilibrage (question et verdict) | Validation |
|---|---|---|---|
| E04-T01 | Reprise du document d'audit UX de la version Phaser ; conservé : principes UX valables quel que soit le moteur | Sans impact (document) | Non requise |
| E09-T01 | Reprise de l'ADR d'architecture ; complétée par `docs/ARCHITECTURE_UNITY.md` (cœur C# pur partagé, Unity en couche de présentation) | Sans impact (document) | Non requise |
| E13-T02 | Non : le journal des décisions et sa règle sont repris tels quels | Sans impact | Non requise |
| E13-T03 | Non : l'outillage de cohérence des specs est repris tel quel (`tools/specs`) | Sans impact | Non requise |
| E14-T01 | Non : le dépôt parallèle est la demande de l'utilisateur (D-027). Ajustement : les tickets réalisés en Phaser sont remis à « À faire » ici, pour qu'un « Terminé » signifie toujours « fait, testé et revu dans ce dépôt » | Aucun impact : contenu et références golden repris à l'identique, servant de spécification de conformité | Non requise |
| E14-T04 | Non : le squelette (bibliothèque netstandard2.1 + tests) suit `docs/ARCHITECTURE_UNITY.md`. Ajustement : le cœur ne lit aucun fichier (le contenu JSON lui est fourni sous forme de texte) pour rester exécutable côté serveur | Aucun impact | Non requise |
| E14-T05 | Ajustement : les sorts sont fournis par la rencontre (injection de dépendances) au lieu d'un import global comme en TypeScript ; le journal texte `getLog` n'est pas porté. Signalés dans le ticket | Question posée : un portage peut changer les règles. **Mesuré** : les 7 combats de référence (plus de 2 000 lignes d'événements) sont reproduits **ligne pour ligne**. **Verdict : équilibre préservé par construction** | Non requise |
| E14-T06 | Ajustement : pas de script de régénération des références tant qu'aucune régression voulue n'existe (les références ne se modifient que sur décision de l'utilisateur) | Vérifié par mutation : passer l'intervalle d'attaque de 1600 à 1500 ms fait échouer les 7 scénarios dès la ligne 1, avec le message explicatif ; règle restaurée ensuite. **Verdict : le filet détecte bien une dérive** | Non requise |
| E14-T07 | Non : mêmes règles d'intégrité que la version Phaser | Aucun impact : mêmes fichiers JSON, mêmes vérifications, y compris « entiers ronds sauf multiplicateurs et ratios » | Non requise |
| E14-T08 | Non : mêmes profils et mêmes bornes | Question posée et **mesurée** sur 100 combats par profil : attentif 100 % de victoires, PV minimum moyen ≈ 28 %, lent ≈ 16 %, sans purge ≈ 17 %, passif et spam 0 % ; identique aux mesures de la version Phaser (`docs/EQUILIBRAGE.md` §5). **Verdict : équilibré, inchangé** | Non requise |
| E14-T09 | Ajustement : les bibliothèques sont aussi des paquets Unity (`asmdef` en `noEngineReferences`, `package.json`), et les sorties de dotnet sont déplacées dans `.build/` pour que Unity ne compile jamais de fichiers générés (règle testée). Sens des dépendances testé : le cœur ne dépend jamais de l'interface | Aucun impact sur les règles : mêmes 7 combats de référence, mêmes bornes | Non requise |
