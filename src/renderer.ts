import { ITEM, biomeName } from './content'
import { autoplayModeLabel, autoplayPolicyLabel } from './autoplay'
import jomonMastheadSource from '../asset/reference/JOMON.md?raw'
import { merchantStock } from './engine/rewards'
import { assessCompanionAbility, augmentChoices, boonChoices, boonFor, boonRank, encounterOptions, encounterTitle, encyclopediaEntries, fieldReadout, gateForRun, gateModalLines, gateSacrificeCandidates, gateSacrificeConsequence, outpostInteraction, outpostMap, outpostSpawn, partyHud, relicChoices, relicFor, skillChoices, targetPreview, toolChoices, toolCooldown, toolFor, trailcraftChoices, type ActionResult, type HubView, type ScreenRoute } from './engine'
import { TerminalEffects } from './renderer/effects'
import { isItemVisible } from './renderer/fog'
import { mapCellIndex, mapOverlays, visibleMapActor, type MapOverlays } from './renderer/map-overlays'
import { CELL_HEIGHT as CH, CELL_WIDTH as CW, MAP_HEIGHT, MAP_WIDTH, cameraFrame, cellRect } from './renderer/metrics'
import { telegraphBeam } from './renderer/telegraph-overlay'
import { isTelegraphVisible, presentTelegraph } from './renderer/telegraphs'
import { flowGlyph, showMotionAt, terrainInspection, terminalGlyph, terminalTileGlyph, terrainVisual, visualIdentitySnapshot, type VisualIdentityState } from './renderer/visual-grammar'
import { isStoryPageComplete, storyText, type LoadingState, type StoryState, type TransitState } from './lore'
import { LORE_CODEX_PAGES } from './lore-codex'
import { defaultSettings, settingChoices, settingsPageCount, type GameSettings } from './settings'
import { mineSeason, seasonLabel } from './season'
import { drawActorSprite, drawEffectSprite, drawItemSprite, drawPropSprite, drawTileSprite, textureAtlas, type HeroAnimation } from './sprites'
import { propDefinition } from './props'
import { rewardOfferFor } from './reward-contract'
import { SLOT_NAMES, TERMINAL_HEIGHT, TERMINAL_WIDTH, type AutoplayDiagnostic, type AutoplayMode, type Biome, type CourierDraft, type CourierMenuView, type GroundItem, type Hero, type Modal, type Point, type RunAnalysis, type RunMetricSample, type RunState, type Tile } from './types'
import { visualModeLabel, type VisualMode } from './visual-mode'
import { getTile } from './world'

const colors = { back: '#10131d', panel: '#182131', border: '#6f8298', text: '#d6dce8', dim: '#536174', gold: '#f4d26a', red: '#ee6f78', green: '#96d38b', blue: '#8fb8ed', purple: '#d2a4e8', ink: '#05070b' }
const shade = (color: string, amount = .58): string => {
  const match = /^#([0-9a-f]{6})$/i.exec(color)
  if (!match) return colors.dim
  const value = Number.parseInt(match[1], 16)
  const channel = (shift: number) => Math.round(((value >> shift) & 0xff) * (1 - amount)).toString(16).padStart(2, '0')
  return `#${channel(16)}${channel(8)}${channel(0)}`
}
const tileGlyph: Record<string, [string, string]> = {
  wall: ['#', '#7d8792'], floor: ['.', '#586470'], exit: ['>', '#f4d26a'], door: ['+', '#c99f67'], lockedDoor: ['+', '#e9c965'], water: ['~', '#5c9fca'], lava: ['~', '#ec7056'], pit: [' ', '#05070b'], rope: ['|', '#d8ae73'], spikes: ['^', '#d9dce1'], dart: ['>', '#d9dce1'], fireVent: ['^', '#ff855d'], crumble: [',', '#9e856f'], boulder: ['O', '#a7a0a0'], web: ['%', '#d8dce1'], gas: ['*', '#9bc585'], support: ['╫', '#b99b72'], rail: ['=', '#c5b2a0'], rubble: [':', '#8e9298'], bramble: ['"', '#6c9f64'], darkness: ['·', '#30384d'], crate: ['□', '#c69a6b'], chest: ['▣', '#f4d26a'], altar: ['_', '#d2a4e8'], shop: ['$', '#f4d26a'], rescue: ['&', '#8ae0b3'], smoke: ['≈', '#9ca1ad'], lift: ['↕', '#e9c47e'], breakwall: ['#', '#bc8266'], current: ['≋', '#83d1d7'], deepWater: ['≈', '#215b72'], anchor: ['⚓', '#76b7c2'], cliffWall: ['▲', '#71809d'], ledge: ['=', '#c2d4dc'], graveSoil: [';', '#95836f'], cairn: ['▲', '#b9aa94'], ossuary: ['□', '#d9d3c5'], spiritPath: ['·', '#c9a6db'], saltMirror: ['◇', '#f4e7a5'], brine: ['≈', '#63aab5'], ice: ['=', '#bfe8f3'], frostRime: ['*', '#d8f3ff']
}
const runeTileGlyph: Record<string, [string, string, string]> = {
  wall: ['▓', '#79879b', '#131925'], floor: ['·', '#4a586b', '#080b12'], exit: ['>', '#f4d26a', '#15130c'], door: ['+', '#d1a66e', '#16110d'], lockedDoor: ['#', '#e9c965', '#17130b'], water: ['~', '#72b7d2', '#0a1621'], lava: ['~', '#f27a60', '#1c0d0b'], pit: [' ', '#202b38', '#030407'], rope: ['║', '#d8ae73', '#17140d'], spikes: ['^', '#d9dce1', '#15181d'], dart: ['>', '#d9dce1', '#15181d'], fireVent: ['^', '#ff855d', '#1b0d0b'], crumble: [',', '#b89a77', '#15110e'], boulder: ['O', '#a7a0a0', '#15171b'], web: ['%', '#d8dce1', '#17181d'], gas: ['*', '#9bc585', '#10170f'], support: ['╫', '#b99b72', '#17130e'], rail: ['╪', '#d7b95f', '#15130d'], rubble: ['░', '#a7afb8', '#11151d'], bramble: ['♧', '#7da56e', '#0e160d'], darkness: ['·', '#47556a', '#080b12'], crate: ['□', '#c69a6b', '#17120d'], chest: ['▣', '#f4d26a', '#1b150b'], altar: ['_', '#d2a4e8', '#17101b'], shop: ['$', '#f4d26a', '#1a150b'], rescue: ['&', '#8ae0b3', '#0d1714'], smoke: ['≈', '#a3a8b3', '#13161b'], lift: ['↕', '#e9c47e', '#1b170d'], breakwall: ['▓', '#bc8266', '#1c1210'], current: ['≋', '#8edce1', '#0a1920'], deepWater: ['≈', '#4b8ca0', '#061019'], anchor: ['⚓', '#82cbd1', '#0a1820'], cliffWall: ['▲', '#71809d', '#101725'], ledge: ['=', '#c2d4dc', '#101725'], graveSoil: [';', '#95836f', '#17120e'], cairn: ['▲', '#b9aa94', '#17120e'], ossuary: ['□', '#d9d3c5', '#17120e'], spiritPath: ['·', '#c9a6db', '#17101b'], saltMirror: ['◇', '#fff0ab', '#1a170d'], brine: ['≈', '#77c4cb', '#0a1920'], ice: ['═', '#bfeeff', '#0b1821'], frostRime: ['*', '#dff8ff', '#111b24']
}
const outpostAsciiGlyph = {
  space: ['·', '#36445f', '#05070b'], deck: ['·', '#62748b', '#101722'], corridor: ['═', '#b9c9dc', '#172638'], bulkhead: ['▓', '#74889d', '#1a2633'], viewport: ['*', '#79aef4', '#07101c'], airlock: ['≡', '#e2bc70', '#241e14'], hull: ['█', '#6d8095', '#0a1019'], flightConsole: ['⌘', '#f4d26a', '#24334a']
} as const
const outpostRuneGlyph = {
  space: ['·', '#435574', '#05070b'], deck: ['·', '#7389a2', '#0d131d'], corridor: ['═', '#d1dfef', '#132338'], bulkhead: ['▓', '#8da0b4', '#15212d'], viewport: ['✦', '#9ac6ff', '#050d19'], airlock: ['≡', '#f1cd83', '#20190f'], hull: ['█', '#7b8da2', '#080e16'], flightConsole: ['⌘', '#ffe181', '#1d304a']
} as const
const outpostDecorationGlyph: Record<number, [string, string, string]> = {
  8: ['A', '⌂', colors.gold], 9: ['!', '✦', colors.gold], 10: ['!', '✶', colors.red], 11: ['·', '✧', colors.blue], 12: ['|', '╫', '#b9c8d8'], 13: ['+', '⊞', colors.blue], 15: ['?', '▣', colors.gold], 16: ['$', '▣', colors.gold], 17: ['&', '⚒', colors.gold], 18: ['C', '◉', colors.green], 19: ['o', '◉', colors.dim], 20: ['|', '║', colors.dim], 21: ['*', '✧', colors.green]
}
const areaList = (areas: readonly Biome[]): string => areas.map(area => biomeName[area]).join(', ')
const jomonMasthead = jomonMastheadSource.trimEnd()
const jomonMastheadWidth = Math.max(...jomonMasthead.split('\n').map(line => line.length))
const spriteFrameInterval = 80
const courierOrigins = {
  mineborn: { label: 'ENGINEERING DECK', description: 'A field mechanic trained to keep fragile systems alive under pressure.', stats: { strength: 3, agility: 1, vitality: 3, intellect: 1 } },
  mosswalker: { label: 'HABITAT DECK', description: 'A biosphere specialist who reads living terrain and finds stable ground.', stats: { strength: 1, agility: 3, vitality: 3, intellect: 1 } },
  cavernSeeker: { label: 'NAVIGATION DECK', description: 'A survey specialist who reads echoes, pressure, and buried routes.', stats: { strength: 1, agility: 2, vitality: 2, intellect: 3 } },
  tidebound: { label: 'MEDICAL DECK', description: 'An expedition medic trained to turn changing conditions into a way forward.', stats: { strength: 2, agility: 3, vitality: 1, intellect: 2 } }
} as const
const courierCallings = {
  trailguard: { label: 'SECURITY SPECIALIST', description: 'Carry a field tether and guard plate. Hold the landing zone when it closes.', kit: [{ name: 'Field Tether', effect: 'reach 2 strike' }, { name: 'Guard Plate', effect: 'extra defence' }, { name: 'Med Gel', effect: 'restore health' }] },
  pathmaker: { label: 'SYSTEMS SPECIALIST', description: 'Carry breaching charges and line spools. Make a path through damaged ground.', kit: [{ name: 'Field Tether', effect: 'reach 2 strike' }, { name: '6 Breach Charges', effect: 'clear obstacles' }, { name: '6 Line Spools', effect: 'cross pits' }, { name: 'Survey Map', effect: 'reveal floor' }] },
  spiritbearer: { label: 'XENOARCHIVIST', description: 'Carry focus gel and a scanner module for conditions no route file explains.', kit: [{ name: 'Field Tether', effect: 'reach 2 strike' }, { name: 'Focus Gel', effect: 'restore focus' }, { name: 'Scanner Module', effect: 'reveal surroundings' }] }
} as const

export class TerminalRenderer {
  private readonly ctx: CanvasRenderingContext2D
  private readonly effects = new TerminalEffects(CW, CH)
  private spriteMode = false
  private runeMode = false
  private heroFacingLeft = false
  private heroAnimation: HeroAnimation = 'idle'
  private heroAnimationUntil = 0
  private boardZoom = 1
  private camera?: Point
  private cameraFloor?: number
  private lastRoute: ScreenRoute = { screen: 'title', biome: 'mine' }
  private lastState?: RunState
  private lastRecords?: { bestDepth: number; wins: number; deaths: number }
  private lastHub?: HubView
  private lastStory?: StoryState
  private lastLoading?: LoadingState
  private lastTransit?: TransitState
  private lastAnalysis?: RunAnalysis
  private lastCourierMenu?: CourierMenuView
  private lastCourierDraft?: CourierDraft
  private lastAutoplayMode: AutoplayMode = 'off'
  private autoplayDiagnostic?: AutoplayDiagnostic
  private settings: GameSettings = defaultSettings()
  private fontState: 'loading' | 'ready' | 'fallback' = 'loading'
  private bootstrapState: 'loading' | 'ready' | 'error' = 'loading'
  private bootstrapMessage = 'Loading courier records…'
  private persistenceState: 'saved' | 'saving' | 'error' = 'saved'
  private hubAnimationUntil = 0
  private pendingAnimationFrame?: number
  private pendingAnimationTimer?: number

