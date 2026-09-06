import { expect, test, type Page } from '@playwright/test'
import { appendCausalCommand, createCausalCommand } from '../src/medieval/causal-history'
import { assessJomonDeckStep, jomonDeckCoordinateId, type JomonDeckMovementDirection } from '../src/medieval/jomon-navigation'
import { causalReplayProjectionForWorldState, createMedievalWorldState } from '../src/medieval/world-state'
import { createFoundationWorld, loadCargoHold, moveFoundationWorldCourier, replayFoundationWorldCausalHistory } from '../src/medieval/world'
import type { CausalReplayProjection, CausalHistoryState } from '../src/medieval/causal-history'
import type { FoundationWorld } from '../src/medieval/types'

const stationFixtureStateFromProjection = (world: FoundationWorld, projection: CausalReplayProjection, causalHistoryState: CausalHistoryState) => createMedievalWorldState({
  seed: world.manifest.creation.seed,
  configuration: world.manifest.creation.resolvedConfiguration,
  initialWorld: world.initialWorld,
  jomon: world.jomon,
  crew: world.crew,
  frontier: world.state.geography.frontier,
  temporal: projection.temporal,
  ...(projection.courier.initialCourierId === undefined ? {} : { initialCourierId: projection.courier.initialCourierId }),
  ...(projection.courier.activeCourierId === undefined ? {} : { activeCourierId: projection.courier.activeCourierId }),
  ...(projection.courier.initialCourierId === undefined ? {} : { departedCourierIds: projection.courier.departedCourierIds ?? [] }),
  ...(projection.courier.initialCourierId !== undefined && projection.courier.activeCourierId === undefined ? { terminalCrewExtinct: true as const } : {}),
  ...(projection.navigation === undefined ? {} : { navigationState: projection.navigation }),
  jomonState: world.state.jomon,
  peopleState: projection.people,
  simulationState: projection.simulation,
  eraState: projection.era,
  delegationState: projection.delegation,
  autonomyState: projection.autonomy,
  socialMemoryState: projection.socialMemory,
  settlementTradingState: projection.settlementTrading,
  causalHistoryState
})

/**
 * Builds valid, replay-proven saved movement worlds without turning the browser
 * test into a second movement-performance audit. The final one-minute steps for
 * long paths use the public reducer, including normal compaction.
 */
