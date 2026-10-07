import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // forward /api calls to the express server during development
    proxy: {
      '/api': 'http://localhost:5050',
    },
  },
})
