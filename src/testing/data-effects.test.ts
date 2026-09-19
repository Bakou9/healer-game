import { describe, it, expect } from "vitest";
import charactersData from "../data/characters.json";
import skillsData from "../data/skills.json";
import bossData from "../data/boss1.json";
import effectsData from "../data/effects.json";
import { FIXED_STEP_MS } from "../sim/fixedStep";
import type { BossActionDef, BossDef, EffectDef } from "../sim/types";

const boss = bossData as BossDef;
const effects = effectsData as EffectDef[];

describe("intégrité des données : effets et phases", () => {
  it("les effets ont des valeurs valides et un tick supérieur au pas de simulation", () => {
    const ids = effects.map((e) => e.id);
    expect(new Set(ids).size).toBe(ids.length);
    for (const e of effects) {
      expect(e.damagePerTick, e.id).toBeGreaterThan(0);
      expect(e.durationMs, e.id).toBeGreaterThanOrEqual(e.tickMs);
      expect(e.tickMs, e.id).toBeGreaterThan(FIXED_STEP_MS);
    }
  });

  it("toute action de boss qui applique un effet référence un effet existant", () => {
    const actions: BossActionDef[] = [...boss.pattern, ...(boss.phases ?? []).flatMap((p) => p.pattern)];
    const ids = new Set(effects.map((e) => e.id));
    for (const a of actions) {
      if (a.effectId) expect(ids.has(a.effectId), a.effectId).toBe(true);
    }
  });

  it("les phases du boss sont triées par seuil décroissant et ont un rythme valide", () => {
    const phases = boss.phases ?? [];
    const thresholds = phases.map((p) => p.atHpRatio);
    expect(thresholds).toEqual([...thresholds].sort((a, b) => b - a));
    for (const p of phases) {
      expect(p.atHpRatio).toBeGreaterThan(0);
      expect(p.atHpRatio).toBeLessThan(1);
      expect(p.pattern.length).toBeGreaterThan(0);
      expect(p.tickMs).toBeGreaterThan(FIXED_STEP_MS);
      for (const a of p.pattern) expect(a.telegraphMs, `${p.name}/${a.type}`).toBeLessThan(p.tickMs);
    }
  });
});

describe("valeurs à hauteur humaine (docs/EQUILIBRAGE.md)", () => {
  it("toute quantité de jeu est un entier ; seuls multiplicateurs et ratios peuvent être décimaux", () => {
    const offenders: string[] = [];
    const walk = (value: unknown, path: string, key: string) => {
      if (typeof value === "number") {
        const decimalAllowed = /multiplier|ratio/i.test(key);
        if (!Number.isInteger(value) && !decimalAllowed) offenders.push(`${path} = ${value}`);
      } else if (Array.isArray(value)) {
        value.forEach((v, i) => walk(v, `${path}[${i}]`, key));
      } else if (value && typeof value === "object") {
        for (const [k, v] of Object.entries(value)) walk(v, `${path}.${k}`, k);
      }
    };
    walk({ characters: charactersData, skills: skillsData, boss: bossData, effects: effectsData }, "data", "");
    expect(offenders).toEqual([]);
  });
});
