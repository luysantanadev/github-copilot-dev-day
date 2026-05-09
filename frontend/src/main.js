import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router/index'
import { logger } from './utils/logger'

logger.info('Inicializando aplicação', {
  apiUrl:   import.meta.env.VITE_API_URL,
  logLevel: import.meta.env.VITE_LOG_LEVEL,
  mode:     import.meta.env.MODE
})

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.mount('#app')

logger.info('Aplicação montada com sucesso')
