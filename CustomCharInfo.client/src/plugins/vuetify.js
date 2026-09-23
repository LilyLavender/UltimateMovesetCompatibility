import { createVuetify } from 'vuetify'
import 'vuetify/styles'
import { aliases, mdi } from 'vuetify/iconsets/mdi'

const field = { variant: 'outlined', density: 'comfortable', hideDetails: 'auto' }

// Vuetify supplies behavior only; its look is set here and in src/styles/.
// Theme colors mirror src/styles/tokens.css because Vuetify needs literal values.
export default createVuetify({
  theme: {
    defaultTheme: 'umcDefaultTheme',
    themes: {
      umcDefaultTheme: {
        dark: true,
        colors: {
          background: '#000000',
          surface: '#0c0c0c',
          'on-surface': '#f2f2f2',
          primary: '#ffffff',
          'on-primary': '#000000',
          secondary: '#b4b4b4',
          'on-secondary': '#000000',
          error: '#f13434',
          success: '#43a047',
          warning: '#f1f134',
          info: '#2f9ee0',
        },
      },
    },
  },
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: {
      mdi,
    },
  },
  defaults: {
    global: {
      ripple: false,
    },
    VBtn: { variant: 'outlined', rounded: 0, elevation: 0 },
    VCard: { variant: 'flat', rounded: 0, elevation: 0 },
    VSheet: { rounded: 0, elevation: 0 },
    VTextField: field,
    VSelect: field,
    VAutocomplete: field,
    VCombobox: field,
    VTextarea: field,
    VMenu: { transition: 'slide-y-transition' },
    VDataTable: { density: 'comfortable' },
    VCheckbox: { color: 'white' },
    VRadioGroup: { color: 'white' },
    VSwitch: { color: 'white' },
    VChip: { rounded: 0 },
    VAlert: { rounded: 0 },
  },
})
