import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import i18n from './i18n'

import './styles/main.css'

// KTui JavaScript initialization (Metronic interactive components)
// Menu, accordion, dropdown, tooltip vs. için
import('@keenthemes/ktui').then(({ default: KTComponents }) => {
  KTComponents.init()
}).catch(() => {
  // KTui JS yoksa CSS-only mode devam eder
})

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(i18n)

app.mount('#app')
