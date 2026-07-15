import './style.css'
import { AudioBus } from './audio'
import { AUTOPLAY_TURN_MS, autoplayDecision, autoplayModeLabel, autoplayPolicyLabel, autoplayTraceFingerprint, createAutoplayContext, nextAutoplayMode, nextAutoplayPolicy, recordAutoplayTransition, type AutoplayContext, type AutoplayDecision } from './autoplay'
import { latestAutoplayDiagnostic, saveAutoplayDiagnostic } from './autoplay-log'
import { ITEM } from './content'
import { completeCampaignArea, createHubState, event, hasEvent, heirNameFor, hubView, hydrateEncyclopediaLegacy, initialCampaignRoute, initialRoute, navigate, newHero, newRun, perform, quickCast, recordCampaignSacrifice, recordDeath, unlockCampaignArea, type ScreenRoute } from './engine'
import { TerminalRenderer } from './renderer'
import { advanceStory, createStory, openingLore, successionLore, type LoadingState, type StoryState } from './lore'
import { commandForKey, loadSettings, saveSettings, setKeyBinding, settingChoices, settingsPageCount, type GameSettings } from './settings'
import { courierMenuEntries, deleteCourier, loadCouriers, saveCourier, selectCourier } from './storage'
import { analysisFor, observeTelemetryTurn, telemetrySnapshot } from './telemetry'
import type { AutoplayDiagnostic, AutoplayTerminal, AutoplayTraceEntry, CampaignRouteState, CourierDraft, CourierSave, Direction, Hero, HubState, LegacyRecord, Records, RunAnalysis, RunState } from './types'
import { nextVisualMode, normalizeVisualMode } from './visual-mode'

const canvas = document.querySelector<HTMLCanvasElement>('#game')!
const renderer = new TerminalRenderer(canvas)
const audio = new AudioBus()
let settings: GameSettings = loadSettings()
let state: RunState | undefined
let saved: RunState | undefined
let couriers: CourierSave[] = []
let selectedCourierId: string | undefined
let activeCourier: CourierSave | undefined
let courierDraft: CourierDraft | undefined
let confirmingCourierDelete = false
let inheritedCampaign: CampaignRouteState | undefined
let successorParentId: string | undefined
let createAfterStory = false
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
let analysisNext: 'checkpoint' | 'succession' | 'session' | undefined
let autoplayTimer: number | undefined
let autoplayContext: AutoplayContext = createAutoplayContext()
let autoplayTrace: AutoplayTraceEntry[] = []
let autoplayLogged = false
let autoplayDiagnostic: AutoplayDiagnostic | undefined = latestAutoplayDiagnostic()

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
renderer.setAutoplayDiagnostic(autoplayDiagnostic)
applyGameZoom()
canvas.addEventListener('wheel', mouseEvent => {
  mouseEvent.preventDefault()
  setGameZoom(gameZoom + (mouseEvent.deltaY < 0 ? .25 : -.25))
}, { passive: false })

void loadCouriers().then(loaded => {
  couriers = loaded.couriers
  selectedCourierId = loaded.selectedId
  activateCourier(selectedCourierId)
  redraw()
})
redraw()

