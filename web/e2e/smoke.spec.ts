import { test, expect } from "@playwright/test";

/**
 * 12.3.2 smoke — landing + login page render.
 * Full first-admin + subscription flow needs a fresh DB / setup; this keeps CI optional.
 */
test.describe("Subify OS web smoke", () => {
  test("landing shows app name and login link", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByText(/Subify/i).first()).toBeVisible();
    await expect(page.getByRole("link", { name: /giriş|sign in|login/i }).first()).toBeVisible();
  });

  test("login page has email and password fields", async ({ page }) => {
    await page.goto("/login");
    await expect(page.locator('input[type="email"]')).toBeVisible();
    await expect(page.locator('input[type="password"]')).toBeVisible();
  });
});
