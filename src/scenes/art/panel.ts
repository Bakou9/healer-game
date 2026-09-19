import type Phaser from "phaser";
import type { Rect } from "../../ui/layout";

export interface PanelStyle {
  fill: number;
  stroke: number;
  strokeWidth?: number;
  /** Opacité globale (ex. allié K.O.). */
  alpha?: number;
  /** Halo extérieur (ex. cible sélectionnée). */
  glow?: number;
}

const RADIUS = 12;

/** Panneau à coins arrondis avec léger reflet en haut : la base visuelle des cartes et boutons. */
export function drawPanel(g: Phaser.GameObjects.Graphics, r: Rect, style: PanelStyle): void {
  const alpha = style.alpha ?? 1;
  g.clear();
  if (style.glow !== undefined) {
    g.lineStyle(12, style.glow, 0.28 * alpha);
    g.strokeRoundedRect(r.x - 3, r.y - 3, r.w + 6, r.h + 6, RADIUS + 3);
  }
  g.fillStyle(style.fill, alpha);
  g.fillRoundedRect(r.x, r.y, r.w, r.h, RADIUS);
  g.fillStyle(0xffffff, 0.07 * alpha);
  g.fillRoundedRect(r.x + 3, r.y + 3, r.w - 6, r.h * 0.22, RADIUS - 3);
  g.lineStyle(style.strokeWidth ?? 2, style.stroke, alpha);
  g.strokeRoundedRect(r.x, r.y, r.w, r.h, RADIUS);
}
