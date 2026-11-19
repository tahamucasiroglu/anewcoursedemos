import { SystemStatusScreen } from "./src/features/system-status/SystemStatusScreen";
import { ThemeProvider } from "./src/theme/ThemeProvider";

/**
 * Mobil uygulamanın kökü (veli + öğretmen tek uygulama); iskelette yalnızca sistem durumu ekranı var. Tema sağlayıcısı
 * (spec 0002) renkleri ve durum çubuğunu yönetir.
 */
export default function App() {
  return (
    <ThemeProvider>
      <SystemStatusScreen />
    </ThemeProvider>
  );
}
