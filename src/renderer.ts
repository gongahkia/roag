import { ITEM, biomeName } from './content'
import { autoplayModeLabel, autoplayPolicyLabel } from './autoplay'
import jomonMastheadSource from '../JOMON.md?raw'
import { merchantStock } from './engine/rewards'
import { encyclopediaEntries, gateForArea, gateModalLines, skillChoices, targetPreview, type ActionResult, type HubView, type ScreenRoute, type TargetPreview } from './engine'
import { TerminalEffects } from './renderer/effects'
import { isItemVisible } from './renderer/fog'
import { CELL_HEIGHT as CH, CELL_WIDTH as CW, MAP_HEIGHT, MAP_WIDTH, cellRect } from './renderer/metrics'
import { telegraphBeam } from './renderer/telegraph-overlay'
import { presentTelegraph } from './renderer/telegraphs'
import { animationFrame, isStoryPageComplete, loadingAnimation, storyText, type LoadingState, type StoryState } from './lore'
import { defaultSettings, settingChoices, settingsPageCount, type GameSettings } from './settings'
import { mineSeason } from './season'
import { drawActorSprite, drawEffectSprite, drawItemSprite, drawTileSprite, textureAtlas, type HeroAnimation } from './sprites'
import { SLOT_NAMES, TERMINAL_HEIGHT, TERMINAL_WIDTH, type AutoplayDiagnostic, type AutoplayMode, type Biome, type CourierDraft, type CourierMenuView, type GroundItem, type Modal, type RunAnalysis, type RunMetricSample, type RunState } from './types'
import { visualModeLabel, type VisualMode } from './visual-mode'
import { actorAt, getTile } from './world'

