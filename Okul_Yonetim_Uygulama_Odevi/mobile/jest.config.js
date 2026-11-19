// Mobil testler: jest-expo; ortak istemci (ESM) ve openapi-fetch dönüştürülür. Tek işçi, izleme modu yok (resource hygiene).
module.exports = {
  preset: "jest-expo",
  // Kalıcı saklama testte bellek içi taklitle (spec 0002).
  moduleNameMapper: {
    "^@react-native-async-storage/async-storage$": "@react-native-async-storage/async-storage/jest/async-storage-mock",
    // Güvenli depo testte bellek içi (spec 0004).
    "^expo-secure-store$": "<rootDir>/src/test/secureStoreMock.ts",
  },
  transformIgnorePatterns: [
    "node_modules/(?!((jest-)?react-native|@react-native(-community)?)|expo(nent)?|@expo(nent)?/.*|@expo-google-fonts/.*|react-navigation|@react-navigation/.*|@okul/.*|openapi-fetch)",
  ],
};
