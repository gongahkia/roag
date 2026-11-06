import { test, expect } from '@playwright/test';
import type { Locator, Page, TestInfo } from '@playwright/test';
import { readFile } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import type { Save } from '../../src/persistence/schema.ts';
import { canonical } from '../../src/simulation/canonical.ts';
import { materialAudit } from '../../src/simulation/jobs.ts';
import { step } from '../../src/simulation/step.ts';

const hash = (value: unknown): string => createHash('sha256').update(canonical(value)).digest('hex');
// An unloaded forward tower draws the breacher; the loaded tower at 71,60 covers it.
// Its real battle damage supplies the ordinary repair portion of the journey.
const towers = [[71, 60], [64, 69], [60, 63]] as const;
const structure = (page: Page, kind: string): Locator => page.locator('#structures .work-card').filter({ has: page.locator('h4', { hasText: new RegExp(` ${kind} ·`) }) });
const at = (page: Page, x: number, y: number): Locator => page.locator('#structures .work-card').filter({ has: page.locator('h4', { hasText: new RegExp(` · ${x},${y}$`) }) });
const job = (page: Page, name: string): Locator => page.locator('#jobs .work-card').filter({ has: page.locator('h4', { hasText: name }) });
async function pause(page: Page): Promise<void> {
  if (await page.locator('#pause').textContent() === 'Pause') await page.locator('#pause').click();
  await expect(page.locator('#clock-status')).toHaveText('Paused');
}
async function resume(page: Page): Promise<void> {
  if (await page.locator('#pause').textContent() === 'Resume') await page.locator('#pause').click();
}
async function snapshot(page: Page, info: TestInfo, name: string): Promise<Save> {
  const pending = page.waitForEvent('download'); await page.locator('#export').click();
  const download = await pending, path = info.outputPath(`${name}.json`); await download.saveAs(path);
  const save = JSON.parse(await readFile(path, 'utf8')) as Save;
  for (const [kind, audit] of Object.entries(materialAudit(save.world))) expect(audit.produced, kind).toBe(audit.accounted);
  return save;
}
async function place(page: Page, kind: string, x: number, y: number): Promise<void> {
  await page.locator('#building').selectOption(kind);
  await page.locator('#build-x').fill(String(x)); await page.locator('#build-y').fill(String(y));
  await page.locator('#build-form').getByRole('button', { name: 'Place designation', exact: true }).click();
  await expect(page.locator('#storage-status')).toContainText('Work command queued');
}
async function build(page: Page, kind: string, x: number, y: number): Promise<Locator> {
  await place(page, kind, x, y); await resume(page);
  const completed = at(page, x, y); await expect(completed).toHaveCount(1, { timeout: 60000 }); await pause(page);
  return completed;
}
async function produce(page: Page, facility: Locator, recipe: string): Promise<void> {
  await facility.getByRole('button', { name: `Produce ${recipe}`, exact: true }).click();
  await expect(page.locator('#storage-status')).toContainText('Work command queued');
}
async function order(page: Page, mode: 'rally' | 'retreat' | 'shelter', x: number, y: number): Promise<void> {
  await page.locator('#battle-mode').selectOption(mode);
  await page.locator('#battle-x').fill(String(x)); await page.locator('#battle-y').fill(String(y));
  await page.locator('#battle-submit').click(); await expect(page.locator('#storage-status')).toContainText('Work command queued');
}

