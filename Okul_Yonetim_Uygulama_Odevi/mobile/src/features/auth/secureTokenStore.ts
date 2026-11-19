import type { RefreshTokenStore } from "@okul/api-client";
import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";

// Mobil yenileme anahtarı telefonun güvenli deposunda (iOS Keychain / Android Keystore) durur (spec 0004, plan Risks 2).
// Expo'nun tarayıcı sürümünde güvenli depo yoktur: anahtar yalnızca bellekte tutulur (sayfa kapanınca oturum biter).
const KEY = "okul.refreshToken";

let memory: string | null = null;

export const secureTokenStore: RefreshTokenStore = {
  async get() {
    return Platform.OS === "web" ? memory : SecureStore.getItemAsync(KEY);
  },
  async set(token) {
    if (Platform.OS === "web") {
      memory = token;
    } else if (token === null) {
      await SecureStore.deleteItemAsync(KEY);
    } else {
      await SecureStore.setItemAsync(KEY, token);
    }
  },
};
