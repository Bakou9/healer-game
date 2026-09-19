import Phaser from "phaser";
import { BattleScene } from "./scenes/BattleScene";
import { GAME_H, GAME_W, renderScale } from "./ui/layout";

// La toile est agrandie à la résolution de l'appareil (netteté) ; la scène
// dessine en pixels logiques GAME_W × GAME_H grâce au zoom de sa caméra.
const RES = renderScale();

const config: Phaser.Types.Core.GameConfig = {
  type: Phaser.AUTO,
  parent: "app",
  backgroundColor: "#10121a",
  scale: {
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH,
    width: GAME_W * RES,
    height: GAME_H * RES,
  },
  scene: [BattleScene],
};

const game = new Phaser.Game(config);

// Poignée de débogage, uniquement en développement (retirée du build de production).
if (import.meta.env.DEV) {
  (window as unknown as { __game: Phaser.Game }).__game = game;
}