const colors = { back: '#10131d', panel: '#182131', border: '#6f8298', text: '#d6dce8', dim: '#536174', gold: '#f4d26a', red: '#ee6f78', green: '#96d38b', blue: '#8fb8ed', purple: '#d2a4e8', ink: '#05070b' }
const shade = (color: string, amount = .58): string => {
  const match = /^#([0-9a-f]{6})$/i.exec(color)
  if (!match) return colors.dim
  const value = Number.parseInt(match[1], 16)
  const channel = (shift: number) => Math.round(((value >> shift) & 0xff) * (1 - amount)).toString(16).padStart(2, '0')
  return `#${channel(16)}${channel(8)}${channel(0)}`
}
const tileGlyph: Record<string, [string, string]> = {
  wall: ['#', '#7d8792'], floor: ['.', '#586470'], exit: ['>', '#f4d26a'], door: ['+', '#c99f67'], lockedDoor: ['+', '#e9c965'], water: ['~', '#5c9fca'], lava: ['~', '#ec7056'], pit: [' ', '#05070b'], rope: ['|', '#d8ae73'], spikes: ['^', '#d9dce1'], dart: ['>', '#d9dce1'], fireVent: ['^', '#ff855d'], crumble: [',', '#9e856f'], boulder: ['O', '#a7a0a0'], web: ['%', '#d8dce1'], gas: ['*', '#9bc585'], support: ['╫', '#b99b72'], rail: ['=', '#c5b2a0'], rubble: [':', '#8e9298'], bramble: ['"', '#6c9f64'], darkness: ['·', '#30384d'], crate: ['□', '#c69a6b'], chest: ['▣', '#f4d26a'], altar: ['_', '#d2a4e8'], shop: ['$', '#f4d26a'], rescue: ['&', '#8ae0b3']
}
const runeTileGlyph: Record<string, [string, string, string]> = {
  wall: ['▓', '#79879b', '#131925'], floor: ['·', '#4a586b', '#080b12'], exit: ['>', '#f4d26a', '#15130c'], door: ['+', '#d1a66e', '#16110d'], lockedDoor: ['#', '#e9c965', '#17130b'], water: ['~', '#72b7d2', '#0a1621'], lava: ['~', '#f27a60', '#1c0d0b'], pit: [' ', '#202b38', '#030407'], rope: ['║', '#d8ae73', '#17140d'], spikes: ['^', '#d9dce1', '#15181d'], dart: ['>', '#d9dce1', '#15181d'], fireVent: ['^', '#ff855d', '#1b0d0b'], crumble: [',', '#b89a77', '#15110e'], boulder: ['O', '#a7a0a0', '#15171b'], web: ['%', '#d8dce1', '#17181d'], gas: ['*', '#9bc585', '#10170f'], support: ['╫', '#b99b72', '#17130e'], rail: ['╪', '#d7b95f', '#15130d'], rubble: ['░', '#a7afb8', '#11151d'], bramble: ['♧', '#7da56e', '#0e160d'], darkness: ['·', '#47556a', '#080b12'], crate: ['□', '#c69a6b', '#17120d'], chest: ['▣', '#f4d26a', '#1b150b'], altar: ['_', '#d2a4e8', '#17101b'], shop: ['$', '#f4d26a', '#1a150b'], rescue: ['&', '#8ae0b3', '#0d1714']
}
const areaList = (areas: readonly Biome[]): string => areas.map(area => biomeName[area]).join(', ')
const jomonMasthead = jomonMastheadSource.trimEnd()
const jomonMastheadWidth = Math.max(...jomonMasthead.split('\n').map(line => line.length))
const courierOrigins = {
  mineborn: { label: 'MINEBORN', description: 'Raised among rails, stone dust, and the measured weight of a sealed parcel.', stats: { strength: 3, agility: 1, vitality: 3, intellect: 1 } },
  mosswalker: { label: 'MOSSWALKER', description: 'A trail reader who finds sure footing beneath root, rain, and bramble.', stats: { strength: 1, agility: 3, vitality: 3, intellect: 1 } },
  cavernSeeker: { label: 'CAVERN SEEKER', description: 'A lantern scholar who follows echoes through the buried dark.', stats: { strength: 1, agility: 2, vitality: 2, intellect: 3 } }
} as const
const courierCallings = {
  trailguard: { label: 'TRAILGUARD', description: 'Carry a woven guard and hold the route when the trail closes in.', kit: 'Courier Cord · Woven Guard · tonic' },
  pathmaker: { label: 'PATHMAKER', description: 'Carry extra fire-ash and rope to force a way through bad ground.', kit: 'Courier Cord · 6 bombs · 6 ropes · map' },
  spiritbearer: { label: 'SPIRITBEARER', description: 'Begin with a focus tonic and a Sight Charm for the unseen route.', kit: 'Courier Cord · focus tonic · Sight Charm' }
} as const

export class TerminalRenderer {
  private readonly ctx: CanvasRenderingContext2D
  private readonly effects = new TerminalEffects(CW, CH, MAP_WIDTH, MAP_HEIGHT)
  private spriteMode = false
  private runeMode = false
  private heroFacingLeft = false
  private heroAnimation: HeroAnimation = 'idle'
  private heroAnimationUntil = 0
  private boardZoom = 1
  private lastRoute: ScreenRoute = { screen: 'title', biome: 'mine' }
  private lastState?: RunState
  private lastRecords?: { bestDepth: number; wins: number; deaths: number }
  private lastHub?: HubView
  private lastStory?: StoryState
  private lastLoading?: LoadingState
  private lastAnalysis?: RunAnalysis
  private lastCourierMenu?: CourierMenuView
  private lastCourierDraft?: CourierDraft
  private lastAutoplayMode: AutoplayMode = 'off'
  private autoplayDiagnostic?: AutoplayDiagnostic
  private settings: GameSettings = defaultSettings()