  constructor(private readonly canvas: HTMLCanvasElement) {
    const ctx = canvas.getContext('2d', { alpha: false })
    if (!ctx) throw new Error('Canvas 2D unavailable')
    this.ctx = ctx
    canvas.width = TERMINAL_WIDTH * CW
    canvas.height = TERMINAL_HEIGHT * CH
    ctx.imageSmoothingEnabled = false
    ctx.font = '12px "BigBlueTerm", monospace'
    ctx.textBaseline = 'top'
    this.loadFont()
    textureAtlas.onReady(() => this.render(this.lastRoute, this.lastState, this.lastRecords, this.lastHub, this.lastStory, this.lastLoading, this.lastAnalysis, this.lastCourierMenu, this.lastCourierDraft, this.lastAutoplayMode))
  }

  private loadFont(): void {
    if (!('fonts' in document)) { this.fontState = 'fallback'; return }
    void document.fonts.load('12px "BigBlueTerm"', 'JOMON').then(() => {
      this.fontState = document.fonts.check('12px "BigBlueTerm"', 'JOMON') ? 'ready' : 'fallback'
      if (this.fontState === 'fallback') this.ctx.font = '12px monospace'
    }).catch(() => {
      this.fontState = 'fallback'
      this.ctx.font = '12px monospace'
    }).finally(() => this.render(this.lastRoute, this.lastState, this.lastRecords, this.lastHub, this.lastStory, this.lastLoading, this.lastAnalysis, this.lastCourierMenu, this.lastCourierDraft, this.lastAutoplayMode))
  }

  setVisualMode(value: VisualMode): void {
    this.spriteMode = false
    this.runeMode = value === 'runes'
    this.heroAnimation = 'idle'; this.heroAnimationUntil = 0
  }
  setHeroFacingLeft(value: boolean): void { this.heroFacingLeft = value }
  setHubMoved(): void { this.hubAnimationUntil = performance.now() + 220 }
  setBoardZoom(value: number): void {
    this.boardZoom = Math.max(.5, Math.min(5, value))
  }
  panCamera(dx: number, dy: number): void {
    if (!this.lastState) return
    const floor = this.lastState.floor
    const current = this.camera ?? { x: this.lastState.hero.x, y: this.lastState.hero.y }
    this.camera = { x: Math.max(0, Math.min(floor.width - 1, current.x + dx)), y: Math.max(0, Math.min(floor.height - 1, current.y + dy)) }
    this.cameraFloor = floor.index
    this.render(this.lastRoute, this.lastState, this.lastRecords, this.lastHub, this.lastStory, this.lastLoading, this.lastAnalysis, this.lastCourierMenu, this.lastCourierDraft, this.lastAutoplayMode)
  }
  recenterCamera(): void { this.camera = undefined; this.cameraFloor = undefined }
  setSettings(settings: GameSettings): void { this.settings = settings; this.effects.setReducedFlash(settings.reducedFlash) }
  setAutoplayDiagnostic(value: AutoplayDiagnostic | undefined): void { this.autoplayDiagnostic = value }
  setBootstrapState(state: 'loading' | 'ready' | 'error', message?: string): void { this.bootstrapState = state; if (message) this.bootstrapMessage = message }
  setPersistenceState(state: 'saved' | 'saving' | 'error'): void { this.persistenceState = state }
  get visualMode(): VisualMode { return this.runeMode ? 'runes' : 'ascii' }
  trigger(events: ActionResult, state?: RunState, effectId?: string): void {
    const now = performance.now()
    if (events.some(event => event.type === 'death')) { this.heroAnimation = 'death'; this.heroAnimationUntil = Number.POSITIVE_INFINITY }
    else if (events.some(event => event.type === 'hurt')) { this.heroAnimation = 'hit'; this.heroAnimationUntil = now + 320 }
    else if (events.some(event => event.type === 'hit')) { this.heroAnimation = 'attack'; this.heroAnimationUntil = now + 360 }
    else if (events.some(event => event.type === 'move')) { this.heroAnimation = 'walk'; this.heroAnimationUntil = now + 280 }
    this.effects.trigger(events, state, this.canvas, effectId)
  }

  render(route: ScreenRoute, state: RunState | undefined, records?: { bestDepth: number; wins: number; deaths: number }, hub?: HubView, story?: StoryState, loading?: LoadingState, analysis?: RunAnalysis, courierMenu?: CourierMenuView, courierDraft?: CourierDraft, autoplayMode: AutoplayMode = 'off', transit?: TransitState): void {
    this.lastRoute = route
    this.lastState = state
    this.lastRecords = records
    this.lastHub = hub
    this.lastStory = story
    this.lastLoading = loading
    this.lastTransit = transit
    this.lastAnalysis = analysis
    this.lastCourierMenu = courierMenu
    this.lastCourierDraft = courierDraft
    this.lastAutoplayMode = autoplayMode
    if (this.fontState === 'loading') {
      this.ctx.fillStyle = colors.back
      this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
      return
    }
    const now = performance.now()
    this.effects.update(now)
    this.ctx.setTransform(1, 0, 0, 1, 0, 0)
    this.ctx.fillStyle = colors.back
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
    this.ctx.save()
    this.effects.applyShake(this.ctx, now)
    if (route.screen === 'splash') this.splash(courierMenu)
    else if (route.screen === 'title') this.title(courierMenu)
    else if (route.screen === 'codex') this.codex(route.codexPage ?? 0)
    else if (route.screen === 'createCourier' && courierDraft) this.createCourier(courierDraft)
    else if (route.screen === 'approach') this.approach(route, story, now)
    else if (route.screen === 'hub') this.hub(route, hub, now)
    else if (route.screen === 'area') this.area(route)
    else if (route.screen === 'loading') this.loading(state, loading, now)
    else if (route.screen === 'transit' && transit) this.transit(transit, now)
    else if (route.screen === 'analysis' && analysis) this.analysis(analysis)
    else if (!state || state.status === 'title') this.title()
    else {
      this.stage(state)
      this.sidebar(state)
      this.log(state)
      if (state.modal) this.modal(state, state.modal)
      if (state.status === 'dead') this.end(state, false)
      if (state.status === 'victory') this.end(state, true)
    }
    this.persistenceNotice()
    this.ctx.restore()
    this.effects.drawFlash(this.ctx, this.canvas, now)
    if (this.effects.needsFrame(now) || route.screen === 'loading' || route.screen === 'transit' || Boolean(story)) this.scheduleRender()
    else if (route.screen === 'level' && state?.floor.tiles.some(tile => tile.visible && tile.flow)) this.scheduleRender(180)
    else if ((this.spriteMode && route.screen === 'level' && state) || (route.screen === 'hub' && now < this.hubAnimationUntil) || (route.screen === 'createCourier' && courierDraft?.focus === 0)) this.scheduleRender(spriteFrameInterval)
  }

  private scheduleRender(delay = 0): void {
    if (delay <= 0) {
      if (this.pendingAnimationTimer !== undefined) {
        window.clearTimeout(this.pendingAnimationTimer)
        this.pendingAnimationTimer = undefined
      }
      if (this.pendingAnimationFrame !== undefined) return
      this.pendingAnimationFrame = requestAnimationFrame(() => {
        this.pendingAnimationFrame = undefined
        this.render(this.lastRoute, this.lastState, this.lastRecords, this.lastHub, this.lastStory, this.lastLoading, this.lastAnalysis, this.lastCourierMenu, this.lastCourierDraft, this.lastAutoplayMode, this.lastTransit)
      })
      return
    }
    if (this.pendingAnimationFrame !== undefined || this.pendingAnimationTimer !== undefined) return
    this.pendingAnimationTimer = window.setTimeout(() => {
      this.pendingAnimationTimer = undefined
      this.scheduleRender()
    }, delay)
  }

  private title(menu?: CourierMenuView): void { this.splash(menu) }

  private splash(menu?: CourierMenuView): void {
    this.box(8, 5, 80, 50, '')
    this.ascii(Math.floor((TERMINAL_WIDTH - jomonMastheadWidth) / 2), 9, jomonMasthead, colors.text)
    if (this.bootstrapState !== 'ready') {
      const message = this.bootstrapState === 'loading' ? this.bootstrapMessage : `${this.bootstrapMessage} · F2 retry`
      this.text(Math.floor((TERMINAL_WIDTH - message.length) / 2), 31, message, this.bootstrapState === 'loading' ? colors.dim : colors.red)
      return
    }
    const entries = menu?.entries ?? []
    this.text(18, 23, 'WHICH LANDING SPECIALIST IS ON DUTY?', colors.text)
    if (!entries.length) this.text(18, 26, '(No active crew records. Please create a specialist.)', colors.dim)
    else entries.slice(0, 8).forEach((entry, index) => {
      const selected = entry.id === menu?.selectedId
      const marker = selected ? '>' : ' '
      const mode = entry.deathMode === 'checkpoint' ? 'checkpoint' : 'iron trail'
      this.text(18, 26 + index * 2, `${marker} ${entry.name.padEnd(16)} ${entry.origin.padEnd(14)} ${entry.calling.padEnd(14)} ${mode}`, selected ? colors.green : colors.text)
    })
    if (entries.length) {
      const selected = entries.find(entry => entry.id === menu?.selectedId) ?? entries[0]
      const status = selected.floor ? `landing ${String(selected.floor).padStart(2, '0')} · turn ${selected.turn ?? 0}` : 'aboard the Jomon Voyager'
      this.text(18, 43, `${selected.name} waits at ${selected.area ? biomeName[selected.area] : 'the carrier'} · ${status}.`, colors.dim)
    }
    const controls = entries.length
      ? ['[L]/ENTER  resume · [↑↓]  change selection', '[N]  create courier · [D]  delete courier · [W]  codex']
      : ['[N]  create courier · [W]  codex']
    const controlsY = controls.length === 2 ? 48 : 51
    controls.forEach((line, index) => this.text(8 + Math.floor((80 - line.length) / 2), controlsY + index * 3, line, colors.text))
    if (menu?.confirmingDelete) this.box(27, 27, 42, 9, 'RETIRE COURIER'), this.text(31, 31, 'D confirms · ESC cancels', colors.red)
  }

  private persistenceNotice(): void {
    if (this.persistenceState === 'saved') return
    const notice = this.persistenceState === 'saving' ? 'SAVE PENDING' : 'SAVE FAILED · F2 RETRY'
    this.text(TERMINAL_WIDTH - notice.length - 2, TERMINAL_HEIGHT - 2, notice, this.persistenceState === 'saving' ? colors.dim : colors.red)
  }

  private codex(page: number): void {
    const current = LORE_CODEX_PAGES[Math.max(0, Math.min(page, LORE_CODEX_PAGES.length - 1))]!
    this.box(8, 5, 80, 50, 'JOMON VOYAGER · MISSION ARCHIVE')
    this.text(14, 11, current.title, colors.gold)
    this.ruleHorizontal(14, 13, 68)
    current.lines.flatMap(line => this.wrap(line, 66)).forEach((line, index) => this.text(14, 17 + index * 3, line, colors.text))
    this.text(14, 49, `${String(page + 1).padStart(2, '0')}/${String(LORE_CODEX_PAGES.length).padStart(2, '0')}  ←/→ page · W/ESC return`, colors.green)
  }

