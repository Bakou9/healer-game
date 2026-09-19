import type Phaser from "phaser";
import type { PortraitIcon, SkillIcon } from "../../ui/artMap";

type G = Phaser.GameObjects.Graphics;

const WHITE = 0xffffff;

/** Icônes de portrait (rôle du personnage), dessinées centrées sur (0, 0), environ 44 px. */
export function drawPortraitIcon(g: G, icon: PortraitIcon): void {
  g.clear();
  switch (icon) {
    case "shield":
      g.fillStyle(WHITE, 0.95);
      g.fillPoints([{ x: -17, y: -19 }, { x: 17, y: -19 }, { x: 17, y: 3 }, { x: 0, y: 23 }, { x: -17, y: 3 }], true);
      g.fillStyle(0x000000, 0.22);
      g.fillPoints([{ x: 0, y: -19 }, { x: 17, y: -19 }, { x: 17, y: 3 }, { x: 0, y: 23 }], true);
      g.lineStyle(3, 0x000000, 0.28);
      g.strokePoints([{ x: -17, y: -19 }, { x: 17, y: -19 }, { x: 17, y: 3 }, { x: 0, y: 23 }, { x: -17, y: 3 }], true);
      break;
    case "bow": {
      g.lineStyle(4, WHITE, 0.95);
      g.beginPath();
      g.arc(-10, 0, 22, -1.15, 1.15);
      g.strokePath();
      const top = { x: -10 + 22 * Math.cos(-1.15), y: 22 * Math.sin(-1.15) };
      const bottom = { x: -10 + 22 * Math.cos(1.15), y: 22 * Math.sin(1.15) };
      g.lineStyle(2, WHITE, 0.75);
      g.lineBetween(top.x, top.y, bottom.x, bottom.y);
      g.lineStyle(3, 0xe8d9b0, 1);
      g.lineBetween(-12, 0, 20, 0);
      g.fillStyle(WHITE, 1);
      g.fillTriangle(20, -6, 20, 6, 30, 0);
      break;
    }
    case "staff":
      g.lineStyle(4, 0xe8d9b0, 1);
      g.lineBetween(-16, 22, 8, -12);
      g.fillStyle(0xc9b3ff, 0.35);
      g.fillCircle(12, -17, 15);
      g.fillStyle(WHITE, 1);
      g.fillCircle(12, -17, 7);
      g.fillStyle(0xc9b3ff, 0.95);
      g.fillTriangle(12, -30, 8, -22, 16, -22);
      g.fillTriangle(-6, -10, -2, -14, 0, -6);
      break;
    case "cross":
      g.fillStyle(WHITE, 0.96);
      g.fillRoundedRect(-7, -21, 14, 42, 4);
      g.fillRoundedRect(-21, -7, 42, 14, 4);
      break;
  }
}

/** Icônes de sorts, centrées sur (0, 0), environ 36 px. Couleurs : celles du langage visuel (soin vert, bouclier bleu, poison violet). */
export function drawSkillIcon(g: G, icon: SkillIcon): void {
  g.clear();
  switch (icon) {
    case "heal":
      g.lineStyle(2, 0x7cffb2, 0.55);
      g.strokeCircle(0, 0, 18);
      g.fillStyle(0x7cffb2, 1);
      g.fillRoundedRect(-4, -12, 8, 24, 3);
      g.fillRoundedRect(-12, -4, 24, 8, 3);
      break;
    case "wave":
      g.lineStyle(2, 0x7cffb2, 0.3);
      g.strokeCircle(0, 0, 22);
      g.lineStyle(2, 0x7cffb2, 0.6);
      g.strokeCircle(0, 0, 15);
      g.fillStyle(0x7cffb2, 1);
      g.fillRoundedRect(-3, -9, 6, 18, 2);
      g.fillRoundedRect(-9, -3, 18, 6, 2);
      break;
    case "barrier":
      g.fillStyle(0x7cc8ff, 1);
      g.fillPoints([{ x: -15, y: -16 }, { x: 15, y: -16 }, { x: 15, y: 2 }, { x: 0, y: 19 }, { x: -15, y: 2 }], true);
      g.fillStyle(0xffffff, 0.35);
      g.fillPoints([{ x: -15, y: -16 }, { x: 0, y: -16 }, { x: 0, y: 19 }, { x: -15, y: 2 }], true);
      break;
    case "purify":
      g.fillStyle(0xc78cff, 1);
      g.fillCircle(0, 5, 11);
      g.fillTriangle(-10, 1, 10, 1, 0, -17);
      g.fillStyle(0xffffff, 0.85);
      g.fillCircle(-4, 3, 3);
      g.lineStyle(3, 0xffffff, 0.9);
      g.lineBetween(9, -12, 17, -20);
      g.lineBetween(15, -12, 9, -20);
      break;
  }
}
