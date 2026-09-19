import Phaser from "phaser";
import { Battle } from "../sim/Battle";
import { createEncounter } from "../sim/encounter";
import { FixedStepper } from "../sim/fixedStep";
import type { BattleEvent } from "../sim/events";
import skillsData from "../data/skills.json";
import type { SkillDef, UnitState } from "../sim/types";
import { formatNumber, formatRatio, formatSeconds } from "../ui/format";
import { FloatingTextPool } from "./FloatingTextPool";

const SKILLS = skillsData as SkillDef[];

const ROLE_COLOR: Record<UnitState["role"], number> = {
  tank: 0x4a6fa5,
  dps: 0xb5495b,
  healer: 0x4caf7d,
};

const HEALER_ID = "healer";

const COLOR = {
  heal: "#7CFFB2",
  damage: "#FF7C7C",
  poison: "#C78CFF",
  shield: "#7CC8FF",
  boss: "#E6E6E6",
};

const BOSS_ACTION_LABEL: Record<string, string> = {
  attack: "attaque",
  bigAttack: "attaque de zone",
  poison: "poison",
};

const LOG_LINES = 4;

/**
 * Écran de combat. Rend l'état de `Battle` (simulation pure) et transforme
 * les taps du joueur en commandes envoyées au soigneur. Tank et DPS sont en
 * auto-battle : seule la partie soin/survie est jouée manuellement, comme
 * discuté pour la fantasy "healer".
 */
export class BattleScene extends Phaser.Scene {
  private battle!: Battle;
  private stepper = new FixedStepper();
  private paused = false;
  private armedSkillId: string | null = null;

  // Éléments visuels ré-utilisés d'une frame à l'autre.
  private bossRect!: Phaser.GameObjects.Rectangle;
  private bossHpBar!: Phaser.GameObjects.Graphics;
  private bossNameText!: Phaser.GameObjects.Text;
  private telegraphText!: Phaser.GameObjects.Text;

  private allyViews: Array<{
    id: string;
    rect: Phaser.GameObjects.Rectangle;
    hpBar: Phaser.GameObjects.Graphics;
    manaBar: Phaser.GameObjects.Graphics | null;
    nameText: Phaser.GameObjects.Text;
    hpText: Phaser.GameObjects.Text;
    statusText: Phaser.GameObjects.Text;
    manaText: Phaser.GameObjects.Text | null;
  }> = [];

  private skillButtons: Array<{
    def: SkillDef;
    bg: Phaser.GameObjects.Rectangle;
    label: Phaser.GameObjects.Text;
    cooldownText: Phaser.GameObjects.Text;
  }> = [];

  private floating!: FloatingTextPool;
  private phaseBanner!: Phaser.GameObjects.Text;
  private logLines: string[] = [];
  private logText!: Phaser.GameObjects.Text;
  private endOverlay: Phaser.GameObjects.Container | null = null;
  private pauseButton!: Phaser.GameObjects.Text;

  constructor() {
    super("BattleScene");
  }

  create(): void {
    this.paused = false;
    this.armedSkillId = null;
    this.endOverlay = null;
    this.stepper = new FixedStepper();
    // La scène est réutilisée par « Recommencer » : on repart de listes vides.
    this.allyViews = [];
    this.skillButtons = [];
    this.logLines = [];

    // Seed fixe pour l'instant : à terme, tirée par le serveur pour chaque run.
    this.battle = new Battle(createEncounter(Date.now() % 100000));

    this.buildBossUi();
    this.buildAlliesUi();
    this.buildSkillBar();
    this.buildTopBar();

    this.floating = new FloatingTextPool(this);
    this.phaseBanner = this.add
      .text(this.scale.width / 2, 250, "", {
        fontFamily: "sans-serif",
        fontSize: "26px",
        fontStyle: "bold",
        color: "#ffb347",
        stroke: "#000000",
        strokeThickness: 5,
      })
      .setOrigin(0.5)
      .setAlpha(0)
      .setDepth(60);
    // Observer : la scène réagit aux événements de la simulation, qui ne la connaît pas.
    const unsubscribe = this.battle.subscribe((event) => this.onBattleEvent(event));
    this.events.once(Phaser.Scenes.Events.SHUTDOWN, unsubscribe);

    // Sous les cartes d'alliés, dans l'espace libre au-dessus de la barre de compétences.
    this.logText = this.add.text(24, 450, "", {
      fontFamily: "monospace",
      fontSize: "14px",
      color: "#8a8fa3",
      lineSpacing: 6,
    });
  }

