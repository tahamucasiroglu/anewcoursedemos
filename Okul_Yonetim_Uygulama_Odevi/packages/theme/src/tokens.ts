// Renklerin tek kaynağı: tokens.json (spec 0002, AC-1). Ana renk "primary"; kurum markası gelince yalnızca o değişir.
import tokens from "../tokens.json";

export type ColorScheme = "light" | "dark";

export type ColorRole = keyof typeof tokens.light;

export type ThemeColors = Readonly<Record<ColorRole, string>>;

export const themeColors: Readonly<Record<ColorScheme, ThemeColors>> = tokens;

export const colorRoles = Object.keys(tokens.light) as ColorRole[];
