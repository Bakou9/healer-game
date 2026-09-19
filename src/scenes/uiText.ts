import Phaser from "phaser";
import { renderScale } from "../ui/layout";

const RESOLUTION = renderScale();

/**
 * Crée un texte à la résolution de l'appareil : sans cela, le texte est
 * rastérisé à 1x puis agrandi, donc flou sur les écrans denses (ticket E04-T02).
 * Tout texte du jeu passe par cette fonction.
 */
export function addText(
  scene: Phaser.Scene,
  x: number,
  y: number,
  text: string,
  style: Phaser.Types.GameObjects.Text.TextStyle = {},
): Phaser.GameObjects.Text {
  return scene.add.text(x, y, text, { fontFamily: "sans-serif", resolution: RESOLUTION, ...style });
}
