import { ITEM, biomeName } from './content'
import { merchantStock } from './engine/rewards'
import { encyclopediaEntries, gateForArea, gateModalLines, skillChoices, targetPreview, type ActionResult, type HubView, type ScreenRoute, type TargetPreview } from './engine'
import { TerminalEffects } from './renderer/effects'
import { isItemVisible } from './renderer/fog'
import { telegraphBeam } from './renderer/telegraph-overlay'
import { presentTelegraph } from './renderer/telegraphs'
import { isStoryPageComplete, storyText, type LoadingState, type StoryState } from './lore'
import { defaultSettings, settingChoices, settingsPageCount, type GameSettings } from './settings'
import { mineSeason } from './season'
import { drawActorSprite, drawEffectSprite, drawItemSprite, drawTileSprite, textureAtlas, type HeroAnimation } from './sprites'
import { SLOT_NAMES, TERMINAL_HEIGHT, TERMINAL_WIDTH, type Biome, type GroundItem, type Modal, type RunState } from './types'
import { actorAt, getTile } from './world'

const CW = 10
const CH = 14
const colors = { back: '#10131d', panel: '#182131', border: '#6f8298', text: '#d6dce8', dim: '#536174', gold: '#f4d26a', red: '#ee6f78', green: '#96d38b', blue: '#8fb8ed', purple: '#d2a4e8', ink: '#05070b' }
const tileGlyph: Record<string, [string, string]> = {
  wall: ['#', '#7d8792'], floor: ['.', '#586470'], exit: ['>', '#f4d26a'], door: ['+', '#c99f67'], lockedDoor: ['+', '#e9c965'], water: ['~', '#5c9fca'], lava: ['~', '#ec7056'], pit: [' ', '#05070b'], rope: ['|', '#d8ae73'], spikes: ['^', '#d9dce1'], dart: ['>', '#d9dce1'], fireVent: ['^', '#ff855d'], crumble: [',', '#9e856f'], boulder: ['O', '#a7a0a0'], web: ['%', '#d8dce1'], gas: ['*', '#9bc585'], support: ['╫', '#b99b72'], rail: ['=', '#c5b2a0'], rubble: [':', '#8e9298'], bramble: ['"', '#6c9f64'], darkness: ['·', '#30384d'], crate: ['□', '#c69a6b'], chest: ['▣', '#f4d26a'], altar: ['_', '#d2a4e8'], shop: ['$', '#f4d26a'], rescue: ['&', '#8ae0b3']
}
const areaList = (areas: readonly Biome[]): string => areas.map(area => biomeName[area]).join(', ')

export class TerminalRenderer {
  private readonly ctx: CanvasRenderingContext2D
  private readonly effects = new TerminalEffects(CW, CH, 48, 35)
  private spriteMode = false
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
  private settings: GameSettings = defaultSettings()

  constructor(private readonly canvas: HTMLCanvasElement) {
    const ctx = canvas.getContext('2d')
    if (!ctx) throw new Error('Canvas 2D unavailable')
    this.ctx = ctx
    canvas.width = TERMINAL_WIDTH * CW
    canvas.height = TERMINAL_HEIGHT * CH
    ctx.imageSmoothingEnabled = false
    ctx.font = '14px ui-monospace, SFMono-Regular, Menlo, monospace'
    ctx.textBaseline = 'top'
    textureAtlas.onReady(() => this.render(this.lastRoute, this.lastState, this.lastRecords, this.lastHub, this.lastStory, this.lastLoading))
  }

  setSpriteMode(value: boolean): void { this.spriteMode = value; if (!value) { this.heroAnimation = 'idle'; this.heroAnimationUntil = 0 } }
  setHeroFacingLeft(value: boolean): void { this.heroFacingLeft = value }
  setBoardZoom(value: number): void {
    this.boardZoom = Math.max(.5, Math.min(5, value))
  }
  setSettings(settings: GameSettings): void { this.settings = settings; this.effects.setReducedFlash(settings.reducedFlash) }
  get isSpriteMode(): boolean { return this.spriteMode }
  trigger(events: ActionResult, state?: RunState, effectId?: string): void {
    const now = performance.now()
    if (events.some(event => event.type === 'death')) { this.heroAnimation = 'death'; this.heroAnimationUntil = Number.POSITIVE_INFINITY }
    else if (events.some(event => event.type === 'hurt')) { this.heroAnimation = 'hit'; this.heroAnimationUntil = now + 320 }
    else if (events.some(event => event.type === 'hit')) { this.heroAnimation = 'attack'; this.heroAnimationUntil = now + 360 }
    else if (events.some(event => event.type === 'move')) { this.heroAnimation = 'walk'; this.heroAnimationUntil = now + 280 }
    this.effects.trigger(events, state, this.canvas, effectId)
  }

