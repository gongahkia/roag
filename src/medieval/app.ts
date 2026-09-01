import { MedievalWorldRepository } from './storage'
import { MutableWorldSession } from './session'
import { JOMON_NON_COLOR_STATE_CUES, JOMON_PALETTE } from './palette'
import type { FoundationCrewMember, FoundationWorld, MedievalRoute, WorldChronicle, WorldIndex } from './types'
import { chronicleExport, chooseInitialCourier, createFoundationWorld } from './world'

type PersistenceState = 'loading' | 'saved' | 'error'

const palette = JOMON_PALETTE
const cues = JOMON_NON_COLOR_STATE_CUES
const width = 800
const height = 630
const left = 38
const lineHeight = 22
const contentWidth = width - left * 2
const terminalFont = '18px "BigBlueTerm", monospace'

const errorMessage = (error: unknown): string => error instanceof Error ? error.message : 'Unknown local storage error'

const row = (context: CanvasRenderingContext2D, line: number, text: string, color: string = palette.bodyText): void => {
  context.fillStyle = color
  context.fillText(text, left, 78 + line * lineHeight)
}

const wrapText = (context: CanvasRenderingContext2D, text: string): readonly string[] => {
  const words = text.trim().split(/\s+/u).filter(Boolean)
  if (!words.length) return ['']

  const splitLongWord = (word: string): readonly string[] => {
    const pieces: string[] = []
    let piece = ''
    for (const character of word) {
      if (!piece || context.measureText(`${piece}${character}`).width <= contentWidth) piece += character
      else { pieces.push(piece); piece = character }
    }
    if (piece) pieces.push(piece)
    return pieces
  }

  const lines: string[] = []
  let current = ''
  for (const word of words) {
    const next = current ? `${current} ${word}` : word
    if (context.measureText(next).width <= contentWidth) { current = next; continue }
    if (current) { lines.push(current); current = '' }
    if (context.measureText(word).width <= contentWidth) { current = word; continue }
    const pieces = splitLongWord(word)
    lines.push(...pieces.slice(0, -1))
    current = pieces.at(-1) ?? ''
  }
  if (current) lines.push(current)
  return lines
}

const wrappedRows = (context: CanvasRenderingContext2D, line: number, text: string, color: string = palette.bodyText): number => {
  const lines = wrapText(context, text)
  lines.forEach((wrapped, index) => row(context, line + index, wrapped, color))
  return line + lines.length
}

const rule = (context: CanvasRenderingContext2D, line: number): void => {
  context.strokeStyle = palette.panelBorder
  context.beginPath()
  context.moveTo(left, 86 + line * lineHeight)
  context.lineTo(width - left, 86 + line * lineHeight)
  context.stroke()
}

const activeCourier = (world: FoundationWorld): FoundationCrewMember | undefined => world.crew.find(member => member.id === world.manifest.initialCourierId)

export class MedievalApp {
  private readonly context: CanvasRenderingContext2D
  private readonly repository = new MedievalWorldRepository()
  private readonly session = new MutableWorldSession()
  private route: MedievalRoute = 'worlds'
  private index: WorldIndex = { version: 1, activeWorlds: [], chronicles: [] }
  private world: FoundationWorld | undefined
  private chronicle: WorldChronicle | undefined
  private selectedRow = 0
  private seed = 'jomon-foundation'
  private persistence: PersistenceState = 'loading'
  private error: string | undefined

  constructor(private readonly canvas: HTMLCanvasElement) {
    const context = canvas.getContext('2d')
    if (!context) throw new Error('Jomon needs a canvas-capable browser')
    this.context = context
    canvas.width = width
    canvas.height = height
    canvas.addEventListener('pointerdown', () => canvas.focus())
    window.addEventListener('keydown', event => this.handleKey(event))
    if ('fonts' in document) void document.fonts.load(terminalFont, 'JOMON').then(() => this.render()).catch(() => undefined)
  }

  async start(): Promise<void> {
    await this.refreshIndex()
    this.render()
  }

