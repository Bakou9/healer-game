import { describe, it, expect } from "vitest";
import { FixedStepper } from "./fixedStep";

describe("FixedStepper", () => {
  it("exécute des pas identiques quelle que soit la cadence d'affichage", () => {
    const run = (frameMs: number) => {
      const stepper = new FixedStepper(50);
      let total = 0;
      for (let t = 0; t < 10000; t += frameMs) stepper.advance(frameMs, (dt) => (total += dt));
      return total;
    };
    // 60 FPS et 30 FPS simulent (à un pas près) la même durée.
    expect(Math.abs(run(1000 / 60) - run(1000 / 30))).toBeLessThanOrEqual(50);
  });

  it("n'exécute aucun pas tant que le temps accumulé est inférieur à un pas", () => {
    const stepper = new FixedStepper(50);
    expect(stepper.advance(20, () => {})).toBe(0);
    expect(stepper.advance(20, () => {})).toBe(0);
    expect(stepper.advance(20, () => {})).toBe(1); // 60 ms cumulés
  });

  it("abandonne le retard après un gros ralentissement au lieu de rattraper d'un coup", () => {
    const stepper = new FixedStepper(50, 5);
    let steps = 0;
    stepper.advance(60000, () => (steps += 1));
    expect(steps).toBe(5);
    // Le retard a été jeté : la frame suivante repart normalement.
    expect(stepper.advance(16, () => {})).toBe(0);
  });

  it("ignore les delta négatifs", () => {
    const stepper = new FixedStepper(50);
    expect(stepper.advance(-100, () => {})).toBe(0);
  });
});
