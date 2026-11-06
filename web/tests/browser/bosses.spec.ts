import { test, expect } from '@playwright/test';
import type { Page, TestInfo } from '@playwright/test';
import { readFile } from 'node:fs/promises';
import { PNG } from 'pngjs';
import { parseSave } from '../../src/persistence/schema.ts';
import type { Save } from '../../src/persistence/schema.ts';
import type { Manifest } from '../../src/assets.ts';
import { founders } from '../../src/content.ts';
import { materialAudit } from '../../src/simulation/jobs.ts';
import { same } from '../../src/simulation/model.ts';
import { BOSS_CONTENT } from '../../src/simulation/boss-content.ts';
import { bossCases, bossFixture, bossFixtureDirectory, counterPlan, exactContinuation, pendingCounter, worldHash } from './helpers/boss-fixtures.ts';

type Case = typeof bossCases[number];
async function start(page: Page): Promise<string[]> {
  const failures: string[] = [];
  page.on('pageerror', e => failures.push(e.message)); page.on('console', m => { if (m.type() === 'error') failures.push(m.text()); });
  page.on('requestfailed', r => failures.push(`${r.url()} ${r.failure()?.errorText}`)); page.on('response', r => { if (r.status() >= 400) failures.push(`${r.status()} ${r.url()}`); });
  await page.goto('./'); await expect(page.locator('#stage')).toHaveAttribute('data-ready', 'true'); return failures;
}
async function importCheckpoint(page: Page, seed: number, label: string): Promise<Save> {
  const { saved } = await bossFixture(seed, label);
  await page.locator('#import-file').setInputFiles({ name: `${seed}-${label}.json`, mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(saved)) });
  await expect(page.locator('#storage-status')).toContainText(`Imported and saved tick ${saved.world.tick}.`);
  await expect(page.locator('#tick')).toHaveText(String(saved.world.tick)); await expect(page.locator('#clock-status')).toHaveText('Paused'); return saved;
}
async function exported(page: Page, info: TestInfo, label: string): Promise<Save> {
  const download = page.waitForEvent('download'); await page.locator('#export').click(); const path = info.outputPath(`${label}.json`); await (await download).saveAs(path);
  const saved = parseSave(await readFile(path, 'utf8')); for (const [kind, count] of Object.entries(materialAudit(saved.world))) expect(count.produced, kind).toBe(count.accounted); return saved;
}
async function reloadExact(page: Page, info: TestInfo, label: string): Promise<Save> {
  const before = await exported(page, info, `${label}-before-reload`); await page.locator('#save').click(); await expect(page.locator('#storage-status')).toContainText(`Saved tick ${before.world.tick}`);
  await page.reload(); await expect(page.locator('#stage')).toHaveAttribute('data-ready', 'true'); expect(await exported(page, info, `${label}-after-reload`)).toEqual(before); return before;
}
async function advancePast(page: Page, tick: number): Promise<void> {
  await page.getByRole('button', { name: '1×', exact: true }).click(); await page.locator('#pause').focus(); await page.keyboard.press('Enter');
  await expect.poll(async () => Number(await page.locator('#tick').textContent()), { timeout: 40000, intervals: [50] }).toBeGreaterThanOrEqual(tick);
  await page.keyboard.press('Enter'); await expect(page.locator('#clock-status')).toHaveText('Paused');
}
async function spriteEvidence(page: Page, info: TestInfo, config: Case, saved: Save): Promise<void> {
  const manifestResponse = await page.request.get(new URL('game-assets/manifest.json', page.url()).toString()); expect(manifestResponse.ok()).toBe(true);
  const manifest = await manifestResponse.json() as Manifest, sprite = manifest.sprites[config.alias]!; expect(sprite.id).toBe(config.sprite); expect(sprite.identityConfidence).toBe('high'); expect(sprite.rect.slice(2)).toEqual([12, 12]);
  const imageResponse = await page.request.get(new URL(`game-assets/${manifest.image}`, page.url()).toString()); expect(imageResponse.ok()).toBe(true); const source = PNG.sync.read(await imageResponse.body());
  const boss = saved.world.combat.raiders.find(r => r.kind === config.kind)!; expect(boss).toBeDefined(); expect(boss.next).toBeNull();
  await page.locator(`#raiders li[data-raider="${boss.id}"]`).getByRole('button').click();
  const canvas = PNG.sync.read(await page.locator('canvas').screenshot()), view = JSON.parse((await page.locator('#stage').getAttribute('data-camera'))!) as { x: number; y: number; zoom: number };
  const left = Math.round(canvas.width / 2 + (boss.x * 12 - view.x) * view.zoom), top = Math.round(canvas.height / 2 + (boss.y * 12 - view.y) * view.zoom), [sx, sy] = sprite.frame;
  const colors = { soundseek: 0xffdf88, ring: 0xffdf88, surge: 0x8ac8d8, feast: 0x8fcf9d, partition: 0xd5b5e8, reel: 0xffa0c0, fire: 0xff9a60 };
  const overlays = saved.world.bosses.effects.filter(e => e.cells.some(c => same(c, boss))).map(e => colors[e.kind]);
  const matches: { x: number; y: number }[] = [];
  // Interior opaque source pixels survive authored outlines/facing cues. Match
  // the actual packed crop, hostile tint and committed hazard fills on canvas.
  for (let y = 3; y <= 8; y++) for (let x = 3; x <= 8; x++) {
    const sourceIndex = ((sy! + y) * source.width + sx! + x) * 4; if (source.data[sourceIndex + 3] !== 255) continue;
    const expected = [244, 176, 160].map((tint, channel) => source.data[sourceIndex + channel]! * tint / 255);
    for (const color of overlays) for (let channel = 0; channel < 3; channel++) expected[channel] = expected[channel]! * .82 + ((color >> (16 - channel * 8)) & 255) * .18;
    const destination = ((top + y * view.zoom + Math.floor(view.zoom / 2)) * canvas.width + left + x * view.zoom + Math.floor(view.zoom / 2)) * 4;
    if (expected.every((value, channel) => Math.abs(canvas.data[destination + channel]! - value) <= 4)) matches.push({ x, y });
  }
  expect(matches.length, `${config.name} rendered source-crop pixels`).toBeGreaterThanOrEqual(3);
  await info.attach(`${config.kind}-sprite-evidence`, { body: JSON.stringify({ alias: config.alias, id: sprite.id, source: sprite.source, originalName: sprite.originalName, rect: sprite.rect, frame: sprite.frame, cropRgbaSha256: sprite.cropRgbaSha256, matchedCanvasPixels: matches, overlays }, null, 2), contentType: 'application/json' });
}

