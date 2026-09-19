import { describe, it, expect } from "vitest";
import charactersData from "../data/characters.json";
import skillsData from "../data/skills.json";
import type { CharacterDef, SkillDef } from "../sim/types";
import { SKILL_ICON, portraitFor } from "./artMap";

describe("icônes : tout le contenu de jeu a son dessin", () => {
  it("chaque personnage a une icône de portrait (par id ou par rôle)", () => {
    for (const c of charactersData as CharacterDef[]) {
      expect(portraitFor(c.id, c.role), `personnage « ${c.id} » sans icône`).toBeDefined();
    }
  });

  it("chaque sort a une icône", () => {
    for (const s of skillsData as SkillDef[]) {
      expect(SKILL_ICON[s.id], `sort « ${s.id} » sans icône`).toBeDefined();
    }
  });

  it("les personnages de rôle identique restent distinguables par leur icône", () => {
    const dps = (charactersData as CharacterDef[]).filter((c) => c.role === "dps");
    const icons = dps.map((c) => portraitFor(c.id, c.role));
    expect(new Set(icons).size).toBe(icons.length);
  });
});
