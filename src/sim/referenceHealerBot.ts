import type { Battle } from "./Battle";

/**
 * Bot de soin "raisonnable" utilisé pour valider l'équilibrage : il soigne la
 * cible la plus abîmée, bascule en soin de zone si plusieurs alliés souffrent,
 * et pose un bouclier quand la grosse attaque du boss est télégraphiée.
 *
 * Ce n'est pas une IA optimale, seulement une référence pour vérifier qu'un
 * jeu "raisonnablement bien joué" peut gagner. Un vrai joueur fera mieux (ou moins bien).
 */
export function runWithReferenceHealer(battle: Battle, durationMs: number, stepMs = 100): void {
  for (let t = battle.getClock(); t < durationMs && battle.getResult() === "ongoing"; t += stepMs) {
    const allies = battle.getAllies().filter((a) => a.alive);
    const healer = allies.find((a) => a.role === "healer");

    if (healer) {
      const lowest = [...allies].sort((a, b) => a.hp / a.maxHp - b.hp / b.maxHp)[0];
      const hurtCount = allies.filter((a) => a.hp / a.maxHp < 0.75).length;
      const telegraph = battle.getTelegraph();

      if (telegraph?.type === "bigAttack" && battle.canUseSkillNow("healer", "shield")) {
        battle.issueCommand({ timeMs: t, skillId: "shield", targetId: lowest.id });
      } else if (hurtCount >= 2 && battle.canUseSkillNow("healer", "heal_aoe")) {
        battle.issueCommand({ timeMs: t, skillId: "heal_aoe" });
      } else if (lowest.hp / lowest.maxHp < 0.85 && battle.canUseSkillNow("healer", "heal_single")) {
        battle.issueCommand({ timeMs: t, skillId: "heal_single", targetId: lowest.id });
      }
    }

    battle.step(stepMs);
  }
}