  private async refreshIndex(): Promise<void> {
    this.persistence = 'loading'
    this.render()
    try {
      this.index = await this.repository.loadIndex()
      this.persistence = 'saved'
      this.error = undefined
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
  }

  private async createWorld(): Promise<void> {
    const world = createFoundationWorld({ seed: this.seed })
    this.world = world
    this.selectedRow = 0
    this.persistence = 'loading'
    this.render()
    try {
      await this.repository.saveWorld(world)
      await this.refreshIndex()
      this.session.open(world.id)
      this.route = 'choose-courier'
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
    this.render()
  }

  private async selectCourier(): Promise<void> {
    if (!this.world) return
    const candidate = this.world.crew.filter(member => member.eligible)[this.selectedRow]
    if (!candidate) return
    this.persistence = 'loading'
    this.render()
    try {
      this.session.assertOwner(this.world.id)
      this.world = chooseInitialCourier(this.world, candidate.id)
      await this.repository.saveWorld(this.world)
      await this.refreshIndex()
      this.route = 'world'
      this.error = undefined
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
    this.render()
  }

  private async resumeSelectedWorld(): Promise<void> {
    const entry = this.index.activeWorlds[this.selectedRow]
    if (!entry) return
    this.persistence = 'loading'
    this.render()
    try {
      const world = await this.repository.loadWorld(entry.id)
      if (!world) throw new Error('Selected world is no longer present in local storage')
      this.session.open(world.id)
      this.world = world
      this.route = world.manifest.initialCourierId ? 'world' : 'choose-courier'
      this.selectedRow = 0
      this.persistence = 'saved'
      this.error = undefined
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
    this.render()
  }

  private async openSelectedChronicle(): Promise<void> {
    const entry = this.index.chronicles[this.selectedRow]
    if (!entry) return
    this.persistence = 'loading'
    this.render()
    try {
      const chronicle = await this.repository.loadChronicle(entry.id)
      if (!chronicle) throw new Error('Selected chronicle is no longer present in local storage')
      this.chronicle = chronicle
      this.route = 'chronicle'
      this.persistence = 'saved'
      this.error = undefined
    } catch (error) {
      this.persistence = 'error'
      this.error = errorMessage(error)
    }
    this.render()
  }

  private exportChronicle(): void {
    if (!this.chronicle) return
    const blob = new Blob([chronicleExport(this.chronicle)], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `${this.chronicle.world.manifest.creation.label.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-chronicle.json`
    link.click()
    URL.revokeObjectURL(url)
  }

  private handleKey(event: KeyboardEvent): void {
    if (event.metaKey || event.ctrlKey || event.altKey) return
    const key = event.key
    if (['ArrowUp', 'ArrowDown', 'Enter', 'Escape', 'Backspace', 'n', 'N', 'c', 'C', 'e', 'E'].includes(key) || key.length === 1) event.preventDefault()
    if (this.persistence === 'loading') return
    if (this.route === 'worlds') {
      if (key === 'n' || key === 'N') { this.route = 'create-world'; this.seed = 'jomon-foundation'; this.selectedRow = 0; this.render(); return }
      if (key === 'c' || key === 'C') { this.route = 'chronicles'; this.selectedRow = 0; this.render(); return }
      if (key === 'ArrowUp') { this.selectedRow = Math.max(0, this.selectedRow - 1); this.render(); return }
      if (key === 'ArrowDown') { this.selectedRow = Math.min(Math.max(0, this.index.activeWorlds.length - 1), this.selectedRow + 1); this.render(); return }
      if (key === 'Enter') void this.resumeSelectedWorld()
      return
    }
    if (this.route === 'create-world') {
      if (key === 'Escape') { this.route = 'worlds'; this.render(); return }
      if (key === 'Backspace') { this.seed = this.seed.slice(0, -1); this.render(); return }
      if (key === 'Enter') { void this.createWorld(); return }
      if (key.length === 1 && this.seed.length < 64 && /[\w .,'-]/u.test(key)) { this.seed += key; this.render() }
      return
    }
    if (this.route === 'choose-courier') {
      const eligible = this.world?.crew.filter(member => member.eligible) ?? []
      if (key === 'Escape') { this.releaseWorld(); this.route = 'worlds'; this.world = undefined; this.render(); return }
      if (key === 'ArrowUp') { this.selectedRow = Math.max(0, this.selectedRow - 1); this.render(); return }
      if (key === 'ArrowDown') { this.selectedRow = Math.min(Math.max(0, eligible.length - 1), this.selectedRow + 1); this.render(); return }
      if (key === 'Enter') void this.selectCourier()
      return
    }
    if (this.route === 'world') {
      if (key === 'Escape') { this.releaseWorld(); this.route = 'worlds'; this.world = undefined; this.selectedRow = 0; this.render() }
      return
    }
    if (this.route === 'chronicles') {
      if (key === 'Escape') { this.route = 'worlds'; this.selectedRow = 0; this.render(); return }
      if (key === 'ArrowUp') { this.selectedRow = Math.max(0, this.selectedRow - 1); this.render(); return }
      if (key === 'ArrowDown') { this.selectedRow = Math.min(Math.max(0, this.index.chronicles.length - 1), this.selectedRow + 1); this.render(); return }
      if (key === 'Enter') void this.openSelectedChronicle()
      return
    }
    if (this.route === 'chronicle') {
      if (key === 'Escape') { this.route = 'chronicles'; this.chronicle = undefined; this.render(); return }
      if (key === 'e' || key === 'E') this.exportChronicle()
    }
  }

  private releaseWorld(): void {
    if (this.world && this.session.current() === this.world.id) this.session.release(this.world.id)
  }

  private render(): void {
    const context = this.context
    context.fillStyle = palette.consoleGround
    context.fillRect(0, 0, width, height)
    context.fillStyle = palette.panelSurface
    context.fillRect(20, 20, width - 40, height - 40)
    context.strokeStyle = palette.panelBorder
    context.strokeRect(20.5, 20.5, width - 41, height - 41)
    context.font = terminalFont
    context.textBaseline = 'top'
    context.fillStyle = palette.titleText
    context.fillText('JOMON', left, 38)
    context.fillStyle = palette.mutedText
    context.fillText('RIVER AND COAST // FOUNDATION', left + 108, 39)
    rule(context, 0)
    this.canvas.dataset.route = this.route
    this.canvas.dataset.persistence = this.persistence
    delete this.canvas.dataset.worldId
    delete this.canvas.dataset.crewCount
    if (this.world) {
      this.canvas.dataset.worldId = this.world.id
      this.canvas.dataset.crewCount = String(this.world.crew.length)
    }
    const nextLine = this.route === 'worlds' ? this.renderWorlds(context)
      : this.route === 'create-world' ? this.renderCreateWorld(context)
        : this.route === 'choose-courier' ? this.renderCourierChoice(context)
          : this.route === 'world' ? this.renderWorld(context)
            : this.route === 'chronicles' ? this.renderChronicles(context)
              : this.renderChronicle(context)
    if (this.error) wrappedRows(context, Math.min(24, nextLine + 1), `${cues.errorPrefix} LOCAL STORAGE: ${this.error}`, palette.errorText)
  }

  private renderWorlds(context: CanvasRenderingContext2D): number {
    this.canvas.setAttribute('aria-label', `Jomon worlds. ${this.index.activeWorlds.length} active world${this.index.activeWorlds.length === 1 ? '' : 's'} and ${this.index.chronicles.length} finalized chronicle${this.index.chronicles.length === 1 ? '' : 's'}.`)
    row(context, 2, 'LOCAL WORLDS', palette.titleText)
    if (!this.index.activeWorlds.length) row(context, 4, 'No active worlds. Create a seeded river world to begin.', palette.mutedText)
    this.index.activeWorlds.forEach((entry, index) => row(context, 4 + index, `${index === this.selectedRow ? cues.selectionMarker : ' '} ${entry.label}  ${entry.initialCourierId ? cues.activeState : cues.awaitingCourierState}`, index === this.selectedRow ? palette.selectedText : palette.bodyText))
    rule(context, 14)
    row(context, 16, 'N  create world     ENTER  resume selected world', palette.actionText)
    row(context, 17, 'C  inspect finalized chronicles', palette.actionText)
    let line = wrappedRows(context, 19, 'Foundation only: seeded crew and local persistence are ready.', palette.mutedText)
    line = wrappedRows(context, line, 'The walkable deck, trade, travel, simulation, and combat follow the roadmap.', palette.mutedText)
    return line
  }

  private renderCreateWorld(context: CanvasRenderingContext2D): number {
    this.canvas.setAttribute('aria-label', `Create a seeded Jomon world. Current seed ${this.seed}.`)
    row(context, 2, 'CREATE SEEDED WORLD', palette.titleText)
    row(context, 4, 'Seed', palette.mutedText)
    let line = wrappedRows(context, 5, `${cues.selectionMarker} ${this.seed}_`, palette.selectedText)
    line = Math.max(8, line + 1)
    line = wrappedRows(context, line, 'Enter creates the deterministic foundation world.', palette.bodyText)
    line = wrappedRows(context, line, 'Changing seed or generation settings changes the household; no individual rerolls.', palette.mutedText)
    line = Math.max(line + 1, 11)
    return wrappedRows(context, line, 'Esc returns without changing local worlds.', palette.mutedText)
  }

  private renderCourierChoice(context: CanvasRenderingContext2D): number {
    const world = this.world
    if (!world) { this.route = 'worlds'; this.render(); return 0 }
    this.canvas.setAttribute('aria-label', `Choose an initial courier for ${world.manifest.creation.label}. ${world.crew.length} generated crew members are eligible.`)
    row(context, 2, `CHOOSE INITIAL COURIER // ${world.manifest.creation.label.toUpperCase()}`, palette.titleText)
    row(context, 3, `SEED ${world.manifest.creation.seed} // time remains 0`, palette.mutedText)
    let line = 5
    world.crew.filter(member => member.eligible).forEach((member, index) => {
      const color = index === this.selectedRow ? palette.selectedText : palette.bodyText
      line = wrappedRows(context, line, `${index === this.selectedRow ? cues.selectionMarker : ' '} ${member.name.toUpperCase()} // ${member.role.toUpperCase()} // CONVERSATION ${member.conversation}`, color)
      line = wrappedRows(context, line, `   ${member.history}.`, palette.mutedText)
    })
    return wrappedRows(context, Math.max(19, line + 1), 'Arrow keys choose. Enter confirms. Esc returns to local worlds.', palette.actionText)
  }

  private renderWorld(context: CanvasRenderingContext2D): number {
    const world = this.world
    if (!world) { this.route = 'worlds'; this.render(); return 0 }
    const courier = activeCourier(world)
    this.canvas.setAttribute('aria-label', `Jomon foundation world ${world.manifest.creation.label}, active courier ${courier?.name ?? 'unassigned'}.`)
    row(context, 2, `${world.manifest.creation.label.toUpperCase()} // FOUNDATION WORLD`, palette.titleText)
    row(context, 4, `ACTIVE COURIER  ${courier?.name.toUpperCase() ?? 'UNASSIGNED'} // ${courier?.role.toUpperCase() ?? 'NONE'}`, palette.statusReady)
    row(context, 5, `SEED  ${world.manifest.creation.seed} // WORLD TIME ${world.worldTime}`, palette.bodyText)
    let line = wrappedRows(context, 7, 'The household exists. Time has not advanced and no map has been generated yet.', palette.mutedText)
    line = wrappedRows(context, line, 'This is the clean persistence boundary before the walkable Jomon foundation.', palette.mutedText)
    rule(context, 14)
    return wrappedRows(context, Math.max(16, line + 1), 'Esc returns to local worlds. All gameplay actions are still roadmap work.', palette.actionText)
  }

  private renderChronicles(context: CanvasRenderingContext2D): number {
    this.canvas.setAttribute('aria-label', `${this.index.chronicles.length} finalized Jomon chronicles.`)
    row(context, 2, 'FINALIZED CHRONICLES', palette.titleText)
    if (!this.index.chronicles.length) row(context, 4, 'No finalized chronicles are stored locally.', palette.mutedText)
    this.index.chronicles.forEach((entry, index) => row(context, 4 + index, `${index === this.selectedRow ? cues.selectionMarker : ' '} ${entry.label} // ${entry.reason.toUpperCase()}`, index === this.selectedRow ? palette.selectedText : palette.bodyText))
    rule(context, 14)
    return wrappedRows(context, 16, 'Arrow keys choose. Enter inspects. Esc returns to local worlds.', palette.actionText)
  }

  private renderChronicle(context: CanvasRenderingContext2D): number {
    const chronicle = this.chronicle
    if (!chronicle) { this.route = 'chronicles'; this.render(); return 0 }
    this.canvas.setAttribute('aria-label', `Read-only Jomon chronicle ${chronicle.world.manifest.creation.label}, finalized by ${chronicle.reason}.`)
    row(context, 2, `${chronicle.world.manifest.creation.label.toUpperCase()} // READ-ONLY CHRONICLE`, palette.titleText)
    row(context, 4, `FINAL REASON  ${chronicle.reason.toUpperCase()}`, palette.statusRisk)
    row(context, 5, `SEED  ${chronicle.world.manifest.creation.seed} // WORLD TIME ${chronicle.world.worldTime}`, palette.bodyText)
    let line = 7
    chronicle.world.causalHistory.forEach(record => { line = wrappedRows(context, line, `${record.sequence}. ${record.detail}`, palette.mutedText) })
    rule(context, 14)
    return wrappedRows(context, Math.max(16, line + 1), 'E exports JSON. Esc returns to finalized chronicles.', palette.actionText)
  }
}
