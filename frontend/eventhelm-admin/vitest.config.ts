import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

export default defineConfig({
    envDir: false,
    plugins: [react()],
    test: {
        environment: "jsdom",
        setupFiles: ["./src/test/setup.ts"],
        clearMocks: true,
        env: {
            VITE_API_URL: "/api",
        },
    },
});
