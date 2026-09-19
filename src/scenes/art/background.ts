import type Phaser from "phaser";
import { createRng } from "../../sim/rng";
import { GAME_H, GAME_W, ZONES } from "../../ui/layout";

/**
 * Décor de fond : dégradé sombre, lueur d'ambiance derrière le boss, sol et
 * poussières lumineuses. Statique (dessiné une fois), donc sans coût par image.
 * Les poussières sont placées avec le RNG seedé : le décor est identique à chaque lancement.
 */
export function drawBackground(scene: Phaser.Scene): void {
  const g = scene.add.graphics().setDepth(-10);

  g.fillGradientStyle(0x090b13, 0x090b13, 0x1c1a36, 0x1c1a36, 1);
  g.fillRect(0, 0, GAME_W, GAME_H);

  // Lueur d'ambiance derrière le boss.
  const glowY = ZONES.boss.y + 78;
  g.fillStyle(0x3a2f66, 0.16);
  g.fillCircle(GAME_W / 2, glowY, 170);
  g.fillStyle(0x4a3a86, 0.16);
  g.fillCircle(GAME_W / 2, glowY, 115);
  g.fillStyle(0x6a52b8, 0.12);
  g.fillCircle(GAME_W / 2, glowY, 70);

  // Sol du boss.
  g.fillStyle(0x000000, 0.4);
  g.fillEllipse(GAME_W / 2, ZONES.boss.y + 150, 230, 30);

  // Poussières lumineuses.
  const rng = createRng(7);
  for (let i = 0; i < 46; i++) {
    const x = rng() * GAME_W;
    const y = rng() * (ZONES.team.y - 10);
    const size = rng() < 0.2 ? 2 : 1;
    g.fillStyle(0xbfc6ff, 0.12 + rng() * 0.28);
    g.fillRect(x, y, size, size);
  }

  // Bas de l'écran : léger voile pour détacher l'équipe et les sorts.
  g.fillStyle(0x000000, 0.22);
  g.fillRect(0, ZONES.team.y - 12, GAME_W, GAME_H - ZONES.team.y + 12);
}
