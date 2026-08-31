import { expect, test, type Page } from '@playwright/test'
import { autoplayTaskCatalog } from '../src/autoplay-task-catalog'
import { moveOutpost, outpostInteraction, outpostSpawn } from '../src/engine'
import { outpostAutoplayCommand } from '../src/outpost-autoplay'
import type { Direction, Point } from '../src/types'

const directionForCommand: Record<string, Direction> = { i: 'nw', o: 'n', p: 'ne', k: 'w', ';': 'e', ',': 'sw', '.': 's', '/': 'se' }

const loadGame = async (page: Page): Promise<void> => {
  await page.addInitScript(() => { Math.random = () => 7 / 0x7fffffff })
  await page.goto('/')
  await expect(page.locator('#game')).toHaveAttribute('data-route', /splash|title/)
}

const enterCarrier = async (page: Page): Promise<void> => {
  await loadGame(page)
  await page.locator('#game').click()
  await page.keyboard.press('n')
  await expect(page.locator('#game')).toHaveAttribute('data-route', 'createCourier')
  await page.keyboard.type('Kestrel')
  await page.keyboard.press('Enter')
  await expect(page.locator('#game')).toHaveAttribute('data-route', 'approach', { timeout: 30_000 })
  await page.keyboard.press('Space')
  await expect(page.locator('#game')).toHaveAttribute('data-route', 'hub')
}

const openRouteBoard = async (page: Page, initialPosition: Point = outpostSpawn()): Promise<void> => {
  let position = { ...initialPosition }
  for (let step = 0; step < 100; step++) {
    const command = outpostAutoplayCommand(position)
    expect(command).toBeDefined()
    await page.keyboard.press(command!)
    if (command === 'c') {
      await expect(page.locator('#game')).toHaveAttribute('data-route', 'sector')
      return
    }
    position = moveOutpost(position, directionForCommand[command!]!).position
  }
  throw new Error('flight console was not reached from the current carrier position')
}

test('opens the Jomon immediately and accepts keyboard input', async ({ page }) => {
  await loadGame(page)
  await page.locator('#game').click()
  await page.keyboard.press('n')
  await expect(page.locator('#game')).toHaveAttribute('data-route', 'createCourier')
  await page.keyboard.type('Ari')
  await page.keyboard.press('Enter')
  await expect(page.locator('#game')).toHaveAttribute('data-route', /loading|approach/)
})

test('retains player-facing visual and zoom controls before route selection', async ({ page }) => {
  await loadGame(page)
  await page.locator('#game').click()
  await page.keyboard.press('v')
  await expect.poll(() => page.evaluate(() => localStorage.getItem('jomon-visual-mode'))).toBe('runes')
  await page.keyboard.press('=')
  await expect.poll(() => page.evaluate(() => localStorage.getItem('jomon-board-zoom'))).toBe('1.25')
})

