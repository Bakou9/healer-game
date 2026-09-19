import { describe, it, expect } from "vitest";
import { Battle } from "../sim/Battle";
import { createEncounter } from "../sim/encounter";
import { runWithReferenceHealer } from "../sim/referenceHealerBot";
import { diffLines, expectMatchesGolden } from "./golden";
import { SCENARIOS, recordScenario } from "./scenarios";

describe("non-régression : déroulés de combat figés (golden)", () => {
  for (const scenario of SCENARIOS) {
    it(`le scénario « ${scenario.name} » se déroule comme la référence`, () => {
      expectMatchesGolden(scenario.name, recordScenario(scenario));
    });
  }
});

describe("outil golden", () => {
  it("signale le premier point de divergence et le nombre de lignes différentes", () => {
    const diff = diffLines(["a", "b", "c", "d"], ["a", "b", "X", "Y"]);
    expect(diff).toEqual({ firstDivergenceLine: 3, expected: "c", actual: "X", differingLines: 2 });
  });

  it("détecte un combat plus court ou plus long que la référence", () => {
    expect(diffLines(["a", "b"], ["a"])?.actual).toBeUndefined();
    expect(diffLines(["a"], ["a", "b"])?.expected).toBeUndefined();
  });

  it("ne renvoie rien quand les déroulés sont identiques", () => {
    expect(diffLines(["a", "b"], ["a", "b"])).toBeNull();
  });
});

describe("non-régression : équilibrage avec le bot de soin de référence", () => {
  const SEEDS = 100;

  function playAll() {
    return Array.from({ length: SEEDS }, (_, i) => {
      const battle = new Battle(createEncounter(i + 1), []);
      let lowestHpRatio = 1;
      battle.subscribe(() => {
        for (const u of battle.getAllies()) lowestHpRatio = Math.min(lowestHpRatio, u.hp / u.maxHp);
      });
      runWithReferenceHealer(battle, 120000);
      return { result: battle.getResult(), durationMs: battle.getClock(), lowestHpRatio };
    });
  }

  it("le combat reste gagnable : au moins 95 % de victoires", () => {
    const wins = playAll().filter((r) => r.result === "victory").length;
    expect(wins / SEEDS).toBeGreaterThanOrEqual(0.95);
  });

  it("le combat reste tendu : l'équipe descend en moyenne sous 60 % de PV au plus bas", () => {
    const lows = playAll().map((r) => r.lowestHpRatio);
    const average = lows.reduce((a, b) => a + b, 0) / lows.length;
    expect(average).toBeLessThan(0.6);
  });

  it("la durée du combat reste dans une fourchette raisonnable (40 s à 110 s)", () => {
    for (const r of playAll()) {
      expect(r.durationMs).toBeGreaterThanOrEqual(40000);
      expect(r.durationMs).toBeLessThanOrEqual(110000);
    }
  });
});
