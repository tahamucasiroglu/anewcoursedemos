import type { ThemePreference } from "@okul/theme";
import { t, type TextKey } from "../i18n/t";

/**
 * Tema seçici (spec 0002): "Sistem · Açık · Koyu" düğme grubu. Grup adı "Tema"; seçili düğme aria-pressed ile
 * ekran okuyucuya bildirilir. Tab ile her düğmeye gelinir, Enter/Boşluk ile seçilir (AC-11).
 */
const options: ReadonlyArray<{ value: ThemePreference; label: TextKey; icon: string }> = [
  { value: "system", label: "theme.system", icon: "◐" },
  { value: "light", label: "theme.light", icon: "☀" },
  { value: "dark", label: "theme.dark", icon: "☾" },
];

interface ThemeSwitcherProps {
  preference: ThemePreference;
  onChange: (preference: ThemePreference) => void;
}

export function ThemeSwitcher({ preference, onChange }: ThemeSwitcherProps) {
  return (
    <div className="theme-switcher" role="group" aria-label={t("theme.label")}>
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          aria-pressed={preference === option.value}
          onClick={() => onChange(option.value)}
        >
          <span aria-hidden="true">{option.icon} </span>
          {t(option.label)}
        </button>
      ))}
    </div>
  );
}
