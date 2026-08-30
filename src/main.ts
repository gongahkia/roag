import { AudioBus } from './audio'
import type { AutoplayContext, AutoplayDecision } from './autoplay'
import { latestAutoplayDiagnostic, saveAutoplayDiagnostic } from './autoplay-log'
import { findStructurallyPlayableCampaignSeed } from './campaign-validation'
import { ITEM, biomeName } from './content'
import { nextCourierSelection } from './courier-menu'
import { abandonGalaxyCargo, acceptGalaxyContract, addCompanionLeads, advanceTransitWindow, applyGalaxySiteConditions, availableGalaxySites, beginCompanionRecovery, buyHubItem, campaignContinuationPending, changeCampaignCompanionControlMode, changeCompanionRoster, cloneCompanions, companionLodgeAction, completeCampaignArea, completeCampaignTier, completeCompanionRecovery, continueCampaignRoute, createGalaxy, createHubState, deliverGalaxyContracts, discoverLinkedSites, equipHubItem, event, galaxyRouteLength, galaxyRouteSituation, galaxySnapshot, hasEvent, hubCampaignStatus, hubCarryoverSummary, hubEquipment, hubStock, hubView, hydrateEncyclopediaLegacy, initialCampaignRoute, initialRoute, loseGalaxyCourier, moveOutpost, navigate, newHero, newRun, newTransitRun, nextArea, outpostInteraction, outpostSpawn, perform, quickCast, reconcileGalaxy, recordCampaignSacrifice, recordDeath, recordGalaxyLanding, recoverGalaxyRouteCaches, saveGalaxySite, selectGalaxyCourier, setActiveGalaxySite, snapshotCampaignCarryover, transferCampaignCarryover, unlockCampaignArea, type ScreenRoute } from './engine'
import { shouldPreventKeyboardDefault } from './input-policy'
import { outpostAutoplayCommand } from './outpost-autoplay'
import { TerminalRenderer } from './renderer'
import { advanceStory, createStory, endingLore, openingLore, successionLore, type LoadingState, type StoryState, type TransitState } from './lore'
import { LORE_CODEX_PAGES } from './lore-codex'
import { commandForKey, loadSettings, saveSettings, setKeyBinding, settingChoices, settingsPageCount, type GameSettings } from './settings'
import { courierMenuEntries, deleteCourier, flushCourierWrites, loadCouriers, saveCourier, selectCourier } from './storage'
import { analysisFor, observeTelemetryTurn, telemetrySnapshot } from './telemetry'
import { type AutoplayDiagnostic, type AutoplayTerminal, type AutoplayTraceEntry, type CampaignRouteState, type CourierDraft, type CourierSave, type Direction, type GalaxyState, type Hero, type HubState, type LegacyRecord, type Records, type RunAnalysis, type RunState } from './types'
import { getTile } from './world'
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
let courierCreationPending = false
let records: Records = { bestDepth: 0, wins: 0, deaths: 0, runs: [], analyses: [] }
let recordedEnd = false
let route: ScreenRoute = initialRoute()
let hub: HubState = createHubState(0)
let hubPosition = outpostSpawn()
let hubNotice: string | undefined
let heir: Hero | undefined
let campaign: CampaignRouteState = initialCampaignRoute()
let gameZoom = loadGameZoom()
let story: StoryState | undefined
let storyExit: 'hub' | 'analysis' = 'hub'
let loading: LoadingState | undefined
let transit: TransitState | undefined
let finishTransit: (() => void) | undefined
let pendingSuccessor: { record: LegacyRecord; seed: number } | undefined
let analysis: RunAnalysis | undefined
let analysisNext: 'checkpoint' | 'succession' | 'session' | 'victory' | undefined
let autoplayTimer: number | undefined
type AutoplayFeature = typeof import('./autoplay-runtime')
let autoplayFeature: AutoplayFeature | undefined
let autoplayFeatureLoad: Promise<AutoplayFeature> | undefined
let autoplayContext: AutoplayContext | undefined
let autoplayTrace: AutoplayTraceEntry[] = []
let autoplayLogged = false
let autoplayDiagnostic: AutoplayDiagnostic | undefined = latestAutoplayDiagnostic()
let bootstrapState: 'loading' | 'ready' | 'error' = 'loading'
let persistenceState: 'saved' | 'saving' | 'error' = 'saved'
let pendingPersistence = 0
const failedPersistence = new Map<string, { operation: () => Promise<void>; clearKeys: string[] }>()

const loadAutoplayFeature = (): Promise<AutoplayFeature> => {
  if (autoplayFeature) return Promise.resolve(autoplayFeature)
  autoplayFeatureLoad ??= import('./autoplay-runtime').then(feature => {
    autoplayFeature = feature
    return feature
  }).catch(error => {
    autoplayFeatureLoad = undefined
    throw error
  })
  return autoplayFeatureLoad
}

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
renderer.setBootstrapState('loading')
renderer.setPersistenceState(persistenceState)
applyGameZoom()
canvas.addEventListener('wheel', mouseEvent => {
  mouseEvent.preventDefault()
  setGameZoom(gameZoom + (mouseEvent.deltaY < 0 ? .25 : -.25))
}, { passive: false })
let cameraDrag: { x: number; y: number } | undefined
canvas.addEventListener('pointerdown', mouseEvent => {
  if (mouseEvent.button !== 1 || route.screen !== 'level' || state?.status !== 'playing') return
  mouseEvent.preventDefault()
  cameraDrag = { x: mouseEvent.clientX, y: mouseEvent.clientY }
  canvas.setPointerCapture(mouseEvent.pointerId)
})
canvas.addEventListener('pointermove', mouseEvent => {
  if (!cameraDrag) return
  const dx = Math.trunc((cameraDrag.x - mouseEvent.clientX) / 10)
  const dy = Math.trunc((cameraDrag.y - mouseEvent.clientY) / 12)
  if (!dx && !dy) return
  cameraDrag = { x: mouseEvent.clientX, y: mouseEvent.clientY }
  renderer.panCamera(dx, dy)
})
canvas.addEventListener('pointerup', mouseEvent => {
  if (!cameraDrag) return
  cameraDrag = undefined
  if (canvas.hasPointerCapture(mouseEvent.pointerId)) canvas.releasePointerCapture(mouseEvent.pointerId)
})

void bootstrapCouriers()

