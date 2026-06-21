/// <reference types="vitest/config" />
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

// Under .NET Aspire (AddViteApp + WithReference(adminApi)) the BFF URL is injected as a
// service-discovery env var. Fall back to the Admin.API launch-profile URL for standalone `npm run dev`.
const adminApi =
  process.env["services__admin-api__https__0"] ??
  process.env["services__admin-api__http__0"] ??
  "http://localhost:5310";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { "@": path.resolve(__dirname, "./src") },
  },
  server: {
    host: true,
    port: Number(process.env.PORT) || 5311,
    proxy: {
      "/api": { target: adminApi, changeOrigin: true, secure: false },
    },
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    css: false,
  },
});
