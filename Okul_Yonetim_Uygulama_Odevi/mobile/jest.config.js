// Mobil testler: jest-expo; ortak istemci (ESM) ve openapi-fetch dönüştürülür. Tek işçi, izleme modu yok (resource hygiene).
module.exports = {
  preset: "jest-expo",
  transformIgnorePatterns: [
    "node_modules/(?!((jest-)?react-native|@react-native(-community)?)|expo(nent)?|@expo(nent)?/.*|@expo-google-fonts/.*|react-navigation|@react-navigation/.*|@okul/.*|openapi-fetch)",
  ],
};
