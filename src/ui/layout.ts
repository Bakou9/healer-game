/**
 * Jetons de design et mise en page de l'écran de combat (docs/UX.md, tickets
 * E04-T02 et E04-T03). Tout est exprimé en pixels LOGIQUES sur une toile de
 * 480×854 (portrait) ; le rendu est mis à l'échelle de l'écran par
 * `renderScale`. Aucun import de Phaser : le fichier est testé sans navigateur.
 */

export const GAME_W = 480;
export const GAME_H = 854;

/** Cible tactile minimale (px logiques) : règle UX « ≥ 48 px ». */
export const MIN_TOUCH = 48;

/** Tailles de texte (px logiques). Aucune n'est inférieure à `FONT.small` (règle UX « ≥ 14 px »). */
export const FONT = {
  small: 14,
  body: 16,
  strong: 18,
  title: 20,
  banner: 28,
  cooldown: 24,
} as const;

/** Marges de sécurité : encoches et barres système (à vérifier sur appareil réel, E11-T01). */
export const SAFE = { top: 8, bottom: 40, side: 12 } as const;

export const GAP = 8;

export const COLOR = {
  background: 0x10121a,
  panel: 0x2a2d3a,
  panelStroke: 0x555a70,
  text: "#ffffff",
  textMuted: "#a7adc2",
  heal: "#7CFFB2",
  damage: "#FF7C7C",
  poison: "#C78CFF",
  shield: "#7CC8FF",
  boss: "#E6E6E6",
  danger: "#FF5B5B",
  selected: 0xffe066,
  hpHigh: 0x5fd35f,
  hpMid: 0xf0a23a,
  hpLow: 0xe0443e,
  mana: 0x4aa8ff,
  role: { tank: 0x4a6fa5, dps: 0xb5495b, healer: 0x4caf7d } as Record<string, number>,
} as const;

export interface Rect {
  x: number;
  y: number;
  w: number;
  h: number;
}

/** Zones verticales de l'écran, de haut en bas (voir docs/UX.md §4). */
export const ZONES = {
  topBar: { x: 0, y: SAFE.top, w: GAME_W, h: 52 },
  boss: { x: 0, y: 64, w: GAME_W, h: 186 },
  band: { x: SAFE.side, y: 262, w: GAME_W - 2 * SAFE.side, h: 66 },
  team: { x: SAFE.side, y: 340, w: GAME_W - 2 * SAFE.side, h: 200 },
  strip: { x: SAFE.side, y: 552, w: GAME_W - 2 * SAFE.side, h: 64 },
  skills: { x: SAFE.side, y: 632, w: GAME_W - 2 * SAFE.side, h: 164 },
} satisfies Record<string, Rect>;

/** Bouton de pause : cible tactile ≥ 48 px, en haut à droite. */
export const PAUSE_BUTTON: Rect = { x: GAME_W - SAFE.side - MIN_TOUCH, y: SAFE.top, w: MIN_TOUCH, h: MIN_TOUCH };

/** Barre de PV du boss : s'arrête avant le bouton de pause pour ne pas le chevaucher. */
export const BOSS_HP_BAR: Rect = {
  x: SAFE.side,
  y: SAFE.top + 34,
  w: PAUSE_BUTTON.x - GAP - SAFE.side,
  h: 14,
};

/** Répartit `count` éléments égaux sur la largeur d'une zone, avec `GAP` entre eux. */
export function splitRow(zone: Rect, count: number): Rect[] {
  const width = (zone.w - GAP * (count - 1)) / count;
  return Array.from({ length: count }, (_, i) => ({
    x: zone.x + i * (width + GAP),
    y: zone.y,
    w: width,
    h: zone.h,
  }));
}

export const teamCardRects = (count: number): Rect[] => splitRow(ZONES.team, count);
export const skillButtonRects = (count: number): Rect[] => splitRow(ZONES.skills, count);

/** Couleur de la barre de PV selon le ratio (seuils lisibles ; la couleur n'est jamais seule, le chiffre est affiché). */
export function hpColor(ratio: number): number {
  if (ratio > 0.6) return COLOR.hpHigh;
  if (ratio > 0.3) return COLOR.hpMid;
  return COLOR.hpLow;
}

/** Borne le ratio de pixels de l'appareil : net sur écrans denses, sans surcoût excessif. */
export function clampRenderScale(devicePixelRatio: number): number {
  if (!Number.isFinite(devicePixelRatio) || devicePixelRatio < 1) return 1;
  return Math.min(Math.ceil(devicePixelRatio), 3);
}

/**
 * Échelle de rendu de l'appareil. En développement, `?dpr=2` dans l'adresse
 * force une échelle pour vérifier le rendu haute densité sur un écran normal.
 */
export function renderScale(): number {
  if (typeof window === "undefined") return 1;
  if (import.meta.env.DEV) {
    const forced = Number(new URLSearchParams(window.location.search).get("dpr"));
    if (forced >= 1) return clampRenderScale(forced);
  }
  return clampRenderScale(window.devicePixelRatio);
}