  private createCourier(draft: CourierDraft): void {
    const origin = courierOrigins[draft.origin]
    const calling = courierCallings[draft.calling]
    const death = draft.deathMode === 'checkpoint' ? ['MEDBAY RETURN', 'Death returns you to the last cleared landing checkpoint.'] : ['IRON EXPEDITION', 'Death ends this specialist\'s mission record.']
    this.box(6, 3, 84, 53, 'CREATE LANDING SPECIALIST')
    const name = draft.name.trim()
    this.creatorField(10, 11, 'NAME', name || 'Unnamed Courier', draft.focus === 0, !name, draft.focus === 0 && Math.floor(performance.now() / 500) % 2 === 0)
    this.creatorField(10, 17, 'ORIGIN', origin.label, draft.focus === 1)
    this.creatorField(10, 29, 'CALLING', calling.label, draft.focus === 2)
    this.creatorField(10, 39, 'DEATH', death[0], draft.focus === 3)
    this.creatorField(10, 47, 'COMPANION CONTROL', draft.companionControlMode.toUpperCase(), draft.focus === 4)
    this.wrap(origin.description, 43).slice(0, 4).forEach((line, index) => this.text(40, 17 + index, line, colors.text))
    this.text(40, 22, 'STATS', colors.gold)
    ;(['strength', 'agility', 'vitality', 'intellect'] as const).forEach((stat, index) => {
      const value = origin.stats[stat]
      this.text(40, 24 + index, `${stat.slice(0, 3).toUpperCase()} ${value}  ${'█'.repeat(value)}${'░'.repeat(4 - value)}`, colors.text)
    })
    this.wrap(calling.description, 43).slice(0, 4).forEach((line, index) => this.text(40, 30 + index, line, colors.text))
    this.text(40, 35, 'KIT', colors.gold)
    calling.kit.forEach((item, index) => this.text(40, 36 + index, `· ${item.name} — ${item.effect}`, colors.text))
    this.text(40, 41, death[1], colors.text)
    this.creatorField(40, 43, 'COMPANION LOSS', draft.companionDeathMode === 'permadeath' ? 'PERMADEATH' : 'RECOVERABLE', draft.focus === 5)
    this.text(40, 49, draft.companionDeathMode === 'permadeath' ? 'Loss is irreversible after creation.' : 'Loss uses Lodge injury recovery.', draft.focus === 5 ? colors.green : colors.text)
    this.text(40, 51, draft.companionDeathMode === 'permadeath' ? draft.companionDeathConfirmed ? 'ENTER again: confirm permanent loss.' : 'ENTER: arm irreversible confirmation.' : 'This choice cannot change after creation.', draft.companionDeathMode === 'permadeath' ? colors.red : colors.dim)
    this.text(10, 54, '↑↓ field · ←→ choose · TAB next · A-Z/DEL name · ENTER create · ESC cancel', colors.dim)
  }

  private creatorField(x: number, y: number, label: string, value: string, focus: boolean, placeholder = false, cursor = false): void {
    this.text(x, y, label, colors.gold)
    this.text(x, y + 2, `${focus ? '>' : ' '} `, focus ? colors.green : colors.text)
    this.text(x + 2, y + 2, value, placeholder ? 'rgba(150, 211, 139, .45)' : focus ? colors.green : colors.text)
    if (cursor) {
      const cursorX = x + 2 + (placeholder ? 0 : value.length)
      const cursorY = y + (placeholder ? 3 : 2)
      this.ctx.fillStyle = colors.green
      this.ctx.fillText('_', cursorX * CW, cursorY * CH - (placeholder ? 2 : 0))
    }
  }

  private approach(route: ScreenRoute, story: StoryState | undefined, now: number): void {
    const season = mineSeason(route.heirSeed ?? 0)
    const x = 3
    const y = 2
    this.box(x, y, 90, 56, story?.scene.title ?? 'JOMON VOYAGER // AIRLOCK')
    if (!story) {
      this.drawOutpostViewport(24, 5, { x: 24, y: 29 }, () => this.drawOutpostScene(24, 5, { x: 24, y: 29 }, 'opening', 0, false))
      this.text(x + 5, 44, seasonLabel(season), season.color)
      this.text(x + 5, 47, 'The carrier prepares a landing file for the next colony.', colors.text)
      this.text(x + 5, 50, season.scene, colors.text)
      this.text(x + 5, 54, `ENTER continue · ESC title · V ${visualModeLabel(this.visualMode)} · +/- ${this.boardZoom.toFixed(2)}x`, colors.green)
      return
    }
    const hero = story.scene.vignette === 'opening'
      ? story.page ? { x: 23, y: 15 } : { x: 24, y: 29 }
      : story.scene.vignette === 'succession'
        ? story.page === 2 ? { x: 24, y: 27 } : { x: 21, y: 17 }
        : { x: 24, y: 16 }
    this.drawOutpostViewport(24, 5, hero, () => this.drawOutpostScene(24, 5, hero, story.scene.vignette, story.page, now < this.hubAnimationUntil))
    this.text(x + 5, 42, `${String(story.page + 1).padStart(2, '0')}/${String(story.scene.pages.length).padStart(2, '0')}  ${seasonLabel(season)}`, season.color)
    this.wrap(storyText(story, now), 76).slice(0, 4).forEach((line, index) => this.text(x + 5, 44 + index * 2, line, colors.text))
    const storyControls = isStoryPageComplete(story, now) ? 'ANY KEY continue · SPACE skip · ESC return' : 'ANY KEY reveal · SPACE skip · ESC return'
    this.text(x + 5, 54, `${storyControls} · V ${visualModeLabel(this.visualMode)} · +/- ${this.boardZoom.toFixed(2)}x`, colors.green)
  }

  private loading(state: RunState | undefined, loading: LoadingState | undefined, now: number): void {
    if (loading?.phase === 'fade' && state) {
      this.stage(state)
      this.sidebar(state)
      this.log(state)
      if (state.status !== 'playing') this.end(state, false)
      this.ctx.save()
      this.ctx.globalAlpha = Math.min(1, Math.max(0, (now - loading.startedAt) / 350))
      this.ctx.fillStyle = '#05070b'
      this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
      this.ctx.restore()
      return
    }
    this.ctx.fillStyle = '#05070b'
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
    const biomeTransition = loading?.kind === 'biome'
    const title = biomeTransition ? 'COLONY COMPLETE' : 'PREPARING LANDING'
    const destination = loading?.toBiome ? biomeName[loading.toBiome] : 'JOMON VOYAGER'
    const message = biomeTransition ? `${biomeName[loading?.fromBiome ?? 'mine']}  →  ${destination}` : 'LOADING MISSION FILE'
    this.box(27, 13, 42, 25, title)
    this.text(48 - Math.floor(message.length / 2), 24, message, colors.text)
    this.text(48 - Math.floor((biomeTransition ? 'THE VOYAGER MOVES ON.' : 'The carrier prepares supplies.').length / 2), 27, biomeTransition ? 'THE VOYAGER MOVES ON.' : 'The carrier prepares supplies.', colors.dim)
    this.text(48 - Math.floor('Please stand by.'.length / 2), 30, 'Please stand by.', colors.dim)
  }

  private transit(transit: TransitState, now: number): void {
    const elapsed = now - transit.startedAt
    const destination = transit.toBiome ? biomeName[transit.toBiome] : 'NEW EDO'
    const progress = Math.min(1, elapsed / 3400)
    this.ctx.fillStyle = '#05070b'
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
    for (let index = 0; index < 42; index++) {
      const x = (index * 37 + Math.floor(elapsed / 16) * ((index % 3) + 1)) % 94
      const y = 4 + (index * 17) % 43
      this.text(x, y, index % 7 === 0 ? '✦' : '·', index % 7 === 0 ? colors.gold : colors.dim)
    }
    const shipX = Math.max(5, Math.min(76, Math.floor(5 + progress * 71)))
    this.text(shipX, 28, '<==[ JOMON VOYAGER ]==>', colors.green)
    this.box(20, 10, 56, 10, 'CARRIER TRANSIT')
    const route = `${biomeName[transit.fromBiome]}  →  ${destination}`
    this.text(48 - Math.floor(route.length / 2), 14, route, colors.text)
    const phase = progress < .33 ? 'DEPARTURE BURN' : progress < .77 ? 'CRUISING BETWEEN STARS' : 'APPROACH WINDOW CONFIRMED'
    this.text(48 - Math.floor(phase.length / 2), 17, phase, colors.gold)
    this.text(48 - Math.floor('ANY KEY skips transit'.length / 2), 48, 'ANY KEY skips transit', colors.dim)
  }

  private hub(route: ScreenRoute, hub: HubView | undefined, now: number): void {
    const position = hub?.position ?? outpostSpawn()
    const nearby = outpostInteraction(position)
    const routeBoard = outpostMap.interactables.find(interactable => interactable.destination === 'routes')!
    const routeSteps = Math.max(0, Math.max(Math.abs(position.x - routeBoard.point.x), Math.abs(position.y - routeBoard.point.y)) - 1)
    this.ctx.fillStyle = colors.ink
    this.ctx.fillRect(0, 0, MAP_WIDTH * CW, MAP_HEIGHT * CH)
    this.drawOutpostViewport(0, 0, position, () => this.drawOutpostScene(0, 0, position, undefined, 0, now < this.hubAnimationUntil))
    this.ruleVertical(MAP_WIDTH, 0, TERMINAL_HEIGHT)
    this.hubSidebar(hub, nearby, routeSteps)
    if (route.hubAction && route.hubAction !== 'routes') this.hubService(route.hubAction, hub, route.companionAction, route.companionControlMode)
    this.hubLog(hub, nearby, routeSteps)
  }

  private hubSidebar(hub: HubView | undefined, nearby: ReturnType<typeof outpostInteraction>, routeSteps: number): void {
    const hero = hub?.hero
    const campaign = hub?.campaign
    this.text(50, 1, 'CARRIER HUB', colors.gold)
    this.text(50, 2, 'JOMON VOYAGER', colors.text)
    this.ruleHorizontal(50, 3, 45)
    if (hero) this.courierSheet(hero)
    else this.text(50, 6, 'Specialist record unavailable.', colors.red)
    this.text(50, 32, 'OPEN LANDINGS', colors.gold)
    this.wrap(areaList(hub?.state.unlockedAreas ?? ['mine']), 43).slice(0, 2).forEach((line, index) => this.text(50, 33 + index, line, colors.text))
    this.text(50, 36, `VOYAGE: ${campaign?.tierLabel ?? 'BASE'} · DONE ${campaign?.completedLabel ?? 'NONE'}`, campaign?.terminal ? colors.gold : campaign?.continuationPending ? colors.green : colors.dim)
    this.text(50, 37, `HISTORY: ${campaign?.historyLabel ?? 'BASE ACTIVE'}`.slice(0, 45), colors.text)
    this.text(50, 38, `FIXED: ${campaign?.packageName ?? 'Base Route'}`, colors.gold)
    campaign?.difficultyLines.forEach((line, index) => this.text(50, 39 + index, line, colors.text))
    this.text(50, 42, campaign?.nextLabel ?? 'NEXT: finish BASE to unlock NG+.', campaign?.terminal ? colors.gold : colors.green)
    this.text(50, 44, nearby ? 'NEARBY' : 'NEXT STOP', colors.gold)
    this.text(50, 45, nearby ? nearby.name.toUpperCase() : `FLIGHT CONSOLE · ↑ ${routeSteps}`, colors.green)
    this.text(50, 47, `AUTOPILOT: ${autoplayModeLabel(this.lastAutoplayMode)}`, this.lastAutoplayMode === 'off' ? colors.dim : colors.green)
    if (hero) this.boonRelicLists(hero, 49)
  }

  private hubLog(hub: HubView | undefined, nearby: ReturnType<typeof outpostInteraction>, routeSteps: number): void {
    const context = nearby ? `${nearby.name}: C / ENTER to interact.` : `Bridge flight console: ${routeSteps} tile${routeSteps === 1 ? '' : 's'} away.`
    const lines = [hub?.notice ?? context, hub?.notice ? context : `Open landings: ${areaList(hub?.state.unlockedAreas ?? ['mine'])}.`]
    this.ruleHorizontal(0, 35, 48)
    lines.flatMap((line, lineIndex) => this.wrap(line, 46).map(value => ({ value, color: lineIndex === 0 ? colors.text : colors.dim }))).slice(0, 14).forEach((entry, index) => this.text(1, 36 + index, entry.value, entry.color))
    this.ruleHorizontal(0, 50, MAP_WIDTH)
    this.text(1, 52, 'MOVE arrows/IOP K ; , . / numpad · Shift run', colors.dim)
    this.text(1, 53, 'ACT C interact · 1-6 service · ESC close', colors.dim)
    this.text(1, 54, `F auto ${autoplayModeLabel(this.lastAutoplayMode)} · Shift+F policy`, colors.dim)
    this.text(1, 55, `V ${visualModeLabel(this.visualMode)} · +/- ${this.boardZoom.toFixed(2)}x · F1 settings`, colors.dim)
  }

  private drawOutpostViewport(x: number, y: number, focus: { x: number; y: number }, draw: () => void): void {
    if (this.boardZoom === 1) { draw(); return }
    const width = outpostMap.width * CW
    const height = outpostMap.height * CH
    const viewportCenterX = x * CW + width / 2
    const viewportCenterY = y * CH + height / 2
    const focusX = (x + focus.x + .5) * CW
    const focusY = (y + focus.y + .5) * CH
    this.ctx.save()
    this.ctx.beginPath()
    this.ctx.rect(x * CW, y * CH, width, height)
    this.ctx.clip()
    this.ctx.translate(viewportCenterX, viewportCenterY)
    this.ctx.scale(this.boardZoom, this.boardZoom)
    this.ctx.translate(-focusX, -focusY)
    draw()
    this.ctx.restore()
  }