const stationFixtureWorld = (directions: readonly JomonDeckMovementDirection[]): FoundationWorld => {
  const foundation = createFoundationWorld({ seed: 'all-station-keyboard-fixtures', configuration: { preset: 'watershed' } })
  const context = { worldId: foundation.id, creationDigest: foundation.manifest.creation.digest }
  const courierId = 'crew:0'
  let history = foundation.state.causalHistory
  let coordinate = { column: 4, row: 4 }
  const append = (kind: 'initial-courier-selected' | 'deck-moved', payload: unknown) => {
    const command = createCausalCommand(context, history, kind, payload)
    history = appendCausalCommand(context, history, command, causalReplayProjectionForWorldState(foundation.state))
  }

  append('initial-courier-selected', { courierId })
  const retainedDirections = directions.slice(0, 7)
  for (const direction of retainedDirections) {
    const assessment = assessJomonDeckStep(foundation, coordinate, direction)
    if (assessment.status !== 'moved') throw new Error(`station fixture expected a ${direction} deck step`)
    const sequence = history.checkpoint.sequence + history.tail.length + 1
    append('deck-moved', {
      actionId: `deck-move:${sequence}:${courierId}:${jomonDeckCoordinateId(assessment.from)}:${jomonDeckCoordinateId(assessment.to)}`,
      courierId,
      direction,
      from: assessment.from,
      to: assessment.to
    })
    coordinate = assessment.to
  }
  const staged = { ...foundation, state: { ...foundation.state, causalHistory: history } }
  const projection = replayFoundationWorldCausalHistory(staged)
  let world: FoundationWorld = { ...foundation, state: stationFixtureStateFromProjection(foundation, projection, history) }
  for (const direction of directions.slice(retainedDirections.length)) {
    const moved = moveFoundationWorldCourier(world, direction)
    if (moved.status !== 'moved') throw new Error(`station fixture expected a ${direction} deck step`)
    world = moved.world
  }
  return world
}

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
    activeCourierId: string | undefined
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
            courier?: { initialCourierId?: string; activeCourierId?: string }
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
          activeCourierId: world.state.courier?.activeCourierId,
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

  await expect.poll(() => page.evaluate(() => document.fonts.check('18px Creep', 'JOMON'))).toBe(true)
  await expect.poll(() => page.evaluate(() =>
    getComputedStyle(document.documentElement).fontFamily.split(',')[0]?.replaceAll('"', '').trim(),
  )).toBe('Creep')
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
  await expect(game).toHaveAttribute('aria-label', /6 deterministic eligible household members.*Arrow keys choose.*Enter confirms a zero-time active courier.*Escape returns without selection.*Tavern switching is available only at Jomon's task ledger.*permanent continuity remains an authoritative read-only outcome/i)
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
  await expect(game).toHaveAttribute('data-terminal-presentation-version', '14')
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
  await expect(game).toHaveAttribute('data-terminal-prompt-count', '1')
  await expect(game).toHaveAttribute('aria-label', /Known static Jomon deck map.*113 deck and hull cells and one active courier marker.*each exact physical station provides a source-backed zero-time contextual readout.*tavern task ledger alone also supports zero-time courier switching.*availability\/loss readout.*Map legend.*Source authoritative-record vessel:jomon.*each exact station anchor has a zero-time bounded readout.*tavern task ledger alone also supports zero-time courier switching.*An active courier is selected.*Fixed full-deck camera follows.*Creation provenance seed lower quay.*Current world minute 0.*No current authoritative messages.*Tavern task ledger courier switch/i)
  await page.screenshot({ path: '/tmp/jomon-phase15-terminal-status.png' })
  if (!worldId) throw new Error('created world should have a stable id')

  await expect(game).toHaveAttribute('data-management-visibility', 'expanded')
  await expect(game).toHaveAttribute('data-management-section', 'overview')
  await expect(game).toHaveAttribute('data-management-item-count', '4')
  await expect(game).toHaveAttribute('aria-label', /Management expanded\. OVERVIEW, 4 household-known facts\./)
  const expectedZeroTime = await persistedTemporalState(page, worldId)
  expect(expectedZeroTime).toMatchObject({ worldTime: 0, actionSequence: 0, causalKinds: ['initial-courier-selected'], initialCourierId: 'crew:1', activeCourierId: 'crew:1', coordinate: { column: 4, row: 4 }, tasks: [] })
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
  await expect(game).toHaveAttribute('aria-label', /Map legend.*Known fixed local deck only.*each exact station anchor has a zero-time bounded readout record.*tavern task ledger alone also supports zero-time courier switching.*cargo-hold lots.*physical readout.*no NPCs, hazards, travel, fog, rest, conversation, or succession/i)
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  expect(await persistedTemporalState(page, worldId)).toEqual(expectedZeroTime)

  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', /Tavern task ledger courier switch.*Physical tavern task ledger readout.*6 canonical household members.*ACTIVE.*Arrow keys select.*Enter confirms a zero-time viewpoint switch.*Escape cancels/i)
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'prompt-cancelled')
  expect(await persistedTemporalState(page, worldId)).toEqual(expectedZeroTime)

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

