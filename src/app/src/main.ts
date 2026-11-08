import { createApp } from 'vue'
import App from './App.vue'
import { router } from './router'
import '@forma/design-system/styles.css'

createApp(App).use(router).mount('#app')
