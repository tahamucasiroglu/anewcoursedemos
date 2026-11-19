import AsyncStorage from "@react-native-async-storage/async-storage";
import { parsePreference, resolveScheme, THEME_STORAGE_KEY, themeColors, type ThemePreference } from "@okul/theme";
import { StatusBar } from "expo-status-bar";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { useColorScheme } from "react-native";
import { ThemeContext, type ThemeContextValue } from "./ThemeContext";

/**
 * Mobil tema sağlayıcısı (spec 0002): seçim telefonda saklanır (sunucuya gitmez); "Sistem"de cihaz modu canlı izlenir
 * (useColorScheme). Kayıt okunana kadar boş kare gösterilir (yanlış renk parlaması yok); okuma hata verirse ya da
 * 500 ms'de bitmezse "Sistem" kullanılır (AC-7). Durum çubuğu yazısı moda göre değişir (AC-4).
 */
const STORAGE_TIMEOUT_MS = 500;

export function ThemeProvider({ children }: { children: ReactNode }) {
  const device = useColorScheme();
  const [preference, setPreferenceState] = useState<ThemePreference | null>(null);

  useEffect(() => {
    let settled = false;
    const settle = (value: ThemePreference) => {
      if (!settled) {
        settled = true;
        setPreferenceState(value);
      }
    };
    const timer = setTimeout(() => settle("system"), STORAGE_TIMEOUT_MS);
    AsyncStorage.getItem(THEME_STORAGE_KEY).then(
      (stored) => settle(parsePreference(stored)),
      () => settle("system"),
    );
    return () => {
      settled = true;
      clearTimeout(timer);
    };
  }, []);

  const value = useMemo<ThemeContextValue | null>(() => {
    if (preference === null) {
      return null;
    }

    const scheme = resolveScheme(preference, device === "dark" || device === "light" ? device : null);
    return {
      preference,
      scheme,
      colors: themeColors[scheme],
      setPreference: (next) => {
        setPreferenceState(next);
        AsyncStorage.setItem(THEME_STORAGE_KEY, next).catch(() => {
          // Saklama başarısız: seçim bu oturum boyunca geçerli kalır (AC-7).
        });
      },
    };
  }, [preference, device]);

  if (value === null) {
    return null;
  }

  return (
    <ThemeContext.Provider value={value}>
      <StatusBar style={value.scheme === "dark" ? "light" : "dark"} />
      {children}
    </ThemeContext.Provider>
  );
}
