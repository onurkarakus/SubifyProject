import { defineConfig, devices } from "@playwright/test";

/**
 * Faz 12.3 — Playwright scaffold (optional P2).
 * Requires API + web running:
 *   API: http://localhost:5240
 *   Web: http://localhost:3000
 *
 *   cd web && npx playwright install
 *   npx playwright test
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: "list",
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL || "http://localhost:3000",
    trace: "on-first-retry",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  // Do not auto-start web server here — keep E2E explicit against a live stack.
});
