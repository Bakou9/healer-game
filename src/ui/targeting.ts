/**
 * Ciblage en un geste (ticket E04-T04). Toucher un allié le sélectionne ; les
 * sorts ciblés s'appliquent à l'allié sélectionné, et la sélection RESTE après
 * un sort (soigner plusieurs fois la même cible = un seul geste). Logique pure,
 * sans Phaser : testée sans navigateur.
 *
 * Choix de conception (voir docs/REVUES.md, E04-T04) : pas de ciblage
 * automatique — choisir QUI soigner est la décision centrale du jeu.
 */
export type CastResolution =
  | { kind: "cast"; targetId?: string }
  | { kind: "needTarget" }
  | { kind: "unavailable" };

export interface CastableSkill {
  target: "single" | "all";
}

export class TargetSelection {
  private selectedId: string | null = null;

  get selected(): string | null {
    return this.selectedId;
  }

  /** Toucher une carte : sélectionne l'allié, ou annule si c'est déjà lui. Un allié K.O. ne se sélectionne pas. */
  tap(unitId: string, isAlive: boolean): void {
    if (!isAlive) return;
    this.selectedId = this.selectedId === unitId ? null : unitId;
  }

  /** À appeler à chaque image : abandonne la sélection d'un allié qui n'est plus vivant. */
  sync(aliveIds: Iterable<string>): void {
    if (this.selectedId !== null && !new Set(aliveIds).has(this.selectedId)) this.selectedId = null;
  }

  clear(): void {
    this.selectedId = null;
  }

  /** Que faire quand le joueur touche un sort ? La sélection n'est jamais modifiée ici. */
  resolve(skill: CastableSkill, canUseNow: boolean): CastResolution {
    if (!canUseNow) return { kind: "unavailable" };
    if (skill.target === "all") return { kind: "cast" };
    return this.selectedId === null ? { kind: "needTarget" } : { kind: "cast", targetId: this.selectedId };
  }
}
