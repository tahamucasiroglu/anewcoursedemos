import type { ColorScheme, ThemeColors, ThemePreference } from "@okul/theme";
import { createContext } from "react";

// Mobil tema bağlamı (spec 0002): seçim, gösterilen mod, o modun renkleri ve seçimi değiştirme.
export interface ThemeContextValue {
  preference: ThemePreference;
  scheme: ColorScheme;
  colors: ThemeColors;
  setPreference: (preference: ThemePreference) => void;
}

export const ThemeContext = createContext<ThemeContextValue | null>(null);
