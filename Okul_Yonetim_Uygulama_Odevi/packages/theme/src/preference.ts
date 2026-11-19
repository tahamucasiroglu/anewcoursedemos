// Tema seçimi mantığı (spec 0002): "Sistem / Açık / Koyu"; saklanan değer güvenli okunur, gösterilecek mod hesaplanır.
// web/index.html'deki ön betik bu mantığın düz JS kopyasıdır; prePaint testi ikisini aynı vakalarla sınar (Known traps T11).
import type { ColorScheme } from "./tokens";

export type ThemePreference = "system" | "light" | "dark";

export const THEME_STORAGE_KEY = "okul.theme";

export const themePreferences: readonly ThemePreference[] = ["system", "light", "dark"];

/** Saklanan değeri okur: bilinmeyen, bozuk veya boş değer "system" olur (AC-7). */
export function parsePreference(value: unknown): ThemePreference {
  return value === "light" || value === "dark" || value === "system" ? value : "system";
}

/** Gösterilecek mod: açık seçim kazanır; "system"de cihaz modu, cihaz bilgi vermiyorsa açık (AC-2). */
export function resolveScheme(preference: ThemePreference, device: ColorScheme | null | undefined): ColorScheme {
  if (preference !== "system") {
    return preference;
  }

  return device === "dark" ? "dark" : "light";
}
