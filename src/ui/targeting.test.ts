import { describe, it, expect } from "vitest";
import { TargetSelection } from "./targeting";

const single = { target: "single" as const };
const all = { target: "all" as const };

describe("TargetSelection : sélection en un geste", () => {
  it("toucher un allié le sélectionne", () => {
    const sel = new TargetSelection();
    sel.tap("tank", true);
    expect(sel.selected).toBe("tank");
  });

  it("toucher un autre allié déplace la sélection", () => {
    const sel = new TargetSelection();
    sel.tap("tank", true);
    sel.tap("dps1", true);
    expect(sel.selected).toBe("dps1");
  });

  it("retoucher l'allié sélectionné annule la sélection", () => {
    const sel = new TargetSelection();
    sel.tap("tank", true);
    sel.tap("tank", true);
    expect(sel.selected).toBeNull();
  });

  it("un allié K.O. ne peut pas être sélectionné", () => {
    const sel = new TargetSelection();
    sel.tap("dps1", false);
    expect(sel.selected).toBeNull();
  });

  it("la sélection est abandonnée quand l'allié meurt", () => {
    const sel = new TargetSelection();
    sel.tap("dps1", true);
    sel.sync(["tank", "healer"]);
    expect(sel.selected).toBeNull();
  });

  it("la sélection est conservée tant que l'allié est vivant", () => {
    const sel = new TargetSelection();
    sel.tap("tank", true);
    sel.sync(["tank", "healer"]);
    expect(sel.selected).toBe("tank");
  });
});

describe("TargetSelection : résolution d'un sort", () => {
  it("un sort de zone se lance sans cible, même sans sélection", () => {
    expect(new TargetSelection().resolve(all, true)).toEqual({ kind: "cast" });
  });

  it("un sort ciblé sans sélection demande de choisir une cible", () => {
    expect(new TargetSelection().resolve(single, true)).toEqual({ kind: "needTarget" });
  });

  it("un sort ciblé s'applique à l'allié sélectionné", () => {
    const sel = new TargetSelection();
    sel.tap("tank", true);
    expect(sel.resolve(single, true)).toEqual({ kind: "cast", targetId: "tank" });
  });

  it("un sort indisponible (recharge, mana) ne lance rien, avec ou sans sélection", () => {
    const sel = new TargetSelection();
    sel.tap("tank", true);
    expect(sel.resolve(single, false)).toEqual({ kind: "unavailable" });
    expect(sel.resolve(all, false)).toEqual({ kind: "unavailable" });
  });

  it("la sélection reste après un sort : re-soigner la même cible = un seul geste", () => {
    const sel = new TargetSelection();
    sel.tap("tank", true);
    sel.resolve(single, true);
    expect(sel.selected).toBe("tank");
    expect(sel.resolve(single, true)).toEqual({ kind: "cast", targetId: "tank" });
  });

  it("un sort de zone ne modifie pas la sélection", () => {
    const sel = new TargetSelection();
    sel.tap("dps2", true);
    sel.resolve(all, true);
    expect(sel.selected).toBe("dps2");
  });
});
