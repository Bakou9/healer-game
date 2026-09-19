import type { Battle } from "./Battle";

export interface ReferenceHealerOptions {
  /** Pas de simulation. */
  stepMs?: number;
  /**
   * Délai entre deux décisions du bot. Un humain ne réagit pas à chaque
   * instant : ~500 ms représente un joueur attentif (voir docs/EQUILIBRAGE.md).
   */
  decisionEveryMs?: number;
  /** false = joueur qui ignore le poison (sert à vérifier que la Purge compte vraiment). */
  purge?: boolean;
}

/**
 * Bot de soin "raisonnable" utilisé pour valider l'équilibrage : il purge les
 * alliés empoisonnés, soigne la cible la plus abîmée, bascule en soin de zone
 * si plusieurs alliés souffrent, et pose un bouclier quand la grosse attaque
 * du boss est télégraphiée.
 *
 * Ce n'est pas une IA optimale, seulement une référence pour vérifier qu'un
 * jeu "raisonnablement bien joué" peut gagner. Un vrai joueur fera mieux (ou moins bien).
 */
export function runWithReferenceHealer(
  battle: Battle,
  durationMs: number,
  { stepMs = 100, decisionEveryMs = 500, purge = true }: ReferenceHealerOptions = {},
): void {
  let sinceDecisionMs = decisionEveryMs; // décide dès le premier pas
  for (let t = battle.getClock(); t < durationMs && battle.getResult() === "ongoing"; t += stepMs) {
    if (sinceDecisionMs >= decisionEveryMs) {
      sinceDecisionMs = 0;
      decide(battle, t, purge);
    }
    sinceDecisionMs += stepMs;
    battle.step(stepMs);
  }
}

function decide(battle: Battle, t: number, purge: boolean): void {
  const allies = battle.getAllies().filter((a) => a.alive);
  const healer = allies.find((a) => a.role === "healer");
  if (!healer) return;

  const lowest = [...allies].sort((a, b) => a.hp / a.maxHp - b.hp / b.maxHp)[0];
  const hurtCount = allies.filter((a) => a.hp / a.maxHp < 0.75).length;
  const poisoned = purge ? allies.find((a) => a.effects.length > 0) : undefined;
  const telegraph = battle.getTelegraph();

  if (telegraph?.type === "bigAttack" && battle.canUseSkillNow("healer", "shield")) {
    battle.issueCommand({ timeMs: t, skillId: "shield", targetId: lowest.id });
  } else if (poisoned && battle.canUseSkillNow("healer", "purge")) {
    battle.issueCommand({ timeMs: t, skillId: "purge", targetId: poisoned.id });
  } else if (hurtCount >= 2 && battle.canUseSkillNow("healer", "heal_aoe")) {
    battle.issueCommand({ timeMs: t, skillId: "heal_aoe" });
  } else if (lowest.hp / lowest.maxHp < 0.85 && battle.canUseSkillNow("healer", "heal_single")) {
    battle.issueCommand({ timeMs: t, skillId: "heal_single", targetId: lowest.id });
  }
}
