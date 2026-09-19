import { describe, it, expect } from "vitest";
import {
  BOSS_HP_BAR,
  FONT,
  GAME_H,
  GAME_W,
  MIN_TOUCH,
  PAUSE_BUTTON,
  SAFE,
  ZONES,
  clampRenderScale,
  hpColor,
  COLOR,
  skillButtonRects,
  teamCardRects,
  type Rect,
} from "./layout";

const inside = (r: Rect) => r.x >= 0 && r.y >= 0 && r.x + r.w <= GAME_W && r.y + r.h <= GAME_H;
const overlaps = (a: Rect, b: Rect) => a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h;

describe("jetons de design (docs/UX.md)", () => {
  it("aucune taille de texte n'est inférieure à 14 px", () => {
    for (const [name, size] of Object.entries(FONT)) {
      expect(size, name).toBeGreaterThanOrEqual(14);
    }
  });

  it("les tailles de texte sont des entiers", () => {
    for (const size of Object.values(FONT)) expect(Number.isInteger(size)).toBe(true);
  });
});

describe("mise en page portrait", () => {
  const zones = Object.entries(ZONES);

  it("toutes les zones tiennent dans l'écran, avec les marges de sécurité", () => {
    for (const [name, z] of zones) {
      expect(inside(z), name).toBe(true);
    }
    expect(ZONES.skills.y + ZONES.skills.h).toBeLessThanOrEqual(GAME_H - SAFE.bottom);
    expect(ZONES.topBar.y).toBeGreaterThanOrEqual(SAFE.top);
  });

  it("les zones ne se chevauchent pas", () => {
    for (let i = 0; i < zones.length; i++) {
      for (let j = i + 1; j < zones.length; j++) {
        expect(overlaps(zones[i][1], zones[j][1]), `${zones[i][0]} / ${zones[j][0]}`).toBe(false);
      }
    }
  });

  it("l'ordre vertical est : barre haute, boss, bandeau, équipe, cible/mana, sorts", () => {
    const order = [ZONES.topBar, ZONES.boss, ZONES.band, ZONES.team, ZONES.strip, ZONES.skills];
    for (let i = 1; i < order.length; i++) expect(order[i].y).toBeGreaterThanOrEqual(order[i - 1].y + order[i - 1].h);
  });

  it("les sorts sont dans la moitié basse de l'écran (zone du pouce)", () => {
    expect(ZONES.skills.y).toBeGreaterThan(GAME_H / 2);
  });

  it("la barre de PV du boss ne chevauche pas le bouton de pause et reste dans la barre haute", () => {
    expect(overlaps(BOSS_HP_BAR, PAUSE_BUTTON)).toBe(false);
    expect(inside(BOSS_HP_BAR)).toBe(true);
    expect(BOSS_HP_BAR.y + BOSS_HP_BAR.h).toBeLessThanOrEqual(ZONES.boss.y);
    expect(BOSS_HP_BAR.w).toBeGreaterThan(200);
  });

  it("le bouton de pause est une cible tactile valide, dans la barre haute", () => {
    expect(PAUSE_BUTTON.w).toBeGreaterThanOrEqual(MIN_TOUCH);
    expect(PAUSE_BUTTON.h).toBeGreaterThanOrEqual(MIN_TOUCH);
    expect(inside(PAUSE_BUTTON)).toBe(true);
  });
});

describe("cibles tactiles", () => {
  it.each([[3], [4], [5]])("cartes d'alliés (%i) : ≥ 48 px, dans l'écran, sans chevauchement", (count) => {
    const cards = teamCardRects(count);
    expect(cards).toHaveLength(count);
    cards.forEach((c, i) => {
      expect(c.w).toBeGreaterThanOrEqual(MIN_TOUCH);
      expect(c.h).toBeGreaterThanOrEqual(MIN_TOUCH);
      expect(inside(c)).toBe(true);
      if (i > 0) expect(overlaps(cards[i - 1], c)).toBe(false);
    });
  });

  it.each([[3], [4], [5], [6]])("boutons de sorts (%i) : ≥ 48 px, dans l'écran, sans chevauchement", (count) => {
    const buttons = skillButtonRects(count);
    buttons.forEach((b, i) => {
      expect(b.w, `bouton ${i}`).toBeGreaterThanOrEqual(MIN_TOUCH);
      expect(b.h).toBeGreaterThanOrEqual(MIN_TOUCH);
      expect(inside(b)).toBe(true);
      if (i > 0) expect(overlaps(buttons[i - 1], b)).toBe(false);
    });
  });
});

describe("couleur de la barre de PV", () => {
  it("passe du vert à l'orange puis au rouge selon les seuils", () => {
    expect(hpColor(1)).toBe(COLOR.hpHigh);
    expect(hpColor(0.61)).toBe(COLOR.hpHigh);
    expect(hpColor(0.6)).toBe(COLOR.hpMid);
    expect(hpColor(0.31)).toBe(COLOR.hpMid);
    expect(hpColor(0.3)).toBe(COLOR.hpLow);
    expect(hpColor(0)).toBe(COLOR.hpLow);
  });
});

describe("échelle de rendu (netteté sur écrans denses)", () => {
  it("borne le ratio de pixels entre 1 et 3, en entiers", () => {
    expect(clampRenderScale(1)).toBe(1);
    expect(clampRenderScale(1.5)).toBe(2);
    expect(clampRenderScale(2.625)).toBe(3);
    expect(clampRenderScale(4)).toBe(3);
  });

  it("retombe sur 1 pour une valeur invalide", () => {
    expect(clampRenderScale(0)).toBe(1);
    expect(clampRenderScale(Number.NaN)).toBe(1);
    expect(clampRenderScale(-2)).toBe(1);
  });
});
