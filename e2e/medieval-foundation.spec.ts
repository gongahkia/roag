import { expect, test } from '@playwright/test'

test('configures, saves, inspects, selects, and resumes a medieval world through keyboard input', async ({ page }) => {
  const externalRequests: string[] = []
  const captureExternalRequest = (request: { url: () => string }) => {
    const url = new URL(request.url())
    if (url.origin !== 'http://127.0.0.1:4173') externalRequests.push(url.toString())
  }
  const persistedTemporalState = async (target: typeof page, worldId: string) => target.evaluate(async id => new Promise<{
    worldTime: number
    actionSequence: number
    causalKinds: string[]
    initialCourierId: string | undefined
    coordinate: { column?: number; row?: number } | undefined
    tasks: unknown
    autonomy: unknown
    era: unknown
  }>((resolve, reject) => {
    const request = indexedDB.open('jomon-medieval-worlds-v1')
    request.onerror = () => reject(request.error)
    request.onsuccess = () => {
      const database = request.result
      const read = database.transaction('worlds', 'readonly').objectStore('worlds').get(id)
      read.onerror = () => { database.close(); reject(read.error) }
      read.onsuccess = () => {
        const world = read.result as {
          state?: {
            temporal?: { worldTime?: number; actionSequence?: number }
            causalHistory?: { tail?: Array<{ kind?: string }> }
            navigation?: { coordinate?: { column?: number; row?: number } }
            courier?: { initialCourierId?: string }
            delegation?: { tasks?: unknown }
            autonomy?: unknown
            era?: unknown
          }
        } | undefined
        database.close()
        if (!world?.state?.temporal || !world.state.causalHistory?.tail) return reject(new Error('saved medieval world was not available'))
        resolve({
          worldTime: world.state.temporal.worldTime ?? -1,
          actionSequence: world.state.temporal.actionSequence ?? -1,
          causalKinds: world.state.causalHistory.tail.map(command => command.kind ?? ''),
          initialCourierId: world.state.courier?.initialCourierId,
          coordinate: world.state.navigation?.coordinate,
          tasks: world.state.delegation?.tasks,
          autonomy: world.state.autonomy,
          era: world.state.era
        })
      }
    }
  }), worldId)
  page.on('request', captureExternalRequest)
  await page.goto('/')
  const game = page.locator('#game')

  await expect.poll(() => page.evaluate(() => document.fonts.check('18px "BigBlueTerm"', 'JOMON'))).toBe(true)
  await expect.poll(() => page.evaluate(() => ({
    version: document.documentElement.dataset.medievalPaletteVersion,
    ground: getComputedStyle(document.documentElement).getPropertyValue('--jomon-palette-console-ground').trim(),
    bodyText: getComputedStyle(document.documentElement).getPropertyValue('--jomon-palette-body-text').trim(),
    selected: getComputedStyle(document.documentElement).getPropertyValue('--jomon-palette-selected-text').trim(),
    error: getComputedStyle(document.documentElement).getPropertyValue('--jomon-palette-error-text').trim(),
    themeColor: document.querySelector<HTMLMetaElement>('meta[name="theme-color"]')?.content,
    canvasGround: [...(document.querySelector<HTMLCanvasElement>('#game')?.getContext('2d')?.getImageData(0, 0, 1, 1).data ?? [])],
    canvasPanel: [...(document.querySelector<HTMLCanvasElement>('#game')?.getContext('2d')?.getImageData(30, 30, 1, 1).data ?? [])]
  }))).toEqual({
    version: '2', ground: '#1c1b14', bodyText: '#f0dfb4', selected: '#83cbc3', error: '#de8065', themeColor: '#1c1b14',
    canvasGround: [28, 27, 20, 255], canvasPanel: [41, 39, 28, 255]
  })
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await page.keyboard.press('Tab')
  await expect.poll(() => page.evaluate(() => document.activeElement?.id ?? '')).toBe('game')
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
  await expect(game).toHaveAttribute('aria-label', /6 deterministic eligible household members.*Arrow keys choose.*Enter confirms a zero-time active courier.*Escape returns without selection.*Tavern switching and succession are not implemented/i)
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-route', 'world-result')
  expect(await persistedTemporalState(page, worldId!)).toMatchObject({ worldTime: 0, actionSequence: 0, causalKinds: [], initialCourierId: undefined })
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'choose-courier')
  await page.keyboard.press('ArrowUp')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect(game).toHaveAttribute('data-world-id', worldId!)
  await expect(game).toHaveAttribute('data-persistence', 'saved')
  await expect(game).toHaveAttribute('aria-label', /Jomon foundation world .* active courier/)
  await expect(game).toHaveAttribute('data-terminal-presentation-version', '6')
  await expect(game).toHaveAttribute('data-terminal-map-state', 'materialized')
  await expect(game).toHaveAttribute('data-terminal-map-cell-count', '114')
  await expect(game).toHaveAttribute('data-terminal-static-map-cell-count', '113')
  await expect(game).toHaveAttribute('data-terminal-courier-marker-count', '1')
  await expect(game).toHaveAttribute('data-terminal-legend-version', '1')
  await expect(game).toHaveAttribute('data-terminal-legend-entry-count', '5')
  await expect(game).toHaveAttribute('data-terminal-focus', '4,4')
  await expect(game).toHaveAttribute('data-terminal-visibility', 'all-static-deck-known')
  await expect(game).toHaveAttribute('data-terminal-status-count', '5')
  await expect(game).toHaveAttribute('data-terminal-message-count', '0')
  await expect(game).toHaveAttribute('data-terminal-message-state', 'empty')
  await expect(game).toHaveAttribute('data-terminal-prompt-count', '0')
  await expect(game).toHaveAttribute('aria-label', /Known static Jomon deck map.*113 deck and hull cells and one active courier marker.*Map legend.*Source authoritative-record vessel:jomon.*Known fixed local deck only.*No cargo, NPC, hazard, travel, fog, or prop actions.*An active courier is selected.*Fixed full-deck camera follows.*Creation provenance seed lower quay.*Current world minute 0.*No current authoritative messages/i)
  await page.screenshot({ path: '/tmp/jomon-phase15-terminal-status.png' })
  if (!worldId) throw new Error('created world should have a stable id')

  await expect(game).toHaveAttribute('data-management-visibility', 'expanded')
  await expect(game).toHaveAttribute('data-management-section', 'overview')
  await expect(game).toHaveAttribute('data-management-item-count', '4')
  await expect(game).toHaveAttribute('aria-label', /Management expanded\. OVERVIEW, 4 household-known facts\./)
  const expectedZeroTime = await persistedTemporalState(page, worldId)
  expect(expectedZeroTime).toMatchObject({ worldTime: 0, actionSequence: 0, causalKinds: ['initial-courier-selected'], initialCourierId: 'crew:1', coordinate: { column: 4, row: 4 }, tasks: [] })
  await page.keyboard.press(']')
  await expect(game).toHaveAttribute('data-management-section', 'people-work')
  await expect(game).toHaveAttribute('data-management-item-count', '6')
  await expect(game).toHaveAttribute('aria-label', /Management expanded\. PEOPLE \/ WORK, 6 household-known facts\./)
  await page.keyboard.press('m')
  await expect(game).toHaveAttribute('data-management-visibility', 'collapsed')
  await expect(game).toHaveAttribute('aria-label', /Management collapsed\. PEOPLE \/ WORK/)
  await page.keyboard.press('m')
  await expect(game).toHaveAttribute('data-management-visibility', 'expanded')
  await page.keyboard.press('[')
  await expect(game).toHaveAttribute('data-management-section', 'overview')
  expect(await persistedTemporalState(page, worldId)).toEqual(expectedZeroTime)

  await page.keyboard.press('?')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'command-help')
  await expect(game).toHaveAttribute('aria-label', /Map legend.*Active adult courier.*Command help\. 13 remappable world controls.*Escape closes help/i)
  await page.screenshot({ path: '/tmp/jomon-phase15-terminal-help.png' })
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'overlay-dismissed')
  await page.keyboard.press('F2')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'controls-editor')
  await expect(game).toHaveAttribute('data-terminal-selected-control', 'command-help')
  await page.keyboard.press('Enter')
  await page.keyboard.press('p')
  await expect(game).toHaveAttribute('data-terminal-control-capture', 'idle')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'controls-binding-saved')
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await page.keyboard.press('?')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await page.keyboard.press('p')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'command-help')
  await expect(game).toHaveAttribute('aria-label', /Map legend.*Known fixed local deck only.*No cargo, NPC, hazard, travel, fog, or prop actions/i)
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  expect(await persistedTemporalState(page, worldId)).toEqual(expectedZeroTime)

  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', /Context prompt open\. The only option is disabled because the visible static deck has no operated action rule/i)
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'prompt-option-disabled')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', /OPTION DISABLED.*NO CONTEXTUAL ACTION MATERIALIZED/i)
  await page.screenshot({ path: '/tmp/jomon-phase15-terminal-prompt.png' })
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'prompt-cancelled')

  await page.keyboard.press('k')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'movement-completed')
  await expect(game).toHaveAttribute('data-terminal-focus', '4,3')
  await expect(game).toHaveAttribute('aria-label', /MOVED NORTH.*DECK 4,3.*1 ACTION MINUTE/i)
  await expect.poll(() => persistedTemporalState(page, worldId)).toMatchObject({ worldTime: 1, actionSequence: 1, causalKinds: ['initial-courier-selected', 'deck-moved'], coordinate: { column: 4, row: 3 } })
  await page.keyboard.press('h')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'movement-blocked')
  await expect(game).toHaveAttribute('aria-label', /MOVE BLOCKED.*WEST.*HULL BOUNDARY.*ZERO TIME/i)
  await expect.poll(() => persistedTemporalState(page, worldId)).toMatchObject({ worldTime: 1, actionSequence: 1, coordinate: { column: 4, row: 3 } })
  await page.screenshot({ path: '/tmp/jomon-phase15-terminal-movement.png' })

  await page.keyboard.press('F2')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'controls-editor')
  await expect(game).toHaveAttribute('data-terminal-selected-control', 'command-help')
  for (let index = 0; index < 6; index++) await page.keyboard.press('ArrowDown')
  await expect(game).toHaveAttribute('data-terminal-selected-control', 'move-north')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-control-capture', 'pending')
  await page.keyboard.press('l')
  await expect(game).toHaveAttribute('data-terminal-control-capture', 'pending')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'controls-binding-rejected')
  await expect(game).toHaveAttribute('aria-label', /BINDING REJECTED.*BINDING CONFLICT/i)
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-control-capture', 'idle')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'controls-capture-cancelled')
  await page.keyboard.press('Enter')
  await page.keyboard.press('F2')
  await expect(game).toHaveAttribute('data-terminal-control-capture', 'pending')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'controls-binding-rejected')
  await expect(game).toHaveAttribute('aria-label', /BINDING REJECTED.*PROTECTED KEY/i)
  await page.screenshot({ path: '/tmp/jomon-phase15-terminal-controls.png' })
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-control-capture', 'idle')
  await page.keyboard.press('Enter')
  await page.keyboard.press('q')
  await expect(game).toHaveAttribute('data-terminal-control-capture', 'idle')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'controls-binding-saved')
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await page.keyboard.press('q')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'movement-completed')
  await expect(game).toHaveAttribute('data-terminal-focus', '4,2')
  await expect.poll(() => persistedTemporalState(page, worldId)).toMatchObject({ worldTime: 2, actionSequence: 2, coordinate: { column: 4, row: 2 } })

  await page.evaluate(() => document.querySelector<HTMLCanvasElement>('#game')?.blur())
  await expect.poll(() => page.evaluate(() => document.activeElement?.id ?? '')).toBe('')
  await page.evaluate(() => {
    const input = document.createElement('input')
    input.id = 'outside-terminal-focus'
    input.addEventListener('keydown', event => { document.documentElement.dataset.outsideTerminalKeyPrevented = String(event.defaultPrevented) })
    document.body.append(input)
    input.focus()
  })
  await page.keyboard.press('?')
  await expect.poll(() => page.evaluate(() => document.documentElement.dataset.outsideTerminalKeyPrevented)).toBe('false')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await page.evaluate(() => document.querySelector('#outside-terminal-focus')?.remove())
  await game.click()
  await page.waitForTimeout(300)
  await expect.poll(() => persistedTemporalState(page, worldId)).toMatchObject({ worldTime: 2, actionSequence: 2, coordinate: { column: 4, row: 2 } })

  await page.evaluate(async id => new Promise<void>((resolve, reject) => {
    const request = indexedDB.open('jomon-medieval-worlds-v1')
    request.onerror = () => reject(request.error)
    request.onsuccess = () => {
      const database = request.result
      const transaction = database.transaction(['catalog', 'worlds', 'chronicles'], 'readwrite')
      const worldRead = transaction.objectStore('worlds').get(id)
      const catalogRead = transaction.objectStore('catalog').get('world-index')
      let pendingReads = 2
      const writeChronicle = () => {
        pendingReads -= 1
        if (pendingReads !== 0) return
        const world = worldRead.result as { manifest?: { creation?: { label?: string } } } | undefined
        const catalog = catalogRead.result as { version?: number; activeWorlds?: unknown[]; chronicles?: Array<{ id: string }> } | undefined
        if (!world?.manifest?.creation?.label || !catalog || !Array.isArray(catalog.activeWorlds) || !Array.isArray(catalog.chronicles)) {
          transaction.abort()
          reject(new Error('valid world and index are required for the chronicle keyboard fixture'))
          return
        }
        transaction.objectStore('chronicles').put({
          version: 12,
          id: `chronicle:${id}`,
          status: 'finalized',
          reason: 'crew-extinction',
          world
        }, `chronicle:${id}`)
        transaction.objectStore('catalog').put({
          version: catalog.version,
          activeWorlds: catalog.activeWorlds,
          chronicles: [...catalog.chronicles.filter(entry => entry.id !== `chronicle:${id}`), {
            id: `chronicle:${id}`,
            label: world.manifest.creation.label,
            reason: 'crew-extinction'
          }].sort((left, right) => left.id.localeCompare(right.id))
        }, 'world-index')
      }
      worldRead.onerror = () => { transaction.abort(); reject(worldRead.error) }
      catalogRead.onerror = () => { transaction.abort(); reject(catalogRead.error) }
      worldRead.onsuccess = writeChronicle
      catalogRead.onsuccess = writeChronicle
      transaction.oncomplete = () => { database.close(); resolve() }
      transaction.onerror = () => { database.close(); reject(transaction.error) }
      transaction.onabort = () => { database.close(); reject(transaction.error) }
    }
  }), worldId)
  await page.reload()
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await game.click()
  await page.keyboard.press('c')
  await expect(game).toHaveAttribute('data-route', 'chronicles')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'chronicle')
  await page.screenshot({ path: '/tmp/jomon-semantic-palette-chronicle.png' })
  const download = page.waitForEvent('download')
  await page.keyboard.press('e')
  await expect((await download).suggestedFilename()).toMatch(/-chronicle\.json$/u)
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-route', 'chronicles')
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await page.keyboard.press('n')
  await expect(game).toHaveAttribute('data-route', 'create-world')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('ArrowUp')
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-settings-page', 'basic')
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await expect.poll(() => persistedTemporalState(page, worldId)).toMatchObject({ worldTime: 2, actionSequence: 2, coordinate: { column: 4, row: 2 } })

  await page.reload()
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await game.click()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect(game).toHaveAttribute('data-world-id', worldId)
  await page.keyboard.press('q')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'movement-completed')
  await expect(game).toHaveAttribute('data-terminal-focus', '4,1')
  await page.keyboard.press('F2')
  for (let index = 0; index < 6; index++) await page.keyboard.press('ArrowDown')
  await expect(game).toHaveAttribute('data-terminal-selected-control', 'move-north')
  await page.keyboard.press('r')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'controls-reset-current')
  await page.keyboard.press('Escape')
  await page.keyboard.press('k')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'movement-blocked')
  await expect.poll(() => persistedTemporalState(page, worldId)).toMatchObject({ worldTime: 3, actionSequence: 3, coordinate: { column: 4, row: 1 } })

  const browserContext = page.context()
  await page.close()
  const replacement = await browserContext.newPage()
  replacement.on('request', captureExternalRequest)
  await replacement.goto('/')
  const replacementGame = replacement.locator('#game')
  await expect(replacementGame).toHaveAttribute('data-route', 'worlds')
  await replacementGame.click()
  await replacement.keyboard.press('Enter')
  await expect(replacementGame).toHaveAttribute('data-route', 'world')
  await expect(replacementGame).toHaveAttribute('data-world-id', worldId)
  await expect(replacementGame).toHaveAttribute('data-terminal-focus', '4,1')
  await expect.poll(() => persistedTemporalState(replacement, worldId)).toMatchObject({ worldTime: 3, actionSequence: 3, coordinate: { column: 4, row: 1 } })
  expect(externalRequests).toEqual([])
})

