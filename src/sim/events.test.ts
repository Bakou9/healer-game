import { describe, it, expect } from "vitest";
import { Battle } from "./Battle";
import { createEncounter } from "./encounter";
import { runWithReferenceHealer } from "./referenceHealerBot";
import type { BattleEvent } from "./events";

function record(seed: number): { battle: Battle; events: BattleEvent[] } {
  const battle = new Battle(createEncounter(seed), []);
  const events: BattleEvent[] = [];
  battle.subscribe((e) => events.push(e));
  runWithReferenceHealer(battle, 120000);
  return { battle, events };
}

describe("événements de combat (Observer)", () => {
  it("émet exactement un battleEnded, en dernier, cohérent avec le résultat", () => {
    const { battle, events } = record(7);
    const ended = events.filter((e) => e.type === "battleEnded");
    expect(ended).toHaveLength(1);
    expect(events[events.length - 1]).toEqual(ended[0]);
    expect(ended[0]).toMatchObject({ result: battle.getResult() });
  });

  it("les événements sont émis dans l'ordre chronologique", () => {
    const { events } = record(42);
    const times = events.map((e) => e.timeMs);
    expect(times).toEqual([...times].sort((a, b) => a - b));
  });

  it("les dégâts infligés au boss correspondent à ses PV perdus", () => {
    const { battle, events } = record(7);
    const dealt = events.reduce((sum, e) => sum + (e.type === "bossDamaged" ? e.amount : 0), 0);
    // La victoire se produit quand les PV du boss tombent à 0 ou moins (dégâts excédentaires possibles).
    expect(dealt).toBeGreaterThanOrEqual(battle.getBossMaxHp());
  });

  it("se désabonner arrête bien la réception des événements", () => {
    const battle = new Battle(createEncounter(1), []);
    let received = 0;
    const unsubscribe = battle.subscribe(() => (received += 1));
    battle.run(5000);
    const before = received;
    unsubscribe();
    battle.run(20000);
    expect(before).toBeGreaterThan(0);
    expect(received).toBe(before);
  });

  it("un soin n'annonce que les PV réellement rendus (pas de surplus au-delà du maximum)", () => {
    const battle = new Battle(createEncounter(1), [{ timeMs: 0, skillId: "heal_single", targetId: "tank" }]);
    const healed: number[] = [];
    battle.subscribe((e) => {
      if (e.type === "healed") healed.push(e.amount);
    });
    battle.step(100);
    expect(healed).toEqual([0]); // le tank est déjà à pleine vie
  });
});
