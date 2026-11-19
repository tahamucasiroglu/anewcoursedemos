import { SystemStatus } from "./features/system-status/SystemStatus";
import { t } from "./i18n/t";

/** Web panelinin kök bileşeni: ürün başlığı ve sistem durumu. */
export default function App() {
  return (
    <main className="page">
      <h1>{t("app.title")}</h1>
      <SystemStatus />
    </main>
  );
}
