import { test, expect } from '@playwright/test';
import type { Page, TestInfo } from '@playwright/test';
import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { parseSave } from '../../src/persistence/schema.ts';
import type { Save } from '../../src/persistence/schema.ts';
import { canonical } from '../../src/simulation/canonical.ts';
import { COMBAT_CONTENT } from '../../src/simulation/combat-content.ts';
import { distance, same } from '../../src/simulation/model.ts';
import { enqueue, step } from '../../src/simulation/step.ts';
import { materialAudit } from '../../src/simulation/jobs.ts';

// These are accepted ordinary-command checkpoints, not injected inventory or
// browser globals. The scenario runner supplies provenance and fresh-seed replay.
const directory = resolve(process.env.ROAG_RAID_FIXTURE_DIR ?? resolve(import.meta.dirname, '../..', process.env.ROAG_EVIDENCE_DIR ?? '../artifacts/colony-web/P06', 'acceptance-scenarios'));
const hash = (value: unknown): string => createHash('sha256').update(canonical(value)).digest('hex');
async function fixture(label: string): Promise<Save> {
  const metadata = JSON.parse(await readFile(resolve(directory, 'raid-scenarios.json'), 'utf8'));
  expect(metadata.origin).toContain('ordinary commands');
  expect(metadata.final.completed).toBe(5); expect(metadata.final.combat.bosses).toBe(1);
  expect(metadata.replayHash).toBe(metadata.final.hash);
  expect(metadata.continuations.some((c: { label: string }) => c.label === label)).toBe(true);
  const saved = parseSave(await readFile(resolve(directory, `${label}.json`), 'utf8'));
  expect(saved.world.scenario).toBe('colony'); expect(saved.controls.manualPaused).toBe(true);
  return saved;
}
async function start(page: Page): Promise<string[]> {
  const failures: string[] = [];
  page.on('pageerror', error => failures.push(error.message));
  page.on('console', message => { if (message.type() === 'error') failures.push(message.text()); });
  page.on('requestfailed', request => failures.push(`${request.url()} ${request.failure()?.errorText}`));
  page.on('response', response => { if (response.status() >= 400) failures.push(`${response.status()} ${response.url()}`); });
  await page.goto('./'); await expect(page.locator('#stage')).toHaveAttribute('data-ready', 'true');
  return failures;
}
async function importCheckpoint(page: Page, label: string): Promise<Save> {
  const saved = await fixture(label);
  await page.locator('#import-file').setInputFiles({ name: `${label}.json`, mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(saved)) });
  await expect(page.locator('#storage-status')).toContainText(`Imported and saved tick ${saved.world.tick}.`);
  await expect(page.locator('#tick')).toHaveText(String(saved.world.tick));
  await expect(page.locator('#clock-status')).toHaveText('Paused');
  return saved;
}
async function exported(page: Page, info: TestInfo, label: string): Promise<Save> {
  const pending = page.waitForEvent('download'); await page.locator('#export').click();
  const file = info.outputPath(`${label}.json`); await (await pending).saveAs(file);
  const saved = parseSave(await readFile(file, 'utf8'));
  for (const [kind, count] of Object.entries(materialAudit(saved.world))) expect(count.produced, kind).toBe(count.accounted);
  return saved;
}
async function reloadExact(page: Page, info: TestInfo, label: string): Promise<Save> {
  const before = await exported(page, info, `${label}-before-reload`);
  await page.locator('#save').click(); await expect(page.locator('#storage-status')).toContainText(`Saved tick ${before.world.tick}`);
  await page.reload(); await expect(page.locator('#stage')).toHaveAttribute('data-ready', 'true');
  expect(await exported(page, info, `${label}-after-reload`)).toEqual(before);
  return before;
}
async function pause(page: Page): Promise<void> {
  if (await page.locator('#pause').textContent() === 'Pause') await page.locator('#pause').click();
  await expect(page.locator('#clock-status')).toHaveText('Paused');
}
async function resume(page: Page): Promise<void> {
  if (await page.locator('#pause').textContent() === 'Resume') await page.locator('#pause').click();
}
function expectedContinuation(before: Save, after: Save): void {
  const expected = structuredClone(before.world);
  while (expected.tick < after.world.tick) step(expected);
  expect(hash(after.world)).toBe(hash(expected));
}