  render(route: ScreenRoute, state: RunState | undefined, records?: { bestDepth: number; wins: number; deaths: number }, hub?: HubView, story?: StoryState, loading?: LoadingState): void {
    this.lastRoute = route
    this.lastState = state
    this.lastRecords = records
    this.lastHub = hub
    this.lastStory = story
    this.lastLoading = loading
    const now = performance.now()
    this.effects.update(now)
    this.ctx.setTransform(1, 0, 0, 1, 0, 0)
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height)
    this.ctx.save()
    this.effects.applyShake(this.ctx, now)
    this.ctx.fillStyle = colors.back
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
    if (route.screen === 'title') this.title(records)
    else if (route.screen === 'approach') this.approach(route, story, now)
    else if (route.screen === 'hub') this.hub(route, hub)
    else if (route.screen === 'area') this.area(route)
    else if (route.screen === 'loading') this.loading(state, loading, now)
    else if (!state || state.status === 'title') this.title(records)
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
    if (this.effects.needsFrame(now) || (this.spriteMode && route.screen === 'level' && state) || route.screen === 'loading' || Boolean(story && !isStoryPageComplete(story, now))) requestAnimationFrame(() => this.render(this.lastRoute, this.lastState, this.lastRecords, this.lastHub, this.lastStory, this.lastLoading))
  }

  private title(records?: { bestDepth: number; wins: number; deaths: number }): void {
    this.box(13, 10, 54, 24, 'JOMON: SECRET DELIVERY')
    this.text(19, 14, 'CARRY A SEALED PARCEL FOR YOUR VILLAGE', colors.gold)
    this.text(20, 18, 'N  begin a new delivery', colors.green)
    this.text(20, 20, 'L  resume saved floor', colors.text)
    this.text(20, 26, `best depth ${records?.bestDepth ?? 0}  wins ${records?.wins ?? 0}  deaths ${records?.deaths ?? 0}`, colors.dim)
    this.text(22, 30, 'one life · sixteen paths · four regions', colors.purple)
  }

  private approach(route: ScreenRoute, story: StoryState | undefined, now: number): void {
    const season = mineSeason(route.heirSeed ?? 0)
    this.box(13, 10, 54, 24, story?.scene.title ?? 'VILLAGE TRAILHEAD')
    if (story) {
      this.text(19, 14, `${String(story.page + 1).padStart(2, '0')}/${String(story.scene.pages.length).padStart(2, '0')}`, season.color)
      this.wrap(storyText(story, now), 46).slice(0, 6).forEach((line, index) => this.text(19, 17 + index * 2, line, colors.text))
      this.text(19, 31, isStoryPageComplete(story, now) ? 'ANY KEY  continue · SPACE  skip' : 'ANY KEY  reveal · SPACE  skip', colors.green)
      return
    }
    this.text(19, 15, season.name.toUpperCase(), season.color)
    this.text(19, 18, 'Your village entrusts you with a sealed parcel.', colors.text)
    this.text(19, 20, season.scene, colors.text)
    this.text(19, 22, 'ENTER  continue to village outpost', colors.green)
    this.text(19, 24, 'ESC    return to title', colors.dim)
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
    const dots = '.'.repeat(Math.floor(now / 280) % 4)
    this.text(28, 20, 'THREADS OF THE TRAIL', colors.gold)
    this.text(35, 23, `loading${dots}`, colors.dim)
  }

  private hub(route: ScreenRoute, hub?: HubView): void {
    this.box(13, 10, 54, 24, 'VILLAGE OUTPOST')
    this.text(19, 14, `COURIER: ${hub?.heirName ?? 'Unassigned'}`, colors.gold)
    this.text(19, 16, 'PARCEL: sealed — do not open', colors.text)
    this.text(19, 18, `COMPANIONS: ${hub?.state.rescued.length ? hub.state.rescued.map(npc => npc.name).join(', ') : 'none'}`, colors.text)
    const action = route.hubAction ?? 'routes'
    this.text(19, 20, `H trails  R companions  S supplies  [${action.toUpperCase()}]`, colors.green)
    if (action === 'routes') this.text(19, 22, `OPEN: ${areaList(hub?.state.unlockedAreas ?? ['mine'])}`, colors.text)
    if (action === 'roster') this.text(19, 22, hub?.state.rescued.length ? hub.state.rescued.map(npc => `${npc.name} (${biomeName[npc.biome]})`).join(', ') : 'No companions have joined you.', colors.text)
    if (action === 'supplies') this.text(19, 22, `SUPPLIES: ${(hub?.state.supplies ?? []).join(', ') || 'none'}`, colors.text)
    this.text(19, 25, 'A / ENTER  choose a delivery trail', colors.green)
    this.text(19, 27, 'ESC        return to title', colors.dim)
  }

  private area(route: ScreenRoute): void {
    this.box(13, 10, 54, 24, 'DELIVERY TRAIL')
    this.text(19, 15, `${biomeName[route.biome]} — stage 01/04`, colors.gold)
    this.text(19, 19, 'E / ENTER  travel', colors.green)
    this.text(19, 22, 'ESC        return to hub', colors.dim)
  }

  private stage(state: RunState): void {
    const preview = state.modal?.kind === 'target' ? targetPreview(state, state.modal) : undefined
    const boardWidth = 48 * CW
    const boardHeight = 35 * CH
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
    for (let y = 0; y < 35; y++) for (let x = 0; x < 48; x++) this.drawMapCell(state, x, y, preview)
    if (this.spriteMode) this.drawTelegraphs(state)
    if (this.spriteMode) {
      const animation = state.status === 'dead' || performance.now() < this.heroAnimationUntil ? this.heroAnimation : 'idle'
      drawActorSprite(this.ctx, undefined, true, state.hero.x, state.hero.y, false, this.heroFacingLeft, animation)
    }
    else this.cell(state.hero.x, state.hero.y, '@', state.hero.health * 4 < state.hero.maxHealth ? colors.red : colors.text)
    this.effects.drawMap(this.ctx)
    this.ctx.restore()
    this.ruleVertical(48, 0, 35)
  }

  private drawMapCell(state: RunState, x: number, y: number, preview?: TargetPreview): void {
    const tile = getTile(state.floor, x, y)!
    const item = state.floor.items.find(current => current.x === x && current.y === y)
    if (!tile.explored) {
      if (isItemVisible(tile, item)) this.drawItem(item!, x, y)
      else this.cell(x, y, ' ', colors.ink, colors.ink)
      return
    }
    const telegraph = state.floor.telegraphs?.find(current => current.cells.some(cell => cell.x === x && cell.y === y))
    const previewPath = preview?.path.some(cell => cell.x === x && cell.y === y)
    const previewCell = preview?.cells.some(cell => cell.x === x && cell.y === y)
    const [glyph, color] = tileGlyph[tile.kind]
    if (this.spriteMode) drawTileSprite(this.ctx, tile, state.area ?? state.floor.biome, x, y, !tile.visible)
    else this.cell(x, y, glyph, tile.visible ? color : colors.dim, tile.kind === 'pit' ? colors.ink : undefined)
    if (!tile.visible) {
      if (isItemVisible(tile, item)) this.drawItem(item!, x, y)
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
    if (!this.spriteMode && (previewPath || previewCell)) this.cell(x, y, previewCell ? 'X' : '·', previewCell ? colors.purple : colors.blue)
  }

  private drawItem(item: GroundItem, x: number, y: number): void {
    if (this.spriteMode) drawItemSprite(this.ctx, item.id, x, y)
    else this.cell(x, y, ITEM[item.id]?.glyph ?? '*', ITEM[item.id]?.color ?? colors.gold)
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
    this.ruleHorizontal(50, 3, 29)
    this.text(50, 5, `HP    ${String(hero.health).padStart(2)}/${String(hero.maxHealth).padStart(2)}`, colors.red)
    this.meter(64, 5, 14, hero.health, hero.maxHealth, colors.red)
    this.text(50, 6, `FOCUS ${String(hero.focus).padStart(2)}/${String(hero.maxFocus).padStart(2)}`, colors.blue)
    this.meter(64, 6, 14, hero.focus, hero.maxFocus, colors.blue)
    this.text(50, 8, `CASH ${String(hero.gold).padStart(4)} B ${hero.bombs} R ${hero.ropes}`, colors.gold)
    this.text(50, 9, `KEYS ${hero.keys}  XP ${hero.xp}  LV ${hero.level}`, colors.text)
    this.ruleHorizontal(50, 10, 29)
    this.text(50, 12, `STR ${hero.stats.strength}  AGI ${hero.stats.agility}`, colors.text)
    this.text(50, 13, `VIT ${hero.stats.vitality}  INT ${hero.stats.intellect}`, colors.text)
    this.text(50, 15, 'EQUIPMENT', colors.gold)
    for (const [i, slot] of (Object.keys(SLOT_NAMES) as Array<keyof typeof SLOT_NAMES>).entries()) {
      const id = hero.equipment[slot]
      this.text(50, 16 + i, `${SLOT_NAMES[slot].slice(0, 8).padEnd(8)} ${id ? ITEM[id].glyph : '-'}`, id ? ITEM[id].color : colors.dim)
    }
    this.text(50, 23, 'VISIBLE THREATS', colors.gold)
    const foes = state.floor.actors.filter(actor => actor.hostile && getTile(state.floor, actor.x, actor.y)?.visible).sort((a, b) => Math.abs(a.x - hero.x) + Math.abs(a.y - hero.y) - Math.abs(b.x - hero.x) - Math.abs(b.y - hero.y)).slice(0, 3)
    foes.forEach((foe, i) => this.text(50, 24 + i, `${foe.glyph} ${foe.name.slice(0, 19).padEnd(19)} ${Math.max(0, foe.health)}`, foe.color))
    state.floor.telegraphs?.slice(0, 2).forEach((telegraph, i) => {
      const source = state.floor.actors.find(actor => actor.id === telegraph.sourceId)?.name ?? telegraph.sourceId
      const presentation = presentTelegraph(telegraph, state.turn, source)
      this.text(50, 27 + i, presentation.label.slice(0, 29), presentation.color)
    })
    const objective = state.floor.objective
    this.text(50, 29, `OBJECTIVE: ${objective.status === 'complete' ? 'DONE — ' : ''}${objective.label}`.slice(0, 29), objective.status === 'complete' ? colors.green : colors.gold)
    this.text(50, 30, 'G get  U use  C act', colors.dim)
    this.text(50, 31, 'T throw  B bomb  R rope', colors.dim)
    this.text(50, 32, `A skills  J journal  V ${this.spriteMode ? 'ascii' : 'sprites'}`, colors.dim)
  }

  private log(state: RunState): void {
    this.ruleHorizontal(0, 35, 80)
    this.text(1, 36, state.messages[0] ?? '', colors.text)
    this.text(1, 37, state.messages[1] ?? '', colors.dim)
    this.text(1, 38, state.messages[2] ?? '', colors.dim)
    this.text(1, 41, 'IOP/K;/,./ + numpad: 8-way · Shift: run · Alt: cast · L: rest', colors.dim)
    this.text(1, 43, `seed ${state.seed} · floor seed ${state.floor.seed} · turn ${state.turn}`, colors.dim)
  }

  private modal(state: RunState, modal: Modal): void {
    this.ctx.fillStyle = '#05070bbb'
    this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height)
    if (modal.kind === 'help') return this.help()
    if (modal.kind === 'encyclopedia') return this.encyclopedia(state, modal)
    if (modal.kind === 'settings') return this.settingsModal(modal)
    if (modal.kind === 'inventory') return this.inventory(state, modal.mode)
    if (modal.kind === 'skills') return this.skills(state)
    if (modal.kind === 'shop') return this.shop(state)
    if (modal.kind === 'gate') return this.gate(state, modal)
    if (modal.kind === 'target') return this.target(state, modal)
  }

  private help(): void {
    this.box(8, 3, 64, 37, 'FIELD MANUAL')
    const lines = ['Movement: IOP / K ; / , . / or numpad 1-9.', 'Arrows move cardinally. L or numpad-5 rests.', 'Shift-direction runs until interrupted. Alt-direction', 'uses the first ready charm. B chooses bomb direction.', 'G get · U use · D drop · T throw · E equip · X swap.', 'C operates doors, traders, travelers, and shrines.', 'R secures rope over a pit. Q exits at a cleared stair.', 'A opens disciplines. S uses charms. J opens journal.', 'Combat uses attack rolls, defense, gear, stats, and XP.', 'The current floor is saved only when you descend.', '', 'Press any key to return.']
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
    this.box(12, 8, 56, 24, 'CHOOSE A DISCIPLINE')
    const choices = skillChoices(state)
    choices.forEach((skill, i) => this.text(16, 12 + i * 3, `${i + 1}. ${skill.name} — ${skill.text}`, skill.stat === 'intellect' ? colors.purple : colors.text))
    if (!choices.length) this.text(16, 20, 'All disciplines are mastered.', colors.gold)
    this.text(16, 28, 'number chooses · Esc/backtick cancels', colors.dim)
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