  private drawOutpostScene(x: number, y: number, hero: { x: number; y: number }, vignette: 'opening' | 'succession' | 'ending' | undefined, page: number, walking: boolean): void {
    outpostMap.tiles.forEach((tile, index) => {
      const column = index % outpostMap.width
      const row = Math.floor(index / outpostMap.width)
      const [glyph, color, background] = (this.runeMode ? outpostRuneGlyph : outpostAsciiGlyph)[tile]
      if (tile === 'space') {
        const star = (column * 17 + row * 31) % 29 === 0
        this.cell(x + column, y + row, star ? (this.runeMode ? '✦' : '*') : glyph, star ? colors.blue : color, background)
      } else this.cell(x + column, y + row, glyph, color, background)
    })
    outpostMap.decorations.forEach(decoration => {
      const [ascii, rune, color] = outpostDecorationGlyph[decoration.tile] ?? ['*', '✧', colors.text]
      this.cell(x + decoration.x, y + decoration.y, this.runeMode ? rune : ascii, color)
    })
    this.text(x + 12, y + 1, '[ JOMON VOYAGER // CARRIER DECK ]', colors.gold)
    this.text(x + 6, y + 11, 'BRIDGE', colors.dim)
    this.text(x + 15, y + 10, 'HABITAT', colors.dim)
    this.text(x + 22, y + 12, 'CARGO', colors.dim)
    this.text(x + 21, y + 27, 'ENGINEERING', colors.dim)
    this.text(x + 35, y + 11, 'SCIENCE', colors.dim)
    this.text(x + 40, y + 22, 'DOCK', colors.dim)
    const keeper = vignette === 'ending' ? { x: 27, y: 14 } : { x: 27, y: 9 }
    const porter = vignette === 'succession' && page === 0 ? { x: 24, y: 17 } : { x: 29, y: 18 }
    this.cell(x + keeper.x, y + keeper.y, this.runeMode ? '♜' : 'K', colors.gold)
    this.cell(x + porter.x, y + porter.y, this.runeMode ? '♟' : 'P', '#c7976f')
    const specialistGlyph = this.runeMode ? (walking ? '◉' : '☉') : walking ? '◌' : '@'
    this.cell(x + hero.x, y + hero.y, specialistGlyph, colors.text)
  }

  private hubService(action: Exclude<NonNullable<ScreenRoute['hubAction']>, 'routes'>, hub?: HubView, companionAction?: ScreenRoute['companionAction'], companionControlMode?: ScreenRoute['companionControlMode']): void {
    if (action === 'continuation') {
      const campaign = hub?.campaign
      const carryover = hub?.carryover
      this.box(49, 3, 46, 46, 'NEW EDO OR CONTINUE')
      let y = 6
      const text = (value: string, color = colors.text) => { this.text(52, y++, value.slice(0, 40), color) }
      const wrapped = (value: string, color = colors.text) => this.wrap(value, 40).forEach(line => text(line, color))
      text(`CURRENT: ${campaign?.tierLabel ?? 'BASE'} · DONE ${campaign?.completedLabel ?? 'NONE'}`, colors.gold)
      wrapped(`HISTORY: ${campaign?.historyLabel ?? 'BASE ACTIVE'}`, colors.dim)
      text(`FIXED: ${campaign?.packageName ?? 'Base Route'}`, colors.gold)
      campaign?.difficultyLines.forEach(line => text(line))
      wrapped(campaign?.nextLabel ?? 'NEXT: finish BASE to unlock NG+.', colors.green)
      y++
      text('RETAINED VOYAGER REFITS — NO RESET', colors.gold)
      text(`CASH: ${carryover?.currency ?? 0}`, colors.text)
      wrapped(`ITEMS: ${carryover?.items.join(', ') || 'none'}`)
      wrapped(`TOOLS: ${carryover?.tools.join(', ') || 'none'}`)
      wrapped(`ROSTER: ${carryover?.roster.map(entry => `${entry.name} ${entry.status}`).join(', ') || 'none'}`)
      wrapped(`INJURIES: ${carryover?.injuries.join(', ') || 'none'}`)
      wrapped(`LOSSES: ${carryover?.losses.join(', ') || 'none'}`)
      y++
      text('Dock at New Edo with C / ESC.', colors.dim)
      text('ENTER / E accepts a harder revised route.', colors.green)
      return
    }
    this.box(53, action === 'roster' ? 19 : 22, 40, action === 'roster' ? 23 : 17, action === 'shop' ? 'SUPPLY STALL' : action === 'outfitter' ? 'OUTFITTER' : 'COMPANION LODGE')
    if (action === 'roster') {
      const companions = hub?.companions ?? []
      if (companionControlMode) {
        const direct = companionControlMode === 'direct'
        this.text(56, 23, `SET ${companionControlMode.toUpperCase()} CONTROL`, colors.gold)
        this.text(56, 25, direct ? 'Turn order: courier, then companions.' : 'Turn order: companions follow priorities.', colors.text)
        this.text(56, 28, direct ? 'Input: choose each active companion action.' : 'Input: companion commands are unavailable.', colors.text)
        this.wrap('Switches apply only at the Lodge between floors; never during combat, autoplay, replay, or command phases.', 34).forEach((line, index) => this.text(56, 31 + index * 2, line, colors.dim))
        this.text(56, 39, 'ENTER confirm · C / ESC cancel', colors.green)
        return
      }
      const selected = companionAction ? companions.find(companion => companion.id === companionAction.id) : undefined
      if (selected && companionAction) {
        this.text(56, 23, `${companionAction.action.toUpperCase()} ${selected.name}`.slice(0, 32), colors.gold)
        this.text(56, 25, `${selected.role.toUpperCase()} · ${selected.rosterStatus.toUpperCase()}`, colors.text)
        this.text(56, 27, `${biomeName[selected.recruitment.biome]} stage ${selected.recruitment.floor + 1}`, colors.dim)
        this.text(56, 30, companionAction.action === 'recruit' ? 'Free · status becomes BENCHED.' : companionAction.action === 'activate' ? 'Joins active party if capacity allows.' : companionAction.action === 'beginRecovery' ? '10 cash · benched for one cleared floor.' : companionAction.action === 'completeRecovery' ? 'No cost · returns healthy to the bench.' : 'Returns to the lodge bench.', colors.text)
        this.text(56, 39, 'ENTER confirm · C / ESC cancel', colors.green)
        return
      }
      this.text(56, 23, `0. CONTROL: ${(hub?.companionControlMode ?? 'autonomous').toUpperCase()} · LOSS: ${(hub?.companionDeathMode === 'permadeath' ? 'PERMANENT' : 'RECOVERABLE')}`.slice(0, 34), colors.gold)
      if (!companions.length) { this.text(56, 26, hub?.state.rescued.length ? 'Rescues are being logged as leads.' : 'No rescue leads are available.', colors.dim); this.text(56, 39, '0 change control · C / ESC close', colors.green); return }
      companions.slice(0, 5).forEach((companion, index) => {
        const status = companion.permanentlyLost ? 'UNAVAILABLE' : companion.injury === 'recovering' ? `REC ${companion.recoveryFloors ?? 1}F` : companion.injury === 'injured' ? 'INJURED' : companion.rosterStatus.toUpperCase()
        this.text(56, 26 + index * 2, `${index + 1}. ${companion.name.slice(0, 12).padEnd(12)} ${companion.role.slice(0, 5).padEnd(5)} ${status}`.slice(0, 34), companion.permanentlyLost || companion.injury !== 'healthy' ? colors.dim : companion.rosterStatus === 'active' ? colors.green : colors.text)
      })
      this.text(56, 39, `0 control · 1-${Math.min(5, companions.length)} review · C / ESC close`, colors.green)
      return
    }
    const ids = action === 'shop' ? hub?.stock ?? [] : hub?.equipment ?? []
    if (!ids.length) this.text(56, 27, action === 'shop' ? 'No stock is available.' : 'No equipment is ready.', colors.dim)
    ids.slice(0, 6).forEach((id, index) => {
      const item = ITEM[id]
      const value = action === 'shop' ? `${String(item.value).padStart(3)} cash` : hub?.hero?.equipment[item.slot ?? 'mainHand'] === id ? 'EQUIPPED' : item.slot?.toUpperCase() ?? ''
      this.text(56, 26 + index * 2, `${index + 1}. ${item.name.slice(0, 19).padEnd(19)} ${value}`, action === 'outfitter' && value === 'EQUIPPED' ? colors.green : colors.text)
    })
  }

  private area(route: ScreenRoute): void {
    const campaign = this.lastHub?.campaign
    this.box(21, 16, 54, 26, 'DELIVERY TRAIL')
    this.text(27, 23, `${biomeName[route.biome]} — stage 01/04`, colors.gold)
    this.text(27, 25, `CAMPAIGN: ${campaign?.tierLabel ?? 'BASE'} · DONE ${campaign?.completedLabel ?? 'NONE'}`, colors.text)
    this.text(27, 27, `FIXED: ${campaign?.packageName ?? 'Base Route'}`, colors.gold)
    campaign?.difficultyLines.forEach((line, index) => this.text(27, 28 + index, line, colors.text))
    this.text(27, 32, campaign?.nextLabel ?? 'NEXT: finish BASE to unlock NG+.', campaign?.terminal ? colors.gold : colors.green)
    this.text(27, 35, campaign?.terminal ? 'No active next-tier control.' : 'E / ENTER  travel', campaign?.terminal ? colors.dim : colors.green)
    this.text(27, 38, 'ESC        return to hub', colors.dim)
  }

  private stage(state: RunState): void {
    const preview = state.modal?.kind === 'target' ? targetPreview(state, state.modal) : undefined
    const overlays = mapOverlays(state.floor, preview)
    const boardWidth = MAP_WIDTH * CW
    const boardHeight = MAP_HEIGHT * CH
    if (this.cameraFloor !== state.floor.index) this.recenterCamera()
    const frame = cameraFrame(state.floor, this.camera ?? state.hero, this.boardZoom)
    const focusX = (frame.focus.x + .5) * CW
    const focusY = (frame.focus.y + .5) * CH
    const centerX = boardWidth / 2
    const centerY = boardHeight / 2
    this.ctx.save()
    this.ctx.fillStyle = colors.ink
    this.ctx.fillRect(0, 0, boardWidth, boardHeight)
    this.ctx.beginPath()
    this.ctx.rect(0, 0, boardWidth, boardHeight)
    this.ctx.clip()
    this.ctx.translate(centerX, centerY)
    this.ctx.scale(this.boardZoom, this.boardZoom)
    this.ctx.translate(-focusX, -focusY)
    for (let y = 0; y < state.floor.height; y++) for (let x = 0; x < state.floor.width; x++) this.drawMapCell(state, overlays, x, y)
    if (this.spriteMode) this.drawTelegraphs(state)
    if (this.spriteMode) {
      const animation = state.status === 'dead' || performance.now() < this.heroAnimationUntil ? this.heroAnimation : 'idle'
      drawActorSprite(this.ctx, undefined, true, state.hero.x, state.hero.y, false, this.heroFacingLeft, animation, state.hero.origin)
    }
    else this.cell(state.hero.x, state.hero.y, '@', state.hero.health * 4 < state.hero.maxHealth ? colors.red : colors.text)
    this.effects.drawMap(this.ctx)
    if (this.spriteMode) this.spriteFog(state)
    this.ctx.restore()
    this.ruleVertical(MAP_WIDTH, 0, TERMINAL_HEIGHT)
  }

