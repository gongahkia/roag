import { expect, test } from '@playwright/test'

test('configures, saves, inspects, selects, and resumes a medieval world through keyboard input', async ({ page }) => {
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
    version: '1', ground: '#000000', bodyText: '#c0c0c0', selected: '#00ffff', error: '#ff0000', canvasGround: [0, 0, 0, 255]
  })
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await game.click()
  await page.keyboard.press('n')
  await expect(game).toHaveAttribute('data-route', 'create-world')
  await expect(game).toHaveAttribute('data-settings-page', 'basic')

  for (let index = 0; index < 16; index++) await page.keyboard.press('Backspace')
  await page.keyboard.type('lower   quay')
  await expect(game).toHaveAttribute('aria-label', /Current normalized seed lower quay/)

  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('ArrowRight')
  await expect(game).toHaveAttribute('data-settings-preset', 'far-coast')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-settings-page', 'advanced')
  await page.keyboard.press('ArrowLeft')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('ArrowLeft')
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-settings-page', 'basic')

  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'creation-profiles')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('s')
  await expect(game).toHaveAttribute('data-settings-profiles', '1')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'create-world')
  await expect(game).toHaveAttribute('data-settings-page', 'advanced')
  await expect(game).toHaveAttribute('data-settings-preset', 'far-coast')

  await page.keyboard.press('Escape')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world-generation')
  await expect(game).toHaveAttribute('data-generation-stage', 'validation')
  await expect(game).toHaveAttribute('data-persistence', 'saved')
  const worldId = await game.getAttribute('data-world-id')
  await expect(worldId).toBeTruthy()

  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world-result')
  await expect(game).toHaveAttribute('data-result-page', 'summary')
  await expect(game).toHaveAttribute('data-crew-count', '6')
  await page.keyboard.press('ArrowRight')
  await expect(game).toHaveAttribute('data-result-page', 'configuration')
  await page.keyboard.press('ArrowRight')
  await expect(game).toHaveAttribute('data-result-page', 'provenance')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'choose-courier')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect(game).toHaveAttribute('data-world-id', worldId!)
  await expect(game).toHaveAttribute('data-persistence', 'saved')
  await expect(game).toHaveAttribute('aria-label', /Jomon foundation world .* active courier/)

  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await page.reload()
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await game.click()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect(game).toHaveAttribute('data-world-id', worldId!)
  expect(externalRequests).toEqual([])
})