test('ordinary recurring-wave checkpoints show warning budgets, recovery history and exact later-wave reload', async ({ page }, info) => {
  test.setTimeout(120000);
  const failures = await start(page), warning = await importCheckpoint(page, 'wave-4-warning');
  await expect(page.locator('#raid-phase')).toHaveText('warning');
  await expect(page.locator('#raid-announcement')).toContainText('Ossuary Captain');
  await expect(page.locator('#raid-budget')).toContainText(`Wave 4 budget: ${warning.world.combat.spent}/${warning.world.combat.budget}`);
  await expect(page.locator('#raid-timing')).toContainText(`${warning.world.combat.spawnAt - warning.world.tick} ticks`);
  await expect(page.locator('#raid-history-body tr')).toHaveCount(3);
  await page.locator('#battle-find-entry').click();
  await page.screenshot({ path: info.outputPath('ordinary-wave-four-warning.png'), fullPage: true });
  expect((await reloadExact(page, info, 'ordinary-warning')).world).toEqual(warning.world);

  const later = await importCheckpoint(page, 'wave-5-warning');
  await expect(page.locator('#raid-announcement')).toContainText('wave 5');
  await expect(page.locator('#battle-statistics')).toContainText('1 Captains defeated');
  await expect(page.locator('#raid-history-body tr')).toHaveCount(4);
  await resume(page); await expect.poll(async () => Number(await page.locator('#tick').textContent()), { timeout: 10000 }).toBeGreaterThan(later.world.tick + 20);
  await pause(page); const continued = await exported(page, info, 'ordinary-later-wave-continued');
  expectedContinuation(later, continued); expect(continued.world.terminal).toBeNull();

  const final = await importCheckpoint(page, 'final');
  await expect(page.locator('#raid-phase')).toHaveText('recovery');
  await expect(page.locator('#raid-timing')).toContainText('Wave 6 warning in');
  await expect(page.locator('#raid-history-body tr')).toHaveCount(5);
  for (const summary of final.world.combat.history) {
    const values = await page.locator(`#raid-history-body tr[data-wave="${summary.wave}"] td`).allTextContents();
    expect(values).toEqual([String(summary.wave), `${summary.spent}/${summary.budget}`, String(summary.kills), String(summary.colonistsLost), String(summary.ammoSpent), String(summary.structuresLost), String(summary.repairs), `${summary.startedAt}–${summary.endedAt}`]);
  }
  await page.setViewportSize({ width: 390, height: 844 });
  await page.locator('#raid-history-heading').scrollIntoViewIfNeeded();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath('ordinary-five-wave-history-390.png'), fullPage: true });
  expect(failures).toEqual([]);
  await info.attach('recurring-raid-evidence', { body: JSON.stringify({ origin: 'Imported accepted ordinary-command scenario checkpoints through native JSON input', fixtureDirectory: directory, warningHash: hash(warning.world), continuation: { before: later.world.tick, after: continued.world.tick, hash: hash(continued.world) }, history: final.world.combat.history, errors: failures }, null, 2), contentType: 'application/json' });
});

test('ordinary squad and Captain checkpoints expose facing, support and distinct locked telegraphs through native reload', async ({ page }, info) => {
  test.setTimeout(120000);
  const failures = await start(page), evidence: unknown[] = [];
  for (const kind of ['arrow', 'heal', 'charge', 'rampart'] as const) {
    const saved = await importCheckpoint(page, `first-${kind}`), attack = saved.world.combat.attacks.find(a => a.kind === kind)!;
    expect(attack).toBeDefined();
    await expect(page.locator('#attack-summary')).toContainText(`Hostile ${kind}:`);
    await expect(page.locator('#attack-summary')).toContainText(`${attack.cells.length} locked`);
    for (const enemy of saved.world.combat.raiders) {
      const row = page.locator(`#raiders li[data-raider="${enemy.id}"]`);
      await expect(row).toHaveAttribute('data-role', enemy.kind);
      await expect(row).toContainText(`health ${enemy.health}/${COMBAT_CONTENT[enemy.kind].health}`);
      await expect(row).toContainText(`Facing ${enemy.facing}`);
      if (enemy.squadId !== null) await expect(row).toContainText(`Squad #${enemy.squadId}`);
    }
    const source = page.locator(`#raiders li[data-raider="${attack.source.id}"]`);
    if (kind === 'heal') await expect(source).toContainText(`Healing #${attack.recipient}`);
    if (kind === 'charge' || kind === 'rampart') await expect(source).toContainText('Ossuary Captain');
    await source.getByRole('button').click();
    await page.screenshot({ path: info.outputPath(`ordinary-${kind}-locked-cells.png`), fullPage: true });
    expect((await reloadExact(page, info, `ordinary-${kind}`)).world).toEqual(saved.world);
    evidence.push({ kind, tick: saved.world.tick, attack, hash: hash(saved.world) });
  }
  const moving = await importCheckpoint(page, 'mid-charge'), captain = moving.world.combat.raiders.find(r => r.charge)!;
  await expect(page.locator(`#raiders li[data-raider="${captain.id}"]`)).toContainText(`Charging ${captain.charge!.cursor}/${captain.charge!.cells.length}`);
  await reloadExact(page, info, 'ordinary-mid-charge');
  await page.getByRole('button', { name: '1×', exact: true }).click();
  await resume(page); await expect.poll(async () => Number(await page.locator('#tick').textContent())).toBeGreaterThan(moving.world.tick + 5);
  await pause(page); const continued = await exported(page, info, 'ordinary-charge-continued');
  expectedContinuation(moving, continued);
  expect(failures).toEqual([]);
  await info.attach('encounter-presentation-evidence', { body: JSON.stringify({ origin: 'Imported ordinary encounter checkpoints; no world injection', checkpoints: evidence, continuation: { start: moving.world.tick, end: continued.world.tick, hash: hash(continued.world) }, errors: failures }, null, 2), contentType: 'application/json' });
});