window.addEventListener('keydown', keyboardEvent => {
  if (keyboardEvent.ctrlKey && route.screen === 'level' && state?.status === 'playing') {
    const delta = ({ ArrowUp: [0, -4], ArrowDown: [0, 4], ArrowLeft: [-4, 0], ArrowRight: [4, 0] } as const)[keyboardEvent.key]
    if (delta) { keyboardEvent.preventDefault(); renderer.panCamera(delta[0], delta[1]); return }
  }
  if (keyboardEvent.metaKey || keyboardEvent.ctrlKey) return
  if (route.screen === 'transit') {
    keyboardEvent.preventDefault()
    finishTransit?.()
    return
  }
  if (bootstrapState !== 'ready') {
    keyboardEvent.preventDefault()
    if (bootstrapState === 'error' && keyboardEvent.key === 'F2') void bootstrapCouriers()
    return
  }
  if (keyboardEvent.key === 'F2' && persistenceState === 'error') { keyboardEvent.preventDefault(); retryFailedPersistence(); return }
  if (keyboardEvent.key === 'Tab' && shouldPreventKeyboardDefault(keyboardEvent.key)) keyboardEvent.preventDefault()
  if ((route.screen === 'hub' || route.screen === 'area' || route.screen === 'level' && state?.status === 'playing') && keyboardEvent.key.toLowerCase() === 'f') { keyboardEvent.preventDefault(); keyboardEvent.shiftKey ? toggleAutoplayPolicy() : toggleAutoplay(); return }
  const command = commandForKey(keyboardEvent.key, settings)
  if (route.screen === 'level' && state?.status === 'playing' && settings.autoplayMode !== 'off' && keyboardEvent.key.toLowerCase() === 'v' && command === 'v') { keyboardEvent.preventDefault(); toggleVisualMode(); return }
  if (zoomForKey(keyboardEvent)) { keyboardEvent.preventDefault(); return }
  if (canAutoplay() && (keyboardEvent.key === 'Escape' || keyboardEvent.key === '`')) {
    settings = { ...settings, autoplayMode: 'off' }
    saveSettings(settings)
  }
  if (canAutoplay()) { keyboardEvent.preventDefault(); return }
  if (keyboardEvent.key.toLowerCase() === 'v' && command === 'v') { keyboardEvent.preventDefault(); toggleVisualMode(); return }
  if (route.screen === 'analysis') {
    if (!keyboardEvent.repeat) { keyboardEvent.preventDefault(); continueAnalysis() }
    return
  }
  if (route.screen === 'loading') { keyboardEvent.preventDefault(); return }
  if (route.screen === 'approach' && story) { handleStoryInput(keyboardEvent); return }
  if (route.screen === 'codex') { handleCodexInput(keyboardEvent); return }
  if (route.screen === 'splash' || route.screen === 'title') { handleCourierTitle(keyboardEvent); return }
  if (route.screen === 'createCourier') { handleCourierCreation(keyboardEvent); return }
  if (state?.modal?.kind === 'settings') { keyboardEvent.preventDefault(); handleSettingsInput(keyboardEvent.key); return }
  if (shouldPreventKeyboardDefault(command ?? keyboardEvent.key)) keyboardEvent.preventDefault()
  if (route.screen !== 'level') {
    const input = command ?? keyboardEvent.key
    if (route.screen === 'hub' && handleHubInput(input, keyboardEvent.shiftKey)) { audio.play([event('menu')]); redraw(); return }
    if (route.screen === 'sector' && handleSectorInput(input)) { audio.play([event('menu')]); redraw(); return }
    let nextRoute = navigate(route, input, Boolean(saved))
    if (nextRoute === route) return
    if (nextRoute.screen === 'title') { nextRoute = { ...nextRoute, heirSeed: undefined }; story = undefined }
    if (nextRoute.screen === 'hub' && route.screen !== 'hub') hubPosition = outpostSpawn()
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

async function bootstrapCouriers(): Promise<void> {
  bootstrapState = 'loading'
  renderer.setBootstrapState('loading', 'Loading courier records…')
  redraw()
  try {
    const loaded = await loadCouriers()
    couriers = loaded.couriers
    selectedCourierId = loaded.selectedId
    activateCourier(selectedCourierId, false)
    bootstrapState = 'ready'
    renderer.setBootstrapState('ready')
  } catch {
    bootstrapState = 'error'
    renderer.setBootstrapState('error', 'Courier records unavailable')
  }
  redraw()
}

function updatePersistenceState(): void {
  persistenceState = failedPersistence.size ? 'error' : pendingPersistence ? 'saving' : 'saved'
  renderer.setPersistenceState(persistenceState)
}

function recordPersistence(key: string, operation: () => Promise<void>, clearKeys: string[] = []): void {
  pendingPersistence++
  updatePersistenceState()
  void operation().then(() => {
    pendingPersistence--
    failedPersistence.delete(key)
    for (const clearKey of clearKeys) failedPersistence.delete(clearKey)
    updatePersistenceState()
    redraw()
  }).catch(() => {
    pendingPersistence--
    failedPersistence.set(key, { operation, clearKeys })
    updatePersistenceState()
    redraw()
  })
}

function persistCourier(courier: CourierSave, selectedId?: string): void {
  const snapshot = structuredClone(courier)
  recordPersistence(`courier:${snapshot.identity.id}`, () => saveCourier(snapshot, selectedId), selectedId ? ['selection'] : [])
}

function retryFailedPersistence(): void {
  if (pendingPersistence) return
  const retries = [...failedPersistence]
  failedPersistence.clear()
  for (const [key, retry] of retries) recordPersistence(key, retry.operation, retry.clearKeys)
}

const flushCourierPersistence = (): void => { void flushCourierWrites() }
document.addEventListener('visibilitychange', () => { if (document.visibilityState === 'hidden') flushCourierPersistence() })
window.addEventListener('pagehide', flushCourierPersistence)

function activateCourier(id: string | undefined, persistSelection = true): void {
  selectedCourierId = id
  activeCourier = couriers.find(courier => courier.identity.id === id && !courier.archived)
  if (!activeCourier) { saved = undefined; state = undefined; records = { bestDepth: 0, wins: 0, deaths: 0, runs: [], analyses: [] }; campaign = initialCampaignRoute(); hub = createHubState(0); hubPosition = outpostSpawn(); return }
  saved = activeCourier.run ? structuredClone(activeCourier.run) : undefined
  records = activeCourier.records
  campaign = activeCourier.campaign
  if (saved) hydrateEncyclopediaLegacy(saved, campaign.legacyRecords)
  hub = { ...createHubState(saved?.seed ?? 0), unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
  hubPosition = outpostSpawn()
  route = { screen: route.screen === 'splash' ? 'splash' : 'title', biome: campaign.selectedBiome }
  if (persistSelection) {
    const selectedId = activeCourier.identity.id
    recordPersistence('selection', () => selectCourier(selectedId))
  }
}

function courierMenu() {
  return { entries: courierMenuEntries(couriers), selectedId: selectedCourierId, confirmingDelete: confirmingCourierDelete }
}

function handleCourierTitle(keyboardEvent: KeyboardEvent): void {
  const key = keyboardEvent.key
  const command = key.toLowerCase()
  if (route.screen === 'splash' && !['n', 'N', 'l', 'L', 'w', 'W', 'Enter', 'ArrowUp', 'ArrowDown', 'd', 'D'].includes(key)) { route = { ...route, screen: 'title' }; redraw(); return }
  route = { ...route, screen: 'title' }
  const entries = courierMenuEntries(couriers)
  if (confirmingCourierDelete) {
    if (command === 'd') {
      const id = selectedCourierId
      if (id) { couriers = couriers.filter(courier => courier.identity.id !== id); recordPersistence(`courier-delete:${id}`, () => deleteCourier(id), ['selection']) }
      confirmingCourierDelete = false
      activateCourier(courierMenuEntries(couriers)[0]?.id)
    } else if (key === 'Escape' || key === '`') confirmingCourierDelete = false
    redraw()
    return
  }
  if (key === 'ArrowUp' || key === 'ArrowDown') {
    const nextId = nextCourierSelection(entries, selectedCourierId, key)
    if (nextId) activateCourier(nextId)
  } else if (command === 'n') {
    courierDraft = { name: '', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint', companionControlMode: 'autonomous', companionDeathMode: 'injury', companionDeathConfirmed: false, focus: 0 }
    inheritedCampaign = undefined
    successorParentId = undefined
    route = { ...route, screen: 'createCourier' }
  } else if ((command === 'l' || key === 'Enter') && activeCourier) resumeCourier()
  else if (command === 'w') route = { ...route, screen: 'codex', codexPage: 0 }
  else if (command === 'd' && activeCourier) confirmingCourierDelete = true
  audio.play([event('menu')])
  redraw()
}

function handleCodexInput(keyboardEvent: KeyboardEvent): void {
  const key = keyboardEvent.key
  if (key === 'Escape' || key.toLowerCase() === 'w') route = { ...route, screen: 'title', codexPage: undefined }
  else if (key === 'ArrowRight' || key === 'Enter' || key === ' ') route = { ...route, codexPage: Math.min(LORE_CODEX_PAGES.length - 1, (route.codexPage ?? 0) + 1) }
  else if (key === 'ArrowLeft' || key === 'Backspace') route = { ...route, codexPage: Math.max(0, (route.codexPage ?? 0) - 1) }
  else return
  keyboardEvent.preventDefault()
  audio.play([event('menu')])
  redraw()
}

function handleCourierCreation(keyboardEvent: KeyboardEvent): void {
  if (!courierDraft) return
  const key = keyboardEvent.key
  if (key === 'Escape' || key === '`') { courierDraft = undefined; route = { ...route, screen: 'title' }; redraw(); return }
  if (key === 'Tab') { courierDraft = { ...courierDraft, focus: ((courierDraft.focus + (keyboardEvent.shiftKey ? 5 : 1)) % 6) as CourierDraft['focus'] }; redraw(); return }
  if (key === 'ArrowUp' || key === 'ArrowDown') { courierDraft = { ...courierDraft, focus: ((courierDraft.focus + (key === 'ArrowUp' ? 5 : 1)) % 6) as CourierDraft['focus'] }; redraw(); return }
  if (key === 'Enter') {
    if (courierDraft.companionDeathMode === 'permadeath' && !courierDraft.companionDeathConfirmed) { courierDraft = { ...courierDraft, companionDeathConfirmed: true }; redraw(); return }
    createCourierFromDraft(); return
  }
  if ((key === 'ArrowLeft' || key === 'ArrowRight') && courierDraft.focus > 0) {
    const forward = key === 'ArrowRight'
    if (courierDraft.focus === 1) {
      const options: CourierDraft['origin'][] = ['mineborn', 'mosswalker', 'cavernSeeker', 'tidebound']
      const index = options.indexOf(courierDraft.origin)
      courierDraft = { ...courierDraft, origin: options[(index + (forward ? 1 : options.length - 1)) % options.length] }
    } else if (courierDraft.focus === 2) {
      const options: CourierDraft['calling'][] = ['trailguard', 'pathmaker', 'spiritbearer']
      const index = options.indexOf(courierDraft.calling)
      courierDraft = { ...courierDraft, calling: options[(index + (forward ? 1 : options.length - 1)) % options.length] }
    } else if (courierDraft.focus === 3) courierDraft = { ...courierDraft, deathMode: courierDraft.deathMode === 'checkpoint' ? 'ironTrail' : 'checkpoint' }
    else if (courierDraft.focus === 4) courierDraft = { ...courierDraft, companionControlMode: courierDraft.companionControlMode === 'autonomous' ? 'direct' : 'autonomous' }
    else courierDraft = { ...courierDraft, companionDeathMode: courierDraft.companionDeathMode === 'injury' ? 'permadeath' : 'injury', companionDeathConfirmed: false }
    redraw(); return
  }
  if (courierDraft.focus === 0) {
    if (key === 'Backspace' || key === 'Delete') courierDraft = { ...courierDraft, name: courierDraft.name.slice(0, -1) }
    else if (/^[a-zA-Z0-9 ]$/.test(key) && courierDraft.name.length < 20) courierDraft = { ...courierDraft, name: `${courierDraft.name}${key}` }
  }
  redraw()
}

function createCourierFromDraft(): void {
  if (!courierDraft || courierCreationPending) return
  const name = courierDraft.name.trim()
  if (!name || name.toLowerCase() === 'unnamed courier') { redraw(); return }
  const draft = { ...courierDraft, name }
  const startedAt = performance.now()
  courierCreationPending = true
  courierDraft = undefined
  route = { screen: 'loading', biome: campaign.selectedBiome }
  loading = { kind: 'trailhead', phase: 'loading', startedAt }
  audio.play([event('menu')])
  redraw()
  window.requestAnimationFrame(() => window.requestAnimationFrame(() => {
    const id = crypto.randomUUID()
    const identity = { id, name: draft.name, origin: draft.origin, calling: draft.calling, deathMode: draft.deathMode, companionControlMode: draft.companionControlMode, companionDeathMode: draft.companionDeathMode, createdAt: new Date().toISOString(), ...(successorParentId ? { parentId: successorParentId } : {}) }
    const seed = acceptedCampaignSeed(Math.floor(Math.random() * 0x7fffffff))
    const courier: CourierSave = { version: 1, identity, heir: newHero(identity), campaign: structuredClone(inheritedCampaign ?? initialCampaignRoute(seed, draft.companionControlMode)), records: { bestDepth: 0, wins: 0, deaths: 0, runs: [], analyses: [] } }
    const nextHeir = structuredClone(courier.heir)
    couriers = [...couriers, courier]
    activeCourier = courier
    selectedCourierId = id
    records = courier.records
    campaign = courier.campaign
    heir = nextHeir
    galaxyForVoyager(seed)
    courier.campaign = campaign
    inheritedCampaign = undefined
    successorParentId = undefined
    persistCourier(courier, id)
    const loadingStartedAt = performance.now()
    loading = { kind: 'trailhead', phase: 'loading', startedAt: loadingStartedAt }
    redraw()
    window.setTimeout(() => {
      if (!courierCreationPending) return
      courierCreationPending = false
      loading = undefined
      beginTrailhead(seed, openingLore(seed, courier.identity.name), nextHeir)
      redraw()
    }, 1800)
  }))
}

function resumeCourier(): void {
  if (!activeCourier) return
  records = activeCourier.records
  campaign = activeCourier.campaign
  hub = { ...createHubState(activeCourier.run?.seed ?? 0), unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
  if (activeCourier.run) { state = structuredClone(activeCourier.run); saved = structuredClone(activeCourier.run); heir = structuredClone(state.hero); galaxyForVoyager(state.seed); route = { screen: 'level', biome: campaign.selectedBiome }; recordedEnd = false; resetAutoplaySession() }
  else { state = undefined; saved = undefined; heir = activeCourier.heir ? structuredClone(activeCourier.heir) : newHero(activeCourier.identity); galaxyForVoyager(campaign.galaxy?.seed ?? 1); hubPosition = outpostSpawn(); route = { screen: 'hub', biome: campaign.selectedBiome } }
  const selectedId = activeCourier.identity.id
  recordPersistence('selection', () => selectCourier(selectedId))
}

function persistActiveCourier(restoreCheckpoint = false): void {
  if (!activeCourier) return
  if (state?.status === 'playing' && state.alignment) campaign = { ...campaign, alignment: { ...state.alignment } }
  if (state?.status === 'playing' && state.reputation) campaign = { ...campaign, reputation: { ...state.reputation } }
  if (state?.status === 'playing') campaign = { ...campaign, companions: cloneCompanions(state.companions ?? campaign.companions, campaign.rescuedNpcs) }
  synchronizeActiveGalaxyCourier()
  activeCourier.run = state && state.status === 'playing' ? structuredClone(state) : restoreCheckpoint && activeCourier.checkpoint ? structuredClone(activeCourier.checkpoint) : undefined
  if (state?.status === 'playing') activeCourier.heir = structuredClone(state.hero)
  activeCourier.campaign = campaign
  activeCourier.records = records
  saved = activeCourier.run ? structuredClone(activeCourier.run) : undefined
  couriers = couriers.map(courier => courier.identity.id === activeCourier!.identity.id ? activeCourier! : courier)
  persistCourier(activeCourier, selectedCourierId)
}

function synchronizeActiveGalaxyCourier(): void {
  const galaxy = campaign.galaxy
  const currentHero = state?.status === 'playing' ? state.hero : heir
  if (!galaxy || !currentHero) return
  const current = galaxy.couriers.find(courier => courier.id === galaxy.activeCourierId)
  if (!current) return
  campaign = {
    ...campaign,
    galaxy: {
      ...galaxy,
      couriers: galaxy.couriers.map(courier => courier.id === current.id ? { ...courier, name: currentHero.name, origin: currentHero.origin, calling: currentHero.calling, hero: structuredClone(currentHero), personalItems: [...courier.personalItems] } : courier)
    }
  }
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
  const galaxy = galaxyForVoyager(route.heirSeed ?? state?.seed ?? campaign.galaxy?.seed ?? 1)
  const siteId = route.siteId ?? galaxy?.activeSiteId
  const site = galaxy && siteId ? galaxy.sites[siteId] : undefined
  if (galaxy && (!site || !site.discovered)) { hubNotice = 'That landing approach has not been physically surveyed.'; route = { ...route, screen: 'hub' }; return }
  if (campaign.cycle.completedCap) { hubNotice = 'NG++ is complete. No further escalation is available.'; route = { ...route, screen: 'hub' }; return }
  if (campaignContinuationPending(campaign.cycle)) { hubNotice = 'Confirm the next campaign tier at the route board first.'; route = { ...route, screen: 'hub' }; return }
  const biome = site?.biome ?? route.biome
  campaign = { ...campaign, selectedBiome: biome, ...(galaxy && siteId ? { galaxy: setActiveGalaxySite(galaxy, siteId) } : {}) }
  hubNotice = undefined
  const snapshot = campaign.galaxy && siteId ? galaxySnapshot(campaign.galaxy, siteId) : undefined
  state = snapshot ?? newRun(route.heirSeed, biome, 0, heir, campaign.rescuedNpcs, campaign.legacyRecords, campaign.areaOrder, campaign.cycle, campaign.companions, activeCourier.identity.companionDeathMode)
  if (snapshot && heir) {
    state.hero = structuredClone(heir)
    state.hero.x = state.floor.start.x
    state.hero.y = state.floor.start.y
    state.status = 'playing'
    state.modal = undefined
    state.messages.unshift(`Returning to the evolving ${site?.name ?? biome} landing.`)
  }
  state.area = biome
  state.alignment = { ...campaign.alignment }
  state.reputation = { trailfolk: campaign.reputation?.trailfolk ?? 0, kami: campaign.reputation?.kami ?? 0 }
  if (!snapshot && site) applyGalaxySiteConditions(state, site)
  if (site && galaxy) attachGalaxyAirlocks(state, galaxy, site.id)
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

function attachGalaxyAirlocks(game: RunState, galaxy: GalaxyState, siteId: string): void {
  const site = galaxy.sites[siteId]
  if (!site || game.floor.airlocks?.length) return
  const destinations = site.links.filter(id => galaxy.sites[id]?.discovered)
  const candidates = game.floor.tiles.flatMap((tile, index) => tile.kind === 'floor' && Math.max(Math.abs(index % game.floor.width - game.floor.start.x), Math.abs(Math.floor(index / game.floor.width) - game.floor.start.y)) <= 8 ? [{ x: index % game.floor.width, y: Math.floor(index / game.floor.width) }] : [])
  const targets = [...destinations, 'voyager']
  game.floor.airlocks = targets.flatMap((destination, index) => {
    const point = candidates[index * 2 + 1]
    if (!point) return []
    game.floor.tiles[point.y * game.floor.width + point.x]!.kind = 'airlock'
    return [{ id: `airlock:${siteId}:${destination}`, x: point.x, y: point.y, ...(destination === 'voyager' ? {} : { destinationSiteId: destination }), label: destination === 'voyager' ? 'Jomon Voyager return airlock' : `Route airlock to ${galaxy.sites[destination]!.name}` }]
  })
}

function galaxyForVoyager(seed: number): GalaxyState | undefined {
  if (!heir) return campaign.galaxy ? reconcileGalaxy(campaign.galaxy) : undefined
  const galaxy = reconcileGalaxy(campaign.galaxy ?? createGalaxy(seed, heir))
  campaign = { ...campaign, galaxy }
  return galaxy
}

function beginTrailhead(seed: number, scene: ReturnType<typeof openingLore> | ReturnType<typeof successionLore>, nextHero?: Hero): void {
  route = { screen: 'approach', biome: campaign.selectedBiome, heirSeed: seed }
  heir = nextHero
  hub = { ...createHubState(seed), unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
  hubPosition = outpostSpawn()
  story = createStory(scene, performance.now())
  storyExit = 'hub'
}

function beginLoadingTransition(nextRoute: ScreenRoute, transition: Omit<LoadingState, 'phase' | 'startedAt'>, onComplete: () => void): void {
  route = nextRoute
  loading = { ...transition, phase: 'fade', startedAt: performance.now() }
  redraw()
  window.setTimeout(() => {
    if (loading?.phase !== 'fade') return
    loading = { ...transition, phase: 'loading', startedAt: performance.now() }
    redraw()
    window.setTimeout(() => {
      if (loading?.phase !== 'loading') return
      loading = undefined
      onComplete()
      redraw()
    }, 1400)
  }, 350)
}

function beginTrailheadAfterLoading(seed: number, scene: ReturnType<typeof openingLore> | ReturnType<typeof successionLore>, nextHero?: Hero): void {
  beginLoadingTransition({ screen: 'loading', biome: campaign.selectedBiome, heirSeed: seed }, { kind: 'trailhead' }, () => beginTrailhead(seed, scene, nextHero))
}

function beginVoyagerTransit(fromBiome: ScreenRoute['biome'], toBiome: ScreenRoute['biome'] | undefined, onComplete: () => void): void {
  route = { ...route, screen: 'transit', biome: toBiome ?? fromBiome }
  transit = { fromBiome, toBiome, startedAt: performance.now() }
  const complete = () => {
    if (finishTransit !== complete) return
    finishTransit = undefined
    transit = undefined
    onComplete()
    redraw()
  }
  finishTransit = complete
  window.setTimeout(complete, 3400)
  redraw()
}

function acceptedCampaignSeed(requestedSeed: number): number {
  const validation = findStructurallyPlayableCampaignSeed(requestedSeed)
  if (!validation.accepted) throw new Error(`no playable campaign seed after ${validation.errors.at(-1) ?? 'validation failure'}`)
  return validation.seed
}

function handleStoryInput(keyboardEvent: KeyboardEvent): void {
  if (!story || keyboardEvent.repeat) return
  if (keyboardEvent.key === 'Escape') {
    if (storyExit === 'analysis') finishStory()
    else {
      story = undefined
      route = { ...route, screen: 'title', heirSeed: undefined }
      audio.play([event('menu')])
      redraw()
    }
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
  const exit = storyExit
  story = undefined
  storyExit = 'hub'
  if (exit === 'analysis') {
    route = { ...route, screen: 'analysis' }
    audio.play([event('menu')])
    redraw()
    return
  }
  if (createAfterStory) {
    createAfterStory = false
    courierDraft = { name: '', origin: 'mineborn', calling: 'trailguard', deathMode: 'checkpoint', companionControlMode: 'autonomous', companionDeathMode: 'injury', companionDeathConfirmed: false, focus: 0 }
    route = { ...route, screen: 'createCourier' }
  } else { hubPosition = outpostSpawn(); route = { ...route, screen: 'hub', hubAction: undefined } }
  audio.play([event('menu')])
  redraw()
}

function beginSuccession(): void {
  if (!pendingSuccessor || loading) return
  story = undefined
  const successor = pendingSuccessor
  pendingSuccessor = undefined
  createAfterStory = true
  const seed = acceptedCampaignSeed(successor.seed)
  beginTrailheadAfterLoading(seed, successionLore(successor.record, seed))
}

function completeArea(): 'finished' | 'returned' | 'transitioning' {
  if (!state) return 'returned'
  const completedState = state
  const completed = completedState.area ?? completedState.floor.biome
  heir = structuredClone(completedState.hero)
  campaign = { ...campaign, companions: cloneCompanions(completedState.companions ?? campaign.companions, campaign.rescuedNpcs) }
  synchronizeActiveGalaxyCourier()
  const galaxy = galaxyForVoyager(completedState.seed)
  const siteId = route.siteId ?? galaxy?.activeSiteId
  if (galaxy && siteId) {
    const landed = recordGalaxyLanding(galaxy, siteId, completedState)
    completedState.messages.unshift(`Landing yield secured: ${completedState.hero.gold} credits carried by ${activeCourier?.identity.name ?? completedState.hero.name}.`)
    const savedSite = saveGalaxySite(landed, siteId, completedState)
    campaign = { ...campaign, galaxy: discoverLinkedSites(savedSite, siteId) }
    hub = { ...hub, rescued: campaign.rescuedNpcs }
    beginVoyagerTransit(completed, undefined, () => {
      state = undefined
      saved = undefined
      hubPosition = outpostSpawn()
      route = { screen: 'hub', biome: completed }
      persistActiveCourier()
    })
    return 'transitioning'
  }
  campaign = completeCampaignArea(campaign, completed)
  hub = { ...hub, unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
  const successor = settings.autoplayMode === 'off' ? undefined : nextArea(completed, campaign.areaOrder)
  if (successor) {
    campaign = unlockCampaignArea(campaign, successor)
    hub = { ...hub, unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
    beginVoyagerTransit(completed, successor, () => {
      const next = newRun(completedState.seed, successor, 0, heir, campaign.rescuedNpcs, campaign.legacyRecords, campaign.areaOrder, campaign.cycle, campaign.companions, completedState.companionDeathMode ?? activeCourier?.identity.companionDeathMode ?? 'injury')
      next.turn = completedState.turn
      next.lineageEvents = structuredClone(completedState.lineageEvents ?? [])
      next.telemetry = structuredClone(completedState.telemetry!)
      next.alignment = { ...(completedState.alignment ?? campaign.alignment) }
      next.reputation = { ...(completedState.reputation ?? campaign.reputation ?? { trailfolk: 0, kami: 0 }) }
      state = next
      saved = structuredClone(next)
      route = { ...route, screen: 'level', biome: successor }
      resetAutoplaySession()
      if (activeCourier) activeCourier.checkpoint = structuredClone(next)
      persistActiveCourier()
    })
    return 'transitioning'
  }
  beginVoyagerTransit(completed, undefined, () => {
    if (!state) return
    state.status = 'victory'
    finish(true)
  })
  return 'transitioning'
}

function beginConnectorTravel(galaxy: GalaxyState, originId: string, destinationId: string): void {
  const origin = galaxy.sites[originId]
  const destination = galaxy.sites[destinationId]
  if (!origin || !destination || !heir) return
  const savedOrigin = state ? saveGalaxySite(galaxy, originId, state) : galaxy
  const accepted = acceptGalaxyContract(savedOrigin, origin.id, destination.id)
  campaign = { ...campaign, galaxy: accepted.galaxy }
  const linkId = [origin.id, destination.id].sort().join('::')
  const chunkCount = galaxyRouteLength(accepted.galaxy, origin.id, destination.id)
  const situations = Array.from({ length: chunkCount }, (_, chunk) => galaxyRouteSituation(accepted.galaxy, origin.id, destination.id, chunk))
  const routeCacheChunks = accepted.galaxy.routeCaches.filter(cache => cache.linkId === linkId && !cache.recovered).map(cache => cache.chunk)
  const travel = newTransitRun(accepted.galaxy.seed, destination.biome, heir, { version: 1, fromSiteId: origin.id, toSiteId: destination.id, linkId, chunkCount, residentStart: 0, activeChunk: 0, situations, ...(routeCacheChunks.length ? { routeCacheChunks } : {}) }, campaign.rescuedNpcs, campaign.legacyRecords, campaign.areaOrder, campaign.cycle, campaign.companions, activeCourier?.identity.companionDeathMode ?? 'injury')
  if (routeCacheChunks.length) {
    travel.messages.unshift('A recoverable cargo cache is marked in this route window. Operate beside it to retrieve its contents.')
  }
  travel.messages.unshift(accepted.message)
  travel.alignment = { ...campaign.alignment }
  travel.reputation = { trailfolk: campaign.reputation?.trailfolk ?? 0, kami: campaign.reputation?.kami ?? 0 }
  state = travel
  saved = structuredClone(travel)
  route = { screen: 'level', biome: destination.biome, siteId: destination.id }
  renderer.setHeroFacingLeft(false)
  resetAutoplaySession()
  if (activeCourier) activeCourier.checkpoint = structuredClone(travel)
  persistActiveCourier()
  audio.play([event('floor')])
}

function completeConnector(): void {
  if (!state?.travel) return
  const destination = state.travel.toSiteId
  heir = structuredClone(state.hero)
  if (activeCourier) activeCourier.heir = structuredClone(heir)
  state = undefined
  saved = undefined
  route = { screen: 'level', biome: campaign.galaxy?.sites[destination]?.biome ?? route.biome, siteId: destination }
  start()
  const landed = state as RunState | undefined
  if (landed && campaign.galaxy) {
    const delivered = deliverGalaxyContracts(campaign.galaxy, destination, landed.hero)
    campaign = { ...campaign, galaxy: delivered.galaxy }
    if (delivered.message) landed.messages.unshift(delivered.message)
    persistActiveCourier()
  }
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
  campaign = { ...campaign, rescuedNpcs, companions: addCompanionLeads(campaign.companions, rescuedNpcs, campaign.companionControlMode) }
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
  const apply = (feature: AutoplayFeature) => {
    if (settings.autoplayMode !== 'off') finalizeAutoplay('manual', 'mode toggled off')
    settings = { ...settings, autoplayMode: feature.nextAutoplayMode(settings.autoplayMode) }
    if (settings.autoplayMode !== 'off') resetAutoplaySession()
    saveSettings(settings)
    if (state) state.messages.unshift(`Autoplay: ${feature.autoplayModeLabel(settings.autoplayMode)} · ${feature.autoplayPolicyLabel(settings.autoplayPolicy)}.`)
    audio.play([event('menu')])
    redraw()
  }
  if (autoplayFeature) { apply(autoplayFeature); return }
  if (state) state.messages.unshift('Loading autoplay systems…')
  redraw()
  void loadAutoplayFeature().then(apply).catch(() => {
    if (state) state.messages.unshift('Autoplay systems failed to load. Try again.')
    redraw()
  })
}

function toggleAutoplayPolicy(): void {
  const apply = (feature: AutoplayFeature) => {
    if (settings.autoplayMode !== 'off') finalizeAutoplay('manual', 'policy changed')
    settings = { ...settings, autoplayPolicy: feature.nextAutoplayPolicy(settings.autoplayPolicy) }
    resetAutoplaySession()
    saveSettings(settings)
    if (state) state.messages.unshift(`Autoplay policy: ${feature.autoplayPolicyLabel(settings.autoplayPolicy)}.`)
    audio.play([event('menu')])
    redraw()
  }
  if (autoplayFeature) { apply(autoplayFeature); return }
  if (state) state.messages.unshift('Loading autoplay systems…')
  redraw()
  void loadAutoplayFeature().then(apply).catch(() => {
    if (state) state.messages.unshift('Autoplay systems failed to load. Try again.')
    redraw()
  })
}

function resetAutoplaySession(): void {
  autoplayContext = autoplayFeature?.createAutoplayContext()
  autoplayTrace = []
  autoplayLogged = false
}

function finalizeAutoplay(outcome: AutoplayTerminal, reason: string): void {
  if (autoplayLogged || !state || settings.autoplayMode === 'off' || !autoplayTrace.length && outcome !== 'unsupported') return
  autoplayLogged = true
  autoplayDiagnostic = { id: `${state.seed}:${state.floor.seed}:${Date.now()}`, date: new Date().toISOString(), seed: state.seed, biome: state.area ?? state.floor.biome, floor: (state.areaFloor ?? state.floor.index % 4) + 1, mode: settings.autoplayMode, policy: settings.autoplayPolicy, outcome, turns: state.turn, reason, trace: structuredClone(autoplayTrace) }
  saveAutoplayDiagnostic(autoplayDiagnostic)
  renderer.setAutoplayDiagnostic(autoplayDiagnostic)
}

function canAutoplay(): boolean {
  if (settings.autoplayMode === 'off') return false
  return route.screen === 'level' && state?.status === 'playing' || route.screen === 'hub' && !route.hubAction || route.screen === 'area'
}

function syncAutoplay(): void {
  if (!canAutoplay()) {
    if (autoplayTimer !== undefined) window.clearTimeout(autoplayTimer)
    autoplayTimer = undefined
    return
  }
  const feature = autoplayFeature
  if (!feature) {
    void loadAutoplayFeature().then(() => {
      if (!canAutoplay()) return
      resetAutoplaySession()
      redraw()
    }).catch(() => {
      settings = { ...settings, autoplayMode: 'off' }
      saveSettings(settings)
      if (state) state.messages.unshift('Autoplay systems failed to load.')
      redraw()
    })
    return
  }
  if (autoplayTimer !== undefined) return
  autoplayTimer = window.setTimeout(() => {
    autoplayTimer = undefined
    if (!canAutoplay()) return
    if (route.screen === 'hub') {
      const command = outpostAutoplayCommand(hubPosition)
      if (!command) {
        settings = { ...settings, autoplayMode: 'off' }
        saveSettings(settings)
        hubNotice = 'Autoplay halted: route board is unreachable.'
        redraw()
        return
      }
      handleHubInput(command)
      audio.play([event('menu')])
      redraw()
      return
    }
    if (route.screen === 'area') {
      const nextRoute = navigate(route, 'Enter', Boolean(saved))
      if (nextRoute.screen !== 'level') return
      start()
      route = nextRoute
      audio.play([event('menu')])
      redraw()
      return
    }
    if (!state) return
    const context = autoplayContext ??= feature.createAutoplayContext()
    const decision = feature.autoplayDecision(state, settings.autoplayMode, settings.autoplayPolicy, context)
    if (!decision) {
      const reason = context.lastReason ?? 'no legal progress action'
      const unsupported = reason === 'unsupported direct companion control'
      finalizeAutoplay(unsupported ? 'unsupported' : 'stalled', reason)
      settings = { ...settings, autoplayMode: 'off' }
      saveSettings(settings)
      state.messages.unshift(unsupported ? 'Autoplay unavailable: direct companion control needs player commands.' : `Autoplay halted (${reason}); trace saved locally.`)
      redraw()
      return
    }
    executeGameplayCommand(decision.command, { autoplay: decision })
  }, feature.AUTOPLAY_TURN_MS)
}

function redraw(): void {
  canvas.dataset.route = route.screen
  canvas.dataset.status = state?.status ?? 'none'
  canvas.dataset.autoplay = settings.autoplayMode
  canvas.dataset.autoplayPolicy = settings.autoplayPolicy
  canvas.dataset.notice = hubNotice ?? ''
  const campaignStatus = hubCampaignStatus(campaign.cycle)
  const galaxy = campaign.galaxy
  canvas.setAttribute('aria-label', `Jomon Voyager living sector. ${galaxy ? `${Object.values(galaxy.sites).filter(site => site.discovered).length} physical landings charted; sector day ${galaxy.sectorDay.toFixed(1)}.` : campaignStatus.accessibleLabel}${heir ? ` ${hubCarryoverSummary(heir, campaign.companions).accessibleLabel}` : ''}`)
  renderer.render(route, state, records, hubView(heir?.name ?? activeCourier?.identity.name ?? 'Unassigned', hub, { hero: heir, biome: route.biome, notice: hubNotice, position: hubPosition, cycle: campaign.cycle, ...(heir ? { carryover: hubCarryoverSummary(heir, campaign.companions) } : {}), companions: campaign.companions, companionControlMode: campaign.companionControlMode, companionDeathMode: activeCourier?.identity.companionDeathMode, galaxy }), story, loading, analysis, courierMenu(), courierDraft, settings.autoplayMode, transit)
  syncAutoplay()
}

function handleHubInput(key: string, run = false): boolean {
  const action = route.hubAction
  if (action) {
    if (action === 'crew') {
      if (key === 'Escape' || key.toLowerCase() === 'c' || key === 'Enter') { route = { ...route, hubAction: undefined }; return true }
      const index = Number(key) - 1
      const galaxy = galaxyForVoyager(campaign.galaxy?.seed ?? 1)
      const courier = galaxy?.couriers[index]
      if (!galaxy || !courier) { hubNotice = 'Choose a listed Voyager specialist (1-5).'; return true }
      const selected = selectGalaxyCourier(galaxy, courier.id)
      if (!selected.hero) { hubNotice = `${courier.name} is unavailable (${courier.status}).`; return true }
      campaign = { ...campaign, galaxy: selected.galaxy }
      heir = selected.hero
      if (activeCourier) activeCourier.heir = structuredClone(heir)
      hubNotice = `${heir.name} takes the landing watch. Their previous duty resumes autonomously.`
      persistActiveCourier()
      return true
    }
    if (action === 'continuation') {
      if (key === 'Escape' || key.toLowerCase() === 'c') { route = { ...route, hubAction: undefined }; return true }
      if (key === 'Enter' || key.toLowerCase() === 'e') {
        if (!heir || !activeCourier) { hubNotice = 'Courier carryover is unavailable.'; return true }
        const before = campaign.cycle.currentTier
        const snapshot = snapshotCampaignCarryover(heir, campaign, records)
        const advanced = continueCampaignRoute(campaign)
        if (advanced.cycle.currentTier === before) { hubNotice = 'No campaign continuation is pending.'; route = { ...route, hubAction: undefined }; return true }
        const transfer = transferCampaignCarryover(advanced, snapshot)
        campaign = transfer.campaign
        heir = transfer.hero
        records = transfer.records
        activeCourier.heir = structuredClone(heir)
        hub = { ...hub, unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
        hubNotice = `${campaign.cycle.currentTier === 'ngPlus' ? 'NG+' : 'NG++'} revised route recorded. Press E / ENTER to launch.`
        route = { screen: 'area', biome: campaign.selectedBiome }
        persistActiveCourier()
        return true
      }
      hubNotice = 'ENTER / E accepts the revised route. C / ESC docks at New Edo.'
      return true
    }
    if (action === 'roster') {
      if (key === 'Escape' || key.toLowerCase() === 'c') { route = { ...route, hubAction: undefined, companionAction: undefined, companionControlMode: undefined }; return true }
      const pendingControlMode = route.companionControlMode
      if (pendingControlMode) {
        if (key !== 'Enter') { hubNotice = 'ENTER confirms. C / ESC cancels.'; return true }
        const result = changeCampaignCompanionControlMode(campaign, pendingControlMode, 'lodge')
        campaign = result.state
        hubNotice = result.message
        route = { ...route, companionControlMode: undefined }
        if (result.changed) persistActiveCourier()
        return true
      }
      const pending = route.companionAction
      if (pending) {
        if (key !== 'Enter') { hubNotice = 'ENTER confirms. C / ESC cancels.'; return true }
        if ((pending.action === 'beginRecovery' || pending.action === 'completeRecovery') && !heir) { hubNotice = 'Courier funds are unavailable.'; return true }
        const result = pending.action === 'beginRecovery'
          ? beginCompanionRecovery(campaign.companions, campaign.rescuedNpcs, pending.id, heir!.gold)
          : pending.action === 'completeRecovery'
            ? completeCompanionRecovery(campaign.companions, campaign.rescuedNpcs, pending.id)
            : changeCompanionRoster(campaign.companions, campaign.rescuedNpcs, pending.id, pending.action)
        campaign = { ...campaign, companions: result.companions }
        if (result.changed && result.cashSpent && heir) heir.gold -= result.cashSpent
        hubNotice = result.message
        route = { ...route, companionAction: undefined }
        if (result.changed) persistActiveCourier()
        return true
      }
      if (key === 'Enter') { route = { ...route, hubAction: undefined }; return true }
      if (key === '0') { route = { ...route, companionControlMode: campaign.companionControlMode === 'autonomous' ? 'direct' : 'autonomous' }; hubNotice = undefined; return true }
      const choice = Number(key) - 1
      const companion = campaign.companions[choice]
      if (!Number.isInteger(choice) || !companion || choice > 4) { hubNotice = 'Choose a listed companion (1-5).'; return true }
      const next = companionLodgeAction(companion)
      if (!next) { hubNotice = `${companion.name} is unavailable (${companion.permanentlyLost ? 'permanently lost' : companion.injury === 'recovering' ? `${companion.recoveryFloors ?? 0} cleared floor remaining` : companion.injury}).`; return true }
      route = { ...route, companionAction: { id: companion.id, action: next } }
      hubNotice = undefined
      return true
    }
    if (key === 'Escape' || key.toLowerCase() === 'c' || key === 'Enter') { route = { ...route, hubAction: undefined }; return true }
    const choice = Number(key) - 1
    if (!heir || !activeCourier) { hubNotice = 'Courier record is unavailable.'; return true }
    if (!Number.isInteger(choice) || choice < 0 || choice > 5) { hubNotice = 'Choose a listed option (1-6).'; return true }
    const result = action === 'shop'
      ? buyHubItem(heir, hubStock(route.biome)[choice] ?? '')
      : action === 'outfitter'
        ? equipHubItem(heir, hubEquipment(heir)[choice] ?? '')
        : undefined
    if (!result) { hubNotice = 'That service cannot complete this action.'; return true }
    hubNotice = result.message
    if (result.changed) { activeCourier.heir = structuredClone(heir); persistActiveCourier() }
    return true
  }
  if (key.toLowerCase() === 'c' || key === 'Enter') {
    const interaction = outpostInteraction(hubPosition)
    if (!interaction) { hubNotice = 'No service is within reach.'; return true }
    hubNotice = undefined
    if (interaction.destination === 'routes') {
      const galaxy = galaxyForVoyager(state?.seed ?? campaign.galaxy?.seed ?? 1)
      const site = galaxy?.sites[galaxy.activeSiteId]
      if (!galaxy || !site) hubNotice = 'The flight console cannot recover a galaxy route.'
      else route = { screen: 'sector', biome: site.biome, siteId: site.id }
    }
    else route = { ...route, hubAction: interaction.destination }
    return true
  }
  const direction = directionFor(key)
  if (!direction) return false
  const previous = hubPosition
  let moved = false
  for (let step = 0; step < (run ? 5 : 1); step++) {
    const move = moveOutpost(hubPosition, direction)
    if (!move.moved) break
    hubPosition = move.position
    moved = true
  }
  if (moved) {
    renderer.setHeroFacingLeft(hubPosition.x < previous.x)
    renderer.setHubMoved()
    hubNotice = undefined
  } else if (direction !== 'wait') hubNotice = 'The way is blocked.'
  return true
}

function handleSectorInput(key: string): boolean {
  const galaxy = galaxyForVoyager(state?.seed ?? campaign.galaxy?.seed ?? 1)
  if (!galaxy) { route = { ...route, screen: 'hub' }; return true }
  if (key === 'Escape' || key.toLowerCase() === 'c') { route = { ...route, screen: 'hub', siteId: undefined }; return true }
  const origin = galaxy.sites[galaxy.activeSiteId]
  const sites = availableGalaxySites(galaxy).filter(site => site.id === origin?.id || Boolean(origin?.links.includes(site.id)))
  if (!sites.length) return true
  const current = Math.max(0, sites.findIndex(site => site.id === (route.siteId ?? galaxy.activeSiteId)))
  if (key === 'ArrowUp' || key === 'ArrowLeft' || key === 'ArrowDown' || key === 'ArrowRight') {
    const delta = key === 'ArrowUp' || key === 'ArrowLeft' ? -1 : 1
    const next = sites[(current + delta + sites.length) % sites.length]!
    route = { ...route, biome: next.biome, siteId: next.id }
    return true
  }
  if (key === 'Enter' || key.toLowerCase() === 'e') {
    const selected = sites[current]!
    if (!origin) return true
    const launch = () => { route = { ...route, screen: 'level', biome: selected.biome, siteId: selected.id }; start() }
    route = { ...route, biome: selected.biome, siteId: selected.id }
    if (selected.id === origin.id) launch()
    return true
  }
  return true
}

function finish(won: boolean): void {
  if (!state) return
  if (state.alignment) campaign = { ...campaign, alignment: { ...state.alignment } }
  if (state.reputation) campaign = { ...campaign, reputation: { ...state.reputation } }
  if (won) {
    heir = structuredClone(state.hero)
    if (activeCourier) activeCourier.heir = structuredClone(heir)
    if (!campaign.cycle.completedTiers.includes(campaign.cycle.currentTier)) campaign = { ...campaign, cycle: completeCampaignTier(campaign.cycle) }
    state.campaignCycle = structuredClone(campaign.cycle)
    state.messages.unshift(`Campaign tier complete. ${hubCampaignStatus(campaign.cycle).nextLabel}`)
  }
  finalizeAutoplay(won ? 'complete' : 'dead', won ? 'campaign complete' : 'courier defeated')
  if (settings.autoplayMode !== 'off') {
    settings = { ...settings, autoplayMode: 'off' }
    saveSettings(settings)
  }
  recordedEnd = true
  const checkpointDeath = !won && state.hero.deathMode === 'checkpoint'
  if (!won && !checkpointDeath) {
    campaign = recordDeath(campaign, state, state.hero.name)
    if (campaign.galaxy) {
      const abandoned = state.travel ? abandonGalaxyCargo(campaign.galaxy, state.travel.linkId, Math.max(0, Math.floor(state.hero.x / Math.max(1, state.floor.width / state.travel.chunkCount)))) : campaign.galaxy
      campaign = { ...campaign, galaxy: loseGalaxyCourier(abandoned, abandoned.activeCourierId, `${state.hero.name} fell during a landing on ${biomeName[state.area ?? state.floor.biome]}.`) }
      hubNotice = `${state.hero.name}'s death is recorded. Another Voyager specialist can continue the sector.`
    } else {
      const record = campaign.legacyRecords.at(-1)
      if (!record) throw new Error('missing death legacy record')
      pendingSuccessor = { record, seed: Math.floor(Math.random() * 0x7fffffff) }
      inheritedCampaign = structuredClone(campaign)
      successorParentId = activeCourier?.identity.id
    }
  }
  records.bestDepth = Math.max(records.bestDepth, state.floor.index + 1)
  if (won) records.wins++
  else records.deaths++
  records.runs.unshift({ seed: state.seed, floor: state.floor.index + 1, score: state.hero.gold, won, date: new Date().toISOString() })
  records.runs = records.runs.slice(0, 20)
  analysis = analysisFor(state, won ? 'complete' : 'lost')
  records.analyses.unshift(analysis)
  records.analyses = records.analyses.slice(0, 20)
  analysisNext = won || Boolean(campaign.galaxy) ? 'victory' : checkpointDeath ? 'checkpoint' : 'succession'
  if (checkpointDeath && activeCourier?.checkpoint) saved = structuredClone(activeCourier.checkpoint)
  else saved = undefined
  if (!won && !checkpointDeath && activeCourier) { activeCourier.run = undefined; activeCourier.archived = !campaign.galaxy }
  if (won) {
    story = createStory(endingLore(state, campaign.completedAreas, campaign.alignment), performance.now())
    storyExit = 'analysis'
    route = { ...route, screen: 'approach' }
  } else route = { ...route, screen: 'analysis' }
  persistActiveCourier(checkpointDeath)
}

function run(game: RunState, command: string): ReturnType<typeof perform> {
  const events = [] as ReturnType<typeof perform>
  for (let i = 0; i < 18; i++) {
    const x = game.hero.x
    const y = game.hero.y
    const next = performTracked(game, command)
    events.push(...next)
    const threats = game.floor.actors.some(actor => actor.hostile && Math.max(Math.abs(actor.x - game.hero.x), Math.abs(actor.y - game.hero.y)) <= 7 && getTile(game.floor, actor.x, actor.y)?.visible)
    if (game.status !== 'playing' || game.modal || (x === game.hero.x && y === game.hero.y) || threats) break
  }
  return events
}

type GameplayCommandOptions = { quickCast?: boolean; run?: boolean; spellEffect?: string; autoplay?: AutoplayDecision }

function executeGameplayCommand(command: string, options: GameplayCommandOptions = {}): void {
  if (!state || route.screen !== 'level' || state.status !== 'playing') return
  const game = state
  const autoplayBefore = options.autoplay ? structuredClone(game) : undefined
  const autoplayFingerprint = options.autoplay && autoplayFeature ? autoplayFeature.autoplayTraceFingerprint(game) : undefined
  const previousX = game.hero.x
  const previousY = game.hero.y
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
  const rebased = advanceTransitWindow(game)
  if (rebased) renderer.shiftCameraWindow(game.hero.x - previousX, game.hero.y - previousY)
  if (game.hero.level > previousLevel) events.push(event('level'))
  if (game.hero.x !== previousX) renderer.setHeroFacingLeft(game.hero.x < previousX)
  if (!rebased && (game.hero.x !== previousX || game.hero.y !== previousY)) renderer.recenterCamera()
  if (options.autoplay && autoplayBefore && autoplayFingerprint && autoplayFeature && autoplayContext) {
    autoplayFeature.recordAutoplayTransition(autoplayContext, autoplayBefore, command, game)
    autoplayTrace.push({
      turn: autoplayBefore.turn,
      replay: autoplayFeature.autoplayReplayMetadata(autoplayBefore),
      fingerprint: autoplayFingerprint,
      command,
      reason: options.autoplay.reason,
      candidates: options.autoplay.candidates,
      events: events.map(entry => entry.type),
      nextFingerprint: autoplayFeature.autoplayTraceFingerprint(game),
      before: { x: autoplayBefore.hero.x, y: autoplayBefore.hero.y, health: autoplayBefore.hero.health, focus: autoplayBefore.hero.focus, bombs: autoplayBefore.hero.bombs, ropes: autoplayBefore.hero.ropes, objective: autoplayBefore.floor.objective.status },
      after: { x: game.hero.x, y: game.hero.y, health: game.hero.health, focus: game.hero.focus, bombs: game.hero.bombs, ropes: game.hero.ropes, objective: game.floor.objective.status, ...(game.modal ? { modal: game.modal.kind } : {}) }
    })
    if (autoplayTrace.length > 600) autoplayTrace = autoplayTrace.slice(-600)
    if (hasEvent(events, 'death') || game.status === 'dead') finalizeAutoplay('dead', 'courier defeated')
  }
  audio.play(events)
  renderer.trigger(events, game, options.spellEffect)
  if (hasEvent(events, 'suspend')) { suspendRun(); return }
  if (hasEvent(events, 'floor')) {
    const galaxy = campaign.galaxy
    const siteId = route.siteId ?? galaxy?.activeSiteId
    if (galaxy && siteId) attachGalaxyAirlocks(game, galaxy, siteId)
    saved = structuredClone(game); checkpointActiveCourier()
  }
  if (hasEvent(events, 'rescue')) persistRescuedRoster()
  if (hasEvent(events, 'connectorComplete')) { completeConnector(); redraw(); return }
  const cache = events.find(entry => entry.type === 'routeCache')
  if (cache?.id && campaign.galaxy) {
    const recovered = recoverGalaxyRouteCaches(campaign.galaxy, cache.id)
    campaign = { ...campaign, galaxy: recovered.galaxy }
    game.floor.routeCache = undefined
    if (game.travel) game.travel = { ...game.travel, routeCacheChunks: [] }
    game.messages.unshift(recovered.message)
  }
  const departure = events.find(entry => entry.type === 'routeDeparture')
  if (departure) {
    const galaxy = campaign.galaxy
    const origin = galaxy?.activeSiteId
    if (departure.id === 'voyager') {
      state = undefined; saved = undefined; hubPosition = outpostSpawn(); route = { screen: 'hub', biome: game.floor.biome }; persistActiveCourier(); redraw(); return
    }
    if (galaxy && origin && departure.id && galaxy.sites[departure.id]) { beginConnectorTravel(galaxy, origin, departure.id); redraw(); return }
  }
  const areaResult = hasEvent(events, 'areaComplete') ? completeArea() : undefined
  if (hasEvent(events, 'gateResolved')) unlockGateDestination()
  if (areaResult === 'transitioning') { redraw(); return }
  if (areaResult === 'finished' && state) {
    state.status = 'victory'
    finish(true)
    redraw()
    return
  }
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
  if (next === 'victory') {
    state = undefined
    saved = undefined
    hub = { ...hub, unlockedAreas: campaign.unlockedAreas, completedAreas: campaign.completedAreas, rescued: campaign.rescuedNpcs }
    hubPosition = outpostSpawn()
    route = { screen: 'hub', biome: campaign.selectedBiome }
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
