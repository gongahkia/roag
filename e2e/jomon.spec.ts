import { expect, test, type Page } from '@playwright/test'
import { autoplayTaskCatalog } from '../src/autoplay-task-catalog'

const loadGame = async (page: Page): Promise<void> => {
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
  await expect(page.locator('#game')).toHaveAttribute('data-route', 'approach', { timeout: 10_000 })
  await page.keyboard.press('Space')
  await expect(page.locator('#game')).toHaveAttribute('data-route', 'hub')
}

test('opens the Voyager immediately and accepts keyboard input', async ({ page }) => {
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

test('accepts a sealed package at Jomon, makes a physical Kestrel landing, returns, and settles it explicitly', async ({ page }) => {
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
  for (const command of 'iiiiiik,,ik,kkkii,,kkiiiiiiii,,,,,,c') await page.keyboard.press(command)
  await expect(game).toHaveAttribute('data-route', 'sector')
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

test('keeps every headless task associated with a browser coverage id', () => {
  const identifiers = autoplayTaskCatalog().map(task => task.uiTaskId)
  expect(new Set(identifiers).size).toBe(identifiers.length)
  expect(identifiers.every(identifier => identifier.startsWith('ui.'))).toBe(true)
})
