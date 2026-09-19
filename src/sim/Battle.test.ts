import { describe, it, expect } from "vitest";
import { Battle } from "./Battle";
import { createEncounter } from "./encounter";
import { runWithReferenceHealer } from "./referenceHealerBot";
import type { Command } from "./types";

const FIGHT_DURATION_MS = 120000;

function healerScript(durationMs = FIGHT_DURATION_MS): Command[] {
  const commands: Command[] = [];
  for (let t = 0; t < durationMs; t += 5000) {
    commands.push({ timeMs: t, skillId: "heal_aoe" });
  }
  for (let t = 1300; t < durationMs; t += 1300) {
    commands.push({ timeMs: t, skillId: "heal_single", targetId: "tank" });
  }
  return commands;
}

describe("Battle", () => {
  it("est déterministe : même seed + mêmes commandes => même résultat", () => {
    const run = () => {
      const battle = new Battle(createEncounter(42), healerScript(60000));
      battle.run(60000);
      return (
        battle.getAllies().map((u) => `${u.id}:${Math.round(u.hp)}`).join(",") +
        `|boss:${battle.getBossHp()}`
      );
    };
    expect(run()).toBe(run());
  });

  it("l'équipe est vaincue si le soigneur ne soigne jamais", () => {
    const battle = new Battle(createEncounter(1), []);
    const result = battle.run(60000);
    expect(result).toBe("defeat");
  });

  it("un soigneur qui réagit à l'état du combat permet de gagner", () => {
    // Script figé remplacé par un bot réactif : il soigne la cible la plus
    // basse et bouclie avant les grosses attaques, comme le ferait un joueur
    // raisonnable. C'est le vrai test d'équilibrage du combat.
    const battle = new Battle(createEncounter(7), []);
    runWithReferenceHealer(battle, FIGHT_DURATION_MS);
    expect(battle.getResult()).toBe("victory");
    // Avec des soins réactifs, personne ne devrait mourir.
    expect(battle.getAllies().every((u) => u.alive)).toBe(true);
  });

  it("un soin échoue s'il n'y a plus assez de mana, même si le cooldown est prêt", () => {
    // Un soin toutes les 1200ms (dès que le cooldown le permet) draine plus
    // vite que la régénération (18 mana / cast contre 7.2 mana / cycle) :
    // au bout de plusieurs cycles, le mana vient à manquer.
    const spam: Command[] = [];
    for (let t = 0; t < 9600; t += 1200) {
      spam.push({ timeMs: t, skillId: "heal_single", targetId: "tank" });
    }
    const battle = new Battle(createEncounter(3), spam);
    battle.run(9600);
    const healer = battle.getAllies().find((u) => u.role === "healer")!;
    expect(healer.mana).toBeLessThan(18);
    expect(battle.canUseSkillNow("healer", "heal_single")).toBe(false);
  });

  it("le bouclier absorbe les dégâts avant les PV", () => {
    const battle = new Battle(createEncounter(5), [
      { timeMs: 0, skillId: "shield", targetId: "tank" },
    ]);
    battle.step(100);
    const before = battle.getAllies().find((u) => u.id === "tank")!.hp;
    // Avance jusqu'à la première attaque simple du boss.
    battle.run(2300);
    const after = battle.getAllies().find((u) => u.id === "tank")!.hp;
    // Le bouclier (260) doit avoir encaissé au moins une partie des dégâts.
    expect(after).toBeGreaterThanOrEqual(before - 50);
  });

  it("le boss télégraphie sa grosse attaque avant de frapper", () => {
    const battle = new Battle(createEncounter(9), []);
    let sawTelegraph = false;
    for (let t = 0; t < 8000; t += 100) {
      battle.step(100);
      if (battle.getTelegraph()?.type === "bigAttack") sawTelegraph = true;
    }
    expect(sawTelegraph).toBe(true);
  });
});
