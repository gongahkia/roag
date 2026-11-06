import { test, expect } from '@playwright/test';
import type { Page, TestInfo } from '@playwright/test';
import { founders } from '../../src/content.ts';
import { createWorld } from '../../src/simulation/world.ts';
const residents = createWorld(42).people.map(p => ({ ...p, ...founders[p.founder]! }));
import { PNG } from 'pngjs';

const camera = async (page: Page): Promise<{ x: number; y: number; zoom: number }> => JSON.parse((await page.locator('#stage').getAttribute('data-camera'))!);
async function screenshot(page: Page, info: TestInfo, name: string): Promise<void> {
  await page.screenshot({ path: info.outputPath(`${name}.png`), fullPage: true });
}
function failures(page: Page) {
  const errors: string[] = []; const requests: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  page.on('console', (message) => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('requestfailed', (request) => errors.push(`${request.url()}: ${request.failure()?.errorText}`));
  page.on('response', (response) => { if (response.status() >= 400) errors.push(`${response.status()} ${response.url()}`); });
  // Phaser's built-in fallback textures use data URIs; these are not network traffic.
  page.on('request', (request) => { if (/^https?:/.test(request.url())) requests.push(request.url()); });
  return { errors, requests };
}
async function ready(page: Page) {
  await page.goto('./'); await expect(page.locator('#stage')).toHaveAttribute('data-ready', 'true');
}
async function clickFounder(page: Page, index: number) {
  const resident = residents[index]!; const view = await camera(page);
  const box = (await page.locator('canvas').boundingBox())!;
  await page.mouse.click(box.x + box.width / 2 + ((resident.x + .5) * 12 - view.x) * view.zoom,
    box.y + box.height / 2 + ((resident.y + .5) * 12 - view.y) * view.zoom);
  await expect(page.locator('#inspection h3')).toHaveText(resident.name);
  await expect(page.locator('#inspection .role')).toHaveText(resident.role);
}

test('production artwork, pointer selection, drag, wheel, keyboard, bounds, UI isolation, and credits', async ({ page, baseURL }, info) => {
  const evidence = failures(page); await ready(page);
  const pixels = PNG.sync.read(await page.locator('canvas').screenshot());
  const colors = new Set<string>(); for (let i = 0; i < pixels.data.length; i += 4) colors.add(pixels.data.subarray(i, i + 4).toString('hex'));
  expect(colors.size).toBeGreaterThan(20); // Empty/missing artwork must not pass on DOM alone.
  await info.attach('render-context', { body: JSON.stringify({ browserVersion: page.context().browser()?.version(), viewport: page.viewportSize(), canvas: { width: pixels.width, height: pixels.height }, distinctCanvasColors: colors.size }), contentType: 'application/json' });
  await screenshot(page, info, '01-clearing');
  await clickFounder(page, 0);
  await expect(page.locator('[data-resident=ada]')).toHaveAttribute('aria-pressed', 'true');
  await page.getByText('Artwork provenance', { exact: true }).click();
  await expect(page.locator('.provenance')).toContainText('urizen-onebit-r00-c105');
  await screenshot(page, info, '02-selected-provenance');
  await page.getByText('Artwork provenance', { exact: true }).click();
  const original = await camera(page); const box = (await page.locator('canvas').boundingBox())!;
  await page.mouse.move(box.x + 150, box.y + 150); await page.mouse.down();
  await page.mouse.move(box.x + 250, box.y + 210, { steps: 8 }); await page.mouse.up();
  const dragged = await camera(page); expect(dragged.x).toBeLessThan(original.x - 25);
  await expect(page.locator('#inspection h3')).toHaveText('Ada Rowan'); // dragging must not select/clear
  await page.mouse.wheel(0, -100); await expect.poll(async () => (await camera(page)).zoom).toBe(4);
  await screenshot(page, info, '03-zoom-400');
  await page.locator('#stage').focus(); const start = await camera(page);
  await page.keyboard.down('ArrowRight'); await page.waitForTimeout(240); await page.keyboard.up('ArrowRight');
  expect((await camera(page)).x).toBeGreaterThan(start.x + 8);
  await page.keyboard.press('Home'); await expect(page.locator('#zoom')).toHaveText('300%');
  await clickFounder(page, 1); // click remains accurate after camera movement and zoom
  await page.getByRole('button', { name: 'Find Cora Flint, Miner' }).click();
  await expect(page.locator('#inspection h3')).toHaveText('Cora Flint');
  const focused = await camera(page); expect(focused.x).toBe((68.5) * 12);
  // Keyboard input on ordinary DOM controls must not move the camera.
  await page.keyboard.press('ArrowRight'); expect(await camera(page)).toEqual(focused);
  await page.getByRole('button', { name: 'Zoom in', exact: true }).click();
  await page.getByRole('button', { name: 'Zoom in', exact: true }).click();
  await page.getByRole('button', { name: 'Zoom in', exact: true }).click();
  await expect(page.locator('#zoom')).toHaveText('600%'); await expect(page.locator('#zoom-in')).toBeDisabled();
  await screenshot(page, info, '04-zoom-600');
  await page.locator('#stage').focus(); await page.keyboard.press('Escape');
  await expect(page.locator('#selection-state')).toHaveText('NO SELECTION');
  await page.keyboard.press('Home');
  await page.getByRole('button', { name: 'Zoom out', exact: true }).click();
  await page.getByRole('button', { name: 'Zoom out', exact: true }).click();
  await expect(page.locator('#zoom')).toHaveText('100%'); await expect(page.locator('#zoom-out')).toBeDisabled();
  await screenshot(page, info, '06-zoom-200');
  // Drag far beyond every edge. Clamped world bounds remain inside the viewport.
  for (const [dx, dy] of [[10000, 10000], [-10000, -10000]]) {
    await page.mouse.move(box.x + 150, box.y + 150); await page.mouse.down();
    // Pointer outside canvas still receives the drag; pointerupoutside must release it.
    await page.mouse.move(box.x + dx!, box.y + dy!, { steps: 5 }); await page.mouse.up();
    const v = await camera(page); expect(v.x - box.width / (2 * v.zoom)).toBeGreaterThanOrEqual(-1);
    expect(v.x + box.width / (2 * v.zoom)).toBeLessThanOrEqual(1537);
    expect(v.y - box.height / (2 * v.zoom)).toBeGreaterThanOrEqual(-1);
    expect(v.y + box.height / (2 * v.zoom)).toBeLessThanOrEqual(1537);
  }
  await page.getByRole('button', { name: 'Center', exact: true }).click();
  await page.getByRole('button', { name: 'Pan left' }).click(); expect((await camera(page)).x).toBe(original.x - 48);
  await page.getByRole('button', { name: 'Center', exact: true }).click();
  await page.getByRole('button', { name: 'Artwork & credits' }).click();
  await expect(page.getByRole('dialog')).toContainText('vurmux'); await expect(page.getByRole('dialog')).toContainText('pixel-boy');
  const credits = await page.request.get(new URL('game-assets/CREDITS.txt', baseURL).toString()); expect(credits.ok()).toBe(true);
  await screenshot(page, info, '05-credits'); await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).not.toBeVisible();
  // Blank ground deselects, and a real page reload renders again.
  await clickFounder(page, 0); await page.mouse.click(box.x + 10, box.y + 10);
  await expect(page.locator('#selection-state')).toHaveText('NO SELECTION');
  await page.reload(); await expect(page.locator('#stage')).toHaveAttribute('data-ready', 'true');
  const mount = new URL(baseURL!).pathname;
  await info.attach('network-and-console', { body: JSON.stringify(evidence, null, 2), contentType: 'application/json' });
  expect(evidence.requests.every((url) => new URL(url).origin === new URL(baseURL!).origin && new URL(url).pathname.startsWith(mount))).toBe(true);
  expect(evidence.requests.some((url) => url.endsWith('/game-assets/colony.png'))).toBe(true);
  expect(evidence.requests.some((url) => /catalog|atlas-index|\.zip$/.test(new URL(url).pathname))).toBe(false);
  expect(evidence.errors).toEqual([]);
});

