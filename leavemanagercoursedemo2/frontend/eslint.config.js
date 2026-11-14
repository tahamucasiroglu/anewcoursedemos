import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import importX, { createNodeResolver } from 'eslint-plugin-import-x'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'
import { readdirSync } from 'node:fs'
import path from 'node:path'

// Forbidden dependency 5 (docs/architecture.md): a feature never imports another feature,
// and shared/ never imports a feature. One zone per feature folder, generated from the tree.
const featuresDir = path.join(import.meta.dirname, 'src/features')
const features = readdirSync(featuresDir, { withFileTypes: true })
  .filter((entry) => entry.isDirectory())
  .map((entry) => entry.name)

const featureZones = features.map((feature) => ({
  target: `./src/features/${feature}`,
  from: './src/features',
  except: [`./${feature}`],
  message:
    'Bir feature başka bir feature’ı import edemez; ortak kod src/shared/ içinde durur.',
}))

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    plugins: { 'import-x': importX },
    languageOptions: {
      globals: globals.browser,
    },
    settings: {
      // Without a resolver no-restricted-paths silently reports nothing (plan 0001, R-3).
      'import-x/resolver-next': [
        createNodeResolver({ extensions: ['.ts', '.tsx', '.js'] }),
      ],
    },
    rules: {
      'import-x/no-restricted-paths': [
        'error',
        {
          zones: [
            {
              target: './src/shared',
              from: './src/features',
              message: 'src/shared/ hiçbir feature’ı import edemez.',
            },
            ...featureZones,
          ],
        },
      ],
      // docs/security.md: React escaping stays on — no raw HTML injection.
      'no-restricted-syntax': [
        'error',
        {
          selector: "JSXAttribute[name.name='dangerouslySetInnerHTML']",
          message: 'dangerouslySetInnerHTML yasak (docs/security.md).',
        },
      ],
    },
  },
])