for (const config of bossCases) {
  test(`ordinary seed ${config.seed} ${config.name} shows preserved sprites, biome and locked effects through native reload`, async ({ page }, info) => {
    test.setTimeout(120000); const failures = await start(page), evidence: unknown[] = [];
    for (const [n, kind] of config.effects.entries()) {
      const saved = await importCheckpoint(page, config.seed, `first-${kind}`), effect = saved.world.bosses.effects.find(e => e.kind === kind)!;
      expect(effect).toBeDefined(); await expect(page.locator('#biome-name')).toHaveText(config.biome.toUpperCase());
      await expect(page.locator('#biome-description')).toContainText(config.biome === 'ash' ? '2 damage every 50 ticks' : config.biome === 'marsh' ? '6 ticks per step' : 'balanced finite sources');
      await expect(page.locator('#boss-encounters')).toContainText(config.name); await expect(page.locator('#boss-effects')).toContainText(`${kind} #${effect.id}`); await expect(page.locator('#boss-effects')).toContainText(`impact in ${effect.due - saved.world.tick} ticks`);
      for (const cell of effect.cells) await expect(page.locator('#boss-effects')).toContainText(`${cell.x},${cell.y}`);
      if (n === 0) await spriteEvidence(page, info, config, saved);
      else { const row = page.locator(`#raiders li[data-raider="${effect.owner}"]`); if (await row.count()) await row.getByRole('button').click(); }
      await page.screenshot({ path: info.outputPath(`${config.seed}-${kind}-locked.png`), fullPage: true });
      expect((await reloadExact(page, info, `${config.seed}-${kind}`)).world).toEqual(saved.world);
      await advancePast(page, Math.min(effect.due + 1, saved.world.tick + 25)); const continued = await exported(page, info, `${config.seed}-${kind}-continued`); exactContinuation(saved, continued);
      evidence.push({ kind, tick: saved.world.tick, effect, continuedAt: continued.world.tick, hash: worldHash(continued.world) });
    }
    await page.setViewportSize({ width: 390, height: 844 }); await page.locator('#boss-effects').scrollIntoViewIfNeeded();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: info.outputPath(`${config.seed}-boss-narrow.png`), fullPage: true }); expect(failures).toEqual([]);
    await info.attach('ordinary-boss-effect-evidence', { body: JSON.stringify({ fixtureDirectory: bossFixtureDirectory, seed: config.seed, origin: 'Accepted fresh-seed ordinary command replay; native JSON import and IndexedDB reload', effects: evidence, errors: failures }, null, 2), contentType: 'application/json' });
  });

  test(`ordinary seed ${config.seed} selected founder physically completes ${config.mode} through visible counter controls`, async ({ page }, info) => {
    test.setTimeout(120000); const failures = await start(page), initial = await importCheckpoint(page, config.seed, `mid-${config.mode}`), plan = counterPlan(initial, config.mode);
    const person = initial.world.people.find(p => p.id === plan.actor)!, founder = founders[person.founder]!, feature = initial.world.bosses.features.find(f => f.id === plan.target)!;
    await page.getByRole('button', { name: `Find ${founder.name}, ${founder.role}`, exact: true }).click();
    await expect(page.locator('#battle-selected')).toContainText(founder.name);
    const row = page.locator(`#boss-counters li[data-feature="${feature.id}"]`); await expect(row).toHaveAttribute('data-feature-kind', feature.kind);
    await row.getByRole('button', { name: 'Find counter', exact: true }).click();
    const button = row.locator(`button[data-counter="${config.mode}"]`); await expect(button).toBeEnabled(); await button.click();
    await expect(page.locator('#storage-status')).toContainText('Work command queued'); const ordered = await exported(page, info, `${config.seed}-${config.mode}-queued`); expect(pendingCounter(ordered.world, plan.input)).toBe(true);
    expect(ordered.world.bosses.features.find(f => f.id === feature.id)!.health).toBe(feature.health); expect(ordered.world.tick).toBe(initial.world.tick);
    const saved = await reloadExact(page, info, `${config.seed}-${config.mode}-pending`);
    await advancePast(page, plan.completedAt); const completed = await exported(page, info, `${config.seed}-${config.mode}-completed`); exactContinuation(saved, completed);
    expect(completed.world.tick - initial.world.tick).toBeGreaterThanOrEqual(BOSS_CONTENT.counters[config.mode]);
    const afterFeature = completed.world.bosses.features.find(f => f.id === feature.id), encounter = completed.world.bosses.encounters.find(e => e.owner === feature.owner);
    if (config.mode === 'drain') { expect(encounter?.drained[feature.channel!]).toBe(true); await expect(row).toContainText('drained'); await expect(button).toBeDisabled(); }
    else if (config.mode === 'ignite') { expect(completed.world.bosses.effects.some(e => e.kind === 'fire' && e.source === plan.actor && e.feature === feature.id)).toBe(true); await expect(page.locator('#boss-effects')).toContainText('fire'); }
    else { expect(afterFeature?.health ?? 0).toBeLessThan(feature.health); expect(encounter!.exposedUntil).toBeGreaterThan(completed.world.tick); await expect(page.locator('#boss-encounters')).toContainText('exposed'); }
    await page.screenshot({ path: info.outputPath(`${config.seed}-${config.mode}-completed.png`), fullPage: true }); await reloadExact(page, info, `${config.seed}-${config.mode}-completed`);
    const final = await importCheckpoint(page, config.seed, 'final'); expect(final.world.people.every(p => p.life === 'alive' && p.body.health === 100)).toBe(true); expect(final.world.combat.stats.bosses).toBe(1);
    await page.setViewportSize({ width: 390, height: 844 }); await page.locator('#boss-encounters').scrollIntoViewIfNeeded(); expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: info.outputPath(`${config.seed}-recovered-narrow.png`), fullPage: true }); expect(failures).toEqual([]);
    await info.attach('paid-boss-counter-evidence', { body: JSON.stringify({ seed: config.seed, origin: 'Accepted ordinary mid-counter save; visible selected-founder button queues a real battle command', plan, initialTick: initial.world.tick, completedTick: completed.world.tick, completedHash: worldHash(completed.world), finalHash: worldHash(final.world), errors: failures }, null, 2), contentType: 'application/json' });
  });
}
