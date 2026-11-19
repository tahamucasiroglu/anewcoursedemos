import { StatusBar } from "expo-status-bar";
import { SystemStatusScreen } from "./src/features/system-status/SystemStatusScreen";

/** Mobil uygulamanın kökü (veli + öğretmen tek uygulama); iskelette yalnızca sistem durumu ekranı var. */
export default function App() {
  return (
    <>
      <SystemStatusScreen />
      <StatusBar style="auto" />
    </>
  );
}