test('switches a selected courier only through the remappable tavern ledger operation', async ({ page }) => {
  const persistedCourier = async (worldId: string) => page.evaluate(async id => new Promise<{
    worldTime: number
    actionSequence: number
    causalKinds: string[]
    initialCourierId: string | undefined
    activeCourierId: string | undefined
    coordinate: { column?: number; row?: number } | undefined
  }>((resolve, reject) => {
    const request = indexedDB.open('jomon-medieval-worlds-v1')
    request.onerror = () => reject(request.error)
    request.onsuccess = () => {
      const database = request.result
      const read = database.transaction('worlds', 'readonly').objectStore('worlds').get(id)
      read.onerror = () => { database.close(); reject(read.error) }
      read.onsuccess = () => {
        const state = (read.result as { state?: { temporal?: { worldTime?: number; actionSequence?: number }; causalHistory?: { tail?: Array<{ kind?: string }> }; courier?: { initialCourierId?: string; activeCourierId?: string }; navigation?: { coordinate?: { column?: number; row?: number } } } } | undefined)?.state
        database.close()
        if (!state?.temporal || !state.causalHistory?.tail) return reject(new Error('saved courier world was not available'))
        resolve({ worldTime: state.temporal.worldTime ?? -1, actionSequence: state.temporal.actionSequence ?? -1, causalKinds: state.causalHistory.tail.map(command => command.kind ?? ''), initialCourierId: state.courier?.initialCourierId, activeCourierId: state.courier?.activeCourierId, coordinate: state.navigation?.coordinate })
      }
    }
  }), worldId)

  await page.goto('/')
  const game = page.locator('#game')
  await game.click()
  await page.keyboard.press('n')
  await expect(game).toHaveAttribute('data-route', 'create-world')
  for (let index = 0; index < 4; index++) await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world-generation')
  await expect(game).toHaveAttribute('data-persistence', 'saved')
  const worldId = await game.getAttribute('data-world-id')
  await expect(worldId).toBeTruthy()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world-result')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'choose-courier')
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect.poll(() => persistedCourier(worldId!)).toMatchObject({ worldTime: 0, actionSequence: 0, initialCourierId: 'crew:1', activeCourierId: 'crew:1', coordinate: { column: 4, row: 4 } })

  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', /Tavern task ledger courier switch.*Current courier.*Physical tavern task ledger readout.*6 canonical household members.*Arrow keys select.*Escape cancels/i)
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'prompt-cancelled')
  await expect.poll(() => persistedCourier(worldId!)).toMatchObject({ worldTime: 0, actionSequence: 0, causalKinds: ['initial-courier-selected'], initialCourierId: 'crew:1', activeCourierId: 'crew:1' })

  await page.keyboard.press('k')
  await expect(game).toHaveAttribute('data-terminal-focus', '4,3')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('aria-label', /No physical vessel prop is at the active courier's exact anchor.*Contextual operation is unavailable/i)
  await expect(game).not.toHaveAttribute('aria-label', /6 canonical household members in household order/i)
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'prompt-option-disabled')
  await page.keyboard.press('Escape')
  await page.keyboard.press('j')
  await expect.poll(() => persistedCourier(worldId!)).toMatchObject({ worldTime: 2, actionSequence: 2, coordinate: { column: 4, row: 4 } })

  await page.keyboard.press('F2')
  await expect(game).toHaveAttribute('data-terminal-selected-control', 'command-help')
  await page.keyboard.press('ArrowDown')
  await expect(game).toHaveAttribute('data-terminal-selected-control', 'contextual-prompt')
  await page.keyboard.press('Enter')
  await page.keyboard.press('o')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'controls-binding-saved')
  await page.keyboard.press('Escape')
  await page.keyboard.press('o')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', /Physical tavern task ledger readout.*6 canonical household members.*Arrow keys select.*Enter confirms a zero-time viewpoint switch/i)
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'courier-switched')
  await expect.poll(() => persistedCourier(worldId!)).toMatchObject({ worldTime: 2, actionSequence: 2, causalKinds: ['initial-courier-selected', 'deck-moved', 'deck-moved', 'tavern-courier-switched'], initialCourierId: 'crew:1', activeCourierId: 'crew:2', coordinate: { column: 4, row: 4 } })

  await page.reload()
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await game.click()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect.poll(() => persistedCourier(worldId!)).toMatchObject({ worldTime: 2, actionSequence: 2, initialCourierId: 'crew:1', activeCourierId: 'crew:2', coordinate: { column: 4, row: 4 } })
})

