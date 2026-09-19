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

/**
 * Bornes d'équilibrage : voir docs/EQUILIBRAGE.md (définition de « équilibré »
 * pour ce jeu). Une modification de valeurs qui sort de ces bornes est une
 * régression d'équilibrage à expliquer à l'utilisateur.
 */
describe("non-régression : équilibrage (docs/EQUILIBRAGE.md)", () => {
  const SEEDS = 100;
  const MAX_MS = 150000;

  interface Stats {
    winRate: number;
    deathRate: number;
    avgLowestHp: number;
    minDurationMs: number;
    maxDurationMs: number;
  }

  function play(drive: (b: Battle) => void): Stats {
    let wins = 0;
    let deaths = 0;
    let lowSum = 0;
    const durations: number[] = [];
    for (let seed = 1; seed <= SEEDS; seed++) {
      const battle = new Battle(createEncounter(seed), []);
      let lowest = 1;
      battle.subscribe(() => {
        for (const u of battle.getAllies()) lowest = Math.min(lowest, u.hp / u.maxHp);
      });
      drive(battle);
      if (battle.getResult() === "victory") wins += 1;
      if (battle.getAllies().some((u) => !u.alive)) deaths += 1;
      lowSum += lowest;
      durations.push(battle.getClock());
    }
    return {
      winRate: wins / SEEDS,
      deathRate: deaths / SEEDS,
      avgLowestHp: lowSum / SEEDS,
      minDurationMs: Math.min(...durations),
      maxDurationMs: Math.max(...durations),
    };
  }

  const attentive = play((b) => runWithReferenceHealer(b, MAX_MS));
  const slow = play((b) => runWithReferenceHealer(b, MAX_MS, { decisionEveryMs: 1500 }));
  const noPurge = play((b) => runWithReferenceHealer(b, MAX_MS, { purge: false }));
  const noHealer = play((b) => void b.run(MAX_MS));
  const spamSingleHeal = play((b) => {
    for (let i = 0; i < 100; i++) b.issueCommand({ timeMs: i * 1200, skillId: "heal_single", targetId: "tank" });
    b.run(MAX_MS);
  });

  it("gagnable : un joueur attentif gagne au moins 95 % des combats", () => {
    expect(attentive.winRate).toBeGreaterThanOrEqual(0.95);
  });

  it("juste : un joueur attentif perd rarement un allié (10 % des combats au plus)", () => {
    expect(attentive.deathRate).toBeLessThanOrEqual(0.1);
  });

  it("tendu mais pas au bord du gouffre : PV minimum moyen de l'équipe entre 15 % et 45 %", () => {
    expect(attentive.avgLowestHp).toBeGreaterThanOrEqual(0.15);
    expect(attentive.avgLowestHp).toBeLessThanOrEqual(0.45);
  });

  it("durée adaptée au mobile : 60 s à 120 s", () => {
    expect(attentive.minDurationMs).toBeGreaterThanOrEqual(60000);
    expect(attentive.maxDurationMs).toBeLessThanOrEqual(120000);
  });

  it("la passivité est punie : sans soigneur, ou en spammant un seul sort, on perd toujours", () => {
    expect(noHealer.winRate).toBe(0);
    expect(spamSingleHeal.winRate).toBe(0);
  });

  it("la réactivité compte : un joueur lent (1,5 s) descend nettement plus bas qu'un joueur attentif", () => {
    expect(attentive.avgLowestHp - slow.avgLowestHp).toBeGreaterThanOrEqual(0.05);
  });

  it("la Purge compte : ignorer le poison fait descendre l'équipe nettement plus bas", () => {
    expect(attentive.avgLowestHp - noPurge.avgLowestHp).toBeGreaterThanOrEqual(0.05);
    expect(noPurge.deathRate).toBeGreaterThan(attentive.deathRate);
  });
});
