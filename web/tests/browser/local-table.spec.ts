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
  await expect(page.getByLabel("Replay frame")).toBeVisible();
  await expect(page.getByRole("button", { name: "Undo" })).toBeEnabled();
  await page.getByRole("button", { name: "Undo" }).click();
  await expect(page.getByRole("button", { name: "Undo" })).toBeDisabled();
  await expect(page.getByText(/Saved version \d+ locally\.|Local storage unavailable/)).toBeVisible();
  await expect(page.getByRole("heading", { name: "Table ledger" })).toBeVisible();
});

test("autoplay uses the local policy for every seat and can be stopped", async ({ page }) => {
  await page.goto("/");
  const history = page.locator(".history-panel");
  const initialEvents = await history.locator(".event-log li").count();
  await page.getByRole("button", { name: "Autoplay all" }).click();
  await expect(page.getByRole("button", { name: "Stop autoplay" })).toBeVisible();
  await expect(page.getByText("all seats autoplaying")).toBeVisible();
  await expect.poll(() => history.locator(".event-log li").count()).toBeGreaterThan(initialEvents);
  await page.getByRole("button", { name: "Stop autoplay" }).click();
  await expect(page.getByRole("button", { name: "Autoplay all" })).toBeVisible();
});

test("starts a deterministic Sanma table and exposes engine inspection", async ({ page }) => {
  await page.goto("/");
  await page.getByLabel("Game seed").fill("browser-sanma-seed");
  await page.getByRole("button", { name: "New 3P" }).click();
  await expect(page.getByText("3P / E1")).toBeVisible();
  await expect(page.getByText("Kamicha", { exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Show engine hands" }).click();
  await expect(page.getByRole("button", { name: "Hide engine hands" })).toBeVisible();
  await expect(page.getByText("browser-sanma-seed")).toBeVisible();
});