test('records a remapped chart-table readout through real keyboard input and reload', async ({ page }) => {
  const world = stationFixtureWorld(['east', 'east', 'east'])
  await page.goto('/')
  await page.evaluate(async source => new Promise<void>((resolve, reject) => {
    const request = indexedDB.open('jomon-medieval-worlds-v1')
    request.onerror = () => reject(request.error)
    request.onsuccess = () => {
      const database = request.result
      const transaction = database.transaction(['worlds', 'catalog'], 'readwrite')
      transaction.onerror = () => { database.close(); reject(transaction.error) }
      transaction.oncomplete = () => { database.close(); resolve() }
      transaction.objectStore('worlds').put(source, source.id)
      transaction.objectStore('catalog').put({ version: 1, activeWorlds: [{ id: source.id, label: source.manifest.creation.label, initialCourierId: source.state.courier.initialCourierId }], chronicles: [] }, 'world-index')
    }
  }), world)
  await page.reload()
  const game = page.locator('#game')
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await game.click()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect(game).toHaveAttribute('data-terminal-presentation-version', '14')
  await expect(game).toHaveAttribute('data-terminal-focus', '7,4')

  await page.keyboard.press('F2')
  await expect(game).toHaveAttribute('data-terminal-selected-control', 'command-help')
  await page.keyboard.press('ArrowDown')
  await expect(game).toHaveAttribute('data-terminal-selected-control', 'contextual-prompt')
  await page.keyboard.press('Enter')
  await page.keyboard.press('o')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'controls-binding-saved')
  await page.keyboard.press('Escape')

  const chartBefore = await stationFixtureTemporal(page, world.id)
  await page.keyboard.press('o')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', /Chart table.*Route comparison is not yet implemented.*Enter records this bounded station readout at zero world time.*Escape cancels without mutation/i)
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'prompt-cancelled')
  expect(await stationFixtureTemporal(page, world.id)).toEqual(chartBefore)

  await page.keyboard.press('o')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await page.keyboard.press('Enter')
  await expect.poll(() => game.getAttribute('data-persistence')).toBe('saved')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'station-readout-recorded')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  expect(await stationFixtureTemporal(page, world.id)).toEqual(chartBefore)

  await page.reload()
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await game.click()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect(game).toHaveAttribute('data-terminal-message-count', '1')
  await expect(game).toHaveAttribute('data-terminal-message-state', 'available')
  await expect(game).toHaveAttribute('aria-label', /Chart table: bounded station readout recorded.*Source world-state:jomon:prop-actions:prop:chart-table.*recorded at world minute 3/i)
})

test('opens a bounded gangplank prompt through real keyboard movement', async ({ page }) => {
  await page.goto('/')
  const game = page.locator('#game')
  await game.click()
  await page.keyboard.press('n')
  await expect(game).toHaveAttribute('data-route', 'create-world')
  for (let index = 0; index < 4; index++) await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world-generation')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world-result')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'choose-courier')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect(game).toHaveAttribute('data-persistence', 'saved')

  await page.keyboard.press('j')
  await expect(game).toHaveAttribute('data-terminal-focus', '4,5')
  await page.keyboard.press('h')
  await expect(game).toHaveAttribute('data-terminal-focus', '3,5')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', /Gangplank.*Jomon is moored.*Quay departure and travel are not yet implemented.*Enter records this bounded station readout at zero world time.*Escape cancels without mutation/i)
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'prompt-cancelled')
})

