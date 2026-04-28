import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (id.includes("node_modules")) {
            if (id.includes("react") || id.includes("react-dom") || id.includes("react-router-dom")) {
              return "vendor-react";
            }

            if (id.includes("framer-motion")) {
              return "vendor-motion";
            }

            if (id.includes("lucide-react")) {
              return "vendor-icons";
            }

            return "vendor";
          }

          return undefined;
        },
      },
    },
  },
  server: {
    proxy: {
      "/api": {
        target: "http://localhost:5183",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
