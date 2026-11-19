// WCAG 2 kontrast oranı (spec 0002, AC-9): normal metin ≥ 4.5:1, büyük metin / düğme ve çerçeve kenarı ≥ 3:1.
import type { ColorRole } from "./tokens";

export const TEXT_MIN = 4.5;
export const NON_TEXT_MIN = 3;

/** Denetlenen renk çiftleri: [ön plan, arka plan, en az oran]. Yeni bir rol kullanılınca buraya eklenir. */
export const contrastPairs: ReadonlyArray<readonly [ColorRole, ColorRole, number]> = [
  ["text", "background", TEXT_MIN],
  ["text", "surface", TEXT_MIN],
  ["textMuted", "background", TEXT_MIN],
  ["textMuted", "surface", TEXT_MIN],
  ["onPrimary", "primary", TEXT_MIN],
  ["primary", "surface", NON_TEXT_MIN],
  ["primary", "background", NON_TEXT_MIN],
  ["border", "surface", NON_TEXT_MIN],
  ["border", "background", NON_TEXT_MIN],
  ["success", "surface", NON_TEXT_MIN],
  ["success", "background", NON_TEXT_MIN],
  ["danger", "surface", NON_TEXT_MIN],
  ["danger", "background", NON_TEXT_MIN],
  ["focus", "background", NON_TEXT_MIN],
  ["focus", "surface", NON_TEXT_MIN],
];

function channel(value: number): number {
  const c = value / 255;
  return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
}

function luminance(hex: string): number {
  const match = /^#([0-9a-f]{6})$/i.exec(hex);
  if (!match) {
    throw new Error(`Geçersiz renk: ${hex}`);
  }

  const n = Number.parseInt(match[1], 16);
  return 0.2126 * channel((n >> 16) & 255) + 0.7152 * channel((n >> 8) & 255) + 0.0722 * channel(n & 255);
}

/** İki rengin kontrast oranı (1–21). */
export function contrastRatio(foreground: string, background: string): number {
  const a = luminance(foreground);
  const b = luminance(background);
  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
}
