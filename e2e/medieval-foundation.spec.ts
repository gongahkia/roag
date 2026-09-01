import { expect, test } from '@playwright/test'

test('creates, selects, saves, and resumes a seeded medieval world through keyboard input', async ({ page }) => {
  const externalRequests: string[] = []
  page.on('request', request => {
    const url = new URL(request.url())
    if (url.origin !== 'http://127.0.0.1:4173') externalRequests.push(url.toString())
  })
  await page.goto('/')
  const game = page.locator('#game')

  await expect.poll(() => page.evaluate(() => document.fonts.check('18px "BigBlueTerm"', 'JOMON'))).toBe(true)
  await expect.poll(() => page.evaluate(() => ({
    version: document.documentElement.dataset.medievalPaletteVersion,
    ground: getComputedStyle(document.documentElement).getPropertyValue('--jomon-palette-console-ground').trim(),
    bodyText: getComputedStyle(document.documentElement).getPropertyValue('--jomon-palette-body-text').trim(),
    selected: getComputedStyle(document.documentElement).getPropertyValue('--jomon-palette-selected-text').trim(),
    error: getComputedStyle(document.documentElement).getPropertyValue('--jomon-palette-error-text').trim(),
    canvasGround: [...(document.querySelector<HTMLCanvasElement>('#game')?.getContext('2d')?.getImageData(0, 0, 1, 1).data ?? [])]
  }))).toEqual({
    version: '1',
    ground: '#000000',
    bodyText: '#c0c0c0',
    selected: '#00ffff',
    error: '#ff0000',
    canvasGround: [0, 0, 0, 255]
  })
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
  expect(externalRequests).toEqual([])
})
