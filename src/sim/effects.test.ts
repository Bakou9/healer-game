import { describe, it, expect } from "vitest";
import { Battle } from "./Battle";
import { createEncounter } from "./encounter";
import { runWithReferenceHealer } from "./referenceHealerBot";
import type { BattleEvent } from "./events";
import type { BossActionDef, BossDef, Command, EncounterDef } from "./types";

const POISON: BossActionDef = { type: "poison", telegraphMs: 0, multiplier: 0, effectId: "poison" };
const IDLE: BossActionDef = { type: "attack", telegraphMs: 0, multiplier: 0 };

/** Rencontre où le boss ne fait qu'empoisonner (toutes les secondes), sans phases. */
function encounterWith(pattern: BossActionDef[], overrides: Partial<EncounterDef> = {}): EncounterDef {
  const base = createEncounter(1);
  const boss: BossDef = { ...base.boss, tickMs: 1000, pattern, phases: undefined };
  return { ...base, boss, ...overrides };
}

/** Un seul empoisonnement (à 1000 ms), puis le boss ne fait plus rien. */
function singlePoisonEncounter(): EncounterDef {
  return encounterWith([POISON, ...Array.from({ length: 200 }, () => IDLE)]);
}

function collect(battle: Battle): BattleEvent[] {
  const events: BattleEvent[] = [];
  battle.subscribe((e) => events.push(e));
  return events;
}

function firstVictim(enc: EncounterDef): string {
  const probe = new Battle(enc, []);
  const events = collect(probe);
  probe.run(1000);
  const applied = events.find((e) => e.type === "effectApplied");
  if (applied?.type !== "effectApplied") throw new Error("aucun effet appliqué");
  return applied.unitId;
}

const KEEP_ALIVE: Command[] = Array.from({ length: 40 }, (_, i) => ({ timeMs: i * 1200, skillId: "heal_aoe" }));

describe("effets sur la durée : poison", () => {
  it("chaque tick inflige exactement les dégâts définis dans les données", () => {
    const enc = singlePoisonEncounter();
    const battle = new Battle(enc, KEEP_ALIVE);
    const events = collect(battle);
    battle.run(12000);
    const ticks = events.filter((e) => e.type === "effectTick");
    expect(ticks.length).toBeGreaterThan(0);
    for (const tick of ticks) {
      if (tick.type === "effectTick") expect(tick.amount + tick.absorbed).toBe(enc.effects[0].damagePerTick);
    }
  });

  it("le poison expire tout seul après sa durée, après le nombre de ticks attendu", () => {
    const enc = singlePoisonEncounter();
    const fx = enc.effects[0];
    const battle = new Battle(enc, KEEP_ALIVE);
    const events = collect(battle);
    battle.run(12000);
    expect(events.filter((e) => e.type === "effectApplied")).toHaveLength(1);
    const ended = events.filter((e) => e.type === "effectEnded");
    expect(ended).toHaveLength(1);
    expect(ended[0]).toMatchObject({ reason: "expired", timeMs: 1000 + fx.durationMs });
    expect(events.filter((e) => e.type === "effectTick")).toHaveLength(fx.durationMs / fx.tickMs);
  });

  it("une nouvelle application rafraîchit le poison au lieu de l'empiler", () => {
    const battle = new Battle(encounterWith([POISON]), []);
    let maxStacks = 0;
    battle.subscribe(() => {
      for (const u of battle.getAllies()) maxStacks = Math.max(maxStacks, u.effects.length);
    });
    battle.run(20000);
    expect(maxStacks).toBe(1);
  });

  it("la Purge retire le poison de la cible et arrête ses dégâts", () => {
    const enc = singlePoisonEncounter();
    const victim = firstVictim(enc);
    const battle = new Battle(enc, [{ timeMs: 2500, skillId: "purge", targetId: victim }, ...KEEP_ALIVE]);
    const events = collect(battle);
    battle.run(12000);
    expect(events.filter((e) => e.type === "effectEnded" && e.reason === "cleansed")).toHaveLength(1);
    expect(events.filter((e) => e.type === "effectTick").every((e) => e.timeMs < 2500)).toBe(true);
    expect(battle.getAllies().find((u) => u.id === victim)!.effects).toHaveLength(0);
  });

  it("la Purge n'agit que sur la cible visée", () => {
    const enc = singlePoisonEncounter();
    const victim = firstVictim(enc);
    const other = enc.allies.find((a) => a.id !== victim && a.role !== "healer")!.id;
    const battle = new Battle(enc, [{ timeMs: 2500, skillId: "purge", targetId: other }, ...KEEP_ALIVE]);
    const events = collect(battle);
    battle.run(4000);
    expect(events.filter((e) => e.type === "effectEnded" && e.reason === "cleansed")).toHaveLength(0);
    expect(battle.getAllies().find((u) => u.id === victim)!.effects).toHaveLength(1);
  });

  it("un allié qui meurt du poison perd ses effets et ne subit plus de dégâts", () => {
    const enc = singlePoisonEncounter();
    const dps = enc.allies.find((a) => a.role === "dps")!;
    const healer = enc.allies.find((a) => a.role === "healer")!;
    const fragile = { ...dps, id: "fragile", maxHp: 30 };
    const withFragile = { ...enc, allies: [fragile, healer] };
    // Le ciblage est aléatoire (mais seedé) : on cherche un seed où « fragile » est la victime.
    const seed = Array.from({ length: 40 }, (_, i) => i + 1).find(
      (s) => firstVictim({ ...withFragile, seed: s }) === "fragile",
    );
    expect(seed).toBeDefined();
    const battle = new Battle({ ...withFragile, seed: seed! }, []);
    const events = collect(battle);
    battle.run(20000);
    const died = events.filter((e) => e.type === "unitDied" && e.unitId === "fragile");
    expect(died).toHaveLength(1);
    expect(events.some((e) => e.type === "effectEnded" && e.unitId === "fragile" && e.reason === "died")).toBe(true);
    expect(events.filter((e) => e.type === "effectTick" && e.unitId === "fragile" && e.timeMs > died[0].timeMs)).toHaveLength(0);
  });
});

describe("phases du boss", () => {
  function fullFight() {
    const battle = new Battle(createEncounter(7), []);
    const events = collect(battle);
    runWithReferenceHealer(battle, 150000);
    return { battle, events };
  }

  it("le boss change de phase une seule fois, quand ses PV passent sous le seuil", () => {
    const { events } = fullFight();
    const changes = events.filter((e) => e.type === "bossPhaseChanged");
    expect(changes).toHaveLength(1);
    expect(changes[0]).toMatchObject({ phase: 1, name: "Fureur" });
  });

  it("aucune phase n'est déclenchée tant que le boss est au-dessus du seuil", () => {
    const battle = new Battle(createEncounter(7), []);
    const events = collect(battle);
    battle.run(10000);
    expect(battle.getBossHp() / battle.getBossMaxHp()).toBeGreaterThan(0.5);
    expect(events.some((e) => e.type === "bossPhaseChanged")).toBe(false);
    expect(battle.getBossPhase()).toEqual({ index: 0, name: "" });
  });

  it("le comportement change : le poison n'apparaît qu'à partir de la phase 2", () => {
    const { events } = fullFight();
    const phaseTime = events.find((e) => e.type === "bossPhaseChanged")!.timeMs;
    const poisonActions = events.filter((e) => e.type === "bossAction" && e.action === "poison");
    expect(poisonActions.length).toBeGreaterThan(0);
    expect(poisonActions.every((e) => e.timeMs > phaseTime)).toBe(true);
  });
});