test('resize and narrow layout preserve canvas hit testing, readable inspector and all founders', async ({ page }, info) => {
  const evidence = failures(page); await ready(page);
  for (const viewport of [{width:960,height:720}, {width:390,height:844}]) {
    await page.setViewportSize(viewport);
    await expect.poll(async () => Math.round((await page.locator('canvas').boundingBox())!.width)).toBe(await page.locator('#stage').evaluate((e) => e.clientWidth));
    await page.getByRole('button', { name: 'Find Ada Rowan, Builder' }).click();
    await page.locator('canvas').scrollIntoViewIfNeeded(); await clickFounder(page, 0);
    await expect(page.locator('#inspection')).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(viewport.width);
    await screenshot(page, info, `resize-${viewport.width}`);
  }
  for (const resident of residents) {
    await page.getByRole('button', { name: `Find ${resident.name}, ${resident.role}` }).click();
    await expect(page.locator('#inspection h3')).toHaveText(resident.name);
    await expect(page.locator('#inspection .role')).toHaveText(resident.role);
  }
  expect(evidence.errors).toEqual([]);
});

test('viewport zoom controls refresh when resize leaves the camera unchanged', async ({ page, baseURL }, info) => {
  const evidence = failures(page);
  const observations: unknown[] = [];
  const zoomOut = page.getByRole('button', { name: 'Zoom out', exact: true });
  const zoomIn = page.getByRole('button', { name: 'Zoom in', exact: true });
  const resize = async (width: number, height: number) => {
    await page.setViewportSize({ width, height });
    await expect.poll(async () => (await page.locator('canvas').boundingBox())!.width)
      .toBe(await page.locator('#stage').evaluate((node) => node.clientWidth));
  };
  const record = async (step: string) => observations.push({ step, viewport: page.viewportSize(),
    camera: await camera(page), zoomOutDisabled: await zoomOut.isDisabled(), zoomInDisabled: await zoomIn.isDisabled() });
  try {
    await page.setViewportSize({width:2080,height:1000});
    await ready(page);
    await clickFounder(page, 0);
    await zoomOut.click();
    await expect(page.locator('#zoom')).toHaveText('200%');
    await expect(zoomOut).toBeDisabled();
    await expect(zoomIn).toBeEnabled();
    const desktopMinimum = await camera(page);
    await record('desktop minimum');

    await resize(390, 844);
    // No focus/selection/zoom command may refresh controls before this assertion.
    expect(await camera(page)).toEqual(desktopMinimum);
    await record('shrink with unchanged camera');
    await expect(zoomOut).toBeEnabled();
    await expect(zoomIn).toBeEnabled();
    await screenshot(page, info, 'zoom-shrink-200-enabled');
    await zoomOut.click();
    await expect(page.locator('#zoom')).toHaveText('100%');
    await expect(zoomOut).toBeDisabled();
    await record('button reaches narrow minimum');
    await screenshot(page, info, 'zoom-narrow-100-minimum');

    // Growing from 100% must clamp to the new 200% desktop minimum.
    await resize(2080, 1000);
    await expect(page.locator('#zoom')).toHaveText('200%');
    await expect(zoomOut).toBeDisabled();
    await expect(zoomIn).toBeEnabled();
    await record('growth clamps camera to desktop minimum');

    await resize(390, 844);
    await expect(zoomOut).toBeEnabled();
    const narrowAtTwo = await camera(page);
    // Growth at 200% changes the limit but must preserve the entire camera view.
    await resize(2080, 1000);
    expect(await camera(page)).toEqual(narrowAtTwo);
    await record('grow with unchanged camera');
    await expect(zoomOut).toBeDisabled();
    await expect(zoomIn).toBeEnabled();
    await screenshot(page, info, 'zoom-grow-200-disabled');

    for (let step = 0; step < 4; step++) await zoomIn.click();
    await expect(page.locator('#zoom')).toHaveText('600%');
    await expect(zoomIn).toBeDisabled();
    const maximum = await camera(page);
    for (const size of [{ width: 390, height: 844 }, { width: 2080, height: 1000 }]) {
      await resize(size.width, size.height);
      expect(await camera(page)).toEqual(maximum);
      await expect(zoomIn).toBeDisabled();
      await expect(zoomOut).toBeEnabled();
    }
    await record('maximum preserved across both resizes');
    await expect(page.locator('#inspection h3')).toHaveText('Ada Rowan');
    const origin = new URL(baseURL!).origin; const mount = new URL(baseURL!).pathname;
    expect(evidence.requests.every((url) => new URL(url).origin === origin && new URL(url).pathname.startsWith(mount))).toBe(true);
    expect(evidence.errors).toEqual([]);
  } finally {
    await info.attach('viewport-zoom-observations', { body: JSON.stringify({ browserVersion: page.context().browser()?.version(), observations, ...evidence }, null, 2), contentType: 'application/json' });
  }
});

for (const target of ['manifest.json', 'colony.png']) {
  test(`missing ${target} gives a visible loading failure`, async ({ page }) => {
    const pageErrors: string[] = [];
    page.on('pageerror', (error) => pageErrors.push(error.message));
    await page.route(`**/game-assets/${target}`, (route) => route.fulfill({ status: 404, body: 'Missing asset' }));
    await page.goto('./');
    await expect(page.getByRole('alert')).toContainText('could not be loaded');
    await expect(page.locator('#stage')).toHaveAttribute('data-ready', 'error');
    if (target === 'colony.png') {
      // Panels exist before the texture finishes loading. Failure must disable scene commands.
      await page.getByRole('button', { name: 'Find Ada Rowan, Builder' }).click();
      await page.getByRole('button', { name: 'Zoom in', exact: true }).click();
      await expect(page.locator('#stage')).toHaveAttribute('data-ready', 'error');
    }
    expect(pageErrors).toEqual([]);
  });
}
