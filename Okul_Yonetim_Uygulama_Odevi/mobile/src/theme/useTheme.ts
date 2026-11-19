import { useContext } from "react";
import { ThemeContext, type ThemeContextValue } from "./ThemeContext";

/** Geçerli tema (renkler, mod, seçim). Yalnızca ThemeProvider içinde kullanılır. */
export function useTheme(): ThemeContextValue {
  const value = useContext(ThemeContext);
  if (value === null) {
    throw new Error("useTheme yalnızca ThemeProvider içinde kullanılabilir.");
  }

  return value;
}
