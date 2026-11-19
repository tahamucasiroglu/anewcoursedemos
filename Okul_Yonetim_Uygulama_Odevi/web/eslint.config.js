import js from "@eslint/js";
import globals from "globals";
import reactHooks from "eslint-plugin-react-hooks";
import tseslint from "typescript-eslint";

// Web lint kuralları: uyarı = hata (--max-warnings 0). HTTP yalnızca @okul/api-client üzerinden (yasak bağımlılık 7).
export default tseslint.config(
  { ignores: ["dist", "node_modules"] },
  {
    files: ["**/*.{ts,tsx}"],
    extends: [js.configs.recommended, ...tseslint.configs.recommended, reactHooks.configs.flat.recommended],
    languageOptions: { ecmaVersion: 2023, globals: globals.browser },
  },
  {
    files: ["src/**/*.{ts,tsx}"],
    ignores: ["src/**/*.test.{ts,tsx}", "src/test/**"],
    rules: {
      "no-restricted-globals": ["error", { name: "fetch", message: "HTTP yalnızca @okul/api-client üzerinden." }, { name: "XMLHttpRequest", message: "HTTP yalnızca @okul/api-client üzerinden." }],
      "no-restricted-properties": [
        "error",
        ...["globalThis", "window", "self", "global"].map((object) => ({ object, property: "fetch", message: "HTTP yalnızca @okul/api-client üzerinden." })),
      ],
      "no-restricted-imports": ["error", { paths: [{ name: "axios", message: "HTTP yalnızca @okul/api-client üzerinden." }] }],
    },
  },
);
