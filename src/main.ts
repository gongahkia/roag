import './style.css'
import { AudioBus } from './audio'
import { ITEM } from './content'
import { completeCampaignArea, createHubState, event, hasEvent, heirNameFor, hubView, hydrateEncyclopediaLegacy, initialCampaignRoute, initialRoute, navigate, newRun, perform, quickCast, recordCampaignSacrifice, recordDeath, unlockCampaignArea, type ScreenRoute } from './engine'
import { TerminalRenderer } from './renderer'
import { advanceStory, createStory, openingLore, successionLore, type LoadingState, type StoryState } from './lore'
import { commandForKey, loadSettings, saveSettings, setKeyBinding, settingChoices, settingsPageCount, type GameSettings } from './settings'
import { deleteRun, loadCampaignRoute, loadRecords, loadRun, saveCampaignRoute, saveRecords, saveRun } from './storage'
import { analysisFor, observeTelemetryTurn, telemetrySnapshot } from './telemetry'
import type { CampaignRouteState, Direction, Hero, HubState, LegacyRecord, Records, RunAnalysis, RunState } from './types'
import { nextVisualMode, normalizeVisualMode } from './visual-mode'

const canvas = document.querySelector<HTMLCanvasElement>('#game')!
const renderer = new TerminalRenderer(canvas)
const audio = new AudioBus()
let settings: GameSettings = loadSettings()
let state: RunState | undefined
let saved: RunState | undefined
let records: Records = { bestDepth: 0, wins: 0, deaths: 0, runs: [], analyses: [] }
let recordedEnd = false
let route: ScreenRoute = initialRoute()
let hub: HubState = createHubState(0)
let heir: Hero | undefined
let campaign: CampaignRouteState = initialCampaignRoute()
let gameZoom = loadGameZoom()
let story: StoryState | undefined
let loading: LoadingState | undefined
let pendingSuccessor: { record: LegacyRecord; seed: number } | undefined
let analysis: RunAnalysis | undefined
let analysisNext: 'succession' | 'session' | undefined

const showSessionSplash = (): boolean => {
  try {
    if (sessionStorage.getItem('jomon-session-splash')) return false
    sessionStorage.setItem('jomon-session-splash', '1')
    return true
  } catch { return false }
}
const loadVisualMode = () => { try { return normalizeVisualMode(localStorage.getItem('jomon-visual-mode')) } catch { return 'ascii' as const } }
if (showSessionSplash()) route = { ...route, screen: 'splash' }
renderer.setVisualMode(loadVisualMode())
renderer.setSettings(settings)
applyGameZoom()
canvas.addEventListener('wheel', mouseEvent => {
  mouseEvent.preventDefault()
  setGameZoom(gameZoom + (mouseEvent.deltaY < 0 ? .25 : -.25))
}, { passive: false })

