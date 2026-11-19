// Mobil lint kuralları (web ile aynı set): uyarı = hata (--max-warnings 0). HTTP yalnızca @okul/api-client üzerinden.
// Not: eslint-config-expo henüz ESLint 10 ile uyumlu değil (eslint-plugin-react); bu yüzden web'deki set kullanılır.
const js = require("@eslint/js");
const globals = require("globals");
const reactHooks = require("eslint-plugin-react-hooks");
const tseslint = require("typescript-eslint");

const onlyApiClient = "HTTP yalnızca @okul/api-client üzerinden.";

module.exports = tseslint.config(
  { ignores: ["dist/**", "web-build/**", ".expo/**", "node_modules/**", "*.config.js"] },
  {
    files: ["**/*.{ts,tsx}"],
    extends: [js.configs.recommended, ...tseslint.configs.recommended, reactHooks.configs.flat.recommended],
    languageOptions: { ecmaVersion: 2023, globals: { ...globals.es2021, ...globals.jest } },
  },
  {
    files: ["src/**/*.{ts,tsx}", "App.tsx", "index.ts"],
    ignores: ["src/**/*.test.{ts,tsx}"],
    rules: {
      "no-restricted-globals": ["error", { name: "fetch", message: onlyApiClient }, { name: "XMLHttpRequest", message: onlyApiClient }],
      "no-restricted-properties": [
        "error",
        ...["globalThis", "window", "self", "global"].map((object) => ({ object, property: "fetch", message: onlyApiClient })),
      ],
      "no-restricted-imports": ["error", { paths: [{ name: "axios", message: onlyApiClient }] }],
    },
  },
);
