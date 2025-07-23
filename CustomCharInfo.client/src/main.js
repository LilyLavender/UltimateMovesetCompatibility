import { createApp } from 'vue';
import '@/style.css';
import App from '@/App.vue';
import vuetify from '@/plugins/vuetify'
import router from '@/router';

const app = createApp(App)

app.use(router)
app.use(vuetify)

app.config.warnHandler = (msg, instance, trace) => {
  if (msg.includes('Invoke the slot function inside the render function instead.')) return

  console.warn(`[Vue warn]: ${msg}\nTrace: ${trace}`)
}

app.mount('#app')