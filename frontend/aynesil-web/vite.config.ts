import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [
      vue(),
      tailwindcss(),
    ],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: env.VITE_API_BASE_URL || 'http://localhost:5000',
          changeOrigin: true,
        },
      },
    },
    build: {
      outDir: 'dist',
      sourcemap: mode !== 'production',
      rollupOptions: {
        output: {
          // Keep shared app code out of the entry chunk so lazy route modules
          // do not import back into index (Safari: "Importing a module script failed").
          manualChunks(id) {
            const normalized = id.replace(/\\/g, '/')
            if (normalized.includes('node_modules')) {
              if (normalized.includes('vue-i18n')) return 'i18n'
              if (normalized.includes('apexcharts') || normalized.includes('vue3-apexcharts')) return 'charts'
              if (
                normalized.includes('vue-router') ||
                normalized.includes('pinia') ||
                normalized.includes('@vue/') ||
                /(?:^|\/)vue(?:\/|$)/.test(normalized)
              ) {
                return 'vendor'
              }
              return
            }
            if (
              normalized.includes('/src/stores/') ||
              normalized.includes('/src/services/') ||
              normalized.includes('/src/composables/') ||
              normalized.includes('/src/types/')
            ) {
              return 'app-core'
            }
          },
        },
      },
    },
  }
})