  update(_time: number, deltaMs: number): void {
    if (!this.paused && this.battle.getResult() === "ongoing") {
      // Pas fixe : la simulation ne dépend pas du FPS de l'appareil.
      this.stepper.advance(deltaMs, (dt) => this.battle.step(dt));
    }
    this.render();
    if (this.battle.getResult() !== "ongoing" && !this.endOverlay) {
      this.showEndOverlay(this.battle.getResult() === "victory");
    }
  }

  // ---- Construction de l'UI ---------------------------------------------

  private buildTopBar(): void {
    this.pauseButton = this.add
      .text(this.scale.width - 60, 16, "⏸", { fontSize: "28px", color: "#ffffff" })
      .setInteractive({ useHandCursor: true })
      .on("pointerdown", () => {
        this.paused = !this.paused;
        this.pauseButton.setText(this.paused ? "▶" : "⏸");
      });
  }

  private buildBossUi(): void {
    this.bossNameText = this.add
      .text(this.scale.width / 2, 40, "", {
        fontFamily: "sans-serif",
        fontSize: "18px",
        color: "#ffffff",
      })
      .setOrigin(0.5, 0);

    this.bossRect = this.add.rectangle(this.scale.width / 2, 130, 140, 140, 0x5a4a6a);
    this.bossHpBar = this.add.graphics();

    this.telegraphText = this.add
      .text(this.scale.width / 2, 210, "", {
        fontFamily: "sans-serif",
        fontSize: "16px",
        color: "#ff5b5b",
        fontStyle: "bold",
      })
      .setOrigin(0.5, 0);
  }

  private buildAlliesUi(): void {
    const allies = this.battle.getAllies();
    const boxWidth = 100;
    const gap = 10;
    const totalWidth = allies.length * boxWidth + (allies.length - 1) * gap;
    const startX = (this.scale.width - totalWidth) / 2 + boxWidth / 2;
    const y = 340;

    allies.forEach((ally, i) => {
      const x = startX + i * (boxWidth + gap);
      const rect = this.add.rectangle(x, y, boxWidth - 8, 100, ROLE_COLOR[ally.role]);
      const nameText = this.add
        .text(x, y - 62, ally.name, { fontSize: "12px", color: "#ffffff" })
        .setOrigin(0.5, 0);
      const hpBar = this.add.graphics();
      const manaBar = ally.role === "healer" ? this.add.graphics() : null;
      const hpText = this.add.text(x, y - 14, "", { fontSize: "13px", color: "#ffffff" }).setOrigin(0.5);
      const statusText = this.add.text(x, y + 6, "", { fontSize: "11px", color: COLOR.poison }).setOrigin(0.5);
      const manaText = manaBar
        ? this.add.text(x, y + 62, "", { fontSize: "11px", color: "#8fc9ff" }).setOrigin(0.5)
        : null;

      // Tap sur un allié = cible pour la compétence armée (le cas échéant).
      rect.setInteractive({ useHandCursor: true }).on("pointerdown", () => {
        if (this.armedSkillId) {
          this.castArmedSkill(ally.id);
        }
      });

      this.allyViews.push({ id: ally.id, rect, hpBar, manaBar, nameText, hpText, statusText, manaText });
    });
  }

  private buildSkillBar(): void {
    const boxWidth = 104;
    const gap = 8;
    const totalWidth = SKILLS.length * boxWidth + (SKILLS.length - 1) * gap;
    const startX = (this.scale.width - totalWidth) / 2 + boxWidth / 2;
    const y = this.scale.height - 90;

    SKILLS.forEach((skill, i) => {
      const x = startX + i * (boxWidth + gap);
      const bg = this.add
        .rectangle(x, y, boxWidth - 6, 76, 0x2a2d3a)
        .setStrokeStyle(2, 0x555a70)
        .setInteractive({ useHandCursor: true });

      const label = this.add
        .text(x, y - 10, `${skill.name}\n${skill.manaCost} mana`, {
          fontSize: "11px",
          color: "#ffffff",
          align: "center",
        })
        .setOrigin(0.5, 0.5);

      const cooldownText = this.add
        .text(x, y + 24, "", { fontSize: "14px", color: "#ff9d9d" })
        .setOrigin(0.5, 0.5);

      bg.on("pointerdown", () => this.onSkillButtonTapped(skill));

      this.skillButtons.push({ def: skill, bg, label, cooldownText });
    });
  }

  // ---- Interaction --------------------------------------------------------

  private onSkillButtonTapped(skill: SkillDef): void {
    if (this.battle.getResult() !== "ongoing") return;
    if (!this.battle.canUseSkillNow(HEALER_ID, skill.id)) return;

    if (skill.target === "all") {
      this.battle.issueCommand({ timeMs: this.battle.getClock(), skillId: skill.id });
      this.armedSkillId = null;
      return;
    }

    // Compétence ciblée : on "l'arme", le prochain tap sur un allié la lance.
    this.armedSkillId = this.armedSkillId === skill.id ? null : skill.id;
  }

