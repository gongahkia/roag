import { expect, test, type Page } from '@playwright/test'
import { autoplayTaskCatalog } from '../src/autoplay-task-catalog'

const activate = async (page: Page): Promise<void> => {
  await page.goto('/')
  await expect(page.locator('.boot-activate')).toHaveText('Initialize Voyager')
  await page.locator('.boot-activate').click()
  await expect(page.locator('#game')).toHaveAttribute('data-route', /splash|title/)
}

test('boots through the deferred Voyager entry and accepts keyboard input', async ({ page }) => {
  await activate(page)
  await page.locator('#game').click()
  await page.keyboard.press('n')
  await expect(page.locator('#game')).toHaveAttribute('data-route', 'createCourier')
  await page.keyboard.type('Ari')
  await page.keyboard.press('Enter')
  await expect(page.locator('#game')).toHaveAttribute('data-route', /loading|approach/)
})

test('retains player-facing visual and zoom controls before route selection', async ({ page }) => {
  await activate(page)
  await page.locator('#game').click()
  await page.keyboard.press('v')
  await expect.poll(() => page.evaluate(() => localStorage.getItem('jomon-visual-mode'))).toBe('runes')
  await page.keyboard.press('=')
  await expect.poll(() => page.evaluate(() => localStorage.getItem('jomon-board-zoom'))).toBe('1.25')
})

test('keeps every headless task associated with a browser coverage id', () => {
  const identifiers = autoplayTaskCatalog().map(task => task.uiTaskId)
  expect(new Set(identifiers).size).toBe(identifiers.length)
  expect(identifiers.every(identifier => identifier.startsWith('ui.'))).toBe(true)
})
