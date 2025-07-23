import { createVuetify } from 'vuetify'
import 'vuetify/styles'
import { aliases, mdi } from 'vuetify/iconsets/mdi'

export default createVuetify({
  theme: {
    defaultTheme: 'umcDefaultTheme',
    themes: {
      umcDefaultTheme: {
        dark: false,
        colors: {
          background: '#000000',
          surface: 'rgba(200, 200, 200, 0.9)',
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
  },
})