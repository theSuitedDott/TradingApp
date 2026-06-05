import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      // Proxy API requests to backend HTTPS during development (Kestrel HTTPS at 7065)
      '/api': {
        target: 'https://localhost:7065',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api/, '/api')
      },
      // Proxy SignalR hub requests
      '/hubs': {
        target: 'https://localhost:7065',
        changeOrigin: true,
        secure: false,
        ws: true
      }
    }
  }
})

