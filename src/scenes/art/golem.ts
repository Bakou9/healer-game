import Phaser from "phaser";

const SCALE = 0.8;
const CORE_CALM = 0x5be7ff;
const CORE_FURY = 0xff6a3d;

/**
 * Le Golem Ancestral, dessiné en formes vectorielles : pierre sombre, fissures
 * et un cœur lumineux qui pulse. Le cœur est cyan au calme, orange en phase 2 ;
 * il s'emballe pendant un télégraphe pour annoncer le danger (le signal reste
 * doublé par le texte du compte à rebours).
 */
export class GolemView {
  private readonly container: Phaser.GameObjects.Container;
  private readonly glow: Phaser.GameObjects.Graphics;
  private readonly baseX: number;
  private readonly baseY: number;
  private lungeY = 0;
  private hitPulse = 0;

  constructor(private readonly scene: Phaser.Scene, x: number, y: number) {
    this.baseX = x;
    this.baseY = y;

    const body = scene.add.graphics();
    // Jambes
    body.fillStyle(0x4a4562, 1);
    body.fillRoundedRect(-48, 40, 36, 50, 9);
    body.fillRoundedRect(12, 40, 36, 50, 9);
    // Bras
    body.fillStyle(0x5a5474, 1);
    body.fillRoundedRect(-96, -22, 36, 84, 13);
    body.fillRoundedRect(60, -22, 36, 84, 13);
    // Poings
    body.fillStyle(0x6b6485, 1);
    body.fillRoundedRect(-102, 52, 48, 36, 11);
    body.fillRoundedRect(54, 52, 48, 36, 11);
    // Torse
    body.fillStyle(0x6b6485, 1);
    body.fillRoundedRect(-60, -30, 120, 88, 18);
    body.fillStyle(0x82799f, 1);
    body.fillRoundedRect(-60, -30, 120, 24, 18);
    // Épaules
    body.fillStyle(0x8a82a8, 1);
    body.fillCircle(-68, -26, 23);
    body.fillCircle(68, -26, 23);
    // Tête
    body.fillStyle(0x776f99, 1);
    body.fillRoundedRect(-30, -74, 60, 48, 13);
    body.fillStyle(0x8f87b0, 1);
    body.fillRoundedRect(-30, -74, 60, 14, 13);
    // Fissures
    body.lineStyle(2, 0x2f2b44, 0.9);
    body.lineBetween(-40, 12, -26, 28);
    body.lineBetween(-26, 28, -34, 44);
    body.lineBetween(38, -8, 30, 8);
    body.lineBetween(30, 8, 42, 22);
    body.lineBetween(-78, 8, -70, 26);
    // Contour léger
    body.lineStyle(3, 0x2a2640, 0.85);
    body.strokeRoundedRect(-60, -30, 120, 88, 18);
    body.strokeRoundedRect(-30, -74, 60, 48, 13);

    this.glow = scene.add.graphics();
    this.container = scene.add.container(x, y, [body, this.glow]).setScale(SCALE);
  }

  /** À appeler à chaque image. `timeMs` = horloge de la scène. */
  update(timeMs: number, phaseIndex: number, telegraphing: boolean): void {
    const color = phaseIndex > 0 ? CORE_FURY : CORE_CALM;
    const beat = Math.sin(timeMs / (telegraphing ? 90 : 420));
    const intensity = telegraphing ? 0.75 + 0.25 * beat : 0.7 + 0.3 * beat;

    const g = this.glow;
    g.clear();
    // Cœur
    g.fillStyle(color, 0.22 * intensity);
    g.fillCircle(0, 10, telegraphing ? 44 : 36);
    g.fillStyle(color, 0.95);
    g.fillCircle(0, 10, 15);
    g.fillStyle(0xffffff, 0.55 + 0.45 * intensity);
    g.fillCircle(0, 10, 7);
    // Yeux
    g.fillStyle(color, 0.7 + 0.3 * intensity);
    g.fillRoundedRect(-19, -58, 13, 9, 3);
    g.fillRoundedRect(6, -58, 13, 9, 3);
    // Runes des bras
    g.fillStyle(color, 0.45 * intensity + 0.15);
    g.fillRoundedRect(-86, 4, 16, 5, 2);
    g.fillRoundedRect(-86, 18, 16, 5, 2);
    g.fillRoundedRect(70, 4, 16, 5, 2);
    g.fillRoundedRect(70, 18, 16, 5, 2);

    // Respiration, coup d'impact et tremblement pendant un télégraphe.
    const breathe = Math.sin(timeMs / 700);
    const shake = telegraphing ? Math.sin(timeMs / 22) * 2.2 : 0;
    this.hitPulse = Math.max(0, this.hitPulse - 0.06);
    this.container.setPosition(this.baseX + shake, this.baseY + breathe * 2.5 + this.lungeY);
    this.container.setScale(SCALE * (1 + breathe * 0.008 + this.hitPulse * 0.035), SCALE * (1 + breathe * 0.01 - this.hitPulse * 0.02));
  }

  /** Petit sursaut quand un allié le touche. */
  hit(): void {
    this.hitPulse = 1;
  }

  /** Coup porté : le Golem se penche vers l'équipe puis revient. */
  strike(big: boolean): void {
    this.scene.tweens.add({
      targets: this,
      lungeY: big ? 18 : 9,
      duration: big ? 160 : 100,
      yoyo: true,
      ease: "Quad.easeOut",
    });
  }
}
