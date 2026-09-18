import js from '@eslint/js'
import pluginVue from 'eslint-plugin-vue'
import prettier from 'eslint-config-prettier'
import globals from 'globals'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist/', 'node_modules/', 'coverage/']),

  js.configs.recommended,
  ...pluginVue.configs['flat/recommended'],

  {
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: {
        ...globals.browser,
        ...globals.node,
      },
    },
    rules: {
      'vue/no-undef-components': 'off', // Vuetify components are registered by the plugin instead of imported so this rule can't resolve them.
      'vue/multi-word-component-names': 'off', // Guards against clashing with HTML elements.
      'vue/valid-v-slot': ['error', { allowModifiers: true }], // Vuetify data tables name their slots with dots (v-slot:item.column), which this rule otherwise rejects.
      'no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
    },
  },

  {
    files: ['**/__tests__/**/*.js'],
    languageOptions: {
      globals: {
        ...globals.vitest,
      },
    },
  },

  prettier,
])
