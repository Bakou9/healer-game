import Phaser from "phaser";
import { FONT } from "../ui/layout";
import { addText } from "./uiText";

export interface FloatingTextStyle {
  color: string;
  sizePx?: number;
}

/**
 * Réserve de textes flottants réutilisés (patron Object Pool) : chaque soin ou
 * coup affiche un chiffre, sans créer/détruire un objet Phaser à chaque
 * événement (ce qui provoquerait des saccades sur un mobile d'entrée de gamme).
 */
export class FloatingTextPool {
  private free: Phaser.GameObjects.Text[] = [];
  private spawnCount = 0;

  constructor(private readonly scene: Phaser.Scene) {}

  spawn(x: number, y: number, text: string, { color, sizePx = FONT.body }: FloatingTextStyle): void {
    const label =
      this.free.pop() ??
      addText(this.scene, 0, 0, "", { fontStyle: "bold", stroke: "#000000", strokeThickness: 3 })
        .setOrigin(0.5)
        .setDepth(50);

    // Léger décalage cyclique pour que des chiffres simultanés ne se superposent pas.
    const offsetX = ((this.spawnCount % 3) - 1) * 16;
    this.spawnCount += 1;

    label
      .setText(text)
      .setColor(color)
      // Jamais sous la taille minimale de lisibilité.
      .setFontSize(Math.max(sizePx, FONT.small))
      .setPosition(x + offsetX, y)
      .setAlpha(1)
      .setVisible(true);

    this.scene.tweens.add({
      targets: label,
      y: y - 44,
      alpha: 0,
      duration: 900,
      ease: "Cubic.easeOut",
      onComplete: () => {
        label.setVisible(false);
        this.free.push(label);
      },
    });
  }
}
