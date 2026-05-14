import { createApp } from 'vue';
import { createUnhead, headSymbol } from '@unhead/vue'
import '@/style.css';
import App from '@/App.vue';
import vuetify from '@/plugins/vuetify'
import router from '@/router';

const app = createApp(App)
const head = createUnhead()

app.use(router)
app.use(vuetify)
app.provide(headSymbol, head)

app.config.warnHandler = (msg, instance, trace) => {
  if (msg.includes('Invoke the slot function inside the render function instead.')) return

  console.warn(`[Vue warn]: ${msg}\nTrace: ${trace}`)
}

app.mount('#app')