import { AuthGate } from "./src/features/auth/AuthGate";
import { SystemStatusScreen } from "./src/features/system-status/SystemStatusScreen";
import { ThemeProvider } from "./src/theme/ThemeProvider";

/**
 * Mobil uygulamanın kökü (veli + öğretmen tek uygulama): sistem durumu ve altında oturum alanı (spec 0004). Tema
 * sağlayıcısı (spec 0002) renkleri ve durum çubuğunu yönetir.
 */
export default function App() {
  return (
    <ThemeProvider>
      <SystemStatusScreen>
        <AuthGate />
      </SystemStatusScreen>
    </ThemeProvider>
  );
}