  private castArmedSkill(targetId: string): void {
    if (!this.armedSkillId) return;
    this.battle.issueCommand({
      timeMs: this.battle.getClock(),
      skillId: this.armedSkillId,
      targetId,
    });
    this.armedSkillId = null;
  }

  // ---- Rendu ---------------------------------------------------------------

  private render(): void {
    this.renderBoss();
    this.renderAllies();
    this.renderSkillBar();
    this.renderLog();
  }

  private renderBoss(): void {
    const hp = this.battle.getBossHp();
    const maxHp = this.battle.getBossMaxHp();
    const phase = this.battle.getBossPhase();
    const phaseSuffix = phase.index > 0 ? `  · ${phase.name}` : "";
    this.bossNameText.setText(
      `${this.battle.getBossName()}  ${formatNumber(hp)} / ${formatNumber(maxHp)}${phaseSuffix}`,
    );

    const barWidth = 260;
    const x = this.scale.width / 2 - barWidth / 2;
    const y = 200;
    this.bossHpBar.clear();
    this.bossHpBar.fillStyle(0x000000, 0.4).fillRect(x, y, barWidth, 10);
    this.bossHpBar.fillStyle(0xd9455f, 1).fillRect(x, y, barWidth * (hp / maxHp), 10);

    const telegraph = this.battle.getTelegraph();
    if (telegraph?.type === "bigAttack") {
      this.telegraphText.setText(`⚠ Attaque de zone dans ${formatSeconds(telegraph.msRemaining)}`);
      this.bossRect.setStrokeStyle(4, 0xff5b5b);
    } else {
      this.telegraphText.setText("");
      this.bossRect.setStrokeStyle();
    }
  }

  private renderAllies(): void {
    const allies = this.battle.getAllies();
    for (const view of this.allyViews) {
      const ally = allies.find((a) => a.id === view.id);
      if (!ally) continue;

      view.rect.setAlpha(ally.alive ? 1 : 0.25);
      view.rect.setStrokeStyle(this.armedSkillId ? 3 : 0, 0xffe066);

      const barWidth = 84;
      const x = view.rect.x - barWidth / 2;

      view.hpBar.clear();
      view.hpBar.fillStyle(0x000000, 0.5).fillRect(x, view.rect.y + 30, barWidth, 8);
      view.hpBar
        .fillStyle(0x5fd35f, 1)
        .fillRect(x, view.rect.y + 30, barWidth * Math.max(0, ally.hp / ally.maxHp), 8);

      if (view.manaBar) {
        view.manaBar.clear();
        view.manaBar.fillStyle(0x000000, 0.5).fillRect(x, view.rect.y + 42, barWidth, 6);
        view.manaBar
          .fillStyle(0x4aa8ff, 1)
          .fillRect(x, view.rect.y + 42, barWidth * (ally.mana / (ally.maxMana || 1)), 6);
      }

      const shieldSuffix = ally.shield > 0 ? ` 🛡${formatNumber(ally.shield)}` : "";
      view.nameText.setText(`${ally.name}${shieldSuffix}`);
      view.hpText.setText(ally.alive ? formatRatio(ally.hp, ally.maxHp) : "K.O.");
      view.statusText.setText(ally.effects.map((e) => `${e.name} ${formatSeconds(e.msRemaining)}`).join("\n"));
      view.manaText?.setText(`Mana ${formatRatio(ally.mana, ally.maxMana)}`);
    }
  }

  private renderSkillBar(): void {
    for (const button of this.skillButtons) {
      const usable = this.battle.canUseSkillNow(HEALER_ID, button.def.id);
      const cooldownMs = this.battle.getCooldownRemaining(HEALER_ID, button.def.id);
      const armed = this.armedSkillId === button.def.id;

      button.bg.setFillStyle(armed ? 0x445577 : 0x2a2d3a);
      button.bg.setAlpha(usable || armed ? 1 : 0.5);
      button.cooldownText.setText(cooldownMs > 0 ? formatSeconds(cooldownMs) : "");
    }
  }

  private renderLog(): void {
    this.logText.setText(this.logLines.join("\n"));
  }

  // ---- Réaction aux événements de combat (Observer) --------------------------

  private allyPosition(unitId: string): { x: number; y: number } {
    const view = this.allyViews.find((v) => v.id === unitId);
    return view ? { x: view.rect.x, y: view.rect.y - 30 } : { x: this.scale.width / 2, y: 340 };
  }

