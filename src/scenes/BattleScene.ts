import Phaser from "phaser";
import { Battle } from "../sim/Battle";
import { createEncounter } from "../sim/encounter";
import { FixedStepper } from "../sim/fixedStep";
import type { BattleEvent } from "../sim/events";
import skillsData from "../data/skills.json";
import type { SkillDef, UnitState } from "../sim/types";
import { formatNumber, formatRatio, formatSeconds } from "../ui/format";
import {
  BOSS_HP_BAR,
  COLOR,
  FONT,
  GAME_H,
  GAME_W,
  PAUSE_BUTTON,
  ZONES,
  hpColor,
  renderScale,
  skillButtonRects,
  teamCardRects,
  type Rect,
} from "../ui/layout";
import { TargetSelection } from "../ui/targeting";
import { FloatingTextPool } from "./FloatingTextPool";
import { addText } from "./uiText";

const SKILLS = skillsData as SkillDef[];
const HEALER_ID = "healer";

const ROLE_LABEL: Record<UnitState["role"], string> = { tank: "Tank", dps: "Dégâts", healer: "Soin" };

const BOSS_ACTION_LABEL: Record<string, string> = {
  attack: "attaque",
  bigAttack: "attaque de zone",
  poison: "poison",
};

const LOG_LINES = 3;
const HINT_DURATION_MS = 1500;

const center = (r: Rect) => ({ x: r.x + r.w / 2, y: r.y + r.h / 2 });

/**
 * Écran de combat. Rend l'état de `Battle` (simulation pure) et transforme
 * les taps du joueur en commandes envoyées au soigneur. Tank et DPS sont en
 * auto-battle : seule la partie soin/survie est jouée manuellement.
 *
 * Mise en page : voir `ui/layout.ts` et `docs/UX.md`. Ciblage : toucher un
 * allié le sélectionne, puis toucher un sort (`ui/targeting.ts`).
 */
export class BattleScene extends Phaser.Scene {
  private battle!: Battle;
  private stepper = new FixedStepper();
  private selection = new TargetSelection();
  private paused = false;
  private hintUntilMs = 0;

  private bossRect!: Phaser.GameObjects.Rectangle;
  private bossHpBar!: Phaser.GameObjects.Graphics;
  private bossNameText!: Phaser.GameObjects.Text;
  private telegraphText!: Phaser.GameObjects.Text;

  private allyViews: Array<{
    id: string;
    rect: Rect;
    bg: Phaser.GameObjects.Rectangle;
    portrait: Phaser.GameObjects.Rectangle;
    hpBar: Phaser.GameObjects.Graphics;
    nameText: Phaser.GameObjects.Text;
    hpText: Phaser.GameObjects.Text;
    koText: Phaser.GameObjects.Text;
    shieldText: Phaser.GameObjects.Text;
    statusText: Phaser.GameObjects.Text;
  }> = [];

  private skillButtons: Array<{
    def: SkillDef;
    bg: Phaser.GameObjects.Rectangle;
    costText: Phaser.GameObjects.Text;
    cooldownText: Phaser.GameObjects.Text;
  }> = [];

  private targetText!: Phaser.GameObjects.Text;
  private manaBar!: Phaser.GameObjects.Graphics;
  private manaText!: Phaser.GameObjects.Text;
  private floating!: FloatingTextPool;
  private phaseBanner!: Phaser.GameObjects.Text;
  private logLines: string[] = [];
  private logText!: Phaser.GameObjects.Text;
  private endOverlay: Phaser.GameObjects.Container | null = null;
  private pauseLabel!: Phaser.GameObjects.Text;

  constructor() {
    super("BattleScene");
  }

