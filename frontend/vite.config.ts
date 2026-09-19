import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      // Proxy API requests to backend HTTP during development (Kestrel HTTP at 5181)
      '/api': {
        target: 'http://localhost:5181',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, '/api')
      },
      // Proxy SignalR hub requests
      '/hubs': {
        target: 'http://localhost:5181',
        changeOrigin: true,
        secure: false,
        ws: true
      }
    }
  }
})

