// Release UI regression in an isolated browser; gameplay is driven only by input.
const {chromium} = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const url = process.env.SETTLEMENT_URL || 'http://127.0.0.1:8765/roag/settlement/';
const out = path.resolve(process.argv[2] || '/var/tmp/roag-sidebar-browser');
const key = 'roag.settlement.landscape.v1';
fs.mkdirSync(out, {recursive: true});
let browser, page;
(async () => {
  browser = await chromium.launch({executablePath: process.env.CHROMIUM_BIN || '/usr/bin/chromium', headless: true, args: ['--no-sandbox']});
  const context = await browser.newContext({viewport: {width: 1024, height: 640}, acceptDownloads: true});
  const fixture = fs.readFileSync(path.resolve(__dirname, '../test/fixtures/goal09-town.json'), 'utf8');
  await context.addInitScript(({key, fixture}) => {if (!localStorage.getItem(key)) localStorage.setItem(key, fixture);}, {key, fixture});
  page = await context.newPage();
  const errors = [], failedAssets = [], checks = [];
  page.on('pageerror', e => errors.push(e.message));
  page.on('console', m => {if (m.type() === 'error') errors.push(m.text());});
  page.on('response', r => {if (r.status() >= 400) failedAssets.push([r.status(), r.url()]);});
  const btn = name => page.getByRole('button', {name, exact: typeof name === 'string'});
  const state = () => page.evaluate(k => JSON.parse(localStorage.getItem(k)), key);
  const world = () => page.getByRole('group', {name: 'Settlement world', exact: true}).boundingBox();
  const hud = () => page.getByRole('group', {name: 'Settlement controls', exact: true}).boundingBox();
  const pass = text => {checks.push(text); console.log('PASS', text);};
  const snap = async name => {const h = await hud(); await page.mouse.move(h.x + 6, 8); await page.waitForTimeout(100); await page.screenshot({path: path.join(out, name + '.png')});};
  async function ready() {
    await page.locator('#loading').waitFor({state: 'detached'});
    const enable = page.locator('flt-semantics-placeholder');
    if (await enable.count()) await enable.press('Enter');
    await btn(/^1 Cottage ·/).waitFor();
  }
  async function scrollTo(item) {
    const h = await hud();
    for (let i = 0; i < 24; i++) {
      const box = await item.boundingBox();
      if (box && box.y >= 180 && box.y + box.height <= h.height - 8) return;
      await page.mouse.move(h.x + h.width / 2, h.height - 70);
      await page.mouse.wheel(0, box && box.y < 180 ? -350 : 350);
      await page.waitForTimeout(80);
    }
    throw Error('Sidebar item could not be scrolled into view');
  }
  async function tool(label) {
    const escaped = label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const item = btn(new RegExp('^(?:[0-9] )?' + escaped + ' ·'));
    await scrollTo(item);
    const box = await item.boundingBox(), h = await hud();
    assert(box.x >= h.x, label + ' stays in the sidebar');
    await item.click();
  }
  async function point(x, y) {
    const w = await world();
    const cx = Math.max(9, (w.width / 2 - 24) / 48);
    return [w.x + w.width / 2 + (x + .5 - cx) * 48, w.y + w.height / 2 + (y - 15) * 48];
  }
  async function clickCell(x, y) {await page.mouse.click(...await point(x, y)); await page.waitForTimeout(100);}
  const lot = (s, x, y) => s.lots[y * s.width + x];
  await page.goto(url);
  await ready();
  const initial = await state(), old = JSON.parse(fixture);
  assert.deepEqual(initial.lots, old.lots);
  assert.deepEqual(initial.colony.residents, old.colony.residents);
  assert.equal(initial.colony.money, old.colony.money);
  assert.equal(initial.colony.clock, old.colony.clock);
  const tree = await page.locator('body').ariaSnapshot();
  assert(!tree.includes('A place to return'));
  assert(!tree.includes('Homes, farms, parks'));
  assert(!tree.includes('Build catalogue (B)'));
  pass('Developed legacy town retained; removed title, tagline and old tool strip are absent');
  for (const size of [{width: 800, height: 600}, {width: 1024, height: 640}, {width: 1440, height: 900}]) {
    await page.setViewportSize(size);
    await page.waitForTimeout(200);
    const w = await world(), h = await hud();
    assert(w.width > size.width / 2);
    assert.equal(w.y, 0);
    assert.equal(w.height, size.height);
    assert(Math.abs(w.x + w.width - h.x) < 1);
    assert(Math.abs(h.x + h.width - size.width) < 1);
    await snap('sidebar-' + size.width);
  }
  await page.setViewportSize({width: 1024, height: 640});
  await btn('Return to lane (H)').click();
  pass('World takes the majority and full height at 800×600, 1024×640 and 1440×900');
  for (const label of ['Cottage', 'Farm', 'Workshop', 'Market', 'Inn', 'Firehouse', 'Watchpost', 'Refuge', 'Repair', 'Park', 'Community Hall', 'Path', 'Stone Road', 'Garden Path', 'Clear', 'Plant', 'Bridge', 'Remove']) await tool(label);
  await snap('sidebar-landscape-tools');
  assert.deepEqual(await state(), initial);
  await page.keyboard.press('b');
  await page.waitForTimeout(220);
  const construction = await page.getByText('Construction', {exact: true}).boundingBox();
  assert(construction.y >= 175 && construction.y < 640);
  assert.equal(await page.getByRole('menu').count(), 0);
  pass('All 18 tools selectable in the scrollable HUD; B finds catalogue without a popup; UI leaves saved state unchanged');
  await tool('Cottage');
  await clickCell(9, 14);
  let built = await state();
  assert(lot(built, 9, 14)[4]);
  assert.equal(built.colony.money, initial.colony.money - 5000);
  assert.equal(built.colony.clock, initial.colony.clock);
  await btn('Undo').click();
  assert.deepEqual((await state()).lots, initial.lots);
  assert.equal((await state()).colony.money, initial.colony.money);
  await btn('Redo').click();
  assert.deepEqual((await state()).lots, built.lots);
  pass('Sidebar construction uses normal price and placement; Undo/Redo restores lots and finances without advancing time');
  await tool('Bridge');
  await page.keyboard.press('i');
  await clickCell(9, 14);
  const move = btn('Move building · Free (M)');
  await move.waitFor();
  const mb = await move.boundingBox(), h = await hud();
  assert(mb.x >= h.x && mb.y >= 180 && mb.y < 640);
  await snap('sidebar-inspection');
  await move.click();
  await clickCell(9, 16);
  const moved = await state();
  assert.equal(lot(moved, 9, 16)[5], lot(built, 9, 14)[5]);
  assert.equal(lot(moved, 9, 14)[4], null);
  assert.equal(moved.colony.money, built.colony.money);
  pass('Inspection becomes visible from the bottom of the catalogue; relocation preserves identity and money');
  await page.keyboard.press('i');
  await clickCell(12, 15);
  await btn('Talk').click();
  await btn('Close conversation').waitFor();
  await snap('sidebar-conversation');
  await btn('Close conversation').click();
  await page.keyboard.press('Escape');
  pass('Resident conversation uses the sidebar and closes normally');
  await page.keyboard.press('2');
  await page.mouse.move(...await point(11, 15));
  await page.mouse.down();
  await page.mouse.move(...await point(11, 17), {steps: 12});
  await page.mouse.up();
  await page.waitForTimeout(120);
  const roads = await state();
  assert.equal(lot(roads, 11, 16)[3], true);
  assert.equal(lot(roads, 11, 17)[3], true);
  assert.equal(roads.colony.money, moved.colony.money);
  await page.keyboard.press('Control+z');
  assert.deepEqual((await state()).lots, moved.lots);
  await page.keyboard.press('Control+Shift+z');
  const checkpoint = await state();
  assert.deepEqual(checkpoint.lots, roads.lots);
  assert.equal(checkpoint.colony.money, roads.colony.money);
  assert.equal(checkpoint.colony.clock, roads.colony.clock);
  assert.equal(checkpoint.colony.navigationRevision, roads.colony.navigationRevision + 2);
  await btn('Mute').click();
  await btn('Unmute').click();
  await btn('Zoom in (+)').click();
  await btn('Zoom out (−)').click();
  await btn('Return to lane (H)').click();
  assert.deepEqual(await state(), checkpoint);
  pass('Ordinary drag painting, keyboard shortcuts, mute and camera controls remain independent of sidebar scrolling');
  await btn('Landscape menu').click();
  const download = page.waitForEvent('download');
  await page.getByRole('menuitem', {name: 'Export landscape…', exact: true}).click();
  const file = await download;
  const exported = path.join(out, 'sidebar-town.json');
  await file.saveAs(exported);
  assert.deepEqual(JSON.parse(fs.readFileSync(exported)), checkpoint);
  await page.reload();
  await ready();
  assert.deepEqual(await state(), checkpoint);
  await snap('sidebar-final');
  pass('Landscape menu export and paused reload retain the resulting settlement exactly');
  assert.deepEqual(errors, []);
  assert.deepEqual(failedAssets, []);
  fs.writeFileSync(path.join(out, 'results.json'), JSON.stringify({url, input: 'Playwright mouse, wheel and keyboard in the real release browser, isolated context and explicit legacy-town fixture; no gameplay callbacks or private browser storage.', checks, errors, failedAssets}, null, 2));
  await context.close();
  await browser.close();
})().catch(async e => {
  console.error(e);
  if (page) {
    await page.screenshot({path: path.join(out, 'error.png')}).catch(() => {});
    console.error((await page.locator('body').ariaSnapshot().catch(() => '')).slice(0, 6500));
  }
  if (browser) await browser.close();
  process.exitCode = 1;
});
