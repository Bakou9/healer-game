# Registre des revues de tickets

Une ligne par ticket **terminé**. Ce registre matérialise le préambule
systématique de `CLAUDE.md` (décisions D-016 et D-017) :

- **Specs remises en question ?** Ce que j'ai contesté dans la spec du ticket
  ou de ses voisins parce qu'une alternative apporte au gameplay, et ce qui a
  été amendé (avec renvoi à la décision).
- **Équilibrage.** La réponse à « est-ce que cela peut changer l'équilibre ? »,
  avec les mesures. Si le jeu ne paraît plus équilibré, le ticket n'est **pas**
  terminé tant que l'utilisateur n'a pas validé.
- **Validation.** `Non requise` (aucun doute), `À valider` (information ou
  hypothèse à confirmer, non bloquante) ou `Validé` (accord de l'utilisateur).

`src/testing/specs.test.ts` échoue si un ticket « Terminé » n'a pas de ligne ici.

| Ticket | Specs remises en question | Équilibrage (question et verdict) | Validation |
|---|---|---|---|
| E01-T01 | Antérieur à la règle | Fondation, sans impact | Non requise |
| E01-T02 | Antérieur à la règle | Pas de changement de règles | Non requise |
| E01-T03 | Antérieur à la règle | Pas de changement de règles | Non requise |
| E01-T04 | Antérieur à la règle | Pas de changement de règles | Non requise |
| E01-T05 | Antérieur à la règle | Mesuré : Purge devenue nécessaire (EQUILIBRAGE.md §5) | Non requise |
| E01-T06 | Antérieur à la règle | Mesuré : phase 2 réglée sur 150 combats | Non requise |
| E01-T07 | Antérieur à la règle | Valeurs de base des sorts, bornes tenues | Non requise |
| E03-T02 | Antérieur à la règle | Référence actuelle de l'équilibrage | Non requise |
| E04-T01 | Antérieur à la règle | Sans impact (document) | Non requise |
| E08-T01 | Antérieur à la règle | Base de l'équilibrage | Non requise |
| E09-T01 | Antérieur à la règle | Sans impact (document) | Non requise |
| E13-T01 | Antérieur à la règle | Filet de tests | Non requise |
| E13-T02 | Antérieur à la règle | Sans impact | Non requise |
| E13-T03 | Antérieur à la règle | Sans impact | Non requise |
| E04-T02 | Non : la spec (rendu net + jetons) reste la bonne approche. Ajustement : « zones sûres » réduit à des marges fixes dans la mise en page, la vérification réelle étant renvoyée à E11-T01 | Aucun impact : simulation inchangée, références golden et bornes identiques | Non requise |
| E04-T03 | Non : la disposition de `docs/UX.md` §4 est conservée. Ajustement : journal limité à 3 lignes dans un bandeau | Aucun impact sur les règles. Le rythme d'action est traité en E04-T04 | Non requise |
| E04-T04 | **Oui** : « geste rapide pour le soin par défaut » retiré (conflit avec la sélection, gaspille du mana, rapproche du soin automatique, contraire au pilier 1) ; remplacé par une sélection persistante. Décision D-019, ticket amendé, `docs/UX.md` amendé | Question posée : ce ticket ne change pas les règles mais accélère l'action d'un humain. **Mesuré** (200 combats par délai) : de 100 à 500 ms de délai de décision, 100 % de victoires et PV minimum ≈ 28 % (plateau), donc un joueur plus rapide ne rend pas le jeu plus facile que le modèle actuel. **Verdict : équilibré.** Deux points d'information : (1) le délai humain réel reste une hypothèse (à confirmer au playtest, E04-T14) ; (2) le bot à pas fixe donne des résultats non monotones (800 ms pire que 1000 ms) : ticket E08-T15 créé | À valider |
| E04-T05 | Non : la spec reste bonne. Ajustements : statuts en pastilles de texte (icônes en E12), rôle par libellé (formes en E04-T12/E12) | Aucun impact sur les règles ; renforce l'équité (états dangereux plus visibles) | Non requise |
| E13-T04 | Non : dépôt GitHub fourni par l'utilisateur, comme prévu (visibilité non vérifiée). Ajustement : le workflow d'intégration continue est traité à part (E13-T09) | Aucun impact | Non requise |