test('ordinary hostile barricades can be focused and physically cleared through visible founder orders', async ({ page }, info) => {
  test.setTimeout(120000);
  const failures = await start(page), initial = await importCheckpoint(page, 'first-barricades');
  const barriers = [...initial.world.combat.barricades].sort((a, b) => Math.min(...initial.world.people.filter(p => p.life === 'alive').map(p => distance(p, a))) - Math.min(...initial.world.people.filter(p => p.life === 'alive').map(p => distance(p, b))) || a.id - b.id);
  // Choose an accessible target by replaying the same legal command in an isolated
  // copy. This never installs or mutates browser state, supplies, health or terrain.
  const barrier = barriers.find(candidate => {
    const copy = structuredClone(initial.world);
    if (!enqueue(copy, { kind: 'battle', actor: 0, mode: 'clear', target: { x: candidate.x, y: candidate.y } })) return false;
    let struck = false;
    for (let i = 0; i < 250 && !copy.terminal; i++) {
      step(copy);
      struck ||= copy.combat.attacks.some(a => a.source.kind === 'person' && a.kind === 'melee' && same(a.target, candidate));
      if (!copy.combat.barricades.some(b => b.id === candidate.id)) return struck && copy.combat.raiders.some(r => r.id === candidate.owner) && copy.tick < candidate.expiresAt;
    }
    return false;
  });
  expect(barrier, 'ordinary checkpoint offers a physical clear before owner death/expiry').toBeDefined();
  const row = page.locator(`#barricades li[data-barricade="${barrier!.id}"]`);
  await expect(row).toContainText(`health ${barrier!.health}/36`); await expect(row).toContainText('Blocks movement and shots');
  await row.getByRole('button', { name: 'Find / select barricade', exact: true }).click();
  await expect(page.locator('#battle-mode')).toHaveValue('clear'); await expect(page.locator('#battle-x')).toHaveValue(String(barrier!.x)); await expect(page.locator('#battle-y')).toHaveValue(String(barrier!.y));
  const camera = JSON.parse((await page.locator('#stage').getAttribute('data-camera'))!);
  expect(camera.x).toBe((barrier!.x + .5) * 12); expect(camera.y).toBe((barrier!.y + .5) * 12);
  await page.screenshot({ path: info.outputPath('ordinary-hostile-barricades.png'), fullPage: true });
  await row.getByRole('button', { name: 'Order all to clear', exact: true }).click();
  await expect(page.locator('#storage-status')).toContainText('Work command queued');
  const ordered = await exported(page, info, 'ordinary-clear-command');
  expect(ordered.world.pending.some(c => c.input.kind === 'battle' && c.input.mode === 'clear' && same(c.input.target, barrier!))).toBe(true);
  await resume(page); await expect(row).toHaveCount(0, { timeout: 30000 }); await pause(page);
  const cleared = await exported(page, info, 'ordinary-barricade-cleared');
  expectedContinuation(ordered, cleared);
  expect(cleared.world.tick).toBeLessThan(barrier!.expiresAt);
  expect(cleared.world.combat.raiders.some(r => r.id === barrier!.owner)).toBe(true);
  expect(cleared.world.combat.barricades.some(b => b.id === barrier!.id)).toBe(false);
  await reloadExact(page, info, 'ordinary-cleared-wall');
  expect(failures).toEqual([]);
  await info.attach('physical-clearing-evidence', { body: JSON.stringify({ origin: 'Imported ordinary checkpoint; visible clear-all command', barrier, start: ordered.world.tick, end: cleared.world.tick, ownerStillAlive: true, hash: hash(cleared.world), errors: failures }, null, 2), contentType: 'application/json' });
});