  constructor(private readonly canvas: HTMLCanvasElement) {
    const ctx = canvas.getContext('2d')
    if (!ctx) throw new Error('Canvas 2D unavailable')
    this.ctx = ctx
    canvas.width = TERMINAL_WIDTH * CW
    canvas.height = TERMINAL_HEIGHT * CH
    ctx.imageSmoothingEnabled = false
    ctx.font = '12px BigBlueTerm'
    ctx.textBaseline = 'top'
    void document.fonts.ready.then(() => this.render(this.lastRoute, this.lastState, this.lastRecords, this.lastHub, this.lastStory, this.lastLoading, this.lastAnalysis, this.lastCourierMenu, this.lastCourierDraft, this.lastAutoplayMode))
    textureAtlas.onReady(() => this.render(this.lastRoute, this.lastState, this.lastRecords, this.lastHub, this.lastStory, this.lastLoading, this.lastAnalysis, this.lastCourierMenu, this.lastCourierDraft, this.lastAutoplayMode))
  }

  setVisualMode(value: VisualMode): void {
    this.spriteMode = value === 'sprites'
    this.runeMode = value === 'runes'
    if (!this.spriteMode) { this.heroAnimation = 'idle'; this.heroAnimationUntil = 0 }
  }
  setHeroFacingLeft(value: boolean): void { this.heroFacingLeft = value }
  setBoardZoom(value: number): void {
    this.boardZoom = Math.max(.5, Math.min(5, value))
  }
  setSettings(settings: GameSettings): void { this.settings = settings; this.effects.setReducedFlash(settings.reducedFlash) }
  setAutoplayDiagnostic(value: AutoplayDiagnostic | undefined): void { this.autoplayDiagnostic = value }
  get visualMode(): VisualMode { return this.spriteMode ? 'sprites' : this.runeMode ? 'runes' : 'ascii' }
  trigger(events: ActionResult, state?: RunState, effectId?: string): void {
    const now = performance.now()
    if (events.some(event => event.type === 'death')) { this.heroAnimation = 'death'; this.heroAnimationUntil = Number.POSITIVE_INFINITY }
    else if (events.some(event => event.type === 'hurt')) { this.heroAnimation = 'hit'; this.heroAnimationUntil = now + 320 }
    else if (events.some(event => event.type === 'hit')) { this.heroAnimation = 'attack'; this.heroAnimationUntil = now + 360 }
    else if (events.some(event => event.type === 'move')) { this.heroAnimation = 'walk'; this.heroAnimationUntil = now + 280 }
    this.effects.trigger(events, state, this.canvas, effectId)
  }

