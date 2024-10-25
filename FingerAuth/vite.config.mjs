import { defineConfig } from 'vite'
import path from "path";
import fs from "fs";

// https://vitejs.dev/config/
export default defineConfig({
    root: path.resolve('wwwroot'),
    plugins: [],
    optimizeDeps: {},

    build: {
        outDir: "dist"
    },

    server: {
        https: {
            key: fs.readFileSync('localhost-key.pem'),
            cert: fs.readFileSync('localhost-cert.pem'),
        },
        port: 5173,
        proxy: {
            '/Server': {
                target: 'https://localhost:5000',
                changeOrigin: true,
                secure: false
            }
        }
    }
})