  create(): void {
    this.paused = false;
    this.endOverlay = null;
    this.stepper = new FixedStepper();
    this.selection = new TargetSelection();
    this.hintUntilMs = 0;
    // La scène est réutilisée par « Recommencer » : on repart de listes vides.
    this.allyViews = [];
    this.skillButtons = [];
    this.logLines = [];

    // Toile agrandie à la résolution de l'appareil : on dessine en pixels logiques (480×854).
    const camera = this.cameras.main;
    camera.setOrigin(0, 0);
    camera.setZoom(renderScale());

    // Seed fixe pour l'instant : à terme, tirée par le serveur pour chaque run.
    this.battle = new Battle(createEncounter(Date.now() % 100000));

    this.buildTopBar();
    this.buildBossUi();
    this.buildBand();
    this.buildTeam();
    this.buildStrip();
    this.buildSkillBar();

    this.floating = new FloatingTextPool(this);
    this.phaseBanner = addText(this, GAME_W / 2, ZONES.band.y + 24, "", {
      fontSize: `${FONT.banner}px`,
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
  }

  update(_time: number, deltaMs: number): void {
    if (!this.paused && this.battle.getResult() === "ongoing") {
      // Pas fixe : la simulation ne dépend pas du FPS de l'appareil.
      this.stepper.advance(deltaMs, (dt) => this.battle.step(dt));
    }
    this.selection.sync(this.battle.getAllies().filter((a) => a.alive).map((a) => a.id));
    this.render();
    if (this.battle.getResult() !== "ongoing" && !this.endOverlay) {
      this.showEndOverlay(this.battle.getResult() === "victory");
    }
  }

  // ---- Construction de l'UI ---------------------------------------------

  private buildTopBar(): void {
    const bg = this.add
      .rectangle(
        PAUSE_BUTTON.x + PAUSE_BUTTON.w / 2,
        PAUSE_BUTTON.y + PAUSE_BUTTON.h / 2,
        PAUSE_BUTTON.w,
        PAUSE_BUTTON.h,
        COLOR.panel,
      )
      .setStrokeStyle(2, COLOR.panelStroke)
      .setInteractive({ useHandCursor: true });
    this.pauseLabel = addText(this, center(PAUSE_BUTTON).x, center(PAUSE_BUTTON).y, "⏸", {
      fontSize: `${FONT.banner}px`,
      color: COLOR.text,
    }).setOrigin(0.5);
    bg.on("pointerdown", () => {
      this.paused = !this.paused;
      this.pauseLabel.setText(this.paused ? "▶" : "⏸");
    });
  }

  private buildBossUi(): void {
    this.bossNameText = addText(this, BOSS_HP_BAR.x + BOSS_HP_BAR.w / 2, ZONES.topBar.y + 2, "", {
      fontSize: `${FONT.strong}px`,
      fontStyle: "bold",
      color: COLOR.text,
    }).setOrigin(0.5, 0);

    this.bossHpBar = this.add.graphics();

    this.bossRect = this.add.rectangle(GAME_W / 2, ZONES.boss.y + 76, 160, 120, 0x5a4a6a);

    this.telegraphText = addText(this, GAME_W / 2, ZONES.boss.y + 154, "", {
      fontSize: `${FONT.strong}px`,
      fontStyle: "bold",
      color: COLOR.danger,
    }).setOrigin(0.5, 0);
  }

  private buildBand(): void {
    this.logText = addText(this, ZONES.band.x, ZONES.band.y, "", {
      fontSize: `${FONT.small}px`,
      color: COLOR.textMuted,
      lineSpacing: 3,
    });
  }

  private buildTeam(): void {
    const allies = this.battle.getAllies();
    const rects = teamCardRects(allies.length);

    allies.forEach((ally, i) => {
      const rect = rects[i];
      const { x: cx } = center(rect);
      const bg = this.add
        .rectangle(cx, center(rect).y, rect.w, rect.h, COLOR.panel)
        .setStrokeStyle(2, COLOR.panelStroke)
        .setInteractive({ useHandCursor: true });

      const nameText = addText(this, cx, rect.y + 8, ally.name, {
        fontSize: `${FONT.body}px`,
        fontStyle: "bold",
        color: COLOR.text,
      }).setOrigin(0.5, 0);
      addText(this, cx, rect.y + 30, ROLE_LABEL[ally.role], {
        fontSize: `${FONT.small}px`,
        color: COLOR.textMuted,
      }).setOrigin(0.5, 0);

      const portrait = this.add.rectangle(cx, rect.y + 82, rect.w - 24, 52, COLOR.role[ally.role]);
      const koText = addText(this, cx, rect.y + 82, "K.O.", {
        fontSize: `${FONT.title}px`,
        fontStyle: "bold",
        color: COLOR.text,
      })
        .setOrigin(0.5)
        .setVisible(false);

      const hpBar = this.add.graphics();
      const hpText = addText(this, cx, rect.y + 136, "", {
        fontSize: `${FONT.body}px`,
        color: COLOR.text,
      }).setOrigin(0.5, 0);
      const shieldText = addText(this, cx, rect.y + 160, "", {
        fontSize: `${FONT.small}px`,
        color: COLOR.shield,
      }).setOrigin(0.5, 0);
      const statusText = addText(this, cx, rect.y + 176, "", {
        fontSize: `${FONT.small}px`,
        fontStyle: "bold",
        color: "#ffffff",
        backgroundColor: "#5b2f86",
        padding: { x: 6, y: 1 },
      })
        .setOrigin(0.5, 0)
        .setVisible(false);

      // Toucher un allié = le sélectionner (ou annuler la sélection).
      bg.on("pointerdown", () => {
        const current = this.battle.getAllies().find((a) => a.id === ally.id);
        this.selection.tap(ally.id, !!current?.alive);
      });

      this.allyViews.push({ id: ally.id, rect, bg, portrait, hpBar, nameText, hpText, koText, shieldText, statusText });
    });
  }

  private buildStrip(): void {
    this.targetText = addText(this, ZONES.strip.x, ZONES.strip.y + 2, "", {
      fontSize: `${FONT.body}px`,
      color: COLOR.text,
    });
    this.manaBar = this.add.graphics();
    this.manaText = addText(this, GAME_W / 2, ZONES.strip.y + 46, "", {
      fontSize: `${FONT.small}px`,
      fontStyle: "bold",
      color: COLOR.text,
    }).setOrigin(0.5);
  }

  private buildSkillBar(): void {
    const rects = skillButtonRects(SKILLS.length);
    SKILLS.forEach((skill, i) => {
      const rect = rects[i];
      const { x: cx, y: cy } = center(rect);
      const bg = this.add
        .rectangle(cx, cy, rect.w, rect.h, COLOR.panel)
        .setStrokeStyle(2, COLOR.panelStroke)
        .setInteractive({ useHandCursor: true });

      addText(this, cx, rect.y + 12, skill.name, {
        fontSize: `${FONT.body}px`,
        fontStyle: "bold",
        color: COLOR.text,
        align: "center",
        wordWrap: { width: rect.w - 12 },
      }).setOrigin(0.5, 0);
      const costText = addText(this, cx, rect.y + 72, `${skill.manaCost} mana`, {
        fontSize: `${FONT.small}px`,
        color: COLOR.textMuted,
      }).setOrigin(0.5, 0);
      addText(this, cx, rect.y + 94, skill.target === "all" ? "Toute l'équipe" : "1 allié", {
        fontSize: `${FONT.small}px`,
        color: COLOR.textMuted,
      }).setOrigin(0.5, 0);
      const cooldownText = addText(this, cx, rect.y + 136, "", {
        fontSize: `${FONT.cooldown}px`,
        fontStyle: "bold",
        color: "#ff9d9d",
      }).setOrigin(0.5);

      bg.on("pointerdown", () => this.onSkillButtonTapped(skill));
      this.skillButtons.push({ def: skill, bg, costText, cooldownText });
    });
  }

  // ---- Interaction --------------------------------------------------------

  private onSkillButtonTapped(skill: SkillDef): void {
    if (this.battle.getResult() !== "ongoing" || this.paused) return;
    const resolution = this.selection.resolve(skill, this.battle.canUseSkillNow(HEALER_ID, skill.id));
    if (resolution.kind === "needTarget") {
      this.hintUntilMs = this.battle.getClock() + HINT_DURATION_MS;
    } else if (resolution.kind === "cast") {
      this.battle.issueCommand({
        timeMs: this.battle.getClock(),
        skillId: skill.id,
        targetId: resolution.targetId,
      });
    }
  }

  // ---- Rendu ---------------------------------------------------------------

  private render(): void {
    this.renderBoss();
    this.renderAllies();
    this.renderStrip();
    this.renderSkillBar();
    this.logText.setText(this.logLines.join("\n"));
  }

  private renderBoss(): void {
    const hp = this.battle.getBossHp();
    const maxHp = this.battle.getBossMaxHp();
    const phase = this.battle.getBossPhase();
    const phaseSuffix = phase.index > 0 ? `  · ${phase.name}` : "";
    this.bossNameText.setText(
      `${this.battle.getBossName()}  ${formatNumber(hp)} / ${formatNumber(maxHp)}${phaseSuffix}`,
    );

    const bar = BOSS_HP_BAR;
    this.bossHpBar.clear();
    this.bossHpBar.fillStyle(0x000000, 0.4).fillRect(bar.x, bar.y, bar.w, bar.h);
    this.bossHpBar.fillStyle(0xd9455f, 1).fillRect(bar.x, bar.y, bar.w * (hp / maxHp), bar.h);

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

      const selected = this.selection.selected === ally.id;
      view.bg.setStrokeStyle(selected ? 5 : 2, selected ? COLOR.selected : COLOR.panelStroke);
      view.bg.setAlpha(ally.alive ? 1 : 0.45);
      view.portrait.setAlpha(ally.alive ? 1 : 0.3);
      view.koText.setVisible(!ally.alive);

      const ratio = Math.max(0, ally.hp / ally.maxHp);
      const bar = { x: view.rect.x + 12, y: view.rect.y + 112, w: view.rect.w - 24, h: 18 };
      view.hpBar.clear();
      view.hpBar.fillStyle(0x000000, 0.5).fillRect(bar.x, bar.y, bar.w, bar.h);
      view.hpBar.fillStyle(hpColor(ratio), 1).fillRect(bar.x, bar.y, bar.w * ratio, bar.h);

      view.hpText.setText(ally.alive ? formatRatio(ally.hp, ally.maxHp) : "");
      view.shieldText.setText(ally.shield > 0 ? `Bouclier ${formatNumber(ally.shield)}` : "");

      const effect = ally.effects[0];
      view.statusText.setVisible(!!effect);
      if (effect) {
        const more = ally.effects.length > 1 ? ` +${ally.effects.length - 1}` : "";
        view.statusText.setText(`${effect.name} ${formatSeconds(effect.msRemaining)}${more}`);
      }
    }
  }

  private renderStrip(): void {
    const healer = this.battle.getAllies().find((a) => a.id === HEALER_ID);
    const selectedId = this.selection.selected;
    const selectedName = this.battle.getAllies().find((a) => a.id === selectedId)?.name;

    if (this.battle.getClock() < this.hintUntilMs) {
      this.targetText.setText("Choisissez d'abord un allié !").setColor(COLOR.danger);
    } else if (selectedName) {
      this.targetText.setText(`Cible : ${selectedName}`).setColor(COLOR.text);
    } else {
      this.targetText.setText("Touchez un allié pour le cibler").setColor(COLOR.textMuted);
    }

    const bar = { x: ZONES.strip.x, y: ZONES.strip.y + 34, w: ZONES.strip.w, h: 24 };
    const maxMana = healer?.maxMana ?? 1;
    const mana = healer?.mana ?? 0;
    this.manaBar.clear();
    this.manaBar.fillStyle(0x000000, 0.5).fillRect(bar.x, bar.y, bar.w, bar.h);
    this.manaBar.fillStyle(COLOR.mana, 1).fillRect(bar.x, bar.y, bar.w * (mana / maxMana), bar.h);
    this.manaText.setText(`Mana ${formatRatio(mana, maxMana)}`);
  }

  private renderSkillBar(): void {
    const healer = this.battle.getAllies().find((a) => a.id === HEALER_ID);
    for (const button of this.skillButtons) {
      const usable = this.battle.canUseSkillNow(HEALER_ID, button.def.id);
      const cooldownMs = this.battle.getCooldownRemaining(HEALER_ID, button.def.id);
      const enoughMana = (healer?.mana ?? 0) >= button.def.manaCost;

      button.bg.setAlpha(usable ? 1 : 0.55);
      button.costText.setColor(enoughMana ? COLOR.textMuted : COLOR.damage);
      button.cooldownText.setText(cooldownMs > 0 ? formatSeconds(cooldownMs) : "");
    }
  }

  // ---- Réaction aux événements de combat (Observer) --------------------------

  private allyPosition(unitId: string): { x: number; y: number } {
    const view = this.allyViews.find((v) => v.id === unitId);
    return view ? { x: center(view.rect).x, y: view.rect.y + 60 } : { x: GAME_W / 2, y: ZONES.team.y };
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
        this.floating.spawn(x, y, `+${formatNumber(event.amount)}`, { color: COLOR.shield });
        break;
      }
      case "unitDamaged": {
        const { x, y } = this.allyPosition(event.unitId);
        if (event.amount > 0) this.floating.spawn(x, y, `-${formatNumber(event.amount)}`, { color: COLOR.damage });
        else this.floating.spawn(x, y, "Absorbé", { color: COLOR.shield });
        break;
      }
      case "effectTick": {
        const { x, y } = this.allyPosition(event.unitId);
        if (event.amount > 0) this.floating.spawn(x, y, `-${formatNumber(event.amount)}`, { color: COLOR.poison });
        break;
      }
      case "effectApplied": {
        const { x, y } = this.allyPosition(event.unitId);
        this.floating.spawn(x, y - 20, "Empoisonné !", { color: COLOR.poison });
        break;
      }
      case "effectEnded":
        if (event.reason === "cleansed") {
          const { x, y } = this.allyPosition(event.unitId);
          this.floating.spawn(x, y - 20, "Purgé", { color: COLOR.heal });
        }
        break;
      case "bossDamaged":
        this.floating.spawn(GAME_W / 2 + this.bossHitOffset(event.sourceId), ZONES.boss.y + 40, formatNumber(event.amount), {
          color: COLOR.boss,
          sizePx: FONT.small,
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
    return (index - 1) * 40;
  }

  private showPhaseBanner(text: string): void {
    this.phaseBanner.setText(text).setAlpha(1);
    this.tweens.add({ targets: this.phaseBanner, alpha: 0, delay: 1400, duration: 700 });
  }

  private showEndOverlay(victory: boolean): void {
    const container = this.add.container(0, 0).setDepth(100);
    const bg = this.add.rectangle(GAME_W / 2, GAME_H / 2, GAME_W, GAME_H, 0x000000, 0.75).setInteractive();
    const title = addText(this, GAME_W / 2, GAME_H / 2 - 50, victory ? "Victoire !" : "Défaite…", {
      fontSize: "36px",
      fontStyle: "bold",
      color: victory ? "#7CFFB2" : "#FF7C7C",
    }).setOrigin(0.5);
    const retryBg = this.add
      .rectangle(GAME_W / 2, GAME_H / 2 + 30, 240, 56, 0x333652)
      .setStrokeStyle(2, COLOR.panelStroke)
      .setInteractive({ useHandCursor: true })
      .on("pointerdown", () => this.scene.restart());
    const retryLabel = addText(this, GAME_W / 2, GAME_H / 2 + 30, "Recommencer", {
      fontSize: `${FONT.title}px`,
      color: COLOR.text,
    }).setOrigin(0.5);

    container.add([bg, title, retryBg, retryLabel]);
    this.endOverlay = container;
  }
}
