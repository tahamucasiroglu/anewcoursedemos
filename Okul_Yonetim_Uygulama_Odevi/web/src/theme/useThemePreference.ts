import { parsePreference, resolveScheme, THEME_STORAGE_KEY, type ColorScheme, type ThemePreference } from "@okul/theme";
import { useEffect, useState } from "react";

// Web tema seçimi (spec 0002): seçim tarayıcıda saklanır (sunucuya gitmez), "Sistem"de cihaz modu canlı izlenir,
// saklama okunamaz/yazılamazsa uygulama hata göstermeden çalışmaya devam eder (AC-7).
const DARK_QUERY = "(prefers-color-scheme: dark)";

function readStoredPreference(): ThemePreference {
  try {
    return parsePreference(window.localStorage.getItem(THEME_STORAGE_KEY));
  } catch {
    return "system";
  }
}

function storePreference(preference: ThemePreference): void {
  try {
    window.localStorage.setItem(THEME_STORAGE_KEY, preference);
  } catch {
    // Saklama kapalı: seçim bu oturum boyunca geçerli kalır (AC-7).
  }
}

function deviceMedia(): MediaQueryList | null {
  return typeof window.matchMedia === "function" ? window.matchMedia(DARK_QUERY) : null;
}

function readDeviceScheme(): ColorScheme | null {
  const media = deviceMedia();
  return media ? (media.matches ? "dark" : "light") : null;
}

export interface ThemePreferenceState {
  preference: ThemePreference;
  scheme: ColorScheme;
  setPreference: (preference: ThemePreference) => void;
}

export function useThemePreference(): ThemePreferenceState {
  const [preference, setPreferenceState] = useState<ThemePreference>(readStoredPreference);
  const [device, setDevice] = useState<ColorScheme | null>(readDeviceScheme);

  useEffect(() => {
    const media = deviceMedia();
    if (!media) {
      return undefined;
    }

    const onChange = (event: MediaQueryListEvent) => setDevice(event.matches ? "dark" : "light");
    media.addEventListener("change", onChange);
    return () => media.removeEventListener("change", onChange);
  }, []);

  const scheme = resolveScheme(preference, device);

  useEffect(() => {
    document.documentElement.setAttribute("data-theme", scheme);
    document.documentElement.style.colorScheme = scheme;
  }, [scheme]);

  const setPreference = (next: ThemePreference) => {
    setPreferenceState(next);
    storePreference(next);
  };

  return { preference, scheme, setPreference };
}
