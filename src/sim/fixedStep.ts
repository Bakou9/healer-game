/**
 * Pas de simulation fixe, en ms. Doit rester nettement inférieur aux plus
 * petits intervalles des données (tickMs du boss, attaque des alliés) :
 * `Battle.step()` ne traite qu'une action par appel. Vérifié par un test.
 */
export const FIXED_STEP_MS = 50;

/**
 * Accumulateur de temps (Game Loop à pas fixe). Le rendu tourne à la cadence
 * de l'écran (delta variable) ; la simulation avance par pas identiques, ce
 * qui la garde déterministe quel que soit le FPS de l'appareil.
 */
export class FixedStepper {
  private accumulatorMs = 0;

  constructor(
    private readonly stepMs: number = FIXED_STEP_MS,
    /** Garde-fou : après un gros ralentissement (onglet en arrière-plan), on abandonne le retard. */
    private readonly maxStepsPerFrame: number = 5,
  ) {}

  /** Appelle `step(stepMs)` autant de fois que nécessaire. Renvoie le nombre de pas exécutés. */
  advance(deltaMs: number, step: (dtMs: number) => void): number {
    this.accumulatorMs += Math.max(0, deltaMs);
    let steps = 0;
    while (this.accumulatorMs >= this.stepMs && steps < this.maxStepsPerFrame) {
      step(this.stepMs);
      this.accumulatorMs -= this.stepMs;
      steps += 1;
    }
    if (steps === this.maxStepsPerFrame) this.accumulatorMs = 0;
    return steps;
  }
}