test('keeps a missing-local-world diagnostic inside the fixed canvas panel', async ({ page }) => {
  await page.goto('/')
  const game = page.locator('#game')
  await expect(game).toHaveAttribute('data-persistence', 'saved')
  await page.evaluate(async () => {
    await new Promise<void>((resolve, reject) => {
      const request = indexedDB.open('jomon-medieval-worlds-v1')
      request.onerror = () => reject(request.error)
      request.onsuccess = () => {
        const database = request.result
        const transaction = database.transaction('catalog', 'readwrite')
        transaction.objectStore('catalog').put({
          version: 1,
          activeWorlds: [{ id: 'world:not-present', label: 'Missing Mooring' }],
          chronicles: []
        }, 'world-index')
        transaction.oncomplete = () => { database.close(); resolve() }
        transaction.onerror = () => reject(transaction.error)
      }
    })
  })

  await page.reload()
  await game.click()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-persistence', 'error')
  await expect.poll(() => page.evaluate(() => {
    const canvas = document.querySelector<HTMLCanvasElement>('#game')
    const pixels = canvas?.getContext('2d')?.getImageData(0, 610, 800, 20).data ?? []
    return pixels.some((value, index) => index % 4 === 0 && value === 255 && pixels[index + 1] === 0 && pixels[index + 2] === 0)
  })).toBe(false)
})