void Promise.all([loadRun(), loadRecords(), loadCampaignRoute()]).then(([run, loadedRecords, loadedCampaign]) => {
  saved = run
  renderer.setSavedRun(saved)
  records = loadedRecords
  campaign = loadedCampaign
  if (saved) hydrateEncyclopediaLegacy(saved, campaign.legacyRecords)
  hub = { ...hub, unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
  route = { ...route, biome: campaign.selectedBiome }
  redraw()
})
redraw()

window.addEventListener('keydown', keyboardEvent => {
  if (keyboardEvent.metaKey || keyboardEvent.ctrlKey) return
  if (zoomForKey(keyboardEvent)) { keyboardEvent.preventDefault(); return }
  const command = commandForKey(keyboardEvent.key, settings)
  if (route.screen === 'analysis') {
    if (!keyboardEvent.repeat) { keyboardEvent.preventDefault(); continueAnalysis() }
    return
  }
  if (route.screen === 'loading') { keyboardEvent.preventDefault(); return }
  if (route.screen === 'approach' && story) { handleStoryInput(keyboardEvent); return }
  if (state?.modal?.kind === 'settings') { keyboardEvent.preventDefault(); handleSettingsInput(keyboardEvent.key); return }
  if (shouldPrevent(command ?? keyboardEvent.key)) keyboardEvent.preventDefault()
  if (keyboardEvent.key.toLowerCase() === 'v' && command === 'v') { toggleVisualMode(); return }
  if (route.screen !== 'level') {
    let nextRoute = navigate(route, command ?? keyboardEvent.key, Boolean(saved))
    if (nextRoute === route) return
    if (nextRoute.screen === 'approach') {
      const heirSeed = Math.floor(Math.random() * 0x7fffffff)
      beginTrailhead(heirSeed, openingLore(heirSeed))
      audio.play([event('menu')])
      redraw()
      return
    }
    if (nextRoute.screen === 'title') { nextRoute = { ...nextRoute, heirSeed: undefined }; story = undefined }
    if (nextRoute.screen === 'level') {
      if (route.screen === 'area') start()
      else if (saved) { state = structuredClone(saved); recordedEnd = false }
      else return
    }
    route = nextRoute
    audio.play([event('menu')])
    redraw()
    return
  }
  if (!state || state.status === 'dead' || state.status === 'victory' || !command) return
  const direction = directionFor(command)
  const spellEffect = spellEffectForInput(state, keyboardEvent, direction)
  const previousX = state.hero.x
  const previousLevel = state.hero.level
  let events = [] as ReturnType<typeof perform>
  if (keyboardEvent.altKey && direction && direction !== 'wait') {
    const before = telemetrySnapshot(state)
    events = quickCast(state, direction)
    observeTelemetryTurn(state, before, events, command)
  }
  else if (keyboardEvent.shiftKey && direction && direction !== 'wait' && !state.modal) events = run(state, command)
  else events = performTracked(state, command)
  if (state.hero.level > previousLevel) events.push(event('level'))
  if (state.hero.x !== previousX) renderer.setHeroFacingLeft(state.hero.x < previousX)
  audio.play(events)
  renderer.trigger(events, state, spellEffect)
  if (hasEvent(events, 'suspend')) { suspendRun(); return }
  if (hasEvent(events, 'floor')) { saved = structuredClone(state); renderer.setSavedRun(saved); void saveRun(state) }
  if (hasEvent(events, 'rescue')) persistRescuedRoster()
  if (hasEvent(events, 'areaComplete')) completeArea()
  if (hasEvent(events, 'gateResolved')) unlockGateDestination()
  if ((hasEvent(events, 'death') || hasEvent(events, 'win')) && !recordedEnd) finish(hasEvent(events, 'win'))
  redraw()
})

function loadGameZoom(): number {
  try {
    const stored = localStorage.getItem('jomon-board-zoom')
    const value = stored === null ? Number.NaN : Number(stored)
    return Number.isFinite(value) ? Math.max(.5, Math.min(5, value)) : 1
  } catch { return 1 }
}

function setGameZoom(value: number): void {
  gameZoom = Math.max(.5, Math.min(5, Math.round(value * 4) / 4))
  try { localStorage.setItem('jomon-board-zoom', String(gameZoom)) } catch { }
  applyGameZoom()
  redraw()
}

function applyGameZoom(): void {
  renderer.setBoardZoom(gameZoom)
}

function zoomForKey(keyboardEvent: KeyboardEvent): boolean {
  if (keyboardEvent.key === '+' || keyboardEvent.key === '=' || keyboardEvent.code === 'NumpadAdd') { setGameZoom(gameZoom + .25); return true }
  if (keyboardEvent.key === '-' || keyboardEvent.key === '_' || keyboardEvent.code === 'NumpadSubtract') { setGameZoom(gameZoom - .25); return true }
  if (keyboardEvent.key === '0' || keyboardEvent.code === 'Numpad0') { setGameZoom(1); return true }
  return false
}

function handleSettingsInput(key: string): void {
  if (!state?.modal || state.modal.kind !== 'settings') return
  const modal = state.modal
  if (key === 'Escape' || key === '`') {
    if (modal.awaiting) state.modal = { kind: 'settings', page: modal.page }
    else state.modal = undefined
  } else if (modal.awaiting) {
    const next = setKeyBinding(settings, modal.awaiting, key)
    if (next === settings) state.messages.unshift('That key is already bound.')
    else { settings = next; saveSettings(settings); renderer.setSettings(settings) }
    state.modal = { kind: 'settings', page: modal.page }
  } else if (key === '[' || key === 'ArrowLeft') state.modal = { kind: 'settings', page: Math.max(0, (modal.page ?? 0) - 1) }
  else if (key === ']' || key === 'ArrowRight') state.modal = { kind: 'settings', page: Math.min(settingsPageCount() - 1, (modal.page ?? 0) + 1) }
  else {
    const choice = settingChoices(settings, modal.page ?? 0)[Number(key) - 1]
    if (choice?.kind === 'reducedFlash') { settings = { ...settings, reducedFlash: !settings.reducedFlash }; saveSettings(settings); renderer.setSettings(settings) }
    if (choice?.kind === 'binding') state.modal = { kind: 'settings', page: modal.page, awaiting: choice.binding.id }
  }
  audio.play([event('menu')])
  redraw()
}

function start(): void {
  campaign = { ...campaign, selectedBiome: route.biome }
  void saveCampaignRoute(campaign)
  state = newRun(route.heirSeed, route.biome, 0, heir, campaign.rescuedNpcs, campaign.legacyRecords)
  renderer.setHeroFacingLeft(false)
  heir = state.hero
  saved = structuredClone(state)
  renderer.setSavedRun(saved)
  recordedEnd = false
  void saveRun(state)
  audio.play([event('menu')])
  renderer.trigger([event('floor')], state)
}

function beginTrailhead(seed: number, scene: ReturnType<typeof openingLore> | ReturnType<typeof successionLore>): void {
  route = { screen: 'approach', biome: campaign.selectedBiome, heirSeed: seed }
  heir = undefined
  hub = { ...createHubState(seed), unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
  story = createStory(scene, performance.now())
}

function handleStoryInput(keyboardEvent: KeyboardEvent): void {
  if (!story || keyboardEvent.repeat) return
  if (keyboardEvent.key === 'Escape') {
    story = undefined
    route = { ...route, screen: 'title', heirSeed: undefined }
    audio.play([event('menu')])
    redraw()
    return
  }
  if (!isStoryKey(keyboardEvent)) return
  keyboardEvent.preventDefault()
  if (keyboardEvent.code === 'Space') finishStory()
  else {
    const next = advanceStory(story, performance.now())
    story = next.story
    if (next.finished) finishStory()
    else { audio.play([event('menu')]); redraw() }
  }
}

function isStoryKey(keyboardEvent: KeyboardEvent): boolean {
  return !['Shift', 'Alt', 'Control', 'Meta', 'CapsLock', 'Tab'].includes(keyboardEvent.key)
}

function finishStory(): void {
  story = undefined
  route = { ...route, screen: 'hub', hubAction: 'routes' }
  audio.play([event('menu')])
  redraw()
}

function beginSuccession(): void {
  if (!pendingSuccessor || loading) return
  story = undefined
  route = { ...route, screen: 'loading', heirSeed: pendingSuccessor.seed }
  loading = { phase: 'fade', startedAt: performance.now() }
  redraw()
  window.setTimeout(() => {
    if (loading?.phase !== 'fade') return
    loading = { phase: 'loading', startedAt: performance.now() }
    redraw()
    window.setTimeout(() => {
      const successor = pendingSuccessor
      if (!successor || loading?.phase !== 'loading') return
      loading = undefined
      pendingSuccessor = undefined
      beginTrailhead(successor.seed, successionLore(successor.record, successor.seed))
      redraw()
    }, 650)
  }, 350)
}

function completeArea(): void {
  if (!state) return
  const completed = state.area ?? state.floor.biome
  heir = structuredClone(state.hero)
  campaign = completeCampaignArea(campaign, completed)
  hub = { ...hub, unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
  void saveCampaignRoute(campaign)
  saved = undefined
  renderer.setSavedRun(undefined)
  void deleteRun()
  route = { ...route, screen: 'hub', biome: campaign.selectedBiome, hubAction: 'routes' }
}

function unlockGateDestination(): void {
  if (!state?.gateDestination) return
  for (const lineageEvent of state.lineageEvents ?? []) campaign = recordCampaignSacrifice(campaign, lineageEvent)
  campaign = unlockCampaignArea(campaign, state.gateDestination)
  hub = { ...hub, unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
  route = { ...route, biome: campaign.selectedBiome }
  state.gateDestination = undefined
  void saveCampaignRoute(campaign)
  if (state.status === 'playing') { saved = structuredClone(state); renderer.setSavedRun(saved); void saveRun(state) }
}

function persistRescuedRoster(): void {
  if (!state?.rescuedNpcs?.length) return
  const rescuedNpcs = [...campaign.rescuedNpcs]
  for (const npc of state.rescuedNpcs) if (!rescuedNpcs.some(existing => existing.id === npc.id)) rescuedNpcs.push({ ...npc })
  campaign = { ...campaign, rescuedNpcs }
  hub = { ...hub, rescued: rescuedNpcs }
  void saveCampaignRoute(campaign)
  if (state.status === 'playing') { saved = structuredClone(state); renderer.setSavedRun(saved); void saveRun(state) }
}

function toggleVisualMode(): void {
  const mode = nextVisualMode(renderer.visualMode)
  renderer.setVisualMode(mode)
  try { localStorage.setItem('jomon-visual-mode', mode) } catch { }
  redraw()
}

function redraw(): void { canvas.dataset.route = route.screen; canvas.dataset.status = state?.status ?? 'none'; renderer.render(route, state, records, hubView(route.heirSeed ?? 0, hub), story, loading, analysis) }

function finish(won: boolean): void {
  if (!state) return
  recordedEnd = true
  if (!won) {
    campaign = recordDeath(campaign, state, heirNameFor(route.heirSeed ?? state.seed))
    const record = campaign.legacyRecords.at(-1)
    if (!record) throw new Error('missing death legacy record')
    pendingSuccessor = { record, seed: Math.floor(Math.random() * 0x7fffffff) }
  }
  records.bestDepth = Math.max(records.bestDepth, state.floor.index + 1)
  if (won) records.wins++
  else records.deaths++
  records.runs.unshift({ seed: state.seed, floor: state.floor.index + 1, score: state.hero.gold, won, date: new Date().toISOString() })
  records.runs = records.runs.slice(0, 20)
  analysis = analysisFor(state, won ? 'complete' : 'lost')
  records.analyses.unshift(analysis)
  records.analyses = records.analyses.slice(0, 20)
  analysisNext = won ? 'session' : 'succession'
  saved = undefined
  renderer.setSavedRun(undefined)
  route = { ...route, screen: 'analysis' }
  void Promise.all([deleteRun(), saveRecords(records), saveCampaignRoute(campaign)])
}

function run(game: RunState, command: string): ReturnType<typeof perform> {
  const events = [] as ReturnType<typeof perform>
  for (let i = 0; i < 18; i++) {
    const x = game.hero.x
    const y = game.hero.y
    const next = performTracked(game, command)
    events.push(...next)
    const threats = game.floor.actors.some(actor => actor.hostile && Math.max(Math.abs(actor.x - game.hero.x), Math.abs(actor.y - game.hero.y)) <= 7 && game.floor.tiles[actor.y * 48 + actor.x].visible)
    if (game.status !== 'playing' || game.modal || (x === game.hero.x && y === game.hero.y) || threats) break
  }
  return events
}

function performTracked(game: RunState, command: string): ReturnType<typeof perform> {
  const before = telemetrySnapshot(game)
  const events = perform(game, command)
  observeTelemetryTurn(game, before, events, command)
  return events
}

function suspendRun(): void {
  if (!state) return
  saved = structuredClone(state)
  renderer.setSavedRun(saved)
  analysis = analysisFor(state, 'suspended')
  records.analyses.unshift(analysis)
  records.analyses = records.analyses.slice(0, 20)
  analysisNext = 'session'
  route = { ...route, screen: 'analysis' }
  void Promise.all([saveRun(saved), saveRecords(records)])
  redraw()
}

function continueAnalysis(): void {
  const next = analysisNext
  analysis = undefined
  analysisNext = undefined
  if (next === 'succession') { beginSuccession(); return }
  state = undefined
  route = { screen: 'splash', biome: campaign.selectedBiome }
  redraw()
}

function directionFor(command: string): Direction | undefined {
  const directions: Record<string, Direction> = { i: 'nw', o: 'n', p: 'ne', k: 'w', ';': 'e', ',': 'sw', '.': 's', '/': 'se', ArrowUp: 'n', ArrowDown: 's', ArrowLeft: 'w', ArrowRight: 'e', Numpad7: 'nw', Numpad8: 'n', Numpad9: 'ne', Numpad4: 'w', Numpad5: 'wait', Numpad6: 'e', Numpad1: 'sw', Numpad2: 's', Numpad3: 'se', l: 'wait', Enter: 'wait' }
  return directions[command] ?? directions[command.toLowerCase()]
}

function spellEffectForInput(game: RunState, keyboardEvent: KeyboardEvent, direction: Direction | undefined): string | undefined {
  const modalItem = game.modal?.kind === 'target' && game.modal.action === 'spell' ? game.modal.item : undefined
  const quickCastItem = keyboardEvent.altKey && direction && direction !== 'wait' ? game.hero.inventory.find(id => ITEM[id]?.use === 'spell') : undefined
  return ITEM[modalItem ?? quickCastItem ?? '']?.spell
}

function shouldPrevent(command: string): boolean { return command === 'settings' || Boolean(directionFor(command)) || [' ', 'Escape', '`', '[', ']', 'h', 'H', 'j', 'J', 'u', 'U', 'd', 'D', 't', 'T', 'e', 'E', 'a', 'A', 'b', 'B', 'r', 'R', 'g', 'G', 'c', 'C', 'q', 'Q', 'x', 'X', 's', 'S'].includes(command) }