test('uses the Route Board to compare travel, returns to Kestrel, physically lands, and settles its package explicitly', async ({ page }) => {
  await enterCarrier(page)
  const game = page.locator('#game')
  await page.keyboard.press('ArrowLeft')
  await page.keyboard.press('c')
  await expect(game).toHaveAttribute('data-sealed-package', 'offered')
  await expect(game).toHaveAttribute('aria-label', /Sealed package .* is offered/)
  await page.keyboard.press('i')
  await page.keyboard.press('a')
  await expect(game).toHaveAttribute('data-sealed-package', 'accepted')
  await expect(game).toHaveAttribute('aria-label', /Sealed package .* is accepted/)

  await page.keyboard.press('c')
  await openRouteBoard(page, moveOutpost(outpostSpawn(), 'w').position)
  await expect(game).toHaveAttribute('data-current-destination', 'destination:kestrel')
  await expect(game).toHaveAttribute('data-route-board-selection', 'destination:kestrel')
  await expect(game).not.toHaveAttribute('aria-label', /Voyager|New Edo/)
  await page.keyboard.press('ArrowRight')
  await expect(game).toHaveAttribute('data-route-board-selection', 'destination:orison')
  await page.keyboard.press('ArrowRight')
  await expect(game).toHaveAttribute('data-route-board-selection', 'destination:halcyon')
  await page.keyboard.press('ArrowLeft')
  await expect(game).toHaveAttribute('data-route-board-selection', 'destination:orison')
  await page.keyboard.press('e')
  await expect(game).toHaveAttribute('data-route-board-confirmation', 'transit')
  const beforeTravel = Number(await game.getAttribute('data-route-reckoning'))
  await page.clock.install({ time: new Date('2026-08-31T00:00:00.000Z') })
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'transit')
  await expect(game).not.toHaveAttribute('data-route-transit', '')
  await page.clock.runFor(3_500)
  await expect(game).toHaveAttribute('data-route', 'hub')
  await expect(game).toHaveAttribute('data-current-destination', 'destination:orison')
  await expect.poll(async () => Number(await game.getAttribute('data-route-reckoning'))).toBeGreaterThan(beforeTravel)
  await page.keyboard.press('m')
  await expect(game).toHaveAttribute('data-hub-action', 'manifest')
  await expect(game).toHaveAttribute('aria-label', /General Manifest: Jomon arrived at Orison Relay/)
  await page.keyboard.press('m')

  await openRouteBoard(page)
  await expect(game).toHaveAttribute('data-current-destination', 'destination:orison')
  await page.keyboard.press('ArrowRight')
  await expect(game).toHaveAttribute('data-route-board-selection', 'destination:kestrel')
  await page.keyboard.press('e')
  await expect(game).toHaveAttribute('data-route-board-confirmation', 'transit')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'transit')
  await page.clock.runFor(3_500)
  await expect(game).toHaveAttribute('data-route', 'hub')
  await expect(game).toHaveAttribute('data-current-destination', 'destination:kestrel')

  await openRouteBoard(page)
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route-board-confirmation', 'landing')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'level')
  await page.keyboard.press('c')
  await expect(game).toHaveAttribute('data-route', 'hub')

  await page.keyboard.press('ArrowLeft')
  await page.keyboard.press('c')
  await page.keyboard.press('e')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-sealed-package', 'completed')
  await expect(game).toHaveAttribute('aria-label', /Sealed package .* is completed/)
})

test('advances Route Reckoning only during an active scene, exposes the Manifest, and does not reconcile on reload', async ({ page }) => {
  await enterCarrier(page)
  const game = page.locator('#game')
  await page.clock.install({ time: new Date('2026-08-31T00:00:00.000Z') })
  const before = Number(await game.getAttribute('data-route-reckoning'))
  await page.clock.runFor(1_200)
  await expect.poll(async () => Number(await game.getAttribute('data-route-reckoning'))).toBeGreaterThan(before)
  const active = Number(await game.getAttribute('data-route-reckoning'))

  await page.keyboard.press('m')
  await expect(game).toHaveAttribute('data-hub-action', 'manifest')
  await page.clock.runFor(1_000)
  await expect(game).toHaveAttribute('data-route-reckoning', String(active))
  await page.keyboard.press('m')
  await expect(game).toHaveAttribute('data-hub-action', '')
  await page.clock.runFor((720 - active + 1) * 100)
  await expect.poll(async () => Number(await game.getAttribute('data-route-reckoning'))).toBeGreaterThanOrEqual(720)
  const scheduled = Number(await game.getAttribute('data-route-reckoning'))
  await page.keyboard.press('m')
  await expect(game).toHaveAttribute('data-hub-action', 'manifest')
  await expect(game).toHaveAttribute('aria-label', /General Manifest: .*controlled/)
  await page.keyboard.press('m')
  await page.clock.setSystemTime(new Date('2036-08-31T00:00:00.000Z'))
  await page.reload()
  await expect(game).toHaveAttribute('data-route', /splash|title/)
  await game.click()
  await page.keyboard.press('l')
  await expect(game).toHaveAttribute('data-route', 'hub')
  await expect(game).toHaveAttribute('data-route-reckoning', String(scheduled))
})

test('keeps every headless task associated with a browser coverage id', () => {
  const identifiers = autoplayTaskCatalog().map(task => task.uiTaskId)
  expect(new Set(identifiers).size).toBe(identifiers.length)
  expect(identifiers.every(identifier => identifier.startsWith('ui.'))).toBe(true)
})