test('accepts the one physical public-tally handoff through keyboard input and retains its zero-time burden', async ({ page }) => {
  const world = stationFixtureWorld(['south', 'west', 'west', 'west', 'north-west'])
  await page.goto('/')
  await page.evaluate(async source => new Promise<void>((resolve, reject) => {
    const request = indexedDB.open('jomon-medieval-worlds-v1')
    request.onerror = () => reject(request.error)
    request.onsuccess = () => {
      const database = request.result
      const transaction = database.transaction(['worlds', 'catalog'], 'readwrite')
      transaction.onerror = () => { database.close(); reject(transaction.error) }
      transaction.oncomplete = () => { database.close(); resolve() }
      transaction.objectStore('worlds').put(source, source.id)
      transaction.objectStore('catalog').put({ version: 1, activeWorlds: [{ id: source.id, label: source.manifest.creation.label, initialCourierId: source.state.courier.initialCourierId }], chronicles: [] }, 'world-index')
    }
  }), world)
  await page.reload()
  const game = page.locator('#game')
  await game.focus()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect(game).toHaveAttribute('data-terminal-focus', '0,4')
  const before = await stationFixtureTemporal(page, world.id)

  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', /Hearthford Mill Quay public tally.*one ironwork case.*Arrow keys choose accept or refuse.*Enter confirms at zero world time.*Escape cancels without mutation/i)
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'prompt-cancelled')
  expect(await stationFixtureTemporal(page, world.id)).toEqual(before)

  await page.keyboard.press('Enter')
  await page.keyboard.press('Enter')
  await expect.poll(() => game.getAttribute('data-persistence')).toBe('saved')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'settlement-trade-recorded')
  expect(await stationFixtureTemporal(page, world.id)).toEqual(before)
  await expect.poll(() => page.evaluate(async id => new Promise<{ status?: string; lots?: unknown[] }>((resolve, reject) => {
    const request = indexedDB.open('jomon-medieval-worlds-v1')
    request.onerror = () => reject(request.error)
    request.onsuccess = () => {
      const database = request.result
      const read = database.transaction('worlds', 'readonly').objectStore('worlds').get(id)
      read.onerror = () => { database.close(); reject(read.error) }
      read.onsuccess = () => {
        const source = read.result as { state?: { settlementTrading?: { contracts?: Array<{ status?: string }> }; jomon?: { cargo?: { lots?: unknown[] } } } } | undefined
        database.close()
        resolve({ status: source?.state?.settlementTrading?.contracts?.[0]?.status, lots: source?.state?.jomon?.cargo?.lots })
      }
    }
  }), world.id))).toEqual({ status: 'accepted', lots: [] })

  await page.reload()
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await game.focus()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('aria-label', /Hearthford tally.*ironwork case awaits delivery to Jomon's cargo hold/i)
})

test('starts at the quay, crosses the gangplank, switches at the tavern, records a station, and returns through the gangplank with keyboard input', async ({ page }) => {
  /** The source is replay-valid public movement from the canonical tavern spawn to quay cell 2,5. */
  const world = stationFixtureWorld(['south', 'west', 'west'])
  await page.goto('/')
  await page.evaluate(async source => new Promise<void>((resolve, reject) => {
    const request = indexedDB.open('jomon-medieval-worlds-v1')
    request.onerror = () => reject(request.error)
    request.onsuccess = () => {
      const database = request.result
      const transaction = database.transaction(['worlds', 'catalog'], 'readwrite')
      transaction.onerror = () => { database.close(); reject(transaction.error) }
      transaction.oncomplete = () => { database.close(); resolve() }
      transaction.objectStore('worlds').put(source, source.id)
      transaction.objectStore('catalog').put({ version: 1, activeWorlds: [{ id: source.id, label: source.manifest.creation.label, initialCourierId: source.state.courier.initialCourierId }], chronicles: [] }, 'world-index')
    }
  }), world)
  await page.reload()
  const game = page.locator('#game')
  await game.focus()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await expect(game).toHaveAttribute('data-terminal-focus', '2,5')

  const moveAndSave = async (key: string, focus: string) => {
    await page.keyboard.press(key)
    await expect(game).toHaveAttribute('data-terminal-focus', focus)
  }

  // Board the physical gangplank and walk to the task ledger.
  await moveAndSave('l', '3,5')
  await moveAndSave('l', '4,5')
  await moveAndSave('k', '4,4')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', /Tavern task ledger courier switch.*Physical tavern task ledger readout/i)
  await page.keyboard.press('ArrowDown')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'courier-switched')
  await expect(game).toHaveAttribute('data-terminal-focus', '4,4')

  // Return to and operate the bounded gangplank acknowledgement, not travel.
  await moveAndSave('j', '4,5')
  await moveAndSave('h', '3,5')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', /Gangplank.*Jomon is moored.*Quay departure and travel are not yet implemented.*Enter records this bounded station readout at zero world time/i)
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'station-readout-recorded')

  // Leave through that same physical gangplank onto the local quay approach.
  await moveAndSave('h', '2,5')
  expect(await stationFixtureTemporal(page, world.id)).toEqual({ worldTime: 9, actionSequence: 9 })
  await expect(game).toHaveAttribute('data-terminal-message-count', '2')
  await expect(game).toHaveAttribute('aria-label', /Gangplank: bounded station readout recorded.*Tavern task ledger: courier switch recorded/i)
})

