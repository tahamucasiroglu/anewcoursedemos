import AsyncStorage from "@react-native-async-storage/async-storage";
import { cleanup } from "@testing-library/react-native";
import * as SecureStore from "expo-secure-store";

// Soğuk başlangıç ısınması (spec 0013 VERIFY V-1 → backlog B-36, conventions T21). Bir test dosyasındaki ilk çizim,
// react-native'in tembel yüklenen modüllerini (FlatList, ScrollView, TextInput…) ve uygulama ekranlarını o an dönüştürüp
// yükler: ölçümde ilk test Jest önbelleği soğukken ~2 sn, sonrakiler ~0,01–0,08 sn; tam kontrolün paralel yükünde bu bedel
// ilk testin süre sınırını aştı. Bu bedel testlerden önce, doğrulama içermeyen bir turda ödenir (dosyanın kendi sahte API'si
// ve ilk testinin yolu: giriş → test edilen ekran); sonra ekran kaldırılır ve saklama alanları boşaltılır, testler sıcak ve
// temiz başlar, varsayılan sürede kalır. Kancanın süresi bu yüzden geniş: yalnızca yükleme süresini sınırlar, hiçbir
// doğrulamayı gevşetmez. Turdaki sorgular (findBy…) yalnızca ekranın gelmesini bekler; tur başarısızsa dosyanın testleri
// zaten aynı yolda kırmızı olur.
export const WARM_UP_TIMEOUT_MS = 60_000;

const resetSecureStore = (SecureStore as unknown as { __reset: () => void }).__reset;

/** Dosya başında çağrılır: `firstRender` (doğrulamasız ilk çizim turu) testlerden önce bir kez çalışır. */
export function warmUpFirstRender(firstRender: () => Promise<unknown>): void {
  beforeAll(async () => {
    resetSecureStore();
    await AsyncStorage.clear();
    await firstRender();
    await cleanup();
    resetSecureStore();
    await AsyncStorage.clear();
  }, WARM_UP_TIMEOUT_MS);
}
