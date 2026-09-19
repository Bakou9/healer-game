import { describe, it, expect } from "vitest";
import charactersData from "../data/characters.json";
import skillsData from "../data/skills.json";
import bossData from "../data/boss1.json";
import { ALLY_ATTACK_INTERVAL_MS } from "../sim/Battle";
import { FIXED_STEP_MS } from "../sim/fixedStep";
import type { BossDef, CharacterDef, SkillDef } from "../sim/types";

const characters = charactersData as CharacterDef[];
const skills = skillsData as SkillDef[];
const boss = bossData as BossDef;

/** Ids référencés en dur dans le code (BattleScene, bot de référence, tests). */
const HARD_CODED_CHARACTER_IDS = ["healer", "tank"];
const HARD_CODED_SKILL_IDS = ["heal_single", "heal_aoe", "shield", "purge"];

describe("intégrité des données de jeu", () => {
  it("les ids de personnages sont uniques et ceux référencés dans le code existent", () => {
    const ids = characters.map((c) => c.id);
    expect(new Set(ids).size).toBe(ids.length);
    for (const id of HARD_CODED_CHARACTER_IDS) expect(ids).toContain(id);
  });

  it("il y a exactement un soigneur, et il a du mana", () => {
    const healers = characters.filter((c) => c.role === "healer");
    expect(healers).toHaveLength(1);
    expect(healers[0].maxMana).toBeGreaterThan(0);
    expect(healers[0].manaRegenPerSec).toBeGreaterThan(0);
  });

  it("les personnages ont des statistiques valides", () => {
    for (const c of characters) {
      expect(c.maxHp, c.id).toBeGreaterThan(0);
      expect(c.atk, c.id).toBeGreaterThanOrEqual(0);
      expect(c.def, c.id).toBeGreaterThanOrEqual(0);
    }
  });

  it("les ids de compétences sont uniques et ceux référencés dans le code existent", () => {
    const ids = skills.map((s) => s.id);
    expect(new Set(ids).size).toBe(ids.length);
    for (const id of HARD_CODED_SKILL_IDS) expect(ids).toContain(id);
  });

  it("chaque compétence a un coût, une recharge et au moins un effet", () => {
    for (const s of skills) {
      expect(s.manaCost, s.id).toBeGreaterThanOrEqual(0);
      expect(s.cooldownMs, s.id).toBeGreaterThan(0);
      const hasEffect = !!s.healAmount || !!s.shieldAmount || !!s.cleanse;
      expect(hasEffect, `${s.id} n'a aucun effet`).toBe(true);
    }
  });

  it("le boss a des statistiques et un pattern valides", () => {
    expect(boss.maxHp).toBeGreaterThan(0);
    expect(boss.tickMs).toBeGreaterThan(0);
    expect(boss.pattern.length).toBeGreaterThan(0);
    for (const action of boss.pattern) {
      // Un télégraphe plus long que l'intervalle serait affiché en permanence.
      expect(action.telegraphMs, action.type).toBeLessThan(boss.tickMs);
    }
  });

  it("le pas de simulation fixe est plus petit que les plus petits intervalles des données", () => {
    // Battle.step() ne traite qu'une action par appel : un pas trop grand ferait « sauter » des actions.
    expect(FIXED_STEP_MS).toBeLessThan(boss.tickMs);
    expect(FIXED_STEP_MS).toBeLessThan(ALLY_ATTACK_INTERVAL_MS);
  });
});
