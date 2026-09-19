import { Battle } from "../sim/Battle";
import { createEncounter } from "../sim/encounter";
import { formatEvent } from "../sim/events";
import { runWithReferenceHealer } from "../sim/referenceHealerBot";
import type { Command } from "../sim/types";

/**
 * Scénarios de combat rejoués par les tests de non-régression. Chacun est
 * déterministe (seed + commandes fixes). Ajouter un scénario quand on ajoute
 * une mécanique ou qu'on corrige un bug (pour qu'il ne revienne pas).
 */
const MAX_MS = 120000;

function spamHeal(): Command[] {
  const commands: Command[] = [];
  for (let t = 0; t < 9600; t += 1200) {
    commands.push({ timeMs: t, skillId: "heal_single", targetId: "tank" });
  }
  return commands;
}

function shieldOnlyOnTank(): Command[] {
  return [{ timeMs: 0, skillId: "shield", targetId: "tank" }];
}

export interface Scenario {
  name: string;
  subscribeAndRun: (onEvent: (line: string) => void) => Battle;
}

function scenario(name: string, drive: (battle: Battle) => void, commands: Command[] = [], seed = 1): Scenario {
  return {
    name,
    subscribeAndRun: (onEvent) => {
      const battle = new Battle(createEncounter(seed), commands);
      battle.subscribe((e) => onEvent(formatEvent(e)));
      drive(battle);
      return battle;
    },
  };
}

export const SCENARIOS: Scenario[] = [
  scenario("bot-seed7", (b) => runWithReferenceHealer(b, MAX_MS), [], 7),
  scenario("bot-seed42", (b) => runWithReferenceHealer(b, MAX_MS), [], 42),
  scenario("sans-soigneur-seed1", (b) => void b.run(MAX_MS), [], 1),
  scenario("spam-soin-seed3", (b) => void b.run(MAX_MS), spamHeal(), 3),
  scenario("bouclier-initial-seed5", (b) => void b.run(MAX_MS), shieldOnlyOnTank(), 5),
];

/** Déroulé complet d'un scénario : un événement par ligne, puis un résumé de l'état final. */
export function recordScenario(s: Scenario): string[] {
  const lines: string[] = [];
  const battle = s.subscribeAndRun((line) => lines.push(line));
  lines.push(
    `FINAL ${battle.getResult()} t=${battle.getClock()} boss=${battle.getBossHp()} ` +
      battle
        .getAllies()
        .map((u) => `${u.id}:${Math.round(u.hp)}/${Math.round(u.shield)}/${Math.round(u.mana)}`)
        .join(" "),
  );
  return lines;
}