const stationKeyboardCases = [
  { propId: 'prop:task-ledger', directions: [], text: /Tavern task ledger courier switch.*Physical tavern task ledger readout.*Arrow keys select.*Escape cancels/i, confirm: false },
  { propId: 'prop:berth', directions: ['east', 'east', 'east', 'north', 'north', 'north'], text: /Berth.*Berth capacity: 6 slots.*Rest and recovery are not modeled.*Enter records this bounded station readout at zero world time.*Escape cancels without mutation/i, confirm: true },
  { propId: 'prop:cargo-hold-rack', directions: ['east', 'east', 'east', 'east', 'east', 'east'], text: /Cargo hold rack.*Cargo hold: 0\/12 units occupied.*No cargo lots are recorded.*Enter records this bounded station readout at zero world time.*Escape cancels without mutation/i, confirm: true },
  { propId: 'prop:chart-table', directions: ['east', 'east', 'east'], text: /Chart table.*Route comparison is not yet implemented.*Enter records this bounded station readout at zero world time.*Escape cancels without mutation/i, confirm: true },
  { propId: 'prop:galley-hearth', directions: ['east', 'east', 'east', 'north', 'north', 'north', 'east', 'east', 'east', 'east'], text: /Galley hearth.*Meals, rations, and cooking are not modeled.*Enter records this bounded station readout at zero world time.*Escape cancels without mutation/i, confirm: true },
  { propId: 'prop:gangplank', directions: ['south', 'west'], text: /Gangplank.*Jomon is moored.*Quay departure and travel are not yet implemented.*Enter records this bounded station readout at zero world time.*Escape cancels without mutation/i, confirm: true },
  { propId: 'prop:repair-space-rack', directions: ['east', 'east', 'east', 'east', 'east', 'east', 'east', 'east', 'east', 'east'], text: /Repair-space rack.*Jomon integrity: 100\/100.*Repair work is not yet implemented.*Enter records this bounded station readout at zero world time.*Escape cancels without mutation/i, confirm: true },
  { propId: 'prop:stores-rack', directions: ['north', 'north', 'north'], text: /Stores rack.*Onboard provisions and inventory are not yet modeled.*Enter records this bounded station readout at zero world time.*Escape cancels without mutation/i, confirm: true }
] as const satisfies readonly { propId: string; directions: readonly JomonDeckMovementDirection[]; text: RegExp; confirm: boolean }[]

const stationFixtureTemporal = async (page: Page, worldId: string) => page.evaluate(async id => new Promise<{ worldTime: number; actionSequence: number }>((resolve, reject) => {
  const request = indexedDB.open('jomon-medieval-worlds-v1')
  request.onerror = () => reject(request.error)
  request.onsuccess = () => {
    const database = request.result
    const read = database.transaction('worlds', 'readonly').objectStore('worlds').get(id)
    read.onerror = () => { database.close(); reject(read.error) }
    read.onsuccess = () => {
      const state = (read.result as { state?: { temporal?: { worldTime?: number; actionSequence?: number } } } | undefined)?.state?.temporal
      database.close()
      if (!state) return reject(new Error('station fixture was not persisted'))
      resolve({ worldTime: state.worldTime ?? -1, actionSequence: state.actionSequence ?? -1 })
    }
  }
}), worldId)

