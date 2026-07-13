import { expect, test } from "@playwright/test";
import { fileURLToPath } from "node:url";

const FIXTURE_TRAJECTORY = fileURLToPath(
  new URL("../../../data/fixtures/mjai/events_4p.mjson", import.meta.url)
);
const FIXTURE_ONNX_MODEL = Buffer.from(
  "CAo6rAEKMwoRbGVnYWxfYWN0aW9uX21hc2sSDWFjdGlvbl9sb2dpdHMiBENhc3QqCQoCdG8YAaABAhIMYnJvd3Nlci10ZXN0Wh8KDG9ic2VydmF0aW9ucxIPCg0IARIJCgIIAQoDCKIEWiQKEWxlZ2FsX2FjdGlvbl9tYXNrEg8KDQgJEgkKAggBCgMIlAJiIAoNYWN0aW9uX2xvZ2l0cxIPCg0IARIJCgIIAQoDCJQCQgQKABAS",
  "base64"
);

test("replays the four-player fixture while offline after page load", async ({ context, page }) => {
  await page.goto("/");
  await context.setOffline(true);
  await page.locator('input[accept*=".mjson"]').setInputFiles(FIXTURE_TRAJECTORY);

  await expect(page.getByText("Loaded 11 events with 0 invalid lines.")).toBeVisible();
  await expect(page.getByText("Event 1 of 11: Game started")).toBeVisible();
  await page.getByRole("button", { name: "9. Seat 2: won on 4p" }).click();
  await expect(page.getByText("Event 9 of 11: Seat 2: won on 4p")).toBeVisible();
});

test("initializes the fixed local ONNX graph in the browser", async ({ page }) => {
  await page.goto("/");
  await page.locator('input[accept*=".onnx"]').setInputFiles({
    name: "mask-cast.onnx",
    mimeType: "application/onnx",
    buffer: FIXTURE_ONNX_MODEL
  });

  await expect(page.getByText(/^Loaded mask-cast\.onnx with (WEBGPU|WASM)( fallback)?\.$/)).toBeVisible();
});
