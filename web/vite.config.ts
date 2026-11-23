import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// In development the Vite server proxies /api to the Orders API, so the browser sees a single origin.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:8080',
    },
  },
})