  private drawMapCell(state: RunState, overlays: MapOverlays, x: number, y: number): void {
    const tile = getTile(state.floor, x, y)!
    const index = mapCellIndex(state.floor, x, y)
    const item = overlays.items[index]
    const prop = overlays.props[index]
    if (!tile.explored) {
      if (!this.spriteMode) this.cell(x, y, ' ', colors.ink, colors.ink)
      if (!this.spriteMode && isItemVisible(tile, item)) this.drawItem(item!, x, y)
      const milestone = state.floor.milestones.find(current => current.x === x && current.y === y && current.discovered && !current.claimed)
      if (milestone && !this.spriteMode) this.cell(x, y, milestone.kind === 'waycache' ? 'W' : milestone.kind === 'augment' ? '!' : milestone.kind === 'relic' ? 'R' : this.runeMode ? '✦' : '*', milestone.kind === 'waycache' || milestone.kind === 'relic' ? colors.gold : milestone.kind === 'augment' ? colors.red : colors.purple)
      return
    }
    const telegraph = overlays.telegraphs[index]
    const previewPath = Boolean(overlays.previewPath[index])
    const previewCell = Boolean(overlays.previewCells[index])
    if (this.spriteMode) {
      drawTileSprite(this.ctx, tile, state.area ?? state.floor.biome, x, y, false, !tile.visible)
      if (tile.flow) this.drawFlowMarker(tile, x, y)
    } else if (this.runeMode) {
      this.drawRuneTile(state.area ?? state.floor.biome, tile.kind, tile.visible, x, y)
      if (tile.flow) this.drawFlowMarker(tile, x, y)
    }
    else {
      const [baseGlyph, color] = tileGlyph[tile.kind]
      const terrain = terrainVisual(state.area ?? state.floor.biome, tile.kind, 'ascii')
      const showDirection = showMotionAt(performance.now())
      const glyph = terrain.glyph ?? terminalTileGlyph(tile.kind, baseGlyph)
      this.cell(x, y, tile.flow && showDirection ? flowGlyph(tile.flow.direction, 'ascii') : glyph, tile.visible ? tile.flow?.hazard ? colors.red : terrain.color ?? color : colors.dim, tile.visible ? terrain.background ?? (tile.kind === 'pit' ? colors.ink : undefined) : undefined)
    }
    if (!tile.visible) {
      if (isItemVisible(tile, item) && !this.spriteMode) this.drawItem(item!, x, y)
      return
    }
    if (this.spriteMode && (tile.kind === 'fireVent' || tile.kind === 'gas')) drawEffectSprite(this.ctx, tile.kind === 'fireVent' ? 'fire' : 'smokeGas', x, y, Math.floor(performance.now() / 120) % 4)
    if (this.spriteMode && (previewPath || previewCell)) {
      this.ctx.fillStyle = previewCell ? '#bea6ff90' : '#8fb8ed70'
      this.ctx.fillRect(x * CW, y * CH, CW, CH)
    }
    if (prop) {
      if (this.spriteMode) drawPropSprite(this.ctx, prop, x, y, false)
      else {
        const definition = propDefinition(prop.kind)
        const glyph = prop.kind === 'mine.brokenCart' ? definition.glyph : prop.kind === 'mine.lanternPost' && prop.state === 'activated' ? '*' : prop.state === 'activated' ? '+' : prop.state === 'inspected' ? '?' : definition.glyph
        this.cell(x, y, this.runeMode ? glyph : terminalGlyph(glyph), definition.color)
      }
    }
    const milestone = state.floor.milestones.find(current => current.x === x && current.y === y && current.discovered && !current.claimed)
    if (milestone) this.cell(x, y, milestone.kind === 'waycache' ? 'W' : milestone.kind === 'augment' ? '!' : milestone.kind === 'relic' ? 'R' : this.runeMode ? '✦' : '*', milestone.kind === 'waycache' || milestone.kind === 'relic' ? colors.gold : milestone.kind === 'augment' ? colors.red : colors.purple)
    const secret = state.floor.secretRooms?.find(room => room.discovery && room.entries.some(entry => entry.x === x && entry.y === y))
    if (secret) this.cell(x, y, '?', colors.gold)
    const shortcut = state.floor.secretRoutes?.find(route => route.kind === 'rare-transition' && route.entry.x === x && route.entry.y === y && state.floor.secretRooms?.find(room => room.id === route.roomId)?.discovery)
    if (shortcut) this.cell(x, y, '>', colors.green)
    if (state.shortcutReturn && state.floor.start.x === x && state.floor.start.y === y) this.cell(x, y, '<', colors.green)
    const encounter = state.floor.encounters?.find(current => current.x === x && current.y === y && current.state === 'dormant')
    if (encounter && !this.spriteMode) this.cell(x, y, encounter.kind === 'wayfarer' ? '&' : encounter.kind === 'bloodBargain' ? '$' : '≈', encounter.kind === 'bloodBargain' ? colors.red : encounter.kind === 'shiftingChamber' ? colors.blue : colors.green)
    if (item) this.drawItem(item, x, y)
    const actor = visibleMapActor(state.floor, overlays.actors[index])
    if (actor) this.spriteMode ? drawActorSprite(this.ctx, actor, false, x, y) : this.cell(x, y, this.runeMode ? actor.glyph : terminalGlyph(actor.glyph), actor.color)
    if (telegraph && !this.spriteMode) {
      const presentation = presentTelegraph(telegraph, state.turn, '')
      this.cell(x, y, presentation.glyph, presentation.color)
    }
    if (!this.spriteMode && (previewPath || previewCell)) this.cell(x, y, previewCell ? 'X' : '·', previewCell ? colors.purple : colors.blue, this.runeMode ? previewCell ? '#2a203d' : '#182842' : undefined)
  }

  private drawFlowMarker(tile: Tile, x: number, y: number): void {
    if (!tile.flow || !showMotionAt(performance.now())) return
    this.cell(x, y, flowGlyph(tile.flow.direction, this.runeMode ? 'runes' : 'ascii'), tile.flow.hazard ? colors.red : '#a8eff0')
  }

  private drawRuneTile(biome: Biome, kind: string, visible: boolean, x: number, y: number): void {
    const [glyph, fore, back] = runeTileGlyph[kind]
    const terrain = terrainVisual(biome, kind as Tile['kind'], 'runes')
    this.cell(x, y, terrain.glyph ?? glyph, visible ? terrain.color ?? fore : shade(terrain.color ?? fore), visible ? terrain.background ?? back : shade(terrain.background ?? back))
  }

  private drawItem(item: GroundItem, x: number, y: number, clip = false): void {
    if (this.spriteMode) drawItemSprite(this.ctx, item.id, x, y, clip)
    else this.cell(x, y, item.tool ? toolFor(item.tool).glyph : this.runeMode ? ITEM[item.id]?.glyph ?? '*' : terminalGlyph(ITEM[item.id]?.glyph ?? '*', '*'), item.tool ? colors.green : ITEM[item.id]?.color ?? colors.gold)
  }

  private spriteFog(state: RunState): void {
    this.ctx.save()
    for (let y = 0; y < state.floor.height; y++) for (let x = 0; x < state.floor.width; x++) {
      const tile = getTile(state.floor, x, y)!
      const rect = cellRect(x, y)
      if (!tile.explored) {
        this.ctx.fillStyle = colors.ink
        this.ctx.fillRect(rect.x, rect.y, rect.width, rect.height)
      } else if (!tile.visible) {
        this.ctx.globalAlpha = .62
        this.ctx.fillStyle = colors.ink
        this.ctx.fillRect(rect.x, rect.y, rect.width, rect.height)
        this.ctx.globalAlpha = 1
      }
    }
    this.ctx.restore()
    for (const item of state.floor.items) {
      const tile = getTile(state.floor, item.x, item.y)
      if (tile && !tile.visible && isItemVisible(tile, item)) this.drawItem(item, item.x, item.y, true)
    }
  }

  private drawTelegraphs(state: RunState): void {
    for (const telegraph of state.floor.telegraphs ?? []) {
      const source = state.floor.actors.find(actor => actor.id === telegraph.sourceId)
      const visibleCells = telegraph.cells.filter(cell => getTile(state.floor, cell.x, cell.y)?.visible)
      const beam = telegraphBeam(source, telegraph.cells)
      if (source && beam && getTile(state.floor, source.x, source.y)?.visible && visibleCells.length === telegraph.cells.length) {
        this.drawTelegraphBeam(telegraph.danger, beam)
        this.drawTelegraphReticle(telegraph.danger, beam.at(-1)!)
      } else for (const cell of visibleCells) this.drawTelegraphReticle(telegraph.danger, cell)
    }
  }

  private drawTelegraphBeam(danger: 'minor' | 'major', points: readonly { x: number; y: number }[]): void {
    this.ctx.save()
    this.ctx.strokeStyle = danger === 'major' ? colors.red : colors.gold
    this.ctx.lineWidth = 2
    this.ctx.lineCap = 'round'
    this.ctx.setLineDash([2, 3])
    this.ctx.beginPath()
    this.ctx.moveTo((points[0].x + .5) * CW, (points[0].y + .5) * CH)
    for (const point of points.slice(1)) this.ctx.lineTo((point.x + .5) * CW, (point.y + .5) * CH)
    this.ctx.stroke()
    this.ctx.restore()
  }

  private drawTelegraphReticle(danger: 'minor' | 'major', point: { x: number; y: number }): void {
    const left = point.x * CW + 2
    const right = (point.x + 1) * CW - 2
    const top = point.y * CH + 2
    const bottom = (point.y + 1) * CH - 2
    const arm = 3
    this.ctx.save()
    this.ctx.strokeStyle = danger === 'major' ? colors.red : colors.gold
    this.ctx.lineWidth = 2
    this.ctx.beginPath()
    this.ctx.moveTo(left + arm, top); this.ctx.lineTo(left, top); this.ctx.lineTo(left, top + arm)
    this.ctx.moveTo(right - arm, top); this.ctx.lineTo(right, top); this.ctx.lineTo(right, top + arm)
    this.ctx.moveTo(left + arm, bottom); this.ctx.lineTo(left, bottom); this.ctx.lineTo(left, bottom - arm)
    this.ctx.moveTo(right - arm, bottom); this.ctx.lineTo(right, bottom); this.ctx.lineTo(right, bottom - arm)
    this.ctx.stroke()
    this.ctx.restore()
  }

  private sidebar(state: RunState): void {
    const hero = state.hero
    const biome = state.area ?? state.floor.biome
    const identityState: VisualIdentityState = state.floor.actors.some(actor => actor.role === 'guardian' && actor.hostile)
      ? 'climax'
      : state.floor.ecology?.some(ecology => ecology.state !== 'resolved')
        ? 'event'
        : state.floor.actors.some(actor => actor.hostile && getTile(state.floor, actor.x, actor.y)?.visible) || (state.floor.telegraphs ?? []).some(telegraph => isTelegraphVisible(state.floor, telegraph))
          ? 'threat'
          : 'normal'
    this.text(50, 1, 'DELIVERY', colors.gold)
    this.text(50, 2, `${String((state.areaFloor ?? state.floor.index % 4) + 1).padStart(2, '0')}/04 ${biomeName[biome]}`, colors.text)
    this.ruleHorizontal(50, 3, 45)
    this.text(50, 4, visualIdentitySnapshot(biome, identityState).line, colors.dim)
    this.courierSheet(hero)
    const ground = state.floor.items.filter(item => item.x === hero.x && item.y === hero.y)
    this.text(50, 32, 'ON GROUND', colors.gold)
    if (!ground.length) this.text(50, 33, 'none', colors.dim)
    ground.slice(0, 3).forEach((item, index) => this.text(50, 33 + index, `${item.tool ? toolFor(item.tool).glyph : ITEM[item.id]?.glyph ?? '*'} ${item.tool ? toolFor(item.tool).name : ITEM[item.id]?.name ?? item.id}${item.count > 1 ? ` ×${item.count}` : ''}`, item.tool ? colors.green : ITEM[item.id]?.color ?? colors.text))
    this.text(50, 37, 'VISIBLE THREATS', colors.gold)
    const foes = state.floor.actors.filter(actor => actor.hostile && getTile(state.floor, actor.x, actor.y)?.visible).sort((a, b) => Math.abs(a.x - hero.x) + Math.abs(a.y - hero.y) - Math.abs(b.x - hero.x) - Math.abs(b.y - hero.y)).slice(0, 3)
    foes.forEach((foe, i) => {
      const y = 38 + i
      this.text(50, y, `${foe.glyph} ${foe.name.slice(0, 18).padEnd(18)}`, foe.color)
      this.meter(71, y, 24, foe.health, foe.maxHealth, foe.color)
    })
    const telegraphs = (state.floor.telegraphs ?? []).filter(telegraph => isTelegraphVisible(state.floor, telegraph)).slice(0, Math.max(0, 3 - foes.length))
    telegraphs.forEach((telegraph, i) => {
      const source = state.floor.actors.find(actor => actor.id === telegraph.sourceId)?.name ?? telegraph.sourceId
      const presentation = presentTelegraph(telegraph, state.turn, source)
      this.text(50, 38 + foes.length + i, presentation.label.slice(0, 45), presentation.color)
    })
    if (!foes.length && !telegraphs.length) this.text(50, 38, 'none', colors.dim)
    const readout = fieldReadout(state)
    const objective = state.floor.objective
    this.wrap(readout.lines[0], 45).slice(0, 1).forEach(line => this.text(50, 44, line, objective.status === 'complete' ? colors.green : colors.gold))
    const milestones = state.floor.milestones.filter(current => current.discovered && !current.claimed)
    const secrets = state.floor.secretRooms?.filter(room => room.discovery) ?? []
    const terrain = getTile(state.floor, hero.x, hero.y)
    if (terrain) this.text(50, 45, terrainInspection(biome, terrain.kind), colors.dim)
    this.text(50, 46, `MARKS ${milestones.length} seen · SECRETS ${secrets.length} found`, colors.dim)
    if (state.companions?.length) this.partyHud(state, 48)
    else this.boonRelicLists(hero, 48)
  }