  render(route: ScreenRoute, state: RunState | undefined, records?: { bestDepth: number; wins: number; deaths: number }, hub?: HubView, story?: StoryState, loading?: LoadingState, analysis?: RunAnalysis, courierMenu?: CourierMenuView, courierDraft?: CourierDraft, autoplayMode: AutoplayMode = 'off'): void {
    this.lastRoute = route
    this.lastState = state
    this.lastRecords = records
    this.lastHub = hub
    this.lastStory = story
    this.lastLoading = loading
    this.lastAnalysis = analysis
    this.lastCourierMenu = courierMenu
    this.lastCourierDraft = courierDraft
    this.lastAutoplayMode = autoplayMode
    const now = performance.now()
    this.effects.update(now)
    this.ctx.setTransform(1, 0, 0, 1, 0, 0)
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height)
    this.ctx.save()
    this.effects.applyShake(this.ctx, now)
    this.ctx.fillStyle = colors.back
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
    if (route.screen === 'splash') this.splash(courierMenu)
    else if (route.screen === 'title') this.title(courierMenu)
    else if (route.screen === 'createCourier' && courierDraft) this.createCourier(courierDraft)
    else if (route.screen === 'approach') this.approach(route, story, now)
    else if (route.screen === 'hub') this.hub(route, hub)
    else if (route.screen === 'area') this.area(route)
    else if (route.screen === 'loading') this.loading(state, loading, now)
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
    this.ctx.restore()
    this.effects.drawFlash(this.ctx, this.canvas, now)
    if (this.effects.needsFrame(now) || (this.spriteMode && route.screen === 'level' && state) || route.screen === 'loading' || Boolean(story)) requestAnimationFrame(() => this.render(this.lastRoute, this.lastState, this.lastRecords, this.lastHub, this.lastStory, this.lastLoading, this.lastAnalysis, this.lastCourierMenu, this.lastCourierDraft, this.lastAutoplayMode))
  }

  private title(menu?: CourierMenuView): void { this.splash(menu) }

  private splash(menu?: CourierMenuView): void {
    this.box(8, 5, 80, 50, '')
    this.ascii(Math.floor((TERMINAL_WIDTH - jomonMastheadWidth) / 2), 9, jomonMasthead, colors.text)
    const entries = menu?.entries ?? []
    this.text(18, 23, 'WHICH COURIER SHALL YOU PLAY?', colors.text)
    if (!entries.length) this.text(18, 26, '(No active couriers. Please create a new one.)', colors.dim)
    else entries.slice(0, 8).forEach((entry, index) => {
      const selected = entry.id === menu?.selectedId
      const marker = selected ? '>' : ' '
      const mode = entry.deathMode === 'checkpoint' ? 'checkpoint' : 'iron trail'
      this.text(18, 26 + index * 2, `${marker} ${entry.name.padEnd(16)} ${entry.origin.padEnd(14)} ${entry.calling.padEnd(14)} ${mode}`, selected ? colors.green : colors.text)
    })
    if (entries.length) {
      const selected = entries.find(entry => entry.id === menu?.selectedId) ?? entries[0]
      const status = selected.floor ? `trail ${String(selected.floor).padStart(2, '0')} · turn ${selected.turn ?? 0}` : 'at the village outpost'
      this.text(18, 43, `${selected.name} waits in ${selected.area ? biomeName[selected.area] : 'the village'} · ${status}.`, colors.dim)
    }
    const controls = entries.length
      ? ['[L]/ENTER  resume · [↑↓]  change selection', '[N]  create courier · [D]  delete courier']
      : ['[N]  create courier']
    const controlsY = controls.length === 2 ? 48 : 51
    controls.forEach((line, index) => this.text(8 + Math.floor((80 - line.length) / 2), controlsY + index * 3, line, colors.text))
    if (menu?.confirmingDelete) this.box(27, 27, 42, 9, 'RETIRE COURIER'), this.text(31, 31, 'D confirms · ESC cancels', colors.red)
  }

  private createCourier(draft: CourierDraft): void {
    const origin = courierOrigins[draft.origin]
    const calling = courierCallings[draft.calling]
    const death = draft.deathMode === 'checkpoint' ? ['CHECKPOINT', 'Death restores the last cleared floor.'] : ['IRON TRAIL', 'Death ends this courier\'s delivery.']
    this.box(6, 3, 84, 53, 'CREATE COURIER')
    this.text(10, 7, 'Out of the forgotten trail, a courier answers the village call...', colors.text)
    this.creatorField(10, 11, 'NAME', draft.name || 'Unnamed Courier', draft.focus === 0)
    this.creatorField(10, 17, 'ORIGIN', origin.label, draft.focus === 1)
    this.creatorField(10, 29, 'CALLING', calling.label, draft.focus === 2)
    this.creatorField(10, 41, 'DEATH', death[0], draft.focus === 3)
    this.wrap(origin.description, 43).slice(0, 4).forEach((line, index) => this.text(40, 17 + index, line, colors.text))
    this.text(40, 22, 'STATS', colors.gold)
    ;(['strength', 'agility', 'vitality', 'intellect'] as const).forEach((stat, index) => {
      const value = origin.stats[stat]
      this.text(40, 24 + index, `${stat.slice(0, 3).toUpperCase()} ${value}  ${'█'.repeat(value)}${'░'.repeat(4 - value)}`, colors.text)
    })
    this.wrap(calling.description, 43).slice(0, 4).forEach((line, index) => this.text(40, 30 + index, line, colors.text))
    this.text(40, 35, `KIT  ${calling.kit}`, colors.gold)
    this.text(40, 41, death[1], colors.text)
    this.text(10, 51, 'TAB next field · ←→ choose · A-Z/DEL name · ENTER create · ESC cancel', colors.dim)
  }

  private creatorField(x: number, y: number, label: string, value: string, focus: boolean): void {
    this.text(x, y, label, colors.gold)
    this.text(x, y + 2, `${focus ? '>' : ' '} ${value}`, focus ? colors.green : colors.text)
  }

  private approach(route: ScreenRoute, story: StoryState | undefined, now: number): void {
    const season = mineSeason(route.heirSeed ?? 0)
    const width = 54
    const height = 24
    const x = Math.floor((TERMINAL_WIDTH - width) / 2)
    const y = Math.floor((TERMINAL_HEIGHT - height) / 2)
    this.box(x, y, width, height, story?.scene.title ?? 'VILLAGE TRAILHEAD')
    if (story) {
      this.text(x + 6, y + 4, `${String(story.page + 1).padStart(2, '0')}/${String(story.scene.pages.length).padStart(2, '0')}`, season.color)
      this.wrap(storyText(story, now), 42).slice(0, 6).forEach((line, index) => this.text(x + 6, y + 7 + index * 2, line, colors.text))
      this.text(x + 6, y + 21, isStoryPageComplete(story, now) ? 'ANY KEY  continue · SPACE  skip' : 'ANY KEY  reveal · SPACE  skip', colors.green)
      return
    }
    this.text(x + 6, y + 5, season.name.toUpperCase(), season.color)
    this.text(x + 6, y + 8, 'Your village entrusts you with a sealed parcel.', colors.text)
    this.text(x + 6, y + 10, season.scene, colors.text)
    this.text(x + 6, y + 12, 'ENTER  continue to village outpost', colors.green)
    this.text(x + 6, y + 14, 'ESC    return to title', colors.dim)
  }

  private loading(state: RunState | undefined, loading: LoadingState | undefined, now: number): void {
    if (loading?.phase === 'fade' && state) {
      this.stage(state)
      this.sidebar(state)
      this.log(state)
      this.end(state, false)
      this.ctx.save()
      this.ctx.globalAlpha = Math.min(1, Math.max(0, (now - loading.startedAt) / 350))
      this.ctx.fillStyle = '#05070b'
      this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
      this.ctx.restore()
      return
    }
    this.ctx.fillStyle = '#05070b'
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
    this.ascii(33, 19, animationFrame(loadingAnimation, now), colors.gold)
  }

  private hub(route: ScreenRoute, hub?: HubView): void {
    const width = 54
    const height = 24
    const x = Math.floor((TERMINAL_WIDTH - width) / 2)
    const y = Math.floor((TERMINAL_HEIGHT - height) / 2)
    this.box(x, y, width, height, 'VILLAGE OUTPOST')
    this.text(x + 6, y + 4, `COURIER: ${hub?.heirName ?? 'Unassigned'}`, colors.gold)
    this.text(x + 6, y + 6, 'PARCEL: sealed — do not open', colors.text)
    this.text(x + 6, y + 8, `COMPANIONS: ${hub?.state.rescued.length ? hub.state.rescued.map(npc => npc.name).join(', ') : 'none'}`, colors.text)
    const action = route.hubAction ?? 'routes'
    this.text(x + 6, y + 10, `H trails  R companions  S supplies  [${action.toUpperCase()}]`, colors.green)
    if (action === 'routes') this.text(x + 6, y + 12, `OPEN: ${areaList(hub?.state.unlockedAreas ?? ['mine'])}`, colors.text)
    if (action === 'roster') this.text(x + 6, y + 12, hub?.state.rescued.length ? hub.state.rescued.map(npc => `${npc.name} (${biomeName[npc.biome]})`).join(', ') : 'No companions have joined you.', colors.text)
    if (action === 'supplies') this.text(x + 6, y + 12, `SUPPLIES: ${(hub?.state.supplies ?? []).join(', ') || 'none'}`, colors.text)
    this.text(x + 6, y + 15, 'A / ENTER  choose a delivery trail', colors.green)
    this.text(x + 6, y + 17, 'ESC        return to title', colors.dim)
  }

  private area(route: ScreenRoute): void {
    this.box(13, 10, 54, 24, 'DELIVERY TRAIL')
    this.text(19, 15, `${biomeName[route.biome]} — stage 01/04`, colors.gold)
    this.text(19, 19, 'E / ENTER  travel', colors.green)
    this.text(19, 22, 'ESC        return to hub', colors.dim)
  }

  private stage(state: RunState): void {
    const preview = state.modal?.kind === 'target' ? targetPreview(state, state.modal) : undefined
    const boardWidth = MAP_WIDTH * CW
    const boardHeight = MAP_HEIGHT * CH
    const focusX = (state.hero.x + .5) * CW
    const focusY = (state.hero.y + .5) * CH
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
    for (let y = 0; y < MAP_HEIGHT; y++) for (let x = 0; x < MAP_WIDTH; x++) this.drawMapCell(state, x, y, preview)
    if (this.spriteMode) this.drawTelegraphs(state)
    if (this.spriteMode) {
      const animation = state.status === 'dead' || performance.now() < this.heroAnimationUntil ? this.heroAnimation : 'idle'
      drawActorSprite(this.ctx, undefined, true, state.hero.x, state.hero.y, false, this.heroFacingLeft, animation)
    }
    else this.cell(state.hero.x, state.hero.y, '@', state.hero.health * 4 < state.hero.maxHealth ? colors.red : colors.text)
    this.effects.drawMap(this.ctx)
    if (this.spriteMode) this.spriteFog(state)
    this.ctx.restore()
    this.ruleVertical(MAP_WIDTH, 0, 50)
  }

  private drawMapCell(state: RunState, x: number, y: number, preview?: TargetPreview): void {
    const tile = getTile(state.floor, x, y)!
    const item = state.floor.items.find(current => current.x === x && current.y === y)
    if (!tile.explored) {
      if (!this.spriteMode) this.cell(x, y, ' ', colors.ink, colors.ink)
      if (!this.spriteMode && isItemVisible(tile, item)) this.drawItem(item!, x, y)
      return
    }
    const telegraph = state.floor.telegraphs?.find(current => current.cells.some(cell => cell.x === x && cell.y === y))
    const previewPath = preview?.path.some(cell => cell.x === x && cell.y === y)
    const previewCell = preview?.cells.some(cell => cell.x === x && cell.y === y)
    if (this.spriteMode) drawTileSprite(this.ctx, tile, state.area ?? state.floor.biome, x, y, false, !tile.visible)
    else if (this.runeMode) this.drawRuneTile(tile.kind, tile.visible, x, y)
    else {
      const [glyph, color] = tileGlyph[tile.kind]
      this.cell(x, y, glyph, tile.visible ? color : colors.dim, tile.kind === 'pit' ? colors.ink : undefined)
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
    if (item) this.drawItem(item, x, y)
    const actor = actorAt(state.floor, x, y)
    if (actor) this.spriteMode ? drawActorSprite(this.ctx, actor, false, x, y) : this.cell(x, y, actor.glyph, actor.color)
    if (telegraph && !this.spriteMode) {
      const presentation = presentTelegraph(telegraph, state.turn, '')
      this.cell(x, y, presentation.glyph, presentation.color)
    }
    if (!this.spriteMode && (previewPath || previewCell)) this.cell(x, y, previewCell ? 'X' : '·', previewCell ? colors.purple : colors.blue, this.runeMode ? previewCell ? '#2a203d' : '#182842' : undefined)
  }

  private drawRuneTile(kind: string, visible: boolean, x: number, y: number): void {
    const [glyph, fore, back] = runeTileGlyph[kind]
    this.cell(x, y, glyph, visible ? fore : shade(fore), visible ? back : shade(back))
  }

  private drawItem(item: GroundItem, x: number, y: number, clip = false): void {
    if (this.spriteMode) drawItemSprite(this.ctx, item.id, x, y, clip)
    else this.cell(x, y, ITEM[item.id]?.glyph ?? '*', ITEM[item.id]?.color ?? colors.gold)
  }

  private spriteFog(state: RunState): void {
    this.ctx.save()
    for (let y = 0; y < MAP_HEIGHT; y++) for (let x = 0; x < MAP_WIDTH; x++) {
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
    this.text(50, 1, 'DELIVERY', colors.gold)
    this.text(50, 2, `${String((state.areaFloor ?? state.floor.index % 4) + 1).padStart(2, '0')}/04 ${biomeName[state.area ?? state.floor.biome]}`, colors.text)
    this.ruleHorizontal(50, 3, 45)
    this.text(50, 5, `HP    ${String(hero.health).padStart(2)}/${String(hero.maxHealth).padStart(2)}`, colors.red)
    this.meter(65, 5, 28, hero.health, hero.maxHealth, colors.red)
    this.text(50, 6, `FOCUS ${String(hero.focus).padStart(2)}/${String(hero.maxFocus).padStart(2)}`, colors.blue)
    this.meter(65, 6, 28, hero.focus, hero.maxFocus, colors.blue)
    this.text(50, 8, `CASH ${String(hero.gold).padStart(4)} B ${hero.bombs} R ${hero.ropes}`, colors.gold)
    this.text(50, 9, `KEYS ${hero.keys}  XP ${hero.xp}  LV ${hero.level}`, colors.text)
    this.text(72, 9, `${hero.name} · ${hero.deathMode === 'checkpoint' ? 'checkpoint' : 'iron trail'}`, colors.dim)
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
    const ground = state.floor.items.filter(item => item.x === hero.x && item.y === hero.y)
    this.text(50, 32, 'ON GROUND', colors.gold)
    if (!ground.length) this.text(50, 33, 'none', colors.dim)
    ground.slice(0, 3).forEach((item, index) => this.text(50, 33 + index, `${ITEM[item.id]?.glyph ?? '*'} ${ITEM[item.id]?.name ?? item.id}${item.count > 1 ? ` ×${item.count}` : ''}`, ITEM[item.id]?.color ?? colors.text))
    this.text(50, 37, 'VISIBLE THREATS', colors.gold)
    const foes = state.floor.actors.filter(actor => actor.hostile && getTile(state.floor, actor.x, actor.y)?.visible).sort((a, b) => Math.abs(a.x - hero.x) + Math.abs(a.y - hero.y) - Math.abs(b.x - hero.x) - Math.abs(b.y - hero.y)).slice(0, 3)
    foes.forEach((foe, i) => this.text(50, 38 + i, `${foe.glyph} ${foe.name.slice(0, 33).padEnd(33)} ${Math.max(0, foe.health)}`, foe.color))
    state.floor.telegraphs?.slice(0, 2).forEach((telegraph, i) => {
      const source = state.floor.actors.find(actor => actor.id === telegraph.sourceId)?.name ?? telegraph.sourceId
      const presentation = presentTelegraph(telegraph, state.turn, source)
      this.text(50, 41 + i, presentation.label.slice(0, 45), presentation.color)
    })
    const objective = state.floor.objective
    this.wrap(`OBJECTIVE: ${objective.status === 'complete' ? 'DONE — ' : ''}${objective.label}`, 45).slice(0, 2).forEach((line, index) => this.text(50, 44 + index, line, objective.status === 'complete' ? colors.green : colors.gold))
    this.text(50, 47, 'G get · U use · C act · T throw', colors.dim)
    this.text(50, 48, `B bomb · R rope · V ${visualModeLabel(this.visualMode)} · F ${autoplayModeLabel(this.lastAutoplayMode)}`, colors.dim)
  }

  private log(state: RunState): void {
    this.ruleHorizontal(0, 35, 48)
    const lines = state.messages.flatMap((message, messageIndex) => this.wrap(message, 46).map(line => ({ line, color: messageIndex === 0 ? colors.text : colors.dim }))).slice(0, 14)
    lines.forEach((entry, index) => this.text(1, 36 + index, entry.line, entry.color))
    this.ruleHorizontal(0, 50, 96)
    this.text(1, 52, `IOP/K;/,./ + numpad: 8-way · Shift: run · Alt: cast · L: rest · F: autoplay · Shift+F: ${autoplayPolicyLabel(this.settings.autoplayPolicy)} · ESC: pause`, colors.dim)
    this.text(1, 54, `seed ${state.seed} · floor seed ${state.floor.seed} · turn ${state.turn}`, colors.dim)
  }

  private modal(state: RunState, modal: Modal): void {
    this.ctx.fillStyle = '#05070bbb'
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
    if (modal.kind === 'help') return this.help()
    if (modal.kind === 'encyclopedia') return this.encyclopedia(state, modal)
    if (modal.kind === 'settings') return this.settingsModal(modal)
    if (modal.kind === 'inventory') return this.inventory(state, modal.mode)
    if (modal.kind === 'skills') return this.skills(state)
    if (modal.kind === 'pause') return this.pause()
    if (modal.kind === 'shop') return this.shop(state)
    if (modal.kind === 'gate') return this.gate(state, modal)
    if (modal.kind === 'target') return this.target(state, modal)
  }

  private help(): void {
    this.box(8, 3, 64, 37, 'FIELD MANUAL')
    const lines = ['Movement: IOP / K ; / , . / or numpad 1-9.', 'Arrows move cardinally. L or numpad-5 rests.', 'Shift-direction runs until interrupted. Alt-direction', 'uses the first ready charm. B chooses bomb direction.', 'G get · U use · D drop · T throw · E equip · X swap.', 'C operates doors, traders, travelers, and shrines.', 'R secures rope over a pit. Q exits at a cleared stair.', 'A opens disciplines. S uses charms. J opens journal.', 'Combat uses attack rolls, defense, gear, stats, and XP.', 'Esc pauses. Save & quit preserves the current turn.', '', 'Press any key to return.']
    lines.forEach((line, i) => this.text(11, 6 + i * 2, line, i === 9 ? colors.gold : colors.text))
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
    const gate = gateForArea(state.area ?? state.floor.biome)
    this.box(8, 6, 64, 29, 'OPEN TRAIL PASSAGE')
    this.text(12, 10, gate.npcOffering, colors.gold)
    gateModalLines(gate, modal.choice, modal.confirming).forEach((line, index) => this.text(12, 14 + index * 3, line, line.startsWith('FINAL') ? colors.red : modal.confirming && line.startsWith('ENTER') ? colors.gold : colors.text))
  }

  private target(state: RunState, modal: Extract<Modal, { kind: 'target' }>): void {
    this.box(16, 16, 48, 12, 'CHOOSE DIRECTION')
    const action = modal.action === 'bomb' ? 'place bomb' : modal.action === 'spell' ? 'use charm' : 'throw item'
    const preview = targetPreview(state, modal)
    this.text(21, 20, modal.direction ? `${preview.path.length} path · ${preview.cells.length} cells` : `Use an 8-way direction to ${action}.`, colors.text)
    this.text(21, 23, modal.direction ? 'Enter confirms · direction changes preview' : 'Esc/backtick cancels.', colors.dim)
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
    this.box(17, 14, 46, 16, won ? 'DELIVERY COMPLETE' : 'DELIVERY LOST')
    this.text(23, 19, won ? 'The sealed parcel reaches its keeper.' : 'The path keeps its due.', won ? colors.gold : colors.red)
    this.text(23, 22, `cash ${state.hero.gold} · depth ${state.floor.index + 1} · level ${state.hero.level}`, colors.text)
    this.text(23, 26, won ? 'N starts a new delivery.' : 'ANY KEY continues the trail.', colors.green)
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