test('ordinary colony gathers, builds and reloads finite defenses; survives a saved raid and resumes work', async ({ page }, info) => {
  test.setTimeout(600000);
  const failures: string[] = [], commands: string[] = [];
  page.on('pageerror', error => failures.push(error.message));
  page.on('console', message => { if (message.type() === 'error') failures.push(message.text()); });
  page.on('requestfailed', request => failures.push(`${request.url()} ${request.failure()?.errorText}`));
  page.on('response', response => { if (response.status() >= 400) failures.push(`${response.status()} ${response.url()}`); });
  await page.goto('./'); await expect(page.locator('#stage')).toHaveAttribute('data-ready', 'true');
  await expect(page.locator('#battle-submit')).toBeDisabled();
  await expect(page.locator('#battle-find-entry')).toBeDisabled();
  await expect(page.locator('#heart-health')).toHaveText('100');
  await page.getByRole('button', { name: '4×', exact: true }).click();

  // Every material, structure, ammunition round and order comes through ordinary UI.
  for (const x of [63, 62, 61, 60, 59, 58, 57, 56]) { await place(page, 'stockpile', x, 66); commands.push(`stockpile ${x},66`); }
  for (const id of [11, 15, 12, 18]) {
    await page.locator('#work-source').selectOption(String(id));
    await page.getByRole('button', { name: 'Designate gathering', exact: true }).click(); commands.push(`gather ${id}`);
  }
  const farm = await build(page, 'farm', 68, 64);
  await farm.getByRole('button', { name: 'Plant / tend / harvest', exact: true }).click();
  const kitchen = await build(page, 'kitchen', 70, 64); await produce(page, kitchen, 'meal');
  for (const x of [66, 68, 70]) await place(page, 'bed', x, 68);
  const clinic = await build(page, 'infirmary', 72, 68), workshop = await build(page, 'workshop', 74, 68);
  await produce(page, workshop, 'ore'); await produce(page, clinic, 'medicine'); await resume(page);
  await expect(job(page, 'produce medicine').locator('.muted')).toContainText(/· [2-9]\d* cycles/, { timeout: 60000 }); await pause(page);
  await job(page, 'produce ore').getByRole('button', { name: 'Cancel', exact: true }).click(); await resume(page);
  await expect(job(page, 'produce ore')).toContainText('CANCELLED'); await pause(page);
  await produce(page, workshop, 'ammo');
  commands.push('farm 68,64; kitchen 70,64; beds 66/68/70,68; clinic 72,68; workshop 74,68', 'farm, meals, ore and medicine through production; cancel ore; produce ammunition');

  for (const [x, y] of towers) await place(page, 'tower', x, y);
  for (const [kind, x, y] of [['tower', 70, 62], ['trap', 64, 58], ['guardpost', 62, 62], ['wall', 65, 60], ['door', 64, 60]] as const) await place(page, kind, x, y);
  await resume(page); await expect(structure(page, 'tower')).toHaveCount(4, { timeout: 90000 });
  for (const [x, y] of [[64, 58], [62, 62], [65, 60], [64, 60]]) await expect(at(page, x!, y!)).toHaveCount(1, { timeout: 60000 });
  await pause(page);
  for (let batch = 1; batch <= 3; batch++) {
    for (const [x, y] of towers) await at(page, x, y).getByRole('button', { name: 'Reload 4 ammo', exact: true }).click();
    await resume(page);
    for (const [x, y] of towers) await expect(at(page, x, y)).toContainText(`Ammo ${batch * 4}/12`, { timeout: 60000 });
    await pause(page);
  }
  await at(page, 64, 58).getByRole('button', { name: 'Reload 1 ammo', exact: true }).click(); await resume(page);
  await expect(at(page, 64, 58)).toContainText('Ammo 1/1', { timeout: 30000 }); await pause(page);
  const prepared = await snapshot(page, info, 'ordinary-prepared-defense');
  expect(prepared.world.scenario).toBe('colony'); expect(prepared.world.recipeCounts.ammo).toBeGreaterThanOrEqual(10);
  expect(prepared.world.structures.filter(s => s.kind === 'tower' && !(s.x === 70 && s.y === 62)).every(s => s.inventory.ammo === 12)).toBe(true);
  expect(prepared.world.structures.find(s => s.kind === 'tower' && s.x === 70 && s.y === 62)?.inventory.ammo).toBe(0);
  expect(prepared.world.people.every(person => person.life === 'alive')).toBe(true);
  commands.push('four worker-built towers, trap, guard post, wall and gate', 'three physical reload batches for towers 71,60 / 64,69 / 60,63; one trap round', 'front tower 70,62 deliberately unloaded and screened by 71,60 cover; real battle damage requires repair');
  await page.locator('#battle-find-heart').click(); await page.screenshot({ path: info.outputPath('ordinary-loaded-defenses.png'), fullPage: true });

  await resume(page); await expect(page.locator('#raid-phase')).toHaveText('warning', { timeout: 260000 }); await pause(page);
  await expect(page.locator('#raid-announcement')).toContainText(/approaches from the (north|east|south|west)/);
  await page.locator('#battle-find-entry').click(); await page.screenshot({ path: info.outputPath('announced-approach.png'), fullPage: true });
  // Keep a capable healer behind the western cover. The endless director's fair
  // routing makes leaving all six civilians in the same exposed cluster unsafe.
  // This is an ordinary player order, not altered health or a delayed next raid.
  await page.getByLabel('Bram Reed healer priority', { exact: true }).selectOption('3');
  await page.getByRole('button', { name: 'Find Bram Reed, Gatherer', exact: true }).click();
  await order(page, 'shelter', 57, 64); await resume(page);
  await expect(page.locator('#battle-current-order')).toHaveText('shelter at 57, 64'); await pause(page);
  commands.push('warning: Bram healer priority 3 and shelter 57,64 behind western cover; preserve a capable rescuer');
  await page.getByRole('button', { name: 'Find Nell Vale, Guard', exact: true }).click();
  await order(page, 'rally', 67, 63); await resume(page); await expect(page.locator('#battle-current-order')).toHaveText('rally at 67, 63'); await pause(page);
  await order(page, 'retreat', 62, 64); await resume(page); await expect(page.locator('#battle-current-order')).toHaveText('retreat at 62, 64'); await pause(page);
  await order(page, 'shelter', 62, 65); await resume(page); await expect(page.locator('#battle-current-order')).toHaveText('shelter at 62, 65'); await pause(page);
  await page.locator('#battle-release').click(); await resume(page); await expect(page.locator('#battle-current-order')).toContainText('No defense order');
  commands.push('warning: inspect approach, Nell rally / retreat / shelter / release using visible coordinates');
  await expect(page.locator('#raid-phase')).toHaveText('combat', { timeout: 45000 });
  await page.getByRole('button', { name: '1×', exact: true }).click();
  await expect(page.locator('#raiders li')).not.toHaveCount(0);
  await page.locator('#raiders li button').first().click();
  // Keep the native Pause button focused before watching the short telegraph. Scrolling
  // from the enemy list to click it can consume the entire three-tick windup.
  let observedAttack: Save | null = null;
  for (let attempt = 0; attempt < 6; attempt++) {
    await resume(page); await page.locator('#pause').focus();
    await expect(page.locator('#attack-summary')).toContainText('impact in', { timeout: 90000 });
    await page.keyboard.press('Enter'); await expect(page.locator('#clock-status')).toHaveText('Paused');
    const observed = await snapshot(page, info, `mid-wave-pause-attempt-${attempt}`);
    if (observed.world.combat.attacks.length) { observedAttack = observed; break; }
    expect(observed.world.combat.phase).toBe('combat');
  }
  expect(observedAttack, 'Native pause captures a pending attack before impact').not.toBeNull();
  const midWave = await snapshot(page, info, 'mid-wave-before-real-reload');
  expect(midWave.world.combat.phase).toBe('combat'); expect(midWave.world.combat.raiders.length).toBeGreaterThan(0);
  expect(midWave.world.combat.attacks.length).toBeGreaterThan(0);
  await page.locator('#raiders li button').first().click();
  await page.screenshot({ path: info.outputPath('locked-attack-warning.png'), fullPage: true });
  await page.locator('#save').click(); await expect(page.locator('#storage-status')).toContainText(`Saved tick ${midWave.world.tick}`);
  await page.reload(); await expect(page.locator('#stage')).toHaveAttribute('data-ready', 'true');
  expect(await snapshot(page, info, 'mid-wave-after-real-reload')).toEqual(midWave);
  await page.getByRole('button', { name: '4×', exact: true }).click(); await resume(page);
  await expect(page.locator('#raid-phase')).toHaveText('recovery', { timeout: 90000 }); await pause(page);
  const survived = await snapshot(page, info, 'ordinary-wave-survived');
  expect(survived.world.terminal).toBeNull(); expect(survived.world.heart.health).toBeGreaterThan(0);
  expect(survived.world.combat.completed).toBe(1); expect(survived.world.combat.stats.kills).toBe(5);
  expect(survived.world.combat.stats.shots).toBeGreaterThan(0); expect(survived.world.consumed.ammo).toBeGreaterThan(0);
  expect(survived.world.combat.stats.colonyDamage).toBeGreaterThan(prepared.world.combat.stats.colonyDamage);
  const expected = structuredClone(midWave.world); while (expected.tick < survived.world.tick) step(expected);
  expect(hash(survived.world)).toBe(hash(expected));
  await page.locator('#battle-find-heart').click(); await page.screenshot({ path: info.outputPath('ordinary-wave-survived.png'), fullPage: true });

  if (await page.locator('#battle-release-all').isEnabled()) await page.locator('#battle-release-all').click();
  const front = survived.world.structures.find(s => s.kind === 'tower' && s.x === 70 && s.y === 62);
  expect(front?.health).toBeGreaterThan(0); expect(front?.health).toBeLessThan(100);
  const damaged = survived.world.structures.filter(s => s.health < 100);
  for (const s of damaged) await at(page, s.x, s.y).getByRole('button', { name: 'Repair', exact: true }).click();
  // Raid injuries require fresh medicine. The workshop has one recipe order at a
  // time, so switch its real production from ammunition back to medicine's ore input.
  await job(page, 'produce ammo').getByRole('button', { name: 'Cancel', exact: true }).click();
  // Recovery is still part of endless defense. Refill surviving magazines while
  // ore/medicine and repairs run, so the next approach cannot overrun the clinic.
  const rearReloads = survived.world.structures.filter(s => s.kind === 'tower' && s.health === 100 && s.inventory.ammo <= 8);
  for (const s of rearReloads) await at(page, s.x, s.y).getByRole('button', { name: 'Reload 4 ammo', exact: true }).click();
  await resume(page); await expect(job(page, 'produce ammo')).toContainText('CANCELLED'); await pause(page);
  await produce(page, structure(page, 'workshop'), 'ore');
  await resume(page);
  for (const s of damaged) await expect(at(page, s.x, s.y)).toContainText('Condition 100%', { timeout: 60000 });
  await pause(page);
  for (let batch = 1; batch <= 3; batch++) {
    await at(page, 70, 62).getByRole('button', { name: 'Reload 4 ammo', exact: true }).click();
    await resume(page); await expect(at(page, 70, 62)).toContainText(`Ammo ${batch * 4}/12`, { timeout: 60000 }); await pause(page);
  }
  await resume(page);
  await expect.poll(async () => (await page.locator('#duties .duty-card > p').allTextContents()).filter(text => text.includes('Health 100%')).length, { timeout: 90000 }).toBe(6);
  await pause(page); const recovered = await snapshot(page, info, 'ordinary-recovery-work');
  expect(recovered.world.terminal).toBeNull(); expect(recovered.world.statistics.workTicks).toBeGreaterThan(survived.world.statistics.workTicks);
  expect(recovered.world.people.every(person => person.life === 'alive' && person.body.health === 100)).toBe(true);
  expect(recovered.world.statistics.rescues).toBeGreaterThan(prepared.world.statistics.rescues);
  expect(recovered.world.statistics.treatments).toBeGreaterThan(prepared.world.statistics.treatments);
  commands.push('cancel ammunition production after raid; produce ore for fresh medicine; physically rescue/treat all founders to full health', `reload undamaged towers ${rearReloads.map(s => `${s.x},${s.y}`).join(' / ')} once while repairing; fill repaired forward tower with three physical batches`);
  await page.setViewportSize({ width: 390, height: 844 }); await page.locator('#defense-heading').scrollIntoViewIfNeeded();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath('defense-controls-390.png'), fullPage: true });
  expect(failures).toEqual([]);
  await info.attach('defense-evidence', { body: JSON.stringify({ seed: 42, commands, injectedInventory: false, injectedStructures: false, checkpoints: [prepared, midWave, survived, recovered].map(s => ({ tick: s.world.tick, hash: hash(s.world), heart: s.world.heart.health, combat: s.world.combat, statistics: s.world.statistics, materials: materialAudit(s.world) })), continuation: { expected: hash(expected), actual: hash(survived.world) }, repairs: damaged.map(s => s.id), errors: failures }, null, 2), contentType: 'application/json' });
});
