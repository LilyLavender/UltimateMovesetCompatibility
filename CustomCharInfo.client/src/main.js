import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createHead } from '@unhead/vue/client'
import '@/styles/tokens.css'
import '@/styles/fonts.css'
import '@/styles/base.css'
import '@/styles/layout.css'
import App from '@/App.vue'
import vuetify from '@/plugins/vuetify'
import '@/styles/vuetify.css'
import router from '@/router'

const app = createApp(App)
const head = createHead()

app.use(createPinia())
app.use(router)
app.use(vuetify)
app.use(head)

app.config.warnHandler = (msg, instance, trace) => {
  if (msg.includes('Invoke the slot function inside the render function instead.')) return

  console.warn(`[Vue warn]: ${msg}\nTrace: ${trace}`)
}

app.mount('#app')