  private courierSheet(hero: Hero): void {
    this.text(50, 5, `HP    ${String(hero.health).padStart(2)}/${String(hero.maxHealth).padStart(2)}`, colors.red)
    this.meter(65, 5, 28, hero.health, hero.maxHealth, colors.red)
    this.text(50, 6, `FOCUS ${String(hero.focus).padStart(2)}/${String(hero.maxFocus).padStart(2)}`, colors.blue)
    this.meter(65, 6, 28, hero.focus, hero.maxFocus, colors.blue)
    this.text(50, 8, `CASH ${String(hero.gold).padStart(4)} B ${hero.bombs} R ${hero.ropes}`, colors.gold)
    this.text(50, 9, `KEYS ${hero.keys}  XP ${hero.xp}  LV ${hero.level}`, colors.text)
    this.text(72, 9, `${hero.name} · ${hero.deathMode === 'checkpoint' ? 'lodge rest' : 'iron trail'}`, colors.dim)
    this.text(50, 11, `STR ${hero.stats.strength}  AGI ${hero.stats.agility}  VIT ${hero.stats.vitality}  INT ${hero.stats.intellect}`, colors.text)
    this.text(50, 13, 'EQUIPMENT', colors.gold)
    for (const [i, slot] of (Object.keys(SLOT_NAMES) as Array<keyof typeof SLOT_NAMES>).entries()) {
      const id = hero.equipment[slot]
      const label = id ? `${ITEM[id].glyph} ${ITEM[id].name}` : '-'
      this.text(50, 14 + i, `${SLOT_NAMES[slot].padEnd(9)} ${label}`, id ? ITEM[id].color : colors.dim)
    }
    this.text(50, 21, 'INVENTORY', colors.gold)
    const inventory = hero.inventory.slice(0, 8)
    if (!inventory.length) this.text(50, 22, 'pack empty', colors.dim)
    inventory.forEach((id, index) => this.text(50, 22 + index, `${index + 1}. ${ITEM[id].glyph} ${ITEM[id].name}`, ITEM[id].color))
    if (hero.inventory.length > inventory.length) this.text(50, 30, `+${hero.inventory.length - inventory.length} more · U/D/T/E`, colors.dim)
    const tools = hero.traversalTools ?? []
    this.text(72, 21, 'RITUAL TOOLS', colors.gold)
    if (!tools.length) this.text(72, 22, 'none · find Waycache', colors.dim)
    tools.forEach((id, index) => {
      const cooldown = toolCooldown(this.lastState!, id)
      this.text(72, 22 + index, `${index + 1}.${toolFor(id).glyph} ${toolFor(id).name.slice(0, 16)} ${cooldown ? `${cooldown}t` : 'READY'}`, cooldown ? colors.dim : colors.green)
    })
  }

  private boonRelicLists(hero: Hero, y: number): void {
    const boons = Object.entries(hero.boons ?? {}).filter((entry): entry is [string, number] => (entry[1] ?? 0) > 0)
    const relics = hero.relics ?? []
    this.text(50, y, 'BOONS', colors.gold)
    this.text(72, y, 'RELICS', colors.gold)
    if (!boons.length) this.text(50, y + 1, 'none', colors.dim)
    boons.slice(0, 3).forEach(([id, rank], index) => {
      const boon = boonFor(id)
      this.text(50, y + 1 + index, `${index + 1}. ${boon.glyph} ${boon.name.slice(0, 12)} R${rank}`, colors.purple)
    })
    if (boons.length > 3) this.text(50, y + 4, `+${boons.length - 3} more`, colors.dim)
    if (!relics.length) this.text(72, y + 1, 'none', colors.dim)
    relics.slice(0, 3).forEach((id, index) => {
      const relic = relicFor(id)
      this.text(72, y + 1 + index, `${index + 1}. ${relic.glyph} ${relic.name.slice(0, 14)}`, colors.gold)
    })
  }

  private partyHud(state: RunState, y: number): void {
    const party = partyHud(state)
    const order = party.order.map((_, index) => index === 0 ? '@' : String(index)).join('>')
    this.text(50, y, `PARTY ${party.controlMode === 'direct' ? 'DIRECT' : 'AUTO'} · ORDER ${order}`, party.controlMode === 'direct' ? colors.green : colors.gold)
    party.entries.slice(0, 11).forEach((entry, index) => {
      const status = entry.status === 'recovering' ? `REC ${state.companions?.find(companion => companion.id === entry.id)?.recoveryFloors ?? 0}F` : entry.status.toUpperCase()
      const health = entry.health ? `HP ${entry.health.current}/${entry.health.maximum}` : status
      const conditions = entry.conditions.map(condition => condition.toUpperCase()).join('/')
      const cooldowns = entry.cooldowns.map(cooldown => `${cooldown.action.slice(0, 3).toUpperCase()}${cooldown.turns}`).join('/')
      const orderMark = entry.focused ? '>' : entry.turnOrder ? String(entry.turnOrder) : '-'
      const role = entry.role === 'pathmaker' ? 'PATH' : entry.role === 'ritualist' ? 'RITE' : entry.role.toUpperCase()
      const line = `${orderMark} ${entry.glyph} ${role.padEnd(5)} ${entry.name.slice(0, 9).padEnd(9)} ${health}${conditions ? ` ${conditions}` : ''}${cooldowns ? ` ${cooldowns}` : ''}`.slice(0, 45)
      this.text(50, y + 1 + index, line, entry.focused ? colors.green : entry.status === 'lost' || entry.status === 'injured' || entry.status === 'recovering' ? colors.dim : entry.color)
    })
  }

  private log(state: RunState): void {
    this.ruleHorizontal(0, 35, 48)
    const lines = state.messages.flatMap((message, messageIndex) => this.wrap(message, 46).map(line => ({ line, color: messageIndex === 0 ? colors.text : colors.dim }))).slice(0, 14)
    lines.forEach((entry, index) => this.text(1, 36 + index, entry.line, entry.color))
    this.ruleHorizontal(0, 50, MAP_WIDTH)
    this.text(1, 52, 'MOVE arrows/IOP K ; , . / numpad · Shift run', colors.dim)
    this.text(1, 53, 'ACT G/U/D/T/E · A skills · S charm · C act', colors.dim)
    this.text(1, 54, 'B bomb · R rope · Y tools · W rewind · Q exit', colors.dim)
    this.text(1, 55, `F ${autoplayModeLabel(this.lastAutoplayMode)} · Shift+F ${autoplayPolicyLabel(this.settings.autoplayPolicy)} · V ${visualModeLabel(this.visualMode)}`, colors.dim)
  }

  private modal(state: RunState, modal: Modal): void {
    this.ctx.fillStyle = '#05070bbb'
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
    if (modal.kind === 'help') return this.help()
    if (modal.kind === 'readout') return this.readout(state)
    if (modal.kind === 'encyclopedia') return this.encyclopedia(state, modal)
    if (modal.kind === 'settings') return this.settingsModal(modal)
    if (modal.kind === 'inventory') return this.inventory(state, modal.mode)
    if (modal.kind === 'skills') return this.skills(state)
    if (modal.kind === 'trailcraft') return this.trailcraft(state)
    if (modal.kind === 'boon') return this.boon(state, modal)
    if (modal.kind === 'augment') return this.augment(state, modal)
    if (modal.kind === 'relic') return this.relic(state, modal)
    if (modal.kind === 'encounter') return this.encounter(state, modal)
    if (modal.kind === 'tool') return this.tool(state, modal)
    if (modal.kind === 'tools') return this.tools(state)
    if (modal.kind === 'pause') return this.pause()
    if (modal.kind === 'shop') return this.shop(state)
    if (modal.kind === 'gate') return this.gate(state, modal)
    if (modal.kind === 'companionCommand') return this.companionCommand(state, modal)
    if (modal.kind === 'target') return this.target(state, modal)
  }

  private help(): void {
    this.box(8, 3, 64, 37, 'FIELD MANUAL')
    const lines = ['Movement: IOP / K ; / , . / or numpad 1-9.', 'Arrows move cardinally. L or numpad-5 rests.', 'Shift-direction runs until interrupted. Alt-direction', 'uses the first ready charm. B chooses bomb direction.', 'G get · U use · D drop · T throw · E equip · X swap.', 'Y opens ritual tools. O toggles overdrive while targeting.', 'W spends a Time Knot to return to an earlier safe position.', 'C opens Waycaches, Boon sites, and build-up moments.', 'R secures rope over a pit. Q exits at a cleared stair.', 'Z opens a no-cost field readout of current options.', 'A opens disciplines. S uses charms. J opens journal.', 'Esc pauses. Save & quit preserves the current turn.', '', 'Press any key to return.']
    lines.forEach((line, i) => this.text(11, 6 + i * 2, line, i === 10 ? colors.gold : colors.text))
  }

  private encyclopedia(state: RunState, modal: Extract<Modal, { kind: 'encyclopedia' }>): void {
    const entries = encyclopediaEntries(state, modal.section)
    const page = Math.min(modal.page ?? 0, Math.max(0, Math.ceil(entries.length / 10) - 1))
    const sections = ['ENEMIES', 'WARNINGS', 'TAGS', 'TRAILS', 'JOURNEYS']
    this.box(8, 4, 64, 34, 'TRAIL JOURNAL')
    this.text(12, 8, sections.map((section, index) => `${index + 1} ${section}`).join('  '), colors.green)
    this.text(12, 10, `${modal.section.toUpperCase()} ${page + 1}/${Math.max(1, Math.ceil(entries.length / 10))}`, colors.gold)
    const visible = entries.slice(page * 10, page * 10 + 10)
    if (!visible.length) this.text(12, 14, 'No discoveries yet.', colors.dim)
    visible.forEach((entry, index) => this.text(12, 14 + index * 2, entry.slice(0, 56), colors.text))
    this.text(12, 34, '1-5 section · [ ] page · Esc/backtick closes', colors.dim)
  }

  private readout(state: RunState): void {
    const report = fieldReadout(state)
    this.box(10, 5, 64, 38, 'FIELD READOUT — NO TURN PASSES')
    report.lines.flatMap(line => this.wrap(line, 54)).slice(0, 13).forEach((line, index) => this.text(14, 9 + index * 2, line, index === 0 ? colors.gold : line.startsWith('THREAT') ? colors.red : line.startsWith('OPTION') ? colors.green : colors.text))
    this.text(14, 39, 'Esc/backtick closes · choose an action on your terms', colors.dim)
  }

  private settingsModal(modal: Extract<Modal, { kind: 'settings' }>): void {
    const page = Math.min(modal.page ?? 0, settingsPageCount() - 1)
    this.box(13, 5, 54, 32, 'SETTINGS')
    this.text(17, 9, `PAGE ${page + 1}/${settingsPageCount()} · reduced flash ${this.settings.reducedFlash ? 'ON' : 'OFF'}`, colors.gold)
    settingChoices(this.settings, page).forEach((choice, index) => {
      const awaiting = choice.kind === 'binding' && modal.awaiting === choice.binding.id
      this.text(17, 12 + index * 2, `${index + 1}. ${choice.label.padEnd(16)} ${awaiting ? 'PRESS A KEY' : choice.value}`, awaiting ? colors.green : colors.text)
    })
    this.text(17, 33, 'number edits · [ ] page · Esc/backtick closes', colors.dim)
  }

  private inventory(state: RunState, mode: string): void {
    this.box(13, 5, 54, 31, `${mode.toUpperCase()} ITEM`)
    if (!state.hero.inventory.length) this.text(18, 10, 'Your pack is empty.', colors.dim)
    state.hero.inventory.forEach((id, i) => { const item = ITEM[id]; this.text(18, 9 + i, `${i + 1}. ${item.glyph} ${item.name.padEnd(26)} ${item.value}g`, item.color) })
    this.text(18, 33, 'number selects · Esc/backtick cancels', colors.dim)
  }

