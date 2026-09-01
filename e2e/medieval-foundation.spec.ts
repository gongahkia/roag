import { expect, test } from '@playwright/test'

test('creates, selects, saves, and resumes a seeded medieval world through keyboard input', async ({ page }) => {
  await page.goto('/')
  const game = page.locator('#game')

  await expect(game).toHaveAttribute('data-route', 'worlds')
  await game.click()
  await page.keyboard.press('n')
  await expect(game).toHaveAttribute('data-route', 'create-world')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'choose-courier')
  await expect(game).toHaveAttribute('data-crew-count', '6')
  const worldId = await game.getAttribute('data-world-id')
  await expect(worldId).toBeTruthy()

  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect(game).toHaveAttribute('data-world-id', worldId!)
  await expect(game).toHaveAttribute('data-persistence', 'saved')
  await expect(game).toHaveAttribute('aria-label', /Jomon foundation world .* active courier/)

  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect(game).toHaveAttribute('data-world-id', worldId!)
})