for (const station of stationKeyboardCases) test(`renders ${station.propId} from a valid saved movement fixture through real keyboard input`, async ({ page }) => {
  const world = stationFixtureWorld(station.directions)
  await page.goto('/')
  await page.evaluate(async source => new Promise<void>((resolve, reject) => {
    const request = indexedDB.open('jomon-medieval-worlds-v1')
    request.onerror = () => reject(request.error)
    request.onsuccess = () => {
      const database = request.result
      const transaction = database.transaction(['worlds', 'catalog'], 'readwrite')
      transaction.onerror = () => { database.close(); reject(transaction.error) }
      transaction.oncomplete = () => { database.close(); resolve() }
      transaction.objectStore('worlds').put(source, source.id)
      transaction.objectStore('catalog').put({ version: 1, activeWorlds: [{ id: source.id, label: source.manifest.creation.label, initialCourierId: source.state.courier.initialCourierId }], chronicles: [] }, 'world-index')
    }
  }), world)
  await page.reload()
  const game = page.locator('#game')
  await expect(game).toHaveAttribute('data-persistence', 'saved')
  await expect(game).toHaveAttribute('data-route', 'worlds')
  await game.click()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  const before = await stationFixtureTemporal(page, world.id)
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', station.text)
  if (station.confirm) {
    await page.keyboard.press('Enter')
    await expect.poll(() => game.getAttribute('data-persistence')).toBe('saved')
    await expect(game).toHaveAttribute('data-terminal-outcome', 'station-readout-recorded')
    await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
    expect(await stationFixtureTemporal(page, world.id)).toEqual(before)
  } else {
    await page.keyboard.press('Escape')
    await expect(game).toHaveAttribute('data-terminal-overlay', 'none')
    await expect(game).toHaveAttribute('data-terminal-outcome', 'prompt-cancelled')
    expect(await stationFixtureTemporal(page, world.id)).toEqual(before)
  }
})

test('renders validated persisted cargo-hold feedback and summary without browser cargo authority', async ({ page }) => {
  const world = loadCargoHold(stationFixtureWorld(['east', 'east', 'east', 'east', 'east', 'east']), 'commodity:grain', 1)
  await page.goto('/')
  await page.evaluate(async source => new Promise<void>((resolve, reject) => {
    const request = indexedDB.open('jomon-medieval-worlds-v1')
    request.onerror = () => reject(request.error)
    request.onsuccess = () => {
      const database = request.result
      const transaction = database.transaction(['worlds', 'catalog'], 'readwrite')
      transaction.objectStore('worlds').put(source, source.id)
      transaction.objectStore('catalog').put({ version: 1, activeWorlds: [{ id: source.id, label: source.manifest.creation.label, initialCourierId: 'crew:0' }], chronicles: [] }, 'world-index')
      transaction.oncomplete = () => { database.close(); resolve() }
      transaction.onerror = () => { database.close(); reject(transaction.error) }
    }
  }), world)
  await page.reload()
  const game = page.locator('#game')
  await game.click()
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-route', 'world')
  await page.keyboard.press('Enter')
  await expect(game).toHaveAttribute('data-terminal-overlay', 'contextual-prompt')
  await expect(game).toHaveAttribute('aria-label', /Cargo hold rack: cargo loaded.*Source world-state:jomon:prop-actions:prop:cargo-hold-rack.*Cargo hold rack.*Cargo hold: 4\/12 units occupied.*Grain ×1: sound, in-hold/i)
  await page.keyboard.press('Escape')
  await expect(game).toHaveAttribute('data-terminal-outcome', 'prompt-cancelled')
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
