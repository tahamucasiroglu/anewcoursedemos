// VERIFY spec 0003 AC-10 (A-2): mobilin kullandığı kodla (mobile/src/config.ts + @okul/api-client) Pi'ye istek.
// Tarayıcı yok → CORS yok; Expo Go (native) ile aynı yol.
import { resolveApiBaseUrl } from "C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Okul_Yonetim_Uygulama_Odevi/mobile/src/config";
import { createOkulApiClient, fetchSystemHealth } from "C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Okul_Yonetim_Uygulama_Odevi/packages/api-client/src/index";

const baseUrl = resolveApiBaseUrl("http://192.168.1.102:8080");
const result = await fetchSystemHealth(createOkulApiClient(baseUrl));
console.log(`EXPO_PUBLIC_API_BASE_URL=${baseUrl} → ${JSON.stringify(result)}`);
process.exit(result.kind === "healthy" ? 0 : 1);
