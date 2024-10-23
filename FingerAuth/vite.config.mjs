import { defineConfig } from 'vite'
import path from "path";

// https://vitejs.dev/config/
export default defineConfig({
  root: path.resolve('wwwroot'),
  plugins: [],
  optimizeDeps:{
    
  },
  build: {
    outDir: "dist"
  },
    server: {
        proxy: {
            '/Server': {
                target: 'https://localhost:5000',
                changeOrigin: true,
                secure: false
            }
        }
    }
})
