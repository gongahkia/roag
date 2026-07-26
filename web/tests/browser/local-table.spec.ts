import { expect, test } from "@playwright/test";

test("runs the local browser-owned table with interactive tiles, policy, and history", async ({ context, page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Own the hand." })).toBeVisible();
  await expect(page.locator("[data-ascii-renderer]")).toBeVisible();
  await expect(page.getByText(/state [0-9a-f]{8}/)).toBeVisible();
  await expect(page.getByLabel("Player hand").getByRole("button")).toHaveCount(14);
  await expect(page.getByRole("heading", { name: "Action aperture" })).toBeVisible();
  await context.setOffline(true);

  await page.getByLabel("Player hand").getByRole("button").filter({ hasText: /./ }).first().click();
  await expect(page.locator(".history-panel")).toContainText("discards");
  await expect(page.getByText(/Saved version \d+ locally\.|Local storage unavailable/)).toBeVisible();
  await expect(page.getByRole("heading", { name: "Table ledger" })).toBeVisible();
});

test("starts a deterministic Sanma table and exposes engine inspection", async ({ page }) => {
  await page.goto("/");
  await page.getByLabel("Game seed").fill("browser-sanma-seed");
  await page.getByRole("button", { name: "New 3P" }).click();
  await expect(page.getByText("3P / E1")).toBeVisible();
  await page.getByRole("button", { name: "Show engine hands" }).click();
  await expect(page.getByRole("button", { name: "Hide engine hands" })).toBeVisible();
  await expect(page.getByText("browser-sanma-seed")).toBeVisible();
});