window.addEventListener('keydown', keyboardEvent => {
  if (keyboardEvent.metaKey || keyboardEvent.ctrlKey) return
  if (route.screen === 'level' && state?.status === 'playing' && keyboardEvent.key.toLowerCase() === 'f') { keyboardEvent.preventDefault(); keyboardEvent.shiftKey ? toggleAutoplayPolicy() : toggleAutoplay(); return }
  if (route.screen === 'level' && state?.status === 'playing' && settings.autoplayMode !== 'off') { keyboardEvent.preventDefault(); return }
  if (zoomForKey(keyboardEvent)) { keyboardEvent.preventDefault(); return }
  const command = commandForKey(keyboardEvent.key, settings)
  if (route.screen === 'analysis') {
    if (!keyboardEvent.repeat) { keyboardEvent.preventDefault(); continueAnalysis() }
    return
  }
  if (route.screen === 'loading') { keyboardEvent.preventDefault(); return }
  if (route.screen === 'approach' && story) { handleStoryInput(keyboardEvent); return }
  if (route.screen === 'splash' || route.screen === 'title') { handleCourierTitle(keyboardEvent); return }
  if (route.screen === 'createCourier') { handleCourierCreation(keyboardEvent); return }
  if (state?.modal?.kind === 'settings') { keyboardEvent.preventDefault(); handleSettingsInput(keyboardEvent.key); return }
  if (shouldPrevent(command ?? keyboardEvent.key)) keyboardEvent.preventDefault()
  if (keyboardEvent.key.toLowerCase() === 'v' && command === 'v') { toggleVisualMode(); return }
  if (route.screen !== 'level') {
    let nextRoute = navigate(route, command ?? keyboardEvent.key, Boolean(saved))
    if (nextRoute === route) return
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
  executeGameplayCommand(command, { quickCast: Boolean(keyboardEvent.altKey && direction && direction !== 'wait'), run: Boolean(keyboardEvent.shiftKey && direction && direction !== 'wait' && !state.modal), spellEffect: spellEffectForInput(state, keyboardEvent, direction) })
})

function activateCourier(id: string | undefined): void {
  selectedCourierId = id
  activeCourier = couriers.find(courier => courier.identity.id === id && !courier.archived)
  if (!activeCourier) { saved = undefined; state = undefined; records = { bestDepth: 0, wins: 0, deaths: 0, runs: [], analyses: [] }; campaign = initialCampaignRoute(); hub = createHubState(0); return }
  saved = activeCourier.run ? structuredClone(activeCourier.run) : undefined
  records = activeCourier.records
  campaign = activeCourier.campaign
  if (saved) hydrateEncyclopediaLegacy(saved, campaign.legacyRecords)
  hub = { ...createHubState(saved?.seed ?? 0), unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
  route = { screen: route.screen === 'splash' ? 'splash' : 'title', biome: campaign.selectedBiome }
  void selectCourier(activeCourier.identity.id)
}

function courierMenu() {
  return { entries: courierMenuEntries(couriers), selectedId: selectedCourierId, confirmingDelete: confirmingCourierDelete }
}

function handleCourierTitle(keyboardEvent: KeyboardEvent): void {
  const key = keyboardEvent.key
  const command = key.toLowerCase()
  if (route.screen === 'splash' && !['n', 'N', 'l', 'L', 'Enter', 'ArrowUp', 'ArrowDown', 'd', 'D'].includes(key)) { route = { ...route, screen: 'title' }; redraw(); return }
  route = { ...route, screen: 'title' }
  const entries = courierMenuEntries(couriers)
  const selectedIndex = Math.max(0, entries.findIndex(entry => entry.id === selectedCourierId))
  if (confirmingCourierDelete) {
    if (command === 'd') {
      const id = selectedCourierId
      if (id) { couriers = couriers.filter(courier => courier.identity.id !== id); void deleteCourier(id) }
      confirmingCourierDelete = false
      activateCourier(courierMenuEntries(couriers)[0]?.id)
    } else if (key === 'Escape' || key === '`') confirmingCourierDelete = false
    redraw()
    return
  }
  if (key === 'ArrowUp' || key === 'ArrowDown') {
    if (entries.length) {
      const offset = key === 'ArrowUp' ? entries.length - 1 : 1
      activateCourier(entries[(selectedIndex + offset) % entries.length].id)
    }
  } else if (command === 'n') {
    courierDraft = { name: '', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint', focus: 0 }
    inheritedCampaign = undefined
    successorParentId = undefined
    route = { ...route, screen: 'createCourier' }
  } else if ((command === 'l' || key === 'Enter') && activeCourier) resumeCourier()
  else if (command === 'd' && activeCourier) confirmingCourierDelete = true
  audio.play([event('menu')])
  redraw()
}

function handleCourierCreation(keyboardEvent: KeyboardEvent): void {
  if (!courierDraft) return
  const key = keyboardEvent.key
  if (key === 'Escape' || key === '`') { courierDraft = undefined; route = { ...route, screen: 'title' }; redraw(); return }
  if (key === 'Tab') { courierDraft = { ...courierDraft, focus: ((courierDraft.focus + (keyboardEvent.shiftKey ? 3 : 1)) % 4) as CourierDraft['focus'] }; redraw(); return }
  if (key === 'Enter') { createCourierFromDraft(); return }
  if ((key === 'ArrowLeft' || key === 'ArrowRight') && courierDraft.focus > 0) {
    const forward = key === 'ArrowRight'
    if (courierDraft.focus === 1) {
      const options: CourierDraft['origin'][] = ['mineborn', 'mosswalker', 'cavernSeeker']
      const index = options.indexOf(courierDraft.origin)
      courierDraft = { ...courierDraft, origin: options[(index + (forward ? 1 : options.length - 1)) % options.length] }
    } else if (courierDraft.focus === 2) {
      const options: CourierDraft['calling'][] = ['trailguard', 'pathmaker', 'spiritbearer']
      const index = options.indexOf(courierDraft.calling)
      courierDraft = { ...courierDraft, calling: options[(index + (forward ? 1 : options.length - 1)) % options.length] }
    } else courierDraft = { ...courierDraft, deathMode: courierDraft.deathMode === 'checkpoint' ? 'ironTrail' : 'checkpoint' }
    redraw(); return
  }
  if (courierDraft.focus === 0) {
    if (key === 'Backspace' || key === 'Delete') courierDraft = { ...courierDraft, name: courierDraft.name.slice(0, -1) }
    else if (/^[a-zA-Z0-9 ]$/.test(key) && courierDraft.name.length < 20) courierDraft = { ...courierDraft, name: `${courierDraft.name}${key}` }
  }
  redraw()
}

function createCourierFromDraft(): void {
  if (!courierDraft) return
  const name = courierDraft.name.trim() || 'Unnamed Courier'
  const id = crypto.randomUUID()
  const identity = { id, name, origin: courierDraft.origin, calling: courierDraft.calling, deathMode: courierDraft.deathMode, createdAt: new Date().toISOString(), ...(successorParentId ? { parentId: successorParentId } : {}) }
  const courier: CourierSave = { version: 1, identity, heir: newHero(identity), campaign: structuredClone(inheritedCampaign ?? initialCampaignRoute()), records: { bestDepth: 0, wins: 0, deaths: 0, runs: [], analyses: [] } }
  couriers = [...couriers, courier]
  activeCourier = courier
  selectedCourierId = id
  records = courier.records
  campaign = courier.campaign
  heir = structuredClone(courier.heir)
  courierDraft = undefined
  inheritedCampaign = undefined
  successorParentId = undefined
  const seed = Math.floor(Math.random() * 0x7fffffff)
  beginTrailhead(seed, openingLore(seed), heir)
  void saveCourier(courier, id)
  audio.play([event('menu')])
  redraw()
}

function resumeCourier(): void {
  if (!activeCourier) return
  records = activeCourier.records
  campaign = activeCourier.campaign
  hub = { ...createHubState(activeCourier.run?.seed ?? 0), unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
  if (activeCourier.run) { state = structuredClone(activeCourier.run); saved = structuredClone(activeCourier.run); route = { screen: 'level', biome: campaign.selectedBiome }; recordedEnd = false; resetAutoplaySession() }
  else { state = undefined; saved = undefined; heir = activeCourier.heir ? structuredClone(activeCourier.heir) : newHero(activeCourier.identity); route = { screen: 'hub', biome: campaign.selectedBiome, hubAction: 'routes' } }
  void selectCourier(activeCourier.identity.id)
}

function persistActiveCourier(restoreCheckpoint = false): void {
  if (!activeCourier) return
  activeCourier.run = state && state.status === 'playing' ? structuredClone(state) : restoreCheckpoint && activeCourier.checkpoint ? structuredClone(activeCourier.checkpoint) : undefined
  if (state?.status === 'playing') activeCourier.heir = structuredClone(state.hero)
  activeCourier.campaign = campaign
  activeCourier.records = records
  saved = activeCourier.run ? structuredClone(activeCourier.run) : undefined
  couriers = couriers.map(courier => courier.identity.id === activeCourier!.identity.id ? activeCourier! : courier)
  void saveCourier(activeCourier, selectedCourierId)
}

function checkpointActiveCourier(): void {
  if (!activeCourier || !state) return
  activeCourier.checkpoint = structuredClone(state)
  persistActiveCourier()
}

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
  if (!activeCourier) return
  campaign = { ...campaign, selectedBiome: route.biome }
  state = newRun(route.heirSeed, route.biome, 0, heir, campaign.rescuedNpcs, campaign.legacyRecords)
  renderer.setHeroFacingLeft(false)
  heir = state.hero
  activeCourier.heir = structuredClone(state.hero)
  saved = structuredClone(state)
  recordedEnd = false
  resetAutoplaySession()
  activeCourier.checkpoint = structuredClone(state)
  persistActiveCourier()
  audio.play([event('menu')])
  renderer.trigger([event('floor')], state)
}

function beginTrailhead(seed: number, scene: ReturnType<typeof openingLore> | ReturnType<typeof successionLore>, nextHero?: Hero): void {
  route = { screen: 'approach', biome: campaign.selectedBiome, heirSeed: seed }
  heir = nextHero
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
  if (createAfterStory) {
    createAfterStory = false
    courierDraft = { name: '', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint', focus: 0 }
    route = { ...route, screen: 'createCourier' }
  } else route = { ...route, screen: 'hub', hubAction: 'routes' }
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
      createAfterStory = true
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
  if (!activeCourier) return
  saved = undefined
  activeCourier.run = undefined
  activeCourier.checkpoint = undefined
  activeCourier.campaign = campaign
  activeCourier.heir = structuredClone(heir)
  state = undefined
  persistActiveCourier()
  route = { ...route, screen: 'hub', biome: campaign.selectedBiome, hubAction: 'routes' }
}

function unlockGateDestination(): void {
  if (!state?.gateDestination) return
  for (const lineageEvent of state.lineageEvents ?? []) campaign = recordCampaignSacrifice(campaign, lineageEvent)
  campaign = unlockCampaignArea(campaign, state.gateDestination)
  hub = { ...hub, unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
  route = { ...route, biome: campaign.selectedBiome }
  state.gateDestination = undefined
  if (state.status === 'playing') persistActiveCourier()
}

function persistRescuedRoster(): void {
  if (!state?.rescuedNpcs?.length) return
  const rescuedNpcs = [...campaign.rescuedNpcs]
  for (const npc of state.rescuedNpcs) if (!rescuedNpcs.some(existing => existing.id === npc.id)) rescuedNpcs.push({ ...npc })
  campaign = { ...campaign, rescuedNpcs }
  hub = { ...hub, rescued: rescuedNpcs }
  if (state.status === 'playing') persistActiveCourier()
}

function toggleVisualMode(): void {
  const mode = nextVisualMode(renderer.visualMode)
  renderer.setVisualMode(mode)
  try { localStorage.setItem('jomon-visual-mode', mode) } catch { }
  redraw()
}

function toggleAutoplay(): void {
  if (settings.autoplayMode !== 'off') finalizeAutoplay('manual', 'mode toggled off')
  settings = { ...settings, autoplayMode: nextAutoplayMode(settings.autoplayMode) }
  if (settings.autoplayMode !== 'off') resetAutoplaySession()
  saveSettings(settings)
  if (state) state.messages.unshift(`Autoplay: ${autoplayModeLabel(settings.autoplayMode)} · ${autoplayPolicyLabel(settings.autoplayPolicy)}.`)
  audio.play([event('menu')])
  redraw()
}

function toggleAutoplayPolicy(): void {
  if (settings.autoplayMode !== 'off') finalizeAutoplay('manual', 'policy changed')
  settings = { ...settings, autoplayPolicy: nextAutoplayPolicy(settings.autoplayPolicy) }
  resetAutoplaySession()
  saveSettings(settings)
  if (state) state.messages.unshift(`Autoplay policy: ${autoplayPolicyLabel(settings.autoplayPolicy)}.`)
  audio.play([event('menu')])
  redraw()
}

function resetAutoplaySession(): void {
  autoplayContext = createAutoplayContext()
  autoplayTrace = []
  autoplayLogged = false
}

function finalizeAutoplay(outcome: AutoplayTerminal, reason: string): void {
  if (autoplayLogged || !autoplayTrace.length || !state || settings.autoplayMode === 'off') return
  autoplayLogged = true
  autoplayDiagnostic = { id: `${state.seed}:${state.floor.seed}:${Date.now()}`, date: new Date().toISOString(), seed: state.seed, biome: state.area ?? state.floor.biome, floor: (state.areaFloor ?? state.floor.index % 4) + 1, mode: settings.autoplayMode, policy: settings.autoplayPolicy, outcome, turns: state.turn, reason, trace: structuredClone(autoplayTrace) }
  saveAutoplayDiagnostic(autoplayDiagnostic)
  renderer.setAutoplayDiagnostic(autoplayDiagnostic)
}

function canAutoplay(): boolean { return route.screen === 'level' && state?.status === 'playing' && settings.autoplayMode !== 'off' }

function syncAutoplay(): void {
  if (!canAutoplay()) {
    if (autoplayTimer !== undefined) window.clearTimeout(autoplayTimer)
    autoplayTimer = undefined
    return
  }
  if (autoplayTimer !== undefined) return
  autoplayTimer = window.setTimeout(() => {
    autoplayTimer = undefined
    if (!state || !canAutoplay()) return
    const decision = autoplayDecision(state, settings.autoplayMode, settings.autoplayPolicy, autoplayContext)
    if (!decision) {
      finalizeAutoplay('stalled', 'cycle guard or no legal progress action')
      settings = { ...settings, autoplayMode: 'off' }
      saveSettings(settings)
      state.messages.unshift('Autoplay stalled; trace saved locally.')
      redraw()
      return
    }
    executeGameplayCommand(decision.command, { autoplay: decision })
  }, AUTOPLAY_TURN_MS)
}

function redraw(): void {
  canvas.dataset.route = route.screen
  canvas.dataset.status = state?.status ?? 'none'
  canvas.dataset.autoplay = settings.autoplayMode
  canvas.dataset.autoplayPolicy = settings.autoplayPolicy
  renderer.render(route, state, records, hubView(route.heirSeed ?? 0, hub), story, loading, analysis, courierMenu(), courierDraft, settings.autoplayMode)
  syncAutoplay()
}

function finish(won: boolean): void {
  if (!state) return
  finalizeAutoplay(won ? 'complete' : 'dead', won ? 'campaign complete' : 'courier defeated')
  recordedEnd = true
  const checkpointDeath = !won && state.hero.deathMode === 'checkpoint'
  if (!won && !checkpointDeath) {
    campaign = recordDeath(campaign, state, heirNameFor(route.heirSeed ?? state.seed))
    const record = campaign.legacyRecords.at(-1)
    if (!record) throw new Error('missing death legacy record')
    pendingSuccessor = { record, seed: Math.floor(Math.random() * 0x7fffffff) }
    inheritedCampaign = structuredClone(campaign)
    successorParentId = activeCourier?.identity.id
  }
  records.bestDepth = Math.max(records.bestDepth, state.floor.index + 1)
  if (won) records.wins++
  else records.deaths++
  records.runs.unshift({ seed: state.seed, floor: state.floor.index + 1, score: state.hero.gold, won, date: new Date().toISOString() })
  records.runs = records.runs.slice(0, 20)
  analysis = analysisFor(state, won ? 'complete' : 'lost')
  records.analyses.unshift(analysis)
  records.analyses = records.analyses.slice(0, 20)
  analysisNext = won ? 'session' : checkpointDeath ? 'checkpoint' : 'succession'
  if (checkpointDeath && activeCourier?.checkpoint) saved = structuredClone(activeCourier.checkpoint)
  else saved = undefined
  if (!won && !checkpointDeath && activeCourier) { activeCourier.run = undefined; activeCourier.archived = true }
  route = { ...route, screen: 'analysis' }
  persistActiveCourier(checkpointDeath)
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

type GameplayCommandOptions = { quickCast?: boolean; run?: boolean; spellEffect?: string; autoplay?: AutoplayDecision }

function executeGameplayCommand(command: string, options: GameplayCommandOptions = {}): void {
  if (!state || route.screen !== 'level' || state.status !== 'playing') return
  const game = state
  const autoplayBefore = options.autoplay ? structuredClone(game) : undefined
  const autoplayFingerprint = options.autoplay ? autoplayTraceFingerprint(game) : undefined
  const previousX = game.hero.x
  const previousLevel = game.hero.level
  let events = [] as ReturnType<typeof perform>
  if (options.quickCast) {
    const direction = directionFor(command)
    if (direction && direction !== 'wait') {
      const before = telemetrySnapshot(game)
      events = quickCast(game, direction)
      observeTelemetryTurn(game, before, events, command)
    }
  } else if (options.run && !game.modal) events = run(game, command)
  else events = performTracked(game, command)
  if (game.hero.level > previousLevel) events.push(event('level'))
  if (game.hero.x !== previousX) renderer.setHeroFacingLeft(game.hero.x < previousX)
  if (options.autoplay && autoplayBefore && autoplayFingerprint) {
    recordAutoplayTransition(autoplayContext, autoplayBefore, command, game)
    autoplayTrace.push({
      turn: autoplayBefore.turn,
      fingerprint: autoplayFingerprint,
      command,
      reason: options.autoplay.reason,
      candidates: options.autoplay.candidates,
      events: events.map(entry => entry.type),
      nextFingerprint: autoplayTraceFingerprint(game),
      before: { x: autoplayBefore.hero.x, y: autoplayBefore.hero.y, health: autoplayBefore.hero.health, focus: autoplayBefore.hero.focus, bombs: autoplayBefore.hero.bombs, ropes: autoplayBefore.hero.ropes, objective: autoplayBefore.floor.objective.status },
      after: { x: game.hero.x, y: game.hero.y, health: game.hero.health, focus: game.hero.focus, bombs: game.hero.bombs, ropes: game.hero.ropes, objective: game.floor.objective.status, ...(game.modal ? { modal: game.modal.kind } : {}) }
    })
    if (autoplayTrace.length > 600) autoplayTrace = autoplayTrace.slice(-600)
    if (hasEvent(events, 'areaComplete')) finalizeAutoplay('complete', 'area completed')
    else if (hasEvent(events, 'death') || game.status === 'dead') finalizeAutoplay('dead', 'courier defeated')
  }
  audio.play(events)
  renderer.trigger(events, game, options.spellEffect)
  if (hasEvent(events, 'suspend')) { suspendRun(); return }
  if (hasEvent(events, 'floor')) { saved = structuredClone(game); checkpointActiveCourier() }
  if (hasEvent(events, 'rescue')) persistRescuedRoster()
  if (hasEvent(events, 'areaComplete')) completeArea()
  if (hasEvent(events, 'gateResolved')) unlockGateDestination()
  if ((hasEvent(events, 'death') || hasEvent(events, 'win')) && !recordedEnd) finish(hasEvent(events, 'win'))
  else persistActiveCourier()
  redraw()
}

function performTracked(game: RunState, command: string): ReturnType<typeof perform> {
  const before = telemetrySnapshot(game)
  const events = perform(game, command)
  observeTelemetryTurn(game, before, events, command)
  return events
}

function suspendRun(): void {
  if (!state) return
  finalizeAutoplay('manual', 'run suspended')
  saved = structuredClone(state)
  analysis = analysisFor(state, 'suspended')
  records.analyses.unshift(analysis)
  records.analyses = records.analyses.slice(0, 20)
  analysisNext = 'session'
  route = { ...route, screen: 'analysis' }
  persistActiveCourier()
  redraw()
}

function continueAnalysis(): void {
  const next = analysisNext
  analysis = undefined
  analysisNext = undefined
  if (next === 'succession') { beginSuccession(); return }
  if (next === 'checkpoint' && activeCourier?.checkpoint) {
    state = structuredClone(activeCourier.checkpoint)
    saved = structuredClone(activeCourier.checkpoint)
    persistActiveCourier()
    route = { screen: 'level', biome: campaign.selectedBiome }
    recordedEnd = false
    redraw()
    return
  }
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