  private skills(state: RunState): void {
    const leveling = state.modal?.kind === 'skills' && state.modal.source === 'level'
    this.box(10, 6, 60, 28, leveling ? `LEVEL ${state.hero.level} — AWAKENING` : 'CHOOSE A DISCIPLINE')
    if (leveling) this.text(14, 10, '✦ TRAIL MARK AWAKENS · MAX HP +1 · HP +4 ✦', colors.gold)
    const choices = skillChoices(state)
    const statColor = { strength: colors.red, agility: colors.green, vitality: colors.blue, intellect: colors.purple }
    choices.forEach((skill, i) => {
      const y = (leveling ? 13 : 11) + i * 5
      this.text(14, y, `${i + 1}. [${skill.stat.slice(0, 3).toUpperCase()}] ${skill.name}`, statColor[skill.stat])
      this.text(17, y + 2, skill.text.slice(0, 47), colors.text)
    })
    if (!choices.length) this.text(14, 20, 'All disciplines are mastered.', colors.gold)
    this.text(14, 31, 'number chooses · Esc/backtick cancels', colors.dim)
  }

  private trailcraft(state: RunState): void {
    this.box(10, 6, 60, 28, 'TRAILCRAFT — CHOOSE ONE')
    this.text(14, 10, '✦ FLOOR CLEARED · SHAPE THE NEXT TRAIL ✦', colors.gold)
    trailcraftChoices(state).forEach((choice, index) => {
      const rank = state.hero.trailcrafts?.[choice.id] ?? 0
      this.text(14, 14 + index * 5, `${index + 1}. ${choice.name.toUpperCase()} ${rank ? `RANK ${rank + 1}` : ''}`, colors.green)
      this.text(18, 16 + index * 5, choice.text, colors.text)
    })
    this.text(14, 30, 'number chooses · Esc/backtick skips', colors.dim)
  }

  private boon(state: RunState, modal: Extract<Modal, { kind: 'boon' }>): void {
    const milestone = state.floor.milestones.find(current => current.id === modal.milestoneId)
    if (!milestone) return
    const offer = rewardOfferFor(state.floor, milestone.rewardKey)
    this.box(10, 6, 60, 28, 'BOON SITE — CHOOSE ONE')
    this.text(14, 10, '✦ DISCOVERED WHILE TRAVERSING · STACKS ENDLESSLY ✦', colors.gold)
    if (offer?.kind === 'boon') this.text(14, 12, `TEACH: ${offer.choices[0].problem} · TEST: ${offer.choices[0].route}/${offer.choices[0].terrain}`.slice(0, 52), colors.dim)
    boonChoices(state, milestone).forEach((choice, index) => {
      const rank = boonRank(state, choice.id)
      const annotation = offer?.kind === 'boon' ? offer.choices[index] : undefined
      this.text(14, 14 + index * 5, `${index + 1}. ${choice.glyph} ${choice.name.toUpperCase()}${rank ? ` · RANK ${rank + 1}` : ''}${annotation ? ` · ${annotation.role.toUpperCase()}` : ''}`, colors.purple)
      this.text(18, 16 + index * 5, choice.text.slice(0, 47), colors.text)
    })
    this.text(14, 30, 'number chooses · Esc/backtick leaves it for later', colors.dim)
  }

  private augment(state: RunState, modal: Extract<Modal, { kind: 'augment' }>): void {
    const milestone = state.floor.milestones.find(current => current.id === modal.milestoneId)
    if (!milestone) return
    this.box(10, 6, 60, 28, 'BUILD-UP MOMENT')
    if (!modal.mode) {
      this.text(14, 10, 'CHANGE THE BUILD YOU HAVE MADE', colors.gold)
      this.text(14, 15, '1. EVOLVE · strengthen an owned Boon engine', colors.green)
      this.text(14, 20, '2. REFORGE · trade one Boon for a related form', colors.purple)
      this.text(14, 25, '3. TRANSMUTE · trade one Boon for rare power', colors.red)
      this.text(14, 30, 'number chooses · Esc/backtick leaves it for later', colors.dim)
      return
    }
    const choices = augmentChoices(state, modal.milestoneId, modal.mode)
    const title = modal.selected?.length ? `${modal.mode.toUpperCase()} RESULT` : `${modal.mode.toUpperCase()} — CHOOSE A BOON`
    this.text(14, 10, title, colors.gold)
    if (!choices.length) this.text(14, 17, 'Claim a Boon site first; this path needs a build to alter.', colors.dim)
    choices.slice(0, 3).forEach((choice, index) => {
      const rank = boonRank(state, choice.id)
      this.text(14, 14 + index * 5, `${index + 1}. ${choice.glyph} ${choice.name.toUpperCase()}${rank ? ` · RANK ${rank}` : ''}`, modal.mode === 'transmute' ? colors.red : colors.purple)
      this.text(18, 16 + index * 5, choice.text.slice(0, 47), colors.text)
    })
    this.text(14, 30, 'number chooses · Esc/backtick leaves it for later', colors.dim)
  }

  private tool(state: RunState, modal: Extract<Modal, { kind: 'tool' }>): void {
    const milestone = state.floor.milestones.find(current => current.id === modal.milestoneId)
    if (!milestone) return
    const offer = rewardOfferFor(state.floor, milestone.rewardKey)
    const tools = state.hero.traversalTools ?? []
    this.box(10, 6, 60, 28, 'WAYCACHE — BIND A RITUAL TOOL')
    if (tools.length >= 2 && modal.replace === undefined) {
      this.text(14, 11, 'LOADOUT FULL · CHOOSE A TOOL TO REPLACE', colors.gold)
      tools.forEach((id, index) => this.text(16, 15 + index * 4, `${index + 1}. ${toolFor(id).glyph} ${toolFor(id).name} · ${toolFor(id).text}`, colors.text))
      this.text(14, 29, 'number chooses slot · Esc/backtick leaves Waycache', colors.dim)
      return
    }
    this.text(14, 10, modal.replace === undefined ? 'CHOOSE A TOOL FOR AN EMPTY SLOT' : `REPLACING SLOT ${(modal.replace ?? 0) + 1}`, colors.gold)
    if (offer?.kind === 'waycache') this.text(14, 11, `TEACH: ${offer.choices[0].problem} · TEST: ${offer.choices[0].route}/${offer.choices[0].terrain}`.slice(0, 52), colors.dim)
    toolChoices(state, milestone).forEach((choice, index) => {
      const annotation = offer?.kind === 'waycache' ? offer.choices[index] : undefined
      this.text(14, 14 + index * 5, `${index + 1}. ${choice.glyph} ${choice.name.toUpperCase()}${annotation ? ` · ${annotation.role.toUpperCase()}` : ''}`, colors.green)
      this.text(18, 16 + index * 5, choice.text.slice(0, 47), colors.text)
      this.text(18, 17 + index * 5, `OVERDRIVE: ${choice.overdrive}`.slice(0, 47), colors.dim)
    })
    this.text(14, 30, 'number binds · Esc/backtick leaves it for later', colors.dim)
  }

  private relic(state: RunState, modal: Extract<Modal, { kind: 'relic' }>): void {
    const milestone = state.floor.milestones.find(current => current.id === modal.milestoneId)
    if (!milestone) return
    const relics = state.hero.relics ?? []
    this.box(10, 6, 60, 28, 'GUARDIAN ECHO — BIND A RELIC')
    if (relics.length >= 3 && modal.replace === undefined) {
      this.text(14, 10, 'RELIC LOADOUT FULL · CHOOSE ONE TO REPLACE', colors.gold)
      relics.forEach((id, index) => this.text(16, 15 + index * 4, `${index + 1}. ${relicFor(id).glyph} ${relicFor(id).name.toUpperCase()} · ${relicFor(id).text}`.slice(0, 48), colors.text))
      this.text(14, 30, 'number chooses slot · Esc/backtick leaves the echo', colors.dim)
      return
    }
    this.text(14, 10, modal.replace === undefined ? 'CHOOSE A RUN-ONLY ACTIVE RELIC' : `REPLACING SLOT ${(modal.replace ?? 0) + 1}`, colors.gold)
    relicChoices(state, milestone).forEach((choice, index) => {
      this.text(14, 14 + index * 5, `${index + 1}. ${choice.glyph} ${choice.name.toUpperCase()}`, colors.gold)
      this.text(18, 16 + index * 5, choice.text.slice(0, 47), colors.text)
    })
    this.text(14, 30, 'number binds · Esc/backtick leaves the echo', colors.dim)
  }

  private encounter(state: RunState, modal: Extract<Modal, { kind: 'encounter' }>): void {
    const source = state.floor.encounters?.find(current => current.id === modal.encounterId)
    if (!source) return
    const title = encounterTitle(source.kind)
    this.box(10, 6, 60, 28, title)
    this.text(14, 10, source.kind === 'wayfarer' ? 'A TRADER OFFERS A ROUTE-BOUND CHOICE' : source.kind === 'bloodBargain' ? 'POWER IS OFFERED WITH A VISIBLE COST' : 'CHANGE THE ROOM; THE CONSEQUENCE STAYS', colors.gold)
    encounterOptions(state, source).forEach((choice, index) => {
      this.text(14, 14 + index * 5, `${index + 1}. ${choice.label}`, choice.available ? colors.green : colors.dim)
      this.text(18, 16 + index * 5, choice.detail.slice(0, 47), choice.available ? colors.text : colors.dim)
    })
    this.text(14, 30, 'number chooses · Esc/backtick leaves for now', colors.dim)
  }

  private tools(state: RunState): void {
    this.box(13, 8, 54, 24, 'RITUAL TOOL LOADOUT')
    const tools = state.hero.traversalTools ?? []
    tools.forEach((id, index) => {
      const definition = toolFor(id)
      const cooldown = toolCooldown(state, id)
      this.text(18, 13 + index * 7, `${index + 1}. ${definition.glyph} ${definition.name.toUpperCase()} · ${cooldown ? `RECOVERS ${cooldown}T` : 'READY'}`, cooldown ? colors.dim : colors.green)
      this.text(21, 15 + index * 7, definition.text, colors.text)
      this.text(21, 16 + index * 7, `OVERDRIVE: ${definition.overdrive}`, colors.dim)
    })
    this.text(18, 28, 'number targets · Esc/backtick cancels', colors.dim)
  }

  private pause(): void {
    this.box(20, 12, 40, 20, 'PAUSE DELIVERY')
    this.text(28, 19, '1 / ENTER  continue', colors.green)
    this.text(28, 22, '2 / Q      save & quit', colors.text)
    if (this.autoplayDiagnostic) this.text(24, 26, `AUTO ${this.autoplayDiagnostic.outcome.toUpperCase()} · ${this.autoplayDiagnostic.reason}`.slice(0, 36), colors.dim)
    this.text(28, 29, 'Esc/backtick continues', colors.dim)
  }

  private shop(state: RunState): void {
    this.box(12, 5, 56, 32, 'TRADER STOCK')
    merchantStock(state).forEach((id, i) => { const item = ITEM[id]; this.text(17, 9 + i * 2, `${i + 1}. ${item.glyph} ${item.name.padEnd(24)} ${item.value} cash`, item.color) })
    this.text(17, 29, `your cash: ${state.hero.gold}`, colors.gold)
    this.text(17, 32, 'number buys · Esc/backtick leaves', colors.dim)
  }

  private gate(state: RunState, modal: Extract<Modal, { kind: 'gate' }>): void {
    const gate = gateForRun(state)
    this.box(8, 6, 64, 35, 'OPEN TRAIL PASSAGE')
    if (!gate) { this.text(12, 10, 'This is the final route. Cross the area to complete the delivery.', colors.gold); return }
    this.text(12, 10, gate.npcOffering, colors.gold)
    const candidates = gateSacrificeCandidates(state)
    const offering = candidates.find(candidate => candidate.id === modal.offeringId)
    const consequence = offering ? gateSacrificeConsequence(state, offering) : undefined
    gateModalLines(gate, modal.choice, modal.confirming, candidates, modal.offeringId, consequence).forEach((line, index) => this.text(12, 14 + index * 3, line, line.startsWith('FINAL') || line.startsWith('CONSEQUENCE') ? colors.red : modal.confirming && line.startsWith('ENTER') ? colors.gold : colors.text))
  }

