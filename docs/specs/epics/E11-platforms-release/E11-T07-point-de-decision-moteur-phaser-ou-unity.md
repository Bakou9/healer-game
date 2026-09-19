---
id: E11-T07
epic: E11
titre: Point de décision moteur : Phaser ou Unity
type: Design
priorité: P2
phase: 4
statut: À faire
taille: M
dépendances: E11-T01, E11-T02
---

# E11-T07 — Point de décision moteur : Phaser ou Unity

## Contexte
Décision D-026 (proposition). Unity dispose d'un serveur MCP officiel et d'un plugin officiel pour Claude Code (contrôle de l'éditeur : scènes, objets, composants, console). Cela rend Unity praticable pour un agent, mais changer de moteur coûte cher : le projet a une simulation TypeScript pure, 116 tests, une chaîne Capacitor et une validation serveur envisagée en TypeScript. On ne bascule donc pas sur une intuition : on décide à une porte, avec des critères posés à l'avance.

## Critères d'acceptation
- [ ] Le test sur appareil Android d'entrée de gamme (E11-T01) et les budgets de performance (E11-T02) sont mesurés
- [ ] Critères de bascule écrits avant la mesure (exemples : moins de 45 images par seconde sur l'appareil cible malgré les optimisations ; besoin d'animation squelettique, de shaders ou de 3D que Phaser ne couvre pas raisonnablement ; coût de production des visuels)
- [ ] Comparaison chiffrée : coût de portage, gains, risques, effet sur l'architecture (E09) et sur la validation serveur (E10-T04)
- [ ] Décision consignée dans `docs/DECISIONS.md`, validée avec l'utilisateur ; si bascule, plan de portage de la simulation en C# avec les références golden comme spécification de conformité

## Tests automatiques exigés
Si portage : les mêmes fichiers golden (`src/testing/golden/*.txt`) doivent être reproduits à l'identique par la simulation portée.

## Impact équilibrage
Aucun si les golden sont reproduits à l'identique (l'équilibre est alors préservé par construction). Toute divergence est une régression à expliquer.
