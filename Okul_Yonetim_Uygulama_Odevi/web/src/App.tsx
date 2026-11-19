import { SystemStatus } from "./features/system-status/SystemStatus";
import { t } from "./i18n/t";
import { ThemeSwitcher } from "./theme/ThemeSwitcher";
import { useThemePreference } from "./theme/useThemePreference";

/** Web panelinin kök bileşeni: başlık satırı (ürün adı + tema seçici, spec 0002) ve sistem durumu. */
export default function App() {
  const { preference, setPreference } = useThemePreference();

  return (
    <main className="page">
      <header className="app-header">
        <h1>{t("app.title")}</h1>
        <ThemeSwitcher preference={preference} onChange={setPreference} />
      </header>
      <SystemStatus />
    </main>
  );
}