  private target(state: RunState, modal: Extract<Modal, { kind: 'target' }>): void {
    this.box(16, 15, 48, 14, 'CHOOSE DIRECTION')
    const action = modal.tool ? `${toolFor(modal.tool).name}${modal.overdrive ? ' OVERDRIVE' : ''}` : modal.action === 'bomb' ? 'place bomb' : modal.action === 'spell' ? 'use charm' : modal.action === 'drill' ? 'breach blocked ground' : modal.action === 'glide' ? 'cross hazardous ground' : modal.action === 'grapple' ? 'grapple across a gap' : modal.action === 'bridge' ? 'deploy a bridge' : modal.action === 'dash' ? 'dash through smoke, gas, fire, or current' : modal.action === 'winch' ? 'reel through an obstruction' : 'throw item'
    const preview = targetPreview(state, modal)
    const mutation = preview.mutation
    this.text(21, 20, modal.direction ? mutation ? `${mutation.tool} · ${mutation.ready ? 'READY' : mutation.reason.toUpperCase()}` : `${preview.path.length} path · ${preview.cells.length} cells` : `Use an 8-way direction to ${action}.`, mutation?.ready ? colors.green : colors.text)
    this.text(21, 23, mutation ? `TILES ${mutation.affected.length} · ${mutation.risk ? `RISK ${mutation.risk.toUpperCase()} · ` : ''}${mutation.cooldown ? `RECOVERS ${mutation.cooldown}T` : 'READY'}` : modal.direction ? `${modal.tool ? 'O toggles overdrive · ' : ''}Enter confirms · direction changes preview` : 'Esc/backtick cancels.', colors.dim)
    this.text(21, 26, modal.direction ? `${modal.tool ? 'O toggles overdrive · ' : ''}Enter confirms · direction changes preview` : 'Esc/backtick cancels.', colors.dim)
  }

  private companionCommand(state: RunState, modal: Extract<Modal, { kind: 'companionCommand' }>): void {
    const id = modal.companionIds[modal.index]
    const companion = state.companions?.find(candidate => candidate.id === id)
    this.box(15, 14, 54, 18, 'COMPANION COMMAND')
    if (!companion) { this.text(20, 21, 'Companion record is unavailable.', colors.red); this.text(20, 27, 'ENTER resolves the remaining turn.', colors.green); return }
    const actions = companion.role === 'guard' ? ['intercept', 'protect'] as const : companion.role === 'scout' ? ['observe', 'mark'] as const : companion.role === 'pathmaker' ? ['traverse', 'stabilizeTerrain'] as const : ['ward', 'stabilizeHazard'] as const
    const actor = state.floor.actors.find(candidate => candidate.status?.includes(`companion:${companion.id}`))
    const assessments = actions.map(action => actor ? assessCompanionAbility(state, companion, actor, action) : undefined)
    const actionLine = (index: number): string => {
      const action = actions[index]!
      const assessment = assessments[index]
      return `${index + 1} ${action.toUpperCase()} · ${assessment?.ready ? `READY → ${assessment.targetLabel ?? 'SELF'}` : assessment ? assessment.reason.replaceAll('-', ' ').toUpperCase() : 'ACTOR MISSING'}`.slice(0, 46)
    }
    this.text(20, 18, `${String(modal.index + 1)}/${modal.companionIds.length} · ${companion.name} · ${companion.role.toUpperCase()}`, colors.gold)
    this.text(20, 21, actionLine(0), assessments[0]?.ready ? colors.green : colors.dim)
    this.text(20, 23, actionLine(1), assessments[1]?.ready ? colors.green : colors.dim)
    this.text(20, 25, `${assessments[0]?.effect ?? 'No ability effect'} · range ${assessments[0]?.range ?? 0} · companion cooldown`, colors.text)
    this.text(20, 28, '8-way direction moves · ENTER / L waits · ESC explains cancellation', colors.dim)
  }

  private analysis(analysis: RunAnalysis): void {
    const metrics = analysis.metrics
    const outcome = analysis.outcome === 'complete' ? 'DELIVERY COMPLETE' : analysis.outcome === 'lost' ? 'DELIVERY LOST' : 'DELIVERY SUSPENDED'
    const color = analysis.outcome === 'complete' ? colors.gold : analysis.outcome === 'lost' ? colors.red : colors.blue
    const width = 68
    const height = 42
    const x = Math.floor((TERMINAL_WIDTH - width) / 2)
    const y = Math.floor((TERMINAL_HEIGHT - height) / 2)
    this.box(x, y, width, height, outcome)
    this.text(x + 4, y + 4, `trail ${String(analysis.floor).padStart(2, '0')} · ${biomeName[analysis.biome]} · ${metrics.turns} turns`, color)
    this.text(x + 4, y + 6, 'RUN HISTOGRAMS  ·  FREQUENCY BY VALUE', colors.gold)
    const samples = metrics.samples.length ? metrics.samples : [{ turn: 0, floor: analysis.floor, health: 0, focus: 0, gold: 0, bombs: 0, ropes: 0, kills: 0, damageDealt: 0, damageTaken: 0 }]
    this.histogramPanel(x + 2, y + 8, 'HEALTH', samples.map(sample => sample.health), colors.red)
    this.histogramPanel(x + 24, y + 8, 'FOCUS', samples.map(sample => sample.focus), colors.blue)
    this.histogramPanel(x + 46, y + 8, 'DAMAGE / TURN', this.timeline(samples, sample => sample.damageDealt + sample.damageTaken, true), colors.gold)
    this.text(x + 4, y + 28, 'RUN TOTALS', colors.gold)
    this.text(x + 4, y + 30, `cash +${metrics.goldGained} · xp +${metrics.xpGained} · pickups ${metrics.pickups}`, colors.text)
    this.text(x + 4, y + 31, `kills ${metrics.kills} · dealt ${metrics.damageDealt} · taken ${metrics.damageTaken}`, colors.text)
    this.text(x + 4, y + 32, `bombs ${metrics.bombsUsed} · ropes ${metrics.ropesUsed} · rests ${metrics.actions.rests} · moves ${metrics.actions.moves}`, colors.dim)
    this.text(x + 4, y + 34, 'FLOOR SPLITS', colors.gold)
    metrics.floors.slice(-4).forEach((floor, index) => {
      const splitX = x + (index % 2 === 0 ? 4 : 36)
      const splitY = y + 36 + Math.floor(index / 2) * 2
      this.text(splitX, splitY, `F${String(floor.floor).padStart(2, '0')}  ${floor.turns}T ${floor.kills}K ${floor.damageDealt}/${floor.damageTaken}D ${floor.pickups}L`, colors.text)
    })
    this.text(x + 4, y + 39, 'ANY KEY  continue', colors.green)
  }

  private timeline(samples: readonly RunMetricSample[], value: (sample: RunMetricSample) => number, cumulative: boolean, descending = false): number[] {
    const values: number[] = []
    let previous = value(samples[0])
    for (let bucket = 0; bucket < 20; bucket++) {
      const index = Math.min(samples.length - 1, Math.max(0, Math.ceil(samples.length * (bucket + 1) / 20) - 1))
      const current = value(samples[index])
      values.push(cumulative ? Math.max(0, descending ? previous - current : current - previous) : current)
      previous = current
    }
    return values
  }

  private histogramPanel(x: number, y: number, title: string, values: readonly number[], color: string): void {
    const width = 20
    const graphWidth = width - 4
    const graphHeight = 6
    const data = this.histogramData(values, graphWidth)
    this.box(x, y, width, 18, title)
    this.text(x + 2, y + 3, `${String(data.low).padEnd(8)}${String(data.high).padStart(8)}`, colors.dim)
    this.gridRect(x + 1, y + 4, width - 2, graphHeight + 3)
    const maxCount = Math.max(1, ...data.counts)
    this.ctx.save()
    this.ctx.strokeStyle = color
    this.ctx.lineWidth = 1
    data.counts.forEach((count, index) => {
      if (!count) return
      const height = Math.max(1, Math.round(count / maxCount * graphHeight))
      this.ctx.strokeRect((x + 2 + index) * CW + .5, (y + 12 - height) * CH + .5, CW - 1, height * CH - 1)
    })
    this.ctx.restore()
    this.ruleHorizontal(x + 2, y + 12, graphWidth)
    for (let tick = 0; tick < graphWidth; tick += 3) this.ruleVertical(x + 2 + tick, y + 12, 1)
    this.text(x + 2, y + 14, `NOW ${data.last}`, color)
    this.text(x + 2, y + 15, `PEAK ${data.high} · N ${values.length}`, colors.text)
  }

  private histogramData(values: readonly number[], bins: number): { low: number; high: number; last: number; counts: number[] } {
    const source = values.length ? values : [0]
    const low = Math.min(...source)
    const high = Math.max(...source)
    const counts = Array.from({ length: bins }, () => 0)
    for (const value of source) {
      const index = high === low ? 0 : Math.min(bins - 1, Math.floor((value - low) / (high - low) * bins))
      counts[index]++
    }
    return { low, high, last: source.at(-1) ?? 0, counts }
  }

  private end(state: RunState, won: boolean): void {
    this.ctx.fillStyle = '#05070bdd'
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
    const campaign = this.lastHub?.campaign
    this.box(17, 12, 62, 21, won ? 'DELIVERY COMPLETE' : 'DELIVERY LOST')
    this.text(23, 17, won ? 'The coastal warning reaches the shrine chief.' : 'The road ends for this courier.', won ? colors.gold : colors.red)
    this.text(23, 20, `cash ${state.hero.gold} · depth ${state.floor.index + 1} · level ${state.hero.level}`, colors.text)
    if (won) {
      this.text(23, 22, `CAMPAIGN: ${campaign?.tierLabel ?? 'BASE'} · DONE ${campaign?.completedLabel ?? 'NONE'}`, colors.gold)
      this.text(23, 24, `FIXED: ${campaign?.packageName ?? 'Base Route'}`, colors.text)
      campaign?.difficultyLines.forEach((line, index) => this.text(23, 25 + index, line, colors.text))
      this.text(23, 29, campaign?.nextLabel ?? 'NEXT: finish BASE to unlock NG+.', campaign?.terminal ? colors.gold : colors.green)
    }
    this.text(23, 31, won ? 'N starts a new delivery.' : 'ANY KEY continues the trail.', colors.green)
  }

  private box(x: number, y: number, width: number, height: number, title: string): void {
    this.ctx.fillStyle = colors.panel
    this.ctx.fillRect(x * CW, y * CH, width * CW, height * CH)
    this.gridRect(x, y, width, height)
    this.text(x + 3, y + 1, title, colors.gold)
  }
  private meter(x: number, y: number, width: number, value: number, max: number, color: string): void {
    const fill = Math.max(0, Math.round(width * value / max))
    this.text(x, y, '█'.repeat(fill), color)
    this.text(x + fill, y, '░'.repeat(width - fill), colors.dim)
  }
  private wrap(value: string, width: number): string[] {
    return value.split('\n').flatMap(paragraph => {
      if (!paragraph) return ['']
      const lines: string[] = []
      let line = ''
      for (const word of paragraph.split(' ')) {
        const next = line ? `${line} ${word}` : word
        if (line && next.length > width) { lines.push(line); line = word }
        else line = next
      }
      if (line) lines.push(line)
      return lines
    })
  }
  private ascii(x: number, y: number, value: string, color = colors.text): void { value.split('\n').forEach((line, index) => this.text(x, y + index, line, color)) }
  private text(x: number, y: number, value: string, color = colors.text, background?: string): void { for (let i = 0; i < value.length && x + i < TERMINAL_WIDTH; i++) this.cell(x + i, y, value[i], color, background) }
  private gridRect(x: number, y: number, width: number, height: number): void {
    this.ctx.save(); this.ctx.strokeStyle = colors.border; this.ctx.lineWidth = 1
    this.ctx.strokeRect(x * CW + .5, y * CH + .5, width * CW - 1, height * CH - 1)
    this.ctx.restore()
  }
  private ruleHorizontal(x: number, y: number, width: number): void {
    this.ctx.save(); this.ctx.strokeStyle = colors.border; this.ctx.lineWidth = 1; this.ctx.beginPath()
    this.ctx.moveTo(x * CW, y * CH + .5); this.ctx.lineTo((x + width) * CW, y * CH + .5); this.ctx.stroke(); this.ctx.restore()
  }
  private ruleVertical(x: number, y: number, height: number): void {
    this.ctx.save(); this.ctx.strokeStyle = colors.border; this.ctx.lineWidth = 1; this.ctx.beginPath()
    this.ctx.moveTo(x * CW + .5, y * CH); this.ctx.lineTo(x * CW + .5, (y + height) * CH); this.ctx.stroke(); this.ctx.restore()
  }
  private cell(x: number, y: number, glyph: string, color = colors.text, background?: string): void {
    if (x < 0 || y < 0 || x >= TERMINAL_WIDTH || y >= TERMINAL_HEIGHT) return
    if (background) { this.ctx.fillStyle = background; this.ctx.fillRect(x * CW, y * CH, CW, CH) }
    this.ctx.fillStyle = color
    this.ctx.fillText(glyph, x * CW, y * CH)
  }
}