  private allyName(unitId: string): string {
    return this.battle.getAllies().find((a) => a.id === unitId)?.name ?? unitId;
  }

  private pushLog(timeMs: number, text: string): void {
    this.logLines.push(`${formatSeconds(timeMs)}  ${text}`);
    if (this.logLines.length > LOG_LINES) this.logLines.shift();
  }

  private onBattleEvent(event: BattleEvent): void {
    switch (event.type) {
      case "healed":
        if (event.amount > 0) {
          const { x, y } = this.allyPosition(event.unitId);
          this.floating.spawn(x, y, `+${formatNumber(event.amount)}`, { color: COLOR.heal });
        }
        break;
      case "shielded": {
        const { x, y } = this.allyPosition(event.unitId);
        this.floating.spawn(x, y, `Bouclier +${formatNumber(event.amount)}`, { color: COLOR.shield, sizePx: 14 });
        break;
      }
      case "unitDamaged": {
        const { x, y } = this.allyPosition(event.unitId);
        if (event.amount > 0) this.floating.spawn(x, y, `-${formatNumber(event.amount)}`, { color: COLOR.damage });
        else this.floating.spawn(x, y, "Absorbé", { color: COLOR.shield, sizePx: 14 });
        break;
      }
      case "effectTick": {
        const { x, y } = this.allyPosition(event.unitId);
        if (event.amount > 0) {
          this.floating.spawn(x, y, `-${formatNumber(event.amount)}`, { color: COLOR.poison, sizePx: 14 });
        }
        break;
      }
      case "effectApplied": {
        const { x, y } = this.allyPosition(event.unitId);
        this.floating.spawn(x, y - 18, "Empoisonné !", { color: COLOR.poison, sizePx: 14 });
        break;
      }
      case "effectEnded":
        if (event.reason === "cleansed") {
          const { x, y } = this.allyPosition(event.unitId);
          this.floating.spawn(x, y - 18, "Purgé", { color: COLOR.heal, sizePx: 14 });
        }
        break;
      case "bossDamaged":
        this.floating.spawn(this.scale.width / 2 + this.bossHitOffset(event.sourceId), 100, formatNumber(event.amount), {
          color: COLOR.boss,
          sizePx: 12,
        });
        break;
      case "skillUsed": {
        const skill = SKILLS.find((s) => s.id === event.skillId);
        const targets =
          event.targetIds.length > 1 ? "toute l'équipe" : this.allyName(event.targetIds[0] ?? event.casterId);
        this.pushLog(event.timeMs, `${skill?.name ?? event.skillId} → ${targets}`);
        break;
      }
      case "bossAction":
        this.pushLog(event.timeMs, `Le boss : ${BOSS_ACTION_LABEL[event.action] ?? event.action}`);
        break;
      case "unitDied":
        this.pushLog(event.timeMs, `${this.allyName(event.unitId)} est K.O.`);
        break;
      case "bossPhaseChanged":
        this.pushLog(event.timeMs, `Le boss passe en phase « ${event.name} »`);
        this.showPhaseBanner(`Phase ${event.phase + 1} — ${event.name} !`);
        break;
      default:
        break;
    }
  }

  /** Décale les chiffres de dégâts sur le boss selon l'attaquant, pour qu'ils ne se superposent pas. */
  private bossHitOffset(sourceId: string): number {
    const index = this.allyViews.findIndex((v) => v.id === sourceId);
    return (index - 1) * 36;
  }

  private showPhaseBanner(text: string): void {
    this.phaseBanner.setText(text).setAlpha(1);
    this.tweens.add({ targets: this.phaseBanner, alpha: 0, delay: 1400, duration: 700 });
  }

  private showEndOverlay(victory: boolean): void {
    const container = this.add.container(0, 0);
    const bg = this.add.rectangle(
      this.scale.width / 2,
      this.scale.height / 2,
      this.scale.width,
      this.scale.height,
      0x000000,
      0.75,
    );
    const title = this.add
      .text(this.scale.width / 2, this.scale.height / 2 - 40, victory ? "Victoire !" : "Défaite…", {
        fontSize: "32px",
        color: victory ? "#7CFFB2" : "#FF7C7C",
        fontStyle: "bold",
      })
      .setOrigin(0.5);
    const retry = this.add
      .text(this.scale.width / 2, this.scale.height / 2 + 20, "Recommencer", {
        fontSize: "18px",
        color: "#ffffff",
        backgroundColor: "#333652",
        padding: { x: 16, y: 8 },
      })
      .setOrigin(0.5)
      .setInteractive({ useHandCursor: true })
      .on("pointerdown", () => this.scene.restart());

    container.add([bg, title, retry]);
    this.endOverlay = container;
  }
}
