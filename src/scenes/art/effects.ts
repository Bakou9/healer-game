import Phaser from "phaser";

const SPARK = "spark";

type Burst = Phaser.GameObjects.Particles.ParticleEmitter;

/**
 * Effets visuels de combat : jets de particules (soin, bouclier, poison,
 * impact) et anneaux. Les émetteurs Phaser recyclent leurs particules (patron
 * Object Pool, déjà appliqué aux chiffres flottants). Les effets sont brefs,
 * translucides et n'occultent jamais les informations (docs/UX.md, principe 6).
 */
export class CombatEffects {
  private readonly heal: Burst;
  private readonly shield: Burst;
  private readonly poison: Burst;
  private readonly impact: Burst;
  private readonly purge: Burst;

  constructor(private readonly scene: Phaser.Scene) {
    if (!scene.textures.exists(SPARK)) {
      const g = scene.make.graphics({}, false);
      g.fillStyle(0xffffff, 1);
      g.fillCircle(6, 6, 6);
      g.generateTexture(SPARK, 12, 12);
      g.destroy();
    }
    this.heal = this.emitter(0x7cffb2, { speed: { min: 25, max: 70 }, angle: { min: 235, max: 305 }, scale: { start: 0.8, end: 0 }, lifespan: 750 });
    this.shield = this.emitter(0x7cc8ff, { speed: { min: 40, max: 90 }, angle: { min: 0, max: 360 }, scale: { start: 0.7, end: 0 }, lifespan: 520 });
    this.poison = this.emitter(0xc78cff, { speed: { min: 15, max: 45 }, angle: { min: 240, max: 300 }, scale: { start: 0.9, end: 0.1 }, lifespan: 850 });
    this.impact = this.emitter(0xff7c7c, { speed: { min: 50, max: 120 }, angle: { min: 0, max: 360 }, scale: { start: 0.7, end: 0 }, lifespan: 380 });
    this.purge = this.emitter(0xffffff, { speed: { min: 30, max: 90 }, angle: { min: 200, max: 340 }, scale: { start: 0.7, end: 0 }, lifespan: 600 });
  }

  private emitter(tint: number, config: Phaser.Types.GameObjects.Particles.ParticleEmitterConfig): Burst {
    return this.scene.add
      .particles(0, 0, SPARK, { emitting: false, tint, alpha: { start: 0.95, end: 0 }, blendMode: "ADD", ...config })
      .setDepth(40);
  }

  healBurst(x: number, y: number): void {
    this.heal.explode(9, x, y);
  }

  shieldBurst(x: number, y: number): void {
    this.shield.explode(10, x, y);
    this.ring(x, y, 0x7cc8ff);
  }

  poisonBurst(x: number, y: number, count = 6): void {
    this.poison.explode(count, x, y);
  }

  purgeBurst(x: number, y: number): void {
    this.purge.explode(12, x, y);
    this.ring(x, y, 0x7cffb2);
  }

  impactBurst(x: number, y: number): void {
    this.impact.explode(6, x, y);
  }

  /** Anneau qui s'élargit et s'efface. */
  private ring(x: number, y: number, color: number): void {
    const g = this.scene.add.graphics().setDepth(41);
    g.lineStyle(4, color, 1);
    g.strokeCircle(0, 0, 26);
    g.setPosition(x, y).setScale(0.4);
    this.scene.tweens.add({
      targets: g,
      scale: 1.5,
      alpha: 0,
      duration: 420,
      ease: "Cubic.easeOut",
      onComplete: () => g.destroy(),
    });
  }
}
